using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Controls the main menu overlay where users can select pattern presets,
/// start the simulation, or quit the game.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject inGameMenuPanel;
    [SerializeField] private GameObject inGameFPSPanel;
    [SerializeField] private GameObject openSettingsButton;
    [SerializeField] private GameObject openLogButton;
    public GameObject MenuPanel => mainMenuPanel;
    [SerializeField] private TMP_Dropdown patternDropdown;
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button freeModeButton;

    [Header("Particle Settings UI")]
    [SerializeField] private Slider particleCountSlider;
    [SerializeField] private TMP_Text particleCountText;
    [SerializeField] private Toggle reloadOnPlayToggle;
    [SerializeField] private Toggle autoReloadOnSliderChangeToggle;

    [Header("Pattern Assets")]
    [SerializeField] private List<SimulatedInputPatternSO> availablePatterns;

    [Header("Simulation Reference")]
    [SerializeField] private SimulatedInputController simulationController;

    [Header("Particle Manager Reference")]
    [SerializeField] private ParticleManager particleManager;

    private SimulatedInputPatternSO selectedPattern;
    private bool autoReloadOnSliderChange = false;
    private float lastSliderChangeTime = 0f;
    private const float SLIDER_CHANGE_DELAY = 0.3f; // Delay before auto-reload to avoid spamming

    private void Awake()
    {
        // Ensure menu is visible at start
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        // Setup UI callbacks
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitButtonClicked);
        }

        if (freeModeButton != null)
        {
            freeModeButton.onClick.AddListener(OnFreeModeButtonClicked);
        }

        if (patternDropdown != null)
        {
            patternDropdown.onValueChanged.AddListener(OnPatternSelectionChanged);
        }

        // Setup particle count slider
        if (particleCountSlider != null)
        {
            particleCountSlider.onValueChanged.AddListener(OnParticleCountChanged);
        }

        // Setup reload on play toggle
        if (reloadOnPlayToggle != null)
        {
            reloadOnPlayToggle.onValueChanged.AddListener(OnReloadOnPlayToggled);
        }

        // Setup auto-reload on slider change toggle
        if (autoReloadOnSliderChangeToggle != null)
        {
            autoReloadOnSliderChangeToggle.onValueChanged.AddListener(OnAutoReloadOnSliderChangeToggled);
        }

        // Pause the game while in menu
        Time.timeScale = 0f;
    }

    private void Start()
    {
        PopulatePatternDropdown();
        
        // Select the first pattern by default
        if (availablePatterns != null && availablePatterns.Count > 0)
        {
            SelectPattern(0);
        }

        // Auto-find simulation controller if not assigned
        if (simulationController == null)
        {
            simulationController = FindObjectOfType<SimulatedInputController>();
        }

        // Auto-find particle manager if not assigned
        if (particleManager == null)
        {
            particleManager = FindObjectOfType<ParticleManager>();
            if (particleManager == null)
            {
                Debug.LogWarning("[MainMenuController] ParticleManager not found in scene!");
            }
        }

        // Initialize particle count slider
        InitializeParticleCountSlider();

        // Initialize reload on play toggle
        if (reloadOnPlayToggle != null && particleManager != null)
        {
            reloadOnPlayToggle.isOn = particleManager.ReloadOnPlay;
        }

        // Initialize auto-reload toggle
        if (autoReloadOnSliderChangeToggle != null)
        {
            autoReloadOnSliderChangeToggle.isOn = autoReloadOnSliderChange;
        }
    }

    private void InitializeParticleCountSlider()
    {
        if (particleManager == null || particleCountSlider == null)
        {
            Debug.LogWarning("[MainMenuController] Cannot initialize particle count slider - missing references");
            return;
        }

        particleCountSlider.minValue = particleManager.MinParticleCount;
        particleCountSlider.maxValue = particleManager.MaxParticleCount;
        particleCountSlider.wholeNumbers = true;
        
        // Set step size (this is achieved by rounding to nearest step in the callback)
        particleCountSlider.value = particleManager.DefaultParticleCount;
        
        // Set the particle count in the manager
        particleManager.SetParticleCount(particleManager.DefaultParticleCount);
        
        UpdateParticleCountText(particleManager.DefaultParticleCount);
        
        Debug.Log($"[MainMenuController] Initialized slider - min: {particleCountSlider.minValue}, max: {particleCountSlider.maxValue}, value: {particleCountSlider.value}");
    }

    private void OnParticleCountChanged(float value)
    {
        if (particleManager == null)
        {
            Debug.LogWarning("[MainMenuController] ParticleManager is null in OnParticleCountChanged");
            return;
        }

        // Round to nearest step
        int step = particleManager.ParticleCountStep;
        int roundedValue = Mathf.RoundToInt(value / step) * step;
        
        // Clamp to ensure it's within bounds
        roundedValue = Mathf.Clamp(roundedValue, particleManager.MinParticleCount, particleManager.MaxParticleCount);
        
        Debug.Log($"[MainMenuController] Slider changed - raw value: {value}, rounded: {roundedValue}, step: {step}");
        
        // Update slider value if it was rounded
        if (Mathf.Abs(particleCountSlider.value - roundedValue) > 0.1f)
        {
            particleCountSlider.value = roundedValue;
            return; // The callback will be triggered again with the correct value
        }

        particleManager.SetParticleCount(roundedValue);
        UpdateParticleCountText(roundedValue);

        // Auto-reload if enabled and game is running (not in menu)
        if (autoReloadOnSliderChange && Time.timeScale > 0f)
        {
            // Use delayed reload to avoid spamming when dragging slider
            lastSliderChangeTime = Time.realtimeSinceStartup;
            StopCoroutine(nameof(DelayedAutoReload));
            StartCoroutine(DelayedAutoReload());
        }
    }

    private System.Collections.IEnumerator DelayedAutoReload()
    {
        // Wait for slider to settle (using realtime since game might be paused)
        yield return new WaitForSecondsRealtime(SLIDER_CHANGE_DELAY);
        
        // Only reload if no more slider changes happened during the delay
        if (Time.realtimeSinceStartup - lastSliderChangeTime >= SLIDER_CHANGE_DELAY - 0.05f)
        {
            if (particleManager != null)
            {
                Debug.Log("[MainMenuController] Auto-reloading particles after slider change");
                particleManager.ReloadParticles();
            }
        }
    }

    private void UpdateParticleCountText(int count)
    {
        if (particleCountText != null)
        {
            string reloadNote = autoReloadOnSliderChange ? "" : " (reload required)";
            particleCountText.text = $"Particle Count: {count}{reloadNote}";
        }
    }

    private void OnReloadOnPlayToggled(bool value)
    {
        if (particleManager != null)
        {
            particleManager.ReloadOnPlay = value;
            Debug.Log($"[MainMenuController] Reload on play toggled: {value}");
        }
        else
        {
            Debug.LogWarning("[MainMenuController] ParticleManager is null in OnReloadOnPlayToggled");
        }
    }

    private void OnAutoReloadOnSliderChangeToggled(bool value)
    {
        autoReloadOnSliderChange = value;
        Debug.Log($"[MainMenuController] Auto-reload on slider change toggled: {value}");
        
        // Update the particle count text to show/hide "reload required" note
        if (particleManager != null)
        {
            UpdateParticleCountText(particleManager.CurrentParticleCount);
        }
    }

    private void PopulatePatternDropdown()
    {
        if (patternDropdown == null)
            return;

        patternDropdown.ClearOptions();

        if (availablePatterns == null || availablePatterns.Count == 0)
        {
            Debug.LogWarning("[MainMenuController] No patterns available. Please assign patterns in the Inspector.");
            return;
        }

        // Create dropdown options from pattern names
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (var pattern in availablePatterns)
        {
            if (pattern != null)
            {
                options.Add(new TMP_Dropdown.OptionData(pattern.name));
            }
        }

        patternDropdown.AddOptions(options);
    }

    private void OnPatternSelectionChanged(int index)
    {
        SelectPattern(index);
    }

    private void SelectPattern(int index)
    {
        if (availablePatterns == null || index < 0 || index >= availablePatterns.Count)
            return;

        selectedPattern = availablePatterns[index];
    }

    private void OnStartButtonClicked()
    {
        if (selectedPattern == null)
        {
            Debug.LogWarning("[MainMenuController] No pattern selected!");
            return;
        }

        if (simulationController == null)
        {
            Debug.LogError("[MainMenuController] SimulatedInputController not found!");
            return;
        }

        // Always enable SimulatedInputController GameObject and script
        simulationController.gameObject.SetActive(true);
        simulationController.enabled = true;

        // Load the selected pattern
        simulationController.LoadPattern(selectedPattern);
        simulationController.isRunning = true;
        
        // Enable simulated input mode
        InteractionInputSystem.UseSimulatedInput = true;

        // Hide menu and resume game
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        if (inGameMenuPanel != null)
            inGameMenuPanel.SetActive(true);
        if (inGameFPSPanel != null)
            inGameFPSPanel.SetActive(true);
        if (openLogButton != null)
            openLogButton.SetActive(false);
        if (openSettingsButton != null)
            openSettingsButton.SetActive(false);

        Time.timeScale = 1f;

        // Trigger particle reload if enabled
        if (particleManager != null)
        {
            particleManager.OnModeStart();
        }

        Debug.Log($"[MainMenuController] Starting simulation with pattern: {selectedPattern.name}");
    }

    private void OnFreeModeButtonClicked()
    {
        // Always disable SimulatedInputController script and GameObject if present
        if (simulationController != null)
        {
            simulationController.isRunning = false;
            simulationController.enabled = false;
            simulationController.gameObject.SetActive(false);
        }
        
        // Disable simulated input mode
        InteractionInputSystem.UseSimulatedInput = false;

        // Hide menu and resume game
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        if (inGameMenuPanel != null)
            inGameMenuPanel.SetActive(true);
        if (inGameFPSPanel != null)
            inGameFPSPanel.SetActive(true);
        if (openLogButton != null)
            openLogButton.SetActive(false);
        if (openSettingsButton != null)
            openSettingsButton.SetActive(false);

        Time.timeScale = 1f;

        // Trigger particle reload if enabled
        if (particleManager != null)
        {
            particleManager.OnModeStart();
        }

        Debug.Log("[MainMenuController] Free mode started (SimulatedInputController disabled)");
    }

    private void OnQuitButtonClicked()
    {
        Debug.Log("[MainMenuController] Quitting application...");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Shows the menu (called from InGameMenuController)
    /// </summary>
    public void ShowMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        // Pause simulation
        if (simulationController != null)
        {
            simulationController.isRunning = false;
            InteractionInputSystem.UseSimulatedInput = false;
        }

        Time.timeScale = 0f;
    }

    /// <summary>
    /// Loads patterns from Resources folder at runtime
    /// </summary>
    public void LoadPatternsFromResources(string resourcePath = "InputPatterns")
    {
        var patterns = Resources.LoadAll<SimulatedInputPatternSO>(resourcePath);
        if (patterns != null && patterns.Length > 0)
        {
            availablePatterns = patterns.ToList();
            PopulatePatternDropdown();
            
            if (availablePatterns.Count > 0)
            {
                SelectPattern(0);
            }
        }
        else
        {
            Debug.LogWarning($"[MainMenuController] No patterns found in Resources/{resourcePath}");
        }
    }
}
