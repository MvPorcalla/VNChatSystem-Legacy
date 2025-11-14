# **Future Improvements & To-Do List**

[1]

**VN Phone Chat System – Updated Structure**

---

# **Build Settings**

```
[0] 00_Consent       ← Lightweight, one-time only
[1] 01_Bootstrap     ← Managers only, loads next scene
[2] 02_MainUI        ← Lockscreen + HomeScreen + All Phone Apps
[3] 03_ChatApp       ← Heavy dynamic content (chat bubbles, messages, CGs)
[4] 04_Cutscenes     ← Story cutscenes (optional, additive)
```

---

# **02_MainUI Scene Structure**

**Lockscreen and Homescreen are now combined inside one scene.**

---

# **MAINUI HIERARCHY**

```
Hierarchy
├── MainCamera
├── EventSystem
├── Managers
│   └── HomePanelManager
│
└── Canvas
    └── PhoneFrame (1080x1920)
        ├── WallpaperContainer
        │   └── Wallpaper (Image)
        │
        ├── PhoneStatusbar
        │   ├── StatusbarBG
        │   ├── TimeText
        │   ├── SignalIcon
        │   └── BatteryIcon
        │
        ├── PhoneContent        ← HomePanelManager switches these children
        │
        │   ├── ═══════════════════ [LOCKSCREEN] ═══════════════════
        │   ├── LockscreenUI
        │   │   ├── Background
        │   │   ├── Clock
        │   │   └── UnlockButton
        │
        │   ├── ═══════════════════ [HOME LAUNCHER] ═══════════════════
        │   ├── HomeScreenUI
        │   │   ├── Widget
        │   │   ├── AppsContainer      ← App icons here
        │   │   └── DockContainer      ← Bottom dock icons
        │
        │   ├── ═══════════════════ [APPS] ═══════════════════
        │   ├── GalleryContainer
        │   │   ├── GalleryPanel
        │   │   │   ├── GalleryHeader
        │   │   │   └── ScrollView
        │   │   └── GalleryFullView (overlay)
        │
        │   ├── SettingsPanel
        │   │   ├── SettingsHeader
        │   │   └── SettingsContent
        │
        │   ├── ContactsPanel
        │   │   ├── ContactsHeader
        │   │   └── ContactsList
        │
        │   ├── ProfilePanel
        │   │   ├── ProfileHeader
        │   │   └── ProfileContent
        │
        │   ├── ═══════════════════ [OVERLAYS] ═══════════════════
        │   ├── NotificationOverlay
        │   └── LoadingOverlay
        │
        └── PhoneButtons
            ├── BackButton
            ├── HomeButton
            └── ExitButton
```

---

# **Key Improvements You Should Implement Later**

### **1. HomePanelManager Logic**

* Controls which panel is visible (Lock → Home → Apps).
* Smooth transitions (fade, slide, scale).
* Handles back navigation and app-specific return states.

### **2. Overlay System**

* Notifications (badge + popup)
* Global loading spinner
* System alerts (e.g., “New Contact Unlocked”)

### **3. Global Phone State**

* IsLocked
* ActiveApp
* Background wallpaper
* Time sync
* Notification queue

### **4. Modular App Panels**

Each app should be:

* Self-contained
* Activated/deactivated through HomePanelManager
* Not talking to each other directly (use event bus or mediator)

### **5. Performance Future-Proofing**

* All app UIs disabled by default
* Load content lazily
* Preload images for gallery
* Pool views that can be reused (scroll items, cards)

---

[2]

# **TODO – VN Chat System**

**Make chat support multiple characters in one story scene.**

---

[3]

# **TODO – Debug System**

**Refactor DebugHelper and standardize debug logs across all scripts.**

---

[4]

Here’s an updated **simple TODO note** including all app panels:

---

[5]

# **TODO – App Panels**

* Finish all remaining app panels:

  * Contacts → Reset story per character
  * Gallery → polish UI, scrolling, full-view, image quality
  * Settings → volume, text speed, theme options
  * Profile → show character info, stats, unlocked CGs
  * Any other app panels you plan to add
* Ensure all panels are integrated with HomePanelManager.

---

Here’s a simple note for that:

---

[6]

# **TODO – Documentation**

**Refactor and organize project documentation for clarity and consistency.**