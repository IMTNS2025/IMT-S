using System.Collections.Generic;
using UnityEngine;

public class FirstLayerProtectionManager : MonoBehaviour
{
    [SerializeField] private Transform itemContainer;
    private Dictionary<string, string> itemInfos = new();
    private int actualOrder = 1;

    private void RecordPerformance()
    {
        if (FeedbackSingleton.Instance == null) return;

        if (itemContainer != null)
        {
            foreach (PutInOrder putInOrder in itemContainer.GetComponentsInChildren<PutInOrder>())
            {
                if (itemInfos.ContainsKey(putInOrder.gameObject.name)) continue;

                itemInfos.Add(putInOrder.gameObject.name, putInOrder.gameObject.name + " was not put on.");
            }
        }

        FeedbackSingleton.Instance.firstLayerProtectionPerformance = itemInfos;
    }

    private void OnEnable()
    {
        EventManager.OnBeforeSceneExit.AddListener(() =>
        {
            RecordPerformance();
        });

        EventManager.OnDragFailed.AddListener(gameObject =>
        {
            int supposedOrder = gameObject.GetComponent<PutInOrder>().order;
            string itemName = gameObject.name;
            if (itemInfos.ContainsKey(itemName))
            {
                itemInfos.Remove(itemName);
                actualOrder--;
            }
        });

        EventManager.OnDragSuccessed.AddListener(gameObject =>
        {
            int supposedOrder = gameObject.GetComponent<PutInOrder>().order;
            string itemName = gameObject.name;
            if (actualOrder == supposedOrder)
            {
                itemInfos.Add(itemName, itemName + " was put in the right order.");
            } else
            {
                itemInfos.Add(itemName, itemName + " was put as the " + actualOrder + ". clothing item. It should have been put as the " + supposedOrder + ". item.");
            }
            actualOrder++;
        });
    }

    private void OnDisable()
    {
        EventManager.OnBeforeSceneExit.RemoveAllListeners();
    }
}
