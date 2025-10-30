//=====================================
// ChatFlowController.cs
//=====================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine; // For Time
using static ChatDialogueSystem.DebugHelper;

namespace ChatDialogueSystem
{
    public class ChatFlowController
    {
        private const string PAUSE_BUTTON_TEXT = "• • •";
        private const float RESET_COOLDOWN = 2f; // Prevent double-click spam

        // Dependencies
        private readonly ChatManager chatManager;
        private readonly ChatTimingController chatTimingController;

        // Current state references
        private NPCChatData currentChatData;
        private ChatState currentChatState;
        private Dictionary<string, DialogueNode> currentDialogueNodes;
        private DialogueNode currentNode;
        private string currentChatID;
        private bool isInPauseState;

        // Reset cooldown tracking
        private float lastResetTime = -999f;

        public ChatFlowController(
            ChatManager manager,
            ChatTimingController timedController)
        {
            this.chatManager = manager ?? throw new ArgumentNullException(nameof(manager));
            this.chatTimingController = timedController ?? throw new ArgumentNullException(nameof(timedController));
        }

        public void Initialize(
            NPCChatData chatData,
            ChatState chatState,
            Dictionary<string, DialogueNode> dialogueNodes,
            string chatID,
            bool pauseState)
        {
            currentChatData = chatData;
            currentChatState = chatState;
            currentDialogueNodes = dialogueNodes;
            currentChatID = chatID;
            isInPauseState = pauseState;

            if (chatState != null && !string.IsNullOrEmpty(chatState.currentNodeName) && dialogueNodes.ContainsKey(chatState.currentNodeName))
            {
                currentNode = dialogueNodes[chatState.currentNodeName];
            }

            Log(Category.ChatManager, $"ChatFlowController initialized for chat: {chatID}");
        }

        public void UpdateDialogueNodes(Dictionary<string, DialogueNode> newNodes)
        {
            currentDialogueNodes = newNodes;
            Log(Category.ChatManager, $"Dialogue nodes updated: {newNodes?.Count ?? 0} nodes available");
        }

        public bool IsInPauseState => isInPauseState;

        public void ContinueFromCurrentState()
        {
            if (currentChatState == null || currentDialogueNodes == null)
            {
                LogError(Category.ChatManager, "Cannot continue: state or dialogue nodes not initialized");
                return;
            }

            if (!currentDialogueNodes.ContainsKey(currentChatState.currentNodeName))
            {
                LogError(Category.ChatManager, $"Cannot continue: node '{currentChatState.currentNodeName}' not found");
                return;
            }

            currentNode = currentDialogueNodes[currentChatState.currentNodeName];

            if (isInPauseState)
            {
                Log(Category.ChatManager, "Resuming from pause - showing pause button");
                DetermineNextAction();
            }
            else
            {
                ProcessCurrentNode();
            }
        }

        public void ProcessCurrentNode()
        {
            Log(Category.ChatManager, $"Processing node: {currentChatState.currentNodeName}");

            if (currentNode == null || currentChatState == null)
            {
                LogError(Category.ChatManager, "Cannot process - invalid state");
                return;
            }

            if (currentChatState.currentMessageIndex < 0 ||
                currentChatState.currentMessageIndex > currentNode.messages.Count)
            {
                LogError(Category.ChatManager,
                    $"Invalid message index {currentChatState.currentMessageIndex} for node '{currentNode.nodeName}' " +
                    $"(max: {currentNode.messages.Count}). Resetting to 0.");
                currentChatState.currentMessageIndex = 0;
            }

            string processingChatID = currentChatID;
            string processingNodeName = currentChatState.currentNodeName;
            ChatState stateSnapshot = currentChatState;

            var messagesToShow = new List<MessageData>();
            int startIndex = currentChatState.currentMessageIndex;

            int endIndex = currentNode.messages.Count;
            foreach (int pausePoint in currentNode.pausePoints)
            {
                if (pausePoint > startIndex)
                {
                    endIndex = pausePoint;
                    break;
                }
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                var message = currentNode.messages[i];
                if (!currentChatState.readMessageIds.Contains(message.messageId))
                {
                    messagesToShow.Add(message);
                }
            }

            if (messagesToShow.Count > 0)
            {
                Log(Category.ChatManager, $"Queueing {messagesToShow.Count} new messages for chat {processingChatID}");

                chatTimingController.QueueMessages(messagesToShow, () =>
                {
                    if (!ValidateCallbackState(processingChatID, processingNodeName, stateSnapshot))
                    {
                        return;
                    }

                    foreach (var message in messagesToShow)
                    {
                        stateSnapshot.chatHistory.Add(message);
                        stateSnapshot.readMessageIds.Add(message.messageId);
                    }

                    stateSnapshot.currentMessageIndex = endIndex;
                    DetermineNextAction();
                    chatManager.SaveChatState();
                });
            }
            else
            {
                Log(Category.ChatManager, $"No new messages. currentMessageIndex={currentChatState.currentMessageIndex}, endIndex={endIndex}");
                DetermineNextAction();
                chatManager.SaveChatState();
            }
        }

