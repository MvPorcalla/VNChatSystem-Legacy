# UI Hierarchy – HomeScreen Structure

## Overview

This document outlines the **UI hierarchy and layout structure** for the Unity HomeScreen.
It represents how all panels, containers, and prefabs are organized inside the `Canvas`, simulating a mobile phone interface.

---

## HomeScreen Hierarchy Breakdown

```
Canvas
└── PhoneFrame                              // Main phone container (1080x1920, acts like the phone screen)
    ├── WallpaperContainer                 
    │   └── Wallpaper (Image)               // Walpaper
    ├── PhoneStatusbar                      // Fake status bar (time, signal, battery, etc.)
    │   ├── ChatAppPanel                    // App icon / mini panel inside status bar (like Messenger top bar)
    │   └── Image                           // Decorative background or icons (signal, WiFi, battery)
    │
    ├── PhoneContent                        // Main content area (the app screen itself where i put the contents per scene)
    │   └── HomeScreenPanel                 (VLG)
    │   │   ├── Widget                      // 
    │   │   ├── AppsContainer               // main apps grid
    │   │   │    └── AppButton (VLG)        // button
    │   │   │        ├── AppBG 
    │   │   │        │   └── AppIcon (Image, LE)      
    │   │   │        └── AppName (Text, LE)
    │   │   ├── AppSpacer                   // Space Betwween container
    │   │   └── DockContainer               // favorite apps at the bottom
    │   │       └── AppButton (VLG)         // button
    │   │           ├── AppBG 
    │   │           │   └── AppIcon (Image, LE)      
    │   │           └── AppName (Text, LE)
    │   │   
    │   └── GalleryContainer
    │       ├── GalleryPanel
    │       │   ├── GalleryHeader
    │       │   │   ├── TitleText
    │       │   │   ├── ProgressText
    │       │   │   ├── Icon-search
    │       │   │   └── Ivon-3Dot
    │       │   └── ScrollView
    │       │       └── Viewport
    │       │           └── Content (Vertical Layout Group)
    │       │               └── CGContainer (Vertical Layout Group) (Prefab)
    │       │                   ├── CGName (TextMeshProUGUI)       // e.g. "Emma — 2/4"
    │       │                   └── CGGrid (Grid Layout Group - 4 per cell) 
    │       │                        └── CGSlot (200x200) (Prefab)
    │       │                            └─ CropContainer
    │       │                                ├─ RectTransform:
    │       │                                │   ├─ Anchors: Stretch (0,0,1,1)
    │       │                                │   ├─ Left: 0, Top: 0, Right: 0, Bottom: 0
    │       │                                ├─ Mask:
    │       │                                │   └─ Show Mask Graphic: ❌ UNCHECKED
    │       │                                ├─ Image:
    │       │                                │   ├─ Color: White (255, 255, 255, 255) ← IMPORTANT!
    │       │                                │   ├─ Source Image: None (leave empty)
    │       │                                │   └─ Raycast Target: ❌ (optional)
    │       │                                └─ CGImage (child)
    │       │                                    ├─ RectTransform:
    │       │                                    │   ├─ Anchors: Middle-Center (0.5, 0.5)
    │       │                                    │   ├─ Pivot: (0.5, 1.0) ← Top-center
    │       │                                    │   ├─ Width: 200
    │       │                                    │   ├─ Height: 360
    │       │                                    │   ├─ Pos X: 0
    │       │                                    │   ├─ Pos Y: 0
    │       │                                    └─ Image:
    │       │                                        ├─ Source Image: (test sprite - 9:16 portrait)
    │       │                                        ├─ Color: White (255, 255, 255, 255)
    │       │                                        ├─ Image Type: Simple
    │       │                                        ├─ Preserve Aspect: ✅ TRUE
    │       │                                        └─ Raycast Target: ✅ TRUE
    │       └── GalleryFullView
    │           ├── BGOverly            // BG
    │           ├── CGHeader
    │           │   ├── CloseButton
    │           │   └── CGName          // NPC name
    │           └── CGDisplay
    │               └── CGImage         // CG full view
    │
    └── PhoneButtons                       // Fake Android/iOS-like navigation buttons at the bottom
        ├── BackButton                     // Simulates a back button
        ├── HomeButton                     // Simulates home button (could return to phone home screen)
        └── ExitButton                     // Closes the fake phone app / exits VN

```
---