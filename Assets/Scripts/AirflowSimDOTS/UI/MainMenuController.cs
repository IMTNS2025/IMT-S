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

    public GameObject MenuPanel => mainMenuPanel;
    [SerializeField] private TMP_Dropdown patternDropdown;
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button freeModeButton;

    [Header("Pattern Assets")]
    [SerializeField] private List<SimulatedInputPatternSO> availablePatterns;

    [Header("Simulation Reference")]
    [SerializeField] private SimulatedInputController simulationController;

    private SimulatedInputPatternSO selectedPattern;

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

        Time.timeScale = 1f;

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

        Time.timeScale = 1f;

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
