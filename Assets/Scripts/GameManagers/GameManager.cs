//=====================================
// GameManager.cs
//=====================================

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using ChatDialogueSystem;

public class GameManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════
    // ░ SINGLETON
    // ═══════════════════════════════════════════════════════════

    private static GameManager _instance;
    
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                
                if (_instance == null)
                {
                    Debug.LogError("[GameManager] No instance found in scene! " +
                                 "Make sure GameManager exists in 01_Bootstrap scene.");
                }
            }
            return _instance;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ░ MANAGER REFERENCES
    // ═══════════════════════════════════════════════════════════

    public DialogueSaveManager SaveManager { get; private set; }
    public PlayerSaveManager ProfileManager { get; private set; }

    // ═══════════════════════════════════════════════════════════
    // ░ STATE
    // ═══════════════════════════════════════════════════════════

    public string currentSceneName { get; private set; }
    public bool isInitialized { get; private set; }

    // ═══════════════════════════════════════════════════════════
    // ░ INITIALIZATION
    // ═══════════════════════════════════════════════════════════

    private void Awake()
    {
        // Enforce singleton
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[GameManager] Duplicate detected! Destroying {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Track current scene immediately
        currentSceneName = SceneManager.GetActiveScene().name;

        // Subscribe to sceneLoaded event for bulletproof tracking
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Wait one frame for all Awake() calls to complete
        StartCoroutine(DelayedInitialize());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// Waits for all manager Awake() methods to complete before connecting
    /// </summary>
    private IEnumerator DelayedInitialize()
    {
        Debug.Log("[GameManager] Starting delayed initialization...");
        
        // Wait one frame to ensure all GameObject Awake() calls complete
        yield return null;
        
        Initialize();
    }

    private void Initialize()
    {
        Debug.Log("[GameManager] Initializing manager connections...");

        // Access singleton instances
        SaveManager = DialogueSaveManager.Instance;
        ProfileManager = PlayerSaveManager.Instance;

        // CRITICAL: Validate managers exist
        bool hasErrors = false;

        if (SaveManager == null)
        {
            Debug.LogError("[GameManager] ❌ DialogueSaveManager not found! " +
                          "Add DialogueSaveManager to 01_Bootstrap scene.");
            hasErrors = true;
        }
        else
        {
            Debug.Log("[GameManager] ✅ DialogueSaveManager connected");
        }

        if (ProfileManager == null)
        {
            Debug.LogError("[GameManager] ❌ PlayerSaveManager not found! " +
                          "Add PlayerSaveManager to 01_Bootstrap scene.");
            hasErrors = true;
        }
        else
        {
            Debug.Log("[GameManager] ✅ PlayerSaveManager connected");
        }

        if (hasErrors)
        {
            Debug.LogError("[GameManager] ⚠️ Initialization FAILED - missing required managers");

#if UNITY_EDITOR
            Debug.LogError("[GameManager] Stopping play mode due to critical errors.");
            UnityEditor.EditorApplication.isPlaying = false;
#endif

            return;
        }

        isInitialized = true;
        Debug.Log("[GameManager] ✅ Initialization complete - all systems ready");
    }

    // ═══════════════════════════════════════════════════════════
    // ░ SCENE MANAGEMENT
    // ═══════════════════════════════════════════════════════════

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
    }

    public void LoadScene(string sceneName)
    {
        currentSceneName = sceneName;
        Debug.Log($"[GameManager] Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    // ═══════════════════════════════════════════════════════════
    // ░ GAME STATE METHODS
    // ═══════════════════════════════════════════════════════════

    public void PauseGame()
    {
        Time.timeScale = 0f;
        Debug.Log("[GameManager] Game paused");
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        Debug.Log("[GameManager] Game resumed");
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        LoadScene("04_MainMenu");
    }

    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting game");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ═══════════════════════════════════════════════════════════
    // ░ DEBUG TOOLS
    // ═══════════════════════════════════════════════════════════

#if UNITY_EDITOR
    [ContextMenu("Debug/Print Manager Status")]
    private void DebugPrintStatus()
    {
        Debug.Log($"╔═══════════ GAMEMANAGER STATUS ═══════════╗\n" +
                 $"║ Initialized: {(isInitialized ? "YES ✅" : "NO ❌"),-30} ║\n" +
                 $"║ SaveManager: {(SaveManager != null ? "Connected ✅" : "NULL ❌"),-29} ║\n" +
                 $"║ ProfileManager: {(ProfileManager != null ? "Connected ✅" : "NULL ❌"),-26} ║\n" +
                 $"║ Current Scene: {currentSceneName,-28} ║\n" +
                 $"║ Time Scale: {Time.timeScale,-31} ║\n" +
                 $"╚══════════════════════════════════════════╝");
    }

    [ContextMenu("Debug/Force Reinitialization")]
    private void DebugForceReinit()
    {
        isInitialized = false;
        StartCoroutine(DelayedInitialize());
        Debug.Log("✅ Forced reinitialization started");
    }
#endif
}