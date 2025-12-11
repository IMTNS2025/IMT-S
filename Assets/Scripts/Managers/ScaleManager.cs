using UnityEngine;

public class ScaleManager : MonoBehaviour
{
    private RectTransform objectToDrag;
    private DecontaminationItemInfo objectToDragItemInfo;
    private DecontaminationToolInfo objectToDragToolInfo;

    private void OnEnable()
    {
        EventManager.OnItemDragStart.AddListener((item) =>
        {
            ItemInfoDragStart(item);
            ToolInfoDragStart(item);
        });

        EventManager.OnItemDragEnd.AddListener((item) =>
        {
            objectToDrag = null;
            objectToDragItemInfo = null;
            objectToDragItemInfo = null;
        });

        EventManager.OnDragSuccessed.AddListener(gameObject =>
        {
            ItemInfoDragSucceeded();
            ToolInfoDragSucceeded();
        });

        EventManager.OnDragFailed.AddListener(gameObject =>
        {
            ItemInfoDragFailed();
            ToolInfoDragFailed();
        });
    }

    private void ItemInfoDragSucceeded()
    {
        if (objectToDrag == null || objectToDragItemInfo == null) return;
        objectToDrag.sizeDelta = objectToDragItemInfo.originalSize * objectToDragItemInfo.scaleWorkplate;
        CheckScaleBag(objectToDragItemInfo.scaleWorkplate);
    }

    private void ToolInfoDragSucceeded()
    {
        if (objectToDrag == null || objectToDragToolInfo == null) return;
        objectToDrag.sizeDelta = objectToDragToolInfo.originalSize * objectToDragToolInfo.scaleWorkplate;
    }

    private void ItemInfoDragFailed()
    {
        if (objectToDrag == null || objectToDragItemInfo == null) return;
        objectToDrag.sizeDelta = objectToDragItemInfo.originalSize * objectToDragItemInfo.scaleContainer;
        CheckScaleBag(objectToDragItemInfo.scaleContainer);
    }

    private void ToolInfoDragFailed()
    {
        if (objectToDrag == null || objectToDragToolInfo == null) return;
        objectToDrag.sizeDelta = objectToDragToolInfo.originalSize * objectToDragToolInfo.scaleContainer;
    }

    private void ItemInfoDragStart(DragAndDrop item)
    {
        objectToDrag = item.GetComponent<RectTransform>();
        objectToDragItemInfo = item.GetComponentInChildren<DecontaminationItemInfo>();
        if (objectToDragItemInfo == null || objectToDrag == null) return;
        objectToDrag.sizeDelta = objectToDragItemInfo.originalSize * objectToDragItemInfo.scaleDragged;
        CheckScaleBag(objectToDragItemInfo.scaleDragged);
    }

    private void CheckScaleBag(float scale)
    {
        if (objectToDragItemInfo == null || objectToDrag == null) return;

        int levels = objectToDragItemInfo.currentBagLevels;
        if (levels <= 0) return;

        int childCount = objectToDragItemInfo.transform.childCount;
        if (childCount < levels) return;

        // Calculate the start index of the bag children (assumes the bag children are the last `levels` children)
        int startIndex = childCount - levels;

        // Iterate from innermost (smallest) to outermost (largest)
        for (int j = 0; j < levels; j++)
        {
            int childIndex = startIndex + j;
            var child = objectToDragItemInfo.transform.GetChild(childIndex);
            if (child == null) continue;

            RectTransform rectTransformChild = child.GetComponent<RectTransform>();
            if (rectTransformChild == null) continue;

            // Multiplier increases for outer bags so inner bag is smaller
            float multiplier = 1 + objectToDragItemInfo.firstBagSizeMulitplier + (j * objectToDragItemInfo.otherBagsSizeMulitplier);

            // Keep bag size relative to the parent item's current size
            rectTransformChild.sizeDelta = objectToDrag.sizeDelta * multiplier;

            // Ensure correct draw order: innermost (j=0) should be behind outermost (higher j)
            rectTransformChild.SetSiblingIndex(childIndex);
        }
    }

    private void ToolInfoDragStart(DragAndDrop item)
    {
        objectToDrag = item.GetComponent<RectTransform>();
        objectToDragToolInfo = item.GetComponentInChildren<DecontaminationToolInfo>();
        if (objectToDragToolInfo == null || objectToDrag == null) return;
        objectToDrag.sizeDelta = objectToDragToolInfo.originalSize * objectToDragToolInfo.scaleDragged;
    }


    private void OnDisable()
    {
        EventManager.OnItemDragStart.RemoveAllListeners();
        EventManager.OnItemDragEnd.RemoveAllListeners();
        EventManager.OnDragSuccessed.RemoveAllListeners();
        EventManager.OnDragSuccessed.RemoveAllListeners();
    }
}
