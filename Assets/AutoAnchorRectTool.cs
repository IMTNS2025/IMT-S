#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// AutoAnchorFromPosition
/// 
/// Unity Editor tool that automatically recalculates and sets the RectTransform anchors 
/// of selected UI elements based on their current position and size inside their parent RectTransform.
/// 
/// Useful for ensuring UI elements correctly scale and reposition across different screen resolutions,
/// eliminating hard-coded offsets and improving responsive design.
/// 
/// Access via: Tools > Auto Anchor UI (Shortcut: Ctrl + Alt + A)
/// </summary>
public class AutoAnchorFromPosition : EditorWindow
{
    /// <summary>
    /// Auto-anchors all currently selected GameObjects with RectTransform components.
    /// </summary>
    [MenuItem("Tools/Auto Anchor UI %&a")]
    static void AutoAnchorUI()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            // Skip objects that do not have a RectTransform or no parent
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null || rect.parent == null)
                continue;

            // Validate that the RectTransform is under a suitable Canvas (non-WorldSpace)
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
            {
                Debug.LogWarning($"{obj.name} is not in a suitable Canvas (needs Overlay or Screen Space - Camera). Skipped.");
                continue;
            }

            // Ensure the parent is also a RectTransform
            RectTransform parent = rect.parent.GetComponent<RectTransform>();
            if (parent == null)
                continue;

            // Record an Undo step for safe editor operations
            Undo.RecordObject(rect, "Auto Anchor");

            // Retrieve parent size and element's pivot
            Vector2 parentSize = parent.rect.size;
            Vector2 pivot = rect.pivot;

            // Get the local position and size of the element
            Vector2 localPos = rect.localPosition;
            Vector2 size = rect.rect.size;

            // Calculate the normalized minimum and maximum anchor points
            Vector2 min = (localPos - Vector2.Scale(size, pivot)) / parentSize + parent.pivot;
            Vector2 max = min + size / parentSize;

            // Apply the calculated anchors to the RectTransform
            rect.anchorMin = min;
            rect.anchorMax = max;

            // Reset the anchored position and sizeDelta to zero to fully anchor the element
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            Debug.Log($"✅ Anchors set on: {obj.name}");
        }
    }
}
#endif
