using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper component that connects a button to SceneNavManager.
/// Attach this to any GameObject with a Button component.
/// No coding required - just set the action in the Inspector.
/// </summary>
[RequireComponent(typeof(Button))]
public class SceneNavButton : MonoBehaviour
{
    public enum NavigationAction
    {
        GoToLockscreen,
        GoToHomeScreen,
        GoToChatApp,
        ReloadCurrentScene,
        ExitApplication,
        CustomScene
    }

    [Header("Navigation Setup")]
    [SerializeField] private NavigationAction action = NavigationAction.GoToHomeScreen;
    
    [Header("Custom Scene (only if action = CustomScene)")]
    [SerializeField] private string customSceneName;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        
        if (button == null)
        {
            Debug.LogError("[SceneNavButton] No Button component found!", this);
            return;
        }

        button.onClick.AddListener(OnButtonClick);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }

    private void OnButtonClick()
    {
        if (SceneNavManager.Instance == null)
        {
            Debug.LogError("[SceneNavButton] SceneNavManager instance not found! Make sure it exists in your Bootstrap scene.");
            return;
        }

        switch (action)
        {
            case NavigationAction.GoToLockscreen:
                SceneNavManager.Instance.GoToLockscreen();
                break;

            case NavigationAction.GoToHomeScreen:
                SceneNavManager.Instance.GoToHomeScreen();
                break;

            case NavigationAction.GoToChatApp:
                SceneNavManager.Instance.GoToChatApp();
                break;

            case NavigationAction.ReloadCurrentScene:
                SceneNavManager.Instance.ReloadCurrentScene();
                break;

            case NavigationAction.ExitApplication:
                SceneNavManager.Instance.ExitApplication();
                break;

            case NavigationAction.CustomScene:
                if (string.IsNullOrEmpty(customSceneName))
                {
                    Debug.LogError("[SceneNavButton] Custom scene name is empty!", this);
                    return;
                }
                SceneNavManager.Instance.GoToScene(customSceneName);
                break;

            default:
                Debug.LogWarning($"[SceneNavButton] Unhandled action: {action}", this);
                break;
        }
    }
}