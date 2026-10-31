using Fusion;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;
using UnityEngine.UIElements.Experimental;

public class InteractableChair : BaseInteractable
{
    [Header("Necessary SO")]
    [SerializeField] QuestionManagerRefSO _questionManagerSO;
    [SerializeField] InteractableChairRefSO _interactableChairSO;

    [Header("Reference from Itself")]
    [SerializeField] Transform _sitPosition;
    [SerializeField] Transform _standPosition;

    [Header("Optional")]
    [SerializeField] GameObject _foodItem;

    private Vector2 moveInput;

    [SerializeField] InputActionReference _moveAction;

    [Networked, OnChangedRender(nameof(OnEatChanged))]
    public NetworkBool IsEating { get; set; }

    #region Unity Functions

    private void OnEnable()
    {
        // Subscribe to WASD movement perform event
        _moveAction.action.performed += OnMovePerformed;
    }

    private void OnDisable()
    {
        _moveAction.action.performed -= OnMovePerformed;
    }
    #endregion

    #region GameEvent Overrides

    //This is called when Interactable is collided with player
    public override void CallWhenInteracted()
    {
        base.CallWhenInteracted();
        _interactableChairSO.Instance = this;
    }
    protected override void OnLevelRequirementPassed()
    {
        base.OnLevelRequirementPassed();

        _playerManagerSO.Instance.RepositionPlayer(_sitPosition); //Reposition Player to the chair
        _playerManagerSO.Instance.animator.SetBool("Sit", true);
        //StartCoroutine(PositionDelay(_gameManagerSO.Instance._localPlayerNetworkObject.transform, this.transform));
    }

    /*
    IEnumerator PositionDelay(Transform playerPosition, Transform objectPosition)
    {
        //yield return new WaitForSeconds(1);

        _playerManagerSO.Instance.RepositionPlayer(_sitPosition); //Reposition Player to the chair
        _playerManagerSO.Instance.animator.SetBool("Sit", true);
        Debug.LogError("NEW POS B: " + playerPosition.position + " " + objectPosition.position);
    }
    */

    protected override void OnLevelRequirementFailed()
    {
        base.OnLevelRequirementFailed();

        Debug.Log("Displaying 'Level Too Low' notification UI to player.");
    }

    public override void ExitInteractable()
    {
        base.ExitInteractable();
    }

    #endregion

    #region Network Sync Functions

    // Handles late-joiners & initial state Sync when object enters the scene
    public override void Spawned()
    {
        OnEatChanged();
    }

    public void OnEatChanged()
    {
        // Sync the food object visibility across ALL clients
        if (_foodItem != null)
        {
            _foodItem.SetActive(IsEating);
        }

    }
    #endregion
 
    #region Additional functions for ReactActions and Exit
    //Calls by EatReaction.cs
    public void DoEat()
    {
        if (_foodItem == null)
        {
            Debug.LogWarning("No Food Item so it will be skipped");
            return;
        }

        if (_playerManagerSO.Instance.animator.GetBool("Eat") == false)
        {
            _playerManagerSO.Instance.animator.SetBool("Eat", true);
            IsEating = true;

            StartCoroutine(DelayEating()); //so it can be off
        }
        else
        {
            Debug.LogWarning("Already Eating");
        }
    }

    //Calls in Eat()
    IEnumerator DelayEating()
    {
        yield return new WaitForSeconds(3);
        _playerManagerSO.Instance.animator.SetBool("Eat", false);

        ExitInteractable2(); //Mark the Interactable that we finished the 2nd Interaction
        IsEating = false;
    }


    public void DoRaiseHand()
    {
        _playerManagerSO.Instance.networkedRaisedHand = true;

        //Mark that we are doing 2nd Interactable
        _interactableCS.ToggleSecondInteractable(true);
        _gameManagerSO.Instance.ToggleRaiseHand(true);

        _interactableCS.DeactivatePrompt();
    }

    public void DoFPP()
    {
        FocusInteractableCamera(10); //This is just for FPP stuff

        //Mark that we are doing 2nd Interactable
        _gameManagerSO.Instance.TogglePlayerCanvas(false);
        _interactableCS.ToggleSecondInteractable(true);
        _gameManagerSO.Instance.ToggleChairFPPCanvas(true);

        _interactableCS.DeactivatePrompt();
    }

    public void ExitFPPInteractable()
    {
        FocusInteractableCamera(0);
        ExitInteractable2();

        _gameManagerSO.Instance.TogglePlayerCanvas(true);
    }

    public void ExitRaiseHand()
    {
        _playerManagerSO.Instance.networkedRaisedHand = false;
        ExitInteractable2();

        _gameManagerSO.Instance.ToggleRaiseHand(false);
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {

        if (!currentlyInUsed)
        {
            //Debug.LogWarning("Chair is not being used so do not read input from here");
            return;
        }

        if (IsEating)
        {
            Debug.LogWarning("Wait! currently eating");
            return;
        }

        if (_interactableCS._interactedSecond)
        {
            Debug.LogWarning("Currently doing 2nd Interactable");
            return;
        }

        if(_questionManagerSO.Instance != null) //if taking exam then return
        {
            if (_questionManagerSO.Instance.doingExam) return;
        }

        // Only stand up if the player is actually sitting
        moveInput = context.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude > 0.1f) // Player pressed a direction
        {
            _playerManagerSO.Instance.RepositionPlayer(_standPosition);
            _playerManagerSO.Instance.animator.SetBool("Sit", false);

            //_interactableChairSO.Instance = null;

            //Ask the InteractableManager which is our Handler to end this Interactable
            ExitInteractable();
        }
    }
    #endregion
}
