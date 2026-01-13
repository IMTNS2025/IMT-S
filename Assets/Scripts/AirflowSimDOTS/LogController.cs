using System.Collections;
using TMPro;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LogController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textFieldFps;
    [SerializeField] private float interval = 0.5f;
    [SerializeField] private bool doLog = false;

    [Header("Menu Event Logging")]
    [SerializeField] private MainMenuController mainMenuController;
    [SerializeField] private InGameMenuController inGameMenuController;
    [Header("Particle Manager Reference")]
    [SerializeField] private ParticleReloadController particleManager;

    [Header("Log Viewing (iOS)")]
    [SerializeField] private Button viewLogButton;
    [SerializeField] private GameObject logViewPanel;
    [SerializeField] private TextMeshProUGUI logContentText;
    [SerializeField] private Button closeLogButton;
    [SerializeField] private Button shareLogButton;

    private float fps = 0;
    private float duration = 0;
    private float numberOfParticles = 0;
    private string pathFilename;

    private int iterations = 0;
    private int cumulativeFps = 0;
    // Added fields to accumulate and report delta time
    private float cumulativeDeltaTime = 0f;
    private float avgDeltaTime = 0f;

    private void Awake()
    {
        // Setup log viewing UI
        if (viewLogButton != null)
        {
            viewLogButton.onClick.AddListener(OnViewLogButtonClicked);
        }

        if (closeLogButton != null)
        {
            closeLogButton.onClick.AddListener(OnCloseLogButtonClicked);
        }

        if (shareLogButton != null)
        {
            shareLogButton.onClick.AddListener(OnShareLogButtonClicked);
        }

        if (logViewPanel != null)
        {
            logViewPanel.SetActive(false);
        }
    }

    private IEnumerator Start()
    {
        // Use persistentDataPath which is accessible on iOS
        string directory = Path.Combine(Application.persistentDataPath, "TestResults");
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        pathFilename = Path.Combine(directory, SceneManager.GetActiveScene().name + "_" + DateTime.Now.ToString("yyyy-dd-M_HH-mm-ss") + ".txt");
        
        Debug.Log($"[FPSLog] Log file path: {pathFilename}");

        // Auto-find menu controllers if not assigned
        if (mainMenuController == null)
        {
            mainMenuController = FindFirstObjectByType<MainMenuController>();
        }

        if (inGameMenuController == null)
        {
            inGameMenuController = FindFirstObjectByType<InGameMenuController>();
        }

        // Auto-find particle manager if not assigned
        if (particleManager == null)
        {
            particleManager = FindFirstObjectByType<ParticleReloadController>();
            if (particleManager == null)
            {
                Debug.LogWarning("[FPSLog] ParticleManager not found in scene!");
            }
        }

        // Subscribe to menu events
        SubscribeToMenuEvents();

        // Log initial event
        LogEvent("Application Started");

        while (true)
        {
            yield return new WaitForSeconds(interval);

            // Safely compute averages only if we recorded frames
            if (iterations > 0)
            {
                fps = (float)cumulativeFps / iterations;
                avgDeltaTime = cumulativeDeltaTime / iterations;
            }
            else
            {
                fps = 0f;
                avgDeltaTime = 0f;
            }

            iterations = 0;
            cumulativeFps = 0;
            cumulativeDeltaTime = 0f;

            // Update current particle count from ParticleManager if available
            if (particleManager != null)
            {
                numberOfParticles = particleManager.CurrentParticleCount;
            }

            // Update on-screen text with FPS and average delta time
            if (textFieldFps != null)
            {
                textFieldFps.SetText($"FPS: {fps:F1}  dt: {avgDeltaTime:F4}s");
            }

            if (doLog) SaveLogToTxt();

            duration += interval;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from menu events
        UnsubscribeFromMenuEvents();
    }

    private void SubscribeToMenuEvents()
    {
        if (mainMenuController != null)
        {
            // Find buttons and subscribe to their events
            var startButton = FindButtonInController(mainMenuController, "startButton");
            if (startButton != null)
            {
                startButton.onClick.AddListener(() => LogEvent($"Menu: Start Button Clicked (Particles: {GetParticleCountForLog()})"));
            }

            var freeModeButton = FindButtonInController(mainMenuController, "freeModeButton");
            if (freeModeButton != null)
            {
                freeModeButton.onClick.AddListener(() => LogEvent($"Menu: Free Mode Button Clicked (Particles: {GetParticleCountForLog()})"));
            }

            var quitButton = FindButtonInController(mainMenuController, "quitButton");
            if (quitButton != null)
            {
                quitButton.onClick.AddListener(() => LogEvent("Menu: Quit Button Clicked"));
            }

            // Subscribe to dropdown changes
            var dropdown = FindDropdownInController(mainMenuController);
            if (dropdown != null)
            {
                dropdown.onValueChanged.AddListener((index) => 
                {
                    string patternName = dropdown.options[index].text;
                    LogEvent($"Menu: Pattern Changed to '{patternName}' (index: {index})");
                });
            }
        }

        if (inGameMenuController != null)
        {
            var menuButton = FindButtonInController(inGameMenuController, "menuButton");
            if (menuButton != null)
            {
                menuButton.onClick.AddListener(() => LogEvent("Menu: Return to Menu Button Clicked"));
            }
        }
    }

    private void UnsubscribeFromMenuEvents()
    {
        // Button listeners will be automatically removed when destroyed
        // This is just for cleanup if needed
    }

    private Button FindButtonInController(MonoBehaviour controller, string fieldName)
    {
        var field = controller.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.Public);
        
        return field?.GetValue(controller) as Button;
    }

    private TMP_Dropdown FindDropdownInController(MonoBehaviour controller)
    {
        var field = controller.GetType().GetField("patternDropdown", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.Public);
        
        return field?.GetValue(controller) as TMP_Dropdown;
    }

    private void Update()
    {
        iterations++;
        cumulativeFps += (int)Mathf.Round(1f / Time.unscaledDeltaTime);
        // Accumulate unscaled delta time for average delta calculation
        cumulativeDeltaTime += Time.unscaledDeltaTime;

        // ESC key to show log viewer (for testing)
        if (Input.GetKeyDown(KeyCode.L) && Input.GetKey(KeyCode.LeftShift))
        {
            OnViewLogButtonClicked();
        }
    }

    private void SaveLogToTxt()
    {
        if (!File.Exists(pathFilename))
        {
            File.WriteAllText(pathFilename, "Time;Number of Particles;FPS;DeltaTime\n");
        }

        string content = duration + ";" + numberOfParticles + ";" + fps + ";" + avgDeltaTime + "\n";
        File.AppendAllText(pathFilename, content);
    }

    private void LogEvent(string eventDescription)
    {
        try
        {
            if (!File.Exists(pathFilename))
            {
                // Create file with headers if it doesn't exist
                string header = "Timestamp;Event Type;Description\n";
                File.WriteAllText(pathFilename, header);
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string content = $"{timestamp};Event;{eventDescription}\n";
            File.AppendAllText(pathFilename, content);

            Debug.Log($"[FPSLog] Logged event: {eventDescription}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FPSLog] Error logging event: {ex.Message}");
        }
    }

    private string GetParticleCountForLog()
    {
        if (particleManager != null)
            return particleManager.CurrentParticleCount.ToString();

        return "unknown";
    }

    private void OnViewLogButtonClicked()
    {
        if (logViewPanel != null && logContentText != null)
        {
            // Read and display log content
            try
            {
                if (File.Exists(pathFilename))
                {
                    string logContent = File.ReadAllText(pathFilename);
                    logContentText.text = logContent;
                    logViewPanel.SetActive(true);
                }
                else
                {
                    logContentText.text = "No log file found.";
                    logViewPanel.SetActive(true);
                }
            }
            catch (Exception ex)
            {
                logContentText.text = $"Error reading log: {ex.Message}";
                logViewPanel.SetActive(true);
            }
        }
    }

    private void OnCloseLogButtonClicked()
    {
        if (logViewPanel != null)
        {
            logViewPanel.SetActive(false);
        }
    }

    private void OnShareLogButtonClicked()
    {
        #if UNITY_IOS && !UNITY_EDITOR
        StartCoroutine(ShareLogFile());
        #else
        Debug.Log($"[FPSLog] Log file location: {pathFilename}");
        Debug.Log("[FPSLog] Sharing is only available on iOS devices.");
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    private IEnumerator ShareLogFile()
    {
        if (!File.Exists(pathFilename))
        {
            Debug.LogWarning("[FPSLog] No log file to share.");
            yield break;
        }

        // Use iOS native sharing
        NativeShare shareSheet = new ();
        shareSheet.AddFile(pathFilename);
        shareSheet.SetSubject("FPS Log - " + SceneManager.GetActiveScene().name);
        shareSheet.SetText("FPS and event log from " + Application.productName);
        
        yield return shareSheet.Share();
    }
    #endif

    /// <summary>
    /// Public method to get the current log file path
    /// </summary>
    public string GetLogFilePath()
    {
        return pathFilename;
    }

    /// <summary>
    /// Public method to manually log custom events
    /// </summary>
    public void LogCustomEvent(string eventDescription)
    {
        LogEvent(eventDescription);
    }
}
