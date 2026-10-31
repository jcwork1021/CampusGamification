using Fusion;
using Fusion.Sockets;
using Newtonsoft.Json.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Unity.Collections.Unicode;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class GameManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    [Header("Necessary SO")]
    [SerializeField] RememberMeRefSO _rememberMeSO;
    [SerializeField] GameManagerRefSO _gameManagerSO;
    [SerializeField] ReceiverManagerRefSO _receiverManagerRefSO;
    [SerializeField] ChatManagerRefSO _chatManagerSO;

    [Header("Player SO")]
    [SerializeField] PlayerManagerRefSO _playerManagerSO;

    [Header("Interactable SO")]
    [SerializeField] InteractableRefSO _interactableSO;
    [SerializeField] InteractableChairRefSO _interactableChairSO;

    [Header("Player Camera")]
    [SerializeField] Camera _gameCamera;
    [SerializeField] CameraDitherOccluder _cameraDitherOccluder;
    [SerializeField] CinemachineCamera _thirdPersonCinemachine;

    [Header("Player Controls UI")]
    [SerializeField] Canvas _playerControlCanvas;
    [SerializeField] CanvasGroup _mainControllerPanel;
    [SerializeField] GameObject _movementJoystick;
    [SerializeField] GameObject _lookJoystick;
    [SerializeField] Image _micImg;
    [SerializeField] TMP_Text statusTxt;
    [SerializeField] CanvasGroup _playerListScrollView;
    [SerializeField] Transform _playerListParent;
    [SerializeField] GameObject _maximizePlayerListBtn;
    [SerializeField] Button _putHandDownBtn;

    [Header("Other Canvas")]
    [SerializeField] Canvas _chairViewerFPPCanvas;

    [Header("Avatars")]
    [SerializeField] NetworkObject _maleAvatar;
    [SerializeField] NetworkObject _femaleAvatar;

    [Header("Game Event")]
    [SerializeField] GameEvent _onPlayerSpawned;

    [Header("Unity Event")]
    public UnityEvent _onPlayerInstantiated;
    public UnityEvent _onAddPlayerOnTheList;
    public UnityEvent _onRemovePlayerOnTheList;

    [Header("Scene Settings")]
    [SerializeField] string _lobbySceneName = "Lobby";

    [Header("Reference from Project")]
    [SerializeField] Sprite _muteSprite;
    [SerializeField] Sprite _unMuteSprite;
    [SerializeField] Button _playerListButtonPrefab;

    [Header("Fill via scripts")]
    public NetworkObject _localPlayerNetworkObject;
    public PlayerRef _localPlayerRef;
    public List<PlayerRef> _networkedPlayerList;
    [SerializeField] int sceneLoadedCounter; // Tracks how many scenes loaded locally

    [Header("WebGL Background Settings")]
    [SerializeField] float _webGLBackgroundTimeoutSeconds = 30f;

    bool isQuitting = false;
    bool isPlayerExiting = false;

    public PlayerRef LocalPlayerRef => _localPlayerRef;

    public readonly Dictionary<PlayerRef, GameObject> _playerButtons = new Dictionary<PlayerRef, GameObject>();

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern void StartBackgroundTimer(
        string gameObjectName,
        string callbackMethod,
        float durationSeconds
    );

    [DllImport("__Internal")]
    static extern void CancelBackgroundTimer();
