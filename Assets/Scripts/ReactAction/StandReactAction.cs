using UnityEngine;


[CreateAssetMenu(menuName = "SO/ReactAction/Desk")]
public class StandReactAction : InteractorUIReactActions
{
    [SerializeField] InteractableDeskRefSO _interactableDeskSO;

    public override void InitializeUIAsset()
    {
        // Implement any UI initialization if required by the base class, 
        // otherwise leave blank like the GameEvent example.
    }

    public override void InteractorReact(Interactable interactable)
    {
        if(_interactableDeskSO != null && _interactableDeskSO.Instance != null)
        {
            _interactableDeskSO.Instance.OperatorInteraction();
        }
        else
        {
            Debug.LogWarning("There is no InteractableStand.cs Instance");
        }
    }
}
