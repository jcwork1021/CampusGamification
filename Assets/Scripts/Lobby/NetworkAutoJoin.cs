using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkAutomaticJoin : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Necessary SO")]
    [SerializeField] RememberMeRefSO _rememberMeSO;
    [SerializeField] NetworkAutomaticJoinRefSO _networkAutomaticJoinSO;

    [Header("Scene SO")]
    [SerializeField] SceneTransitionSO _defaultSceneTransitionSO;

    [Header("Network Setup")]
    public NetworkRunner runnerPrefab;

    [Header("Room Settings")]
    [Tooltip("Default Player Capacity")]
    [SerializeField] int maxPlayersPerInstance = 20;

    private bool _hasAttemptedJoin = false;
    private CancellationTokenSource _loadingCts;

    #region Unity Functions
    void Awake()
    {
        _networkAutomaticJoinSO.Instance = this;
    }

    void OnDestroy()
    {
        _networkAutomaticJoinSO.Instance = null;
        CancelLoadingTracker();

        if (_rememberMeSO.Instance != null && _rememberMeSO.Instance.runner != null)
        {
            _rememberMeSO.Instance.runner.RemoveCallbacks(this);
        }
    }

    private void Start()
    {
        CleanupRunner();

        if (_rememberMeSO.Instance.runner == null)
        {
            _rememberMeSO.Instance.runner = Instantiate(runnerPrefab);
        }

        _rememberMeSO.Instance.runner.AddCallbacks(this);

        if (_rememberMeSO.Instance.automaticallyHost)
        {
            if (!CheckIfWeHaveSceneTransition())
            {
                _rememberMeSO.Instance.EnableWarning("No Valid Destination for Hosting Room");
                return;
            }
            CreateRoom();
            return;
        }

        if (_rememberMeSO.Instance.automaticallyJoin)
        {
            if (!CheckIfWeHaveSceneTransition())
            {
                _rememberMeSO.Instance.EnableWarning("No Valid Destination for Joining Session");
                return;
            }
            JoinSelectedRoom();
            return;
        }

        if (_rememberMeSO.Instance.sceneTransitionSO != null)
        {
            if (_rememberMeSO.Instance.sceneTransitionSO.autoJoin)
            {
                AutoJoin();
                return;
            }
        }

        _rememberMeSO.Instance.sceneTransitionSO = _defaultSceneTransitionSO;
    }
    #endregion

    #region Calls in Other Script
    public async void AutoJoin()
    {
        if (!CheckIfWeHaveSceneTransition())
        {
            _rememberMeSO.Instance.EnableWarning("No Valid Destination for Auto Join");
            return;
        }

        Debug.Log($"<color=cyan>[FUSION CONNECTION]</color> AutoJoin Step: Entering Session Lobby (Shared Mode)...");

        try
        {
            StartLoadingTracker("Searching Sessions");
            var result = await _rememberMeSO.Instance.runner.JoinSessionLobby(SessionLobby.Shared);
            if (result.Ok)
            {
                Debug.Log($"<color=green>[FUSION CONNECTION]</color> Successfully joined Shared Lobby. Waiting for room list...");
            }
            else
            {
                Debug.LogError($"<color=red>[FUSION CONNECTION]</color> Failed to join lobby. ShutdownReason: {result.ShutdownReason}");
                CancelLoadingTracker();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AutomaticJoin] Failed to join lobby: {e}");
            _rememberMeSO.Instance.ToggleRememberMeCanvas(false);
            CleanupRunner();
        }
    }

    public async void CreateRoom()
    {
        await PrepareRunnerForStartGame();

        int sceneIndex = SceneUtility.GetBuildIndexByScenePath(_rememberMeSO.Instance.gameScenePath);
        if (sceneIndex == -1)
        {
            sceneIndex = SceneManager.GetActiveScene().buildIndex;
        }

        var sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(SceneRef.FromIndex(sceneIndex), LoadSceneMode.Single);

        string roomName = $"{_rememberMeSO.Instance.sceneTransitionSO.sceneName}{UnityEngine.Random.Range(1000, 9999)}";
        string hostName = string.IsNullOrEmpty(_rememberMeSO.Instance.playerName) ? "Host" : _rememberMeSO.Instance.playerName;
        int playerCount = _rememberMeSO.Instance.playerLimit;
        string stageName = string.IsNullOrEmpty(_rememberMeSO.Instance.sceneTransitionSO.sceneName) ? "Default Stage" : _rememberMeSO.Instance.sceneTransitionSO.sceneName;
        int allowedLevel = _rememberMeSO.Instance.playerLevel;
        string roomPassword = _rememberMeSO.Instance.scenePassword ?? "";

        var customProps = new Dictionary<string, SessionProperty>
        {
            { "HostName", (SessionProperty)hostName },
            { "StageName", (SessionProperty)stageName },
            { "AllowedLevel", (SessionProperty)allowedLevel },
            { "Password", (SessionProperty)roomPassword }
        };

        _rememberMeSO.Instance.sessionName = roomName;
        byte[] token = GetConnectionToken();

        Debug.Log($"<color=cyan>[FUSION CONNECTION]</color> StartGame Step: Hosting Room '{roomName}'...");

        try
        {
            StartLoadingTracker($"Preparing {_rememberMeSO.Instance.sceneTransitionSO.sceneName}");

            var result = await _rememberMeSO.Instance.runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Shared,
                SessionName = roomName,
                PlayerCount = playerCount,
                SceneManager = _rememberMeSO.Instance.runner.GetComponent<INetworkSceneManager>(),
                SessionProperties = customProps,
                Scene = sceneInfo,
                ConnectionToken = token
            });

            if (!result.Ok)
            {
                Debug.LogError($"<color=red>[FUSION CONNECTION]</color> Failed to Host Room: {result.ShutdownReason}");
                _rememberMeSO.Instance.ToggleRememberMeCanvas(false); //To disable loading
                CleanupRunner();
            }
            else
            {
                Debug.Log($"<color=green>[FUSION CONNECTION]</color> Successfully hosted room '{roomName}' as Shared Host!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"StartGame threw: {e}");
            _rememberMeSO.Instance.ToggleRememberMeCanvas(false); // To disable loading
            CleanupRunner();
        }
    }

    public async void JoinSelectedRoom()
    {
        string roomName = _rememberMeSO.Instance.sessionName;

        await PrepareRunnerForStartGame();
        byte[] token = GetConnectionToken();

        Debug.Log($"<color=cyan>[FUSION CONNECTION]</color> StartGame Step: Joining Room '{roomName}' as Client...");

        try
        {
            StartLoadingTracker($"Joining Selected Room: {roomName}");

            var sceneManager = _rememberMeSO.Instance.runner.GetComponent<INetworkSceneManager>();

            var result = await _rememberMeSO.Instance.runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Shared,
                SessionName = roomName,
                SceneManager = sceneManager,
                ConnectionToken = token
            });

            if (!result.Ok)
            {
                Debug.LogError($"<color=red>[FUSION CONNECTION]</color> [JOIN ROOM FAILED] Reason: {result.ShutdownReason}");
                CancelLoadingTracker();
                _rememberMeSO.Instance.ToggleRememberMeCanvas(false); //To disable loading, along with entire remember me canvas
                CleanupRunner();
            }
            else
            {
                Debug.Log($"<color=green>[FUSION CONNECTION]</color> Successfully joined room '{roomName}'!");

                // Stop progress tracker and dismiss loading screen once connected
                CancelLoadingTracker();
                //_rememberMeSO.Instance.DisableRememberMeCanvas();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[JOIN ROOM ERROR] Exception: {e}");
            CancelLoadingTracker();
            _rememberMeSO.Instance.ToggleRememberMeCanvas(false); //To disable loading, along with entire remember me canvas
            CleanupRunner();
        }
    }
    #endregion

    #region Fusion Callbacks (Auto-Matching Logic)
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (_hasAttemptedJoin) return;
        _hasAttemptedJoin = true;

        Debug.Log($"<color=cyan>[FUSION CONNECTION]</color> Session List Received. Found {sessionList.Count} session(s). Searching for scene '{_rememberMeSO.Instance.sceneTransitionSO.sceneName}'...");

        SessionInfo targetSession = null;

        foreach (SessionInfo session in sessionList)
        {
            if (session.IsVisible && session.IsOpen && session.PlayerCount < session.MaxPlayers)
            {
                if (session.Properties != null && session.Properties.TryGetValue("globalSceneName", out var sceneProp))
                {
                    string roomGlobalScene = sceneProp.PropertyValue as string;
                    if (roomGlobalScene == _rememberMeSO.Instance.sceneTransitionSO.sceneName)
                    {
                        targetSession = session;
                        break;
                    }
                }
            }
        }

        if (targetSession != null)
        {
            Debug.Log($"<color=green>[AUTO-JOIN]</color> Found open room for scene '{_rememberMeSO.Instance.sceneTransitionSO.sceneName}'. Joining '{targetSession.Name}'...");
            JoinAutomaticExistingRoom(targetSession.Name);
        }
        else
        {
            Debug.Log($"<color=yellow>[AUTO-HOST]</color> No open room found for '{_rememberMeSO.Instance.sceneTransitionSO.sceneName}'. Hosting new instance...");
            HostAutomaticNewRoom(_rememberMeSO.Instance.sceneTransitionSO.sceneName);
        }
    }
    #endregion

    #region Auto Join / Host Core Logic
    private async void JoinAutomaticExistingRoom(string roomName)
    {
        await PrepareRunnerForStartGame();
        byte[] token = GetConnectionToken();
        _rememberMeSO.Instance.sessionName = roomName;

        Debug.Log($"<color=cyan>[FUSION CONNECTION]</color> StartGame Step: Auto-Joining Existing Room '{roomName}'...");

        try
        {
            StartLoadingTracker($"Joining Room: {roomName}");

            var result = await _rememberMeSO.Instance.runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Shared,
                SessionName = roomName,
                SceneManager = _rememberMeSO.Instance.runner.GetComponent<INetworkSceneManager>(),
                ConnectionToken = token
            });

            if (!result.Ok)
            {
                Debug.LogError($"<color=red>[FUSION CONNECTION]</color> [AUTO-JOIN FAILED] Reason: {result.ShutdownReason}");
                _rememberMeSO.Instance.ToggleRememberMeCanvas(false); //To disable loading, along with entire remember me canvas
                CleanupRunner();
            }
            else
            {
                Debug.Log($"<color=green>[FUSION CONNECTION]</color> Auto-Join successful for room '{roomName}'!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AUTO-JOIN ERROR] Exception: {e}");
            _rememberMeSO.Instance.ToggleRememberMeCanvas(false); //To disable loading, along with entire remember me canvas
            CleanupRunner();
        }
    }

    private async void HostAutomaticNewRoom(string globalSceneName)
    {
        await PrepareRunnerForStartGame();

        int sceneIndex = SceneUtility.GetBuildIndexByScenePath(_rememberMeSO.Instance.gameScenePath);
        if (sceneIndex == -1)
        {
            sceneIndex = SceneManager.GetActiveScene().buildIndex;
        }

        var sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(SceneRef.FromIndex(sceneIndex), LoadSceneMode.Single);

        var customProps = new Dictionary<string, SessionProperty>
        {
            { "globalSceneName", globalSceneName }
        };

        string roomName = $"{globalSceneName}{UnityEngine.Random.Range(1000, 9999)}";
        _rememberMeSO.Instance.sessionName = roomName;
        byte[] token = GetConnectionToken();

        Debug.Log($"<color=cyan>[FUSION CONNECTION]</color> StartGame Step: Auto-Hosting Room '{roomName}'...");

        try
        {
            StartLoadingTracker($"Hosting scene: {globalSceneName}");

            var result = await _rememberMeSO.Instance.runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Shared,
                SessionName = roomName,
                PlayerCount = maxPlayersPerInstance,
                SceneManager = _rememberMeSO.Instance.runner.GetComponent<INetworkSceneManager>(),
                SessionProperties = customProps,
                Scene = sceneInfo,
                ConnectionToken = token
            });

            if (!result.Ok)
            {
                Debug.LogError($"<color=red>[FUSION CONNECTION]</color> [AUTO-HOST FAILED] Reason: {result.ShutdownReason}");
                _rememberMeSO.Instance.ToggleRememberMeCanvas(false); //To disable loading, along with entire remember me canvas
                CleanupRunner();
            }
            else
            {
                Debug.Log($"<color=green>[FUSION CONNECTION]</color> Auto-Host successful for room '{roomName}'!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AUTO-HOST ERROR] Exception: {e}");
            _rememberMeSO.Instance.ToggleRememberMeCanvas(false); //To disable loading, along with entire remember me canvas
            CleanupRunner();
        }
    }
    #endregion

    #region Helper Functions & Progress Tracker
    private void StartLoadingTracker(string taskName)
    {
        CancelLoadingTracker();
        _loadingCts = new CancellationTokenSource();
        _ = TrackLoadingProgress(taskName, _loadingCts.Token);
    }

    private void CancelLoadingTracker()
    {
        if (_loadingCts != null)
        {
            _loadingCts.Cancel();
            _loadingCts.Dispose();
            _loadingCts = null;
        }
    }

    private async Task TrackLoadingProgress(string taskName, CancellationToken cancellationToken)
    {
        float progress = 0f;
        float timeoutTimer = 0f;
        const float maxTimeout = 20f; // 20-second safety timeout

        while (!cancellationToken.IsCancellationRequested &&
               _rememberMeSO.Instance.runner != null &&
               !_rememberMeSO.Instance.runner.IsRunning)
        {
            timeoutTimer += Time.deltaTime;
            if (timeoutTimer > maxTimeout)
            {
                Debug.LogWarning($"[ProgressTracker] Timed out waiting for runner to start task: {taskName}");
                break;
            }

            if (!_rememberMeSO.Instance.runner.IsStarting && !_rememberMeSO.Instance.runner.IsRunning)
            {
                progress = Mathf.MoveTowards(progress, 25f, Time.deltaTime * 60f);
            }
            else if (_rememberMeSO.Instance.runner.IsStarting)
            {
                progress = Mathf.MoveTowards(progress, 75f, Time.deltaTime * 60f);
            }

            int progressAmount = Mathf.RoundToInt(progress);
            _rememberMeSO.Instance.EnableLoading($"{taskName}... {progressAmount}%");

            await Task.Yield();
        }
    }

    public bool CheckIfWeHaveSceneTransition()
    {
        if (_rememberMeSO.Instance.sceneTransitionSO == null || string.IsNullOrEmpty(_rememberMeSO.Instance.sceneTransitionSO.sceneName))
        {
            Debug.LogError("No Valid SceneTransitionSO or SceneName in RememberMe");
            return false;
        }
        return true;
    }

    private async Task PrepareRunnerForStartGame()
    {
        // 1. If a runner exists and is running/starting, shut it down cleanly first
        if (_rememberMeSO.Instance.runner != null)
        {
            if (_rememberMeSO.Instance.runner.IsRunning || _rememberMeSO.Instance.runner.IsStarting)
            {
                _rememberMeSO.Instance.runner.RemoveCallbacks(this);
                await _rememberMeSO.Instance.runner.Shutdown();
            }

            // CRITICAL: Always destroy the old GameObject to guarantee clean internal state
            Destroy(_rememberMeSO.Instance.runner.gameObject);
            _rememberMeSO.Instance.runner = null;

            // Yield a frame so Unity flushes the destroyed object reference
            await Task.Yield();
        }

        // 2. Instantiate a fresh NetworkRunner instance
        _rememberMeSO.Instance.runner = Instantiate(runnerPrefab);
        _rememberMeSO.Instance.runner.AddCallbacks(this);

        // 3. Ensure NetworkSceneManagerDefault exists on the runner
        if (_rememberMeSO.Instance.runner.GetComponent<INetworkSceneManager>() == null)
        {
            _rememberMeSO.Instance.runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }
    }

    private byte[] GetConnectionToken()
    {
        return System.Guid.NewGuid().ToByteArray();
    }

    private void CleanupRunner()
    {
        CancelLoadingTracker();
        if (_rememberMeSO.Instance != null && _rememberMeSO.Instance.runner != null)
        {
            Destroy(_rememberMeSO.Instance.runner.gameObject);
            _rememberMeSO.Instance.runner = null;
        }
    }
    #endregion

    #region INetworkRunnerCallbacks Trace Logging
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log($"<color=green>[FUSION CALLBACK]</color> Connected to server/relay successfully! Network Mode: {runner.Mode}");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"<color=yellow>[FUSION CALLBACK]</color> Disconnected from server. Reason: {reason}");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"<color=red>[FUSION CALLBACK]</color> Connection failed to address {remoteAddress}. Reason: {reason}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.LogWarning($"<color=yellow>[FUSION CALLBACK]</color> NetworkRunner Shutdown. Reason: {shutdownReason}");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"<color=cyan>[FUSION CALLBACK]</color> Player Joined: {player} (Local Player: {player == runner.LocalPlayer})");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"<color=orange>[FUSION CALLBACK]</color> Player Left: {player}");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
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