using UnityEngine;
using System;

using Photon.Pun;
using Photon.Realtime;

namespace Com.MyCompany.MyGame
{
    public class MyLauncherPartialInstantiation : MonoBehaviourPunCallbacks
    {
        #region Public Fields

        public GameObject leftHandPrefab;
        public GameObject rightHandPrefab;
        public GameObject headbandPrefab;

        public GameObject originalLeftHand;
        public GameObject originalRightHand;
        public GameObject originalHeadband;

        public GameObject localRig;

        [Tooltip("for testing takeover")]
        public GameObject basketballPrefab;

        #endregion

        //private serializable fields are private fields which are made visible in the inspector via serialization
        // - the default for private fields is to be hidden in the inspector
        #region Private Serializable Fields

        /// <summary>
        /// The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created.
        /// </summary>
        [Tooltip("The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created")]
        [SerializeField]
        private byte maxPlayersPerRoom = 4;

        #endregion

        #region Private Fields

        /// <summary>
        /// This client's version number. Users are separated from each other by gameVersion (which allows you to make breaking changes).
        /// </summary>
        string gameVersion = "1";

        /// <summary>
        /// Keep track of the current process. Since connection is asynchronous and is based on several callbacks from Photon,
        /// we need to keep track of this to properly adjust the behavior when we receive call back by Photon.
        /// Typically this is used for the OnConnectedToMaster() callback.
        /// </summary>
        bool isConnecting;

