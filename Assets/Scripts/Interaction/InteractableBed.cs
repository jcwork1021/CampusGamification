using Fusion;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;
using UnityEngine.UIElements.Experimental;

public class InteractableBed : BaseInteractable
{
    [Header("Necessary SO")]
    [SerializeField] InteractableBedRefSO _interactableBedSO;

    [Header("Reference from Itself")]
    [SerializeField] Transform _layDownPosition;
    [SerializeField] Transform _standPosition;

    [Header("Optional")]
    [SerializeField] GameObject _foodItem;

    private Vector2 moveInput;

    [SerializeField] InputActionReference _moveAction;

    [Networked, OnChangedRender(nameof(OnEatChanged))]
    public NetworkBool IsEating { get; set; }


    #region GameEvent Overrides
    private void OnEnable()
    {
        // Subscribe to WASD movement perform event
        _moveAction.action.performed += OnMovePerformed;
    }

    private void OnDisable()
    {
        _moveAction.action.performed -= OnMovePerformed;
    }

    //This is called when Interactable is collided with player
    public override void CallWhenInteracted()
    {
        base.CallWhenInteracted();
        _interactableBedSO.Instance = this;
    }
    protected override void OnLevelRequirementPassed()
    {
        base.OnLevelRequirementPassed();

        _playerManagerSO.Instance.RepositionPlayer(_layDownPosition); //Reposition Player to the chair
        _playerManagerSO.Instance.animator.SetBool("Laying_Cough", true);
    }


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
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        if(!currentlyInUsed || IsEating) //if not currently using this interactable
        {
            if (IsEating)
            {
                Debug.LogWarning("Wait! currently eating");
            }

            return;
        }

        // Only stand up if the player is actually sitting
        moveInput = context.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude > 0.1f) // Player pressed a direction
        {
            _playerManagerSO.Instance.RepositionPlayer(_standPosition);
            _playerManagerSO.Instance.animator.SetBool("Laying_Cough", false);

            //Ask the InteractableManager which is our Handler to end this Interactable
            ExitInteractable();
        }
    }
    #endregion
}
