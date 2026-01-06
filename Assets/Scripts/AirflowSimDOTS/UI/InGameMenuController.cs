using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the in-game menu button that returns to the main menu.
/// </summary>
public class InGameMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button menuButton;
    [SerializeField] private Button reloadButton;
    [SerializeField] private GameObject buttonPanel;
    [SerializeField] private GameObject fpsPanel;
    [SerializeField] private GameObject openLogButton;
    [SerializeField] private GameObject openSettingsButton;

    [Header("Menu Reference")]
    [SerializeField] private MainMenuController mainMenuController;

    [Header("Particle Manager Reference")]
    [SerializeField] private ParticleManager particleManager;

    private void Awake()
    {
        // Setup button callback
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(OnMenuButtonClicked);
        }

        // Setup reload button callback
        if (reloadButton != null)
        {
            reloadButton.onClick.AddListener(OnReloadButtonClicked);
        }

        // Hide button initially (shown when game starts)
        if (buttonPanel != null)
        {
            buttonPanel.SetActive(false);
        }
        if (fpsPanel != null)
        {
            fpsPanel.SetActive(false);
        }
        if (openSettingsButton != null)
        {
            openSettingsButton.SetActive(true);
        }
        if (openLogButton != null)
        {
            openLogButton.SetActive(true);
        }
    }

    private void Start()
    {
        // Auto-find main menu controller if not assigned
        if (mainMenuController == null)
        {
            mainMenuController = FindObjectOfType<MainMenuController>();
        }

        // Auto-find particle manager if not assigned
        if (particleManager == null)
        {
            particleManager = FindObjectOfType<ParticleManager>();
            if (particleManager == null)
            {
                Debug.LogWarning("[InGameMenuController] ParticleManager not found in scene!");
            }
        }

        // Ensure buttonPanel is active at start
        if (buttonPanel != null)
        {
            buttonPanel.SetActive(true);
        }
        if (fpsPanel != null)
        {
            fpsPanel.SetActive(true);
        }
        if (openSettingsButton != null)
        {
            openSettingsButton.SetActive(false);
        }
        if (openLogButton != null)
        {
            openLogButton.SetActive(false);
        }
    }

    private void Update()
    {
        // Show button only when game is running and main menu is hidden
        bool menuVisible = mainMenuController != null && mainMenuController.MenuPanel != null && mainMenuController.MenuPanel.activeInHierarchy;
        bool shouldShow = Time.timeScale > 0f && !menuVisible;

        if (buttonPanel != null)
        {
            if (!buttonPanel.activeSelf && shouldShow)
                buttonPanel.SetActive(true);
            else if (buttonPanel.activeSelf && !shouldShow)
                buttonPanel.SetActive(false);
        }

        if (fpsPanel != null)
        {
            if (!fpsPanel.activeSelf && shouldShow)
                fpsPanel.SetActive(true);
            else if (fpsPanel.activeSelf && !shouldShow)
                fpsPanel.SetActive(false);
        }

        // Ensure the log and settings buttons are only active when the main menu is visible (not during gameplay)
        if (openLogButton != null)
        {
            bool shouldBeActive = !shouldShow; // active when not showing in-game UI (i.e., on menu)
            if (openLogButton.activeSelf != shouldBeActive)
                openLogButton.SetActive(shouldBeActive);
        }

        if (openSettingsButton != null)
        {
            bool shouldBeActive = !shouldShow; // active when not showing in-game UI (i.e., on menu)
            if (openSettingsButton.activeSelf != shouldBeActive)
                openSettingsButton.SetActive(shouldBeActive);
        }

        // ESC key to return to menu
        if (Input.GetKeyDown(KeyCode.Escape) && shouldShow)
        {
            OnMenuButtonClicked();
        }

        // R key to reload particles
        if (Input.GetKeyDown(KeyCode.R) && shouldShow)
        {
            OnReloadButtonClicked();
        }
    }

    private void OnMenuButtonClicked()
    {
        if (mainMenuController != null)
        {
            mainMenuController.ShowMenu();
        }
        else
        {
            Debug.LogError("[InGameMenuController] MainMenuController reference not found!");
        }
    }

    private void OnReloadButtonClicked()
    {
        Debug.Log("[InGameMenuController] Reload button clicked");
        
        if (particleManager != null)
        {
            particleManager.ReloadParticles();
        }
        else
        {
            Debug.LogError("[InGameMenuController] ParticleManager not found. Cannot reload particles.");
        }
    }
}
