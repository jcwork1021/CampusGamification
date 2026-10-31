using UnityEngine;


[CreateAssetMenu(menuName = "SO/ReactAction/AutoJoin")]
public class AutoJoinReactAction : InteractorUIReactActions
{
    [SerializeField] InteractableAutoJoinRefSO _interactableAutoJoinSO;

    public override void InitializeUIAsset()
    {
        // Implement any UI initialization if required by the base class, 
        // otherwise leave blank like the GameEvent example.
    }

    public override void InteractorReact(Interactable interactable)
    {
        if(_interactableAutoJoinSO != null && _interactableAutoJoinSO.Instance != null)
        {
            _interactableAutoJoinSO.Instance.OperatorInteraction();
        }
        else
        {
            Debug.LogError("There is no AutoJoin.cs Instance");
        }
    }
}
