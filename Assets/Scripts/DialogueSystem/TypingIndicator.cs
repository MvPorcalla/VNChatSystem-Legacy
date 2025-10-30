//=====================================
// TypingIndicator.cs
//=====================================

using UnityEngine;
using TMPro;
using System.Collections;
using static ChatDialogueSystem.DebugHelper;

namespace ChatDialogueSystem
{
    /// <summary>
    /// Displays an animated typing indicator (e.g., "• • •") for NPC messages.
    /// Automatically starts/stops animation when enabled/disabled (pool-safe).
    /// </summary>
    public class TypingIndicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI dotsText;
        
        [Header("Animation Settings")]
        [SerializeField] private float animationSpeed = 0.5f;
        [SerializeField] private string[] dotPatterns = { "•", "• •", "• • •" };
        
        private Coroutine animationCoroutine;
        private bool isInitialized = false;

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Sets a custom static text instead of animated dots.
        /// Stops animation if running.
        /// </summary>
        public void SetTypingText(string customText)
        {
            StopAnimation();
            
            if (dotsText != null)
            {
                dotsText.text = customText;
                Log(Category.UI, $"[TypingIndicator] Custom text set: '{customText}'");
            }
        }

        /// <summary>
        /// Manually restarts the animation (usually automatic via OnEnable).
        /// </summary>
        public void RestartAnimation()
        {
            if (!isInitialized)
            {
                LogWarning(Category.UI, "[TypingIndicator] Cannot restart - not initialized");
                return;
            }

            StopAnimation();
            StartAnimation();
        }

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        private void Awake()
        {
            ValidateReferences();
            ValidateSettings();
            isInitialized = true;
        }

        private void ValidateReferences()
        {
            if (dotsText == null)
            {
                dotsText = GetComponentInChildren<TextMeshProUGUI>();
                
                if (dotsText == null)
                {
                    LogError(Category.UI, $"[TypingIndicator] No TextMeshProUGUI found on '{gameObject.name}'");
                    isInitialized = false;
                    return;
                }
                else
                {
                    Log(Category.UI, $"[TypingIndicator] Auto-found TextMeshProUGUI on '{gameObject.name}'");
                }
            }
        }

        private void ValidateSettings()
        {
            if (dotPatterns == null || dotPatterns.Length == 0)
            {
                LogWarning(Category.UI, "[TypingIndicator] dotPatterns is empty - using default");
                dotPatterns = new string[] { "•", "• •", "• • •" };
            }

            if (animationSpeed <= 0f)
            {
                LogWarning(Category.UI, $"[TypingIndicator] Invalid animationSpeed ({animationSpeed}) - using 0.5s");
                animationSpeed = 0.5f;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ ANIMATION CONTROL
        // ═══════════════════════════════════════════════════════════

        private void StartAnimation()
        {
            if (!isInitialized || dotsText == null)
            {
                LogWarning(Category.UI, "[TypingIndicator] Cannot start animation - invalid state");
                return;
            }

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            animationCoroutine = StartCoroutine(AnimateDots());
            Log(Category.UI, "[TypingIndicator] Animation started");
        }

        private void StopAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
                Log(Category.UI, "[TypingIndicator] Animation stopped");
            }
        }

        private IEnumerator AnimateDots()
        {
            int currentPattern = 0;
            
            while (true)
            {
                // Defensive null check (in case destroyed mid-animation)
                if (dotsText == null)
                {
                    LogWarning(Category.UI, "[TypingIndicator] dotsText became null during animation");
                    yield break;
                }

                dotsText.text = dotPatterns[currentPattern];
                currentPattern = (currentPattern + 1) % dotPatterns.Length;
                
                yield return new WaitForSeconds(animationSpeed);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ LIFECYCLE (Pool-Safe)
        // ═══════════════════════════════════════════════════════════

        private void OnEnable()
        {
            if (!isInitialized)
            {
                // OnEnable can fire before Awake in some Unity scenarios
                ValidateReferences();
                ValidateSettings();
                isInitialized = true;
            }

            StartAnimation();
        }

        private void OnDisable()
        {
            // Clean up when pooled/hidden
            StopAnimation();
        }

        private void OnDestroy()
        {
            // Final cleanup
            StopAnimation();
        }
    }
}