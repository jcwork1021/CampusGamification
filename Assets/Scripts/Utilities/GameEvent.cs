using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/GameEvent")]
public class GameEvent : ScriptableObject
{

#if UNITY_EDITOR
    [Multiline] public string Description = "";
#endif
    [Space(25), Header("Sent Information")]
    public string SentString;
    public int SentInt;
    public float SentFloat;
    public bool SentBool;
    public Action SentAction;

    // Optional object payload (e.g. a ScriptableObject the producer wants the consumer to use).
    // Generic: a feature sets it just before Raise() and reads it synchronously in the handler.
    public UnityEngine.Object SentObject;

    public event Action OnRaised;

    private List<EventListener> _eventListeners = new List<EventListener>();

    public void Raise()
    {
        for (int i = _eventListeners.Count - 1; i >= 0; i--)
        {
            _eventListeners[i].OnEventRaised(this);
        }

        OnRaised?.Invoke();
    }

    public void Register(EventListener passedEvent)
    {
        if (!_eventListeners.Contains(passedEvent))
        {
            _eventListeners.Add(passedEvent);
        }
    }

    public void UnRegister(EventListener passedEvent)
    {

        if (_eventListeners.Contains(passedEvent))
        {
            _eventListeners.Remove(passedEvent);
        }

    }
    public void Subscribe(Action listener)
    {
        OnRaised += listener;
    }

    public void Unsubscribe(Action listener)
    {
        OnRaised -= listener;
    }

    private void ClearAllListeners()
    {
        _eventListeners.Clear();
    }

    private void OnEnable()
    {
        ClearAllListeners();
        OnRaised = null;
    }
}
