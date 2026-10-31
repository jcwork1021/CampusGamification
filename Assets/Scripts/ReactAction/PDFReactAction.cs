using UnityEngine;


[CreateAssetMenu(menuName = "SO/ReactAction/PDF")]
public class PDFReactAction : InteractorUIReactActions
{
    [SerializeField] InteractablePDFRefSO _interactablePDFSO;

    public override void InitializeUIAsset()
    {
        // Implement any UI initialization if required by the base class, 
        // otherwise leave blank like the GameEvent example.
    }

    public override void InteractorReact(Interactable interactable)
    {
        if(_interactablePDFSO != null && _interactablePDFSO.Instance != null)
        {
            _interactablePDFSO.Instance.OperatorInteraction();
        }
        else
        {
            Debug.LogWarning("There is no PDF Instance");
        }
    }
}
