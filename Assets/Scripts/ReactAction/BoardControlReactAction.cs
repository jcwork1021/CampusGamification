using UnityEngine;


[CreateAssetMenu(menuName = "SO/ReactAction/BoardControl")]
public class BoardControlReactAction : InteractorUIReactActions
{
    [SerializeField] InteractableBoardControlRefSO _interactableBoardControlSO;

    public override void InitializeUIAsset()
    {
        // Implement any UI initialization if required by the base class, 
        // otherwise leave blank like the GameEvent example.
    }

    public override void InteractorReact(Interactable interactable)
    {
        if(_interactableBoardControlSO != null && _interactableBoardControlSO.Instance != null)
        {
            _interactableBoardControlSO.Instance.OperatorInteraction();
        }
        else
        {
            Debug.LogWarning("There is no BoardControl.cs Reference");
        }
    }
}
