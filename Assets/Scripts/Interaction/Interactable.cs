using Fusion;
using System.Collections;
using System.Collections.Generic;
using Unity.IL2CPP.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static UnityEngine.UIElements.UxmlAttributeDescription;


public class Interactable : NetworkBehaviour, IStateAuthorityChanged
{
    [Header("Necessary SO")]
    [SerializeField] InteractableRefSO _interactableSO;
    [SerializeField] PlayerManagerRefSO _playerManagerSO;

    [Header("UI Settings")]
    [SerializeField] GameObject _interactableCanvas;
    [SerializeField] Transform _buttonContainer;
    [SerializeField] GameObject _interactableOptionPanel;
    [SerializeField] GameObject _toggleInteractionBtn;

    [Header("From Project")]
    [SerializeField] InteractionOptionButton _interactableBtn;

    [Header("Options Configuration")]
    [SerializeField] List<InteractionOption> _options = new();

    [Header("Adjustable Bool")]
    [SerializeField] bool _shouldLock;
    public bool _doubleInteractable;

    [Header("Runtime Status")]
    [SerializeField] bool _startInactive = false;
    [SerializeField] bool _isInteractableActive;
    [SerializeField] bool _interactedFirst;
    public bool _interactedSecond; //Calls by InputActionManager.cs to check if we can trigger tab key

    public UnityEvent doThisWhenInteracted;

    readonly List<InteractionOptionButton> _spawnedButtons = new();

    InteractionOption pendingOption;

    PlayerRef networkedPlayer;

    // Synced across all players in Shared Mode
    [Header("Read-Only")]
    [Networked, OnChangedRender(nameof(OnChangeStatus))]
    public NetworkBool isBusy { get; set; }

    public bool IsInteractableActive => _isInteractableActive;

    #region Unity Function
    void Awake()
    {
        _isInteractableActive = !_startInactive;
    }

    void Start()
    {
        _toggleInteractionBtn.SetActive(false);
    }

    #endregion

    #region UI & Interaction Logic

    public void ActivatePrompt()
    {
        // Don't show menu if someone else is currently using this object
        if (isBusy) return;

        if (_doubleInteractable) //Since this is double Interactable, lets only show the first one which is sits most likely
        {
            SpawnFirstOption();
        }
        else
        {
            SpawnAllAction();
        }

        //Since we are showing the prompt lets mark this as our Active Interactable
        doThisWhenInteracted?.Invoke();
    }

    //Calls in ActivePrompt() when the Interactable is not double Interactable
    public void SpawnAllAction()
    {
        EnableInteractableCanvas();
        _interactableOptionPanel.SetActive(true);
        _toggleInteractionBtn.SetActive(false); //Default is disable. only active when it is double interactable

        foreach (var option in _options)
        {
            if (option == null) continue;

            InteractionOptionButton btn = Instantiate(_interactableBtn, _buttonContainer);
            btn.Setup(option, ExecuteOption);
            _spawnedButtons.Add(btn);
        }
    }

    //Calls in ActivePrompt() when the Interactable is just single Interactable
    public void SpawnFirstOption()
    {
        if (_options == null || _options.Count == 0)
        {
            Debug.LogWarning("No Options");
            return;
        }

        EnableInteractableCanvas();
        _interactableOptionPanel.SetActive(true);
        _toggleInteractionBtn.SetActive(false); //Default is disable. only active when it is double interactable

        var firstOption = _options[0];
        if (firstOption == null) return;

        InteractionOptionButton btn = Instantiate(_interactableBtn, _buttonContainer);
        btn.Setup(firstOption, ExecuteOption);
        _spawnedButtons.Add(btn);

    }

    //Callls in ExecuteOptions() after checking if this interactable has more than 1 interaction and ToggleSecondInteractable()
    public void SpawnRemainingOptions()
    {
        if (_options == null || _options.Count <= 1)
        {
            Debug.LogWarning("No Other Options Except 1");
            return;
        }

        EnableInteractableCanvas();
        _interactableOptionPanel.SetActive(true);
        _toggleInteractionBtn.SetActive(true); //Enable because this is the part we can minimize and maximize

        for (int i = 1; i < _options.Count; i++)
        {
            var option = _options[i];
            if (option == null) continue;

            InteractionOptionButton btn = Instantiate(_interactableBtn, _buttonContainer);
            btn.Setup(option, ExecuteOption);
            _spawnedButtons.Add(btn);
        }
    }

    //Calls in InteractableManager.cs for Tab key action listener and onclick of ToggleInteractionBtn added manually
    public void ToggleInteractableOptionsPanel()
    {
        if (_interactableOptionPanel.activeSelf)
        {
            _interactableOptionPanel.SetActive(false);
        }
        else
        {
            _interactableOptionPanel.SetActive(true);
        }     
    }
    #endregion

    #region Interaction Logic

