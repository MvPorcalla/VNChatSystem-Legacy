//=====================================
// CGBubble.cs
//=====================================

using UnityEngine;
using UnityEngine.UI;

namespace ChatDialogueSystem
{
    /// <summary>
    /// Helper component for CG message bubbles that maintains a self-reference to the content image.
    /// Implements IPoolableResource for proper cleanup when recycled.
    /// 
    /// CRITICAL: Only attach to CG prefabs (NpcCGContainer, PlayerCGContainer).
    /// DO NOT attach to text bubbles - they don't need dynamic image clearing.
    /// </summary>
    public class CGBubble : MonoBehaviour, IPoolableResource
    {
        [Header("CG Image Reference")]
        [Tooltip("Drag the child CONTENT Image component (NpcImage/PlayerImage) here - NOT the bubble background!")]
        public Image cgImage;

        private void Awake()
        {
            // Fallback: auto-find if not assigned
            if (cgImage == null)
            {
                var images = GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    // Skip the bubble background (usually named "Bubble" or on root)
                    if (img.gameObject != gameObject && !img.name.Contains("Bubble"))
                    {
                        cgImage = img;
                        Debug.LogWarning($"CGBubble: Auto-assigned cgImage to {img.name}");
                        break;
                    }
                }

                if (cgImage == null)
                {
                    Debug.LogError($"CGBubble: No suitable Image component found in {name}");
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ IPoolableResource Implementation
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Called by PoolingManager when this bubble is recycled back to the pool.
        /// ALWAYS clears the sprite - no complex logic, no edge cases.
        /// </summary>
        public void OnRecycle()
        {
            if (cgImage != null)
            {
                // Clear sprite but DON'T disable the Image component
                cgImage.sprite = null;

                // Reset CanvasGroup alpha if it exists
                var canvasGroup = cgImage.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }
            }
        }

        /// <summary>
        /// Called by PoolingManager when this bubble is reused from the pool.
        /// Ensures clean state before new content loads.
        /// </summary>
        public void OnReuse()
        {
            if (cgImage != null)
            {
                // Double-check sprite is cleared (paranoid but safe)
                cgImage.sprite = null;
            }
        }
    }
}