using System.Collections.Generic;
using UnityEngine;

public class DecontaminationFeedbackManager : MonoBehaviour
{
    public List<DecontaminationItemInfo> decontaminationItems = new ();
    public DecontaminationPerformance decontaminationPerformance;

    public void RegisterDecontaminationItem(DecontaminationItemInfo item)
    {
        if (!decontaminationItems.Contains(item))
        {
            decontaminationItems.Add(item);
        }
    }

    public void UnregisterDecontaminationItem(DecontaminationItemInfo item)
    {
        if (decontaminationItems.Contains(item))
        {
            decontaminationItems.Remove(item);
        }
    }

    private void RecordPerformance ()
    {
        if (FeedbackSingleton.Instance == null) return;

        decontaminationPerformance = new()
        {
            itemInfos = new Dictionary<string, string>()
        };

        foreach (DecontaminationItemInfo decontaminationItemInfo in decontaminationItems)
        {
            string feedback = "";
            decontaminationPerformance.totalItems++;
            if (decontaminationItemInfo.maxBagLevels > decontaminationItemInfo.currentBagLevels)
            {
                feedback = "Item should have been put into " + decontaminationItemInfo.maxBagLevels + " bag(s). Item was put into " + decontaminationItemInfo.currentBagLevels + " bag(s).";
            }

            if(decontaminationItemInfo.contaminationSpots.Count > 0)
            {
                feedback += "Item still has " + decontaminationItemInfo.contaminationSpots.Count + " spots of contamination remaining.";
            }

            if (feedback == "")
            {
                feedback = "Item fully decontaminated.";
                decontaminationPerformance.cleanedItems++;
            }

            //Debug.Log("Decontamination feedback for " + decontaminationItemInfo.itemName + ": " + feedback);
            decontaminationPerformance.itemInfos.Add(decontaminationItemInfo.itemName, feedback);
        }

        //Debug.Log("Decontamination performance recorded: " + decontaminationPerformance.cleanedItems + " out of " + decontaminationPerformance.totalItems + " items cleaned.");
        decontaminationPerformance.wasRecorded = true;
        FeedbackSingleton.Instance.decontaminationPerformance = decontaminationPerformance;
    }

    private void OnEnable()
    {
        EventManager.OnBeforeSceneExit.AddListener(() =>
        {
            RecordPerformance();
        });
    }

    private void OnDisable()
    {
        EventManager.OnBeforeSceneExit.RemoveAllListeners();
    }
}

public struct DecontaminationPerformance 
{
    public int totalItems;
    public int cleanedItems;
    public Dictionary<string, string> itemInfos;
    public bool wasRecorded;

    public DecontaminationPerformance(int totalItems, int cleanedItems, Dictionary<string, string> itemInfos, bool wasRecorded = false)
    {
        this.totalItems = totalItems;
        this.cleanedItems = cleanedItems;
        this.itemInfos = itemInfos;
        this.wasRecorded = wasRecorded;
    }
}