#endif

    #region Unity Functions
    void Awake()
    {

        this.enabled = true;

        if (_gameManagerSO != null)
        {
            _gameManagerSO.Instance = this;
        }
        else
        {
            Debug.LogError("No Game Manager Reference");
        }

        ToggleChairFPPCanvas(false);
        ToggleRaiseHand(false);
    }

    void Start()
    {
        this.enabled = true;

        if (_rememberMeSO != null && _rememberMeSO.Instance != null && _rememberMeSO.Instance.runner != null)
        {
            _rememberMeSO.Instance.runner.AddCallbacks(this);
        }
        else
        {
            Debug.LogError("[GameManager] No Active Runner found on Start!");
        }
    }

    void OnDisable()
    {
        // Cancel all pending delay coroutines when disabled/destroyed
        StopAllCoroutines();
    }

    void OnDestroy()
    {
        StopAllCoroutines();

        if (_gameManagerSO != null)
        {
            _gameManagerSO.Instance = null;
        }

        if (_rememberMeSO != null && _rememberMeSO.Instance != null && _rememberMeSO.Instance.runner != null)
        {
            _rememberMeSO.Instance.runner.RemoveCallbacks(this);
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (isQuitting || isPlayerExiting)
            return;

#if UNITY_WEBGL && !UNITY_EDITOR
        if (!hasFocus)
        {
            if (statusTxt != null) statusTxt.text = "IDLE";
            Debug.LogWarning($"[WebGL] Game lost focus. Starting background timeout: {_webGLBackgroundTimeoutSeconds} seconds.");
            StartBackgroundTimer(gameObject.name, nameof(OnBackgroundTimerExpired), _webGLBackgroundTimeoutSeconds);
        }
        else
        {
            if (statusTxt != null) statusTxt.text = "ACTIVE";
            Debug.Log("[WebGL] Game regained focus. Cancelling background timer.");
            CancelBackgroundTimer();
        }
#endif
    }
    #endregion

    #region JSLib Callbacks
    public void OnBackgroundTimerExpired(string message)
    {
        if (message != "EXPIRED") return;
        if (isQuitting || isPlayerExiting) return;

        Debug.LogWarning("[WebGL] Background timeout expired. Returning player to Lobby.");
        if (_rememberMeSO != null && _rememberMeSO.Instance != null)
        {
            _rememberMeSO.Instance.timedOutError = true;
        }

        PlayerExitingGameSession();
    }
    #endregion

    #region Input & Exit Flow

    public async void PlayerExitingGameSession() //Calls in InputActionManager.cs when Exited
    {
        if (isQuitting) return;
        isQuitting = true;
        isPlayerExiting = true;

        StopAllCoroutines();

#if UNITY_WEBGL && !UNITY_EDITOR
        CancelBackgroundTimer();
#endif

        NetworkRunner runner = (Runner != null) ? Runner : (_rememberMeSO != null && _rememberMeSO.Instance != null ? _rememberMeSO.Instance.runner : null);

        if (runner != null && runner.IsRunning)
        {
            // Runner.Shutdown invokes OnShutdown, which handles LoadLobby cleanly
            await runner.Shutdown(destroyGameObject: true, shutdownReason: ShutdownReason.Ok);
        }
        else
        {
            LoadLobby();
        }
    }

    public void LoadLobby()
    {
        if (_rememberMeSO != null && _rememberMeSO.Instance != null)
        {
            _rememberMeSO.Instance.EnableMainCamera(true);
            _rememberMeSO.Instance.EnableLoading("Returning to Lobby, Please Wait..");
        }

        SceneManager.LoadSceneAsync(_lobbySceneName);
    }
    #endregion

    #region INetworkRunnerCallbacks - Spawning Logic
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Fusion] Player joined: {player}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer && _localPlayerNetworkObject != null)
        {
            runner.Despawn(_localPlayerNetworkObject);
            _localPlayerNetworkObject = null;
            Debug.Log($"<color=red>[DESPAWNED]</color> Fusion Local Player left.");
        }
    }

    public async void OnSceneLoadDone(NetworkRunner runner)
    {
        sceneLoadedCounter += 1;

        bool isHostOrMaster = runner.IsServer || runner.IsSharedModeMasterClient;
        int requiredSceneCount = isHostOrMaster ? 2 : 1;

        if (sceneLoadedCounter >= requiredSceneCount)
        {
            await Task.Delay(1000);

            // Perform your spawner logic here directly or via an async method
            FindSpawnerFromLoadedScene();

            //Debug.LogError($"[OnSceneLoadDone] Required scene count ({requiredSceneCount}) reached! Spawning player...");
        }
        else
        {
            Debug.Log($"[OnSceneLoadDone] Scene loaded ({sceneLoadedCounter}/{requiredSceneCount}). Waiting for next scene load...");
        }
    }
    #endregion

    #region Spawner Logic
    public void FindSpawnerFromLoadedScene()
    {
        if (isQuitting)
        {
            Debug.LogError("Player Quitting so Return");
            return;
        }


        if (Runner == null || !Runner.IsRunning)
        {
            Debug.LogError("[GameManager] NetworkRunner is null or not running!");
            return;
        }

        if (!this.gameObject.activeSelf)
        {
            Debug.LogError("GameManager is Inactive");
        }
        else
        {
            StartCoroutine(DelayFindingSpawner());
        }
    }

    IEnumerator DelayFindingSpawner()
    {
        yield return new WaitForSeconds(1f);

        if (Runner == null || !Runner.IsRunning || isQuitting) yield break;

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        string targetReceiverName = _rememberMeSO?.Instance?.sceneTransitionSO?.receiverName;
        Debug.Log($"[Spawner] Searching for Receiver with name: '{targetReceiverName}'...");

        ReceiverObject entrance = GetFirstReceiverByName(targetReceiverName);

        if (entrance == null)
        {
            Debug.LogWarning($"[Spawner] FAILED: Cannot Find Receiver '{targetReceiverName}'. Defaulting to (0,0,0).");
        }
        else
        {
            var spawnList = entrance.ReceiverPositions;
            if (spawnList == null || spawnList.Count == 0)
            {
                Debug.LogError($"[Spawner] FAILED: ReceiverPositions empty on '{entrance.name}'. Defaulting to (0,0,0).");
            }
            else
            {
                int positionIndex = UnityEngine.Random.Range(0, spawnList.Count);
                Transform chosenTransform = spawnList[positionIndex];

                if (chosenTransform != null)
                {
                    spawnPos = chosenTransform.position;
                    spawnRot = chosenTransform.rotation;
                }
            }
        }

        SpawnLocalPlayer(spawnPos, spawnRot);
    }

    private ReceiverObject GetFirstReceiverByName(string targetName)
    {
        if (_receiverManagerRefSO == null || _receiverManagerRefSO.Instance == null || string.IsNullOrEmpty(targetName))
            return null;

        foreach (ReceiverObject receiver in _receiverManagerRefSO.Instance.receivers)
        {
            if (receiver != null && receiver.ReceiverName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                return receiver;
            }
        }
        return null;
    }

    private void SpawnLocalPlayer(Vector3 position, Quaternion rotation)
    {
        if (Runner.GetPlayerObject(Runner.LocalPlayer) != null)
        {
            //Debug.LogError("[GameManager] Local player avatar already exists!");
            return;
        }

        NetworkObject selectedPrefab = (_rememberMeSO != null && _rememberMeSO.Instance != null && _rememberMeSO.Instance.avatarGender == "Male")
            ? _maleAvatar
            : _femaleAvatar;

        if (selectedPrefab == null)
        {
            Debug.LogError("[GameManager] Selected Avatar is missing/null in Inspector!");
            return;
        }

        _localPlayerNetworkObject = Runner.Spawn(
            selectedPrefab,
            position,
            rotation,
            Runner.LocalPlayer
        );

        _localPlayerRef = _localPlayerNetworkObject.InputAuthority;

        /*
        if (_cameraDitherOccluder != null)
        {
            _cameraDitherOccluder.playerTarget = _localPlayerNetworkObject.transform;
        }
        */

        Runner.SetPlayerObject(Runner.LocalPlayer, _localPlayerNetworkObject);
        //Debug.LogError($"[GameManager] Local player successfully spawned at {position}" + "Player: " + _localPlayerNetworkObject.transform.position);

        _onPlayerSpawned?.Raise();
        _onPlayerInstantiated?.Invoke();

        if (_chatManagerSO != null && _chatManagerSO.Instance != null && _rememberMeSO != null && _rememberMeSO.Instance != null)
        {
            _chatManagerSO.Instance.JoinRoomChat();
            _rememberMeSO.Instance.ResetSceneSessionData();
        }
    }

    #endregion

    #region Called by other scripts
    public void EnableWhatPlayerNeed(Transform cinemachineTarget) //Calls by PlayerManager.cs
    {
        if (_rememberMeSO != null && _rememberMeSO.Instance != null) _rememberMeSO.Instance.EnableMainCamera(false);
        if (_gameCamera != null) _gameCamera.gameObject.SetActive(true);

        TogglePlayerCanvas(true);

        if (_thirdPersonCinemachine != null)
        {
            _thirdPersonCinemachine.Target.TrackingTarget = cinemachineTarget;
            _thirdPersonCinemachine.gameObject.SetActive(true);
        }

        //Disable Analog UI if its not WebGL
#if UNITY_WEBGL && !UNITY_EDITOR
        _movementJoystick.SetActive(false);
        _lookJoystick.SetActive(false);
#endif
    }

    public void MuteMic() //Calls by ChatManager.cs
    {
        if (_micImg != null && _muteSprite != null) _micImg.sprite = _muteSprite;

        //Change the network variable to it can sync the status across the scene.
        if (_playerManagerSO.Instance != null)
        {
            _playerManagerSO.Instance.networkedMicMuted = true;
        }
    }

    public void UnMuteMic() //Calls by ChatManager.cs
    {
        if (_micImg != null && _unMuteSprite != null) _micImg.sprite = _unMuteSprite;

        //Change the network variable to it can sync the status across the scene.
        if (_playerManagerSO.Instance != null)
        {
            _playerManagerSO.Instance.networkedMicMuted = false;
        }
    }

    public void CreatePlayerButton(PlayerRef player, string displayName) //Calls by PlayerManager.cs
    {
        // 1. If a button for this player already exists, update its name instead of spawning a new one
        if (_playerButtons.TryGetValue(player, out GameObject existingButton))
        {
            if (existingButton != null)
            {
                PlayerListButton existingPlb = existingButton.GetComponent<PlayerListButton>();
                if (existingPlb != null)
                {
                    existingPlb.SetupContent(displayName);

                    if (_chatManagerSO.Instance.ForcedMute)
                    {
                        existingPlb.SetupMicStatus(_muteSprite);
                    }
                }
                return;
            }
        }

        // 2. Otherwise, spawn a new button
        GameObject newButton = Instantiate(_playerListButtonPrefab.gameObject, _playerListParent);

        PlayerListButton plb = newButton.GetComponent<PlayerListButton>();
        if (plb != null)
        {
            plb.SetupContent(displayName);
        }

        Button playerBtn = newButton.GetComponent<Button>();

        playerBtn.interactable = false;

        playerBtn.onClick.AddListener(() => KickAnotherPlayer(player));

        // 3. Register it in our dictionary
        _playerButtons[player] = newButton;

        //Add on our dictionary
        _networkedPlayerList.Add(player); //this is on list

        _onAddPlayerOnTheList?.Invoke();
    }

    public void RemovedPlayerButton(PlayerRef player) //Calls by PlayerManager.cs
    {
        // Find and destroy the player's button when they disconnect/despawn
        if (_playerButtons.TryGetValue(player, out GameObject buttonToDestroy))
        {
            if (buttonToDestroy != null)
            {
                Destroy(buttonToDestroy);
            }

            _playerButtons.Remove(player);

            _networkedPlayerList.Remove(player); //this is on list
        }

        _onRemovePlayerOnTheList?.Invoke();
    }


    public void UpdateMicStatusNetworked(PlayerRef player, bool muted) //Calls by PlayerManager.cs
    {
        if (_playerButtons.TryGetValue(player, out GameObject existingButton))
        {
            if (existingButton != null)
            {
                PlayerListButton existingPlb = existingButton.GetComponent<PlayerListButton>();
                if (existingPlb != null)
                {
                    if (muted)
                    {
                        existingPlb.SetupMicStatus(_muteSprite);
                    }
                    else
                    {
                        existingPlb.SetupMicStatus(_unMuteSprite);
                    }
                }
                return;

            }

        }
    }

    public void UpdateRaiseHandNetworked(PlayerRef player, bool raisedHand) //Calls by PlayerManager.cs
    {
        if (_playerButtons.TryGetValue(player, out GameObject existingButton))
        {
            if (existingButton != null)
            {
                PlayerListButton existingPlb = existingButton.GetComponent<PlayerListButton>();
                if (existingPlb != null)
                {

                    existingPlb.SetupRaiseHand(raisedHand);
                }
                return;
            }
        }
    }

    public void SetUpPublicRoom() //Calls by LevelManager.cs
    {
        _playerListScrollView.gameObject.SetActive(false);
        _maximizePlayerListBtn.SetActive(false);
    }

    public void SetupPrivateRoom() //Calls by LevelManager.cs
    {
        _playerListScrollView.gameObject.SetActive(true);
        _maximizePlayerListBtn.SetActive(false);
    }

    #endregion

    #region Functions

    // Call this method passing the target PlayerRef
    public void KickAnotherPlayer(PlayerRef targetPlayer)
    {
        // Send the RPC strictly to the targeted player
        Rpc_KickPlayerTarget(targetPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_KickPlayerTarget([RpcTarget] PlayerRef targetPlayer)
    {
        _rememberMeSO.Instance.kicked = true;

        // This code executes ONLY on the kicked player's client
        Debug.LogError("You have been kicked from the session.");

        // Shut down the local runner to disconnect from the room
        Runner.Shutdown();
    }

    #endregion

    #region Click UI

    public void ClickExitChairViewer() //onclick of ViewerExit_Btn from ChairViewer_Canvas
    {
        _chairViewerFPPCanvas.gameObject.SetActive(false);
        _interactableSO.Instance.ToggleSecondInteractable(false);

        _interactableChairSO.Instance.ExitFPPInteractable();
    }

    public void ClickMinimizePlayerList()
    {
        TogglePanel(_playerListScrollView, false);
        _maximizePlayerListBtn.SetActive(true);
    }

    public void ClickMaximizePlayerList()
    {
        TogglePanel(_playerListScrollView, true);
        _maximizePlayerListBtn.SetActive(false);
    }

    public void ClickPutHandDown()
    {
        _interactableChairSO.Instance.ExitRaiseHand();
    }

    private void OnClickKickPlayer()
    {

    }

    #endregion

    #region Toggle Functions
    public void TogglePlayerCanvas(bool yeah) //Calls by other script like abstract class BaseInteractable.cs
    {
        _playerControlCanvas.enabled = yeah;
    }

    public void ToggleMainController(bool visible)
    {
        TogglePanel
            (_mainControllerPanel, visible);
    }

    public void ToggleChairFPPCanvas(bool yeah)
    {
        _chairViewerFPPCanvas.enabled = yeah;
    }

    public void ToggleRaiseHand(bool yeah)
    {

        if (yeah)
        {
            TogglePanel(_mainControllerPanel, false);
            _putHandDownBtn.gameObject.SetActive(true);
        }
        else
        {
            TogglePanel(_mainControllerPanel, true);
            _putHandDownBtn.gameObject.SetActive(false);
        }
    }

    private void TogglePanel(CanvasGroup cg, bool visible)
    {
        // 0 = no display, 1 = fully visible
        cg.alpha = visible ? 1f : 0f;
        // Prevents interacting with buttons on this panel when false
        cg.interactable = visible;
        // Stops this panel from blocking clicks to elements/gameplay behind it when false
        cg.blocksRaycasts = visible;
    }

    #endregion

    #region INetworkRunnerCallbacks
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        StopAllCoroutines();

        if (_rememberMeSO != null && _rememberMeSO.Instance != null)
        {
            _rememberMeSO.Instance.runner = null;
        }

        if (!isPlayerExiting)
        {
            Debug.LogError($"Fusion unexpectedly disconnected. Reason: {shutdownReason}");
        }

        LoadLobby();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[Fusion] OnDisconnectedFromServer: {reason}");
        if (_rememberMeSO != null && _rememberMeSO.Instance != null)
        {
            _rememberMeSO.Instance.disconnectedServerError = true;
        }
        // Let OnShutdown handle LoadLobby to prevent duplicate async scene loads
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}