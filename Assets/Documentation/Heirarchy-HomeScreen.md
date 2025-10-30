# UI Hierarchy – HomeScreen Structure

## Overview

This document outlines the **UI hierarchy and layout structure** for the Unity HomeScreen.
It represents how all panels, containers, and prefabs are organized inside the `Canvas`, simulating a mobile phone interface.

---

## HomeScreen Hierarchy Breakdown

```
Canvas
├── PhoneFrame                              // Main phone container (1080x1920, acts like the phone screen)
│    ├── WallpaperContainer                 
│    │   └── Wallpaper (Image)              // Walpaper
│    ├── PhoneStatusbar                     // Fake status bar (time, signal, battery, etc.)
│    │   ├── ChatAppPanel                   // App icon / mini panel inside status bar (like Messenger top bar)
│    │   └── Image                          // Decorative background or icons (signal, WiFi, battery)
│    │
│    ├── PhoneContent                       // Main content area (the app screen itself where i put the contents per scene)
│    │   └── HomeScreenPanel                (VLG)
│    │          ├── Widget                  // 
│    │          ├── AppsContainer           // main apps grid
│    │          │    └── AppButton (VLG)    // button
│    │          │        ├── AppBG 
│    │          │        │   └── AppIcon (Image, LE)      
│    │          │        └── AppName (Text, LE)
│    │          ├── AppSpacer                  // Space Betwween container
│    │          └── DockContainer           // favorite apps at the bottom
│    │          │    └── AppButton (VLG)    // button
│    │          │        ├── AppBG 
│    │          │        │   └── AppIcon (Image, LE)      
│    │          │        └── AppName (Text, LE)
│    │     
│    └── PhoneButtons                       // Fake Android/iOS-like navigation buttons at the bottom
│        ├── BackButton                     // Simulates a back button
│        ├── HomeButton                     // Simulates home button (could return to phone home screen)
│        └── ExitButton                     // Closes the fake phone app / exits VN

```
---