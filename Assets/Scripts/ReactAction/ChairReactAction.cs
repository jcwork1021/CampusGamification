using UnityEngine;


[CreateAssetMenu(menuName = "SO/ReactAction/Chair")]
public class ChairReactAction : InteractorUIReactActions
{
    [SerializeField] InteractableChairRefSO _interactableChairSO;

    public override void InitializeUIAsset()
    {
        // Implement any UI initialization if required by the base class, 
        // otherwise leave blank like the GameEvent example.
    }

    public override void InteractorReact(Interactable interactable)
    {
        if(_interactableChairSO != null && _interactableChairSO.Instance != null)
        {
            _interactableChairSO.Instance.OperatorInteraction();
        }
        else
        {
            Debug.LogWarning("There is no AutoJoin.cs Instance");
        }
    }
}
