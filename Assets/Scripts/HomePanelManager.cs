//=====================================
// HomePanelManager.cs - Flexible Phone Panel Navigation System
// Inspector-driven configuration - No code changes needed for most use cases
//=====================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ChatDialogueSystem;
using static ChatDialogueSystem.DebugHelper;

/// <summary>
/// Links an app button to its target panel with enable/disable control
/// </summary>
[System.Serializable]
public class AppPanelLink
{
    [Header("Configuration")]
    [Tooltip("Enable this app panel in the build")]
    public bool enabled = true;
    
    [Header("References")]
    public Button appButton;
    public GameObject targetPanel;
    
    [Header("Display (Optional)")]
    [Tooltip("Optional name for debugging and logs")]
    public string panelName;
    
    [Tooltip("If true, hides the app button when disabled")]
    public bool hideButtonWhenDisabled = true;
}

/// <summary>
/// Flexible, inspector-driven phone UI navigation system.
/// Supports hierarchical navigation with panels and overlays.
/// No code changes needed - configure everything in Inspector!
/// </summary>
public class HomePanelManager : MonoBehaviour
{
    public static HomePanelManager Instance { get; private set; }

    [Header("═══════════ CORE SETUP ═══════════")]
    [SerializeField] private GameObject homeScreenPanel;
    
    [Header("═══════════ NAVIGATION BUTTONS ═══════════")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button homeButton;
    
    [Header("═══════════ APP PANELS ═══════════")]
    [Tooltip("Add your app panels here. Disable any you don't need!")]
    [SerializeField] private List<AppPanelLink> appLinks = new List<AppPanelLink>();

    [Header("═══════════ OVERLAYS ═══════════")]
    [Tooltip("Register overlays here for tracking (optional but recommended)")]
    [SerializeField] private List<GameObject> overlayPanels = new List<GameObject>();

    [Header("═══════════ SETTINGS ═══════════")]
    [Tooltip("Log navigation events to console")]
    [SerializeField] private bool enableDebugLogs = true;
    
    [Tooltip("Automatically hide disabled app buttons from UI")]
    [SerializeField] private bool autoHideDisabledButtons = true;

    // Navigation state
    private GameObject currentPanel;
    private Stack<GameObject> navigationHistory = new Stack<GameObject>();
    private Stack<GameObject> activeOverlays = new Stack<GameObject>();
    
    // Stats
    private int totalPanelsOpened = 0;
    private int totalOverlaysOpened = 0;

    // ═══════════════════════════════════════════════════════════
    // ░ SINGLETON & INITIALIZATION
    // ═══════════════════════════════════════════════════════════
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[HomePanelManager] Multiple instances detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        InitializePanels();
        SetupAppButtons();
        SetupNavigationButtons();
        
        LogDebug($"[HomePanelManager] Initialized with {GetEnabledPanelCount()}/{appLinks.Count} panels enabled");
    }

    /// <summary>
    /// Initialize all panels to their default state
    /// </summary>
    private void InitializePanels()
    {
        // Hide all app panels (enabled or disabled)
        foreach (var link in appLinks)
        {
            if (link.targetPanel != null)
            {
                link.targetPanel.SetActive(false);
            }
            
            // Handle button visibility for disabled apps
            if (!link.enabled && link.appButton != null && autoHideDisabledButtons)
            {
                if (link.hideButtonWhenDisabled)
                {
                    link.appButton.gameObject.SetActive(false);
                }
                else
                {
                    // Keep button visible but disable interaction
                    link.appButton.interactable = false;
                }
            }
        }

        // Hide all overlays
        foreach (var overlay in overlayPanels)
        {
            if (overlay != null)
            {
                overlay.SetActive(false);
            }
        }

        // Show home screen as default
        if (homeScreenPanel != null)
        {
            homeScreenPanel.SetActive(true);
            currentPanel = homeScreenPanel;
            LogDebug("[HomePanelManager] Home screen initialized");
        }
        else
        {
            Debug.LogError("[HomePanelManager] ❌ Home screen panel is not assigned! Assign it in Inspector.");
        }
    }