        private bool ValidateCallbackState(string expectedChatID, string expectedNodeName, ChatState expectedState)
        {
            if (currentChatID != expectedChatID)
            {
                LogWarning(Category.ChatManager,
                    $"Callback aborted: Chat switched from {expectedChatID} to {currentChatID}");
                return false;
            }

            if (currentChatState != expectedState)
            {
                LogWarning(Category.ChatManager, "Callback aborted: currentChatState reference changed");
                return false;
            }

            if (chatManager == null)
            {
                LogWarning(Category.ChatManager, "Callback aborted: chatManager is null");
                return false;
            }

            if (currentChatState.currentNodeName != expectedNodeName)
            {
                LogWarning(Category.ChatManager,
                    $"Callback aborted: Node changed from {expectedNodeName} to {currentChatState.currentNodeName}");
                return false;
            }

            if (currentDialogueNodes == null || !currentDialogueNodes.ContainsKey(currentChatState.currentNodeName))
            {
                LogWarning(Category.ChatManager, "Callback aborted: Current node no longer exists");
                return false;
            }

            return true;
        }

        public void DetermineNextAction()
        {
            chatManager.ClearChoices();

            if (currentNode.ShouldPauseAfter(currentChatState.currentMessageIndex))
            {
                Log(Category.ChatManager, "Showing pause button");
                isInPauseState = true;
                ShowPauseButton();
            }
            else if (currentNode.choices.Count > 0)
            {
                Log(Category.ChatManager, $"Showing {currentNode.choices.Count} choices");
                isInPauseState = false;
                chatManager.ShowChoices(currentNode.choices, OnChoiceSelected);
            }
            else if (!string.IsNullOrEmpty(currentNode.nextNode))
            {
                Log(Category.ChatManager, $"Auto-jumping to node: {currentNode.nextNode}");
                isInPauseState = false;
                JumpToNode(currentNode.nextNode);
            }
            else
            {
                Log(Category.ChatManager, "End of story reached - showing reset option");
                isInPauseState = false;
                chatManager.ShowEndResetChoice(false, OnEndResetClicked);
            }
        }

        private void ShowPauseButton()
        {
            chatManager.ShowPauseButton(PAUSE_BUTTON_TEXT, OnPauseButtonClicked);
        }

        private void OnPauseButtonClicked()
        {
            if (chatTimingController.IsDisplayingMessages)
                return;

            Log(Category.ChatManager, "Pause button clicked - continuing");
            isInPauseState = false;
            chatManager.ClearChoices();
            ProcessCurrentNode();
        }

        public void OnChoiceSelected(ChoiceData choice)
        {
            if (chatTimingController.IsDisplayingMessages)
                return;

            Log(Category.ChatManager, $"Choice selected: {choice.choiceText} -> {choice.targetNode}");

            string processingChatID = currentChatID;
            string processingNodeName = currentChatState.currentNodeName;
            ChatState stateSnapshot = currentChatState;

            isInPauseState = false;
            chatManager.ClearChoices();

            if (choice.playerMessages.Count > 0)
            {
                chatTimingController.QueuePlayerMessages(choice.playerMessages, () =>
                {
                    if (!ValidateCallbackState(processingChatID, processingNodeName, stateSnapshot))
                    {
                        return;
                    }

                    foreach (var playerMessage in choice.playerMessages)
                    {
                        stateSnapshot.chatHistory.Add(playerMessage);
                        stateSnapshot.readMessageIds.Add(playerMessage.messageId);
                    }

                    JumpToNode(choice.targetNode);
                });
            }
            else
            {
                JumpToNode(choice.targetNode);
            }
        }

        public void JumpToNode(string nodeName)
        {
            if (currentDialogueNodes.ContainsKey(nodeName))
            {
                Log(Category.ChatManager, $"Jumping to node: {nodeName}");
                currentChatState.currentNodeName = nodeName;
                currentChatState.currentMessageIndex = 0;
                currentNode = currentDialogueNodes[nodeName];
                ProcessCurrentNode();
            }
            else
            {
                Log(Category.ChatManager, $"Node '{nodeName}' not found in current chapter. Attempting to load next chapter...");
                LoadNextChapter(nodeName);
            }
        }

