using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor script to help set up the FPSLog UI components
/// Run this from the Unity menu: Tools/Setup FPSLog UI
/// </summary>
public class FPSLogUISetup : MonoBehaviour
{
    [MenuItem("Tools/Setup FPSLog UI")]
    public static void SetupUI()
    {
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create View Log Button
        GameObject viewLogButton = CreateButton(canvas.transform, "ViewLogButton", new Vector2(20, -20), new Vector2(150, 50), "View Log");
        
        // Create Log View Panel
        GameObject logPanel = CreatePanel(canvas.transform, "LogViewPanel", Vector2.zero, new Vector2(800, 600));
        logPanel.SetActive(false);

        // Create title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(logPanel.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "FPS and Event Log";
        title.fontSize = 24;
        title.alignment = TextAlignmentOptions.Center;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(-40, 50);
        titleRect.anchoredPosition = new Vector2(0, -10);

        // Create scroll view for log content
        GameObject scrollView = CreateScrollView(logPanel.transform, "LogScrollView", new Vector2(0, -60), new Vector2(-40, -120));
        
        // Get the content text from scroll view
        TextMeshProUGUI logContentText = scrollView.GetComponentInChildren<TextMeshProUGUI>();

        // Create Close button
        GameObject closeButton = CreateButton(logPanel.transform, "CloseLogButton", new Vector2(-20, 20), new Vector2(120, 40), "Close");
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 0);
        closeRect.anchorMax = new Vector2(1, 0);
        closeRect.pivot = new Vector2(1, 0);

        // Create Share button
        GameObject shareButton = CreateButton(logPanel.transform, "ShareLogButton", new Vector2(-160, 20), new Vector2(120, 40), "Share");
        RectTransform shareRect = shareButton.GetComponent<RectTransform>();
        shareRect.anchorMin = new Vector2(1, 0);
        shareRect.anchorMax = new Vector2(1, 0);
        shareRect.pivot = new Vector2(1, 0);

        // Find or create FPSLog component
        FPSLog fpsLog = FindObjectOfType<FPSLog>();
        if (fpsLog == null)
        {
            GameObject fpsLogObj = new GameObject("FPSLog");
            fpsLog = fpsLogObj.AddComponent<FPSLog>();
        }

        // Auto-assign references using reflection
        var fpsLogType = typeof(FPSLog);
        
        SetField(fpsLog, "viewLogButton", viewLogButton.GetComponent<Button>());
        SetField(fpsLog, "logViewPanel", logPanel);
        SetField(fpsLog, "logContentText", logContentText);
        SetField(fpsLog, "closeLogButton", closeButton.GetComponent<Button>());
        SetField(fpsLog, "shareLogButton", shareButton.GetComponent<Button>());

        EditorUtility.SetDirty(fpsLog);
        Debug.Log("[FPSLogUISetup] UI components created and assigned successfully!");
    }

    private static GameObject CreateButton(Transform parent, string name, Vector2 position, Vector2 size, string text)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        Button button = buttonObj.AddComponent<Button>();
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 16;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return buttonObj;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject panelObj = new GameObject(name);
        panelObj.transform.SetParent(parent, false);
        
        RectTransform rect = panelObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = panelObj.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        return panelObj;
    }

    private static GameObject CreateScrollView(Transform parent, string name, Vector2 position, Vector2 sizeDelta)
    {
        GameObject scrollViewObj = new GameObject(name);
        scrollViewObj.transform.SetParent(parent, false);
        
        RectTransform scrollRect = scrollViewObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.sizeDelta = sizeDelta;
        scrollRect.anchoredPosition = position;

        Image scrollImage = scrollViewObj.AddComponent<Image>();
        scrollImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        
        ScrollRect scroll = scrollViewObj.AddComponent<ScrollRect>();

        // Create Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollViewObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        
        Image viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        Mask mask = viewportObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Create Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 1000);
        contentRect.anchoredPosition = Vector2.zero;

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Create Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(contentObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Log content will appear here...";
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.color = Color.white;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 1);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 1);
        textRect.sizeDelta = new Vector2(-20, 0);
        textRect.anchoredPosition = new Vector2(0, -10);

        // Configure ScrollRect
        scroll.content = contentRect;
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        return scrollViewObj;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.Public);
        
        if (field != null)
        {
            field.SetValue(target, value);
        }
        else
        {
            Debug.LogWarning($"[FPSLogUISetup] Field '{fieldName}' not found in FPSLog");
        }
    }
}
#endif
