using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class TestManager
{
    // Output CSV path
    private static readonly string path = Path.Combine(Application.persistentDataPath, "test.csv");

    // Buffer lines to reduce disk writes
    private static readonly List<string> csvLines = new();

    // Ensure CSV header is present once
    private static bool headerWritten = false;

    public static void Save(ComputationAndLengthData data)
    {
        if (data == null) return;

        // Write header if file does not exist (or first call)
        if (!headerWritten)
        {
            if (!File.Exists(path))
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(path, "Agent,ComputationTimeMs,PathNodesCount,CalculationSteps\n");
            }
            headerWritten = true;
        }

        csvLines.Add(string.Join(",",
            Escape(data.agent),
            data.computationTimeMs.ToString("F6", CultureInfo.InvariantCulture),
            data.pathNodesCount.ToString(CultureInfo.InvariantCulture),
            //data.pathWorldDistance.ToString("F4", CultureInfo.InvariantCulture),
            data.stepsTaken.ToString(CultureInfo.InvariantCulture)
        ));

        try
        {
            File.AppendAllLines(path, csvLines);
            csvLines.Clear();
        }
        catch (Exception ex)
        {
            Debug.LogError($"CSV write failed: {ex.Message}");
        }
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return (s.Contains(",") || s.Contains("\""))
            ? "\"" + s.Replace("\"", "\"\"") + "\""
            : s;
    }
}

[Serializable]
public class ComputationAndLengthData
{
    public string agent;              
    public float computationTimeMs;   
    public int pathNodesCount;         
    //public float pathWorldDistance;
    public int stepsTaken;
}