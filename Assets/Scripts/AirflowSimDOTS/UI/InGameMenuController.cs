using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the in-game menu button that returns to the main menu.
/// </summary>
public class InGameMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button menuButton;
    [SerializeField] private GameObject buttonPanel;
    [SerializeField] private GameObject fpsPanel;

    [Header("Menu Reference")]
    [SerializeField] private MainMenuController mainMenuController;

    private void Awake()
    {
        // Setup button callback
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(OnMenuButtonClicked);
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
    }

    private void Start()
    {
        // Auto-find main menu controller if not assigned
        if (mainMenuController == null)
        {
            mainMenuController = FindObjectOfType<MainMenuController>();
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

        // ESC key to return to menu
        if (Input.GetKeyDown(KeyCode.Escape) && shouldShow)
        {
            OnMenuButtonClicked();
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
}
