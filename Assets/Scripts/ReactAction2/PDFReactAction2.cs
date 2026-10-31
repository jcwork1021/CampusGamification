using UnityEngine;


[CreateAssetMenu(menuName = "SO/ReactAction2/PDF")]
public class PDFReactAction2 : InteractorUIReactActions
{
    [SerializeField] RememberMeRefSO _rememberMeSO;
    [SerializeField] InteractableRefSO _interactableSO;
    [SerializeField] InteractablePDFRefSO _interactablePDFSO;

    public override void InitializeUIAsset()
    {
        // Implement any UI initialization if required by the base class, 
        // otherwise leave blank like the GameEvent example.
    }

    public override void InteractorReact(Interactable interactable)
    {
        if(_interactablePDFSO.Instance != null)
        {
            _interactablePDFSO.Instance.ViewerInteraction();
        }
        else
        {
            _rememberMeSO.Instance.EnableWarning("No PDF in the Room");

            if (_interactableSO != null)
            {
                _interactableSO.Instance.CancelSecondInteractable();
            }
        }
    }
}
