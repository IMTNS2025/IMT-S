using UnityEngine;

/// <summary>
/// Utility script to automatically load all pattern presets from a Resources folder.
/// This is useful for automatically discovering patterns without manual assignment.
/// </summary>
public class PatternPresetLoader : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Path within Resources folder where patterns are stored")]
    [SerializeField] private string resourcePath = "InputPatterns";

    [Tooltip("Auto-load patterns on start")]
    [SerializeField] private bool autoLoadOnStart = true;

    [Header("Target")]
    [SerializeField] private MainMenuController menuController;

    private void Start()
    {
        if (autoLoadOnStart)
        {
            LoadPatterns();
        }
    }

    public void LoadPatterns()
    {
        if (menuController == null)
        {
            menuController = FindObjectOfType<MainMenuController>();
        }

        if (menuController != null)
        {
            menuController.LoadPatternsFromResources(resourcePath);
            Debug.Log($"[PatternPresetLoader] Loaded patterns from Resources/{resourcePath}");
        }
        else
        {
            Debug.LogError("[PatternPresetLoader] MainMenuController not found!");
        }
    }
}
