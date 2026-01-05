using System.Collections;
using TMPro;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.SceneManagement;

public class FPSLog : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textFieldFps;
    [SerializeField] private float interval = 0.5f;
    [SerializeField] private bool doLog = false;

    private float fps = 0;
    private float duration = 0;
    private float numberOfParticles = 0;
    private string pathFilename;

    private int iterations = 0;
    private int cumulativeFps = 0;

    private IEnumerator Start()
    {
        pathFilename = Application.dataPath + @"\TestResults\" + SceneManager.GetActiveScene().name + "_" + DateTime.Now.ToString("yyyy-dd-M_HH-mm-ss") + ".txt";
        while (true)
        {
            yield return new WaitForSeconds(interval);

            fps = cumulativeFps / iterations;
            iterations = 0;
            cumulativeFps = 0;

            textFieldFps.SetText("FPS: " + fps);
            if (doLog) SaveLogToTxt();

            duration += interval;
        }
    }

    private void Update()
    {
        iterations++;
        cumulativeFps += (int)Mathf.Round(1f / Time.unscaledDeltaTime);
    }

    private void SaveLogToTxt()
    {
        if (!File.Exists(pathFilename))
        {
            File.WriteAllText(pathFilename, "Time;Number of Particles;FPS\n");
        }

        string content = duration + ";" + numberOfParticles + ";" + fps + "\n";
        File.AppendAllText(pathFilename, content);
    }
}
