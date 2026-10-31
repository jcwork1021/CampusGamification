using UnityEngine;


[CreateAssetMenu(menuName = "SO/ReactAction2/RaiseHand")]
public class RaiseHandReactAction2 : InteractorUIReactActions
{
    [SerializeField] InteractableChairRefSO _interactableChairSO;

    public override void InitializeUIAsset()
    {
        // Implement any UI initialization if required by the base class, 
        // otherwise leave blank like the GameEvent example.
    }

    public override void InteractorReact(Interactable interactable)
    {
        if(_interactableChairSO != null)
        {
            _interactableChairSO.Instance.DoRaiseHand();
        }
        else
        {
            Debug.LogWarning("There is no Instance for PlayerManager.cs");
        }
    }
}
