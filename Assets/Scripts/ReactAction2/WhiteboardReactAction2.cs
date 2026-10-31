using UnityEngine;


[CreateAssetMenu(menuName = "SO/ReactAction2/Whiteboard")]
public class WhiteboardReactAction2 : InteractorUIReactActions
{
    [SerializeField] RememberMeRefSO _rememberMeSO;
    [SerializeField] InteractableRefSO _interactableSO;
    [SerializeField] InteractableWhiteboardRefSO _interactableWhiteboardSO;

    public override void InitializeUIAsset()
    {
        // Implement any UI initialization if required by the base class, 
        // otherwise leave blank like the GameEvent example.
    }

    public override void InteractorReact(Interactable interactable)
    {
        if(_interactableWhiteboardSO.Instance != null)
        {
            _interactableWhiteboardSO.Instance.ViewerInteraction();
        }
        else
        {
            _rememberMeSO.Instance.EnableWarning("No Whiteboard in the Room");

            if (_interactableSO != null)
            {
                _interactableSO.Instance.CancelSecondInteractable();
            }
        }
    }
}
