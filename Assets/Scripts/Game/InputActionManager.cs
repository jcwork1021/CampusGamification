using UnityEngine;
using UnityEngine.InputSystem;

public class InputActionManager : MonoBehaviour
{
    [Header("Necessary SO")]
    [SerializeField] GameManagerRefSO _gameManagerSO;
    [SerializeField] InteractableRefSO _interactableSO;
    [SerializeField] ChatManagerRefSO _chatManagerSO;
    [SerializeField] PlayerControllerRefSO _playerControllerSO;

    [Header("Input Action Reference")]
    [SerializeField] InputActionReference exitAction;
    [SerializeField] InputActionReference tabAction;
    [SerializeField] InputActionReference enterAction;
    [SerializeField] InputActionReference muteAction;

    #region Unity Functions
    private void OnEnable()
    {
        if (exitAction != null)
        {
            exitAction.action.Enable();
            exitAction.action.performed += OnExitPressed;
        }

        if (tabAction != null)
        {
            // Subscribe to "TAB" event
            tabAction.action.Enable();
            tabAction.action.performed += OnTabPressed;
        }

        if (enterAction != null)
        {
            // Subscribe to "ENTER" event
            enterAction.action.Enable();
            enterAction.action.performed += OnEnterPressed;
        }

        if (muteAction != null)
        {
            // Subscribe to "M" Event
            muteAction.action.Enable();
            muteAction.action.performed += OnMPressed;
        }
    }

    private void OnDisable()
    {
        if (tabAction != null)
        {
            tabAction.action.performed -= OnTabPressed;
            tabAction.action.Disable();
        }

        if (enterAction != null)
        {
            enterAction.action.performed -= OnEnterPressed;
            enterAction.action.Disable();
        }

        if (muteAction != null)
        {
            muteAction.action.performed -= OnMPressed;
            muteAction.action.Disable();
        }


        if (exitAction != null)
        {
            exitAction.action.performed -= OnExitPressed;
            exitAction.action.Disable();
        }
    }
    #endregion

    #region Input Actions
    void OnExitPressed(InputAction.CallbackContext context)
    {
        _gameManagerSO.Instance.PlayerExitingGameSession();
    }

    private void OnEnterPressed(InputAction.CallbackContext context)
    {
        _chatManagerSO.Instance.EnableDisableChatCanvas();
    }

    private void OnMPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Pressed M");
        _chatManagerSO.Instance.ToggleVoiceMute();
    }

    private void OnTabPressed(InputAction.CallbackContext context)
    {
        if (_interactableSO != null && _interactableSO.Instance._doubleInteractable)
        {
            _interactableSO.Instance.ToggleInteractableOptionsPanel();
        }
    }

    #endregion

    #region Button Click from Player Controller

    public void ClickMute()
    {
        _chatManagerSO.Instance.ToggleVoiceMute();
    }

    public void ClickChatEnter()
    {
        _chatManagerSO.Instance.EnableDisableChatCanvas();
    }

    public void ClickJump()
    {
        if(_playerControllerSO != null || _playerControllerSO.Instance != null)
        {
            _playerControllerSO.Instance.ClickJump();
        }
        else
        {
            Debug.LogError("No Instance of PlayerController.cs");
        }
    }

    public void ClickExit()
    {
        _gameManagerSO.Instance.PlayerExitingGameSession();
    }
    #endregion
}
