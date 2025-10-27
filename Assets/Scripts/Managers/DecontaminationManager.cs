using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DecontaminationManager : MonoBehaviour
{
    [SerializeField] private DecontaminationItemSO[] items;
    [SerializeField] private GameObject[] itemContainers;
    [SerializeField] private DecontaminationToolSO[] tools;
    [SerializeField] private GameObject[] toolContainers;
    [SerializeField] private ContaminationTypeSO[] contaminations;

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
                    //image.color = items[i].dirty;
                    image.texture = items[i].imageOnCharacter;
                    DecontaminationInfo decontaminationInfo = item.GetComponent<DecontaminationInfo>();
                    decontaminationInfo.acceptedTypes = items[i].acceptedTypes;
                    //decontaminationInfo.clean = items[i].clean;
                    if (contaminations.Length - 1 < i)
                    {
                        Debug.LogWarning($"Contamination array does not contain contamination at index {i}.");
                        continue;
                    }
                    contaminations[i].spawnContamination(decontaminationInfo);
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
                item.GetComponent<RawImage>().texture = tools[i].imageOnCharacter;
                DecontaminationInfo decontaminationInfo = item.GetComponent<DecontaminationInfo>();
                decontaminationInfo.toolType = tools[i].toolType;
            }
        }
    }
}
