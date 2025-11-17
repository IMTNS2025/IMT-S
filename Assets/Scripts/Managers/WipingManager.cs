using UnityEngine;
using UnityEngine.UI;

public class WipingManager : MonoBehaviour
{
    [Tooltip("Sensitivity of wiping motion. 0 = you need to rub really hard. 1 = you dont need to rub at all.")]
    [SerializeField][Range(0, 1)] private float wipeSensitivity = 0.5f;
    [Tooltip("Radius of brush. Low value = you need to be really close to the dirt spot. High value = you can be further away.")]
    [SerializeField] private float wipeBrushRadius = 50f;
    [Tooltip("Strength of brush. Low value = you need to wipe for a long time. High value = you can wipe for a short amount of time.")]
    [SerializeField][Range(0, 1)] private float wipeStrength = 0.1f;
    [Tooltip("Threshhold when the dirt dissapears. Since the dirt becomes more transparent, it becomes hard to see.")]
    [SerializeField] private float wipeLowerThrashhold = 0.1f;
    [SerializeField] private DropTarget workplate;
    private DecontaminationToolInfo draggedToolInfo;
    private Vector2 lastDirection;
    private GameObject wipesDispenserPlaceholder;

    private void StartUseWipes()
    {
        wipesDispenserPlaceholder = new GameObject("BagDispenser", typeof(RectTransform));
        wipesDispenserPlaceholder.transform.SetParent(draggedToolInfo.transform.parent, false);
        RectTransform rt = wipesDispenserPlaceholder.GetComponent<RectTransform>();
        rt.sizeDelta = draggedToolInfo.originalSize * draggedToolInfo.scaleContainer;
        RawImage ri = wipesDispenserPlaceholder.AddComponent<RawImage>();
        ri.raycastTarget = false;

        // Assign texture from the sprite (if available)
        ri.texture = draggedToolInfo.imageOriginal.texture;
        ri.color = Color.white;

        draggedToolInfo.GetComponent<RawImage>().texture = draggedToolInfo.imageDrag.texture;
    }

    private void Update()
    {
        UseWipes();
    }

    private void UseWipes() 
    {
        if (workplate == null || !workplate.IsOccupied() || draggedToolInfo == null || draggedToolInfo.toolType != ToolTypes.Wipes) return;

        DragAndDrop occupiedItemDragDrop = workplate.GetObjectOccupied();
        DecontaminationItemInfo occupiedItemInfo = occupiedItemDragDrop.GetComponentInChildren<DecontaminationItemInfo>();

        if (occupiedItemInfo == null || occupiedItemDragDrop == null
        || Vector3.Distance(draggedToolInfo.transform.position, workplate.transform.position) > occupiedItemDragDrop.getSnapDistance()
        || occupiedItemInfo.contaminationSpots.Count == 0 || occupiedItemInfo.currentBagLevels > 0) return;

        DragAndDrop draggedToolDragDrop = draggedToolInfo.GetComponentInParent<DragAndDrop>();

        if (draggedToolDragDrop == null) return;

        Vector2 currentDirection = (draggedToolInfo.transform.position - draggedToolDragDrop.getLastPosition()).normalized;
        float dirChange = Vector2.Dot(currentDirection, lastDirection); // 1 = same direction, -1 = opposite
        lastDirection = currentDirection;

        if (dirChange >= (1f - wipeSensitivity)) return; // movement reversed enough

        for (int i = occupiedItemInfo.contaminationSpots.Count - 1; i >= 0; i--)
        {
            ContaminationSpot contaminationSpot = occupiedItemInfo.contaminationSpots[i];

            if (contaminationSpot.needsAlcohol && !contaminationSpot.isSoaked) continue;

            float distToSpot = Vector3.Distance(occupiedItemInfo.transform.TransformPoint(contaminationSpot.pos), draggedToolInfo.transform.position);

            if (distToSpot > wipeBrushRadius) continue;

            contaminationSpot.intensity -= wipeStrength;
            if (contaminationSpot.visible) {
                Color color = contaminationSpot.image.color;
                color.a = contaminationSpot.intensity;
                contaminationSpot.image.color = color;
            }
            occupiedItemInfo.contaminationSpots[i] = contaminationSpot;

            if (contaminationSpot.intensity > wipeLowerThrashhold) continue;

            Destroy(occupiedItemInfo.contaminationSpots[i].image.gameObject);
            occupiedItemInfo.contaminationSpots.RemoveAt(i);
        }
    }

    private void OnEnable()
    {
        EventManager.OnItemDragStart.AddListener((item) =>
        {
            DecontaminationToolInfo decontaminationToolInfo = item.gameObject.GetComponentInChildren<DecontaminationToolInfo>();
            if (decontaminationToolInfo == null || decontaminationToolInfo.toolType != ToolTypes.Wipes) return;
            draggedToolInfo = decontaminationToolInfo;
            StartUseWipes();
        });

        EventManager.OnItemDragEnd.AddListener((item) =>
        {
            if (draggedToolInfo == null || draggedToolInfo.toolType != ToolTypes.Wipes) return;
            draggedToolInfo.GetComponent<RawImage>().texture = draggedToolInfo.imageOriginal.texture;
            draggedToolInfo = null;
            Destroy(wipesDispenserPlaceholder);
        });
    }

    private void OnDisable()
    {
        EventManager.OnItemDragStart.RemoveAllListeners();

        EventManager.OnItemDragEnd.RemoveAllListeners();
    }
}
