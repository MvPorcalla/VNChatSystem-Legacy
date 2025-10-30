# Unity Chat System Documentation

This folder contains documentation for the **Unity Chat System**, designed for a fake phone-style VN/chat game.  
It covers both the **architecture logic** and the **UI hierarchy setup** for easier reference and maintenance.

---

## 📂 Documents

### 1. Architecture
**File:** `Architecture_SinglePanelMultiCharacter.md`  
- Explains the **Single Panel Multi-Character** (dynamic contact switching) pattern  
- Covers separation of concerns (UI, controller, data)  
- Guides on state management, switching characters, and optimization  
- Best practices for memory efficiency and scalability  

---

### 2. UI Hierarchy
**File:** `Hierarchy_UIChatStructure.md`  
- Visual breakdown of the **Canvas → PhoneFrame → ChatPanel** setup  
- Shows where chat bubbles, headers, and buttons live in the scene  
- Useful for prefab organization and scene consistency  

---

## 🧩 How They Fit Together
- **Architecture Doc:** Explains *how the system works*  
- **Hierarchy Doc:** Explains *where things live*  

Think of it like this:
- **Blueprint (Architecture)** = Rules, flow, and system logic  
- **Wiring Diagram (Hierarchy)** = Exact placement in Unity’s UI tree  

---

## 🚀 Usage Notes
- Start with **Architecture** to understand the logic behind the chat system  
- Use **Hierarchy** when setting up prefabs and Canvas objects in Unity  
- Keep both updated when changes are made to avoid mismatched docs  

---

_Last Updated: YYYY-MM-DD_
