using UnityEngine;
using UnityEngine.Rendering.Universal;

public class UVManager : MonoBehaviour
{
    [SerializeField] private DropTarget workplate;
    [SerializeField] private Light2D uvLight;

    private DecontaminationToolInfo draggedToolInfo;

    private void Update()
    {
        UseLamp();
    }

    private void UseLamp()
    {
        if (draggedToolInfo != null && draggedToolInfo.toolType == ToolTypes.Lamp)
        {
            uvLight.transform.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, uvLight.transform.position.z);
            uvLight.enabled = true;

            if (workplate == null || !workplate.IsOccupied())
            {
                return;
            }

            DragAndDrop occupiedItemDragDrop = workplate.GetObjectOccupied();
            DecontaminationItemInfo occupiedItemInfo = occupiedItemDragDrop.GetComponentInChildren<DecontaminationItemInfo>();

            if (occupiedItemInfo == null || occupiedItemInfo.contaminationSpots.Count == 0)
            {
                return;
            }

            float outer = uvLight.pointLightOuterRadius;
            float inner = uvLight.pointLightInnerRadius;

            for (int i = occupiedItemInfo.contaminationSpots.Count - 1; i >= 0; i--)
            {
                ContaminationSpot contaminationSpot = occupiedItemInfo.contaminationSpots[i];

                if (contaminationSpot.visible) continue;

                Vector3 spotWorldPos = occupiedItemInfo.transform.TransformPoint(contaminationSpot.pos);
                float distToSpot = Vector3.Distance(spotWorldPos, uvLight.transform.position);

                // Calculate alpha:
                // - dist >= outer -> alpha = 0
                // - dist <= inner -> alpha = contaminationSpot.intensity
                // - between -> linear interpolation from 0 (at outer) to intensity (at inner)
                float alpha = 0f;

                if (distToSpot <= inner || outer <= inner)
                {
                    // If the spot is within inner radius (or inner/outer invalid), use full intensity
                    alpha = Mathf.Clamp01(contaminationSpot.intensity);
                }
                else if (distToSpot < outer)
                {
                    float t = (outer - distToSpot) / (outer - inner); // 0 at outer, 1 at inner
                    alpha = Mathf.Clamp01(contaminationSpot.intensity * t);
                }
                else
                {
                    alpha = 0f;
                }

                Color color = contaminationSpot.image.color;
                color.a = alpha;
                contaminationSpot.image.color = color;
                occupiedItemInfo.contaminationSpots[i] = contaminationSpot;
            }
        }
        else
        {
            uvLight.enabled = false;
        }
    }

    private void OnEnable()
    {
        EventManager.OnItemDragStart.AddListener((item) =>
        {
            DecontaminationToolInfo decontaminationToolInfo = item.gameObject.GetComponentInChildren<DecontaminationToolInfo>();
            if (decontaminationToolInfo != null)
            {
                draggedToolInfo = decontaminationToolInfo;
            }
        });

        EventManager.OnItemDragEnd.AddListener((item) =>
        {
            draggedToolInfo = null;
            if (workplate == null || !workplate.IsOccupied())
            {
                return;
            }

            DragAndDrop occupiedItemDragDrop = workplate.GetObjectOccupied();
            DecontaminationItemInfo occupiedItemInfo = occupiedItemDragDrop.GetComponentInChildren<DecontaminationItemInfo>();

            if (occupiedItemInfo == null || occupiedItemInfo.contaminationSpots.Count == 0)
            {
                return;
            }

            for (int i = occupiedItemInfo.contaminationSpots.Count - 1; i >= 0; i--)
            {
                ContaminationSpot contaminationSpot = occupiedItemInfo.contaminationSpots[i];

                if(contaminationSpot.visible) continue;

                Color color = contaminationSpot.image.color;
                color.a = 0;
                contaminationSpot.image.color = color;
                occupiedItemInfo.contaminationSpots[i] = contaminationSpot;
            }
        });
    }

    private void OnDisable()
    {
        EventManager.OnItemDragStart.RemoveAllListeners();
        EventManager.OnItemDragEnd.RemoveAllListeners();
    }
}
