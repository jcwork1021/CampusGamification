using System.Collections.Generic;
using UnityEngine;

public class ReceiverObject : MonoBehaviour
{
    [Header("Necessary SO")]
    [SerializeField] ReceiverManagerRefSO _receiverManagerSO;

    [Header("Data Fill Manually")]
    [SerializeField] private string receiverName;
    [SerializeField] private List<Transform> receiverPositions = new List<Transform>();

    [Header("Public Getter")]
    public string ReceiverName => receiverName;
    public List<Transform> ReceiverPositions => receiverPositions;

    private void OnEnable()
    {
        // Auto-register to the Manager when activated
        if (_receiverManagerSO.Instance != null)
        {
            _receiverManagerSO.Instance.RegisterReceiver(this);
        }
    }

    private void OnDisable()
    {
        // Unregister when destroyed or disabled
        if (_receiverManagerSO.Instance != null)
        {
            _receiverManagerSO.Instance.UnregisterReceiver(this);
        }
    }
}