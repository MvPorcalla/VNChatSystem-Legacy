//=====================================
// ChatTimingController.cs
//=====================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;
using static ChatDialogueSystem.DebugHelper;

namespace ChatDialogueSystem
{
    public class ChatTimingController : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ░ CONSTANTS
        // ═══════════════════════════════════════════════════════════

        private const string FAST_MODE_PREF_KEY = "ChatFastMode";
        private const float PLAYER_SEQUENCE_FINAL_DELAY = 0.5f;

        // ═══════════════════════════════════════════════════════════
        // ░ SERIALIZED FIELDS
        // ═══════════════════════════════════════════════════════════

        [Header("Timing Settings")]
        public float messageDelay = 1.2f;
        public float typingIndicatorDuration = 1.5f;
        public float playerMessageDelay = 0.3f;
        public float finalDelayBeforeChoices = 0.2f;

        [Header("Fast Mode")]
        [Tooltip("Enable fast mode to skip typing indicators and reduce delays")]
        public bool isFastMode = false;
        [Tooltip("Delay between messages in fast mode")]
        public float fastModeSpeed = 0.1f;

        [Header("Fast Mode Toggle UI")]
        [Tooltip("Button to toggle fast mode on/off")]
        [SerializeField] private Button fastModeToggleButton;
        [Tooltip("Icon image that changes based on mode")]
        [SerializeField] private Image toggleIcon;
        [Tooltip("Icon shown in normal speed mode")]
        [SerializeField] private Sprite normalSpeedIcon;
        [Tooltip("Icon shown in fast speed mode")]
        [SerializeField] private Sprite fastSpeedIcon;
        [Tooltip("Optional: Tint color for normal mode")]
        [SerializeField] private Color normalModeColor = Color.white;
        [Tooltip("Optional: Tint color for fast mode")]
        [SerializeField] private Color fastModeColor = Color.white;

        [Header("Typing Indicator")]
        public GameObject typingIndicatorPrefab;

        // ═══════════════════════════════════════════════════════════
        // ░ PRIVATE FIELDS
        // ═══════════════════════════════════════════════════════════

        private Queue<MessageData> messageQueue = new Queue<MessageData>();
        private bool isDisplayingMessages = false;
        private Coroutine currentMessageSequence;
        private Coroutine currentPlayerSequence;

        private ChatManager chatManager;

        // Callback management
        private System.Action pendingCallback = null;
        private bool isSequenceCancelled = false;

        // Active typing indicator tracking
        private GameObject activeTypingIndicator = null;

        // ═══════════════════════════════════════════════════════════
        // ░ PROPERTIES
        // ═══════════════════════════════════════════════════════════

        public bool IsDisplayingMessages => isDisplayingMessages;

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        private void Awake()
        {
            chatManager = GetComponent<ChatManager>();
            LoadFastModePreference();
        }