    public void HandleHotkeys()
    {
        if (_doubleInteractable)
        {
            if (_doubleInteractable && _interactedFirst && _interactedSecond) return;
        }
        else
        {
            if (_shouldLock && isBusy) return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        foreach (var btn in _spawnedButtons)
        {
            var option = btn.OptionData;
            if (option != null && option.Enabled && option.Hotkey != Key.None)
            {
                if (keyboard[option.Hotkey].wasPressedThisFrame)
                {
                    ExecuteOption(option);
                    break;
                }
            }
        }
    }

    public void ExecuteOption(InteractionOption option)
    {
        if (option == null || !option.Enabled) return;

        if (_doubleInteractable)
        {
            if (_doubleInteractable && _interactedFirst && _interactedSecond) return;
        }
        else
        {
            if (_shouldLock && isBusy) return;
        }

        // Save the chosen option to run once authority is guaranteed
        pendingOption = option;

        DeactivatePrompt();

        if (!HasStateAuthority)
        {
            Debug.Log("[Interactable] Requesting State Authority...");
            Object.RequestStateAuthority(); // Triggers StateAuthorityChanged() when granted
        }
        else
        {
            Debug.Log("[Interactable] Already have authority!");
            // Manually invoke callback logic if we already own authority
            StateAuthorityChanged();
        }

        if (_doubleInteractable)
        {
            if (_interactedFirst == false)
            {
                ToggleFirstInteractable(true);
                SpawnRemainingOptions();
                return;
            }

            if (_interactedFirst && _interactedSecond == false)
            {
                ToggleSecondInteractable(true); //Just to mark
                DeactivatePrompt();
                return;
            }
        }
    }

    //Call when the interaction finishes, so it wont be busy anymmore
    public void ReleaseInteraction()
    {
        if (HasStateAuthority)
        {
            isBusy = false;
        }
    }

    public void SetInteractableActive(bool active)
    {
        _isInteractableActive = active;

        if (!active)
        {
            DeactivatePrompt();
        }

        Debug.LogError("Interactable Active");
    }

    //Calls by InteractableScripts to mark that this script hold the first Interactable and not
    public void ToggleFirstInteractable(bool use)
    {
        _interactedFirst = use;

        if (use)
        {
            _interactableSO.Instance = this;
        }
        else
        {
            _interactableSO.Instance = null;
            SpawnFirstOption(); //Since we exited the Primary Interaction lets show the Primary Interaction Options
        }
    }

    //Call in BaseInteractable.cs (Abstract)
    public void ToggleSecondInteractable(bool use)
    {
        _interactedSecond = use; //Mark if true or false

        if (!use) //Means player just exited the 2nd Interactable so lets show the prompt again
        {
            SpawnRemainingOptions();
        }
    }

    public void CancelSecondInteractable()
    {
        StartCoroutine(DelayInCanceling());
    }

    //Need Delay to give way to ExecuteOptions() so it can be finished
    IEnumerator DelayInCanceling()
    {
        yield return new WaitForSeconds(1);
        Debug.LogError("Cancel Interaction " + this.gameObject.name);

        _interactedSecond = false;
        _interactableCanvas.SetActive(true);
    }

    #endregion

    #region Reusable Functions to help functions above
    public void EnableInteractableCanvas() //Calls in functions that Enable InteractableOptions
    {
        ClearButtons();

        if (_playerManagerSO != null && _playerManagerSO.Instance != null)
        {
            _playerManagerSO.Instance.EnablePlayerBubbleCanvas(false); //Disable the player bubble canvas or the Name Tag
        }

        if (_interactableCanvas != null)
        {
            _interactableCanvas.SetActive(true);
        }

    }

    //public because it is being called in PlayerInteractor.cs as well
    public void DeactivatePrompt()
    {
        if (_doubleInteractable && _interactedFirst && !_interactedSecond) return;

        if(_playerManagerSO != null && _playerManagerSO.Instance != null)
        {
            _playerManagerSO.Instance.EnablePlayerBubbleCanvas(true); //Enable the player bubble canvas or the Name Tag
        }

        if (_interactableCanvas != null)
        {
            _interactableCanvas.SetActive(false);
        }
    }

    void ClearButtons()
    {
        foreach (var btn in _spawnedButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        _spawnedButtons.Clear();
    }
    #endregion

    #region Network Function
    void OnChangeStatus()
    {
        if (isBusy)
        {
            // Close prompts for everyone else nearby who had the UI open
            DeactivatePrompt();
        }
        else
        {   
            //Turning off by  ReleaseInteraction() which is called by gameManagerScript upon exiting Interaction
            //Debug.Log($"{this.name} can be used again");
        }
    }

    // Called automatically by Fusion once authority transfer is confirmed over the network
    public void StateAuthorityChanged()
    {
        // Confirm we actually received authority and have a pending option
        if (HasStateAuthority && pendingOption != null)
        {
            // 1. Lock the object for all players across the network
            if (_shouldLock)
            {
                isBusy = true;
            }

            // 2. Execute the actions now that we own state authority
            if (pendingOption.Actions != null)
            {
                foreach (var action in pendingOption.Actions)
                {
                    if (action == null) continue;
                    action.InteractorReact(this);
                }
            }

            // 3. Clear the pending option
            pendingOption = null;
            return;
        }

        /*
        // CASE 2: Player Disconnected / Left Mid-Interaction!
        // If authority was transferred to us (Master Client) while the object was locked, unlock it!
        if (HasStateAuthority && isBusy && pendingOption == null)
        {
            Debug.LogWarning($"[Interactable] Previous owner left! Unlocking {gameObject.name} for other players.");
            isBusy = false;
        }
        */

        // CASE 2: Player Disconnected / Left Mid-Interaction!
        // If authority was transferred to us (Master Client) while the object was locked, unlock it!
        if (isBusy)
        {
            Debug.LogWarning($"[Interactable] Previous owner left! Unlocking {gameObject.name} for other players.");
            isBusy = false;
        }
    }

    #endregion
}