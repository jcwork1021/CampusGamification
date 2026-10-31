using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.LowLevelPhysics2D.PhysicsLayers;

public class ChatManager : MonoBehaviour
{
    [Header("Necessary SO")]
    [SerializeField] RememberMeRefSO _rememberMeSO;
    [SerializeField] ChatManagerRefSO _chatManagerSO;
    [SerializeField] GameManagerRefSO _gameManagerSO;

    [Header("Pubnub Reference")]
    [SerializeField] PNManager _pnManager;

    [Header("Channel Names")]
    public string globalChannelName = "GlobalChannel"; //calls by PNManager.cs as well

    [Header("Reference from Chat_Canvas")]
    [SerializeField] GameObject _chatCanvas;
    [SerializeField] GameObject _chatPanel;
    [SerializeField] TMP_InputField chatInputField;
    [SerializeField] Button sendButton;
    [SerializeField] Button globalTabButton;
    [SerializeField] Button roomTabButton;

    [Header("UI - Scroll Views for Global Channel")]
    [SerializeField] GameObject globalScrollView;
    [SerializeField] Transform globalContentParent;
    [SerializeField] ScrollRect globalScrollRect;

    [Header("UI - Scroll Views for Room/Session Channel")]
    [SerializeField] GameObject roomScrollView;
    [SerializeField] Transform roomContentParent;
    [SerializeField] ScrollRect roomScrollRect;

    [Header("Prefabs")]
    [SerializeField] MessagePrefab playerMessagePrefab; // Right side
    [SerializeField] MessagePrefab otherMessagePrefab;  // Left side

    [Header("Read-Only")]
    [Tooltip("Used to saved the current channel name so we can unsubscribe to it later")]
    [SerializeField] string _roomChannelName;
    public string micAccess; //calls by VivoxPermissionHandler.cs
    public string micStatus; //calls by VivoxPermissionHandler.cs

    [SerializeField] string activeChannel;
    [SerializeField] bool _isConnecting = false;
    [SerializeField] bool _isVivoxConnected = false;
    [SerializeField] bool _isJoinedRoomChat = false;
    [SerializeField] bool _isMuted = false;
    [SerializeField] bool _forcedMute;
    [SerializeField] ChatCapability capability;

    public bool ForcedMute => _forcedMute;

