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

        EventManager.OnDragSuccessed.AddListener(() =>
        {
            ItemInfoDragSucceeded();
            ToolInfoDragSucceeded();
        });

        EventManager.OnDragFailed.AddListener(() =>
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
        for (int i = 1; i <= objectToDragItemInfo.currentBagLevels; i++)
        {
            float multiplier = 1 + objectToDragItemInfo.firstBagSizeMulitplier + (i * objectToDragItemInfo.otherBagsSizeMulitplier);
            RectTransform rectTransformChild = objectToDragItemInfo.transform.GetChild(objectToDragItemInfo.transform.childCount - i).GetComponent<RectTransform>();
            rectTransformChild.sizeDelta = objectToDragItemInfo.originalSize * scale * multiplier;
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
