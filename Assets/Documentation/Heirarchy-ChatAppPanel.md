# Chat App Hierarchy

```
Outside Canvas:
DialogueSaveManager                <- DialogueSaveManager.cs
DialogueNavigationManager          <- DialogueNavigationManager.cs

PhoneContent                       <- RectTransform: Anchor Stretch All Sides  + [ChatManager.cs]
│
├── ContactListPanel (RectTransform: Stretch All)
│   │
│   ├── Header
│   │    └── Title / Icon
│   └── ContactPanel (ScrollRect)
│        └── Viewport (Mask + RectMask2D)
│             └── Content (Vertical Layout Group [Control Child Size and Child Force Expand width checked] + Content Size Fitter)
│                  └── CharacterButton1 (Prefab) (Layout Element preferred Height 170)
│                      ├── ProfileIMG     
│                      ├── ProfileName
│                      └── Badge
│
└── ChatAppPanel                   <- Vertical Layout Group
     • Child Control Size: Height ✅
     • Child Force Expand: Width ✅
     • Padding/Margins as needed
    │
    ├── ChatHeader                 <- Layout Element
    │    • Preferred Height = 150
    │    • Flexible Height = 
    │   ├── ChatBackButton
    │   ├── ChatProfileContainer
    │   │   └── ChatProfileIMG
    │   └── ChatProfileName
    │   └── ChatModeToggle          <- The normal mode / Fast mode Toggle
    │       └── Icon
    │
    ├── ChatPanel                  <- Scroll View (ScrollRect)
    │    • Mask / RectMask2D
    │    • Layout Element: Preferred Height = 0, Flexible Height = 1
    │   ├── Viewport
    │   │  └── Content           <- Vertical Layout Group + Content Size Fitter
    │   │       • Vertical Layout Group:
    │   │           - Control Child Size: Width ✅ Height ✅
    │   │           - Child Force Expand: Width ✅
    │   │       • Content Size Fitter: Vertical Fit = Preferred 
    │   │      ├── SystemContainer <- Horizontal Layout Group
    │   │      │    • Child Alignment: Middle Center
    │   │      │    • Child Force Expand: Width ✅
    │   │      │   └── SystemBubble (Image)
    │   │      │        • Content Size Fitter: Horizontal & Vertical Fit = Preferred Size
    │   │      │        • Horizontal Layout Group:
    │   │      │             - Control Child Size: Width ✅, Height ✅
    │   │      │       └── SystemMessage (TextMeshProUGUI)
    │   │      │            • AutoResizeText.cs attached
    │   │      │            • TMP Settings:
    │   │      │                - Word Wrapping ✅
    │   │      │                - Horizontal Overflow → Wrap
    │   │      │                - Auto Size → OFF
    │   │      ├── NpcChatContainer <- Horizontal Layout Group
    │   │      │    • Child Alignment: Middle Left (NPC)
    │   │      │    • Child Force Expand: Width ✅
    │   │      │   └── NpcBubble (Image)
    │   │      │        • Content Size Fitter: Horizontal & Vertical Fit = Preferred Size
    │   │      │        • Horizontal Layout Group:
    │   │      │             - Control Child Size: Width ✅, Height ✅
    │   │      │       └── NpcMessage (TextMeshProUGUI)
    │   │      │            • AutoResizeText.cs attached
    │   │      │            • TMP Settings:
    │   │      │                - Word Wrapping ✅
    │   │      │                - Horizontal Overflow → Wrap
    │   │      │                - Auto Size → OFF
    │   │      ├── NpcCGContainer <- Horizontal Layout Group
    │   │      │    • Child Alignment: Middle Left (NPC)
    │   │      │    • Control Child Size: Height ✅
    │   │      │    • Child Force Expand: Width ✅
    │   │      │   └── NpcBubble (Image)
    │   │      │        • Vertical Layout Group:
    │   │      │             - Control Child Size: Width ✅, Height ✅
    │   │      │        • Layout Elemnt:
    │   │      │             - preferred width: 500
    │   │      │             - preferred Heigth: 725
    │   │      │       └── NpcImage
    │   │      │             - Image: Preserve Aspect: ✅
    │   │      ├── TypingIndicator <- Horizontal Layout Group
    │   │      │    • Child Alignment: Middle Left (NPC)
    │   │      │    • Child Force Expand: Width ✅
    │   │      │   └── TypingBubble (Image)
    │   │      │        • Content Size Fitter: Horizontal & Vertical Fit = Preferred Size
    │   │      │        • Horizontal Layout Group:
    │   │      │             - Control Child Size: Width ✅, Height ✅
    │   │      │       └── TypingText (TextMeshProUGUI)
    │   │      │            • TMP Settings:
    │   │      │                - Word Wrapping ✅
    │   │      │                - Horizontal Overflow → Wrap
    │   │      │                - Auto Size → OFF
    │   │      │            
    │   │      ├── PlayerChatContainer <- Horizontal Layout Group
    │   │      │    • Child Alignment: Middle Right (Player)
    │   │      │    • Child Force Expand: Width ✅
    │   │      │   └── PlayerBubble (Image)
    │   │      │        • Content Size Fitter: Horizontal & Vertical Fit = Preferred Size
    │   │      │        • Horizontal Layout Group:
    │   │      │             - Control Child Size: Width ✅, Height ✅
    │   │      │       └── PlayerMessage (TextMeshProUGUI)
    │   │      │            • AutoResizeText.cs attached
    │   │      │            • TMP Settings:
    │   │      │                - Word Wrapping ✅
    │   │      │                - Horizontal Overflow → Wrap
    │   │      │                - Auto Size → OFF
    │   │      ├── PlayerCGtContainer <- Horizontal Layout Group
    │   │           • Child Alignment: Middle Right (NPC)
    │   │           • Control Child Size: Height ✅
    │   │           • Child Force Expand: Width ✅
    │   │          └── PlayerBubble (Image)
    │   │               • Vertical Layout Group:
    │   │                    - Control Child Size: Width ✅, Height ✅
    │   │               • Layout Elemnt:
    │   │                    - preferred width: 500
    │   │                    - preferred Heigth: 725
    │   │              └── PlayerImage
    │   │                    - Image: Preserve Aspect: ✅
    │   └── NewMessageIndicator (UI Button)
    │       ├── Background (Image)
    │       └── IndicatorText (TextMeshProUGUI)
    │
    └── ChatChoices                    <- Layout Element + Vertical Layout Group + Content Size Fitter
         • Layout Element:
           - Preferred Height = 0 (changed from 150)
           - Flexible Height = 0
           - Min Height = 50 (minimum space)
         • Vertical Layout Group:
           - Control Child Size: Width ✅ Height ✅
           - Child Force Expand: Width ✅
           - Spacing: 10
         • Content Size Fitter:
           - Vertical Fit = Preferred Size
         │
         └── ChoiceButton1              <- Default Button (always present)
              • Layout Element:
                - Min Height = 100
              • Content Size Fitter:
                - Vertical Fit = Preferred Size
              • Button component for interaction
              • Default text: "Continue" or " • • • "
              └── ButtonText (TextMeshProUGUI)
                   • RectTransform:
                     - Anchor Presets: Stretch (all sides)
                     - Left/Right/Top/Bottom: 15 (padding margins)
                   • TMP Settings:
                     - Word Wrapping ✅
                     - Horizontal Overflow → Wrap
                     - Vertical Overflow → Overflow
                     - Auto Size → On Default 48
                     - Alignment: Middle Center
```

