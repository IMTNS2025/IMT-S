using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to organize pattern presets and prepare them for runtime loading.
/// </summary>
public static class PatternOrganizer
{
    [MenuItem("Tools/Airflow Simulation/Copy Patterns to Resources")]
    public static void CopyPatternsToResources()
    {
        string sourceFolder = "Assets/ScriptableObjects/InputPatterns";
        string resourcesFolder = "Assets/Resources/InputPatterns";

        // Ensure Resources folder exists
        if (!Directory.Exists("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        
        if (!Directory.Exists(resourcesFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "InputPatterns");
        }

        // Find all pattern assets
        string[] guids = AssetDatabase.FindAssets("t:SimulatedInputPatternSO", new[] { sourceFolder });
        
        if (guids.Length == 0)
        {
            Debug.LogWarning($"No patterns found in {sourceFolder}. Create patterns first using the preset menu.");
            return;
        }

        int copiedCount = 0;
        foreach (string guid in guids)
        {
            string sourcePath = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileName(sourcePath);
            string destPath = Path.Combine(resourcesFolder, fileName);

            // Copy the asset
            if (AssetDatabase.CopyAsset(sourcePath, destPath))
            {
                copiedCount++;
                Debug.Log($"Copied: {fileName}");
            }
            else if (File.Exists(destPath))
            {
                Debug.Log($"Already exists: {fileName}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"Pattern organization complete! Copied {copiedCount} new patterns to Resources folder.");
        Debug.Log($"Total patterns in Resources/InputPatterns: {guids.Length}");
    }

    [MenuItem("Tools/Airflow Simulation/List All Patterns")]
    public static void ListAllPatterns()
    {
        string[] folders = { "Assets/ScriptableObjects/InputPatterns", "Assets/Resources/InputPatterns" };
        
        Debug.Log("=== Pattern List ===");
        
        foreach (string folder in folders)
        {
            if (!Directory.Exists(folder))
                continue;
                
            string[] guids = AssetDatabase.FindAssets("t:SimulatedInputPatternSO", new[] { folder });
            
            Debug.Log($"\n{folder}: {guids.Length} patterns");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SimulatedInputPatternSO pattern = AssetDatabase.LoadAssetAtPath<SimulatedInputPatternSO>(path);
                
                if (pattern != null)
                {
                    string info = $"  - {pattern.name}: {pattern.segments.Count} segments, ";
                    info += pattern.loopPattern ? "loops" : "one-shot";
                    Debug.Log(info);
                }
            }
        }
        
        Debug.Log("===================");
    }

    [MenuItem("Tools/Airflow Simulation/Validate Pattern Setup")]
    public static void ValidatePatternSetup()
    {
        Debug.Log("=== Pattern Setup Validation ===");
        
        // Check for patterns in ScriptableObjects folder
        string sourceFolder = "Assets/ScriptableObjects/InputPatterns";
        string[] sourceGuids = AssetDatabase.FindAssets("t:SimulatedInputPatternSO", new[] { sourceFolder });
        Debug.Log($"Patterns in {sourceFolder}: {sourceGuids.Length}");
        
        if (sourceGuids.Length == 0)
        {
            Debug.LogWarning("? No patterns found! Use 'Assets > Create > Airflow Simulation > Preset Patterns > Create All Presets'");
        }
        
        // Check for patterns in Resources folder
        string resourcesFolder = "Assets/Resources/InputPatterns";
        if (Directory.Exists(resourcesFolder))
        {
            string[] resourceGuids = AssetDatabase.FindAssets("t:SimulatedInputPatternSO", new[] { resourcesFolder });
            Debug.Log($"Patterns in {resourcesFolder}: {resourceGuids.Length}");
            
            if (resourceGuids.Length == 0)
            {
                Debug.LogWarning("? No patterns in Resources folder. Use 'Tools > Airflow Simulation > Copy Patterns to Resources'");
            }
        }
        else
        {
            Debug.LogWarning("? Resources/InputPatterns folder doesn't exist. Use 'Tools > Airflow Simulation > Copy Patterns to Resources'");
        }
        
        // Check for menu controller in scene
        MainMenuController menuController = Object.FindObjectOfType<MainMenuController>();
        if (menuController == null)
        {
            Debug.LogWarning("? No MainMenuController found in scene. Create menu UI using 'GameObject > UI > Airflow Simulation > Complete Menu System'");
        }
        else
        {
            Debug.Log("? MainMenuController found in scene");
            
            SerializedObject so = new SerializedObject(menuController);
            SerializedProperty patterns = so.FindProperty("availablePatterns");
            Debug.Log($"  Assigned patterns: {patterns.arraySize}");
            
            SerializedProperty simControllerProp = so.FindProperty("simulationController");
            if (simControllerProp.objectReferenceValue != null)
            {
                Debug.Log("? SimulatedInputController reference assigned");
            }
            else
            {
                Debug.LogWarning("? SimulatedInputController not assigned to MainMenuController");
            }
        }
        
        // Check for simulation controller in scene
        SimulatedInputController inputController = Object.FindObjectOfType<SimulatedInputController>();
        if (inputController == null)
        {
            Debug.LogWarning("? No SimulatedInputController found in scene");
        }
        else
        {
            Debug.Log("? SimulatedInputController found in scene");
        }
        
        Debug.Log("===============================");
    }
}
