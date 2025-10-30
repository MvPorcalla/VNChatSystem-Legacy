using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Centralized scene navigation manager that persists across all scenes.
/// Place this on a GameObject in your Bootstrap/Startup scene.
/// </summary>
public class SceneNavManager : MonoBehaviour
{
    private static SceneNavManager _instance;
    public static SceneNavManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("[SceneNavManager] No instance found! Make sure it exists in your Bootstrap scene.");
            }
            return _instance;
        }
    }

    [Header("Scene Names")]
    [SerializeField] private string lockscreenScene = "03_Lockscreen";
    [SerializeField] private string homeScreenScene = "04_HomeScreen";
    [SerializeField] private string chatAppScene = "05_ChatApp";

    [Header("Transition Settings")]
    [SerializeField] private bool useTransitionEffect = false;
    [SerializeField] private float transitionDuration = 0.3f;

    private CanvasGroup fadePanel;
    private bool isTransitioning = false;

    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("[SceneNavManager] Initialized and persistent across scenes.");
    }

    #region Public Navigation Methods

    /// <summary>
    /// Navigate to the Lockscreen scene
    /// </summary>
    public void GoToLockscreen()
    {
        LoadSceneWithTransition(lockscreenScene);
    }

    /// <summary>
    /// Navigate to the Home Screen
    /// </summary>
    public void GoToHomeScreen()
    {
        LoadSceneWithTransition(homeScreenScene);
    }

    /// <summary>
    /// Navigate to the Chat App
    /// </summary>
    public void GoToChatApp()
    {
        LoadSceneWithTransition(chatAppScene);
    }

    /// <summary>
    /// Navigate to any scene by name
    /// </summary>
    public void GoToScene(string sceneName)
    {
        LoadSceneWithTransition(sceneName);
    }

    /// <summary>
    /// Reload the current active scene
    /// </summary>
    public void ReloadCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        LoadSceneWithTransition(currentScene);
    }

    /// <summary>
    /// Exit the application
    /// </summary>
    public void ExitApplication()
    {
#if UNITY_EDITOR
        Debug.Log("[SceneNavManager] Quit requested (Editor Mode).");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Debug.Log("[SceneNavManager] Quitting application...");
        Application.Quit();
#endif
    }

    #endregion

    #region Scene Loading Logic

    private void LoadSceneWithTransition(string sceneName)
    {
        if (isTransitioning)
        {
            Debug.LogWarning($"[SceneNavManager] Already transitioning to another scene. Ignoring request for {sceneName}.");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneNavManager] Scene name is null or empty!");
            return;
        }

        if (useTransitionEffect && fadePanel != null)
        {
            StartCoroutine(TransitionToScene(sceneName));
        }
        else
        {
            LoadSceneImmediate(sceneName);
        }
    }

    private void LoadSceneImmediate(string sceneName)
    {
        Debug.Log($"[SceneNavManager] Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator TransitionToScene(string sceneName)
    {
        isTransitioning = true;

        // Fade out
        yield return StartCoroutine(Fade(1f));

        // Load scene
        SceneManager.LoadScene(sceneName);

        // Fade in
        yield return StartCoroutine(Fade(0f));

        isTransitioning = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadePanel.alpha;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / transitionDuration);
            yield return null;
        }

        fadePanel.alpha = targetAlpha;
    }

    #endregion

    #region Optional: Setup Fade Panel

    /// <summary>
    /// Call this to set up a fade panel for transitions (optional).
    /// Pass a CanvasGroup that covers the full screen.
    /// </summary>
    public void SetFadePanel(CanvasGroup panel)
    {
        fadePanel = panel;
        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.blocksRaycasts = false;
        }
    }

    #endregion
}