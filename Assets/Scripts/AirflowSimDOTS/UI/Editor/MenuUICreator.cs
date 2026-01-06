using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor utility to quickly create the menu UI hierarchy for the Airflow Simulation.
/// </summary>
public static class MenuUICreator
{
    [MenuItem("GameObject/UI/Airflow Simulation/Complete Menu System", false, 0)]
    public static void CreateCompleteMenuSystem()
    {
        // Ensure EventSystem exists
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Find or create Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // Set CanvasScaler for mobile landscape
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); // Landscape
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f; // Prefer height for landscape
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            Debug.Log("Created new Canvas");
        }
        else if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        // Create Menu System GameObject
        GameObject menuSystem = new GameObject("MenuSystem");
        MainMenuController menuController = menuSystem.AddComponent<MainMenuController>();
        
        // Create Main Menu Panel (full screen, anchor stretch)
        GameObject mainMenuPanel = CreatePanel(canvas.transform, "MainMenuPanel", new Color(0, 0, 0, 0.85f));
        RectTransform mainMenuRect = mainMenuPanel.GetComponent<RectTransform>();
        mainMenuRect.anchorMin = new Vector2(0f, 0f);
        mainMenuRect.anchorMax = new Vector2(1f, 1f);
        mainMenuRect.offsetMin = Vector2.zero;
        mainMenuRect.offsetMax = Vector2.zero;
        mainMenuRect.sizeDelta = Vector2.zero;

        // Create Title (larger font)
        GameObject title = CreateText(mainMenuPanel.transform, "TitleText", "AIRFLOW SIMULATION", 80);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -60);
        titleRect.sizeDelta = new Vector2(1200, 120);

        // Create Pattern Label
        GameObject patternLabel = CreateText(mainMenuPanel.transform, "PatternLabel", "Select Pattern:", 48);
        RectTransform labelRect = patternLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.8f);
        labelRect.anchorMax = new Vector2(0.5f, 0.8f);
        labelRect.anchoredPosition = new Vector2(0, 0);
        labelRect.sizeDelta = new Vector2(600, 60);

        // Create Pattern Dropdown (larger touch target)
        GameObject dropdown = CreateDropdown(mainMenuPanel.transform, "PatternDropdown");
        RectTransform dropdownRect = dropdown.GetComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0.5f, 0.7f);
        dropdownRect.anchorMax = new Vector2(0.5f, 0.7f);
        dropdownRect.anchoredPosition = new Vector2(0, 0);
        dropdownRect.sizeDelta = new Vector2(700, 90);

        // Create Start Button (large)
        GameObject startButton = CreateButton(mainMenuPanel.transform, "StartButton", "START SIMULATION");
        RectTransform startRect = startButton.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(0.5f, 0.35f);
        startRect.anchorMax = new Vector2(0.5f, 0.35f);
        startRect.anchoredPosition = new Vector2(0, 0);
        startRect.sizeDelta = new Vector2(700, 120);

        // Create Free Mode Button (large)
        GameObject freeModeButton = CreateButton(mainMenuPanel.transform, "FreeModeButton", "FREE MODE");
        RectTransform freeModeRect = freeModeButton.GetComponent<RectTransform>();
        freeModeRect.anchorMin = new Vector2(0.5f, 0.22f);
        freeModeRect.anchorMax = new Vector2(0.5f, 0.22f);
        freeModeRect.anchoredPosition = new Vector2(0, 0);
        freeModeRect.sizeDelta = new Vector2(700, 120);

        // Create Quit Button (large)
        GameObject quitButton = CreateButton(mainMenuPanel.transform, "QuitButton", "QUIT GAME");
        RectTransform quitRect = quitButton.GetComponent<RectTransform>();
        quitRect.anchorMin = new Vector2(0.5f, 0.09f);
        quitRect.anchorMax = new Vector2(0.5f, 0.09f);
        quitRect.anchoredPosition = new Vector2(0, 0);
        quitRect.sizeDelta = new Vector2(700, 120);

        // Create In-Game Menu Panel (top right, mobile size)
        GameObject inGamePanel = CreatePanel(canvas.transform, "InGameMenuPanel", new Color(0, 0, 0, 0.5f));
        RectTransform inGameRect = inGamePanel.GetComponent<RectTransform>();
        inGameRect.anchorMin = new Vector2(0.85f, 0.85f);
        inGameRect.anchorMax = new Vector2(0.99f, 0.99f);
        inGameRect.pivot = new Vector2(1f, 1f);
        inGameRect.anchoredPosition = new Vector2(-20, -20);
        inGameRect.sizeDelta = new Vector2(0, 0);

        // Create Menu Button (large touch target)
        GameObject menuButton = CreateButton(inGamePanel.transform, "MenuButton", "MENU");
        RectTransform menuRect = menuButton.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0, 0);
        menuRect.anchorMax = new Vector2(1, 1);
        menuRect.offsetMin = Vector2.zero;
        menuRect.offsetMax = Vector2.zero;

        // Add InGameMenuController
        InGameMenuController inGameController = inGamePanel.AddComponent<InGameMenuController>();

        // Wire up references
        EditorUtility.SetDirty(menuController);
        SerializedObject menuControllerSO = new SerializedObject(menuController);
        menuControllerSO.FindProperty("menuPanel").objectReferenceValue = mainMenuPanel;
        menuControllerSO.FindProperty("patternDropdown").objectReferenceValue = dropdown.GetComponent<TMP_Dropdown>();
        menuControllerSO.FindProperty("startButton").objectReferenceValue = startButton.GetComponent<Button>();
        menuControllerSO.FindProperty("quitButton").objectReferenceValue = quitButton.GetComponent<Button>();
        menuControllerSO.FindProperty("freeModeButton").objectReferenceValue = freeModeButton.GetComponent<Button>();
        menuControllerSO.ApplyModifiedProperties();

        EditorUtility.SetDirty(inGameController);
        SerializedObject inGameControllerSO = new SerializedObject(inGameController);
        inGameControllerSO.FindProperty("menuButton").objectReferenceValue = menuButton.GetComponent<Button>();
        inGameControllerSO.FindProperty("buttonPanel").objectReferenceValue = inGamePanel;
        inGameControllerSO.FindProperty("mainMenuController").objectReferenceValue = menuController;
        inGameControllerSO.ApplyModifiedProperties();

        // Hide in-game panel initially
        inGamePanel.SetActive(false);

        // Select the menu system in hierarchy
        Selection.activeGameObject = menuSystem;

        Debug.Log("Complete Menu System created successfully! Don't forget to:");
        Debug.Log("1. Assign pattern ScriptableObjects to MainMenuController");
        Debug.Log("2. Assign SimulatedInputController reference to MainMenuController");
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        Image image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        
        return panel;
    }

    private static GameObject CreateText(Transform parent, string name, string text, int fontSize)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);
        
        RectTransform rect = textGO.AddComponent<RectTransform>();
        
        TextMeshProUGUI textMesh = textGO.AddComponent<TextMeshProUGUI>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = Color.white;
        textMesh.raycastTarget = true;
        
        return textGO;
    }

    private static GameObject CreateButton(Transform parent, string name, string buttonText)
    {
        GameObject button = new GameObject(name);
        button.transform.SetParent(parent, false);
        
        RectTransform rect = button.AddComponent<RectTransform>();
        
        Image image = button.AddComponent<Image>();
        image.color = new Color(0.2f, 0.4f, 0.8f, 1f);
        image.raycastTarget = true;
        
        Button buttonComponent = button.AddComponent<Button>();
        
        // Create text child
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(button.transform, false);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI textMesh = textGO.AddComponent<TextMeshProUGUI>();
        textMesh.text = buttonText;
        textMesh.fontSize = 24;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = Color.white;
        textMesh.raycastTarget = true;
        
        return button;
    }

    private static GameObject CreateDropdown(Transform parent, string name)
    {
        GameObject dropdown = new GameObject(name);
        dropdown.transform.SetParent(parent, false);
        
        RectTransform rect = dropdown.AddComponent<RectTransform>();
        
        Image image = dropdown.AddComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        
        TMP_Dropdown dropdownComponent = dropdown.AddComponent<TMP_Dropdown>();
        
        // Create label
        GameObject label = CreateText(dropdown.transform, "Label", "Select...", 20);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.offsetMin = new Vector2(10, 2);
        labelRect.offsetMax = new Vector2(-25, -2);
        TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
        labelText.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Center;
        
        // Create arrow
        GameObject arrow = new GameObject("Arrow");
        arrow.transform.SetParent(dropdown.transform, false);
        RectTransform arrowRect = arrow.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0.5f);
        arrowRect.anchorMax = new Vector2(1, 0.5f);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.sizeDelta = new Vector2(20, 20);
        arrowRect.anchoredPosition = new Vector2(-15, 0);
        Image arrowImage = arrow.AddComponent<Image>();
        arrowImage.color = Color.white;
        
        // Create template
        GameObject template = new GameObject("Template");
        template.transform.SetParent(dropdown.transform, false);
        RectTransform templateRect = template.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.anchoredPosition = new Vector2(0, 2);
        templateRect.sizeDelta = new Vector2(0, 150);
        
        Image templateImage = template.AddComponent<Image>();
        templateImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        ScrollRect scrollRect = template.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        
        // Create viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(template.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        viewport.AddComponent<Image>();
        
        // Create content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 28);
        
        // Create item
        GameObject item = new GameObject("Item");
        item.transform.SetParent(content.transform, false);
        RectTransform itemRect = item.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);
        itemRect.sizeDelta = new Vector2(0, 20);
        
        // Add background image to item
        Image itemBackground = item.AddComponent<Image>();
        itemBackground.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        itemBackground.raycastTarget = true;
        
        // Add toggle to item
        Toggle itemToggle = item.AddComponent<Toggle>();
        itemToggle.isOn = false;
        
        // Create label child for item
        GameObject itemLabel = new GameObject("ItemLabel");
        itemLabel.transform.SetParent(item.transform, false);
        RectTransform itemLabelRect = itemLabel.AddComponent<RectTransform>();
        itemLabelRect.anchorMin = new Vector2(0, 0);
        itemLabelRect.anchorMax = new Vector2(1, 1);
        itemLabelRect.offsetMin = Vector2.zero;
        itemLabelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI itemText = itemLabel.AddComponent<TextMeshProUGUI>();
        itemText.text = "Option";
        itemText.fontSize = 18;
        itemText.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Center;
        itemText.color = Color.white;
        itemText.raycastTarget = true;

        // Assign dropdown references
        dropdownComponent.captionText = labelText;
        dropdownComponent.itemText = itemText;
        dropdownComponent.template = templateRect;
        
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        
        template.SetActive(false);
        
        return dropdown;
    }
}
