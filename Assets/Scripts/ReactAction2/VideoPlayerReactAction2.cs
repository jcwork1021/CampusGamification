using UnityEngine;


[CreateAssetMenu(menuName = "SO/ReactAction2/VideoPlayer")]
public class VideoPlayerReactAction2 : InteractorUIReactActions
{
    [SerializeField] RememberMeRefSO _rememberMeSO;
    [SerializeField] InteractableRefSO _interactableSO;
    [SerializeField] InteractableVideoPlayerRefSO _interactableVideoPlayerSO;

    public override void InitializeUIAsset()
    {
        // Implement any UI initialization if required by the base class, 
        // otherwise leave blank like the GameEvent example.
    }

    public override void InteractorReact(Interactable interactable)
    {
        if(_interactableVideoPlayerSO.Instance != null)
        {
            _interactableVideoPlayerSO.Instance.ViewerInteraction();
        }
        else
        {
            _rememberMeSO.Instance.EnableWarning("No Video Player");

            if (_interactableSO != null)
            {
                _interactableSO.Instance.CancelSecondInteractable();
            }
        }
    }
}
