//=====================================
// DialogueNavigationManager.cs
//=====================================
using UnityEngine;
using UnityEngine.UI;
using static ChatDialogueSystem.DebugHelper;

namespace ChatDialogueSystem
{
    /// <summary>
    /// Handles navigation within the Chat App scene.
    /// Manages switching between Contact List and Chat panels.
    /// Uses SceneNavManager for scene transitions.
    /// </summary>
    public class DialogueNavigationManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button phoneBackButton;        // From PhoneButtons
        [SerializeField] private Button chatHeaderBackButton;   // From ChatHeader
        [SerializeField] private Button homeButton;             // From PhoneButtons
        [SerializeField] private Button exitButton;             // From PhoneButtons
        [SerializeField] private GameObject chatAppPanel;
        [SerializeField] private GameObject contactListPanel;

        private ChatManager chatManager;

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        private void Awake()
        {
            chatManager = FindObjectOfType<ChatManager>();

            SetupButtons();
        }

        private void SetupButtons()
        {
            // Back buttons (in-scene navigation)
            if (phoneBackButton != null)
                phoneBackButton.onClick.AddListener(OnBackPressed);
            else
                LogWarning(Category.UI, "Phone back button not assigned!");

            if (chatHeaderBackButton != null)
                chatHeaderBackButton.onClick.AddListener(OnBackPressed);
            else
                LogWarning(Category.UI, "Chat header back button not assigned!");

            // Home button (scene transition)
            if (homeButton != null)
                homeButton.onClick.AddListener(OnHomePressed);
            else
                LogWarning(Category.UI, "Home button not assigned!");

            // Exit button (app quit)
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitPressed);
            else
                LogWarning(Category.UI, "Exit button not assigned!");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ BACK BUTTON LOGIC (In-Scene Navigation)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Handles back navigation:
        /// - If in chat → Return to contact list
        /// - If in contact list → Return to home screen
        /// </summary>
        private void OnBackPressed()
        {
            if (chatAppPanel != null && chatAppPanel.activeSelf)
            {
                // Going back from chat to contact list (in-scene)
                Log(Category.UI, "[DialogueNav] Back: Chat → Contact List");
                
                SaveChatStateIfNeeded();
                
                chatAppPanel.SetActive(false);
                contactListPanel.SetActive(true);
            }
            else if (contactListPanel != null && contactListPanel.activeSelf)
            {
                // Going back from contact list to home screen (scene transition)
                Log(Category.UI, "[DialogueNav] Back: Contact List → Home Screen");
                
                SaveChatStateIfNeeded();
                GoToHomeScreen();
            }
            else
            {
                LogWarning(Category.UI, "[DialogueNav] Back pressed but no valid panel is active!");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ HOME BUTTON LOGIC (Scene Transition)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Home button pressed - save state and return to home screen
        /// Works from any panel (contact list or chat)
        /// </summary>
        private void OnHomePressed()
        {
            Log(Category.UI, "[DialogueNav] Home button pressed");
            
            SaveChatStateIfNeeded();
            GoToHomeScreen();
        }

        // ═══════════════════════════════════════════════════════════
        // ░ EXIT BUTTON LOGIC (App Quit)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Exit button pressed - save state and quit application
        /// </summary>
        private void OnExitPressed()
        {
            Log(Category.UI, "[DialogueNav] Exit button pressed");
            
            SaveChatStateIfNeeded();
            
            // Use SceneNavManager for consistent exit behavior
            if (SceneNavManager.Instance != null)
            {
                SceneNavManager.Instance.ExitApplication();
            }
            else
            {
                // Fallback if SceneNavManager isn't available
                LogWarning(Category.UI, "SceneNavManager not found - using direct quit");
                
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ HELPER METHODS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Save current chat state if ChatManager exists and player is in a chat
        /// </summary>
        private void SaveChatStateIfNeeded()
        {
            if (chatManager != null && chatAppPanel != null && chatAppPanel.activeSelf)
            {
                Log(Category.SaveManager, "[DialogueNav] Saving chat state before navigation");
                chatManager.SaveChatState();
            }
        }

        /// <summary>
        /// Navigate to home screen using centralized SceneNavManager
        /// </summary>
        private void GoToHomeScreen()
        {
            if (SceneNavManager.Instance != null)
            {
                SceneNavManager.Instance.GoToHomeScreen();
            }
            else
            {
                // Fallback if SceneNavManager isn't available
                LogWarning(Category.UI, "SceneNavManager not found - using direct scene load");
                UnityEngine.SceneManagement.SceneManager.LoadScene("04_HomeScreen");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API (Optional - for other scripts to use)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Programmatically switch to contact list view
        /// </summary>
        public void ShowContactList()
        {
            if (chatAppPanel != null) chatAppPanel.SetActive(false);
            if (contactListPanel != null) contactListPanel.SetActive(true);
            
            Log(Category.UI, "[DialogueNav] Switched to contact list view");
        }

        /// <summary>
        /// Programmatically switch to chat view
        /// </summary>
        public void ShowChat()
        {
            if (contactListPanel != null) contactListPanel.SetActive(false);
            if (chatAppPanel != null) chatAppPanel.SetActive(true);
            
            Log(Category.UI, "[DialogueNav] Switched to chat view");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ DEBUG (Editor Only)
        // ═══════════════════════════════════════════════════════════

#if UNITY_EDITOR
        [ContextMenu("Debug/Simulate Back Press")]
        private void DebugSimulateBack()
        {
            OnBackPressed();
        }

        [ContextMenu("Debug/Simulate Home Press")]
        private void DebugSimulateHome()
        {
            OnHomePressed();
        }

        [ContextMenu("Debug/Print Current State")]
        private void DebugPrintState()
        {
            Debug.Log($"═══════════════════════════════════\n" +
                     $"DIALOGUE NAVIGATION STATE:\n" +
                     $"Chat Panel Active: {(chatAppPanel != null ? chatAppPanel.activeSelf.ToString() : "NULL")}\n" +
                     $"Contact List Active: {(contactListPanel != null ? contactListPanel.activeSelf.ToString() : "NULL")}\n" +
                     $"ChatManager Found: {(chatManager != null ? "YES" : "NO")}\n" +
                     $"SceneNavManager Available: {(SceneNavManager.Instance != null ? "YES" : "NO")}\n" +
                     $"═══════════════════════════════════");
        }
#endif
    }
}