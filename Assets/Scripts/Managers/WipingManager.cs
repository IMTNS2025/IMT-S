using UnityEngine;

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
    private DecontaminationInfo draggedTool;
    private Vector2 lastDirection;

    private void Update()
    {
        UseWipes();
    }

    private void UseWipes()
    {
        if (workplate == null || !workplate.IsOccupied() || draggedTool == null || draggedTool.toolType != ToolTypes.Wipes) return;

        DragAndDrop occupiedItemDaD = workplate.GetObjectOccupied();
        DecontaminationInfo occupiedItemDI = occupiedItemDaD.GetComponentInChildren<DecontaminationInfo>();

        if (occupiedItemDI == null
        || Vector3.Distance(draggedTool.transform.position, workplate.transform.position) > occupiedItemDaD.getSnapDistance()
        || occupiedItemDI.contaminationSpots.Count == 0) return;

        DragAndDrop draggedToolDaD = draggedTool.GetComponentInParent<DragAndDrop>();

        Vector2 currentDirection = (draggedTool.transform.position - draggedToolDaD.getLastPosition()).normalized;
        float dirChange = Vector2.Dot(currentDirection, lastDirection); // 1 = same direction, -1 = opposite
        lastDirection = currentDirection;

        if (dirChange >= (1f - wipeSensitivity)) return; // movement reversed enough

        for (int i = occupiedItemDI.contaminationSpots.Count - 1; i >= 0; i--)
        {
            ContaminationSpot contaminationSpot = occupiedItemDI.contaminationSpots[i];

            if (contaminationSpot.needsAlcohol && !contaminationSpot.isSoaked) continue;

            float distToSpot = Vector3.Distance(occupiedItemDI.transform.TransformPoint(contaminationSpot.pos), draggedTool.transform.position);

            if (distToSpot > wipeBrushRadius) continue;

            contaminationSpot.intensity -= wipeStrength;
            Color color = contaminationSpot.image.color;
            color.a = contaminationSpot.intensity;
            contaminationSpot.image.color = color;
            occupiedItemDI.contaminationSpots[i] = contaminationSpot;

            if (contaminationSpot.intensity > wipeLowerThrashhold) continue;

            Destroy(occupiedItemDI.contaminationSpots[i].image.gameObject);
            occupiedItemDI.contaminationSpots.RemoveAt(i);
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
            draggedTool = null;
        });
    }

    private void OnDisable()
    {
        EventManager.OnItemDragStart.RemoveAllListeners();

        EventManager.OnItemDragEnd.RemoveAllListeners();
    }
}
