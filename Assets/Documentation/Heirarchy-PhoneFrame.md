# UI Hierarchy – Phone Frame Structure

## Overview

This document outlines the **UI hierarchy and layout structure** for the Unity Phone.
It represents how all panels, containers, and prefabs are organized inside the `Canvas`, simulating a mobile phone interface.

---

## Hierarchy Breakdown

```
Canvas
├── PhoneFrame                              // Main phone container (1080x1920, acts like the phone screen)
│    ├── PhoneStatusbar                     // Fake status bar (time, signal, battery, etc.)
│    │   ├── ChatAppPanel                   // App icon / mini panel inside status bar (like Messenger top bar)
│    │   └── Image                          // Decorative background or icons (signal, WiFi, battery)
│    │
│    ├── PhoneContent                       // Main content area (the app screen itself where i put the contents per scene)
│    │     
│    └── PhoneButtons                       // Fake Android/iOS-like navigation buttons at the bottom
│        ├── BackButton                     // Simulates a back button
│        ├── HomeButton                     // Simulates home button (could return to phone home screen)
│        └── ExitButton                     // Closes the fake phone app / exits VN

```

---

## Notes

* **PhoneFrame** acts as the simulated device screen (fixed resolution 1080×1920).
* **PhoneStatusbar** is cosmetic — shows fake signal/battery/time.
* **ChatAppPanel** under `PhoneContent` is the **main chat window** where conversations happen.
* **ChatPanel/Content** dynamically spawns chat bubble prefabs (NPC, Player, System, CG).
* **ChatChoices** dynamically spawns choice buttons (Vertical Layout Group auto-stacks them).
* **PhoneButtons** simulate device navigation (optional VN features).

---

==============================================================================================

Scene: ConsentScene

```
PhoneContent
├── ConsentPanel
│   ├── ConsentContent                          <- wont change the width base on the consentPanel it overflowout
│   │   ├── Header
│   │   │   ├── Title
│   │   │   └── Icon
│   │   │
│   │   ├── ConsentScrollView (ScrollRect)      <- wont change the width base on the ConsentContent it overflowout
│   │   │   ├── Viewport
│   │   │   │   └── Content                     <- Vertical Layout Group + Content Size Fitter
│   │   │   │        └── ConsentText
│   │   │
│   │   └── ConsentButtons
│   │        ├── ButtonAgree
│   │        │   └── AgreeText 
│   │        └── ButtonExit
                 └── ExitText 
```

---

Consent Disclaimer (18+)

⚠ Warning: NSFW Content
This game contains adult content intended for mature audiences only.

All characters, events, and locations in this game are entirely fictional. Any resemblance to real persons or events is purely coincidental.

By clicking "I am 18+", you explicitly confirm that you are at least 18 years old (or the legal age in your country) and consent to view adult content.

Terms Agreement
By entering the game, you agree to these terms and acknowledge that the game saves your progress locally on your device. No personal information is collected.

---

[ I am 18+ ]  [ Exit Game ]

--