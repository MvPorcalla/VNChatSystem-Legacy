# Unity Chat System: Single Panel Multi-Character Architecture
*A comprehensive guide to implementing an efficient, reusable chat interface for multiple characters in Unity*

## Table of Contents
1. [Overview](#overview)
2. [Architecture Principles](#architecture-principles)
3. [Implementation Guide](#implementation-guide)
4. [Code Examples](#code-examples)
5. [Performance Optimization](#performance-optimization)
6. [Best Practices](#best-practices)
7. [Troubleshooting](#troubleshooting)

---

# Implementation Prompt

"Create a Unity chat system using Single Panel Multi-Character Architecture (SPMCA). Build one reusable ChatPanel that displays different character conversations by dynamically loading data rather than creating separate UI instances. Key requirements:

Switch character data without rebuilding UI unnecessarily
Clear and rebuild only when switching to different characters
Preserve scroll position and state when returning to same character
Use object pooling for message bubbles on mobile
Implement proper state saving/loading between character switches
Follow the separation of concerns: Presentation Layer (UI) ↔ Controller Layer (ChatManager) ↔ Data Layer (Character/Message data)

Focus on memory efficiency, smooth UX, and scalability for multiple characters."

---

## Overview

### What is Single Panel Multi-Character Architecture?

The Single Panel Multi-Character Architecture (SPMCA) is a design pattern for Unity chat systems where **one reusable UI panel** displays conversations for **multiple characters** by dynamically loading and switching data, rather than creating separate UI instances for each character.

### Key Benefits
- **Memory Efficient**: Only one set of UI components exists at any time
- **Performance Optimized**: Minimal overhead when switching between characters
- **Consistent UX**: Identical behavior and appearance across all characters
- **Scalable**: Easy to add new characters without UI complexity
- **Maintainable**: Single source of truth for chat UI logic

### When to Use This Pattern
✅ **Perfect for:**
- Dating sims, visual novels, RPGs with multiple NPCs
- Messaging apps with multiple contacts
- Games with 3+ characters requiring conversations
- Mobile games (memory constraints)
- Long conversation histories

❌ **Avoid if:**
- Only 1-2 characters total
- Very short conversations (5-10 messages)
- Need simultaneous multi-character chat display

---

## Architecture Principles

### 1. Separation of Concerns

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Presentation   │    │   Controller    │    │      Data       │
│     Layer       │    │     Layer       │    │     Layer       │
├─────────────────┤    ├─────────────────┤    ├─────────────────┤
│ • ChatPanel UI  │◄──►│ • ChatManager   │◄──►│ • Character     │
│ • Message       │    │ • State Manager │    │   Data          │
│   Bubbles       │    │ • UI Controller │    │ • Chat History  │
│ • Scroll View   │    │                 │    │ • Message Data  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### 2. State Management Strategy

**Current State**: What the user sees right now
- Active character ID
- Displayed messages
- Scroll position
- UI state (typing, choices, etc.)

**Persistent State**: Saved data for each character
- Complete conversation history
- Progress tracking
- User choices made
- Character-specific settings

### 3. Lifecycle Management

```
Contact Selection → Data Load → UI Rebuild → User Interaction → State Save → Character Switch
       ↑                                                                            ↓
       └────────────────── Return to Contact List ←──────────────────────────────────┘
```

---

## Implementation Guide

### Step 1: Core Data Structures

```csharp
[System.Serializable]
public enum MessageType
{
    System,
    NPC,
    Player,
    NpcCG,
    PlayerCG
}

[System.Serializable]
public class ChatMessage
{
    public MessageType type;
    public string content;
    public Sprite image; // For CG messages
    public DateTime timestamp;
    public bool isRead;
}

[System.Serializable]
public class CharacterData
{
    public string characterId;
    public string characterName;
    public Sprite profileImage;
    public Color bubbleColor;
    public List<ChatMessage> chatHistory;
    public bool hasUnreadMessages;
}

[System.Serializable]
public class ChatState
{
    public string characterId;
    public float scrollPosition;
    public int currentMessageIndex;
    public int selectedChoiceIndex;
    public bool isTypingActive;
    public Dictionary<string, object> customData;
}
```

### Step 2: Chat Manager Architecture

```csharp
public class ChatManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject contactListPanel;
    public GameObject chatAppPanel;
    public TextMeshProUGUI chatHeaderTitle;
    public Image chatHeaderProfile;
    public Transform chatContent;
    public ScrollRect chatScrollRect;
    public Transform choicesContainer;
    
    [Header("Prefabs")]
    public GameObject systemContainerPrefab;
    public GameObject npcChatContainerPrefab;
    public GameObject npcCGContainerPrefab;
    public GameObject playerChatContainerPrefab;
    public GameObject playerCGContainerPrefab;
    
    [Header("Character Data")]
    public List<CharacterData> availableCharacters;
    
    // State Management
    private Dictionary<string, ChatState> characterStates;
    private Dictionary<string, List<ChatMessage>> characterHistories;
    private string currentCharacterId;
    private CharacterData currentCharacter;
    
    // UI Management
    private List<GameObject> currentMessageBubbles;
    private MessageBubbleFactory bubbleFactory;
    
    void Start()
    {
        InitializeSystem();
    }
    
    private void InitializeSystem()
    {
        characterStates = new Dictionary<string, ChatState>();
        characterHistories = new Dictionary<string, List<ChatMessage>>();
        currentMessageBubbles = new List<GameObject>();
        
        bubbleFactory = GetComponent<MessageBubbleFactory>();
        
        LoadCharacterData();
        SetupContactList();
    }
}
```

### Step 3: Character Switching Logic

```csharp
public void OpenChat(string characterId)
{
    // Validate character exists
    CharacterData character = availableCharacters.Find(c => c.characterId == characterId);
    if (character == null) return;
    
    // Don't rebuild if same character
    if (currentCharacterId == characterId)
    {
        ShowChatPanel();
        return;
    }
    
    // Save current state before switching
    if (currentCharacterId != null)
    {
        SaveCurrentChatState();
    }
    
    // Switch to new character
    SwitchToCharacter(character);
}

private void SwitchToCharacter(CharacterData character)
{
    // Update current references
    currentCharacter = character;
    currentCharacterId = character.characterId;
    
    // Update UI header
    UpdateChatHeader(character);
    
    // Clear and rebuild chat content
    ClearChatDisplay();
    LoadChatHistory(character.characterId);
    
    // Restore saved state
    RestoreChatState(character.characterId);
    
    // Show chat panel
    ShowChatPanel();
}

public void BackToContactList()
{
    // Save current progress
    SaveCurrentChatState();
    
    // Switch panels (don't clear UI)
    chatAppPanel.SetActive(false);
    contactListPanel.SetActive(true);
    
    // Update contact list (badges, last messages, etc.)
    UpdateContactListDisplay();
}
```

### Step 4: Message Bubble Factory

```csharp
public class MessageBubbleFactory : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject systemContainerPrefab;
    public GameObject npcChatContainerPrefab;
    public GameObject npcCGContainerPrefab;
    public GameObject playerChatContainerPrefab;
    public GameObject playerCGContainerPrefab;
    
    public GameObject CreateMessageBubble(ChatMessage message, Transform parent)
    {
        GameObject prefab = GetPrefabForMessageType(message.type);
        if (prefab == null) return null;
        
        GameObject bubble = Instantiate(prefab, parent);
        ConfigureBubble(bubble, message);
        
        return bubble;
    }
    
    private GameObject GetPrefabForMessageType(MessageType type)
    {
        return type switch
        {
            MessageType.System => systemContainerPrefab,
            MessageType.NPC => npcChatContainerPrefab,
            MessageType.NpcCG => npcCGContainerPrefab,
            MessageType.Player => playerChatContainerPrefab,
            MessageType.PlayerCG => playerCGContainerPrefab,
            _ => null
        };
    }
    
    private void ConfigureBubble(GameObject bubble, ChatMessage message)
    {
        // Configure text content
        var textComponent = bubble.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = message.content;
        }
        
        // Configure image content (for CG messages)
        if (message.type == MessageType.NpcCG || message.type == MessageType.PlayerCG)
        {
            var imageComponent = bubble.GetComponentInChildren<Image>();
            if (imageComponent != null && message.image != null)
            {
                imageComponent.sprite = message.image;
            }
        }
        
        // Apply character-specific styling
        ApplyCharacterStyling(bubble, message);
    }
}
```

---

## Performance Optimization

### Memory Management

```csharp
public class ChatPerformanceManager : MonoBehaviour
{
    [Header("Performance Settings")]
    public int maxBubblesInMemory = 100;
    public bool enableObjectPooling = true;
    public bool enableViewportCulling = true;
    
    private Queue<GameObject> bubblePool;
    private List<GameObject> activeBubbles;
    
    void Start()
    {
        if (enableObjectPooling)
        {
            InitializeObjectPool();
        }
    }
    
    private void InitializeObjectPool()
    {
        bubblePool = new Queue<GameObject>();
        activeBubbles = new List<GameObject>();
        
        // Pre-populate pool
        for (int i = 0; i < 20; i++)
        {
            CreatePooledBubbles();
        }
    }
    
    public GameObject GetPooledBubble(MessageType type)
    {
        if (bubblePool.Count > 0)
        {
            GameObject bubble = bubblePool.Dequeue();
            activeBubbles.Add(bubble);
            return bubble;
        }
        
        return CreateNewBubble(type);
    }
    
    public void ReturnBubbleToPool(GameObject bubble)
    {
        activeBubbles.Remove(bubble);
        bubblePool.Enqueue(bubble);
        bubble.SetActive(false);
    }
}
```

### Viewport Culling

```csharp
public class ViewportCuller : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float cullingBuffer = 200f;
    
    private List<RectTransform> messageElements;
    
    void Update()
    {
        if (messageElements == null) return;
        
        CullInvisibleElements();
    }
    
    private void CullInvisibleElements()
    {
        var viewport = scrollRect.viewport;
        var viewportBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport);
        
        foreach (var element in messageElements)
        {
            var elementBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(element);
            
            bool isVisible = viewportBounds.Intersects(elementBounds);
            element.gameObject.SetActive(isVisible);
        }
    }
}
```

---

## Best Practices

### 1. State Persistence

```csharp
// Save state frequently but efficiently
private void SaveCurrentChatState()
{
    if (currentCharacterId == null) return;
    
    var state = new ChatState
    {
        characterId = currentCharacterId,
        scrollPosition = chatScrollRect.verticalNormalizedPosition,
        currentMessageIndex = GetCurrentMessageCount(),
        selectedChoiceIndex = GetSelectedChoiceIndex(),
        isTypingActive = IsTypingAnimationActive()
    };
    
    characterStates[currentCharacterId] = state;
    
    // Persist to disk periodically
    if (Time.time - lastSaveTime > autoSaveInterval)
    {
        SaveToDisk();
        lastSaveTime = Time.time;
    }
}
```

### 2. Smooth Transitions

```csharp
public void SwitchToCharacterAnimated(string characterId)
{
    StartCoroutine(SwitchCharacterCoroutine(characterId));
}

private IEnumerator SwitchCharacterCoroutine(string characterId)
{
    // Fade out current chat
    yield return FadeOutChat();
    
    // Switch data
    SwitchToCharacter(GetCharacterData(characterId));
    
    // Fade in new chat
    yield return FadeInChat();
}
```

### 3. Error Handling

```csharp
public bool TryOpenChat(string characterId, out string errorMessage)
{
    errorMessage = null;
    
    // Validate character exists
    if (!DoesCharacterExist(characterId))
    {
        errorMessage = $"Character {characterId} not found";
        return false;
    }
    
    // Check if character is available
    if (!IsCharacterAvailable(characterId))
    {
        errorMessage = $"Character {characterId} is not available yet";
        return false;
    }
    
    // Safe to open
    OpenChat(characterId);
    return true;
}
```

### 4. Mobile Optimization

```csharp
// Adjust behavior based on platform
private void OptimizeForPlatform()
{
    if (Application.isMobilePlatform)
    {
        // Reduce memory usage on mobile
        maxBubblesInMemory = 50;
        enableObjectPooling = true;
        enableViewportCulling = true;
        
        // Lower quality settings
        chatScrollRect.decelerationRate = 0.95f; // Faster scroll deceleration
    }
    else
    {
        // Desktop can handle more
        maxBubblesInMemory = 200;
        enableViewportCulling = false; // Better UX on desktop
    }
}
```

---

## Troubleshooting

### Common Issues and Solutions

**Issue**: Messages not displaying correctly after character switch
```csharp
// Solution: Force layout rebuild
private void RebuildLayout()
{
    Canvas.ForceUpdateCanvases();
    LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent as RectTransform);
}
```

**Issue**: Scroll position not preserved
```csharp
// Solution: Restore scroll position after content rebuild
private void RestoreScrollPosition(float normalizedPosition)
{
    Canvas.ForceUpdateCanvases();
    scrollRect.verticalNormalizedPosition = normalizedPosition;
    Canvas.ForceUpdateCanvases();
}
```

**Issue**: Memory leaks with message bubbles
```csharp
// Solution: Proper cleanup
private void ClearChatDisplay()
{
    foreach (GameObject bubble in currentMessageBubbles)
    {
        if (enableObjectPooling)
            ReturnBubbleToPool(bubble);
        else
            DestroyImmediate(bubble);
    }
    
    currentMessageBubbles.Clear();
}
```

**Issue**: Performance drops with long conversations
```csharp
// Solution: Implement pagination
private void LoadMessagesInChunks(int startIndex, int count)
{
    var messages = GetCharacterMessages(currentCharacterId);
    var chunk = messages.Skip(startIndex).Take(count);
    
    foreach (var message in chunk)
    {
        CreateMessageBubble(message);
    }
}
```

---

## Conclusion

The Single Panel Multi-Character Architecture provides an efficient, scalable solution for Unity chat systems. By separating presentation from data and implementing smart state management, you can create smooth, memory-efficient chat experiences that scale to dozens of characters without performance issues.

Remember the key principles:
- **One UI, Multiple Data Sources**
- **Clear and Rebuild Only When Necessary**
- **Save State, Preserve Experience**
- **Optimize for Your Target Platform**

This architecture is battle-tested in production games and messaging applications, providing a solid foundation for your chat system implementation.