        #endregion

        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        void Awake()
        {
            // #Critical
            // this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        void Start()
        {
            Connect();
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Start the connection process.
        /// - If already connected, we attempt joining a random room
        /// - if not yet connected, Connect this application instance to Photon Cloud Network
        /// </summary>
        public void Connect()
        {
            Debug.Log("checking connection...");

            // we check if we are connected or not, we join if we are, else we initiate the connection to the server.
            if (PhotonNetwork.IsConnected)
            {
                Debug.Log("already connected");

                // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
                PhotonNetwork.JoinRandomRoom();
            }
            else
            {
                Debug.Log("connecting to Photon Online Server");

                // #Critical, we must first and foremost connect to Photon Online Server.
                isConnecting = PhotonNetwork.ConnectUsingSettings();
                PhotonNetwork.GameVersion = gameVersion;
            }
        }

        #endregion

        #region Private Methods

        public void InstantiatePlayer(){
            String leftHandPrefabName = "Left Hand Model (networked)";

            // instantiates player's hand models over the network
            // - makes sure to set position and rotation of new object to that of object they're replacing in the rig
            GameObject leftHand = PhotonNetwork.Instantiate(leftHandPrefabName, 
                originalLeftHand.GetComponent<Transform>().position, 
                originalLeftHand.GetComponent<Transform>().rotation, 
                0);
            GameObject rightHand = PhotonNetwork.Instantiate(this.rightHandPrefab.name, 
                originalRightHand.GetComponent<Transform>().position, 
                originalRightHand.GetComponent<Transform>().rotation, 
                0);
            //instantiates player headband over the network
            GameObject headband = PhotonNetwork.Instantiate(this.headbandPrefab.name,
                originalHeadband.GetComponent<Transform>().position,
                originalHeadband.GetComponent<Transform>().rotation,
                0);

            //gets parents of hands
            GameObject leftHandParent = originalLeftHand.GetComponentInParent<Transform>().parent.gameObject;
            GameObject rightHandParent = originalRightHand.GetComponentInParent<Transform>().parent.gameObject;
            //gets parent of headband
            GameObject headbandParent = originalHeadband.GetComponentInParent<Transform>().parent.gameObject;

            leftHand.SetActive(false);
            rightHand.SetActive(false);
            headband.SetActive(false);

            //places player's hand models in the correct position under direct hands in the hierarchy
            leftHand.transform.parent = leftHandParent.transform;
            rightHand.transform.parent = rightHandParent.transform;
            //does the same to headband
            headband.transform.parent = headbandParent.transform;

            Destroy(originalLeftHand);
            Destroy(originalRightHand);
            Destroy(originalHeadband);

            leftHand.SetActive(true);
            rightHand.SetActive(true);
            headband.SetActive(true);

            //makes sure the listed GameObjects are destroyed when their owner leaves the room
            //DestroyOnOwnerLeave leftHandDestroy = leftHand.AddComponent(typeof(DestroyOnOwnerLeave)) as DestroyOnOwnerLeave;
            //DestroyOnOwnerLeave rightHandDestroy = rightHand.AddComponent(typeof(DestroyOnOwnerLeave)) as DestroyOnOwnerLeave;
            //DestroyOnOwnerLeave headbandDestroy = headband.AddComponent(typeof(DestroyOnOwnerLeave)) as DestroyOnOwnerLeave;

            //assigns hand animators of direct hands
            leftHandParent.GetComponent<AnimateHandOnInput>().handAnimator = leftHand.GetComponent<Animator>();
            rightHandParent.GetComponent<AnimateHandOnInput>().handAnimator = rightHand.GetComponent<Animator>();

            //instantiates a basketball over the network for testing purposes 
            //GameObject newBall = PhotonNetwork.Instantiate(this.basketballPrefab.name, 
            //    new Vector3(0, 0, 0), 
            //    Quaternion.identity, 
            //    0);
        }

        #endregion

        #region MonoBehaviourPunCallbacks Callbacks

        /// <summary>
        /// Runs when connection to master server is established 
        /// The master server handles connection to rooms, a responsibility that is reflected in the JoinRandomRoom call below
        /// <summary>
        public override void OnConnectedToMaster()
        {
            Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");

            // we don't want to do anything if we are not attempting to join a room.
            // this case where isConnecting is false is typically when you lost or quit the game, when this level is loaded, OnConnectedToMaster will be called, in that case
            // we don't want to do anything.
            if (isConnecting)
            {
                // #Critical: The first we try to do is to join a potential existing room. If there is, good, else, we'll be called back with OnJoinRandomFailed()
                PhotonNetwork.JoinRandomRoom();
                isConnecting = false;
            }
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}", cause);
            isConnecting = false;
        }

        //called if JoinRandomRoom fails
        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");

            // #Critical: we failed to join a random room, maybe none exists or they are all full. No worries, we create a new room.
                //the max players count is set using the private serialized maxPlayersPerRoom variable initialized in the inspector
            PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom, CleanupCacheOnLeave = false });
        }

        //called if JoinRandomRoom succeeds
        public override void OnJoinedRoom()
        {
            Debug.Log("PUN Basics Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");

            InstantiatePlayer();
        }

        //called when local player leaves room
        public override void OnLeftRoom()
        {
            PhotonNetwork.Destroy(localRig);
        }

        /**
        public override void OnPlayerLeftRoom(Player other)
        {    
            //meant to delete all objects belonging to the player that's leaving, besides basketballs instantiated by them
            if(PhotonNetwork.IsMasterClient){
                //gets every photon view in the scene
                PhotonView[] views = GameObject.FindObjectsOfType<PhotonView>();

                //checks every photonview to see if its owner is the player that left and destroys it if it is
                foreach(PhotonView view in views){
                    GameObject viewParent  = null;
                    
                    viewParent = view.GetComponentInParent<Transform>().parent.gameObject;

                    if(viewParent.GetComponent<IsBasketball>() == null){
                        if(view.Owner == other){
                            //destroys the parent of the view
                            PhotonNetwork.Destroy(viewParent);
                        }
                    }
                    
                    try {
                        viewParent = view.GetComponentInParent<Transform>().parent.gameObject;

                        //makes sure the parent of the view isn't a basketball
                        try {
                            IsBasketball component = viewParent.GetComponent<IsBasketball>();
                            Debug.Log("basketball status: " + component);
                        }
                        catch (Exception e) {
                            Debug.Log("Inner block called");

                            if(view.Owner == other){
                                //destroys the parent of the view
                                PhotonNetwork.Destroy(viewParent);
                            }
                        }
                    }
                    catch (Exception exception) {
                        Debug.Log("Outer block called");
                    }
                    
                }
            }
        }
        */

        #endregion
    }
}