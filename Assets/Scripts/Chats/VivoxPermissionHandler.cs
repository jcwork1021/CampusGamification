using System.Runtime.InteropServices;
using UnityEngine;

public class VivoxPermissionHandler : MonoBehaviour
{
    [Header("Necessary SO")]
    [SerializeField] RememberMeRefSO _rememberMeSO;
    [SerializeField] ChatManagerRefSO _chatManagerSO;
    [SerializeField] GameManagerRefSO _gameManagerSO;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RequestBrowserMicPermission(string gameObjectName, string callbackMethod);

    [DllImport("__Internal")]
    private static extern void StartMicrophoneDeviceMonitoring(string gameObjectName, string callbackMethod);

    [DllImport("__Internal")]
    private static extern void StopMicrophoneDeviceMonitoring();
#endif

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Pass the name of this GameObject and the callback method name
        StartMicrophoneDeviceMonitoring(gameObject.name, nameof(OnMicrophoneDeviceChanged));
#endif
    }

    private void OnDestroy()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        StopMicrophoneDeviceMonitoring();
#endif
    }
    // Called automatically by SendMessage from WebGL JavaScript
    public void OnMicrophoneDeviceChanged(string result)
    {
        if (result == "MIC_CONNECTED")
        {
            Debug.Log("[Vivox] Microphone plugged in.");
            if(_chatManagerSO.Instance.micAccess == "GRANTED")
            {
                //Do nothing since we already granted to speak
            }
            else
            {
                //Lets ask permissions if we are not granted so we can speak
                AskMicPermission();
            }
        }
        else if (result == "MIC_DISCONNECTED")
        {
            Debug.LogWarning("[Vivox] Microphone unplugged.");

            //force mute since the mic has been unplugged
            if(_gameManagerSO.Instance != null)
            {
                _gameManagerSO.Instance.MuteMic();
            }

            _rememberMeSO.Instance.EnableWarning("Mic has been disconnected");
            // e.g., Handle graceful fallback or mute
        }

        _chatManagerSO.Instance.micStatus = result;
    }

    //Calls by EventListener when OnLobbyLoaded
    public void AskMicPermission()
    {

#if UNITY_WEBGL && !UNITY_EDITOR
        // Call browser directly on user click
        RequestBrowserMicPermission(gameObject.name, "OnBrowserPermissionResult");
#else
        // Editor testing mock
        OnBrowserPermissionResult("GRANTED");
        _chatManagerSO.Instance.micStatus = "MIC_CONNECTED";
#endif
    }

    // Listens directly for what the browser returned
    public void OnBrowserPermissionResult(string status)
    {
        if (status == "GRANTED")
        {
            _chatManagerSO.Instance.micStatus = "MIC_CONNECTED"; //safety since at start the mic might been already been plugged
            _rememberMeSO.Instance.EnableWarning("Mic Access is Approved");
        }
        else if(status == "UNSUPPORTED") 
        {
            _rememberMeSO.Instance.EnableWarning("Mic Access is not supported");
        }
        else // "DENIED"
        {
            _rememberMeSO.Instance.EnableWarning("Mic Access is Denied");
            Debug.LogWarning("Vivox Browser mic access denied. Preventing Vivox crash.");
        }

        _chatManagerSO.Instance.micAccess = status;

        //Check if this happened mid game so we can reconnect the player to the channel along with new permissions
        if(status != "DENIED")
        {

        }

        //NOTE: Saved access status and then the LobbyManager will start the Vivox connections
    }
}