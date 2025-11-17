using UnityEngine;
using UnityEngine.UI;

public class BagManager : MonoBehaviour
{
    [SerializeField] private DropTarget workplate;

    //TODOALEX apply scaling to bag as well
    private DecontaminationToolInfo draggedToolInfo;
    private GameObject bagDispenserPlaceholder;
    private float firstBagSize = 0.75f;
    private float bagMulitplier = 0.25f;

    private void StartUseBag()
    {
        bagDispenserPlaceholder = new GameObject("BagDispenser", typeof(RectTransform));
        bagDispenserPlaceholder.transform.SetParent(draggedToolInfo.transform.parent, false);
        RectTransform rt = bagDispenserPlaceholder.GetComponent<RectTransform>();
        rt.sizeDelta = draggedToolInfo.originalSize * draggedToolInfo.scaleContainer;
        RawImage ri = bagDispenserPlaceholder.AddComponent<RawImage>();
        ri.raycastTarget = false;

        // Assign texture from the sprite (if available)
        ri.texture = draggedToolInfo.imageOriginal.texture;
        ri.color = Color.white;

        draggedToolInfo.GetComponent<RawImage>().texture = draggedToolInfo.imageDrag.texture;
    }
      
    private void UseBag()
    {
        if (workplate == null || !workplate.IsOccupied() || draggedToolInfo == null || draggedToolInfo.toolType != ToolTypes.Bag) return;

        DragAndDrop occupiedItemDragDrop = workplate.GetObjectOccupied();
        DecontaminationItemInfo occupiedItemInfo = occupiedItemDragDrop.GetComponentInChildren<DecontaminationItemInfo>();

        if (occupiedItemInfo == null || occupiedItemDragDrop == null
        || Vector3.Distance(draggedToolInfo.transform.position, workplate.transform.position) > occupiedItemDragDrop.getSnapDistance()
        || occupiedItemInfo.currentBagLevels >= occupiedItemInfo.maxBagLevels) return;

        DragAndDrop draggedToolDragDrop = draggedToolInfo.GetComponentInParent<DragAndDrop>();

        if (draggedToolDragDrop == null) return;

        CreateBagOverlay(occupiedItemInfo, draggedToolInfo);
        occupiedItemInfo.currentBagLevels++;
    }

    private void CreateBagOverlay(DecontaminationItemInfo occupiedItemInfo, DecontaminationToolInfo draggedToolInfo)
    {
        if (occupiedItemInfo == null || draggedToolInfo == null) return;

        // Count existing overlays that follow the naming convention
        const string overlayPrefix = "BagOverlay_";
        int existingCount = 0;
        foreach (Transform child in occupiedItemInfo.transform)
        {
            if (child.name.StartsWith(overlayPrefix))
                existingCount++;
        }

        int overlayIndex = existingCount + 1;
        string overlayName = overlayPrefix + overlayIndex;

        GameObject overlayGO = new GameObject(overlayName, typeof(RectTransform));
        overlayGO.transform.SetParent(occupiedItemInfo.transform, false);

        // ensure it's rendered on top among siblings
        overlayGO.transform.SetAsLastSibling();

        RawImage ri = overlayGO.AddComponent<RawImage>();
        ri.raycastTarget = false;

        // Assign texture from the sprite (if available)
        ri.texture = draggedToolInfo.imageDrag != null ? draggedToolInfo.imageDrag.texture : null;
        ri.color = Color.white;

        // Try to size the overlay so it fully covers the parent. Each new overlay is larger than the previous one
        RectTransform parentRt = occupiedItemInfo.GetComponent<RectTransform>();
        RectTransform overlayRt = overlayGO.GetComponent<RectTransform>();

        float parentW = 0f;
        float parentH = 0f;

        if (parentRt != null)
        {
            parentW = parentRt.rect.width;
            parentH = parentRt.rect.height;

            if (parentW <= 0f) parentW = parentRt.sizeDelta.x;
            if (parentH <= 0f) parentH = parentRt.sizeDelta.y;
        }

        // final fallback sizes
        if (parentW <= 0f) parentW = 100f;
        if (parentH <= 0f) parentH = 100f;

        // Base multiplier ensures the overlay at least covers the parent (1.0f).
        // Each existing overlay increases the multiplier so newer overlays are larger.
        float multiplier = (1 + firstBagSize) + (existingCount * bagMulitplier);

        if (overlayRt != null)
        {
            overlayRt.anchorMin = new Vector2(0.5f, 0.5f);
            overlayRt.anchorMax = new Vector2(0.5f, 0.5f);
            overlayRt.pivot = new Vector2(0.5f, 0.5f);
            overlayRt.anchoredPosition = Vector2.zero;

            overlayRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentW * multiplier);
            overlayRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, parentH * multiplier);
        }
        else
        {
            // Non-UI fallback: scale slightly larger and center
            overlayGO.transform.localPosition = Vector3.zero;
            overlayGO.transform.localScale = Vector3.one * multiplier;
        }

        // Make sure overlay renders on top by setting sibling index last
        overlayGO.transform.SetAsLastSibling();
    }

    private void OnEnable()
    {
        EventManager.OnItemDragStart.AddListener((item) =>
        {
            DecontaminationToolInfo decontaminationToolInfo = item.gameObject.GetComponentInChildren<DecontaminationToolInfo>();
            if (decontaminationToolInfo == null || decontaminationToolInfo.toolType != ToolTypes.Bag) return;
           
            draggedToolInfo = decontaminationToolInfo;
            StartUseBag();
        });

        EventManager.OnItemBeforeDragEnd.AddListener((item) =>
        {
            if (draggedToolInfo == null || draggedToolInfo.toolType != ToolTypes.Bag) return;
            draggedToolInfo.GetComponent<RawImage>().texture = draggedToolInfo.imageOriginal.texture;
            Destroy(bagDispenserPlaceholder.gameObject);
            UseBag();
            draggedToolInfo = null;
        });
    }

    private void OnDisable()
    {
        EventManager.OnItemDragStart.RemoveAllListeners();
        EventManager.OnItemDragEnd.RemoveAllListeners();
        EventManager.OnItemBeforeDragEnd.RemoveAllListeners();
    }
}