    #region Unity Functions
    void Awake()
    {
        if (_chatManagerSO.Instance != null && _chatManagerSO.Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Don't Destroy because we need this to retain from lobby to anywhere in the world
        _chatManagerSO.Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (_chatManagerSO.Instance == this)
        {
            _chatManagerSO.Instance = null;
        }

        // Safely unsubscribe only if Vivox was initialized
        if (_isVivoxConnected && VivoxService.Instance != null)
        {
            VivoxService.Instance.ChannelMessageReceived -= OnChannelMessageReceived;
        }
    }

    private void OnEnable()
    {
        if (_pnManager != null)
        {
            _pnManager.OnGlobalMessageReceived += OnPubNubGlobalMessageReceived;
        }
    }

    private void OnDisable()
    {
        if (_pnManager != null)
        {
            _pnManager.OnGlobalMessageReceived -= OnPubNubGlobalMessageReceived;
        }
    }

    void Start()
    {
        // Setup UI Listeners
        sendButton.onClick.AddListener(SendMessage);
        chatInputField.onSubmit.AddListener(delegate { SendMessage(); });

        globalTabButton.onClick.AddListener(SelectGlobalTab);
        roomTabButton.onClick.AddListener(SelectRoomTab);

        // Default to Global Tab
        SelectGlobalTab();

        // Setting Default UI
        //_chatCanvas.SetActive(false);
    }

    #endregion

    #region Calls in EventListener
    public void CallsInOnLoadedLobby()
    {
        _chatCanvas.SetActive(false);
    }

    //Use to Listen on PNManager.cs OnPNMessage
    private void OnPubNubGlobalMessageReceived(string sender, string message)
    {
        if (globalContentParent == null)
            return;

        // Check if the message was sent by the local player
        bool isLocalPlayer = (sender == _rememberMeSO.Instance.playerName);

        // Use the same prefab logic as Vivox
        MessagePrefab prefabToUse =
            isLocalPlayer ? playerMessagePrefab : otherMessagePrefab;

        MessagePrefab newMsg = Instantiate(prefabToUse, globalContentParent);

        // Display sender + message, same as Vivox
        newMsg.SetText($"<b>{sender}:</b> {message}");

        while (globalContentParent.childCount > maxMessageCount)
        {
            Destroy(globalContentParent.GetChild(0).gameObject);
        }

        StartCoroutine(ScrollToBottomNextFrame(globalScrollRect));
    }

    #endregion

    #region Functions

    //Calls in PlayerManager.cs
    public void ForcedToggleMute(bool forcedMute)
    {
        _forcedMute = forcedMute;

        if (_forcedMute)
        {
            _isMuted = false; //Set this first so when we call ToggleMute, it will do mute instead of un mute
            ToggleVoiceMute();
        }
        else
        {
            
        }
    }

    #endregion

    #region Click UI
    public void ClickExitChat()
    {
        _chatCanvas.SetActive(false);
    }
    #endregion

    #region Connected to Input Actions call by InputActionManager.cs
    //When Enter is pressed
    public void EnableDisableChatCanvas()
    {
        if (activeChannel == globalChannelName)
        {
            if (!_pnManager.pubNubConnected)
            {
                _rememberMeSO.Instance.EnableWarning("Global Chat is not ready yet, please try again later");
            }
        }
        else
        {
            if (!_isJoinedRoomChat)
            {
                _rememberMeSO.Instance.EnableWarning("Room Chat is not ready yet, please try again later");
            }
        }

        if (_chatCanvas.activeSelf)
        {
            if (string.IsNullOrWhiteSpace(chatInputField.text))
            {
                _chatCanvas.SetActive(false);
                Debug.LogError("EX");
            }
        }
        else
        {
            _chatCanvas.SetActive(true);
            _chatPanel.SetActive(true);
            chatInputField.ActivateInputField();
        }
    }

    //When M is pressed
    public void ToggleVoiceMute()
    {
        if (!_isVivoxConnected) return;

        if (_gameManagerSO.Instance == null) return;

        if (micStatus != "MIC_CONNECTED") return; //we cannot toggle this if we no longer have mic

        if (_chatCanvas.activeSelf) return; //So we dont toggle mute while player si typing

        Debug.Log("Pressed M");

        _isMuted = !_isMuted;

        if (_isMuted)
        {
            //Direct method call
            VivoxService.Instance.MuteInputDevice();
            _gameManagerSO.Instance.MuteMic(); //This is for UI Display Bruh
            Debug.Log("<color=red>[Vivox Voice] Microphone MUTED</color>");
            

            //Ask the playerManager.cs to ask the GameManager to mark the localplayer mute or unmute in the playerList.. since this is a NetworkBehaviour we cannot 

        }
        else
        {
            if (_forcedMute) 
            {
                _isMuted = !_isMuted; //return to original
                return; //means someone set all players to muted
            }

            //Direct method call
            VivoxService.Instance.UnmuteInputDevice();
            _gameManagerSO.Instance.UnMuteMic(); //This is for UI Display Bruh
            Debug.Log("<color=green>[Vivox Voice] Microphone UNMUTED</color>");
        }
    }
    #endregion

    #region Vivox Connection
    /// <summary>
    /// Call this once your Fusion player is spawned or logged in so you have their username!
    /// </summary>
    public async void ConnectToVivox() //Calls by EventListener when OnLoadedLobby has been raised
    {
        try
        {
            await ConnectToVivoxAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox Chat] Unhandled top-level exception: {ex.Message}");
        }
    }

