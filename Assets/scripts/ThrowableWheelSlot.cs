using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ThrowableWheelSlot : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI countText;
    public Image highlight;
    public float highlightScale = 1.15f;
    private Vector3 baseScale = Vector3.one;
    private void Awake()
    {
        baseScale = transform.localScale;
    }

    public void Setup(string displayName, Sprite sprite, int count)
    {
        if (nameText != null) nameText.text = displayName;
        if (countText != null) countText.text = count.ToString();

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }
        Color c = new Color(1f, 1f, 1f, count > 0 ? 1f : 0.35f);
        if (icon != null) icon.color = c;
        if (nameText != null) nameText.color = c;
        if (countText != null) countText.color = c;

        SetHighlighted(false);
    }

    public void SetHighlighted(bool on)
    {
        if (highlight != null) highlight.enabled = on;
        transform.localScale = on ? baseScale * highlightScale : baseScale;
    }
    
}
