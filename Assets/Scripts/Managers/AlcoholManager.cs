using System.Linq;
using UnityEngine;
using System.Collections;

public class AlcoholManager : MonoBehaviour
{
    [SerializeField] private DropTarget workplate;
    [SerializeField] private float tickInterval = 1f; // Time between ticks in seconds

    private DecontaminationInfo draggedTool;
    private Coroutine alcoholCoroutine;

    private void Update()
    {
        UseAlcohol();
    }

    private void UseAlcohol()
    {
        if (workplate == null || !workplate.IsOccupied() || draggedTool == null || draggedTool.toolType != ToolTypes.Bleach)
        {
            if (alcoholCoroutine != null)
            {
                StopCoroutine(alcoholCoroutine);
                alcoholCoroutine = null;
            }
            return;
        }

        DragAndDrop occupiedItemDaD = workplate.GetObjectOccupied();
        DecontaminationInfo occupiedItemDI = occupiedItemDaD.GetComponentInChildren<DecontaminationInfo>();

        if (occupiedItemDI == null || !occupiedItemDI.acceptedTypes.Contains(draggedTool.toolType)
        || Vector3.Distance(draggedTool.transform.position, workplate.transform.position) > occupiedItemDaD.getSnapDistance()
        || occupiedItemDI.contaminationSpots.Count == 0)
        {
            if (alcoholCoroutine != null)
            {
                StopCoroutine(alcoholCoroutine);
                alcoholCoroutine = null;
            }
            return;
        }

        // Start coroutine if it's not already running
        if (alcoholCoroutine == null)
        {
            alcoholCoroutine = StartCoroutine(AlcoholTick(occupiedItemDI));
        }
    }

    private IEnumerator AlcoholTick(DecontaminationInfo targetItem)
    {
        while (true)
        {
            // Your tick logic here
            Debug.Log("Alcohol tick on: " + targetItem.name);

            yield return new WaitForSeconds(tickInterval);
        }
    }

    private void OnEnable()
    {
        EventManager.OnItemDragStart.AddListener((item) =>
        {
            DecontaminationInfo decontaminationInfo = item.gameObject.GetComponentInChildren<DecontaminationInfo>();
            if (decontaminationInfo != null)
            {
                draggedTool = decontaminationInfo;
            }
        });

        EventManager.OnItemDragEnd.AddListener((item) =>
        {
            if (alcoholCoroutine != null)
            {
                StopCoroutine(alcoholCoroutine);
                alcoholCoroutine = null;
            }
            draggedTool = null;
        });
    }

    private void OnDisable()
    {                                                                       
        if (alcoholCoroutine != null)
        {
            StopCoroutine(alcoholCoroutine);
            alcoholCoroutine = null;
        }
        EventManager.OnItemDragStart.RemoveAllListeners();
        EventManager.OnItemDragEnd.RemoveAllListeners();
    }
}
