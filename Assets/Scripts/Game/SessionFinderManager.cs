using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class SessionFinderManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Necessary SO")]
    [SerializeField] RememberMeRefSO _rememberMeSO;
    [SerializeField] SessionFinderManagerRefSO _sessionFinderManagerSO;
    [SerializeField] GameManagerRefSO _gameManagerSO;
    [SerializeField] InteractableSessionRefSO _interactableSessionSO;

    [Header("Player SO")]
    [SerializeField] SOInteractor _playerInteractorSO;

    [Header("UI Panels")]
    [SerializeField] Canvas _sessionCanvas;
    [SerializeField] GameObject _createPanel;
    [SerializeField] GameObject _joinPanel;
    [SerializeField] GameObject _passwordPanel;

    [Header("Reference from Create_Panel")]
    [SerializeField] TMP_InputField _passwordInput;
    [SerializeField] TMP_InputField _maxPlayerInput;

    [Header("Reference from Password_Panel")]
    [SerializeField] TMP_InputField _enterPasswordInput;
    [SerializeField] TMP_Text _passwordWarningTxt;

    [Header("Join Room UI")]
    public Transform roomListContent;
    public GameObject joinRoomBtnPrefab;

    [Header("Read-Only")]
    [SerializeField] int _maxPlayers;
    public string requiredPasswordByRoom; //Fill by JoinRoomDetails.cs when they are selected
    public string sessionName;

    public NetworkRunner _searchRunner;
    private Dictionary<string, GameObject> _roomListUI = new Dictionary<string, GameObject>();

    #region Unity Functions
    private void Awake()
    {
        EnableLobbyCanvas(false);
        _sessionFinderManagerSO.Instance = this;
    }

    private void OnDestroy()
    {
        _sessionFinderManagerSO.Instance = null;
        if (_rememberMeSO.Instance.runner != null)
        {
            _rememberMeSO.Instance.runner.RemoveCallbacks(this);
        }
    }

    private void Start()
    {
        if (_rememberMeSO.Instance.runner != null)
        {
            _rememberMeSO.Instance.runner.AddCallbacks(this);
        }
        else
        {
            Debug.LogError("No Active Runner");
        }
    }
    #endregion

    #region Calls by other script
    public async void OpenStageBrowserUI() //Calls in InteractableSession.cs
    {
        EnableLobbyCanvas(true);

        // 1. Create a lightweight temporary runner JUST for searching rooms
        if (_searchRunner == null)
        {
            GameObject searchObj = new GameObject("Temp_SessionSearchRunner");
            _searchRunner = searchObj.AddComponent<NetworkRunner>();

            // Prevent this search runner from attempting to load scenes!
            _searchRunner.ProvideInput = false;
        }

        // 2. Attach callbacks to the SEARCH runner
        _searchRunner.AddCallbacks(this);

        // 3. Join the lobby using ONLY the search runner
        try
        {
            await _searchRunner.JoinSessionLobby(SessionLobby.Shared);
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkSceneLoader] Failed to fetch session list: {e}");
        }
    }

    public void OpenPasswordPanel()
    {
        _passwordWarningTxt.text = "";
        _passwordPanel.SetActive(true);
    }

    public void JoinTheIntendedRoom() //Calls by both ClickEnterPassword() and other script JoinRoomDetails.cs
    {
        //Send Data what session to join and mark to join automatically
        _rememberMeSO.Instance.sessionName = sessionName;
        _rememberMeSO.Instance.automaticallyJoin = true;

        //Leave the current session
        _rememberMeSO.Instance.EnableLoading($"Preparing...");

        //Ask the InteractableManager which is our Handler to end this Interactable first before doing Transition
        _interactableSessionSO.Instance.ExitInteractable();

        _gameManagerSO.Instance.PlayerExitingGameSession();
    }
    #endregion

    #region UI Button Handlers

    //Called by CreateRoom_Btn from Create_Panel
    public void ClickCreateRoom()
    {
        Debug.LogWarning("Session Clicked create room");


        int.TryParse(_maxPlayerInput.text, out _maxPlayers);

        if (_maxPlayers < 2)
        {
            Debug.LogWarning("Max Player is not supposed to be less than 2");
            return; // Safe to return now—runners are still alive!
        }

        if (_maxPlayers > 20) // Fixed > 20 check
        {
            Debug.LogWarning("Max Player is not supposed to be greater than 20");
            return;
        }

        _rememberMeSO.Instance.playerLimit = _maxPlayers;
        _rememberMeSO.Instance.scenePassword = _passwordInput.text;
        _rememberMeSO.Instance.automaticallyHost = true;

        DestorySearchRunner();

        /*
        if (_interactableSessionSO != null)
        {
            //Ask the InteractableManager which is our Handler to end this Interactable first before doing Transition
            _interactableSessionSO.Instance.AskInteractableManagerToExit();
        }
        else
        {
            Debug.LogError("No Interactable Session SO");
        }
        */

        //Leave first
        _rememberMeSO.Instance.EnableLoading($"Preparing...");
        _gameManagerSO.Instance.PlayerExitingGameSession();

        Debug.LogWarning("Session Player Exited Session");
    }

    public void ClickExitPasswordPanel()
    {
        _passwordPanel.SetActive(false);
    }

    public void ClickExitInteraction()
    {
        EnableLobbyCanvas(false);

        DestorySearchRunner();

        if (_interactableSessionSO.Instance != null)
        {
            _interactableSessionSO.Instance.ExitInteractable();
        }
    }

    private async void DestorySearchRunner()
    {
        // Clean up ONLY the temporary search runner
        if (_searchRunner != null)
        {
            _searchRunner.RemoveCallbacks(this);

            if (_searchRunner.IsRunning)
            {
                await _searchRunner.Shutdown();
            }

            Destroy(_searchRunner.gameObject);
            _searchRunner = null;
        }
    }

    public void ClickEnterPassword()
    {
        _passwordWarningTxt.text = "";

        if (_enterPasswordInput.text == requiredPasswordByRoom)
        {
            JoinTheIntendedRoom();
        }
        else
        {
            Debug.LogWarning("Incorrect Room Password");
            _passwordWarningTxt.text = "Incorrect Password";
            return; //for safety
        }
    }

    #endregion

    #region Reusable Functions

    public void EnableLobbyCanvas(bool yeah)
    {
        if (_sessionCanvas != null)
        {
            _sessionCanvas.gameObject.SetActive(yeah);

            /*
            _lobbyCanvas.alpha = yeah ? 1 : 0;
            _lobbyCanvas.blocksRaycasts = yeah;
            */
        }
    }

    #endregion

    #region Fusion Callbacks (Session List Update)

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // 1. Clear old list items
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }
        _roomListUI.Clear();

        if (_rememberMeSO.Instance.sceneTransitionSO == null)
        {
            Debug.LogError("Cannot find matching Session because rememberMe sceneTransitionSO is null");
            return;
        }

        // Get target stage filter from your ScriptableObject (or active UI filter setting)
        string targetStageFilter = _rememberMeSO.Instance.sceneTransitionSO.sceneName;

        // 2. Populate open, visible, and stage-matched session list items
        foreach (SessionInfo session in sessionList)
        {
            if (!session.IsVisible || !session.IsOpen)
                continue;

            // Check if custom properties exist
            if (session.Properties != null && session.Properties.TryGetValue("StageName", out var stageProp))
            {
                string sessionStage = stageProp.PropertyValue?.ToString();

                // Filter out any session that does not match the target StageName
                if (!string.Equals(sessionStage, targetStageFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue; // Skip rooms that don't match the current stage
                }
            }
            else
            {
                // Optional fallback: decide if you want to skip rooms missing the StageName property entirely
                continue;
            }

            // Instantiate matching room card
            GameObject roomEntry = Instantiate(joinRoomBtnPrefab, roomListContent);
            JoinRoomDetails details = roomEntry.GetComponent<JoinRoomDetails>();

            if (details != null)
            {
                details.Setup(session, this);
            }
        }
    }

    #endregion

    #region Unused INetworkRunnerCallbacks Required Boilerplate
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}