### 🧩 Save Manager Issue

**Files involved:**
`BaseSaveManager.cs`, `DialogueSaveManager.cs`, `PlayerSaveManager.cs`, `ChatStateManager.cs`, `ChatFlowManager.cs`, `ChatManager.cs`

---

### 💾 Current Save Structure

```
<Application.persistentDataPath>/
└── SaveData/
    ├── ChatData/
    │   ├── save_data.json        ← DialogueSaveManager data
    │   └── Backups/
    │       └── backup_..._save_data.json
    │
    └── PlayerData/
        ├── save_data.json        ← PlayerSaveManager data
        └── Backups/
            └── backup_..._save_data.json
```

---

### ⚠️ The Issue

Saving works fine — the files are created correctly.
However, when I enter Play Mode starting from the **00_Bootstrap** scene (where my singleton managers are initialized), then go to the chat and start a conversation, the data saves properly.

But once I **exit Play Mode** and **enter Play Mode again**, the chat progress resets — it starts over as if no previous data exists.

---

### ❓Question

Why does the saved chat data not persist between play sessions, even though the save files are being created correctly?

---

### Build Settings:
```
[0] 00_Consent    ← Only has Canvas + ConsentManager
[1] 01_Bootstrap  ← Has GameManager + SaveManagers
[2] 01_Cutscene
[3] 02_Lockscreen
[4] 03_HomeScreen
[5] 04_ChatApp
```

---

I’m using a pooling system for chat bubbles in my VN messenger simulation. The prefabs for NPC, player, system, and typing bubbles all have static background images. On first instantiation everything works fine, but when objects are pooled and reused, some bubbles (especially the typing indicator) lose their source image — the pooled clone no longer has the original sprite. As a result, reused bubbles sometimes appear empty, even though the prefab itself is correct. How can I ensure pooled clones retain their static background images while still clearing dynamic content (CGs, text)?

**Example hierarchy:**

```
Prefab:
├── SystemContainer
│   └── SystemBubble (Image)                    <- static background
│       └── SystemMessage (TextMeshProUGUI)     <- dynamic content
├── NpcChatContainer
│   └── NpcBubble (Image)                       <- static background
│       └── NpcMessage (TextMeshProUGUI)        <- dynamic content
├── NpcCGContainer
│   └── NpcBubble (Image)                       <- static background
│       └── NpcImage                            <- dynamic content
├── TypingIndicator
│   └── TypingBubble (Image)                    <- static background
│       └── TypingText (TextMeshProUGUI)        <- dynamic content
├── PlayerChatContainer
│   └── PlayerBubble (Image)                    <- static background
│       └── PlayerMessage (TextMeshProUGUI)     <- dynamic content
└── PlayerCGContainer
    └── PlayerBubble (Image)                    <- static background
        └── PlayerImage                         <- dynamic content
```

The problem seems to be that pooling clears the `Image.sprite` of all components, including static backgrounds, which causes the bubbles to appear empty when reused.

and this code is my fix for this problem do you think i did it rigjt with no more issue?

---

> I’m building a visual novel/chat game with unlockable CGs. I want to implement a system where:
>
> 1. When a CG is unlocked in the chat (Dialogue system), it is saved to a persistent gallery.
> 2. The gallery panel is in the HomeScreen scene.
> 3. CG unlocks should **persist even if the chat is reset**, meaning they need to be saved in the main PlayerSave, separate from the chat dialogue save.
> 4. I want to know the proper workflow for:
>
> * Saving the CG unlocks to PlayerSave
> * Loading and displaying unlocked CGs in the gallery panel
> * Avoiding the CG being lost if the chat system resets
>   **Build Settings:**
>
> ```
> [0] 00_Consent    ← Canvas + ConsentManager
> [1] 01_Bootstrap  ← GameManager + SaveManagers
> [2] 01_Cutscene
> [3] 02_Lockscreen
> [4] 03_HomeScreen  ← Gallery panel is here
> [5] 04_ChatApp     ← Chat system where CGs are unlocked
> ```

> How should I structure the save/load flow so CGs unlock in chat and appear in the gallery persistently?

---

Chatmanager.cs, ChatDisplayManager.cs, ChatFlowController.cs, MugiParser.cs, PlayerSaveManager.cs, DialogueSaveManager.cs, BaseSaveManager.cs MessageData.cs, PlayerData.cs

Got it. You want a **CG unlocking system** for your VN chat game that tracks unlockable CGs from your `.mugi` dialogue files and persists them in a separate JSON. Here’s how I would approach it and how to prompt it clearly for implementation:

---

### Prompt for CG Unlock System Implementation

**Goal:**
Implement a `CGSaveManager` that tracks unlockable CGs for each NPC/chat, reads `.mugi` files for unlockable CG tags, and saves them to a dedicated JSON file like this:

