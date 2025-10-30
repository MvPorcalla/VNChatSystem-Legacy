# Unity Chat App - Complete Documentation

## Table of Contents
1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [UI Hierarchy](#ui-hierarchy)
4. [Component Configuration](#component-configuration)
5. [Scripts and Functionality](#scripts-and-functionality)
6. [Performance Considerations](#performance-considerations)
7. [Implementation Guide](#implementation-guide)
8. [Troubleshooting](#troubleshooting)

---

## Project Overview

This Unity project implements a mobile-responsive chat application interface featuring:
- **Contact List Panel**: Scrollable list of chat contacts with profile images and badges
- **Chat Interface**: Dynamic message display with support for text, images, and system messages
- **Interactive Choices**: Dynamic button system for user responses
- **Responsive Design**: Adapts to different screen sizes and orientations

### Key Features
- NPC and Player message bubbles with different alignments
- Image sharing capabilities (CG/artwork display)
- System messages for notifications
- Dynamic choice buttons with auto-sizing text
- Scroll view optimization for long conversations

---

## Architecture

### Design Pattern
The chat app follows a **hierarchical UI pattern** with clear separation of concerns:
- **PhoneContent**: Root container managing overall layout
- **ContactListPanel**: Handles contact navigation
- **ChatAppPanel**: Main chat interface with three distinct sections

### Layout Strategy
- **Vertical Layout Groups**: For stacking chat elements
- **Horizontal Layout Groups**: For message alignment (left/right)
- **Content Size Fitters**: For dynamic sizing based on content
- **Layout Elements**: For fine-tuned size control

---

## UI Hierarchy

### Root Structure
```
PhoneContent (RectTransform: Stretch All Sides)
├── ContactListPanel
└── ChatAppPanel
```

### Contact List Panel
```
ContactListPanel (RectTransform: Stretch All)
├── Header (Optional)
│   └── Title/Icon
└── ScrollView (ScrollRect)
    └── Viewport (Mask + RectMask2D)
        └── Content (Vertical Layout Group + Content Size Fitter)
            └── CharacterButton1 (Prefab)
                ├── ProfileIMG
                ├── ProfileName
                └── Badge
```

### Chat App Panel
```
ChatAppPanel (Vertical Layout Group)
├── ChatHeader (Layout Element)
├── ChatPanel (ScrollView)
│   └── Content (Vertical Layout Group)
│       ├── SystemContainer
│       ├── NpcChatContainer
│       ├── NpcCGContainer
│       ├── PlayerChatContainer
│       └── PlayerCGContainer
└── ChatChoices (Vertical Layout Group)
    └── ChoiceButton1
```

---

## Component Configuration

### PhoneContent (Root)
- **Component**: RectTransform
- **Anchor**: Stretch All Sides
- **Purpose**: Provides full-screen container for the entire chat interface

### ContactListPanel

#### ScrollView Configuration
- **Component**: ScrollRect
- **Viewport**: 
  - Mask + RectMask2D components
  - Clips content to visible area
- **Content**:
  - Vertical Layout Group
    - Control Child Size: ✅
    - Child Force Expand Width: ✅
  - Content Size Fitter for dynamic height

#### CharacterButton (Prefab)
- **Layout Element**: Preferred Height = 170
- **Child Components**:
  - ProfileIMG: Character avatar
  - ProfileName: Display name
  - Badge: Status/notification indicator

### ChatAppPanel

#### Main Container
- **Vertical Layout Group**:
  - Child Control Size Height: ✅
  - Child Force Expand Width: ✅
  - Padding/Margins as needed

#### ChatHeader
- **Layout Element**:
  - Preferred Height: 150
  - Flexible Height: 0 (fixed size)

#### ChatPanel (ScrollView)
- **ScrollRect** with Mask/RectMask2D
- **Layout Element**:
  - Preferred Height: 0
  - Flexible Height: 1 (fills available space)

##### Content Container
- **Vertical Layout Group**:
  - Control Child Size: Width ✅, Height ✅
  - Child Force Expand Width: ✅
- **Content Size Fitter**: Vertical Fit = Preferred Size

### Message Containers

#### SystemContainer
- **Horizontal Layout Group**:
  - Child Alignment: Middle Center
  - Child Force Expand Width: ✅
- **SystemBubble** (Image):
  - Content Size Fitter: Both axes = Preferred Size
  - Horizontal Layout Group for text control

#### NpcChatContainer
- **Horizontal Layout Group**:
  - Child Alignment: Middle Left
  - Child Force Expand Width: ✅
- **NpcBubble** (Image):
  - Content Size Fitter: Both axes = Preferred Size
  - Contains NpcMessage with AutoResizeText.cs

#### NpcCGContainer (Image Messages)
- **Horizontal Layout Group**:
  - Child Alignment: Middle Left
  - Control Child Size Height: ✅
  - Child Force Expand Width: ✅
- **NpcBubble**:
  - Vertical Layout Group
  - Layout Element: 500×725 preferred size
  - Contains NpcImage with Preserve Aspect

#### PlayerChatContainer
- **Horizontal Layout Group**:
  - Child Alignment: Middle Right
  - Child Force Expand Width: ✅
- **PlayerBubble** (Image):
  - Content Size Fitter: Both axes = Preferred Size
  - Contains PlayerMessage with AutoResizeText.cs

#### PlayerCGContainer (Image Messages)
- **Horizontal Layout Group**:
  - Child Alignment: Middle Right
  - Control Child Size Height: ✅
  - Child Force Expand Width: ✅
- **PlayerBubble**:
  - Layout Element: 500×725 preferred size
  - Contains PlayerImage with Preserve Aspect

### ChatChoices

#### Main Container
- **Layout Element**:
  - Preferred Height: 0
  - Flexible Height: 0
  - Min Height: 50
- **Vertical Layout Group**:
  - Control Child Size: Width ✅, Height ✅
  - Child Force Expand Width: ✅
  - Spacing: 10
- **Content Size Fitter**: Vertical Fit = Preferred Size

#### ChoiceButton
- **Layout Element**: Min Height = 100
- **Content Size Fitter**: Vertical Fit = Preferred Size
- **Button Component**: For user interaction
- **ButtonText** (TextMeshPro):
  - Anchor: Stretch all sides
  - Margins: 15 pixels on all sides
  - Auto Size: Enabled (Default 48)
  - Alignment: Middle Center

---

## Scripts and Functionality

### AutoResizeText.cs

This custom script handles dynamic text resizing for message bubbles.

#### Key Features
- **Responsive Max Width**: Adjusts based on screen size
- **Word Wrapping**: Ensures text fits within bubble constraints
- **Dynamic Layout**: Updates layout elements automatically

#### Implementation Example
```csharp
void Start()
{
    // Adjust max width based on screen width
    float screenWidth = Screen.width;
    maxWidth = Mathf.Min(maxWidth, screenWidth * 0.8f); // 80% of screen width
    
    SetupLayoutElement();
    UpdateWidth();
}
```

#### TextMeshPro Settings
- **Word Wrapping**: ✅ Enabled
- **Horizontal Overflow**: Wrap
- **Auto Size**: OFF (handled by script)
- **Vertical Overflow**: Overflow

### Message Management Scripts

#### ChatManager.cs (Suggested)
Handles message creation and management:
- Message queue system
- Dynamic container instantiation
- Scroll position management
- Choice button generation

#### MessageFactory.cs (Suggested)
Factory pattern for creating different message types:
- System messages
- NPC text messages
- NPC image messages
- Player text messages
- Player image messages

---

## Performance Considerations

### ScrollView Optimization

#### Recommended Settings
- **Movement Type**: Clamped (prevents over-scrolling)
- **Scroll Sensitivity**: 1.0
- **Viewport**: Use RectMask2D instead of Mask when possible

#### Object Pooling
For long conversations, implement object pooling:
```csharp
// Pool message containers to avoid constant instantiation
ObjectPool<GameObject> messagePool;
```

#### Content Size Management
- Use Content Size Fitter judiciously
- Consider manual height calculation for better performance
- Limit the number of active message containers

### Memory Management
- **Image Caching**: Cache frequently used profile images
- **Message Pruning**: Remove old messages beyond a certain threshold
- **Asset Cleanup**: Properly dispose of unused sprites and textures

---

## Implementation Guide

### Step 1: Scene Setup
1. Create empty GameObject named "PhoneContent"
2. Set RectTransform to stretch all sides
3. Add Canvas Scaler component to parent Canvas

### Step 2: Contact List Implementation
1. Create ContactListPanel with ScrollRect
2. Setup Viewport with Mask component
3. Configure Content with Vertical Layout Group
4. Create CharacterButton prefab

### Step 3: Chat Panel Creation
1. Add ChatAppPanel with Vertical Layout Group
2. Create ChatHeader with fixed height
3. Setup ChatPanel ScrollView
4. Configure Content container with layout components

### Step 4: Message Containers
1. Create prefabs for each message type
2. Configure layout groups and size fitters
3. Add AutoResizeText.cs to text components
4. Test with various message lengths

### Step 5: Choice System
1. Setup ChatChoices container
2. Create ChoiceButton prefab
3. Implement dynamic button generation
4. Add interaction callbacks

### Step 6: Script Integration
1. Implement ChatManager for message handling
2. Add MessageFactory for container creation
3. Setup event system for user interactions
4. Test responsiveness across devices

---

## Troubleshooting

### Common Issues

#### Layout Not Updating
**Problem**: UI elements don't resize properly
**Solution**: 
- Ensure Content Size Fitter is set to Preferred Size
- Call `LayoutRebuilder.ForceRebuildLayoutImmediate()` after changes
- Check Layout Element settings

#### Text Overflow
**Problem**: Text extends beyond bubble boundaries
**Solution**:
- Verify AutoResizeText.cs is attached
- Check TextMeshPro overflow settings
- Ensure max width is properly calculated

#### ScrollView Performance
**Problem**: Stuttering during scroll
**Solution**:
- Implement object pooling
- Reduce the number of active UI elements
- Use RectMask2D instead of Mask

#### Button Sizing Issues
**Problem**: Choice buttons not sizing correctly
**Solution**:
- Check Content Size Fitter on button
- Verify Layout Element min/preferred heights
- Ensure text margins are set correctly

### Debug Tips
1. **Layout Debugger**: Use Unity's UI Layout Debugger
2. **Frame Debugger**: Check draw calls and overdraw
3. **Profiler**: Monitor UI performance and memory usage
4. **Device Testing**: Test on target devices early

---

## Best Practices

### UI Design
- **Consistent Spacing**: Use uniform margins and padding
- **Readable Text**: Ensure sufficient contrast and size
- **Touch Targets**: Minimum 44px for mobile interaction
- **Loading States**: Show feedback during message loading

### Code Organization
- **Modular Scripts**: Separate concerns into different classes
- **Event-Driven**: Use UnityEvents for loose coupling
- **Configuration**: Use ScriptableObjects for settings
- **Documentation**: Comment complex layout calculations

### Testing
- **Multiple Devices**: Test various screen sizes and ratios
- **Long Conversations**: Stress test with many messages
- **Edge Cases**: Test with very long/short messages
- **Performance**: Monitor frame rate during interactions

---

## Conclusion

This chat app architecture provides a solid foundation for a mobile messaging interface in Unity. The hierarchical structure with proper layout components ensures responsive design across different devices, while the modular approach allows for easy expansion and maintenance.

Key strengths of this implementation:
- **Flexibility**: Easy to add new message types
- **Performance**: Optimized for mobile devices
- **Maintainability**: Clear separation of concerns
- **Responsiveness**: Adapts to various screen sizes

Remember to thoroughly test on target devices and consider implementing object pooling for production use with extended conversations.