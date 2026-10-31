using UnityEngine;


[CreateAssetMenu(menuName = "SO/ReactAction/Session")]
public class SessionReactAction : InteractorUIReactActions
{
    [SerializeField] InteractableSessionRefSO _interactableSessionSO;

    public override void InitializeUIAsset()
    {
        // Implement any UI initialization if required by the base class, 
        // otherwise leave blank like the GameEvent example.
    }

    public override void InteractorReact(Interactable interactable)
    {
        if(_interactableSessionSO != null || _interactableSessionSO.Instance != null)
        {
            _interactableSessionSO.Instance.OperatorInteraction();
        }
        else
        {
            Debug.LogWarning("There is no Session Instance");
        }
    }
}
