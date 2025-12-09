using System.Collections.Generic;
using UnityEngine;

public class PerformanceManager : MonoBehaviour
{
    public static PerformanceManager Instance;

    public DecontaminationPerformance decontaminationPerformance;

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
