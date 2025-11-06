//=====================================
// MessageData.cs
//=====================================

using System;
using System.Collections.Generic;

namespace ChatDialogueSystem
{
    // ═══════════════════════════════════════════════════════════
    // ░ CORE MESSAGE DATA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Represents a single message in the chat system.
    /// Used for both UI display and save state persistence.
    /// </summary>
    [Serializable]
    public class MessageData
    {
        public enum MessageType
        {
            Text,          // Regular text message
            Image,         // Image message (with optional caption)
            System,        // System notification (e.g., "Chapter 2 unlocked")
            PlayerChoice   // Player's chosen response
        }
        
        public MessageType type;
        public string speaker;      // "player", "emma", "system", etc.
        public string content;      // Text content or image caption
        public string imagePath;    // Addressable path for images
        public string timestamp;    // Display time (HH:mm format)
        public string messageId;    // Unique identifier (for read tracking)

        // ✅ NEW: Flag for unlockable CGs
        public bool shouldUnlockCG;
        
        // Empty constructor for JsonUtility deserialization
        public MessageData() { }
        
        public MessageData(MessageType msgType, string msgSpeaker, string msgContent, string imgPath = "")
        {
            type = msgType;
            speaker = msgSpeaker;
            content = msgContent;
            imagePath = imgPath;
            timestamp = DateTime.Now.ToString("HH:mm");
            messageId = Guid.NewGuid().ToString();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ░ DIALOGUE FLOW
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Represents a player choice option with associated response messages.
    /// </summary>
    [Serializable]
    public class ChoiceData
    {
        public string choiceText;                 // Button text shown to player
        public string targetNode;                 // Node to jump to after selection
        public List<MessageData> playerMessages;  // Player's response messages
        
        // Empty constructor for JsonUtility
        public ChoiceData() 
        {
            playerMessages = new List<MessageData>();
        }
        
        public ChoiceData(string text, string target)
        {
            choiceText = text;
            targetNode = target;
            playerMessages = new List<MessageData>();
        }
    }

    /// <summary>
    /// A dialogue node containing messages, choices, and flow control.
    /// Parsed from MUGI files by MugiParser.
    /// </summary>
    [Serializable]
    public class DialogueNode
    {
        public string nodeName;              // Unique node identifier
        public List<MessageData> messages;   // Messages to display
        public List<ChoiceData> choices;     // Player choices (if any)
        public List<int> pausePoints;        // Message indices where dialogue pauses
        public string nextNode;              // Auto-jump target (empty = end of conversation)

        // Empty constructor for JsonUtility
        public DialogueNode()
        {
            messages = new List<MessageData>();
            choices = new List<ChoiceData>();
            pausePoints = new List<int>();
            nextNode = "";
        }

        public DialogueNode(string name)
        {
            nodeName = name;
            messages = new List<MessageData>();
            choices = new List<ChoiceData>();
            pausePoints = new List<int>();
            nextNode = "";
        }

        /// <summary>
        /// Checks if dialogue should pause after displaying a specific message.
        /// </summary>
        public bool ShouldPauseAfter(int messageIndex)
        {
            return pausePoints.Contains(messageIndex);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ░ SAVE STATE DATA
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Persistent state for a single chat conversation.
    /// Tracks progress, history, and current position in dialogue.
    /// </summary>
    [Serializable]
    public class ChatState
    {
        /// <summary>Save format version for migration support.</summary>
        public const int CURRENT_VERSION = 1;
        
        public int version = CURRENT_VERSION;
        public string chatID;                     // Unique chat identifier
        public string characterName;              // Display name (for debugging)
        public int currentChapterIndex;           // Current chapter in mugiChapters list
        public string currentNodeName;            // Current dialogue node
        public int currentMessageIndex;           // Next message to display (0-based)
        public List<string> readMessageIds;       // IDs of messages player has seen
        public List<MessageData> chatHistory;     // Full conversation history

        // ✅ NEW: Track unlocked CGs per chat
        public List<string> unlockedCGs = new List<string>();
        public bool isInPauseState;               // True if waiting at a pause point

        // ✅ CRITICAL: Empty constructor for JsonUtility deserialization
        public ChatState()
        {
            version = CURRENT_VERSION;
            chatID = "";
            characterName = "";
            currentChapterIndex = 0;
            currentNodeName = "";
            currentMessageIndex = 0;
            readMessageIds = new List<string>();
            chatHistory = new List<MessageData>();
            isInPauseState = false;
        }

        public ChatState(string chatID) : this()
        {
            this.chatID = chatID;
        }
    }

    /// <summary>
    /// Single entry in chat save data, pairing a chatID with its state.
    /// </summary>
    [Serializable]
    public class ChatStateEntry
    {
        public string chatID;
        public ChatState state;

        public ChatStateEntry() 
        { 
            chatID = "";
            state = new ChatState();
        }

        public ChatStateEntry(string id, ChatState chatState)
        {
            chatID = id ?? "";
            state = chatState ?? new ChatState();
        }
    }

    /// <summary>
    /// Root container for all chat states.
    /// Serialized to chat_save.json by DialogueSaveManager.
    /// </summary>
    [Serializable]
    public class ChatSaveData
    {
        /// <summary>Save format version. Increment when changing structure.</summary>
        public const int CURRENT_VERSION = 1;

        public int version = CURRENT_VERSION;
        public List<ChatStateEntry> chatStateList = new List<ChatStateEntry>();

        public ChatSaveData()
        {
            version = CURRENT_VERSION;
            chatStateList = new List<ChatStateEntry>();
        }
    }
}