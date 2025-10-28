using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AlcoholManager : MonoBehaviour
{
    [SerializeField] private Texture2D[] splashTextures;
    [SerializeField] private Color colorMin;
    [SerializeField] private Color colorMax;
    [SerializeField] private float scaleMin;
    [SerializeField] private float scaleMax;
    [SerializeField] private float splashRadius = 50f;
    [SerializeField] private float tickInterval = 1f; // Time between ticks in seconds
    [SerializeField] private DropTarget workplate;

    private DecontaminationInfo draggedTool;
    private Coroutine alcoholCoroutine;

    private void Update()
    {
        UseAlcohol();
    }

    private void UseAlcohol()
    {
        if (workplate == null || !workplate.IsOccupied() || draggedTool == null || draggedTool.toolType != ToolTypes.Alcohol)
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

        if (occupiedItemDI == null
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
            for (int i = targetItem.contaminationSpots.Count - 1; i >= 0; i--)
            {
                ContaminationSpot contaminationSpot = targetItem.contaminationSpots[i];

                float distToSpot = Vector3.Distance(targetItem.transform.TransformPoint(contaminationSpot.pos), draggedTool.transform.position);

                if (distToSpot > splashRadius || contaminationSpot.isSoaked) continue;

                contaminationSpot.isSoaked = true;
                targetItem.contaminationSpots[i] = contaminationSpot;

                GameObject go = new("AlcoholSplash" + (i + 1), typeof(CanvasRenderer), typeof(RawImage));
                go.transform.SetParent(targetItem.transform, false);

                float intensity = Random.Range(colorMin.a, colorMax.a);
                Color color = Color.Lerp(colorMin, colorMax, intensity);

                RawImage image = go.GetComponent<RawImage>();
                image.texture = splashTextures[Random.Range(0, splashTextures.Length - 1)];
                image.color = color;

                float scale = Random.Range(scaleMin, scaleMax);

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(scale, scale);
                rt.localPosition = contaminationSpot.pos;
                rt.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));

                targetItem.contaminationSpots.Add(new ContaminationSpot
                {
                    pos = contaminationSpot.pos,
                    image = image,
                    intensity = intensity,
                    visible = true,
                    needsAlcohol = false,
                    isSoaked = true
                });
            }

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
