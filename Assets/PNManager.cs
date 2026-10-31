using Newtonsoft.Json;
using PubnubApi;
using PubnubApi.Unity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PNManager : PNManagerBehaviour
{

    [Header("Read-Only")]
    public RememberMeRefSO rememberMeSO;
    public ChatManagerRefSO chatManagerSO;

    public event Action<string, string> OnGlobalMessageReceived;

    public bool pubNubConnected = false;

    private void Awake()
    {

        // Listener example.
        listener.onStatus += OnPnStatus;
        listener.onMessage += OnPnMessage;
        listener.onPresence += OnPnPresence;
        listener.onFile += OnPnFile;
        listener.onObject += OnPnObject;
        listener.onSignal += OnPnSignal;
        listener.onMessageAction += OnPnMessageAction;

    }

    //Calls in EventListener when OnLoadedLobby has been raised to make sure login is finished and we hold player name now
    public void InitializePubNub()
    {
        Debug.Log("Pubnub Initialized Attempt");

        // Initialize will create a PubNub instance, pass the configuration object, and prepare the listener. 
        Initialize(rememberMeSO.Instance.playerName);
        chatManagerSO.Instance.JoinGlobalChannel();
    }

    //Subscribe to a channel called in ChatManager.cs
    public void SubscribeToGlobalChannel(string channelName)
    {
        pubnub.Subscribe<string>()
            .Channels(new[] { channelName })
            .Execute();

        Debug.Log($"[PubNub] Subscribed to global channel: {channelName}");

        pubNubConnected = true;
    }



    private void OnPnMessageAction(Pubnub pn, PNMessageActionEventResult result)
    {
        Debug.Log(result.Channel);
    }

    private void OnPnMessage(Pubnub pn, PNMessageResult<object> result)
    {
        Debug.Log($"[PubNub] Raw message: {result.Message}");

        string json = result.Message?.ToString();

        if (string.IsNullOrEmpty(json))
            return;

        GlobalChatMessage chatMessage =
            JsonConvert.DeserializeObject<GlobalChatMessage>(json);

        if (chatMessage == null)
            return;

        Debug.Log($"[PubNub] Sender: {chatMessage.sender}");
        Debug.Log($"[PubNub] Message: {chatMessage.message}");

        if (result.Channel == "GlobalChannel")
        {
            OnGlobalMessageReceived?.Invoke(
                chatMessage.sender,
                chatMessage.message
            );
        }
    }

    //Calls by ChatManager.cs in SendMessage() 
    public async Task PublishGlobalMessage(string user, string messageBody)
    {
        try
        {
            Debug.Log($"[PubNub] pubnub null? {pubnub == null}");
            Debug.Log($"[PubNub] chatManagerSO.Instance null? {chatManagerSO.Instance == null}");
            Debug.Log($"[PubNub] globalChannelName: '{chatManagerSO.Instance?.globalChannelName}'");

            var result = await pubnub.Publish()
                .Channel(chatManagerSO.Instance.globalChannelName)
                .Message(new { sender = user, message = messageBody })
                .ExecuteAsync();

            Debug.Log($"[PubNub] Publish status: {result?.Status?.StatusCode}, Error: {result?.Status?.Error}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PubNub] Publish threw exception: {e}");
        }
    }


    private void OnPnSignal(Pubnub pn, PNSignalResult<object> result)
    {
        Debug.Log(result.Channel);
    }

    private void OnPnObject(Pubnub pn, PNObjectEventResult result)
    {
        Debug.Log(result.Channel);
    }

    private void OnPnFile(Pubnub pn, PNFileEventResult result)
    {
        Debug.Log(result.Channel);
    }

    private void OnPnPresence(Pubnub pn, PNPresenceEventResult result)
    {
        Debug.Log(result.Event);
    }

    private void OnPnStatus(Pubnub pn, PNStatus status)
    {
        Debug.Log(status.Category == PNStatusCategory.PNConnectedCategory ? "Pubnub Connected" : "Not connected");
    }

    protected override void OnDestroy()
    {
        // Use OnDestroy to clean up, e.g. unsubscribe from listeners.
        listener.onStatus -= OnPnStatus;
        listener.onMessage -= OnPnMessage;
        listener.onPresence -= OnPnPresence;
        listener.onFile -= OnPnFile;
        listener.onObject -= OnPnObject;
        listener.onSignal -= OnPnSignal;
        listener.onMessageAction -= OnPnMessageAction;

        base.OnDestroy();
    }

    #region
    [Serializable]
    public class GlobalChatMessage
    {
        public string sender;
        public string message;
    }
    #endregion
}