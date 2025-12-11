using TMPro;
using UnityEngine;

public class GameEndFeedbackManager : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;

    private void Update()
    {
        ShowFeedback();
    }

    private void ShowFeedback()
    {
        canvas.enabled = true;
        string text = "";

        //if (FeedbackSingleton.Instance.decontaminationPerformance?.wasRecorded)
        //{
        //    text += "Decontamination Performance:\n";
        //    text += FeedbackSingleton.Instance.decontaminationPerformance.totalItems + " total items.\n";
        //    text += FeedbackSingleton.Instance.decontaminationPerformance.cleanedItems + " cleaned items.\n";
        //    foreach (var kvp in FeedbackSingleton.Instance.decontaminationPerformance.itemInfos)
        //    {
        //        text += kvp.Key + ": " + kvp.Value + "\n";
        //    }
        //}

        //if (FeedbackSingleton.Instance.lockerPerformance?.Count > 0)
        //{
        //    text += "Locker Performance:\n";
        //    foreach (var kvp in FeedbackSingleton.Instance.lockerPerformance)
        //    {
        //        text += kvp.Key + ": " + kvp.Value + "\n";
        //    }
        //}

        if (FeedbackSingleton.Instance.firstLayerProtectionPerformance?.Count > 0)
        {
            text += "First Protection Layer Performance:\n";
            foreach (var kvp in FeedbackSingleton.Instance.firstLayerProtectionPerformance)
            {
                text += kvp.Value + "\n";
            }
        }

        textMeshProUGUI.SetText(text);
    }
}
