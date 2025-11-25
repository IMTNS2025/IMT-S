using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps a contamination spot positioned and sized relative to its parent container rect.
/// Updates the owning DecontaminationItemInfo.contaminationSpots entry's pos field so existing
/// code that reads contaminationSpot.pos continues to work.
/// </summary>
[RequireComponent(typeof(RectTransform), typeof(RawImage))]
public class ContaminationSpotController : MonoBehaviour
{
    private RectTransform rt;
    private RectTransform parentRt;
    private DecontaminationItemInfo owner;
    private RawImage image;

    // normalized UV coords inside the texture (0..1)
    private float u;
    private float v;

    // normalized size relative to texture dimensions
    private float relW;
    private float relH;

    private Vector2 lastParentSize = Vector2.zero;

    /// <summary>
    /// Initialize the controller with normalized values.
    /// </summary>
    public void Init(DecontaminationItemInfo owner, RawImage image, float u, float v, float relW, float relH)
    {
        this.owner = owner;
        this.image = image;
        this.u = Mathf.Clamp01(u);
        this.v = Mathf.Clamp01(v);
        this.relW = Mathf.Max(0f, relW);
        this.relH = Mathf.Max(0f, relH);

        rt = GetComponent<RectTransform>();
        parentRt = rt.parent as RectTransform;

        // force initial layout
        Recalculate(true);
    }

    private void Update()
    {
        if (parentRt == null) return;
        Vector2 parentSize = parentRt.rect.size;
        if (parentSize != lastParentSize)
        {
            Recalculate(false);
        }
    }

    private void Recalculate(bool force)
    {
        if (parentRt == null || rt == null) return;

        Rect rect = parentRt.rect;
        Vector2 parentSize = rect.size;
        if (!force && parentSize == lastParentSize) return;

        // compute actual size in parent local units
        float actualW = Mathf.Max(1f, relW * rect.width);
        float actualH = Mathf.Max(1f, relH * rect.height);

        // compute local position in parent's rect coordinates (same formula used in spawner)
        float localX = rect.x + u * rect.width;
        float localY = rect.y + v * rect.height;

        // clamp to keep the spot inside the parent's rect
        float halfW = actualW * 0.5f;
        float halfH = actualH * 0.5f;
        localX = Mathf.Clamp(localX, rect.x + halfW, rect.xMax - halfW);
        localY = Mathf.Clamp(localY, rect.y + halfH, rect.yMax - halfH);

        // apply size and position
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, actualW);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, actualH);
        rt.localPosition = new Vector3(localX, localY, rt.localPosition.z);

        lastParentSize = parentSize;

        // sync the struct entry pos with the new localPosition so other systems keep working
        SyncPosToOwner();
    }

    private void SyncPosToOwner()
    {
        if (owner == null || image == null) return;
        var list = owner.contaminationSpots;
        if (list == null || list.Count == 0) return;

        int idx = list.FindIndex(s => s.image == image);
        if (idx >= 0)
        {
            var s = list[idx];
            s.pos = rt.localPosition;
            list[idx] = s;
            owner.contaminationSpots = list;
        }
    }
}