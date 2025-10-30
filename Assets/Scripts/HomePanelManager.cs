//=====================================
// HomePanelManager.cs - Home Screen Panel Navigation
//=====================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ChatDialogueSystem;
using static ChatDialogueSystem.DebugHelper;

/// <summary>
/// Link between an app button and its target panel
/// </summary>
[System.Serializable]
public class AppPanelLink
{
    public Button appButton;       // The button to click
    public GameObject targetPanel; // The panel this button opens
    public string panelName;       // Optional: for debugging
}

/// <summary>
/// Manages panel navigation in the Home Screen.
/// Handles switching between app panels with back/home button support.
/// </summary>
public class HomePanelManager : MonoBehaviour
{
    [Header("App Button to Panel Links")]
    [SerializeField] private List<AppPanelLink> appLinks = new List<AppPanelLink>();

    [Header("Default Panel")]
    [SerializeField] private GameObject defaultPanel;

    [Header("Phone Navigation Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button homeButton;

    private GameObject currentPanel;
    private Stack<GameObject> panelHistory = new Stack<GameObject>();

    // ═══════════════════════════════════════════════════════════
    // ░ INITIALIZATION
    // ═══════════════════════════════════════════════════════════

    private void Start()
    {
        InitializePanels();
        SetupAppButtons();
        SetupNavigationButtons();
        
        Log(Category.UI, $"[HomePanelManager] Initialized with {appLinks.Count} app links");
    }

    /// <summary>
    /// Disable all panels except default
    /// </summary>
    private void InitializePanels()
    {
        // Disable all linked panels
        foreach (var link in appLinks)
        {
            if (link.targetPanel != null)
            {
                link.targetPanel.SetActive(false);
            }
        }

        // Activate default panel
        if (defaultPanel != null)
        {
            defaultPanel.SetActive(true);
            currentPanel = defaultPanel;
            Log(Category.UI, "[HomePanelManager] Default panel activated");
        }
        else
        {
            LogWarning(Category.UI, "[HomePanelManager] No default panel assigned!");
        }
    }

    /// <summary>
    /// Hook up app buttons to their target panels
    /// </summary>
    private void SetupAppButtons()
    {
        foreach (var link in appLinks)
        {
            if (link.appButton == null)
            {
                LogWarning(Category.UI, "[HomePanelManager] App button is null in link!");
                continue;
            }

            if (link.targetPanel == null)
            {
                LogWarning(Category.UI, $"[HomePanelManager] Target panel is null for button: {link.appButton.name}");
                continue;
            }

            // ✅ FIX: Capture panel reference to avoid closure bug
            GameObject panel = link.targetPanel;
            link.appButton.onClick.AddListener(() => SwitchPanel(panel));
        }
    }

    /// <summary>
    /// Hook up back/home navigation buttons
    /// </summary>
    private void SetupNavigationButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBack);
        }
        else
        {
            LogWarning(Category.UI, "[HomePanelManager] Back button not assigned!");
        }

        if (homeButton != null)
        {
            homeButton.onClick.AddListener(GoHome);
        }
        else
        {
            LogWarning(Category.UI, "[HomePanelManager] Home button not assigned!");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ░ PANEL NAVIGATION
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Switch to a new panel and add current to history
    /// </summary>
    public void SwitchPanel(GameObject newPanel)
    {
        if (newPanel == null)
        {
            LogWarning(Category.UI, "[HomePanelManager] Attempted to switch to null panel!");
            return;
        }

        if (newPanel == currentPanel)
        {
            Log(Category.UI, "[HomePanelManager] Already on this panel - ignoring");
            return;
        }

        // ✅ Push current to history (if exists)
        if (currentPanel != null)
        {
            panelHistory.Push(currentPanel);
            currentPanel.SetActive(false);
        }

        // Switch to new panel
        newPanel.SetActive(true);
        currentPanel = newPanel;

        Log(Category.UI, $"[HomePanelManager] Switched to panel: {newPanel.name} | History depth: {panelHistory.Count}");
    }

    /// <summary>
    /// Go back to previous panel in history
    /// </summary>
    public void GoBack()
    {
        if (panelHistory.Count == 0)
        {
            Log(Category.UI, "[HomePanelManager] No history - going home instead");
            GoHome();
            return;
        }

        // ✅ Safely deactivate current panel
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
        }

        // Pop previous panel from history
        currentPanel = panelHistory.Pop();
        
        if (currentPanel != null)
        {
            currentPanel.SetActive(true);
            Log(Category.UI, $"[HomePanelManager] Back to: {currentPanel.name} | History depth: {panelHistory.Count}");
        }
        else
        {
            LogWarning(Category.UI, "[HomePanelManager] Popped null panel from history!");
        }
    }

    /// <summary>
    /// Return to default panel and clear history
    /// </summary>
    public void GoHome()
    {
        // Deactivate current panel
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
        }

        // Clear history
        panelHistory.Clear();

        // Activate default panel
        if (defaultPanel != null)
        {
            defaultPanel.SetActive(true);
            currentPanel = defaultPanel;
            Log(Category.UI, "[HomePanelManager] Returned to home panel");
        }
        else
        {
            LogWarning(Category.UI, "[HomePanelManager] No default panel assigned!");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ░ PUBLIC API
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Get the currently active panel
    /// </summary>
    public GameObject GetCurrentPanel() => currentPanel;

    /// <summary>
    /// Check if we can go back (has history)
    /// </summary>
    public bool CanGoBack() => panelHistory.Count > 0;

    /// <summary>
    /// Check if currently on home panel
    /// </summary>
    public bool IsOnHomePanel() => currentPanel == defaultPanel;

    // ═══════════════════════════════════════════════════════════
    // ░ DEBUG TOOLS (Editor Only)
    // ═══════════════════════════════════════════════════════════

#if UNITY_EDITOR
    [ContextMenu("Debug/Print Current State")]
    private void DebugPrintState()
    {
        Debug.Log($"═══════════════════════════════════\n" +
                 $"PANEL MANAGER STATE:\n" +
                 $"Current Panel: {(currentPanel != null ? currentPanel.name : "NULL")}\n" +
                 $"Default Panel: {(defaultPanel != null ? defaultPanel.name : "NULL")}\n" +
                 $"History Depth: {panelHistory.Count}\n" +
                 $"Can Go Back: {CanGoBack()}\n" +
                 $"Is On Home: {IsOnHomePanel()}\n" +
                 $"App Links: {appLinks.Count}\n" +
                 $"═══════════════════════════════════");
    }

    [ContextMenu("Debug/Print All App Links")]
    private void DebugPrintLinks()
    {
        Debug.Log($"═══════════════════════════════════");
        Debug.Log($"APP PANEL LINKS ({appLinks.Count}):");
        
        for (int i = 0; i < appLinks.Count; i++)
        {
            var link = appLinks[i];
            Debug.Log($"[{i}] Button: {(link.appButton != null ? link.appButton.name : "NULL")} → " +
                     $"Panel: {(link.targetPanel != null ? link.targetPanel.name : "NULL")}");
        }
        
        Debug.Log($"═══════════════════════════════════");
    }

    [ContextMenu("Debug/Simulate Back Press")]
    private void DebugSimulateBack()
    {
        GoBack();
    }

    [ContextMenu("Debug/Simulate Home Press")]
    private void DebugSimulateHome()
    {
        GoHome();
    }

    [ContextMenu("Debug/Clear History")]
    private void DebugClearHistory()
    {
        panelHistory.Clear();
        Debug.Log("✅ Panel history cleared");
    }
#endif
}