        private void Start()
        {
            SetupToggleButton();
            UpdateToggleVisuals();
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Queues NPC messages for timed display with typing indicators and delays.
        /// Messages are displayed sequentially with appropriate timing between them.
        /// </summary>
        /// <param name="messages">List of messages to display</param>
        /// <param name="onComplete">Callback invoked when all messages are displayed</param>
        public void QueueMessages(List<MessageData> messages, System.Action onComplete = null)
        {
            if (!ValidateMessageQueue(messages, onComplete))
                return;

            Log(Category.TimedMessages, $"Queueing {messages.Count} messages (Fast Mode: {isFastMode})");

            StopCurrentSequenceIfRunning();
            ResetSequenceState();

            pendingCallback = onComplete;

            EnqueueMessages(messages);

            if (messageQueue.Count > 0)
            {
                currentMessageSequence = StartCoroutine(DisplayMessagesSequence());
            }
            else
            {
                Log(Category.TimedMessages, "No messages to display, calling onComplete immediately");
                InvokeCallbackSafely();
            }
        }

        /// <summary>
        /// Queues player messages for display. Usually shown instantly or with minimal delay.
        /// Player messages don't show typing indicators.
        /// </summary>
        /// <param name="messages">List of player messages to display</param>
        /// <param name="onComplete">Callback invoked when all messages are displayed</param>
        public void QueuePlayerMessages(List<MessageData> messages, System.Action onComplete = null)
        {
            Log(Category.TimedMessages, $"Queueing {messages.Count} player messages (Fast Mode: {isFastMode})");

            if (currentPlayerSequence != null)
            {
                StopCoroutine(currentPlayerSequence);
                Log(Category.TimedMessages, "Stopped previous player message sequence");
            }

            ResetSequenceState();
            pendingCallback = onComplete;

            currentPlayerSequence = StartCoroutine(DisplayPlayerMessagesSequence(messages));
        }

        /// <summary>
        /// Stops the current message sequence and cancels any pending callbacks.
        /// Call this when switching contacts or interrupting dialogue flow.
        /// </summary>
        public void StopCurrentSequence()
        {
            Log(Category.TimedMessages, "Stopping current message sequence and cancelling callbacks");

            isSequenceCancelled = true;

            var callbackToCancel = pendingCallback;
            pendingCallback = null;

            StopAllSequenceCoroutines();
            CleanupTypingIndicator();
            ClearMessageQueue();

            isDisplayingMessages = false;

            Log(Category.TimedMessages, $"Sequence stopped. Callback {(callbackToCancel != null ? "cancelled" : "was already null")}");
        }

        /// <summary>
        /// Performs full cleanup when switching contacts.
        /// Ensures no orphaned resources remain.
        /// </summary>
        public void FullCleanup()
        {
            StopCurrentSequence();

            isSequenceCancelled = false;
            isDisplayingMessages = false;

            Log(Category.TimedMessages, "Full cleanup completed");
        }

        /// <summary>
        /// Checks if the timing controller is ready to accept new input.
        /// Returns true when no sequences are running.
        /// </summary>
        public bool IsReadyForInput()
        {
            return !isDisplayingMessages
                && currentMessageSequence == null
                && currentPlayerSequence == null;
        }

        /// <summary>
        /// Force completes the current message sequence (Editor only).
        /// Useful for debugging and testing.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void ForceCompleteSequence()
        {
            if (isDisplayingMessages)
            {
                StopCurrentSequence();
                Log(Category.TimedMessages, "Force completed message sequence");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ FAST MODE MANAGEMENT
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Programmatically sets fast mode on/off.
        /// Updates UI toggle visuals and saves preference.
        /// </summary>
        /// <param name="enabled">True to enable fast mode, false for normal speed</param>
        public void SetFastMode(bool enabled)
        {
            isFastMode = enabled;
            UpdateToggleVisuals();
            SaveFastModePreference();
            Log(Category.TimedMessages, $"Fast mode set to: {(enabled ? "ENABLED" : "DISABLED")}");
        }

        private void SetupToggleButton()
        {
            if (fastModeToggleButton != null)
            {
                fastModeToggleButton.onClick.AddListener(OnToggleButtonClicked);
                Log(Category.TimedMessages, "Fast mode toggle button setup complete");
            }
            else
            {
                LogWarning(Category.TimedMessages, "Fast mode toggle button not assigned in Inspector");
            }
        }

        private void OnToggleButtonClicked()
        {
            isFastMode = !isFastMode;
            UpdateToggleVisuals();
            SaveFastModePreference();
            Log(Category.TimedMessages, $"Fast mode toggled to: {(isFastMode ? "ENABLED" : "DISABLED")}");
        }

        private void UpdateToggleVisuals()
        {
            if (toggleIcon == null) return;

            if (isFastMode && fastSpeedIcon != null)
            {
                toggleIcon.sprite = fastSpeedIcon;
            }
            else if (!isFastMode && normalSpeedIcon != null)
            {
                toggleIcon.sprite = normalSpeedIcon;
            }

            toggleIcon.color = isFastMode ? fastModeColor : normalModeColor;
        }

        private void LoadFastModePreference()
        {
            isFastMode = PlayerPrefs.GetInt(FAST_MODE_PREF_KEY, 0) == 1;
            Log(Category.TimedMessages, $"Loaded fast mode preference: {(isFastMode ? "ENABLED" : "DISABLED")}");
        }

        private void SaveFastModePreference()
        {
            PlayerPrefs.SetInt(FAST_MODE_PREF_KEY, isFastMode ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ═══════════════════════════════════════════════════════════
        // ░ MESSAGE PROCESSING COROUTINES
        // ═══════════════════════════════════════════════════════════

        private IEnumerator DisplayMessagesSequence()
        {
            isDisplayingMessages = true;
            Log(Category.TimedMessages, "Starting message display sequence");

            while (messageQueue.Count > 0)
            {
                if (isSequenceCancelled)
                {
                    Log(Category.TimedMessages, "Message sequence aborted due to cancellation");
                    break;
                }

                var message = messageQueue.Dequeue();
                LogMessageProcessing(message);

                if (!isFastMode && ShouldShowTypingIndicator(message))
                {
                    yield return StartCoroutine(ShowTypingIndicatorSequence());
                    
                    if (isSequenceCancelled)
                        break;
                }
                else
                {
                    Log(Category.TimedMessages, isFastMode ? "Fast mode: skipping typing indicator" : "No typing indicator needed");
                }

                yield return StartCoroutine(CreateAndDisplayMessageWithPreCalc(message));

                if (messageQueue.Count > 0)
                {
                    yield return new WaitForSeconds(GetMessageDelay());
                }
            }

            if (!isSequenceCancelled)
            {
                if (!isFastMode && finalDelayBeforeChoices > 0)
                {
                    Log(Category.TimedMessages, $"Final delay of {finalDelayBeforeChoices}s");
                    yield return new WaitForSeconds(finalDelayBeforeChoices);
                }

                isDisplayingMessages = false;
                Log(Category.TimedMessages, "Message display sequence completed");
                InvokeCallbackSafely();
            }
            else
            {
                isDisplayingMessages = false;
                Log(Category.TimedMessages, "Message sequence cancelled - callback suppressed");
            }
        }

        private IEnumerator DisplayPlayerMessagesSequence(List<MessageData> messages)
        {
            Log(Category.TimedMessages, $"Displaying {messages.Count} player messages");

            for (int i = 0; i < messages.Count; i++)
            {
                if (isSequenceCancelled)
                {
                    Log(Category.TimedMessages, "Player message sequence aborted");
                    break;
                }

                yield return StartCoroutine(CreateAndDisplayMessageWithPreCalc(messages[i]));

                if (i < messages.Count - 1)
                {
                    yield return new WaitForSeconds(GetPlayerMessageDelay());
                }
            }

            if (!isSequenceCancelled)
            {
                if (!isFastMode)
                {
                    yield return new WaitForSeconds(PLAYER_SEQUENCE_FINAL_DELAY);
                }
                Log(Category.TimedMessages, "Player messages display completed");
                InvokeCallbackSafely();
            }
            else
            {
                Log(Category.TimedMessages, "Player message sequence cancelled - callback suppressed");
            }

            currentPlayerSequence = null;
        }

        private IEnumerator ShowTypingIndicatorSequence()
        {
            Log(Category.TimedMessages, "Showing typing indicator");

            GameObject typingBubble = ShowTypingIndicator();

            yield return new WaitForSeconds(typingIndicatorDuration);

            if (isSequenceCancelled)
            {
                if (typingBubble != null)
                    chatManager.poolingManager.Recycle(typingBubble);

                activeTypingIndicator = null;
                yield break;
            }

            if (typingBubble != null)
                chatManager.poolingManager.Recycle(typingBubble);

            activeTypingIndicator = null;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ BUBBLE CREATION & VALIDATION
        // ═══════════════════════════════════════════════════════════

        private IEnumerator CreateAndDisplayMessageWithPreCalc(MessageData message)
        {
            if (isSequenceCancelled)
            {
                Log(Category.TimedMessages, "Message creation aborted due to cancellation");
                yield break;
            }

            string preview = GetMessagePreview(message.content, 30);
            Log(Category.TimedMessages, $"Creating message bubble for: {preview}");

            GameObject bubble = CreateBubbleForMessage(message);

            if (bubble != null)
            {
                bubble.SetActive(false);
                ConfigureBubble(bubble, message);

                if (message.type == MessageData.MessageType.Image)
                {
                    yield return StartCoroutine(LoadImageForBubble(bubble, message.imagePath));

                    if (isSequenceCancelled)
                    {
                        chatManager.poolingManager.Recycle(bubble);
                        yield break;
                    }
                }

                yield return StartCoroutine(PerformLayoutCalculations(bubble));

                if (!isSequenceCancelled && ValidateBubbleState(bubble))
                {
                    bubble.SetActive(true);
                    yield return new WaitForEndOfFrame();

                    chatManager.OnNewMessageDisplayed(message);

                    Log(Category.TimedMessages, "Message bubble displayed");
                }
                else
                {
                    if (isSequenceCancelled)
                    {
                        Log(Category.TimedMessages, "Bubble activation cancelled");
                    }
                    else
                    {
                        LogWarning(Category.TimedMessages, "Bubble invalidated - skipping activation");
                    }

                    if (bubble != null && bubble.transform.parent != null)
                    {
                        chatManager.poolingManager.Recycle(bubble);
                    }
                }
            }
        }

        private GameObject CreateBubbleForMessage(MessageData message)
        {
            string speaker = message.speaker.ToLower();
            GameObject bubble = null;

            switch (message.type)
            {
                case MessageData.MessageType.System:
                    bubble = chatManager.RequestMessageBubble(chatManager.GetSystemMessagePrefab());
                    break;

                case MessageData.MessageType.Text:
                    if (speaker == "player")
                        bubble = chatManager.RequestMessageBubble(chatManager.GetPlayerTextBubblePrefab());
                    else
                        bubble = chatManager.RequestMessageBubble(chatManager.GetNPCTextBubblePrefab());
                    break;

                case MessageData.MessageType.Image:
                    if (speaker == "player")
                        bubble = chatManager.RequestMessageBubble(chatManager.GetPlayerImageBubblePrefab());
                    else
                        bubble = chatManager.RequestMessageBubble(chatManager.GetNPCImageBubblePrefab());
                    break;
            }

            return bubble;
        }

        private IEnumerator LoadImageForBubble(GameObject bubble, string imagePath)
        {
            var cgBubble = bubble.GetComponent<CGBubble>();
            if (cgBubble != null && cgBubble.cgImage != null)
            {
                Log(Category.TimedMessages, $"Delegating image load to DisplayManager: {imagePath}");
                yield return StartCoroutine(LoadImageViaDisplayManager(cgBubble.cgImage, imagePath));
            }
        }

        private IEnumerator LoadImageViaDisplayManager(Image imageComponent, string imagePath)
        {
            Log(Category.Addressables, $"Loading image via DisplayManager: {imagePath}");

            var handle = Addressables.LoadAssetAsync<Sprite>(imagePath);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                imageComponent.sprite = handle.Result;

                var rectTransform = imageComponent.transform.parent as RectTransform;
                if (rectTransform != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                }

                chatManager.StoreImageHandle(imagePath, handle);
            }
            else
            {
                LogWarning(Category.Addressables, $"Failed to load image: {imagePath}");
            }
        }

        private bool ValidateBubbleState(GameObject bubble)
        {
            if (bubble == null)
            {
                LogWarning(Category.TimedMessages, "Bubble is null");
                return false;
            }

            if (bubble.transform.parent == null)
            {
                LogWarning(Category.TimedMessages, "Bubble has no parent (was recycled)");
                return false;
            }

            if (bubble.transform.parent != chatManager.ChatContent)
            {
                LogWarning(Category.TimedMessages, "Bubble parent changed unexpectedly");
                return false;
            }

            return true;
        }

        private void ConfigureBubble(GameObject bubble, MessageData message)
        {
            if (message.type == MessageData.MessageType.Text ||
                message.type == MessageData.MessageType.System)
            {
                var textComponent = bubble.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    var autoResize = textComponent.GetComponent<AutoResizeText>();
                    if (autoResize != null)
                    {
                        autoResize.SetText(message.content);
                    }
                    else
                    {
                        textComponent.text = message.content;
                    }
                }
            }
        }

        private IEnumerator PerformLayoutCalculations(GameObject bubble)
        {
            bool wasActive = bubble.activeInHierarchy;
            if (!wasActive)
            {
                bubble.SetActive(true);
            }

            var textComponent = bubble.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.ForceMeshUpdate();

                var autoResize = textComponent.GetComponent<AutoResizeText>();
                if (autoResize != null && !autoResize.IsInitialized)
                {
                    LogWarning(Category.TimedMessages, "AutoResizeText not initialized - forcing reinit");
                    autoResize.ForceReinitialize();
                }
            }

            var rectTransform = bubble.transform as RectTransform;
            if (rectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }

            yield return null;

            if (chatManager.ChatContent != null)
            {
                var chatRect = chatManager.ChatContent as RectTransform;
                if (chatRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(chatRect);
                }
            }

            yield return null;

            if (!wasActive)
            {
                bubble.SetActive(false);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ TYPING INDICATOR
        // ═══════════════════════════════════════════════════════════

        private bool ShouldShowTypingIndicator(MessageData message)
        {
            string speaker = message.speaker.ToLower();
            return typingIndicatorPrefab != null &&
                speaker != "player" &&
                speaker != "system" &&
                message.type == MessageData.MessageType.Text;
        }

        private GameObject ShowTypingIndicator()
        {
            if (typingIndicatorPrefab != null && chatManager != null)
            {
                var indicator = chatManager.poolingManager.Get(
                    typingIndicatorPrefab,
                    chatManager.ChatContent,
                    true
                );

                activeTypingIndicator = indicator;
                return indicator;
            }
            return null;
        }

        private void CleanupTypingIndicator()
        {
            if (activeTypingIndicator == null)
                return;

            var indicator = activeTypingIndicator;
            var manager = chatManager;

            activeTypingIndicator = null;

            if (manager == null)
            {
                Log(Category.TimedMessages, "ChatManager destroyed - cannot recycle typing indicator");
                if (indicator != null)
                {
                    Destroy(indicator);
                }
                return;
            }

            var poolManager = manager.poolingManager;
            if (poolManager == null)
            {
                LogWarning(Category.TimedMessages, "PoolingManager destroyed - cannot recycle typing indicator");
                if (indicator != null)
                {
                    Destroy(indicator);
                }
                return;
            }

            try
            {
                poolManager.Recycle(indicator);
                Log(Category.TimedMessages, "Typing indicator recycled successfully");
            }
            catch (System.Exception ex)
            {
                LogError(Category.TimedMessages, $"Failed to recycle typing indicator: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ HELPER METHODS
        // ═══════════════════════════════════════════════════════════

        private bool ValidateMessageQueue(List<MessageData> messages, System.Action onComplete)
        {
            if (messages == null || messages.Count == 0)
            {
                LogWarning(Category.TimedMessages, "QueueMessages called with null or empty message list");
                onComplete?.Invoke();
                return false;
            }
            return true;
        }

        private void StopCurrentSequenceIfRunning()
        {
            if (currentMessageSequence != null)
            {
                StopCoroutine(currentMessageSequence);
                Log(Category.TimedMessages, "Stopped previous message sequence");
            }
        }

        private void ResetSequenceState()
        {
            isSequenceCancelled = false;
        }

        private void EnqueueMessages(List<MessageData> messages)
        {
            messageQueue.Clear();
            foreach (var message in messages)
            {
                messageQueue.Enqueue(message);
            }
        }

        private void StopAllSequenceCoroutines()
        {
            if (currentMessageSequence != null)
            {
                StopCoroutine(currentMessageSequence);
                currentMessageSequence = null;
            }

            if (currentPlayerSequence != null)
            {
                StopCoroutine(currentPlayerSequence);
                currentPlayerSequence = null;
            }
        }

        private void ClearMessageQueue()
        {
            if (messageQueue != null)
            {
                int queuedCount = messageQueue.Count;
                messageQueue.Clear();

                if (queuedCount > 0)
                {
                    Log(Category.TimedMessages, $"Cleared {queuedCount} queued messages");
                }
            }
        }

        private void InvokeCallbackSafely()
        {
            if (pendingCallback != null && !isSequenceCancelled)
            {
                Log(Category.TimedMessages, "Invoking completion callback");
                var callback = pendingCallback;
                pendingCallback = null;
                callback.Invoke();
            }
            else if (isSequenceCancelled)
            {
                Log(Category.TimedMessages, "Callback suppressed due to cancellation");
                pendingCallback = null;
            }
        }

        private float GetMessageDelay()
        {
            return isFastMode ? fastModeSpeed : messageDelay;
        }

        private float GetPlayerMessageDelay()
        {
            return isFastMode ? fastModeSpeed : playerMessageDelay;
        }

        private string GetMessagePreview(string content, int maxLength)
        {
            return content.Length > maxLength
                ? content.Substring(0, maxLength) + "..."
                : content;
        }

        private void LogMessageProcessing(MessageData message)
        {
            string preview = GetMessagePreview(message.content, 50);
            Log(Category.TimedMessages, $"Processing message: {message.speaker}: {preview}");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ LIFECYCLE EVENTS
        // ═══════════════════════════════════════════════════════════

        private void OnDestroy()
        {
            if (fastModeToggleButton != null)
            {
                fastModeToggleButton.onClick.RemoveListener(OnToggleButtonClicked);
            }

            isSequenceCancelled = true;

            var callbackToDiscard = pendingCallback;
            pendingCallback = null;

            CleanupTypingIndicator();

            Log(Category.Addressables, $"ChatTimingController destroyed. Discarded callback: {callbackToDiscard != null}");
        }
    }
}