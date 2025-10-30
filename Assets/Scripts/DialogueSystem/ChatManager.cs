//=====================================
// ChatManager.cs
//=====================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;
using static ChatDialogueSystem.DebugHelper;

namespace ChatDialogueSystem
{
    public class ChatManager : MonoBehaviour
    {
        [Header("UI Panel References")]
        public GameObject contactListPanel;
        public GameObject chatAppPanel;
        
        [Header("Contact List UI")]
        public Transform contactListContent;
        public GameObject contactButtonPrefab;
        
        [Header("Chat UI")]
        public ScrollRect chatScrollRect;
        public Transform chatContent;

        [Header("Indicator")]
        [SerializeField] private NewMessageIndicator newMessageIndicator;
        
        [Header("Chat Data")]
        public List<NPCChatData> availableChats;

        [Header("Pool Pre-warming (Optional)")]
        [Tooltip("Pre-instantiate this many message bubbles at start")]
        public int preWarmMessageCount = 10;
        [Tooltip("Pre-instantiate this many choice buttons at start")]
        public int preWarmChoiceCount = 5;

        // ═══════════════════════════════════════════════════════════
        // ░ CONSTANTS
        // ═══════════════════════════════════════════════════════════

        private const string PLAYER_SPEAKER = "player";

        // Pooling ratios
        private const float SYSTEM_MESSAGE_POOL_RATIO = 0.2f;
        private const float IMAGE_BUBBLE_POOL_RATIO = 0.2f;
        private const float PLAYER_TEXT_POOL_RATIO = 0.33f;

        // Contact switching delays. Time to ensure coroutines fully stop before proceeding.
        private const float COROUTINE_STOP_DELAY = 0.05f;

        // Memory management thresholds
        private const long HIGH_MEMORY_THRESHOLD = 100 * 1024 * 1024; // 100MB

        private ChatTimingController chatTimingController;
        private ChatAutoScroll autoScroll;
        private ChatFlowController flowController;
        private ChatDisplayManager displayManager;

        public PoolingManager poolingManager { get; private set; }
        
        public Transform ChatContent => chatContent;
                
        private NPCChatData currentChatData;
        private ChatState currentChatState;
        private Dictionary<string, DialogueNode> currentDialogueNodes;

        private Coroutine currentSwitchCoroutine;
        private float lastContactClickTime = -999f;
        private const float CONTACT_CLICK_COOLDOWN = 0.3f;

        private string currentChatID = "";
        private int unreadMessageCount = 0;

        public GameObject GetSystemMessagePrefab() => displayManager?.systemMessagePrefab;
        public GameObject GetNPCTextBubblePrefab() => displayManager?.npcTextBubblePrefab;
        public GameObject GetNPCImageBubblePrefab() => displayManager?.npcImageBubblePrefab;
        public GameObject GetPlayerTextBubblePrefab() => displayManager?.playerTextBubblePrefab;
        public GameObject GetPlayerImageBubblePrefab() => displayManager?.playerImageBubblePrefab;

        public GameObject RequestMessageBubble(GameObject prefab)
        {
            if (prefab == null)
            {
                LogError(Category.ChatManager, "Cannot create bubble from null prefab");
                return null;
            }

            if (poolingManager == null)
            {
                LogError(Category.ChatManager, "PoolingManager is null - cannot create bubble");
                return null;
            }

            if (chatContent == null)
            {
                LogError(Category.ChatManager, "chatContent is null - cannot create bubble");
                return null;
            }

            return poolingManager.Get(prefab, chatContent, false);
        }

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        private void Start()
        {
            Log(Category.ChatManager, "Initializing ChatManager...");
            InitializeComponents();
            InitializePooling();
            InitializeUI();
            PopulateContactList();
            Log(Category.ChatManager, "ChatManager initialized successfully");
        }