    public async Task ConnectToVivoxAsync()
    {
        Debug.Log("[Vivox] Starting connection sequence...");

        // 1. Guard against concurrent calls
        if (_isConnecting)
        {
            Debug.LogWarning("[ChatManager] Connection already in progress. Ignoring request.");
            return;
        }

        _isConnecting = true;

        try
        {
            // 2. Initialize Unity Gaming Services FIRST
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                Debug.Log("[Vivox] Initializing Unity Services...");
                await UnityServices.InitializeAsync();
            }

            while (UnityServices.State == ServicesInitializationState.Initializing)
            {
                await Task.Yield();
            }

            // 3. Sign in Anonymously
            if (AuthenticationService.Instance != null && !AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[Vivox] Signing in anonymously...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            if (AuthenticationService.Instance != null)
            {
                Debug.Log($"[Vivox] Signed in with Player ID: {AuthenticationService.Instance.PlayerId}");
            }

            // 4. Initialize Vivox Service
            Debug.Log("[Vivox] Initializing Vivox Service...");
            await VivoxService.Instance.InitializeAsync();

            // 5. NOW check if already logged in (Safe to check after InitializeAsync)
            if (VivoxService.Instance.IsLoggedIn)
            {
                Debug.LogWarning("[ChatManager] Already logged in to Vivox!");
                _isVivoxConnected = true;
                return;
            }

            if (_rememberMeSO == null || _rememberMeSO.Instance == null)
            {
                Debug.LogError("No Reference for RememberMe.cs cannot assign name");
                return;
            }

            // 6. Safe Event Re-subscription
            VivoxService.Instance.ChannelMessageReceived -= OnChannelMessageReceived;
            VivoxService.Instance.ChannelMessageReceived += OnChannelMessageReceived;

            _isConnecting = false; //Early because for some reason in webgl build it is not proceeding to logged in part and need to be called multiple times first

            // 7. Perform Login
            Debug.Log($"[Vivox] Logging in as '{_rememberMeSO.Instance.playerName}'...");
            LoginOptions options = new LoginOptions { DisplayName = _rememberMeSO.Instance.playerName };
            await VivoxService.Instance.LoginAsync(options);

            _isVivoxConnected = true;
            Debug.Log("[Vivox] Successfully connected!");

        }
        catch (Exception ex)
        {
            Debug.LogError($"[Vivox Chat] Connection error: {ex.Message}\n{ex.StackTrace}");
            _isVivoxConnected = false;
        }
    }
    #endregion

    #region Channel Management

    public void JoinGlobalChannel() //Calls by PNManager.cs after Initializing it
    {
        if (_pnManager == null)
        {
            Debug.LogError("[ChatManager] PNManager reference is missing!");
            return;
        }

        _pnManager.SubscribeToGlobalChannel(globalChannelName);
    }


    /// <summary>
    /// Call this when entering a session (e.g. Runner.SessionInfo.Name)
    /// NOTE: calls by GameManager.cs after spawning a player locally
    /// </summary> 

    public async void JoinRoomChat()
    {
        if (!_isVivoxConnected)
        {
            //Try to Reconnect first if succesful then we can proceed joining a room
            await ConnectToVivoxAsync();
        }

        if (micAccess == "DENIED")
        {
            capability = ChatCapability.TextOnly;
        }
        else //GRANTED or UNSUPPORTED
        {
            capability = ChatCapability.TextAndAudio;
        }

        //Unsubscribe to Prev Channel if there is any
        UnsubscribeToPrevChannel();
        _roomChannelName = _rememberMeSO.Instance.sessionName;

        try
        {
            await VivoxService.Instance.JoinGroupChannelAsync(_roomChannelName, capability);
            _isJoinedRoomChat = true;
            //Debug.LogError($"[Vivox Chat] Joined Room Channel ({capability}): {_roomChannelName}");

            if (micAccess == "DENIED")
            {
                _rememberMeSO.Instance.EnableWarning("Connected to Room Chat. No Voice");
                _gameManagerSO.Instance.MuteMic();
            }
            else if (micAccess == "UNSUPPORTED")
            {
                _rememberMeSO.Instance.EnableWarning("Connected to Room Chat. No Mic");
                _gameManagerSO.Instance.MuteMic();
            }
            else //GRANTED
            {
                _rememberMeSO.Instance.EnableWarning("Connected to Room Chat with Mic");
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"[Vivox Chat] Failed to join channel: {e.Message}");
        }
    }

    /// <summary>
    /// NOTE: calls by LobbyManager.cs, since going back to LobbyManager scene means player left the prev session
    /// </summary> 
    public async void UnsubscribeToPrevChannel()
    {
        // Check if we actually have a room name and active channel connections in Vivox
        if (!string.IsNullOrEmpty(_roomChannelName) && _isVivoxConnected)
        {
            try
            {
                // Verify that Vivox is currently in this channel before trying to leave
                if (VivoxService.Instance.ActiveChannels.ContainsKey(_roomChannelName))
                {
                    await VivoxService.Instance.LeaveChannelAsync(_roomChannelName);
                    _isJoinedRoomChat = false;
                    Debug.Log($"[Vivox Chat] Successfully left room: {_roomChannelName}");
                }
            }
            catch (System.Exception ex)
            {
                // Catch KeyNotFoundException gracefully in WebGL
                Debug.LogWarning($"[Vivox Chat] Ignored safe leave warning: {ex.Message}");
            }

            _roomChannelName = string.Empty; // Reset room channel name
        }

        // Clear prev UI messages
        if (roomContentParent != null)
        {
            for (int i = roomContentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(roomContentParent.GetChild(i).gameObject);
            }
        }
    }

