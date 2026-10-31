/*
 * This is a Abstract Class
 */

using Fusion;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class BaseInteractable : NetworkBehaviour
{
    [Header("Necessary SO")]
    public RememberMeRefSO _rememberMeSO;
    public GameManagerRefSO _gameManagerSO;

    [Header("Reference from Parents")]
    [SerializeField] protected Interactable _interactableCS;

    [Header("Optional")]
    [SerializeField] CinemachineCamera _interactableCamera;

    [Header("Player SO")]
    public PlayerManagerRefSO _playerManagerSO;
    public SOInteractor _playerInteractorSO;

    [Header("Session Requirements")]
    [SerializeField] protected int minimumLevelRequired = 0;


    [Header("Read Only")]
    public bool currentlyInUsed; //public because it was being checked by InteractableManager.cs if this is the correct Interactable
    public GameObject _playerOperator;

    #region for GameEvent
    public virtual void CallWhenInteracted()
    {

    }
    #endregion

    public virtual void ViewerInteraction()
    {
        FocusInteractableCamera(10);
        currentlyInUsed = true; //Mark that we are using this
        _gameManagerSO.Instance.TogglePlayerCanvas(false); //Disable Player Canvas
        _playerManagerSO.Instance.EnableMovement(false); //Disable Player controls and colliders
    }

    public void OperatorInteraction()
    {
        // Check if player meets the level requirements
        if (_rememberMeSO != null && _rememberMeSO.Instance.playerLevel >= minimumLevelRequired)
        {
            OnLevelRequirementPassed();
        }
        else
        {
            OnLevelRequirementFailed();
        }
    }

    public void FocusInteractableCamera(int priorityValue)
    {
        if (_interactableCamera != null)
        {
            //Re Center First the rotation
            _interactableCamera.transform.localRotation = Quaternion.Euler(Vector3.zero);
            _interactableCamera.Priority = priorityValue;
        }
    }

    //Called when the player meet the minimum level required.
    protected virtual void OnLevelRequirementPassed()
    {     
        currentlyInUsed = true; //Mark that we are using this

        _playerManagerSO.Instance.EnableMovement(false); //Disable Player Controls and Colliders

        _playerOperator = _gameManagerSO.Instance._localPlayerNetworkObject.gameObject; //Mark that we are the operator
    }

    //Called when the player does not meet the minimum level required.
    protected virtual void OnLevelRequirementFailed()
    {
        Debug.LogWarning($"Player level is too low to interact. Required: {minimumLevelRequired}");
        _rememberMeSO.Instance.EnableWarning("You did not meet the required Level");
        currentlyInUsed = false;
    }

    //Called by InteractableManager.cs when Interactable scripts ask to end the Interaction
    public virtual void ExitInteractable()
    {
        FocusInteractableCamera(0);
        _playerManagerSO.Instance.EnableMovement(true); //Enable player controls and colliders
        _playerInteractorSO.Value.ReleaseInteraction(); //set the interactable to not busy so it can be interacted again
        currentlyInUsed = false; //Mark that we are using this

        _interactableCS.ToggleFirstInteractable(false); //Mark that we exited the primary or first Interactable

        _playerOperator = null; //we removed as the operator
    }

    //Calls by other script that can be used for 2nd Interactable to mark themselves as exited
    public virtual void ExitInteractable2()
    {
        _interactableCS.ToggleSecondInteractable(false); //Mark in Interactable.cs that we exited the 2nd Interactable
        //currentlyInUsed = false; //Mark that we are not using this
    }

}