# Chat App Hierarchy

```
PhoneContent                       <- RectTransform: Anchor Stretch All Sides  + [ChatManager.cs]
│
├── ContactListPanel (RectTransform: Stretch All)
│   │
│   ├── Header (optional)
│   └── Scroll View (ScrollRect)
│        └── Viewport (Mask + RectMask2D)
│             └── Content (Vertical Layout Group [Control Child Size and Child Force Expand width checked] + Content Size Fitter)
│                  └── CharacterButton1 (Prefab) (Layout Element preferred Height 170)
│                      ├── ProfileIMG     
│                      ├── ProfileName
│                      └── Badge
│
└── ChatAppPanel                    <- Vertical Layout Group
    │
    ├── ChatHeader                  <- Layout Element
    │   ├── ChatBackButton
    │   ├── ChatProfileContainer
    │   │   └── ChatProfileIMG
    │   ├── ChatProfileName
    │   └── ChatModeToggle          <- The normal mode / Fast mode Toggle
    │       └── Icon
    │
    ├── ChatPanel                   <- Scroll View (ScrollRect)
    │   ├── Viewport
    │   │   └── Content              <- Vertical Layout Group + Content Size Fitter
    │   │       ├── SystemContainer  <- Horizontal Layout Group
    │   │       │   └── SystemBubble (Image)
    │   │       │       └── SystemMessage (TextMeshProUGUI)
    │   │       ├── NpcChatContainer <- Horizontal Layout Group
    │   │       │   └── NpcBubble (Image)
    │   │       │       └── NpcMessage (TextMeshProUGUI)
    │   │       ├── NpcCGContainer   <- Horizontal Layout Group
    │   │       │   └── NpcBubble (Image)
    │   │       │       └── NpcImage
    │   │       ├── TypingIndicator  <- Horizontal Layout Group
    │   │       │   └── TypingBubble (Image)
    │   │       │       └── TypingText (TextMeshProUGUI)
    │   │       │            
    │   │       ├── PlayerChatContainer <- Horizontal Layout Group
    │   │       │   └── PlayerBubble (Image)
    │   │       │       └── PlayerMessage (TextMeshProUGUI)
    │   │       └── PlayerCGContainer <- Horizontal Layout Group
    │   │           └── PlayerBubble (Image)
    │   │               └── PlayerImage
    │   └── NewMessageIndicator (Empty Game Object)
    │       └── IndicatorButton (UI Button)
    │           ├── IndicatorIcon
    │           └── IndicatorText (TextMeshProUGUI)
    │
    └── ChatChoices                    <- Layout Element + Vertical Layout Group + Content Size Fitter
         └── ChoiceButton1              <- Default Button (always present)
              └── ButtonText (TextMeshProUGUI)
```