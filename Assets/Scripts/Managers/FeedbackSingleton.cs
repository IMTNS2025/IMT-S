using System.Collections.Generic;
using UnityEngine;

public class FeedbackSingleton : MonoBehaviour
{
    public static FeedbackSingleton Instance;

    public DecontaminationPerformance decontaminationPerformance;
    public Dictionary<string, string> lockerPerformance;
    public Dictionary<string, string> firstLayerProtectionPerformance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
