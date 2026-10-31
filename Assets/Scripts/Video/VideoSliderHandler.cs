using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class VideoSliderHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private InteractableVideoPlayer _videoPlayerController;
    private Slider _slider;
    [SerializeField] private bool _isPointerDown = false;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        _slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnDisable()
    {
        _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPointerDown = true;
        if (_videoPlayerController != null)
        {
            _videoPlayerController.OnSliderBeginDrag();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_isPointerDown)
        {
            _isPointerDown = false;
            if (_videoPlayerController != null)
            {
                // Seeking happens ONCE on mouse release / tap release
                _videoPlayerController.OnSliderEndDrag(_slider.value);
            }
        }
    }

    private void OnSliderValueChanged(float value)
    {
        if (_videoPlayerController == null) return;

        // ONLY update UI text while actively dragging.
        // Ignore automatic slider updates caused by Update() during video playback.
        if (_isPointerDown)
        {
            _videoPlayerController.OnSliderValueDragging(value);
        }
    }
}