    #endregion

    #region Tab Switching Logic

    public void SelectGlobalTab()
    {
        activeChannel = globalChannelName;
        globalScrollView.SetActive(true);
        roomScrollView.SetActive(false);

        // Button colors
        globalTabButton.GetComponent<Image>().color = Color.green;
        roomTabButton.GetComponent<Image>().color = Color.white;
    }

    public void SelectRoomTab()
    {
        if (!_isVivoxConnected)
        {
            _rememberMeSO.Instance.EnableWarning("Not Connected to Vivox yet");
            ConnectToVivox();
            return;
        }

        if (!_isJoinedRoomChat)
        {
            _rememberMeSO.Instance.EnableWarning("Not Connected to Room Chat yet");
            JoinRoomChat();
            return;
        }

        activeChannel = _roomChannelName;
        globalScrollView.SetActive(false);
        roomScrollView.SetActive(true);

        // Button colors
        globalTabButton.GetComponent<Image>().color = Color.white;
        roomTabButton.GetComponent<Image>().color = Color.green;
    }

    #endregion

    #region Sending & Receiving Messages

    public async void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(chatInputField.text)) return;
        if (string.IsNullOrEmpty(activeChannel)) return;

        string text = chatInputField.text;

        if (activeChannel == globalChannelName)
        {
            if (!_pnManager.pubNubConnected)
            {
                _rememberMeSO.Instance.EnableWarning("Global Chat is not ready yet, Try again later");
                return;
            }

            await _pnManager.PublishGlobalMessage(_rememberMeSO.Instance.playerName, text);
            //return;
        }
        else
        {
            // Embed currentUserName directly into the payload: "DisplayName|ActualMessage"
            string formattedMessage = $"{_rememberMeSO.Instance.playerName}|{text}";

            await VivoxService.Instance.SendChannelTextMessageAsync(activeChannel, formattedMessage);
        }

        // Reset Input Field
        chatInputField.text = string.Empty;
        chatInputField.ActivateInputField();

    }

    [SerializeField] private int maxMessageCount = 40;

    private void OnChannelMessageReceived(VivoxMessage message)
    {
        string channelName = message.ChannelName;
        string rawMessage = message.MessageText;

        string sender = _rememberMeSO.Instance.playerName; // Fallback
        string cleanMessageText = rawMessage;

        // Parse embedded sender name if present
        if (rawMessage.Contains("|"))
        {
            string[] parts = rawMessage.Split(new char[] { '|' }, 2);
            sender = parts[0];
            cleanMessageText = parts[1];
        }
        else if (!string.IsNullOrEmpty(message.SenderDisplayName))
        {
            sender = message.SenderDisplayName;
        }

        bool isGlobal = channelName.Equals(globalChannelName);
        Transform parentToUse = isGlobal ? globalContentParent : roomContentParent;
        ScrollRect scrollRectToUse = isGlobal ? globalScrollRect : roomScrollRect;

        if (parentToUse == null) return;

        // Local player check comparing parsed sender name vs current local user name
        bool isLocalPlayer = (sender == _rememberMeSO.Instance.playerName);

        MessagePrefab prefabToUse = isLocalPlayer ? playerMessagePrefab : otherMessagePrefab;

        MessagePrefab newMsg = Instantiate(prefabToUse, parentToUse);
        newMsg.SetText($"<b>{sender}:</b> {cleanMessageText}");

        while (parentToUse.childCount > maxMessageCount)
        {
            Destroy(parentToUse.GetChild(0).gameObject);
        }

        StartCoroutine(ScrollToBottomNextFrame(scrollRectToUse));
    }

    IEnumerator ScrollToBottomNextFrame(ScrollRect targetScrollRect)
    {
        yield return new WaitForEndOfFrame();
        if (targetScrollRect != null)
        {
            targetScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    #endregion
}