```json
"unlockedCGsByChat": [
  {
    "chatID": "ae30dd_sample_NPC",
    "unlockedCGs": ["smp_cg3", "smp_cg2"]
  }
]
```

**Mugi File Rules:**

* Unlockable CG: `>> media npc type:image unlock:true path:smp_cg2`
* Normal CG: `>> media npc type:image path:smp_cg2`

**Requirements:**

1. Parse `.mugi` files to detect `unlock:true` CGs.
2. When the player reaches a line with `unlock:true`, mark the CG as unlocked for that chatID.
3. Persist unlocked CGs to a separate JSON (`CGSave.json`) via `CGSaveManager`.
4. Provide public methods for:

   * Checking if a CG is unlocked.
   * Getting all unlocked CGs per NPC/chat.
   * Saving & loading from JSON.
5. Optional: Support multiple playthroughs where branch-specific CGs unlock independently.

**Example Methods in `CGSaveManager`:**

```csharp
public void UnlockCG(string chatID, string cgID); // unlocks and saves
public bool IsCGUnlocked(string chatID, string cgID); // returns true/false
public List<string> GetUnlockedCGs(string chatID); // returns all unlocked CGs for a chat
```

**Additional Notes:**

* Keep the system lightweight (~2–6KB JSON).
* Decouple from PlayerSaveManager.
* Support future features like achievements or gallery progress.

---

```json
  {
    "chatID": "9429c5_sample_b",
    "state": {
      "version": 1,
      "chatID": "9429c5_sample_b",      <- reset
      "characterName": "sample_b",      <- reset
      "currentChapterIndex": 0,         <- reset
      "currentNodeName": "C1_Start",    <- reset
      "currentMessageIndex": 4,         <- reset
      "readMessageIds": [],             <- reset
      "chatHistory": [],                <- reset
      "unlockedCGs": [                  <- Keep as i need this for Gallery unlocks CG
        "ChatB_CG1",
        "ChatB_CG2"
      ],
      "isInPauseState": true
    }
  }
```

### **Generated UI:**
```
Gallery: 3/6 (50%)               ← stats text

╔════════════════════════════════╗
║ Emma — 2/4                     ║  ← dynamically populated
║ [✓] [🔒] [✓] [🔒]             ║  ← CG grid
╠════════════════════════════════╣
║ Sarah — 1/2                    ║
║ [✓] [🔒]                       ║
╚════════════════════════════════╝

    │   └── GalleryPanel
    │       ├── GalleryHeader
    │       │   ├── HeaderTitle
    │       │   ├── Icon-search
    │       │   ├── Ivon-3Dot
    │       └── ScrollView
    │           └── Viewport
    │               └── Content (Vertical Layout Group)
    │                   └── CGContainer (Vertical Layout Group)
    │                       ├── CGName (TextMeshProUGUI)       // e.g. "Emma — 2/4"
    │                       └── CGGrid (Grid Layout Group - 4 per cell) 
    │                            └── CGSlot (200x200)
    │                                └─ CropContainer
    │                                    ├─ RectTransform:
    │                                    │   ├─ Anchors: Stretch (0,0,1,1)
    │                                    │   ├─ Left: 0, Top: 0, Right: 0, Bottom: 0
    │                                    ├─ Mask:
    │                                    │   └─ Show Mask Graphic: ❌ UNCHECKED
    │                                    ├─ Image:
    │                                    │   ├─ Color: White (255, 255, 255, 255) ← IMPORTANT!
    │                                    │   ├─ Source Image: None (leave empty)
    │                                    │   └─ Raycast Target: ❌ (optional)
    │                                    └─ CGImage (child)
    │                                        ├─ RectTransform:
    │                                        │   ├─ Anchors: Middle-Center (0.5, 0.5)
    │                                        │   ├─ Pivot: (0.5, 1.0) ← Top-center
    │                                        │   ├─ Width: 200
    │                                        │   ├─ Height: 360
    │                                        │   ├─ Pos X: 0
    │                                        │   ├─ Pos Y: 0
    │                                        └─ Image:
    │                                            ├─ Source Image: (test sprite - 9:16 portrait)
    │                                            ├─ Color: White (255, 255, 255, 255)
    │                                            ├─ Image Type: Simple
    │                                            ├─ Preserve Aspect: ✅ TRUE
    │                                            └─ Raycast Target: ✅ TRUE


the unlock CG are dynamically populate base on there character

### Build Settings:
```
[0] 00_Consent    ← Only has Canvas + ConsentManager
[1] 01_Bootstrap  ← Has GameManager + SaveManagers
[2] 02_Cutscene
[3] 03_MainUI
[4] 04_ChatApp
```

# FIXED no worries