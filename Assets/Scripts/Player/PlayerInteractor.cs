using Fusion;
using System.Collections.Generic;
using UnityEngine;


//Detects nearby Interactables via Physics OverlapSphere, selects the closest one, and handles hotkey inputs.
public class PlayerInteractor : NetworkBehaviour
{
    [Header("Necessary SO")]
    [SerializeField] SOInteractor _playerInteractorSO;

    [Header("Detection Settings")]
    [SerializeField] private float _interactionRadius = 3f;
    [SerializeField] private LayerMask _interactableLayer = ~0; // Default to Everything
    [SerializeField] private Vector3 _detectionOffset = Vector3.zero;

    [Header("Gizmo Visualization")]
    [SerializeField] private bool _showGizmos = true;
    [SerializeField] private Color _gizmoColor = new Color(0f, 1f, 0.5f, 0.25f);

    [Header("Read-Only")]
    [SerializeField] Interactable _currentInteractable;
    [SerializeField] bool isInteracting;

    private readonly List<Interactable> _inRangeInteractables = new();
    private readonly Collider[] _overlapResults = new Collider[16];

    #region Fusion Callbacks
    public override void Spawned()
    {
        //ONLY assign the ScriptableObject if this is the local player's character!
        if (HasInputAuthority)
        {
            _playerInteractorSO.Value = this;
            enabled = true; //Enable detection for local player
        }
        else
        {
            enabled = false; //Disable script for remote copies of other players
        }
    }
    #endregion

    #region Unity Functions
    private void OnDestroy()
    {
        //Only clear the SO if this exact instance was the local player registered to it
        if (HasInputAuthority && _playerInteractorSO != null && _playerInteractorSO.Value == this)
        {
            _playerInteractorSO.Value = null;
        }
    }

    private void OnDisable()
    {
        ClearAllInteractables();
    }

    private void Update()
    {
        DetectInteractablesInRadius();
        RefreshNearestInteractable(); //Without this we cannot detect the  NearestInteractable

        if (_currentInteractable != null)
        {
            _currentInteractable.HandleHotkeys();
        }
    }
    #endregion

    #region Sphere Overlap Detection

    private void DetectInteractablesInRadius()
    {
        _inRangeInteractables.Clear();
        Vector3 center = transform.position + transform.TransformDirection(_detectionOffset);

        int count = Physics.OverlapSphereNonAlloc(center, _interactionRadius, _overlapResults, _interactableLayer);

        for (int i = 0; i < count; i++)
        {
            if (_overlapResults[i].TryGetComponent(out Interactable interactable))
            {
                if (interactable.IsInteractableActive && !_inRangeInteractables.Contains(interactable))
                {
                    _inRangeInteractables.Add(interactable);
                }
            }
        }
    }

    #endregion

    #region Target Evaluation

    private void RefreshNearestInteractable()
    {
        if (isInteracting) return;

        Interactable nearest = FindNearestInteractable();

        if (nearest == _currentInteractable) return;

        // Deactivate prompt on previous object before changing
        if (_currentInteractable != null)
        {
            _currentInteractable.DeactivatePrompt();
        }

        _currentInteractable = nearest;

        if (_currentInteractable != null)
        {
            _currentInteractable.ActivatePrompt();
        }
    }

    private Interactable FindNearestInteractable()
    {
        Interactable nearest = null;
        float minSqrDistance = float.MaxValue;
        Vector3 playerPos = transform.position;

        foreach (var interactable in _inRangeInteractables)
        {
            if (interactable == null) continue;

            float sqrDist = (interactable.transform.position - playerPos).sqrMagnitude;
            if (sqrDist < minSqrDistance)
            {
                minSqrDistance = sqrDist;
                nearest = interactable;
            }
        }

        return nearest;
    }

    private void ClearAllInteractables()
    {
        if (_currentInteractable != null)
        {
            _currentInteractable.DeactivatePrompt();
            _currentInteractable = null;
        }



        _inRangeInteractables.Clear();
    }

    //Calls by abstract script when Interaction is Exited to mark the current interactable available again
    public void ReleaseInteraction()
    {
        if (_currentInteractable == null) 
        {
            Debug.LogError("No Interactable " + this.gameObject.name);
            return;
        }

        Debug.Log("Call Release Authority");
        _currentInteractable.ReleaseInteraction();
    }

    #endregion

    #region Scene View Gizmos
    private void OnDrawGizmosSelected()
    {
        if (!_showGizmos) return;

        Vector3 center = transform.position + transform.TransformDirection(_detectionOffset);

        // Draw solid translucent sphere
        Gizmos.color = _gizmoColor;
        Gizmos.DrawSphere(center, _interactionRadius);

        // Draw wireframe outline
        Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 1f);
        Gizmos.DrawWireSphere(center, _interactionRadius);
    }

    #endregion
}