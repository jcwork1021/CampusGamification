using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

    public class EventListener : MonoBehaviour
    {
        #region Fields

#if UNITY_EDITOR
        [Multiline] public string Description;
#endif
        public List<EventAndResponse> EventAndResponses = new List<EventAndResponse>();

        #endregion

        #region Unity Methodds

#if UNITY_EDITOR
        // Editor-only: runs whenever this component is inspected/recompiled. Copies each
        // response's legacy single TargetEvent into the new, visible TargetEvents list, so the
        // migration actually shows up in the inspector instead of only being read silently at
        // runtime. Safe to run repeatedly — Contains() check prevents duplicate entries.
        private void OnValidate()
        {
            foreach (EventAndResponse response in EventAndResponses)
            {
                response.MigrateLegacyTargetEvent();
            }
        }

        [ContextMenu("Migrate Legacy Target Events")]
        private void MigrateAllLegacyTargetEvents()
        {
            foreach (EventAndResponse response in EventAndResponses)
            {
                response.MigrateLegacyTargetEvent();
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[{nameof(EventListener)}] Migrated legacy TargetEvent references into TargetEvents on '{name}'.");
        }
#endif

        #endregion

        private void OnEnable()
        {
            if (EventAndResponses.Count >= 1)
            {
                foreach (EventAndResponse eAndR in EventAndResponses)
                {
                    foreach (GameEvent evt in eAndR.AllTargetEvents())
                    {
                        evt?.Register(this);
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (EventAndResponses.Count >= 1)
            {
                foreach (EventAndResponse eAndR in EventAndResponses)
                {
                    foreach (GameEvent evt in eAndR.AllTargetEvents())
                    {
                        evt?.UnRegister(this);
                    }
                }
            }
        }


        #region Custom Methods

        public void OnEventRaised(GameEvent passedEvent)
        {
            for (int i = EventAndResponses.Count - 1; i >= 0; i--)
            {
                EventAndResponse response = EventAndResponses[i];

                if (!response.Targets(passedEvent)) continue;

                response.EventRaised(passedEvent);

                foreach (EventItemAction eiAction in response.responseForSentAction)
                {
                    if (eiAction._responseTime != 0)
                    {
                        StartCoroutine(eiAction.TriggerAfterSetAmountOfTime(passedEvent));
                    }
                }
            }
        }

        #endregion
    }

    #region Responses Class

    [System.Serializable]
    public class EventAndResponse
    {
        [Obsolete("Use TargetEvents instead. Kept (hidden) so already-wired scenes/prefabs from " +
                  "before this migration keep working without re-wiring — still read at runtime " +
                  "via AllTargetEvents(), just no longer shown in the inspector.")]
        [HideInInspector]
        public GameEvent TargetEvent;

        [Tooltip("Add as many GameEvents as you want — the SAME response below fires for any of them.")]
        public List<GameEvent> TargetEvents = new List<GameEvent>();

        [Space(10)]
        public List<EventItemAction> responseForSentAction;
        public List<EventItemString> responseForSentString;
        public List<EventItemInt> responseForSentInt;
        public List<EventItemFloat> responseForSentFloat;
        public List<EventItemBool> responseForSentBool;

        /// <summary>
        /// Copies the legacy TargetEvent into TargetEvents if it's set and not already present.
        /// Called automatically (OnValidate) and manually (context menu) from EventListener.
        /// Idempotent — safe to call repeatedly without creating duplicates.
        /// </summary>
        public void MigrateLegacyTargetEvent()
        {
#pragma warning disable CS0618 // intentional read of the obsolete field for migration
            if (TargetEvent != null && !TargetEvents.Contains(TargetEvent))
            {
                TargetEvents.Add(TargetEvent);
            }
#pragma warning restore CS0618
        }

        /// <summary>
        /// Every GameEvent this response should register to: the new TargetEvents list, plus
        /// the legacy single TargetEvent field if it's still set (pre-migration data) and not
        /// already duplicated in the list.
        /// </summary>
        public IEnumerable<GameEvent> AllTargetEvents()
        {
            foreach (GameEvent evt in TargetEvents)
            {
                if (evt != null) yield return evt;
            }

#pragma warning disable CS0618 // intentional read of the obsolete field for migration
            if (TargetEvent != null && !TargetEvents.Contains(TargetEvent))
                yield return TargetEvent;
#pragma warning restore CS0618
        }

        /// <summary>True if the given GameEvent is one of this response's targets (new list or legacy field).</summary>
        public bool Targets(GameEvent evt)
        {
            foreach (GameEvent target in AllTargetEvents())
            {
                if (target == evt) return true;
            }
            return false;
        }

        /// <summary>
        /// Fires this response's UnityEvents, reading Sent* values off whichever GameEvent
        /// actually raised (raisedEvent) rather than a single fixed target — this is what lets
        /// one response serve multiple events.
        /// </summary>
        public void EventRaised(GameEvent raisedEvent)
        {
            if (responseForSentAction.Count > 0)
            {
                foreach (EventItemAction eventItemAction in responseForSentAction)
                {
                    if (eventItemAction._response.GetPersistentEventCount() >= 1)
                    {
                        if (eventItemAction._responseTime == 0)
                            eventItemAction._response.Invoke(raisedEvent.SentAction);
                    }
                }
            }

            if (responseForSentBool.Count > 0)
            {
                foreach (EventItemBool eventItemBool in responseForSentBool)
                {
                    if (eventItemBool.response.GetPersistentEventCount() >= 1)
                    {
                        eventItemBool.response.Invoke(raisedEvent.SentBool);
                    }
                }
            }

            if (responseForSentFloat.Count > 0)
            {
                foreach (EventItemFloat eventItemFloat in responseForSentFloat)
                {
                    if (eventItemFloat.response.GetPersistentEventCount() >= 1)
                    {
                        eventItemFloat.response.Invoke(raisedEvent.SentFloat);
                    }
                }
            }

            if (responseForSentInt.Count > 0)
            {
                foreach (EventItemInt eventItemInt in responseForSentInt)
                {
                    if (eventItemInt.response.GetPersistentEventCount() >= 1)
                    {
                        eventItemInt.response.Invoke(raisedEvent.SentInt);
                    }
                }
            }

            if (responseForSentString.Count > 0)
            {
                foreach (EventItemString eventItemString in responseForSentString)
                {
                    if (eventItemString.response.GetPersistentEventCount() >= 1)
                    {
                        eventItemString.response.Invoke(raisedEvent.SentString);
                    }
                }
            }
        }
    }

    [System.Serializable]
    public class ResponseWithString : ResponseWith<string> { }

    [System.Serializable]
    public class ResponseWithInt : ResponseWith<int> { }

    [System.Serializable]
    public class ResponseWithFloat : ResponseWith<float> { }

    [System.Serializable]
    public class ResponseWithBool : ResponseWith<bool> { }

    [System.Serializable]
    public class ResponseWithAction : ResponseWith<Action> { }

    [System.Serializable]
    public abstract class ResponseWith<T> : UnityEvent<T> { }
    #endregion

    #region EventItemsClass

    [System.Serializable]
    public abstract class EventBase
    {
        [SerializeField] private string _sectionTitle;
        [SerializeField, Multiline] private string _description;

        public string SectionTitle { get => _sectionTitle; set => _sectionTitle = value; }
    }

    [System.Serializable]
    public class EventItemAction : EventBase
    {
        public float _responseTime = 0;
        public ResponseWithAction _response;

        public IEnumerator TriggerAfterSetAmountOfTime(GameEvent gEvent)
        {
            yield return new WaitForSeconds(_responseTime);
            _response.Invoke(gEvent.SentAction);
        }
    }

    [System.Serializable]
    public class EventItemBool : EventBase
    {
        public ResponseWithBool response;
    }

    [System.Serializable]
    public class EventItemFloat : EventBase
    {
        public ResponseWithFloat response;
    }

    [System.Serializable]
    public class EventItemInt : EventBase
    {
        public ResponseWithInt response;
    }

    [System.Serializable]
    public class EventItemString : EventBase
    {
        public ResponseWithString response;
    }

    #endregion