    /// <summary>
    /// Setup listeners for enabled app buttons only
    /// </summary>
    private void SetupAppButtons()
    {
        int enabledCount = 0;
        int disabledCount = 0;
        
        foreach (var link in appLinks)
        {
            // Skip disabled apps
            if (!link.enabled)
            {
                disabledCount++;
                continue;
            }
            
            if (link.appButton == null)
            {
                Debug.LogWarning($"[HomePanelManager] ⚠️ App button is null for panel: {link.panelName}");
                continue;
            }

            if (link.targetPanel == null)
            {
                Debug.LogWarning($"[HomePanelManager] ⚠️ Target panel is null for button: {link.appButton.name}");
                continue;
            }

            // Capture the panel reference for the lambda
            GameObject targetPanel = link.targetPanel;
            string panelName = link.panelName;
            
            link.appButton.onClick.AddListener(() => OpenPanel(targetPanel, panelName));
            enabledCount++;
        }
        
        LogDebug($"[HomePanelManager] Setup complete: {enabledCount} enabled, {disabledCount} disabled");
    }

    /// <summary>
    /// Setup listeners for navigation buttons
    /// </summary>
    private void SetupNavigationButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackPressed);
        }
        else
        {
            Debug.LogWarning("[HomePanelManager] ⚠️ Back button is not assigned!");
        }

        if (homeButton != null)
        {
            homeButton.onClick.AddListener(OnHomePressed);
        }
        else
        {
            Debug.LogWarning("[HomePanelManager] ⚠️ Home button is not assigned!");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ░ PANEL NAVIGATION
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Open a new panel and add current panel to history
    /// </summary>
    public void OpenPanel(GameObject newPanel, string panelName = "")
    {
        if (newPanel == null)
        {
            Debug.LogError("[HomePanelManager] Cannot open null panel");
            return;
        }

        if (newPanel == currentPanel)
        {
            LogDebug($"[HomePanelManager] Panel '{newPanel.name}' is already open");
            return;
        }

        // Save current panel to history
        if (currentPanel != null)
        {
            navigationHistory.Push(currentPanel);
            currentPanel.SetActive(false);
            LogDebug($"[HomePanelManager] Hiding panel: {currentPanel.name}");
        }

        // Open new panel
        newPanel.SetActive(true);
        currentPanel = newPanel;
        totalPanelsOpened++;
        
        string displayName = !string.IsNullOrEmpty(panelName) ? panelName : newPanel.name;
        LogDebug($"[HomePanelManager] ✅ Opened panel: {displayName} (History: {navigationHistory.Count})");
    }

    /// <summary>
    /// Go back to the previous panel in history
    /// </summary>
    private void GoBack()
    {
        if (navigationHistory.Count == 0)
        {
            LogDebug("[HomePanelManager] No history, going to home screen");
            GoToHome();
            return;
        }

        // Hide current panel
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            LogDebug($"[HomePanelManager] Closing panel: {currentPanel.name}");
        }

        // Pop and show previous panel
        currentPanel = navigationHistory.Pop();
        
        if (currentPanel != null)
        {
            currentPanel.SetActive(true);
            LogDebug($"[HomePanelManager] ⬅️ Back to: {currentPanel.name} (History: {navigationHistory.Count})");
        }
    }

    /// <summary>
    /// Return directly to home screen, clearing all history
    /// </summary>
    private void GoToHome()
    {
        LogDebug("[HomePanelManager] 🏠 Returning to home screen");

        // Close all overlays
        CloseAllOverlays();

        // Hide current panel if it's not home
        if (currentPanel != null && currentPanel != homeScreenPanel)
        {
            currentPanel.SetActive(false);
        }

        // Clear history
        navigationHistory.Clear();

        // Show home screen
        if (homeScreenPanel != null)
        {
            homeScreenPanel.SetActive(true);
            currentPanel = homeScreenPanel;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ░ OVERLAY MANAGEMENT
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Open an overlay panel on top of current panel
    /// </summary>
    public void OpenOverlay(GameObject overlay)
    {
        if (overlay == null)
        {
            Debug.LogError("[HomePanelManager] Cannot open null overlay");
            return;
        }

        if (!overlayPanels.Contains(overlay))
        {
            Debug.LogWarning($"[HomePanelManager] ⚠️ Overlay '{overlay.name}' is not registered in overlayPanels list. " +
                           "Consider adding it in Inspector for better tracking.");
        }

        overlay.SetActive(true);
        activeOverlays.Push(overlay);
        totalOverlaysOpened++;
        
        LogDebug($"[HomePanelManager] 📱 Opened overlay: {overlay.name} (Active: {activeOverlays.Count})");
    }

    /// <summary>
    /// Close a specific overlay panel (removes it from anywhere in the stack)
    /// </summary>
    public void CloseOverlay(GameObject overlay)
    {
        if (overlay == null) return;

        overlay.SetActive(false);
        
        // Remove from stack (need to rebuild stack without this overlay)
        var tempStack = new Stack<GameObject>();
        bool found = false;
        
        while (activeOverlays.Count > 0)
        {
            var item = activeOverlays.Pop();
            if (item == overlay)
            {
                found = true;
                // Don't add this one back to temp stack
            }
            else
            {
                tempStack.Push(item);
            }
        }
        
        // Restore the stack (maintains original order)
        while (tempStack.Count > 0)
        {
            activeOverlays.Push(tempStack.Pop());
        }
        
        if (found)
        {
            LogDebug($"[HomePanelManager] ❌ Closed overlay: {overlay.name} (Active: {activeOverlays.Count})");
        }
        else
        {
            LogDebug($"[HomePanelManager] ⚠️ Overlay {overlay.name} was not in active stack");
        }
    }

    /// <summary>
    /// Close the most recently opened overlay
    /// </summary>
    private void CloseTopOverlay()
    {
        if (activeOverlays.Count == 0) return;

        var overlay = activeOverlays.Pop();
        if (overlay != null)
        {
            overlay.SetActive(false);
            LogDebug($"[HomePanelManager] ⬅️ Closed top overlay: {overlay.name} (Active: {activeOverlays.Count})");
        }
    }

    /// <summary>
    /// Close all active overlays
    /// </summary>
    private void CloseAllOverlays()
    {
        if (activeOverlays.Count == 0) return;

        int count = activeOverlays.Count;
        
        while (activeOverlays.Count > 0)
        {
            var overlay = activeOverlays.Pop();
            if (overlay != null)
            {
                overlay.SetActive(false);
            }
        }
        
        LogDebug($"[HomePanelManager] Closed all {count} overlays");
    }

    /// <summary>
    /// Check if any overlay is currently open
    /// </summary>
    public bool HasActiveOverlay()
    {
        return activeOverlays.Count > 0;
    }

    // ═══════════════════════════════════════════════════════════
    // ░ BUTTON HANDLERS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Handle back button press - closes overlays first, then navigates back
    /// </summary>
    public void OnBackPressed()
    {
        // Priority 1: Close overlays first (LIFO - Last In First Out)
        if (activeOverlays.Count > 0)
        {
            CloseTopOverlay();
            return;
        }

        // Priority 2: Navigate back through panel history
        GoBack();
    }

    /// <summary>
    /// Handle home button press - return to home screen immediately
    /// </summary>
    public void OnHomePressed()
    {
        GoToHome();
    }

    // ═══════════════════════════════════════════════════════════
    // ░ PUBLIC API
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Get the currently active panel
    /// </summary>
    public GameObject GetCurrentPanel() => currentPanel;

    /// <summary>
    /// Check if we can navigate back
    /// </summary>
    public bool CanGoBack() => navigationHistory.Count > 0 || activeOverlays.Count > 0;

    /// <summary>
    /// Check if we're currently on the home screen
    /// </summary>
    public bool IsOnHomeScreen() => currentPanel == homeScreenPanel && activeOverlays.Count == 0;

    /// <summary>
    /// Get the navigation history depth
    /// </summary>
    public int GetHistoryDepth() => navigationHistory.Count;
    
    /// <summary>
    /// Get number of active overlays
    /// </summary>
    public int GetActiveOverlayCount() => activeOverlays.Count;
    
    /// <summary>
    /// Get total number of enabled panels
    /// </summary>
    public int GetEnabledPanelCount()
    {
        int count = 0;
        foreach (var link in appLinks)
        {
            if (link.enabled) count++;
        }
        return count;
    }
    
    /// <summary>
    /// Check if a specific app panel is enabled
    /// </summary>
    public bool IsPanelEnabled(string panelName)
    {
        foreach (var link in appLinks)
        {
            if (link.panelName == panelName)
                return link.enabled;
        }
        return false;
    }

    // ═══════════════════════════════════════════════════════════
    // ░ RUNTIME PANEL MANAGEMENT (Advanced)
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Enable or disable a panel at runtime by name
    /// </summary>
    public void SetPanelEnabled(string panelName, bool enabled)
    {
        foreach (var link in appLinks)
        {
            if (link.panelName == panelName)
            {
                link.enabled = enabled;
                
                if (link.appButton != null)
                {
                    if (enabled)
                    {
                        link.appButton.gameObject.SetActive(true);
                        link.appButton.interactable = true;
                    }
                    else
                    {
                        if (link.hideButtonWhenDisabled)
                            link.appButton.gameObject.SetActive(false);
                        else
                            link.appButton.interactable = false;
                    }
                }
                
                LogDebug($"[HomePanelManager] Panel '{panelName}' {(enabled ? "enabled" : "disabled")}");
                return;
            }
        }
        
        Debug.LogWarning($"[HomePanelManager] Panel '{panelName}' not found!");
    }

    // ═══════════════════════════════════════════════════════════
    // ░ HELPER METHODS
    // ═══════════════════════════════════════════════════════════
    
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Log(Category.UI, message);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ░ EDITOR UTILITIES
    // ═══════════════════════════════════════════════════════════

#if UNITY_EDITOR
    [ContextMenu("Debug/Print Navigation State")]
    private void DebugPrintState()
    {
        Debug.Log("╔═══════════════════════════════════════════╗");
        Debug.Log("║      HOMEPANELMANAGER STATE              ║");
        Debug.Log("╠═══════════════════════════════════════════╣");
        Debug.Log($"║ Current Panel: {(currentPanel != null ? currentPanel.name : "NULL")}");
        Debug.Log($"║ History Depth: {navigationHistory.Count}");
        Debug.Log($"║ Active Overlays: {activeOverlays.Count}");
        Debug.Log($"║ Can Go Back: {CanGoBack()}");
        Debug.Log($"║ Is On Home: {IsOnHomeScreen()}");
        Debug.Log("╠═══════════════════════════════════════════╣");
        Debug.Log($"║ Stats:");
        Debug.Log($"║   Total Panels Opened: {totalPanelsOpened}");
        Debug.Log($"║   Total Overlays Opened: {totalOverlaysOpened}");
        Debug.Log($"║   Enabled Panels: {GetEnabledPanelCount()}/{appLinks.Count}");
        Debug.Log("╚═══════════════════════════════════════════╝");
    }

    [ContextMenu("Debug/Force Go Home")]
    private void DebugForceHome()
    {
        GoToHome();
        Debug.Log("✅ Forced return to home screen");
    }
    
    [ContextMenu("Debug/List All Panels")]
    private void DebugListPanels()
    {
        Debug.Log("╔═══════════════════════════════════════════╗");
        Debug.Log("║         REGISTERED PANELS                 ║");
        Debug.Log("╠═══════════════════════════════════════════╣");
        
        for (int i = 0; i < appLinks.Count; i++)
        {
            var link = appLinks[i];
            string status = link.enabled ? "✅ ENABLED" : "❌ DISABLED";
            string name = !string.IsNullOrEmpty(link.panelName) ? link.panelName : "Unnamed";
            string button = link.appButton != null ? link.appButton.name : "NULL";
            string panel = link.targetPanel != null ? link.targetPanel.name : "NULL";
            
            Debug.Log($"║ [{i}] {status}");
            Debug.Log($"║     Name: {name}");
            Debug.Log($"║     Button: {button}");
            Debug.Log($"║     Panel: {panel}");
            Debug.Log("╠═══════════════════════════════════════════╣");
        }
        
        Debug.Log("╚═══════════════════════════════════════════╝");
    }
    
    [ContextMenu("Debug/Enable All Panels")]
    private void DebugEnableAllPanels()
    {
        foreach (var link in appLinks)
        {
            link.enabled = true;
            if (link.appButton != null)
            {
                link.appButton.gameObject.SetActive(true);
                link.appButton.interactable = true;
            }
        }
        Debug.Log("✅ All panels enabled!");
    }
    
    [ContextMenu("Debug/Disable All Panels")]
    private void DebugDisableAllPanels()
    {
        foreach (var link in appLinks)
        {
            link.enabled = false;
            if (link.appButton != null && link.hideButtonWhenDisabled)
            {
                link.appButton.gameObject.SetActive(false);
            }
        }
        Debug.Log("❌ All panels disabled!");
    }

    private void OnValidate()
    {
        // Validate that home screen panel is assigned
        if (homeScreenPanel == null)
        {
            Debug.LogWarning("[HomePanelManager] ⚠️ Home screen panel is not assigned!");
        }

        // Validate app links
        foreach (var link in appLinks)
        {
            if (link.enabled && (link.appButton == null || link.targetPanel == null))
            {
                Debug.LogWarning($"[HomePanelManager] ⚠️ Incomplete app link: {link.panelName} (enabled but missing references)");
            }
        }
    }
#endif
}