        private void InitializeComponents()
        {
            chatTimingController = GetComponent<ChatTimingController>();
            if (chatTimingController == null)
            {
                LogWarning(Category.ChatManager, "ChatTimingController not found. Adding automatically.");
                chatTimingController = gameObject.AddComponent<ChatTimingController>();
            }

            autoScroll = GetComponent<ChatAutoScroll>();
            if (autoScroll == null)
            {
                LogWarning(Category.ChatManager, "ChatAutoScroll not found. Adding automatically.");
                autoScroll = gameObject.AddComponent<ChatAutoScroll>();
            }
            else
            {
                autoScroll.OnScrollReachedBottom += OnScrollReachedBottom;
            }

            poolingManager = GetComponent<PoolingManager>();
            if (poolingManager == null)
            {
                LogWarning(Category.ChatManager, "PoolingManager not found. Adding automatically.");
                poolingManager = gameObject.AddComponent<PoolingManager>();
            }

            displayManager = GetComponent<ChatDisplayManager>();
            if (displayManager == null)
            {
                LogWarning(Category.ChatManager, "ChatDisplayManager not found. Adding automatically.");
                displayManager = gameObject.AddComponent<ChatDisplayManager>();
            }
            displayManager.Initialize(poolingManager);

            flowController = new ChatFlowController(this, chatTimingController);

            newMessageIndicator = GetComponentInChildren<NewMessageIndicator>(true);
            if (newMessageIndicator == null)
            {
                LogWarning(Category.ChatManager, "NewMessageIndicator not found in children");
            }
            else
            {
                newMessageIndicator.OnIndicatorClicked.AddListener(OnNewMessageIndicatorClicked);
            }
        }

        private void InitializePooling()
        {
            Log(Category.ChatManager, $"Pre-warming pools: Messages={preWarmMessageCount}, Choices={preWarmChoiceCount}");

            if (preWarmMessageCount > 0)
            {
                var systemPrefab = displayManager.GetSystemMessagePrefab();
                var npcTextPrefab = displayManager.GetNPCTextBubblePrefab();
                var npcImagePrefab = displayManager.GetNPCImageBubblePrefab();
                var playerTextPrefab = displayManager.GetPlayerTextBubblePrefab();
                var playerImagePrefab = displayManager.GetPlayerImageBubblePrefab();

                if (systemPrefab != null)
                    poolingManager.PreWarm(systemPrefab, Mathf.Max(1, Mathf.RoundToInt(preWarmMessageCount * SYSTEM_MESSAGE_POOL_RATIO)));
                if (npcTextPrefab != null)
                    poolingManager.PreWarm(npcTextPrefab, preWarmMessageCount);
                if (npcImagePrefab != null)
                    poolingManager.PreWarm(npcImagePrefab, Mathf.Max(2, Mathf.RoundToInt(preWarmMessageCount * IMAGE_BUBBLE_POOL_RATIO)));
                if (playerTextPrefab != null)
                    poolingManager.PreWarm(playerTextPrefab, Mathf.Max(3, Mathf.RoundToInt(preWarmMessageCount * PLAYER_TEXT_POOL_RATIO)));
                if (playerImagePrefab != null)
                    poolingManager.PreWarm(playerImagePrefab, 1);
            }

            if (preWarmChoiceCount > 0)
            {
                var choicePrefab = displayManager.GetChoiceButtonPrefab();
                var pausePrefab = displayManager.GetPauseButtonPrefab();
                var resetPrefab = displayManager.GetResetButtonPrefab();

                if (choicePrefab != null)
                    poolingManager.PreWarm(choicePrefab, preWarmChoiceCount);
                if (pausePrefab != null)
                    poolingManager.PreWarm(pausePrefab, 2);
                if (resetPrefab != null)
                    poolingManager.PreWarm(resetPrefab, 1);
            }
        }

        private void InitializeUI()
        {
            contactListPanel.SetActive(true);
            chatAppPanel.SetActive(false);
        }

