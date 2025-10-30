# IsekaiChatNTR - Improved Unity Folder Structure

```
IsekaiChatNTR/
└── Assets/
    ├── Art/
    │   ├── Phone/
    │   │   ├── phone_frame.png                 // Phone bezel/border
    │   │   ├── screen_overlay.png              // Screen effects
    │   │   ├── status_icons.png                // Battery, signal, etc.
    │   │   ├── fingerprint_scanner.png         // Lock screen scanner
    │   │   └── home_wallpapers/                // Multiple wallpaper options
    │   │       ├── default_wallpaper.png
    │   │       ├── romantic_wallpaper.png
    │   │       └── dark_wallpaper.png
    │   │
    │   ├── Chat/
    │   │   ├── bubble_sent.png                 // Player message bubble
    │   │   ├── bubble_received.png             // Wife message bubble
    │   │   ├── choice_button.png               // Choice button background
    │   │   ├── typing_dots.png                 // Typing indicator sprites
    │   │   └── chat_backgrounds/               // Chat background options
    │   │       ├── default_chat_bg.png
    │   │       └── romantic_chat_bg.png
    │   │
    │   ├── Apps/
    │   │   ├── chat_app_icon.png        // Chat app icon
    │   │   ├── gallery_app_icon.png     // Gallery app icon
    │   │   ├── settings_app_icon.png    // Settings app icon
    │   │   └── future_app_icons/        // Placeholder for mini-apps
    │   │       ├── game_app_icon.png
    │   │       └── music_app_icon.png
    │   │
    │   └── Common/
    │       ├── loading_spinner.png      // Bootstrap loading animation
    │       ├── popup_background.png     // Generic popup background
    │       └── button_states/           // Universal button assets
    │           ├── button_normal.png
    │           ├── button_pressed.png
    │           └── button_disabled.png
    │
    
    ├── Characters/
    │   ├── sample
    │   │   ├── cg/
    │   │   │   ├── smp_cg1.png
    │   │   │   └── smp_cg2.png
    |   │   |
    │   │   ├── profile/
    │   │   │   ├── smp_Profile.png
    │   │   │   └── smp_sample.asset
    |   │   |
    │   │   └── dialogue/
    │   │       ├── smp_chapter1.mugi
    │   │       └── smp_chapter2.mugi

    |
    ├── Characters/
    │   ├── Belinda/
    │   │   ├── Profile/
    │   │   │   └── belinda_portrait.png
    │   │   ├── Shared/
    │   │   │   ├── belinda_intro.png
    │   │   │   ├── belinda_battle.png
    │   │   │   └── belinda_contract.png
    │   │   ├── Vanilla_Route/
    │   │   │   ├── belinda_loyalty.png
    │   │   │   ├── belinda_reunion.png
    │   │   │   └── belinda_wedding.png
    │   │   ├── NTR_Route/
    │   │   │   ├── belinda_temptation.png
    │   │   │   ├── belinda_betrayal.png
    │   │   │   └── belinda_corruption.png
    │   │   └── Dialogue/
    │   │       ├── belinda_chap1.json
    │   │       ├── belinda_chap2.json
    │   │       └── belinda_chap3.json
    │   └── Irina/
    │       ├── Profile/
    │       │   └── irina_portrait.png
    │       ├── Shared/
    │       ├── Vanilla_Route/
    │       ├── NTR_Route/
    │       └── Dialogue/
    │   
    ├── Scripts/
    │   ├── DialogueSystem/
    │   │   ├── DialogueNode.cs             // Individual dialogue node
    │   │   ├── DialogueData.cs             // Full conversation data
    │   │   ├── DialogueParser.cs           // Load/parse dialogue files
    │   │   └── DialogueRunner.cs           // Runtime dialogue execution
    │   │
    │   ├── Managers/
    │   │   ├── CoreGameData.cs              // Main game state, badPoints, unlockedImages
    │   │   ├── GameManager.cs               // Central manager (singleton bootstrap)
    │   │   ├── SaveManager.cs               // JSON autosave/load system
    │   │   ├── SceneManager.cs              // Scene transitions with persistence
    │   │   └── EventManager.cs              // Global event system for UI updates
    │   │
    │   ├── Characters/
    │   │   ├── CharacterData.cs            // ScriptableObject for character profiles
    │   │   ├── CharacterManager.cs         // Handle multiple characters
    │   │   ├── ContactProfile.cs           // Individual contact data structure
    │   │   └── RouteManager.cs             // Handle Vanilla/NTR route 
    │   │
    │   ├── UI/
    │   │   ├── AutoResizeText.cs           // Your existing script
    │   │   ├── ChatBubble.cs               // Individual bubble behavior
    │   │   ├── ContactButton.cs            // Character button in contact list
    │   │   └── ChatUIManager.cs            // Handles UI panel switching
    │   │    
    │   ├── UI/
    │   │   ├── Scenes/
    │   │   │   ├── BootstrapUI.cs           // Bootstrap scene UI (loading, initialization)
    │   │   │   ├── CutsceneUI.cs            // One-time cutscene logic
    │   │   │   ├── LockScreenUI.cs          // Fingerprint unlock simulation
    │   │   │   ├── HomeScreenUI.cs          // App launcher (Chat, Gallery, Settings)
    │   │   │   ├── ChatAppUI.cs             // Main chat interface
    │   │   │   ├── GalleryUI.cs             // Unlocked CG viewer
    │   │   │   └── SettingsUI.cs            // Game settings
    │   │   │
    │   │   ├── Components/
    │   │   │   ├── MessageBubble.cs         // Individual chat message
    │   │   │   ├── ChoiceButton.cs          // Dialogue choice button
    │   │   │   ├── TypingIndicator.cs       // "..." typing animation
    │   │   │   ├── NotificationIcon.cs      // Notification badges
    │   │   │   ├── CGImageDisplay.cs        // Gallery image viewer
    │   │   │   ├── AppIcon.cs               // Home screen app icons
    │   │   │   └── LoadingSpinner.cs        // Bootstrap loading animation
    │   │   │
    │   │   ├── Phone/
    │   │   │   ├── PhoneFrame.cs            // Phone bezel/frame controller
    │   │   │   ├── StatusBar.cs             // Time, battery, signal (real-time)
    │   │   │   ├── WallpaperManager.cs      // Dynamic wallpaper system
    │   │   │   ├── ScreenTransition.cs      // Scene transition effects
    │   │   │   └── FingerprintScanner.cs    // Lock screen unlock logic
    │   │   │
    │   │   └── Common/
    │   │       ├── UIManager.cs             // Base UI management
    │   │       ├── PopupManager.cs          // Handle popups/dialogs
    │   │       ├── TooltipSystem.cs         // Tooltip display
    │   │       └── ButtonAnimator.cs        // Universal button animations
    │   │
    │   ├── Audio/
    │   │   ├── AudioManager.cs              // Master audio controller (singleton)
    │   │   ├── ChatSFX.cs                   // Message send/receive sounds
    │   │   ├── BGMController.cs             // Background music management
    │   │   ├── VibrationManager.cs          // Phone vibration effects
    │   │   └── AudioSettings.cs             // Audio preferences
    │   │
    │   ├── Utils/
    │   │   ├── SceneLoader.cs               // Scene management with autosave
    │   │   ├── TextAnimator.cs              // Typewriter text effect
    │   │   ├── ImageUnlocker.cs             // Handle CG unlock logic
    │   │   ├── BadPointsTracker.cs          // Track and evaluate bad ending points
    │   │   ├── TimeManager.cs               // Real-time clock for phone
    │   │   ├── DeviceSimulator.cs           // Simulate phone behaviors
    │   │   ├── StringExtensions.cs          // String parsing helpers
    │   │   └── SingletonBase.cs             // Generic singleton pattern
    │   │
    │   ├── Data/
    │   │   ├── SaveData.cs                  // Save file structure
    │   │   ├── GameConstants.cs             // Game-wide constants
    │   │   ├── SceneDatabase.cs             // Scene reference management
    │   │   └── ImageDatabase.cs             // CG image catalog
    │   │
    │   └── Editor/
    │       ├── DialogueImporter.cs          // Import .yarn files to ScriptableObjects
    │       ├── GameDataEditor.cs            // Custom inspector for game state
    │       ├── ChatDebugger.cs              // Debug dialogue flow and variables
    │       ├── BuildProcessor.cs            // Custom build pipeline
    │       └── SceneValidator.cs            // Validate scene setup
    │
    ├── Resources/
    │   └── Characters/
    │       ├── BelindaData.asset
    │       └── IrinaData.asset
    │
    ├── Scenes/
    │   ├── 00_Bootstrap.unity               // Manager singletons initialization
    │   ├── 01_Cutscene.unity                // Story intro (plays once)
    │   ├── 02_LockScreen.unity              // Phone unlock simulation
    │   ├── 03_HomeScreen.unity              // Main phone interface
    │   ├── 04_ChatApp.unity                 // Primary gameplay scene
    │   ├── 05_Gallery.unity                 // CG viewer
    │   ├── 06_Settings.unity                // Game settings
    │   └── 99_TestScene.unity               // Development testing scene
    │
    ├── Prefabs/
    │   ├── Managers/
    │   │   ├── [Core] GameManager.prefab           // Bootstrap manager with DontDestroyOnLoad
    │   │   ├── [Core] SaveManager.prefab           // Persistent save system
    │   │   ├── [Core] AudioManager.prefab          // Audio management
    │   │   ├── [Core] DialogueManager.prefab       // Dialogue system
    │   │   └── [Core] SceneManager.prefab          // Scene transition manager
    │   │
    │   ├── UI/
    │   │   ├── Phone/
    │   │   │   ├── PhoneFrame.prefab               // Complete phone UI container
    │   │   │   ├── StatusBar.prefab                // Top status bar with real time
    │   │   │   ├── HomeScreen.prefab               // App grid layout
    │   │   │   ├── FingerprintScanner.prefab       // Lock screen unlock UI
    │   │   │   └── NotificationPanel.prefab        // Future notification system
    │   │   │
    │   │   ├── Chat/
    │   │   │   ├── MessageBubble_Sent.prefab       // Player messages (blue/right)
    │   │   │   ├── MessageBubble_Received.prefab   // Wife messages (gray/left)
    │   │   │   ├── ChoiceButton.prefab             // Dialogue choice
    │   │   │   ├── TypingIndicator.prefab          // "..." animation
    │   │   │   ├── ChatHeader.prefab               // Contact info header
    │   │   │   └── MessageTimestamp.prefab         // Time stamps for messages
    │   │   │
    │   │   ├── Gallery/
    │   │   │   ├── CGThumbnail.prefab              // Gallery grid item
    │   │   │   ├── CGViewer.prefab                 // Full image viewer
    │   │   │   ├── LockedImage.prefab              // Locked CG placeholder
    │   │   │   └── GalleryNavigation.prefab        // Gallery navigation controls
    │   │   │
    │   │   ├── Common/
    │   │   │   ├── LoadingScreen.prefab     // Bootstrap loading UI
    │   │   │   ├── PopupDialog.prefab       // Generic popup
    │   │   │   ├── ConfirmDialog.prefab     // Yes/No confirmation
    │   │   │   └── TooltipPanel.prefab      // Tooltip display
    │   │   │
    │   │   └── Effects/
    │   │       ├── ScreenGlow.prefab        // Phone screen glow effect
    │   │       ├── MessageAppear.prefab     // Message slide-in animation
    │   │       ├── UnlockEffect.prefab      // CG unlock celebration
    │   │       ├── SceneTransition.prefab   // Fade/slide transitions
    │   │       └── VibrationEffect.prefab   // Visual vibration feedback
    │   │
    │   └── Debug/
    │       ├── DebugConsole.prefab          // In-game debug console
    │       ├── DialogueDebugger.prefab      // Live dialogue testing
    │       └── SaveDataViewer.prefab        // Inspect save data
    │
    │
    ├── StreamingAssets/
    │   │
    │   ├── Localization/
    │   │   ├── english.json                 // English text
    │   │   ├── japanese.json                // Future localization
    │   │   └── localization_keys.txt        // Reference for translators
    │   │
    │   └── Config/
    │       ├── build_settings.json          // Build configuration
    │       └── debug_config.json            // Debug mode settings
    │
    │
    ├── Audio/
    │   ├── SFX/
    │   │   ├── Chat/
    │   │   │   ├── message_send.wav         // Player sends message
    │   │   │   ├── message_receive.wav      // Receive message from wife
    │   │   │   ├── typing_sound.wav         // Typing indicator SFX
    │   │   │   ├── choice_select.wav        // Choice button click
    │   │   │   └── message_swoosh.wav       // Message animation sound
    │   │   │
    │   │   ├── UI/
    │   │   │   ├── app_open.wav             // Open app sound
    │   │   │   ├── app_close.wav            // Close app sound
    │   │   │   ├── unlock_phone.wav         // Unlock fingerprint sound
    │   │   │   ├── cg_unlock.wav            // Image unlock celebration
    │   │   │   ├── notification.wav         // New message notification
    │   │   │   ├── button_click.wav         // Generic button press
    │   │   │   └── error_sound.wav          // Error/invalid action
    │   │   │
    │   │   └── Phone/
    │   │       ├── vibration.wav            // Phone vibration effect
    │   │       ├── screen_on.wav            // Screen activation
    │   │       └── lock_sound.wav           // Phone lock sound
    │   │
    │   ├── Music/
    │   │   ├── main_theme.ogg               // Relaxed main theme
    │   │   └── ambient_chat.ogg             // Subtle chat background
    │   │
    │   └── Voice/ (Future)
    │       ├── wife_samples/                // Future voice acting
    │       │   ├── greeting.wav
    │       │   ├── laugh.wav
    │       │   └── sigh.wav
    │       │
    │       └── ui_voice/                    // UI voice feedback
    │           ├── unlock_voice.wav
    │           └── notification_voice.wav
    │
    ├── Materials/
    │   ├── UI/
    │   │   ├── ChatBubbleGradient.mat       // Message bubble shader
    │   │   ├── PhoneScreenShader.mat        // Screen glow/blur effects
    │   │   ├── CGImageMaterial.mat          // CG image display shader
    │   │   └── ButtonHighlight.mat          // Interactive button materials
    │   │
    │   └── Effects/
    │       ├── UnlockGlow.mat               // CG unlock effect
    │       ├── TypingPulse.mat              // Typing indicator animation
    │       ├── SelectionHighlight.mat       // Choice button highlight
    │       └── ScreenTransition.mat         // Scene transition shader
    │
    ├── Animations/
    │   ├── UI/
    │   │   ├── MessageSlideIn.anim          // Message appear animation
    │   │   ├── MessageSlideOut.anim         // Message disappear (if needed)
    │   │   ├── TypingDots.anim              // Typing indicator loop
    │   │   ├── ChoiceFadeIn.anim            // Choice buttons appear
    │   │   ├── CGUnlock.anim                // Image unlock celebration
    │   │   ├── SceneTransition.anim         // Between scene transitions
    │   │   ├── PhoneUnlock.anim             // Fingerprint unlock animation
    │   │   └── AppOpen.anim                 // App opening animation
    │   │
    │   └── Controllers/
    │       ├── MessageAnimator.controller    // Message animation states
    │       ├── TypingAnimator.controller     // Typing animation states
    │       ├── UITransition.controller       // UI transition states
    │       ├── CGAnimator.controller         // Gallery image animations
    │       └── PhoneAnimator.controller      // Phone-wide animations
    │
    └── Documentation/
        ├── README.md                        // Project setup guide
        ├── DIALOGUE_SYNTAX.md               // Yarn syntax guide + macros
        ├── SAVE_SYSTEM.md                   // Save/load documentation
        ├── CG_INTEGRATION.md                // How to add new images
        ├── EXPANSION_GUIDE.md               // Adding new characters/features
        ├── BUILD_GUIDE.md                   // Build and deployment
        └── TROUBLESHOOTING.md               // Common issues and solutions

```
