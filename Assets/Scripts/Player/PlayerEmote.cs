using DG.Tweening;
using Fusion;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEmote: NetworkBehaviour
{
    [Header("Necessary SO")]
    [SerializeField] PlayerEmoteRefSO _playerEmoteSO;
    [SerializeField] EmoteLibrarySO _emoteLibrarySO;
    [SerializeField] GameManagerRefSO _gameManagerSO;

    [Header("Player SO")]
    [SerializeField] PlayerManagerRefSO _playerManagerSO;

    [Header("Reference from Player Bubble Canvas")]
    [SerializeField] Image _emoteImage;
    [SerializeField] CanvasGroup _emoteImageCanvasGroup;

    [Networked, OnChangedRender(nameof(OnPlayerEmote))]
    public int networkedEmote{ get; set; } //this is changed in InteractableChair.cs

    private Sequence emoteSequence;

    #region Unity Function
    private void Awake()
    {
        // Initializes dictionaries on startup
        if (_emoteLibrarySO != null)
            _emoteLibrarySO.Initialize();

        if (_emoteImage != null)
            _emoteImage.enabled = false;
    }

    private void OnDestroy()
    {
        if (HasStateAuthority)
        {
            _playerEmoteSO.Instance = null;
        }
    }
    #endregion

    #region Fusion Callbacks
    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            _playerEmoteSO.Instance = this;
        }
    }

    #endregion

    #region Networked Function

    public void OnPlayerEmote()
    {
        StartCoroutine(ShowHideEmote());
    }

    IEnumerator ShowHideEmote()
    {
        // Always kill any active tween on this object before starting a new one
        emoteSequence?.Kill();

        // 1. Initial State Setup
        _emoteImage.transform.localScale = Vector3.zero;
        _emoteImage.transform.localRotation = Quaternion.identity; // Reset rotation to 0

        if(_emoteImageCanvasGroup != null)
        {
            _emoteImageCanvasGroup.alpha = 1f;
        }

        // 2. Create the DOTween Sequence
        emoteSequence = DOTween.Sequence();

        emoteSequence
            // Step A: Pop Up (Scale 0 to 1 with a springy overshoot)
            .Append(_emoteImage.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack))

            // Step B: Tilt Left (-12 degrees)
            .Append(_emoteImage.transform.DORotate(new Vector3(0, 0, -12f), 0.1f).SetEase(Ease.OutQuad))

            // Step C: Tilt Right (+12 degrees)
            .Append(_emoteImage.transform.DORotate(new Vector3(0, 0, 12f), 0.12f).SetEase(Ease.InOutQuad))

            // Step D: Return back to straight (0 degrees)
            .Append(_emoteImage.transform.DORotate(Vector3.zero, 0.08f).SetEase(Ease.OutQuad))

            // Step E: Wait/Hold on screen for 1.5 seconds
            .AppendInterval(1.5f)

            // Step F: Pop Out & Fade Out (Shrink back to 0 and fade to 0 alpha)
            .Append(_emoteImage.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));

        if (_emoteImageCanvasGroup != null)
        {
            emoteSequence.Join(_emoteImageCanvasGroup.DOFade(0f, 0.2f));
        }

        Sprite sprite = _emoteLibrarySO.GetEmoteSprite(networkedEmote);
        if (sprite != null)
        {
            _emoteImage.sprite = sprite;
            _emoteImage.enabled = true;
            _playerManagerSO.Instance.ToggleNameText(false);
        }

        yield return new WaitForSeconds(2.5f);
        _emoteImage.enabled = false;
        _playerManagerSO.Instance.ToggleNameText(true);
    }

    #endregion
    // Call by index ID (Best for network RPCs)
    public void ShowEmoteByID(int emoteID)
    {
        networkedEmote = emoteID;
    }

    // Call by filename string (e.g., "E1")
    public void ShowEmoteByName(string emoteName)
    {
        Sprite sprite = _emoteLibrarySO.GetEmoteSprite(emoteName);
        if (sprite != null)
        {
            _emoteImage.sprite = sprite;
            _emoteImage.enabled = true;
        }
    }
}