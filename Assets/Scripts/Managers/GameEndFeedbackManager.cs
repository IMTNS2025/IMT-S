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

        if (PerformanceManager.Instance.decontaminationPerformance.wasRecorded)
        {
            text += "Decontamination Performance:\n";
            text += PerformanceManager.Instance.decontaminationPerformance.totalItems + " total items.\n";
            text += PerformanceManager.Instance.decontaminationPerformance.cleanedItems + " cleaned items.\n";
            foreach (var kvp in PerformanceManager.Instance.decontaminationPerformance.itemInfos)
            {
                text += kvp.Key + ": " + kvp.Value + "\n";
            }
        }


        textMeshProUGUI.SetText(text);
    }
}
