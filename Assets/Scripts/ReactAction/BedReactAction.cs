using UnityEngine;


[CreateAssetMenu(menuName = "SO/ReactAction/Bed")]
public class BedReactAction : InteractorUIReactActions
{
    [SerializeField] InteractableBedRefSO _interactableBedSO;

    public override void InitializeUIAsset()
    {
        // Implement any UI initialization if required by the base class, 
        // otherwise leave blank like the GameEvent example.
    }

    public override void InteractorReact(Interactable interactable)
    {
        if(_interactableBedSO != null && _interactableBedSO.Instance != null)
        {
            _interactableBedSO.Instance.OperatorInteraction();
        }
        else
        {
            Debug.LogWarning("There is no AutoJoin.cs Instance");
        }
    }
}
