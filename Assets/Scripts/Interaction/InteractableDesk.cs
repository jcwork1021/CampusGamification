using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class InteractableDesk : BaseInteractable
{
    [Header("Necessary SO")]
    [SerializeField] InteractableDeskRefSO _interactableDeskSO;
    [SerializeField] ChatManagerRefSO _chatManagerSO;

    [Header("Reference from Itself")]
    [SerializeField] Canvas _deskCanvas;

    [Header("Reference from Itself (Optional)")]
    [SerializeField] Transform _sitPosition;
    [SerializeField] Transform _standPosition;

    [Header("Reference from Desk_Canvas")]
    [SerializeField] CanvasGroup _maximizeBtn;
    [SerializeField] CanvasGroup _optionsScrollview;
    [SerializeField] CanvasGroup _kickListScrollview;
    [SerializeField] Transform _kickListContent;

    [Header("Reference from Options_Scrollview")]
    [SerializeField] TMP_Text _fppStatusTxt;
    [SerializeField] TMP_Text _muteStatusTxt;
    [SerializeField] TMP_Text _kickMenuTxt;

    [Header("Toggles")]
    [SerializeField] bool _standVersion; // if false then it is sit animation, so we play sit animation instead.

    private Vector2 moveInput;

    [Header("Bool Read-Only")]
    [SerializeField] bool _kickMenuEnable;

    [Networked, OnChangedRender(nameof(OnFPPChanged))]
    public NetworkBool networkedFPP { get; set; }

    [Networked, OnChangedRender(nameof(OnForcedEveryoneToMute))]
    public bool networkedForcedEveryoneToMute { get; set; }

    #region Unity Functions

    private void OnEnable()
    {
        ToggleDeskCanvas(false);
        ToggleMaximizeBtn(true);
        ToggleOptionsScrollview(false);
        ToggleKickListScrollview(false);

        //Checker to make sure that the default is the click menu is off
        if (_kickMenuEnable)
        {
            ClickKickMenu();
        }

        _gameManagerSO.Instance._onRemovePlayerOnTheList.AddListener(() => DuplicatePlayerButtons(_kickListContent));
        _gameManagerSO.Instance._onAddPlayerOnTheList.AddListener(() => DuplicatePlayerButtons(_kickListContent));
    }

    private void OnDisable()
    {
        if(_gameManagerSO.Instance != null)
        {
            _gameManagerSO.Instance._onRemovePlayerOnTheList.RemoveListener(() => DuplicatePlayerButtons(_kickListContent));
            _gameManagerSO.Instance._onAddPlayerOnTheList.RemoveListener(() => DuplicatePlayerButtons(_kickListContent));
        }
    }
    #endregion

    #region Fusion Functions

    public override void Spawned()
    {
        OnFPPChanged();
        OnForcedEveryoneToMute();
    }
    #endregion

    #region GameEvent Overrides

    //This is called when Interactable is collided with player
    public override void CallWhenInteracted()
    {
        base.CallWhenInteracted();
        _interactableDeskSO.Instance = this;
    }
    protected override void OnLevelRequirementPassed()
    {
        base.OnLevelRequirementPassed();

        if (_standVersion)
        {
            _playerManagerSO.Instance.RepositionPlayer(_standPosition); //Reposition Player to the stand position
            _playerManagerSO.Instance.animator.SetBool("TalkSpeak1", true);
        }
        else
        {
            if(_sitPosition != null)
            {
                _playerManagerSO.Instance.RepositionPlayer(_sitPosition); //Reposition Player to the chair
                _playerManagerSO.Instance.animator.SetBool("Sit", true);
            }
            else
            {
                Debug.LogError("No Sit Position Assigned");
            }
        }

        //Enable Desk Canvas
        ToggleDeskCanvas(true);

        //Disable Player controls
        _gameManagerSO.Instance.TogglePlayerCanvas(false);
    }

    protected override void OnLevelRequirementFailed()
    {
        base.OnLevelRequirementFailed();

        Debug.Log("Displaying 'Level Too Low' notification UI to player.");
    }

    public override void ExitInteractable()
    {
        base.ExitInteractable();

        if (_standVersion)
        {
            _playerManagerSO.Instance.animator.SetBool("TalkSpeak1", false);
        }
        else
        {
            _playerManagerSO.Instance.animator.SetBool("Sit", false);
        }

        _playerManagerSO.Instance.RepositionPlayer(_standPosition);

        if (networkedFPP)
        {
            ClickOptionFPP(); //so we can turn off the FPP
        }

        if (_kickMenuEnable)
        {
            ClickKickMenu();
        }

        //Disable Desk Canvas
        ToggleDeskCanvas(false);

        //Enable Player controls
        _gameManagerSO.Instance.TogglePlayerCanvas(true);

    }

    #endregion

    #region Network Sync Functions
    private void OnFPPChanged()
    {
        if (networkedFPP)
        {
            _fppStatusTxt.text = "FPP: ON";
        }
        else
        {
            _fppStatusTxt.text = "FPP: OFF";
        }
    }

    public void OnForcedEveryoneToMute()
    {
        if (networkedForcedEveryoneToMute)
        {
            _muteStatusTxt.text = "MUTE EVERYONE: ON";
        }
        else
        {
            _muteStatusTxt.text = "MUTE EVERYONE: OFF";
        }

        if (!Object.HasStateAuthority)
        {
            _chatManagerSO.Instance.ForcedToggleMute(networkedForcedEveryoneToMute);
        }
    }

    #endregion

    #region Additional Options
    public void ClickOptionFPP()
    {
        networkedFPP = !networkedFPP;

        if (networkedFPP)
        {
            FocusInteractableCamera(10);
        }
        else
        {
            FocusInteractableCamera(0);
        }
    }

    public void ClickOptionMuteEveryone()
    {
        networkedForcedEveryoneToMute = !networkedForcedEveryoneToMute;
    }

    public void ClickKickMenu()
    {
        _kickMenuEnable = !_kickMenuEnable;

        if (_kickMenuEnable)
        {
            DuplicatePlayerButtons(_kickListContent);
            ToggleKickListScrollview(true);

            _kickMenuTxt.text = "KICK MENU: ON";
        }
        else
        {
            ToggleKickListScrollview(false);
            _kickMenuTxt.text = "KICK MENU: OFF";
        }
    }

    #endregion

    #region Functions

    public Dictionary<PlayerRef, GameObject> DuplicatePlayerButtons(Transform parentTransform = null)
    {
        // Clear existing instantiated UI buttons under the parent container
        if (parentTransform != null)
        {
            foreach (Transform child in parentTransform)
            {
                Destroy(child.gameObject);
            }
        }

        Dictionary<PlayerRef, GameObject> clonedButtons = new Dictionary<PlayerRef, GameObject>();

        foreach (KeyValuePair<PlayerRef, GameObject> pair in _gameManagerSO.Instance._playerButtons)
        {
            // 1. Skip if it's the local player
            if (pair.Key == _gameManagerSO.Instance.LocalPlayerRef)
            {
                continue;
            }

            if (pair.Value != null)
            {
                GameObject originalButtonObj = pair.Value;
                GameObject newButton = Instantiate(originalButtonObj, parentTransform);

                // Fetch the Button components on both objects
                Button originalButton = originalButtonObj.GetComponent<Button>();
                Button newButtonComponent = newButton.GetComponent<Button>();

                if (newButtonComponent != null)
                {
                    newButtonComponent.interactable = true;

                    if (originalButton != null)
                    {
                        // Clear duplicated inspector events to avoid double-firing
                        newButtonComponent.onClick.RemoveAllListeners();

                        // Forward the click event to the original button
                        newButtonComponent.onClick.AddListener(() =>
                        {
                            originalButton.onClick.Invoke();
                        });
                    }
                }

                clonedButtons.Add(pair.Key, newButton);
            }
        }

        return clonedButtons;
    }
    #endregion

    #region UI Clicks

    public void ClickExitDesk()
    {
        ExitInteractable();
    }

    public void ClickMaximize()
    {
        ToggleOptionsScrollview(true);
        ToggleMaximizeBtn(false);
    }

    public void ClickMinimize()
    {
        ToggleOptionsScrollview(false);
        ToggleMaximizeBtn(true);

        ToggleKickListScrollview(false);
    }
    #endregion

    #region Toggle UI

    private void ToggleDeskCanvas(bool visible)
    {
        _deskCanvas.enabled = visible;
    }

    private void ToggleOptionsScrollview(bool visible)
    {
        ToggleCanvasGroup(_optionsScrollview, visible);
    }

    private void ToggleMaximizeBtn(bool visible)
    {
        ToggleCanvasGroup(_maximizeBtn, visible);
    }

    private void ToggleKickListScrollview(bool visible)
    {
        ToggleCanvasGroup(_kickListScrollview, visible);
    }

    private void ToggleCanvasGroup(CanvasGroup cg, bool visible)
    {
        // 0 = no display, 1 = fully visible
        cg.alpha = visible ? 1f : 0f;
        // Prevents interacting with buttons on this panel when false
        cg.interactable = visible;
        // Stops this panel from blocking clicks to elements/gameplay behind it when false
        cg.blocksRaycasts = visible;
    }

    #endregion
}
