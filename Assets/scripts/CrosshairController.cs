using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    public Image crosshair;
    private float baseAlpha;

    private void Awake()
    {
        if (crosshair != null) baseAlpha = crosshair.color.a;
    }
    private void Update()
    {
        if (crosshair == null) return;
        float aim = AimController.Instance != null ? AimController.Instance.AimAmount : 0f;
        Color c = crosshair.color;
        c.a = baseAlpha * (1f - Mathf.SmoothStep(0f, 1f, aim));
        crosshair.color = c;
    }
}
