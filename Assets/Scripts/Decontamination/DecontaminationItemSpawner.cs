using UnityEngine;
using UnityEngine.UI;

public class DecontaminationItemSpawner : MonoBehaviour
{
    //TODOALEX add random offset
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
            if (toolContainers[i].transform.childCount <= 0) continue;            
                
            Transform item = toolContainers[i].transform.GetChild(0);
            RawImage image = item.GetComponent<RawImage>();
            DecontaminationToolInfo decontaminationToolInfo = item?.GetComponent<DecontaminationToolInfo>();
            RectTransform itemRect = toolContainers[i].transform.GetComponentInParent<RectTransform>();

            if (item == null && image == null && itemRect == null && decontaminationToolInfo == null) continue;

            image.texture = tools[i].originalImage.texture;

            decontaminationToolInfo.toolType = tools[i].toolType;
            decontaminationToolInfo.imageDrag = tools[i].imageDrag;
            decontaminationToolInfo.scaleContainer = tools[i].scaleContainer;
            decontaminationToolInfo.scaleDragged = tools[i].scaleDragged;
            decontaminationToolInfo.scaleWorkplate = tools[i].scaleWorkplate;
            decontaminationToolInfo.imageOriginal = tools[i].originalImage;
            decontaminationToolInfo.originalSize = new Vector2(image.texture.width, image.texture.height);

            itemRect.sizeDelta = decontaminationToolInfo.originalSize * decontaminationToolInfo.scaleContainer;
        }
    }

    private void SetUpItems()
    {
        Shuffle(itemContainers);
        for (int i = 0; i < items.Length; i++)
        {   
            if (itemContainers[i].transform.childCount > 0)
            {
                GameObject item = itemContainers[i].transform.GetChild(0).gameObject;
                RectTransform itemRect = itemContainers[i].GetComponent<RectTransform>();
                RawImage image = item.GetComponent<RawImage>();
                DecontaminationItemInfo decontaminationInfo = item.GetComponent<DecontaminationItemInfo>();

                if (item == null && image == null && itemRect == null && decontaminationInfo == null) continue;
                
                image.texture = items[i].originalImage.texture;                    

                decontaminationInfo.maxBagLevels = items[i].maxBagLevels;
                decontaminationInfo.scaleContainer = items[i].scaleContainer;
                decontaminationInfo.scaleDragged = items[i].scaleDragged;
                decontaminationInfo.scaleWorkplate = items[i].scaleWorkplate;
                decontaminationInfo.originalSize = new Vector2(image.texture.width, image.texture.height);
                decontaminationInfo.firstBagSizeMulitplier = items[i].firstBagSizeMulitplier;
                decontaminationInfo.otherBagsSizeMulitplier = items[i].otherBagsSizeMulitplier;

                itemRect.sizeDelta = decontaminationInfo.originalSize * decontaminationInfo.scaleContainer;
                  
                if (contaminations.Length - 1 < i)
                {
                    Debug.LogWarning($"ContaminationItem array does not contain contamination at index {i}.");
                    continue;
                }
                contaminations[i].spawnContamination(decontaminationInfo);                
            }
            else
            {
                Debug.LogWarning($"Item container at index {i} has no child object.");
            }
        }
    }

    private void Shuffle(GameObject[] array)
    {
        if (array == null || array.Length <= 1) return;

        // Fisher–Yates shuffle using UnityEngine.Random
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1); // inclusive lower, exclusive upper; i+1 makes it inclusive of i
            if (i == j) continue;
            GameObject tmp = array[i];
            array[i] = array[j];
            array[j] = tmp;
        }
    }
}
