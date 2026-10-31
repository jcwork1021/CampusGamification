using UnityEngine;


[CreateAssetMenu(menuName = "SO/ReactAction/VideoPlayer")]
public class VideoPlayerReactAction : InteractorUIReactActions
{
    [SerializeField] InteractableVideoPlayerRefSO _interactableVideoPlayerSO;

    public override void InitializeUIAsset()
    {
        // Implement any UI initialization if required by the base class, 
        // otherwise leave blank like the GameEvent example.
    }

    public override void InteractorReact(Interactable interactable)
    {
        if(_interactableVideoPlayerSO != null && _interactableVideoPlayerSO.Instance != null)
        {
            _interactableVideoPlayerSO.Instance.OperatorInteraction();
        }
        else
        {
            Debug.LogWarning("There is no Video Player Instance");
        }
    }
}
