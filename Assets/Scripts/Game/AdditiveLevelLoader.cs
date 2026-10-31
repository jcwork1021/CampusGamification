using Fusion;
using Fusion.Sockets;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class AdditiveLevelLoader : NetworkBehaviour, INetworkRunnerCallbacks
{
    [Header("Data Reference")]
    [SerializeField] RememberMeRefSO _rememberMeSO;
    [SerializeField] AdditiveLevelLoaderRefSO _additiveLevelLoaderSO;

    [Header("Player Settings")]
    [SerializeField] string completePath;

    [Header("Read-Only")]
    [SerializeField] string sceneChosen;

    public UnityEvent onAdditiveSceneLoadedSuccesfully;

    void Awake()
    {
        _additiveLevelLoaderSO.Instance = this;
    }

    private void OnDestroy()
    {
        _additiveLevelLoaderSO.Instance = null;
    }

    public override void Spawned()
    {
        // Register this component to listen for Fusion scene load callbacks
        Runner.AddCallbacks(this);

        // ONLY the Master Client initiates scene loading across the network
        if (Runner.IsSharedModeMasterClient)
        {
            string basedPath = "Scenes/";

            if (_rememberMeSO.Instance.sceneTransitionSO.sceneName == null)
            {
                Debug.LogError("Does not know which scene because rememberMe sceneTransitionSO is null");
            }
            else
            {
                // Load additive scene 
                sceneChosen = _rememberMeSO.Instance.sceneTransitionSO.sceneName;
            }

            completePath = $"{basedPath}{sceneChosen}";

            StartLoadingSequence(completePath);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Clean up callbacks when object is despawned
        Runner.RemoveCallbacks(this);
    }

    // Only Host is calling this
    private async void StartLoadingSequence(string path)
    {
        try
        {
            await LoadStageAsync(path);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AdditiveLevelLoader] Failed to load scene: {e}");
            _rememberMeSO.Instance.ToggleRememberMeCanvas(false);
        }
    }

    public async Task LoadStageAsync(string scenePathOrName)
    {
        if (!Runner.IsSharedModeMasterClient) return;

        if (string.IsNullOrEmpty(scenePathOrName))
        {
            Debug.LogError("[AdditiveLevelLoader] Scene name/path provided is null or empty!");
            return;
        }

        int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePathOrName);
        if (buildIndex == -1)
        {
            buildIndex = SceneManager.GetSceneByName(scenePathOrName).buildIndex;
        }

        if (buildIndex == -1)
        {
            Debug.LogError($"[AdditiveLevelLoader] Scene '{scenePathOrName}' not found in Build Settings!");
            return;
        }

        Debug.Log($"[AdditiveLevelLoader] Master Client requesting additive scene load: {scenePathOrName}...");

        var sceneRef = SceneRef.FromIndex(buildIndex);

        // Set initial state
        _rememberMeSO.Instance.EnableLoading("Loading 0%...");

        // Initiate Fusion's Scene Loading and await completion
        await Runner.LoadScene(sceneRef, LoadSceneMode.Additive);

        _rememberMeSO.Instance.EnableLoading("Loading 100%...");
    }

    int sceneLoadedCounter; // 1 is the Game Scene, 2 is the additive scene

    #region Fusion Callbacks

    // Fusion triggers this automatically during async scene loading
    public void OnSceneLoadProgress(NetworkRunner runner, float progress)
    {
        // progress spans from 0.0f to 1.0f
        Debug.Log("On Scene Load Progress");

        int progressPercent = Mathf.RoundToInt(progress * 100f);
        _rememberMeSO.Instance.EnableLoading($"Loading {progressPercent}%...");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        sceneLoadedCounter += 1;

        if (sceneLoadedCounter >= 2)
        {
            onAdditiveSceneLoadedSuccesfully.Invoke();
        }
    }

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
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    #endregion
}