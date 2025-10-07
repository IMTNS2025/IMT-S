using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DecontaminationManager : MonoBehaviour
{
    [SerializeField] private DecontaminationItemSO[] items;
    [SerializeField] private GameObject[] itemContainers;
    [SerializeField] private DecontaminationToolSO[] tools;
    [SerializeField] private GameObject[] toolContainers;
    [SerializeField] private DropTarget workplate;

    private DecontaminationInfo draggedTool;

    void Start()
    {
        for (int i = 0; i < itemContainers.Length; i++)
        {
            if (itemContainers[i].transform.childCount > 0)
            {
                GameObject item = itemContainers[i].transform.GetChild(0).gameObject;
                RawImage image = item.GetComponent<RawImage>();

                if (item != null && image != null)
                {
                    image.texture = items[i].image;
                    image.color = items[i].dirty;
                    DecontaminationInfo decontaminationInfo = item.GetComponent<DecontaminationInfo>();
                    decontaminationInfo.acceptedTypes = items[i].acceptedTypes;
                    decontaminationInfo.clean = items[i].clean;
                }
            }
            else
            {
                Debug.LogWarning($"Item container at index {i} has no child object.");
            }
        }

        for (int i = 0; i < toolContainers.Length; i++)
        {
            if (toolContainers[i].transform.childCount > 0)
            {
                Transform item = toolContainers[i].transform.GetChild(0);
                item.GetComponent<RawImage>().texture = tools[i].image;
                DecontaminationInfo decontaminationInfo = item.GetComponent<DecontaminationInfo>();
                decontaminationInfo.toolType = tools[i].toolType;
            }
        }
    }

    private void Update()
    {
        if (draggedTool != null && workplate.IsOccupied())
        {
            DragAndDrop dragAndDrop = workplate.GetObjectOccupied();
            DecontaminationInfo decontaminationInfo = dragAndDrop.GetComponentInChildren<DecontaminationInfo>();
            if (decontaminationInfo != null && decontaminationInfo.acceptedTypes.Contains(draggedTool.toolType)
                && Vector3.Distance(draggedTool.transform.position, workplate.transform.position) <= dragAndDrop.getSnapDistance())
            {
                dragAndDrop.GetComponentInChildren<RawImage>().color = decontaminationInfo.clean;
            }
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
