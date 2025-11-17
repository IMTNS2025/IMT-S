using UnityEngine;
using UnityEngine.UI;

public class DecontaminationManager : MonoBehaviour
{
    [SerializeField] private DecontaminationItemSO[] items;
    [SerializeField] private GameObject[] itemContainers;
    [SerializeField] private DecontaminationToolSO[] tools;
    [SerializeField] private GameObject[] toolContainers;
    [SerializeField] private ContaminationTypeSO[] contaminations;

    void Start()
    {
        SetUpItems();
        SetUpTools();
    }

    private void SetUpTools()
    {
        for (int i = 0; i < toolContainers.Length; i++)
        {
            if (toolContainers[i].transform.childCount > 0)
            {
                Transform item = toolContainers[i].transform.GetChild(0);

                if (item == null)
                {
                    Debug.LogWarning($"ContaminationTool array does not contain contamination at index {i}.");
                    continue;
                }
                RawImage image = item.GetComponent<RawImage>();
                if (image == null)
                {
                    Debug.LogWarning($"ContaminationTool array does not contain image at index {i}.");
                    continue;
                }

                image.texture = tools[i].originalImage.texture;
                DecontaminationToolInfo decontaminationToolInfo = item.GetComponent<DecontaminationToolInfo>();
                if (decontaminationToolInfo == null)
                {
                    Debug.LogWarning($"ContaminationTool array does not contain decontaminationToolInfo at index {i}.");
                    continue;
                }

                decontaminationToolInfo.toolType = tools[i].toolType;
                decontaminationToolInfo.imageDrag = tools[i].imageDrag;
                decontaminationToolInfo.imageOriginal = tools[i].originalImage;
                RectTransform rt = toolContainers[i].transform.GetComponentInParent<RectTransform>();
                decontaminationToolInfo.widthOriginal = rt.rect.width;
                decontaminationToolInfo.heightOriginal = rt.rect.height;
            }
        }
    }

    private void SetUpItems()
    {
        for (int i = 0; i < itemContainers.Length; i++)
        {
            if (itemContainers[i].transform.childCount > 0)
            {
                GameObject item = itemContainers[i].transform.GetChild(0).gameObject;
                RawImage image = item.GetComponent<RawImage>();

                if (item != null && image != null)
                {
                    image.texture = items[i].originalImage.texture;
                    DecontaminationItemInfo decontaminationInfo = item.GetComponent<DecontaminationItemInfo>();
                    if (contaminations.Length - 1 < i)
                    {
                        Debug.LogWarning($"ContaminationItem array does not contain contamination at index {i}.");
                        continue;
                    }
                    contaminations[i].spawnContamination(decontaminationInfo);
                    decontaminationInfo.maxBagLevels = items[i].maxBagLevels;
                }
            }
            else
            {
                Debug.LogWarning($"Item container at index {i} has no child object.");
            }
        }
    }
}
