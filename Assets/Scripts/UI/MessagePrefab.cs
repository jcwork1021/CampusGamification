using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessagePrefab : MonoBehaviour
{
    [Header("Reference from Itself")]
    public Image userIcon;
    public TMP_Text messageTxt;

    public void SetText(string text)
    {
        messageTxt.text = text;

        // Check if active in hierarchy before starting the coroutine
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(RebuildLayout());
        }
        else
        {
            // Optional: Force rebuild directly if it's inactive, 
            // or rely on OnEnable() when it turns back on.
            RebuildLayoutImmediate();
        }
    }

    private IEnumerator RebuildLayout()
    {
        yield return new WaitForEndOfFrame();
        RebuildLayoutImmediate();
    }

    private void RebuildLayoutImmediate()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

            if (transform.parent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
            }
        }
    }
}
