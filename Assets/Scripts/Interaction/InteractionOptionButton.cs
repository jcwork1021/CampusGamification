using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionOptionButton : MonoBehaviour
{
    [SerializeField] private TMP_Text _labelText;
    [SerializeField] private TMP_Text _hotkeyText;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Button _button;

    public InteractionOption OptionData { get; private set; }

    public void Setup(InteractionOption option, System.Action<InteractionOption> onClickCallback)
    {
        OptionData = option;

        if (_labelText != null)
            _labelText.text = option.Label;

        if (_hotkeyText != null)
            _hotkeyText.text = option.HotkeyDisplay;

        if (_iconImage != null)
        {
            _iconImage.sprite = option.Icon;
            _iconImage.gameObject.SetActive(option.Icon != null);
        }

        if (_button != null)
        {
            _button.interactable = option.Enabled;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClickCallback?.Invoke(option));
        }
    }
}