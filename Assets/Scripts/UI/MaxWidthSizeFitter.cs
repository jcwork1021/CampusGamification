using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class MaxWidthSizeFitter : MonoBehaviour, ILayoutElement
{
    public float maxWidth = 400f;

    private TextMeshProUGUI textComponent; // or 'Text' if using legacy UI

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    public float preferredWidth
    {
        get
        {
            if (textComponent == null) return -1;
            // Takes the smaller value between text's rendered width and our cap
            return Mathf.Min(textComponent.preferredWidth, maxWidth);
        }
    }

    public float minWidth => -1;
    public float flexibleWidth => -1;
    public float minHeight => -1;
    public float preferredHeight => -1;
    public float flexibleHeight => -1;
    public int layoutPriority => 1;

    public void CalculateLayoutInputHorizontal() { }
    public void CalculateLayoutInputVertical() { }
}