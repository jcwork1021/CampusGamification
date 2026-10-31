using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A single, reusable interaction choice (Chat, Roast, Cheer, Challenge, Inspect, ...).
/// Authored once as an asset and shared across every interactable that offers it, instead of
/// being re-declared per button in the inspector. Bundles the display info plus the existing
/// condition/action SOs that drive its behaviour.
/// </summary>
[CreateAssetMenu(menuName = "SO/Interaction/InteractionOption")]
public class InteractionOption : ScriptableObject
{
    [SerializeField] private string _label;
    [SerializeField] private Sprite _icon;

    [Tooltip("Optional sprite shown beside the label (e.g. a chat bubble). " +
             "Drag a sprite here per option; leave empty to hide the emoji image.")]
    [SerializeField] private Sprite _emoji;

    [Header("Hotkey")]
    [Tooltip("Keyboard key that triggers this option while its prompt is visible (same as clicking it). " +
             "Set to None for click/controller-only options.")]
    [SerializeField] private Key _hotkey = Key.None;

    [Tooltip("Optional. Text shown on the button's key badge. Defaults to the key name (e.g. \"E\").")]
    [SerializeField] private string _hotkeyDisplayOverride;

    [Tooltip("Drives button.interactable + greyed-out feedback. " +
             "For 'enable buttons only' interactable objects, keep this true with an empty Actions list.")]
    [SerializeField] private bool _enabled = true;

    [SerializeField] private List<InteractorUIReactActions> _actions = new();

    public string Label => _label;
    public Sprite Icon => _icon;

    /// <summary>Optional sprite shown beside the label, or null to hide the emoji image.</summary>
    public Sprite Emoji => _emoji;

    public bool Enabled => _enabled;

    /// <summary>Keyboard key that triggers this option, or <see cref="Key.None"/> for click/controller only.</summary>
    public Key Hotkey => _hotkey;

    /// <summary>Text for the button's key badge: the override if set, else the key name, else empty.</summary>
    public string HotkeyDisplay =>
        !string.IsNullOrEmpty(_hotkeyDisplayOverride) ? _hotkeyDisplayOverride
        : _hotkey == Key.None ? string.Empty
        : _hotkey.ToString();
    public IReadOnlyList<InteractorUIReactActions> Actions => _actions;
}
