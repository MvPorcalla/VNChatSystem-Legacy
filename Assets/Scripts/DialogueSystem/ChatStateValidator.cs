//=====================================
// ChatStateValidator.cs - Static Utility
//=====================================

using System.Collections.Generic;
using System.Linq;
using static ChatDialogueSystem.DebugHelper;

namespace ChatDialogueSystem
{
    /// <summary>
    /// Static utility for validating and correcting chat state data.
    /// Ensures state integrity before dialogue processing.
    /// </summary>
    public static class ChatStateValidator
    {
        /// <summary>
        /// Validates chapter index and auto-corrects if invalid.
        /// </summary>
        public static void ValidateChapterIndex(ChatState state, NPCChatData chatData)
        {
            if (state == null || chatData == null)
            {
                LogError(Category.ChatManager, "Cannot validate chapter: state or chatData is null");
                return;
            }

            if (state.currentChapterIndex < 0 || 
                state.currentChapterIndex >= chatData.mugiChapters.Count)
            {
                LogWarning(Category.ChatManager, 
                    $"Invalid chapter index {state.currentChapterIndex} for {chatData.characterName}. " +
                    $"Valid range: 0-{chatData.mugiChapters.Count - 1}. Resetting to 0.");
                
                state.currentChapterIndex = 0;
                state.currentMessageIndex = 0;
                state.readMessageIds.Clear();
            }
        }

        /// <summary>
        /// Validates state against dialogue nodes (node existence, message bounds, pause state).
        /// Auto-corrects invalid values.
        /// </summary>
        public static void ValidateState(ChatState state, NPCChatData chatData,
                                        Dictionary<string, DialogueNode> currentNodes)
        {
            if (state == null || chatData == null)
            {
                LogError(Category.ChatManager, "Cannot validate: state or chatData is null");
                return;
            }

            if (currentNodes == null || currentNodes.Count == 0)
            {
                LogError(Category.ChatManager, "CRITICAL: currentNodes is null or empty. Cannot validate state.");
                return;
            }

            // Validate node name
            ValidateNodeName(state, currentNodes);
            
            // Validate message index
            ValidateMessageIndex(state, currentNodes);
            
            // Validate pause state consistency
            ValidatePauseState(state, currentNodes);
        }

        private static void ValidateNodeName(ChatState state, Dictionary<string, DialogueNode> currentNodes)
        {
            if (string.IsNullOrEmpty(state.currentNodeName) ||
                !currentNodes.ContainsKey(state.currentNodeName))
            {
                string firstNode = currentNodes.Keys.FirstOrDefault();

                if (string.IsNullOrEmpty(firstNode))
                {
                    LogError(Category.ChatManager,
                        "CRITICAL: No valid nodes found in currentNodes dictionary.");
                    return;
                }

                LogWarning(Category.ChatManager,
                    $"Node '{state.currentNodeName}' not found. " +
                    $"Resetting to first node: '{firstNode}'");

                state.currentNodeName = firstNode;
                state.currentMessageIndex = 0;
            }
        }

        private static void ValidateMessageIndex(ChatState state, Dictionary<string, DialogueNode> currentNodes)
        {
            if (!currentNodes.ContainsKey(state.currentNodeName))
                return;

            var node = currentNodes[state.currentNodeName];
            if (node == null || node.messages == null)
                return;

            // Allow messageIndex to equal messages.Count (end of node state)
            if (state.currentMessageIndex < 0 || state.currentMessageIndex > node.messages.Count)
            {
                LogWarning(Category.ChatManager,
                    $"Invalid message index {state.currentMessageIndex} for node '{state.currentNodeName}' " +
                    $"(valid range: 0-{node.messages.Count}). Resetting to 0.");
                state.currentMessageIndex = 0;
            }
        }

        private static void ValidatePauseState(ChatState state, Dictionary<string, DialogueNode> currentNodes)
        {
            if (!state.isInPauseState)
                return;

            if (!currentNodes.ContainsKey(state.currentNodeName))
                return;

            var node = currentNodes[state.currentNodeName];
            if (node == null)
                return;

            if (!node.ShouldPauseAfter(state.currentMessageIndex))
            {
                LogWarning(Category.ChatManager,
                    $"Pause state mismatch: state says paused but no pause point at index {state.currentMessageIndex}. " +
                    $"Clearing pause state.");
                state.isInPauseState = false;
            }
        }
    }
}