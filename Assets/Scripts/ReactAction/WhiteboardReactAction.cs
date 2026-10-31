using UnityEngine;


[CreateAssetMenu(menuName = "SO/ReactAction/Whiteboard")]
public class WhiteboardReactAction : InteractorUIReactActions
{
    [SerializeField] InteractableWhiteboardRefSO _interactableWhiteboardSO;

    public override void InitializeUIAsset()
    {
        // Implement any UI initialization if required by the base class, 
        // otherwise leave blank like the GameEvent example.
    }

    public override void InteractorReact(Interactable interactable)
    {
        if(_interactableWhiteboardSO != null && _interactableWhiteboardSO.Instance != null)
        {
            _interactableWhiteboardSO.Instance.OperatorInteraction();
        }
        else
        {
            Debug.LogWarning("There is no InteractableWhiteboard.cs Instance");
        }
    }
}