        private void PopulateContactList()
        {
            Log(Category.ChatManager, $"Populating contact list with {availableChats.Count} contacts");

            foreach (var chatData in availableChats)
            {
                GameObject contactButton = Instantiate(contactButtonPrefab, contactListContent);
                var buttonComponent = contactButton.GetComponent<ContactButton>();
                buttonComponent.Initialize(chatData, OnContactSelected);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ CONTACT SELECTION
        // ═══════════════════════════════════════════════════════════

        private void OnContactSelected(NPCChatData chatData)
        {
            // Debounce clicks
            if (Time.realtimeSinceStartup - lastContactClickTime < CONTACT_CLICK_COOLDOWN)
            {
                LogWarning(Category.ChatManager, "Contact click too fast - ignoring");
                return;
            }
            lastContactClickTime = Time.realtimeSinceStartup;

            if (currentSwitchCoroutine != null)
            {
                LogWarning(Category.ChatManager, "Contact switch already in progress");
                return;
            }

            Log(Category.ChatManager, $"Contact selected: {chatData.characterName} (ID: {chatData.ChatID})");
            currentSwitchCoroutine = StartCoroutine(HandleContactSwitch(chatData));
        }

        private void HideNewMessageIndicator()
        {
            if (newMessageIndicator != null && newMessageIndicator.IsVisible)
            {
                newMessageIndicator.HideIndicator();
                unreadMessageCount = 0;
                Log(Category.ChatManager, "New message indicator hidden");
            }
        }

        private IEnumerator HandleContactSwitch(NPCChatData chatData)
        {
            // STEP 1: Stop message display FIRST
            if (chatTimingController != null && chatTimingController.IsDisplayingMessages)
            {
                Log(Category.ChatManager, "Stopping ongoing message sequence");
                chatTimingController.StopCurrentSequence();

                yield return null;
                yield return new WaitForSeconds(COROUTINE_STOP_DELAY);

                Log(Category.ChatManager, "Coroutines terminated");
            }

            // STEP 2: Save current chat state BEFORE switching
            if (currentChatData != null && currentChatState != null)
            {
                Log(Category.ChatManager, $"Saving state for current chat: {currentChatID}");
                SaveCurrentChatState();
            }

            // STEP 3: Check if we're switching to a different contact
            bool isSwitchingContact = (currentChatID != chatData.ChatID);

            if (isSwitchingContact)
            {
                Log(Category.ChatManager, $"Switching chat: {currentChatID} → {chatData.ChatID}");
            }

            // STEP 4: Update current chat data IMMEDIATELY
            currentChatData = chatData;
            currentChatID = chatData.ChatID;

            // STEP 5: Update UI
            contactListPanel.SetActive(false);
            chatAppPanel.SetActive(true);

            HideNewMessageIndicator();

            displayManager.SetProfileName(chatData.characterName);
            displayManager.LoadProfileImage(chatData.profileImage);

            // STEP 6: Load chat for the new character
            LoadChatForCharacter(chatData, isSwitchingContact);

            // ✅ Clear at the end
            currentSwitchCoroutine = null;
        }

        private void LoadChatForCharacter(NPCChatData chatData, bool isSwitchingContact)
        {
            Log(Category.ChatManager,
                $"LoadChatForCharacter: {chatData.characterName} (ID: {chatData.ChatID}), switching={isSwitchingContact}");

            // STEP 1: Clear UI if switching contacts
            if (isSwitchingContact)
            {
                // Use SafeClearForChatSwitch instead of AggressiveClearChatUI
                Log(Category.ChatManager, "Switching contact - performing safe cleanup");
                SafeClearForChatSwitch();
            }

            // STEP 2: Load or create chat state
            currentChatState = LoadOrCreateChatState(chatData.ChatID);
            if (currentChatState == null)
            {
                LogError(Category.ChatManager, $"Failed to load chat state for {chatData.ChatID}");
                return;
            }

            // STEP 3: Load dialogue nodes
            ChatStateValidator.ValidateChapterIndex(currentChatState, chatData);

            var currentChapter = chatData.mugiChapters[currentChatState.currentChapterIndex];
            currentDialogueNodes = MugiParser.ParseMugiFile(currentChapter);

            if (currentDialogueNodes == null || currentDialogueNodes.Count == 0)
            {
                LogError(Category.ChatManager, $"Failed to parse chapter for {chatData.characterName}");
                return;
            }

            // STEP 4: Validate state against dialogue nodes
            ChatStateValidator.ValidateState(currentChatState, chatData, currentDialogueNodes);

            Log(Category.ChatManager,
                $"Loaded state: Chapter={currentChatState.currentChapterIndex}, " +
                $"Node={currentChatState.currentNodeName}, " +
                $"Message={currentChatState.currentMessageIndex}, " +
                $"InPause={currentChatState.isInPauseState}");

            // STEP 5: Initialize flow controller with current state
            flowController.Initialize(
                currentChatData,
                currentChatState,
                currentDialogueNodes,
                currentChatID,
                currentChatState.isInPauseState
            );

            // STEP 6: Rebuild UI
            if (isSwitchingContact)
            {
                Log(Category.ChatManager, "Rebuilding chat UI after contact switch");
                RebuildChatUI();
            }
            else
            {
                if (ShouldRebuildUI())
                {
                    Log(Category.ChatManager, "Rebuilding chat UI (UI invalidated)");
                    RebuildChatUI();
                }
                else
                {
                    Log(Category.ChatManager, "Reusing existing chat UI (still valid)");
                    displayManager.SyncActiveMessages();
                }
            }

            // STEP 7: Continue dialogue from current position
            flowController.ContinueFromCurrentState();

            // STEP 8: Restore scroll position
            ForceScrollToBottom();
            HideNewMessageIndicator();
        }

        // ═══════════════════════════════════════════════════════════
        // ░ STATE MANAGEMENT (Replaces ChatStateManager)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Loads existing state or creates new one for a chat.
        /// Replaces ChatStateManager.LoadOrCreateState()
        /// </summary>
        private ChatState LoadOrCreateChatState(string chatID)
        {
            Log(Category.SaveManager, $"Loading state for chat: {chatID}");
            
            var state = DialogueSaveManager.Instance.GetChatState(chatID);
            
            if (state == null)
            {
                LogError(Category.SaveManager, $"Failed to load/create state for {chatID}");
                return null;
            }
            
            Log(Category.SaveManager, 
                $"State loaded: Chapter={state.currentChapterIndex}, " +
                $"Node={state.currentNodeName}, Message={state.currentMessageIndex}, " +
                $"HistoryCount={state.chatHistory.Count}, ReadMsgCount={state.readMessageIds.Count}");
            
            return state;
        }

        /// <summary>
        /// Saves current chat state to disk.
        /// Replaces ChatStateManager.SaveState()
        /// </summary>
        private void SaveCurrentChatState()
        {
            if (currentChatState == null || currentChatData == null)
            {
                LogWarning(Category.SaveManager, "Cannot save: currentChatState or currentChatData is null");
                return;
            }

            if (flowController == null)
            {
                LogWarning(Category.SaveManager, "Cannot save: flowController not initialized");
                return;
            }

            currentChatState.isInPauseState = flowController.IsInPauseState;
            currentChatState.characterName = currentChatData.characterName;
            
            DialogueSaveManager.Instance.SaveChatState(currentChatData.ChatID, currentChatState);
            
            Log(Category.SaveManager,
                $"Saved state for {currentChatData.ChatID}: " +
                $"Node={currentChatState.currentNodeName}, " +
                $"Message={currentChatState.currentMessageIndex}, " +
                $"Pause={currentChatState.isInPauseState}, " +
                $"HistoryCount={currentChatState.chatHistory.Count}");
        }

        /// <summary>
        /// Resets a chat to its initial state (clears save data).
        /// Replaces ChatStateManager.ResetState()
        /// </summary>
        public void ResetChatState(string chatID)
        {
            Log(Category.SaveManager, $"Resetting state for chat: {chatID}");
            DialogueSaveManager.Instance.ClearChatState(chatID);
        }

        // ═══════════════════════════════════════════════════════════
        // ░ UI COORDINATION
        // ═══════════════════════════════════════════════════════════

        public void StoreImageHandle(string imagePath, AsyncOperationHandle<Sprite> handle)
        {
            if (displayManager != null)
            {
                displayManager.StoreImageHandle(imagePath, handle);
            }
        }

        public void AggressiveClearChatUI()
        {
            Log(Category.ChatManager, "Performing aggressive chat UI cleanup with memory management");

            if (newMessageIndicator != null)
            {
                newMessageIndicator.HideIndicator();
            }
            unreadMessageCount = 0;

            if (chatTimingController != null)
            {
                chatTimingController.StopCurrentSequence();
            }

            if (displayManager != null)
            {
                displayManager.ClearLoadedImages();
                displayManager.AggressiveClear();
            }

            long currentMemory = System.GC.GetTotalMemory(false);
            if (currentMemory > HIGH_MEMORY_THRESHOLD)
            {
                Log(Category.ChatManager, $"High memory usage detected: {currentMemory / (1024 * 1024)}MB - suggesting GC");
                System.GC.Collect(0, System.GCCollectionMode.Optimized);
            }

            Log(Category.ChatManager, "Aggressive cleanup completed");
        }

        /// <summary>
        /// Safe cleanup when switching between chats (not full reset).
        /// Clears UI but KEEPS pooled objects intact for reuse.
        /// </summary>
        public void SafeClearForChatSwitch()
        {
            Log(Category.ChatManager, "Safe clearing for chat switch (preserving pools)");

            if (newMessageIndicator != null)
            {
                newMessageIndicator.HideIndicator();
            }
            unreadMessageCount = 0;

            if (chatTimingController != null)
            {
                chatTimingController.StopCurrentSequence();
            }

            if (displayManager != null)
            {
                // Only clear UI, DON'T release Addressables handles
                displayManager.ClearMessages(); // Recycles to pool
                displayManager.ClearChoices();

                // DO NOT call ClearLoadedImages() here - keeps handles cached
            }

            Log(Category.ChatManager, "Safe clear completed - pools preserved");
        }

        /// <summary>
        /// Nuclear option - destroys ALL pooled objects.
        /// Use ONLY for story resets, NOT for chat switching.
        /// </summary>
        public void NuclearReset()
        {
            Log(Category.ChatManager, "=== NUCLEAR RESET: Complete pool destruction ===");

            if (chatTimingController != null)
            {
                chatTimingController.StopCurrentSequence();
            }

            if (displayManager != null)
            {
                displayManager.ClearLoadedImages();
                displayManager.AggressiveClear();
            }

            if (poolingManager != null)
            {
                poolingManager.HardReset(); // Destroy all pooled AND active objects
            }

            if (newMessageIndicator != null)
            {
                newMessageIndicator.HideIndicator();
            }
            unreadMessageCount = 0;

            // Force garbage collection
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            Log(Category.ChatManager, "Nuclear reset complete - all pools destroyed");
        }

        private bool ShouldRebuildUI()
        {
            displayManager.SyncActiveMessages();
            
            if (currentChatState == null)
                return true;
            
            if (currentChatState.chatHistory.Count != displayManager.GetActiveMessageCount())
            {
                Log(Category.ChatManager, 
                    $"State changed: History={currentChatState.chatHistory.Count}, UI={displayManager.GetActiveMessageCount()}");
                return true;
            }
            
            return false;
        }

        private void RebuildChatUI()
        {
            // DEFENSIVE: Ensure UI is completely clear before rebuild
            displayManager.ClearAllDisplay();

            if (currentChatState.chatHistory.Count > 0)
            {
                Log(Category.ChatManager, $"Displaying {currentChatState.chatHistory.Count} historical messages");
                foreach (var message in currentChatState.chatHistory)
                {
                    DisplayMessage(message);
                }
            }

            displayManager.SyncActiveMessages();
            Log(Category.ChatManager, $"UI rebuilt: {displayManager.GetActiveMessageCount()} messages displayed");
        }

        public void DisplayMessage(MessageData message)
        {
            displayManager.DisplayMessage(message);
        }

        public void OnNewMessageDisplayed(MessageData message)
        {
            if (string.Equals(message.speaker, PLAYER_SPEAKER, StringComparison.OrdinalIgnoreCase) ||
                message.type == MessageData.MessageType.System)
                return;

            if (autoScroll != null && !autoScroll.IsAtBottom())
            {
                unreadMessageCount++;

                if (newMessageIndicator != null)
                {
                    newMessageIndicator.ShowIndicator(unreadMessageCount);
                }

                Log(Category.ChatManager, $"New message indicator shown: {unreadMessageCount} unread");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ UI INTERACTION METHODS
        // ═══════════════════════════════════════════════════════════

        public void ShowPauseButton(string buttonText, UnityEngine.Events.UnityAction callback)
        {
            displayManager.ShowPauseButton(buttonText, callback);
        }

        public void ShowChoices(List<ChoiceData> choices, Action<ChoiceData> onChoiceSelected)
        {
            displayManager.ShowChoiceButtons(choices, onChoiceSelected);
        }

        public void ShowEndResetChoice(bool isContentError, UnityEngine.Events.UnityAction callback)
        {
            displayManager.ShowResetButton(isContentError, callback);
        }

        public void ClearChoices()
        {
            displayManager.ClearChoices();
        }

        public void ForceScrollToBottom()
        {
            if (autoScroll != null)
            {
                autoScroll.ForceScrollToBottom();
            }
        }

        public void ReturnToContactList()
        {
            Log(Category.ChatManager, "Returning to contact list");

            if (chatTimingController != null)
            {
                chatTimingController.StopCurrentSequence();
            }

            currentChatData = null;
            currentChatState = null;
            currentDialogueNodes = null;
            currentChatID = "";

            unreadMessageCount = 0;
            if (newMessageIndicator != null)
            {
                newMessageIndicator.HideIndicator();
            }

            chatAppPanel.SetActive(false);
            contactListPanel.SetActive(true);

            Log(Category.ChatManager, "Returned to contact list successfully");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC SAVE API
        // ═══════════════════════════════════════════════════════════

        public void SaveChatState()
        {
            SaveCurrentChatState();
        }

        // ═══════════════════════════════════════════════════════════
        // ░ EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void OnNewMessageIndicatorClicked()
        {
            Log(Category.ChatManager, "New message indicator clicked - scrolling to bottom");

            if (autoScroll != null)
            {
                autoScroll.ScrollToBottom();
            }

            unreadMessageCount = 0;
        }

        private void OnScrollReachedBottom()
        {
            Log(Category.ChatManager, "Scroll reached bottom - hiding new message indicator");
            HideNewMessageIndicator();
        }

        // ═══════════════════════════════════════════════════════════
        // ░ LIFECYCLE EVENTS
        // ═══════════════════════════════════════════════════════════

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Log(Category.ChatManager, "Application paused - saving state");
                SaveCurrentChatState();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Log(Category.ChatManager, "Application lost focus - saving state");
                SaveCurrentChatState();
            }
        }

        private void OnEnable()
        {
            if (autoScroll != null)
            {
                autoScroll.OnScrollReachedBottom += OnScrollReachedBottom;
            }

            if (newMessageIndicator != null)
            {
                newMessageIndicator.OnIndicatorClicked.AddListener(OnNewMessageIndicatorClicked);
            }
        }

        private void OnDisable()
        {
            if (autoScroll != null)
            {
                autoScroll.OnScrollReachedBottom -= OnScrollReachedBottom;
            }

            if (newMessageIndicator != null)
            {
                newMessageIndicator.OnIndicatorClicked.RemoveListener(OnNewMessageIndicatorClicked);
            }
        }
    }
}