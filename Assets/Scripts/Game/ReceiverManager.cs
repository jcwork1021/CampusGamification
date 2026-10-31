using System.Collections.Generic;
using UnityEngine;

public class ReceiverManager : MonoBehaviour
{
    [Header("Necessary SO")]
    [SerializeField] ReceiverManagerRefSO _receiverManagerSO;

    public List<ReceiverObject> receivers = new List<ReceiverObject>();

    private void Awake()
    {
        _receiverManagerSO.Instance = this;
    }

    private void OnDestroy()
    {
        _receiverManagerSO.Instance = null;
    }


    public void RegisterReceiver(ReceiverObject receiver)
    {
        if (!receivers.Contains(receiver))
            receivers.Add(receiver);
    }

    public void UnregisterReceiver(ReceiverObject receiver)
    {
        if (receivers.Contains(receiver))
            receivers.Remove(receiver);
    }
}