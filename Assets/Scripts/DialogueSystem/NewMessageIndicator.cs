//=====================================
// NewMessageIndicator.cs
//=====================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using static ChatDialogueSystem.DebugHelper;

namespace ChatDialogueSystem
{
    /// <summary>
    /// Shows a floating indicator when new messages arrive while user is scrolled up.
    /// Attach to NewMessageIndicator GameObject in your chat UI.
    /// </summary>
    public class NewMessageIndicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button indicatorButton;
        [SerializeField] private TextMeshProUGUI indicatorText;
        [SerializeField] private Image indicatorIcon;

        [Header("Display Settings")]
        [SerializeField] private string singleMessageFormat = "1 new message";
        [SerializeField] private string multiMessageFormat = "{0} new messages";
        [SerializeField] private string maxMessageFormat = "99+ new messages";
        [SerializeField] private int maxDisplayCount = 99;
        [SerializeField] private int maxInternalCount = 999;

        [Header("Events")]
        public UnityEvent OnIndicatorClicked = new UnityEvent();

        private int currentNewMessageCount = 0;
        private bool isInitialized = false;

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Returns true if the indicator is currently visible
        /// </summary>
        public bool IsVisible => gameObject.activeSelf;

        /// <summary>
        /// Shows the indicator with the specified number of new messages
        /// </summary>
        /// <param name="newMessageCount">Number of new messages (must be > 0)</param>
        public void ShowIndicator(int newMessageCount)
        {
            if (!isInitialized)
            {
                LogWarning(Category.UI, "[NewMessageIndicator] ShowIndicator called before initialization");
                return;
            }

            if (newMessageCount <= 0)
            {
                LogWarning(Category.UI, "[NewMessageIndicator] ShowIndicator called with count <= 0");
                HideIndicator();
                return;
            }

            // Cap internal count to prevent overflow
            currentNewMessageCount = Mathf.Clamp(newMessageCount, 1, maxInternalCount);

            UpdateText();

            // Show if hidden
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                Log(Category.UI, $"[NewMessageIndicator] Shown with {currentNewMessageCount} messages");
            }
        }

        /// <summary>
        /// Increments the new message count by 1 and updates the display
        /// </summary>
        public void IncrementCount()
        {
            ShowIndicator(currentNewMessageCount + 1);
        }

        /// <summary>
        /// Hides the indicator and resets the count
        /// </summary>
        public void HideIndicator()
        {
            if (currentNewMessageCount > 0 || gameObject.activeSelf)
            {
                currentNewMessageCount = 0;
                gameObject.SetActive(false);
                Log(Category.UI, "[NewMessageIndicator] Hidden and reset");
            }
        }

        /// <summary>
        /// Updates the text without changing visibility or count
        /// </summary>
        public void RefreshText()
        {
            if (isInitialized && currentNewMessageCount > 0)
            {
                UpdateText();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        private void Awake()
        {
            ValidateReferences();
            SetupButton();
            
            // Hide by default
            gameObject.SetActive(false);
            
            isInitialized = true;
            Log(Category.UI, "[NewMessageIndicator] Initialized");
        }

        private void ValidateReferences()
        {
            if (indicatorButton == null)
            {
                LogError(Category.UI, "[NewMessageIndicator] indicatorButton is not assigned!");
            }

            if (indicatorText == null)
            {
                LogWarning(Category.UI, "[NewMessageIndicator] indicatorText is not assigned!");
            }

            // Icon is optional
            if (indicatorIcon == null)
            {
                Log(Category.UI, "[NewMessageIndicator] indicatorIcon is not assigned (optional)");
            }
        }

        private void SetupButton()
        {
            if (indicatorButton != null)
            {
                indicatorButton.onClick.AddListener(OnButtonClicked);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ INTERNAL LOGIC
        // ═══════════════════════════════════════════════════════════

        private void UpdateText()
        {
            if (indicatorText == null) return;

            if (currentNewMessageCount == 1)
            {
                indicatorText.text = singleMessageFormat;
            }
            else if (currentNewMessageCount > maxDisplayCount)
            {
                indicatorText.text = maxMessageFormat;
            }
            else
            {
                indicatorText.text = string.Format(multiMessageFormat, currentNewMessageCount);
            }
        }

        private void OnButtonClicked()
        {
            Log(Category.UI, "[NewMessageIndicator] Clicked");
            
            // Invoke event for ChatManager to handle
            OnIndicatorClicked?.Invoke();
            
            // Hide after clicking
            HideIndicator();
        }

        // ═══════════════════════════════════════════════════════════
        // ░ LIFECYCLE
        // ═══════════════════════════════════════════════════════════

        private void OnDestroy()
        {
            if (indicatorButton != null)
            {
                indicatorButton.onClick.RemoveListener(OnButtonClicked);
            }
        }
    }
}