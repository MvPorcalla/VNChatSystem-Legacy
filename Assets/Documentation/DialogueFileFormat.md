# MugiNovel Chat Script Legend

**Engine / Format Name:** MugiNovel (Mugi)  
**File Extension:** `.mugi`  
**Purpose:** Script files in this format contain dialogue, media, and branching choices for **chat-focused visual novels**. Each file can represent a chapter, scene, or node sequence. Each .mugi file usually represents one chapter/scene to make file organization explicit.

These files support:  
- Text bubbles  
- Image/audio media bubbles  
- Player choices and branching paths  
- Variables and conditional dialogue  

**Example File Name:** `C1_Intro.mugi`  

### Note: All character profile images, CGs, and media assets are loaded via Unity’s Addressables system. Do not hard-reference sprites in prefabs. The parser and ChatManager should request assets by their addressable keys (from the .mugi path: field), then asynchronously load them before displaying in chat bubbles. Make sure image loading is asynchronous, with fallback/error handling if an asset cannot be found.

## **Legend for Chat Script Commands**

| Symbol / Command    | Purpose                 | Notes / Usage                                                                               |
|-------------------------------------------------------------------------------------------------------------------------------------------  |

| `contact: Name`     | **Contact Assignment**  | Defines which **character/contact** this node belongs to. Place at the top of each .mugi file to specify the NPC or chat thread.                                                                                                                                  |
| Example: `contact: Elena`                                                                                                                   |
| “Place contact: at the top of each .mugi file to define which NPC or chat thread this file represents.”                                     |
|  
| `title: NodeName`   | **Node Header**         | Marks the start of a conversation or branch node.                                           |
| Example: `title: C1_Start`                                                                                                                  |
| 
| `---`               | **Node Separator**      | Separates node header from its content. Required after `title:`                             |
|
| `[Speaker]:`        | **Text Bubble**         | Marks a line of dialogue. The parser displays it as a text bubble.                          |
| Example: `NPC: "Hi! Let's go out."`                                                                                                         |
|
| `>> media [Speaker] type:image path:` | **CG/Image Bubble**     | Marks a line that shows a character image in a bubble.                    |
| Example: `>> media npc type:image path:npc_happy.png`                                                                                       |
|
|
| `-> ...`            | **Tap-to-Continue**     | Creates a **single continue button** (like a choice) that the player must tap to proceed.   |
| Example: `-> ...`                                                                                                                           |
|
| `>> choice`         | **Player Choice Block** | Signals the start of a branching choice menu. Buttons are generated for each option.        |
|
| `-> "Choice Text"`  | **Individual Choice**   | Defines a button with the text the player sees. The indented block after it is executed when chosen. 
| Example: `-> "Let's meet at the park"`                                                                                                      |
| `# Player:`         | **Player Dialogue**     | Lines that start with a speaker label (# Player:) immediately following a choice are messages that should be displayed in the chat as if the player typed them.
| 
| `<<jump NodeName>>` | **Jump to Node**        | Moves the script to another node. Used for branching paths or ending nodes.                 |
| Example: `<<jump C1_ParkNode>>`                                                                                                             |
|
| `//`                | **Comment**             | Anything after `//` is ignored by the parser. Useful for notes or instructions.             |
| Example: `// Shows NPC happy in the bubble`                                                                                                 |
---

---

| Part            | Meaning                                                                                                     |
| --------------- | ----------------------------------------------------------------------------------------------------------- |
| `>> media`      | Marks this line as a **command to display media content** (image, audio, etc.) inside a chat bubble.        |
| `[Speaker]`     | Specifies **who the bubble belongs to** — e.g., `NPC` or `Player`. Determines where the media is displayed. |
| `type:`         | Defines the **type of media** being shown. Examples: `image`, `audio`.                                      |
| `path:`         | The **file path of the media file** to load and display/play.                                               |
| `npc_happy.png` | Example of an **image file path** (for an NPC’s image bubble).                                              |
| `npc_line1.ogg` | Example of an **audio file path** (for an NPC’s voice or sound bubble).                                     |

---

//=====================================
// MAIN CHAT SCRIPT
// Dialogue script with text bubbles and CG/image bubbles
//=====================================

contact: NPC_Elena  

title: C1_Start
---

System: "4:50 PM"

NPC: "Hi! Let's go out."
NPC: "I want coffee."
>> media npc type:image path:npc_happy.png                  // Shows NPC happy in the bubble

-> ...                                                      // Tap to continue

Player: "Ok, let's go."
Player: "Blah blah."
>> media player type:image path:player_confident.png        // Player sends image

NPC: "Sounds good!"
NPC: "Where should we meet?"
NPC: "I hope the coffee shop is still open."
>> media npc type:image path:npc_anxious.png                // NPC changes expression

>> choice
    -> "Let's meet at the park"
        # Player: "Let's meet at the park."
        <<jump C1_ParkNode>>

    -> "Let's meet at the cafe"
        # Player: "Let's meet at the cafe."
        <<jump C1_CafeNode>>

===

//=====================================
// PARK NODE
//=====================================

title: C1_ParkNode
---
NPC: "Tea drinkers are classy. 🍵"
NPC: "You have my respect."

-> ...                                                      // Tap to continue

Player: "Thank you."
NPC: "Anytime!"
>> media npc type:image path:npc_smug.png                   // NPC smug expression

<<jump EndNode>>

===

//=====================================
// CAFE NODE
//=====================================

title: C1_CafeNode
---
NPC: "Coffee lovers unite! ☕"
NPC: "Here, take this cup of joy."

-> ...                                                      // Tap to continue

Player: "Thanks!"
NPC: "You're welcome!"
>> media npc type:image path:npc_winking.png                      // NPC winking expression

<<jump EndNode>>

===

//=====================================
// END NODE
//=====================================

title: EndNode
---
NPC: "Anyway, that’s the end of this little chat. Thanks for playing! 🎉"


------
