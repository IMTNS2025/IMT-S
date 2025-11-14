using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ensures the native aspect ratio of an attached Image or RawImage is preserved.
/// Works with Image.sprite and RawImage.texture. Adjusts this GameObject's RectTransform
/// so the image is fitted according to the selected FitMode.
/// This version captures the aspect ratio at Start and preserves that initial aspect
/// for all subsequent adjustments.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class PreserveAspectRatio : MonoBehaviour
{
    public enum FitMode
    {
        Contain,   // Fit inside the rect (no cropping) — letterbox if needed
        Cover,     // Fill the rect (may crop) — similar to CSS cover
        FitWidth,  // Match width, adjust height to preserve aspect
        FitHeight  // Match height, adjust width to preserve aspect
    }

    [Tooltip("Which fitting strategy to use when preserving aspect ratio.")]
    [SerializeField] private FitMode fit = FitMode.Contain;

    [Tooltip("When enabled the rect will be updated every frame (useful for dynamic layouts).")]
    [SerializeField] private bool updateEveryFrame = true;

    // The aspect ratio captured at Start() and used thereafter.
    // If it cannot be determined at Start, a fallback is used.
    private float initialAspect = 0f;

    private RectTransform rt;
    private Image uiImage;
    private RawImage rawImage;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        uiImage = GetComponent<Image>();
        rawImage = GetComponent<RawImage>();
    }

    private void Start()
    {
        // Capture the aspect ratio at start and preserve it for subsequent operations.
        initialAspect = GetIntrinsicAspect();
        if (initialAspect <= 0f)
        {
            // fallback to the current rect transform aspect if no image/texture is available
            var size = rt != null ? rt.rect.size : Vector2.one;
            if (size.y != 0f)
                initialAspect = Mathf.Max(0.0001f, size.x / size.y);
            else
                initialAspect = 1f; // final fallback
        }

        Refresh();
    }

    private void OnValidate()
    {
        // Keep editor changes visible immediately
        if (!Application.isPlaying)
            Awake();
        // Do not overwrite the captured initialAspect in the editor validation step.
        Refresh();
    }

    private void LateUpdate()
    {
        if (updateEveryFrame)
            Refresh();
    }

    /// <summary>
    /// Force a recalculation / application of the preserved aspect ratio.
    /// Uses the aspect captured at Start() when available.
    /// </summary>
    public void Refresh()
    {
        if (rt == null) rt = GetComponent<RectTransform>();

        // Use the aspect captured at Start() when available, otherwise try to get intrinsic.
        float intrinsicAspect = initialAspect > 0f ? initialAspect : GetIntrinsicAspect();
        if (intrinsicAspect <= 0f) return;

        // Use the current rect as the available bounds to fit into
        Vector2 available = rt.rect.size;
        if (available.x <= 0f || available.y <= 0f) return;

        float availableAspect = available.x / available.y;
        float newW = available.x;
        float newH = available.y;

        switch (fit)
        {
            case FitMode.Contain:
                if (availableAspect > intrinsicAspect)
                {
                    // available is wider -> limit by height
                    newH = available.y;
                    newW = newH * intrinsicAspect;
                }
                else
                {
                    // available is taller/narrower -> limit by width
                    newW = available.x;
                    newH = newW / intrinsicAspect;
                }
                break;

            case FitMode.Cover:
                if (availableAspect < intrinsicAspect)
                {
                    // available is narrower -> match height
                    newH = available.y;
                    newW = newH * intrinsicAspect;
                }
                else
                {
                    // available is wider -> match width
                    newW = available.x;
                    newH = newW / intrinsicAspect;
                }
                break;

            case FitMode.FitWidth:
                newW = available.x;
                newH = newW / intrinsicAspect;
                break;

            case FitMode.FitHeight:
                newH = available.y;
                newW = newH * intrinsicAspect;
                break;
        }

        // Apply new size preserving anchors behaviour
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newW);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newH);
    }

    private float GetIntrinsicAspect()
    {
        // Prefer Image.sprite if present, otherwise use RawImage.texture
        if (uiImage != null && uiImage.sprite != null)
        {
            var rect = uiImage.sprite.rect;
            if (rect.height != 0f)
                return rect.width / rect.height;
        }

        if (rawImage != null && rawImage.texture != null && rawImage.texture.height != 0)
        {
            return (float)rawImage.texture.width / rawImage.texture.height;
        }

        return 0f;
    }
}