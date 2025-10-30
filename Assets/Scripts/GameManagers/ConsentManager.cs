//=====================================
// ConsentManager.cs - Age Gate & Consent Handler
//=====================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Handles 18+ age verification and content warning.
/// Shows once per device until player accepts.
/// If declined, shows again on next launch.
/// </summary>
public class ConsentManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject consentPanel;
    [SerializeField] private Button buttonAgree;
    [SerializeField] private Button buttonExit;

    [SerializeField] private TextMeshProUGUI titleText;    // Header
    [SerializeField] private TextMeshProUGUI consentText;  // ScrollView content
    [SerializeField] private TextMeshProUGUI agreeText;    // Button text
    [SerializeField] private TextMeshProUGUI exitText;     // Button text

    [Header("Scene Settings")]
    [SerializeField] private string bootstrapSceneName = "01_Bootstrap";

    [Header("Optional: Custom Text")]
    [SerializeField] private bool useCustomText = false;
    [SerializeField][TextArea(10, 20)] private string customConsentText;

    // ═══════════════════════════════════════════════════════════
    // ░ CONSTANTS
    // ═══════════════════════════════════════════════════════════

    private const string CONSENT_KEY = "HasAcceptedConsent";

    private const string DEFAULT_TITLE = "Disclaimer & Age Verification";

    private const string DEFAULT_CONSENT_TEXT =
        "Warning: NSFW Content\n\n" +
        "This game contains adult content intended for mature audiences only.\n\n" +
        "All characters, events, and locations in this game are entirely fictional. Any resemblance to real persons or events is purely coincidental.\n\n" +
        "By clicking \"I am 18+\", you explicitly confirm that you are at least 18 years old (or the legal age in your country) and consent to view adult content.\n\n" +
        "Terms Agreement\n" +
        "By entering the game, you agree to these terms and acknowledge that the game saves your progress locally on your device. No personal information is collected.";

    private const string DEFAULT_AGREE_TEXT = "I am 18+";
    private const string DEFAULT_EXIT_TEXT = "Exit Game";

    // ═══════════════════════════════════════════════════════════
    // ░ INITIALIZATION
    // ═══════════════════════════════════════════════════════════

    private void Awake()
    {
        // Check if player already accepted consent
        if (HasAcceptedConsent())
        {
            Debug.Log("[ConsentManager] Consent already accepted - proceeding to Bootstrap");
            LoadBootstrap();
            return;
        }

        Debug.Log("[ConsentManager] Showing consent screen");

        // Setup UI
        InitializeUI();

        // Setup button listeners
        SetupButtons();
    }

    private void InitializeUI()
    {
        // Ensure panel is visible
        if (consentPanel != null)
            consentPanel.SetActive(true);

        // Set texts using constants
        if (titleText != null)
            titleText.text = DEFAULT_TITLE;

        if (consentText != null)
            consentText.text = useCustomText && !string.IsNullOrEmpty(customConsentText)
                ? customConsentText
                : DEFAULT_CONSENT_TEXT;

        if (agreeText != null)
            agreeText.text = DEFAULT_AGREE_TEXT;

        if (exitText != null)
            exitText.text = DEFAULT_EXIT_TEXT;
    }

    private void SetupButtons()
    {
        if (buttonAgree != null)
            buttonAgree.onClick.AddListener(OnAgreeClicked);
        else
            Debug.LogError("[ConsentManager] ButtonAgree not assigned!");

        if (buttonExit != null)
            buttonExit.onClick.AddListener(OnExitClicked);
        else
            Debug.LogError("[ConsentManager] ButtonExit not assigned!");
    }

    // ═══════════════════════════════════════════════════════════
    // ░ BUTTON CALLBACKS
    // ═══════════════════════════════════════════════════════════

    private void OnAgreeClicked()
    {
        Debug.Log("[ConsentManager] Player accepted consent");

        // Optional: play button sound
        // AudioManager.Instance?.PlayButtonClick();

        SaveConsent();
        LoadBootstrap();
    }

    private void OnExitClicked()
    {
        Debug.Log("[ConsentManager] Player declined consent - exiting game");

        // Optional: play button sound
        // AudioManager.Instance?.PlayButtonClick();

        QuitGame();
    }

    // ═══════════════════════════════════════════════════════════
    // ░ PERSISTENCE (PlayerPrefs)
    // ═══════════════════════════════════════════════════════════

    private bool HasAcceptedConsent()
    {
        return PlayerPrefs.GetInt(CONSENT_KEY, 0) == 1;
    }

    private void SaveConsent()
    {
        PlayerPrefs.SetInt(CONSENT_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("[ConsentManager] Consent saved - will not show again");
    }

    // ═══════════════════════════════════════════════════════════
    // ░ SCENE MANAGEMENT
    // ═══════════════════════════════════════════════════════════

    private void LoadBootstrap()
    {
        Debug.Log($"[ConsentManager] Loading {bootstrapSceneName}...");
        SceneManager.LoadScene(bootstrapSceneName);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("[ConsentManager] Stopping play mode (Editor)");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Debug.Log("[ConsentManager] Quitting application");
        Application.Quit();
#endif
    }

    // ═══════════════════════════════════════════════════════════
    // ░ DEBUG TOOLS (Editor Only)
    // ═══════════════════════════════════════════════════════════

#if UNITY_EDITOR
    [ContextMenu("Debug/Clear Consent (Show Again)")]
    private void DebugClearConsent()
    {
        PlayerPrefs.DeleteKey(CONSENT_KEY);
        PlayerPrefs.Save();
        Debug.Log("✅ Consent cleared - will show on next launch");
    }

    [ContextMenu("Debug/Set Consent Accepted")]
    private void DebugSetConsent()
    {
        SaveConsent();
        Debug.Log("✅ Consent set to accepted - will skip on next launch");
    }

    [ContextMenu("Debug/Check Consent Status")]
    private void DebugCheckConsent()
    {
        bool accepted = HasAcceptedConsent();
        Debug.Log($"Consent Status: {(accepted ? "ACCEPTED ✅" : "NOT ACCEPTED ❌")}");
    }

    [ContextMenu("Debug/Print All UI References")]
    private void DebugPrintReferences()
    {
        Debug.Log($"UI REFERENCES:\n" +
                  $"Consent Panel: {(consentPanel != null ? "✅" : "❌ MISSING")}\n" +
                  $"Button Agree: {(buttonAgree != null ? "✅" : "❌ MISSING")}\n" +
                  $"Button Exit: {(buttonExit != null ? "✅" : "❌ MISSING")}\n" +
                  $"Consent Text: {(consentText != null ? "✅" : "❌ MISSING")}\n" +
                  $"Title Text: {(titleText != null ? "✅" : "❌ MISSING")}");
    }

    [ContextMenu("Debug/Simulate Agree Click")]
    private void DebugSimulateAgree() => OnAgreeClicked();

    [ContextMenu("Debug/Simulate Exit Click")]
    private void DebugSimulateExit()
    {
        Debug.Log("⚠️ Exit simulated in Editor - would quit in build");
        OnExitClicked();
    }
#endif
}
