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
            GameObject item = itemContainers[i].transform.GetChild(0).gameObject;
            Image image = item.GetComponent<Image>();
            image.sprite = items[i].image;
            image.color = items[i].dirty;
            DecontaminationInfo decontaminationInfo = item.GetComponent<DecontaminationInfo>();
            decontaminationInfo.acceptedTypes = items[i].acceptedTypes;
            decontaminationInfo.clean = items[i].clean;
        }

        for (int i = 0; i < toolContainers.Length; i++)
        {
            Transform item = toolContainers[i].transform.GetChild(0);
            item.GetComponent<Image>().sprite = tools[i].image;
            DecontaminationInfo decontaminationInfo = item.GetComponent<DecontaminationInfo>();
            decontaminationInfo.toolType = tools[i].toolType;
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
                dragAndDrop.GetComponentInChildren<Image>().color = decontaminationInfo.clean;
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
}