        public void LoadNextChapter(string targetNode)
        {
            if (currentChatState.currentChapterIndex >= currentChatData.mugiChapters.Count - 1)
            {
                Log(Category.ChatManager, $"Already at last chapter ({currentChatState.currentChapterIndex}). Node '{targetNode}' not found - showing end screen.");
                chatManager.ShowEndResetChoice(false, OnEndResetClicked);
                return;
            }

            currentChatState.currentChapterIndex++;

            // ✅ NULL CHECK BEFORE PARSING
            var nextChapter = currentChatData.mugiChapters[currentChatState.currentChapterIndex];
            if (nextChapter == null)
            {
                LogError(Category.ChatManager,
                    $"CRITICAL: Chapter {currentChatState.currentChapterIndex} is NULL! " +
                    $"Check the mugiChapters list in {currentChatData.characterName}'s NPCChatData.");

                chatManager.ShowEndResetChoice(true, OnEndResetClicked);
                return;
            }

            Log(Category.ChatManager, $"Loading chapter {currentChatState.currentChapterIndex}, target node: {targetNode}");
            currentDialogueNodes = MugiParser.ParseMugiFile(nextChapter);
    
            if (currentDialogueNodes == null || currentDialogueNodes.Count == 0)
            {
                LogError(Category.ChatManager,
                    $"CRITICAL: Failed to parse chapter {currentChatState.currentChapterIndex}. " +
                    $"Chapter file may be corrupt or empty.");

                chatManager.ShowEndResetChoice(true, OnEndResetClicked);
                return;
            }

            if (currentDialogueNodes.ContainsKey(targetNode))
            {
                currentChatState.currentNodeName = targetNode;
                currentChatState.currentMessageIndex = 0;
                currentNode = currentDialogueNodes[targetNode];
                ProcessCurrentNode();
            }
            else
            {
                LogError(Category.ChatManager, $"Node '{targetNode}' not found in chapter {currentChatState.currentChapterIndex}");

                LogError(Category.ChatManager,
                    $"CRITICAL: Node '{targetNode}' missing in chapter {currentChatState.currentChapterIndex}. " +
                    $"This is a content error in your MUGI files. Please check your chapter transitions.");

                chatManager.ShowEndResetChoice(true, OnEndResetClicked);
            }
        }

        private void OnEndResetClicked()
        {
            if (currentChatData == null)
            {
                LogError(Category.ChatManager, "Cannot reset: no active chat");
                return;
            }

            Log(Category.ChatManager, $"Reset story requested for: {currentChatData.characterName}");
            ResetStoryForCurrentChat();
        }

        /// <summary>
        /// Completely resets the current chat story by deleting all save data
        /// and returning to the contact list. Forces full re-initialization on next entry.
        /// </summary>
        /// <remarks>
        /// This method is safe to call at any time. It validates state, stops ongoing
        /// processes, and ensures clean UI/data separation.
        /// </remarks>
        public void ResetStoryForCurrentChat()
        {
            // === COOLDOWN CHECK ===
            if (Time.realtimeSinceStartup - lastResetTime < RESET_COOLDOWN)
            {
                LogWarning(Category.ChatManager, "Reset on cooldown - preventing spam");
                return;
            }

            lastResetTime = Time.realtimeSinceStartup;

            // === VALIDATION ===
            if (currentChatData == null)
            {
                LogError(Category.ChatManager, "Cannot reset: no active chat");
                return;
            }

            if (chatTimingController != null && chatTimingController.IsDisplayingMessages)
            {
                LogWarning(Category.ChatManager, "Cannot reset while messages are displaying");
                return;
            }

            Log(Category.ChatManager, $"=== RESETTING STORY: {currentChatData.characterName} (ID: {currentChatID}) ===");

            // === STEP 1: Stop All Ongoing Processes ===
            if (chatTimingController != null)
            {
                chatTimingController.StopCurrentSequence();
                Log(Category.ChatManager, "Stopped message timing controller");
            }

            // === STEP 2: NUCLEAR CLEAR - Destroy all pooled objects ===
            // Use NuclearReset for story resets (not chat switches)
            if (chatManager != null)
            {
                chatManager.NuclearReset(); // ← This destroys everything
                Log(Category.ChatManager, "Nuclear reset complete - all pools destroyed");
            }

            // === STEP 3: Delete Save Data Entry ===
            Log(Category.ChatManager, "Deleting save data for this chat...");
            DialogueSaveManager.Instance.ClearChatState(currentChatID);

            // === STEP 4: Nullify In-Memory State ===
            currentChatState = null;
            currentDialogueNodes = null;
            currentNode = null;
            isInPauseState = false;

            Log(Category.ChatManager, "In-memory state cleared");

            // === STEP 5: Navigate Back to Contact List ===
            Log(Category.ChatManager, "Returning to contact list...");
            chatManager.ReturnToContactList();

            Log(Category.ChatManager, "=== RESET COMPLETE - All resources freed, ready for fresh start ===");
        }
    }
}