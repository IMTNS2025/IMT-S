using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class LockerFeedbackManager : MonoBehaviour
{
    [SerializeField] private PocketsSysten pocketsSystem;

    private Dictionary<string, string> itemInfos = new ();

    private void RecordPerformance ()
    {
        if (FeedbackSingleton.Instance == null) return;

        foreach (GameObject item in pocketsSystem.getItemInPocket())
        {
            if (item == null) continue;

            string itemName = item.name;
            if (itemInfos.ContainsKey(itemName)) continue;

            GoesToLockerSpotType supposedAction = item.GetComponent<GoesToLockerSpot>().goesToLockerSpotType;
            GoesToLockerSpotType actualAction = GoesToLockerSpotType.Inventory;

            if (supposedAction == actualAction)
            {
                itemInfos.Add(itemName, "Item " + itemName + " was correctly placed in " + actualAction);
            }
            else
            {
                itemInfos.Add(itemName, "Item " + itemName + " was incorrectly placed in " + actualAction + ". It should have been placed in " + supposedAction + ".");
            }
        }

        FeedbackSingleton.Instance.lockerPerformance = itemInfos;
    }

    private void OnEnable()
    {
        EventManager.OnBeforeSceneExit.AddListener(() =>
        {
            RecordPerformance();
        });

        EventManager.OnItemTrashed.AddListener(gameObject =>
        {
            StoreFeedback(gameObject, GoesToLockerSpotType.Trash);
        });

        EventManager.OnDragFailed.AddListener(gameObject =>
        {
            if (itemInfos.ContainsKey(gameObject.name))
            {
                itemInfos.Remove(gameObject.name);
            }
        });

        EventManager.OnDragSuccessed.AddListener(gameObject =>
        {
            StoreFeedback(gameObject, GoesToLockerSpotType.Locker);
        });
    }

    private void StoreFeedback(GameObject gameObject, GoesToLockerSpotType actualAction)
    {
        GoesToLockerSpotType supposedAction = gameObject.GetComponent<GoesToLockerSpot>().goesToLockerSpotType;
        string itemName = gameObject.name;
        if(itemInfos.ContainsKey(itemName))
        {
            itemInfos.Remove(itemName);
        }

        if (supposedAction == actualAction)
        {
            itemInfos.Add(itemName, "Item " + itemName + " was correctly placed in " + actualAction);
        } else
        {
            itemInfos.Add(itemName, "Item " + itemName + " was incorrectly placed in " + actualAction + ". It should have been placed in " + supposedAction + ".");
        }
    }

    private void OnDisable()
    {
        EventManager.OnBeforeSceneExit.RemoveAllListeners();
    }
}