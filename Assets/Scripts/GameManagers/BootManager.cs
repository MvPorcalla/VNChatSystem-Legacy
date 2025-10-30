//=====================================
// BootManager.cs
//=====================================

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using ChatDialogueSystem;

public class BootManager : MonoBehaviour
{
    // ================================
    // Constants
    // ================================
    private const string PREFS_HAS_SEEN_CUTSCENE = "HasSeenCutscene";
    private const string SCENE_LOCKSCREEN = "03_Lockscreen";
    private const string SCENE_CUTSCENE = "02_Cutscene";

    // ================================
    // Settings
    // ================================
    [Header("Cutscene Settings")]
    [Tooltip("If disabled, all players go directly to lockscreen (cutscene scene still in build but never loads)")]
    [SerializeField] private bool enableCutscene = true;

    // ================================
    // Editor / Debug
    // ================================
#if UNITY_EDITOR
    [Header("Debug (Editor Only)")]
    [SerializeField] private bool forceSkipInEditor = false;
#endif

    // ================================
    // Unity Methods
    // ================================
    private IEnumerator Start()
    {
        Debug.Log("[BootManager] Starting boot sequence...");

        float timeout = 5f;
        float elapsed = 0f;

        // Wait for GameManager to initialize (with timeout)
        while ((GameManager.Instance == null || !GameManager.Instance.isInitialized) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Check if initialization succeeded
        if (GameManager.Instance == null || !GameManager.Instance.isInitialized)
        {
            Debug.LogError("[BootManager] GameManager failed to initialize within timeout!");
            yield break;
        }

        Debug.Log("[BootManager] GameManager initialized");

        // Rest of your code...
        PlayerSaveManager profileManager = GameManager.Instance.ProfileManager;
        if (profileManager == null)
        {
            Debug.LogError("[BootManager] ProfileManager is null! Cannot proceed.");
            yield break;
        }

        // Decide which scene to load
        if (ShouldShowCutscene())
        {
            Debug.Log("[BootManager] First time player - loading cutscene");
            SetCutsceneSeen(true);
            LoadScene(SCENE_CUTSCENE);
        }
        else
        {
            Debug.Log("[BootManager] Loading lockscreen");
            LoadScene(SCENE_LOCKSCREEN);
        }
    }

    // ================================
    // Cutscene Logic
    // ================================
    private bool ShouldShowCutscene()
    {
        if (!enableCutscene)
        {
            Debug.Log("[BootManager] Cutscene disabled - skipping");
            return false;
        }

#if UNITY_EDITOR
        // Check for editor debug override
        if (forceSkipInEditor)
        {
            Debug.Log("[BootManager] Editor force skip enabled - skipping cutscene");
            return false;
        }
#endif

        // Check if player has already seen the cutscene
        if (HasSeenCutscene())
        {
            Debug.Log("[BootManager] Player already saw cutscene - skipping");
            return false;
        }

        return true;
    }

    private bool HasSeenCutscene()
    {
        return PlayerPrefs.GetInt(PREFS_HAS_SEEN_CUTSCENE, 0) == 1;
    }

    private void SetCutsceneSeen(bool seen)
    {
        PlayerPrefs.SetInt(PREFS_HAS_SEEN_CUTSCENE, seen ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[BootManager] Cutscene flag set to: {seen}");
    }

    // ================================
    // Scene Handling
    // ================================
    private void LoadScene(string sceneName)
    {
        Debug.Log($"[BootManager] Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    // ================================
    // Editor Debug Commands
    // ================================
#if UNITY_EDITOR
    [ContextMenu("Debug: Reset Cutscene Flag")]
    private void DebugResetCutscene()
    {
        SetCutsceneSeen(false);
        Debug.Log("[BootManager] Cutscene flag reset - player will see cutscene on next boot");
    }

    [ContextMenu("Debug: Force Skip Cutscene")]
    private void DebugSkipCutscene()
    {
        SetCutsceneSeen(true);
        Debug.Log("[BootManager] Cutscene flag set - player will go to lockscreen on next boot");
    }

    [ContextMenu("Debug: Toggle Cutscene Feature")]
    private void DebugToggleCutscene()
    {
        enableCutscene = !enableCutscene;
        Debug.Log($"[BootManager] Cutscene feature: {(enableCutscene ? "ENABLED" : "DISABLED")}");
    }
#endif
}