using UnityEngine;
using DG.Tweening;

public class RotateUI : MonoBehaviour
{
    [SerializeField] private RectTransform uiImageRect;
    [SerializeField] private float durationPerRotation = 2f;

    private Tween rotateTween;

    private void OnEnable()
    {
        // Start or restart the endless rotation when enabled
        rotateTween = uiImageRect.DORotate(new Vector3(0, 0, -360), durationPerRotation, RotateMode.FastBeyond360)
                                .SetEase(Ease.Linear)
                                .SetLoops(-1, LoopType.Incremental);
    }

    private void OnDisable()
    {
        // Kill the tween immediately when disabled to avoid ghost animations in the background
        rotateTween?.Kill();
    }
}