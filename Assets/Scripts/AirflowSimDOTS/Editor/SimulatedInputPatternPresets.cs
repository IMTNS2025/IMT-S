using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to create preset pattern ScriptableObjects.
/// </summary>
public static class SimulatedInputPatternPresets
{
    private const string AssetPath = "Assets/ScriptableObjects/InputPatterns/";

    [MenuItem("Assets/Create/Airflow Simulation/Preset Patterns/Horizontal Sweep")]
    public static void CreateHorizontalSweep()
    {
        var pattern = CreatePatternAsset("HorizontalSweep");
        pattern.startPosition = new Vector2(-6f, 0f);
        pattern.startAngle = 0f;
        pattern.loopPattern = true;
        pattern.startDelay = 1f;
        
        pattern.AddStraight(12f, 3f);
        pattern.AddPause(0.5f);
        pattern.AddTurn(180f, 2f, true);
        pattern.AddStraight(12f, 3f);
        pattern.AddPause(0.5f);
        pattern.AddTurn(180f, 2f, false);
        
        SavePatternAsset(pattern, "HorizontalSweep");
    }

    [MenuItem("Assets/Create/Airflow Simulation/Preset Patterns/Circle")]
    public static void CreateCircle()
    {
        var pattern = CreatePatternAsset("Circle");
        pattern.startPosition = new Vector2(0f, -3f);
        pattern.startAngle = 90f;
        pattern.loopPattern = true;
        pattern.startDelay = 1f;
        
        // Create a circle with 8 segments
        for (int i = 0; i < 8; i++)
        {
            pattern.AddStraight(2f, 4f);
            pattern.AddTurn(45f, 3f, true);
        }
        
        SavePatternAsset(pattern, "Circle");
    }

    [MenuItem("Assets/Create/Airflow Simulation/Preset Patterns/Figure 8")]
    public static void CreateFigure8()
    {
        var pattern = CreatePatternAsset("Figure8");
        pattern.startPosition = new Vector2(0f, 0f);
        pattern.startAngle = 90f;
        pattern.loopPattern = true;
        pattern.startDelay = 1f;
        
        // First loop (left)
        for (int i = 0; i < 8; i++)
        {
            pattern.AddStraight(1f, 3f);
            pattern.AddTurn(45f, 2f, true);
        }

        // Second loop (right)
        for (int i = 0; i < 8; i++)
        {
            pattern.AddStraight(1f, 3f);
            pattern.AddTurn(45f, 2f, false);
        }
        
        SavePatternAsset(pattern, "Figure8");
    }

    [MenuItem("Assets/Create/Airflow Simulation/Preset Patterns/Fast Straight")]
    public static void CreateFastStraight()
    {
        var pattern = CreatePatternAsset("FastStraight");
        pattern.startPosition = new Vector2(-7f, 0f);
        pattern.startAngle = 0f;
        pattern.loopPattern = true;
        pattern.startDelay = 1f;
        
        pattern.AddStraight(14f, 15f); // Very fast
        pattern.AddPause(1f);
        
        SavePatternAsset(pattern, "FastStraight");
    }

    [MenuItem("Assets/Create/Airflow Simulation/Preset Patterns/Slow Weave")]
    public static void CreateSlowWeave()
    {
        var pattern = CreatePatternAsset("SlowWeave");
        pattern.startPosition = new Vector2(-6f, 0f);
        pattern.startAngle = 30f;
        pattern.loopPattern = true;
        pattern.startDelay = 1f;
        
        for (int i = 0; i < 6; i++)
        {
            pattern.AddStraight(2f, 2f);
            pattern.AddTurn(60f, 1.5f, i % 2 == 0);
        }
        
        SavePatternAsset(pattern, "SlowWeave");
    }

    [MenuItem("Assets/Create/Airflow Simulation/Preset Patterns/Spiral Inward")]
    public static void CreateSpiralInward()
    {
        var pattern = CreatePatternAsset("SpiralInward");
        pattern.startPosition = new Vector2(0f, -5f);
        pattern.startAngle = 90f;
        pattern.loopPattern = false;
        pattern.startDelay = 0.5f;
        
        float distance = 3f;
        for (int i = 0; i < 12; i++)
        {
            pattern.AddStraight(distance, 4f);
            pattern.AddTurn(90f, 3f, true);
            distance = Mathf.Max(0.5f, distance - 0.2f);
        }
        
        SavePatternAsset(pattern, "SpiralInward");
    }

    [MenuItem("Assets/Create/Airflow Simulation/Preset Patterns/Zigzag")]
    public static void CreateZigzag()
    {
        var pattern = CreatePatternAsset("Zigzag");
        pattern.startPosition = new Vector2(-6f, -2f);
        pattern.startAngle = 45f;
        pattern.loopPattern = true;
        pattern.startDelay = 0.5f;
        
        for (int i = 0; i < 8; i++)
        {
            pattern.AddStraight(2f, 5f);
            pattern.AddTurn(90f, 4f, i % 2 == 0);
        }
        
        SavePatternAsset(pattern, "Zigzag");
    }

    private static SimulatedInputPatternSO CreatePatternAsset(string name)
    {
        var pattern = ScriptableObject.CreateInstance<SimulatedInputPatternSO>();
        pattern.name = name;
        return pattern;
    }

    private static void SavePatternAsset(SimulatedInputPatternSO pattern, string name)
    {
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
        {
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        }
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/InputPatterns"))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "InputPatterns");
        }

        string path = $"{AssetPath}{name}.asset";
        
        // Check if asset already exists
        var existing = AssetDatabase.LoadAssetAtPath<SimulatedInputPatternSO>(path);
        if (existing != null)
        {
            // Update existing asset
            EditorUtility.CopySerialized(pattern, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(pattern);
            AssetDatabase.SaveAssets();
            Selection.activeObject = existing;
            Debug.Log($"Updated existing pattern asset: {path}");
        }
        else
        {
            // Create new asset
            AssetDatabase.CreateAsset(pattern, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = pattern;
            Debug.Log($"Created pattern asset: {path}");
        }
        
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    [MenuItem("Assets/Create/Airflow Simulation/Preset Patterns/Create All Presets")]
    public static void CreateAllPresets()
    {
        CreateHorizontalSweep();
        CreateCircle();
        CreateFigure8();
        CreateFastStraight();
        CreateSlowWeave();
        CreateSpiralInward();
        CreateZigzag();
        
        Debug.Log("All preset patterns created in: " + AssetPath);
    }
}
