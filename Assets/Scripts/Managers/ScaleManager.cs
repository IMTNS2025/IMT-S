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
