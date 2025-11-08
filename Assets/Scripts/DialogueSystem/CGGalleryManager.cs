//=====================================
// CGGalleryManager.cs - Dynamic Gallery with NPCChatData
//=====================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;
using static ChatDialogueSystem.DebugHelper;

namespace ChatDialogueSystem
{
    /// <summary>
    /// Manages the CG Gallery UI with dynamic population from save data.
    /// Automatically creates sections for each character with unlocked CGs.
    /// </summary>
    public class CGGalleryManager : MonoBehaviour
    {
        [Header("Gallery UI References")]
        [SerializeField] private Transform contentContainer; // The "Content" transform in ScrollView
        [SerializeField] private GameObject cgContainerPrefab; // The CGContainer prefab
        [SerializeField] private GameObject cgSlotPrefab; // The CGSlot prefab
        [SerializeField] private TextMeshProUGUI CGstatProgress; // Text to show overall progress

        [Header("Full View")]
        [SerializeField] private GameObject fullViewPanel;
        [SerializeField] private Image fullViewImage;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI cgTitleText;

        [Header("NPC Chat Data")]
        [Tooltip("Drag your NPCChatData ScriptableObjects here")]
        [SerializeField] private List<NPCChatData> npcChatDataList = new List<NPCChatData>();

        [Header("Display Options")]
        [SerializeField] private bool showLockedCGs = true;
        [SerializeField] private bool showEmptySections = false; // Show characters with 0 CGs unlocked?
        [SerializeField] private Sprite lockedCGSprite; // Optional: sprite to show for locked CGs

        private List<GameObject> galleryObjects = new List<GameObject>();
        private Dictionary<string, AsyncOperationHandle<Sprite>> loadedSprites =
            new Dictionary<string, AsyncOperationHandle<Sprite>>();

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        private void Start()
        {
            SetupUI();
            RefreshGallery();
        }

        private void SetupUI()
        {
            if (fullViewPanel != null)
            {
                fullViewPanel.SetActive(false);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners(); // Clear existing listeners
                closeButton.onClick.AddListener(CloseFullView);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ DYNAMIC GALLERY POPULATION
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Dynamically builds the gallery from save data.
        /// Creates a section for each character with CGs.
        /// </summary>
        public void RefreshGallery()
        {
            ClearGallery();

            var dialogueSaveManager = DialogueSaveManager.Instance;
            if (dialogueSaveManager == null)
            {
                LogError(Category.ChatManager, "DialogueSaveManager not found!");
                return;
            }

            if (npcChatDataList == null || npcChatDataList.Count == 0)
            {
                LogError(Category.ChatManager, "⚠️ No NPCChatData assigned! Drag your .asset files into the inspector.");
                return;
            }

            int totalUnlocked = 0;
            int totalCGs = 0;

            // Build a mapping of character definitions to chat states
            var characterSections = BuildCharacterSections(dialogueSaveManager);

            // Create UI for each character with CGs
            foreach (var section in characterSections)
            {
                // Skip if no CGs defined in ScriptableObject
                if (section.npcData.allCGAddressableKeys == null || section.npcData.allCGAddressableKeys.Count == 0)
                {
                    LogWarning(Category.ChatManager, $"Skipping {section.displayName} (no CGs defined in ScriptableObject)");
                    continue;
                }

                // Skip if no CGs unlocked and we're hiding empty sections
                if (!showEmptySections && section.unlockedCGs.Count == 0)
                {
                    Log(Category.ChatManager, $"Skipping {section.displayName} (0 CGs unlocked)");
                    continue;
                }

                // Count stats
                totalUnlocked += section.unlockedCGs.Count;
                totalCGs += section.npcData.allCGAddressableKeys.Count;

                // ✅ CREATE CHARACTER SECTION
                CreateCharacterSection(section.npcData, section.displayName, section.unlockedCGs);
            }

            // Update overall stats
            UpdateStatsDisplay(totalUnlocked, totalCGs);

            Log(Category.ChatManager, $"[CGGallery] Displayed {totalUnlocked}/{totalCGs} CGs");
        }

        /// <summary>
        /// Maps NPCChatData to their unlocked CGs from save data.
        /// </summary>
        private List<CharacterSection> BuildCharacterSections(DialogueSaveManager saveManager)
        {
            var sections = new List<CharacterSection>();

            foreach (var npcData in npcChatDataList)
            {
                if (npcData == null)
                {
                    LogWarning(Category.ChatManager, "Null NPCChatData in list! Check your inspector assignments.");
                    continue;
                }

                var section = new CharacterSection
                {
                    npcData = npcData,
                    unlockedCGs = new HashSet<string>()
                };

                // Get chat state for this NPC using their chatID
                var chatState = saveManager.GetChatState(npcData.ChatID);

                if (chatState != null)
                {
                    // Collect all unlocked CGs from save data
                    if (chatState.unlockedCGs != null)
                    {
                        foreach (var cgPath in chatState.unlockedCGs)
                        {
                            section.unlockedCGs.Add(cgPath);
                        }
                    }

                    // Use characterName from save data if available, fallback to ScriptableObject
                    section.displayName = !string.IsNullOrEmpty(chatState.characterName)
                        ? chatState.characterName
                        : npcData.characterName;

                    Log(Category.ChatManager, $"Loaded {npcData.characterName}: {section.unlockedCGs.Count}/{npcData.allCGAddressableKeys.Count} CGs unlocked");
                }
                else
                {
                    // No save data yet, use ScriptableObject name
                    section.displayName = npcData.characterName;
                    Log(Category.ChatManager, $"No save data for {npcData.ChatID}, showing 0 unlocked");
                }

                sections.Add(section);
            }

            return sections;
        }

        /// <summary>
        /// Creates a section for a single character with header and CG grid.
        /// </summary>
        private void CreateCharacterSection(
            NPCChatData npcData,
            string displayName,
            HashSet<string> unlockedCGs)
        {
            if (cgContainerPrefab == null || contentContainer == null)
            {
                LogError(Category.ChatManager, "Missing cgContainerPrefab or contentContainer!");
                return;
            }

            // Instantiate CGContainer prefab
            GameObject cgContainer = Instantiate(cgContainerPrefab, contentContainer);
            galleryObjects.Add(cgContainer);

            // ✅ VALIDATE PREFAB STRUCTURE FIRST
            if (cgContainer.transform.childCount < 2)
            {
                LogError(Category.ChatManager,
                    $"CGContainer prefab has invalid structure! Expected 2 children (CGName, CGGrid), found {cgContainer.transform.childCount}");
                Destroy(cgContainer);
                galleryObjects.Remove(cgContainer);
                return;
            }

            // ✅ FIND CGName (TextMeshProUGUI) - should be first child
            TextMeshProUGUI cgNameText = cgContainer.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            if (cgNameText != null)
            {
                int unlockedCount = unlockedCGs.Count;
                int totalCount = npcData.allCGAddressableKeys.Count;

                cgNameText.text = $"{displayName} — {unlockedCount}/{totalCount}";
            }
            else
            {
                LogWarning(Category.ChatManager, "CGName TextMeshProUGUI not found! Check CGContainer prefab structure.");
            }

            // ✅ FIND CGGrid - should be second child (now guaranteed to exist)
            Transform cgGrid = cgContainer.transform.GetChild(1);

            // ✅ POPULATE CG SLOTS
            foreach (string cgPath in npcData.allCGAddressableKeys)
            {
                bool isUnlocked = unlockedCGs.Contains(cgPath);

                // Skip locked CGs if we're hiding them
                if (!showLockedCGs && !isUnlocked)
                    continue;

                CreateCGSlot(cgPath, isUnlocked, cgGrid);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ CG SLOT CREATION
        // ═══════════════════════════════════════════════════════════

        private void CreateCGSlot(string cgPath, bool isUnlocked, Transform parent)
        {
            if (cgSlotPrefab == null)
            {
                LogWarning(Category.ChatManager, $"Missing cgSlotPrefab!");
                return;
            }

            // Instantiate CGSlot
            GameObject cgSlot = Instantiate(cgSlotPrefab, parent);
            galleryObjects.Add(cgSlot);

            // Find the CropContainer -> CGImage
            Transform cropContainer = cgSlot.transform.Find("CropContainer");
            if (cropContainer == null)
            {
                LogError(Category.ChatManager, "CropContainer not found in CGSlot! Check prefab structure.");
                return;
            }

            Transform cgImageTransform = cropContainer.Find("CGImage");
            if (cgImageTransform == null)
            {
                LogError(Category.ChatManager, "CGImage not found in CropContainer! Check prefab structure.");
                return;
            }

            Image cgImage = cgImageTransform.GetComponent<Image>();
            if (cgImage == null)
            {
                LogError(Category.ChatManager, "Image component not found on CGImage!");
                return;
            }

            if (isUnlocked)
            {
                // ✅ LOAD AND DISPLAY UNLOCKED CG
                StartCoroutine(LoadCGSprite(cgPath, cgImage));

                // Add button functionality to the entire CGSlot
                Button button = cgSlot.GetComponent<Button>();
                if (button == null)
                {
                    button = cgSlot.AddComponent<Button>();
                }
                button.onClick.RemoveAllListeners(); // ← FIX: Prevent duplicate listeners
                button.onClick.AddListener(() => ShowFullView(cgPath));
            }
            else
            {
                // ✅ SHOW LOCKED STATE
                if (lockedCGSprite != null)
                {
                    cgImage.sprite = lockedCGSprite;
                }
                else
                {
                    // Darken the image to indicate locked
                    cgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ IMAGE LOADING
        // ═══════════════════════════════════════════════════════════

        private IEnumerator LoadCGSprite(string cgPath, Image targetImage)
        {
            // Check cache first
            if (loadedSprites.ContainsKey(cgPath))
            {
                var cachedHandle = loadedSprites[cgPath];
                if (cachedHandle.IsValid() && cachedHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    if (targetImage != null)
                        targetImage.sprite = cachedHandle.Result;
                    yield break;
                }
                else
                {
                    if (cachedHandle.IsValid())
                        Addressables.Release(cachedHandle);
                    loadedSprites.Remove(cgPath);
                }
            }

            var handle = Addressables.LoadAssetAsync<Sprite>(cgPath);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (targetImage != null)
                {
                    targetImage.sprite = handle.Result;
                    loadedSprites[cgPath] = handle;
                    Log(Category.Addressables, $"Loaded CG: {cgPath}");
                }
                else
                {
                    // Image was destroyed during load, release immediately
                    LogWarning(Category.Addressables, $"Target image destroyed before load completed: {cgPath}");
                    Addressables.Release(handle);
                }
            }
            else
            {
                LogError(Category.Addressables, $"Failed to load CG: {cgPath} - Check if this key exists in Addressables!");
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ FULL VIEW
        // ═══════════════════════════════════════════════════════════

        private void ShowFullView(string cgPath)
        {
            if (fullViewPanel == null || fullViewImage == null)
            {
                LogWarning(Category.ChatManager, "Full view UI not configured");
                return;
            }

            // ✅ TELL HOMEPANELMANAGER ABOUT THE OVERLAY
            if (HomePanelManager.Instance != null)
            {
                HomePanelManager.Instance.OpenOverlay(fullViewPanel);
            }
            else
            {
                // Fallback if no HomePanelManager (shouldn't happen in your setup)
                fullViewPanel.SetActive(true);
                LogWarning(Category.ChatManager, "HomePanelManager not found! Navigation won't work properly.");
            }

            StartCoroutine(LoadFullViewSprite(cgPath));

            if (cgTitleText != null)
            {
                cgTitleText.text = System.IO.Path.GetFileNameWithoutExtension(cgPath);
            }

            Log(Category.ChatManager, $"[CGGallery] Showing full view: {cgPath}");
        }

        private IEnumerator LoadFullViewSprite(string cgPath)
        {
            if (loadedSprites.ContainsKey(cgPath))
            {
                var cachedHandle = loadedSprites[cgPath];
                if (cachedHandle.IsValid() && cachedHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    if (fullViewImage != null)
                        fullViewImage.sprite = cachedHandle.Result;
                    yield break;
                }
            }

            var handle = Addressables.LoadAssetAsync<Sprite>(cgPath);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (fullViewImage != null)
                    fullViewImage.sprite = handle.Result;

                if (!loadedSprites.ContainsKey(cgPath))
                    loadedSprites[cgPath] = handle;
            }
            else
            {
                LogError(Category.Addressables, $"Failed to load full CG: {cgPath}");
            }
        }

        private void CloseFullView()
        {
            if (fullViewPanel == null) return;

            // ✅ TELL HOMEPANELMANAGER TO CLOSE THE OVERLAY
            if (HomePanelManager.Instance != null)
            {
                HomePanelManager.Instance.CloseOverlay(fullViewPanel);
            }
            else
            {
                // Fallback if no HomePanelManager
                fullViewPanel.SetActive(false);
            }

            Log(Category.ChatManager, "[CGGallery] Closed full view");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ STATISTICS
        // ═══════════════════════════════════════════════════════════

        private void UpdateStatsDisplay(int unlocked, int total)
        {
            if (CGstatProgress != null)
            {
                float percentage = total > 0 ? (unlocked / (float)total) * 100f : 0f;
                CGstatProgress.text = $"{unlocked}/{total} ({percentage:F1}%)";
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════

        public int GetTotalUnlockedCount()
        {
            int total = 0;
            var dialogueSaveManager = DialogueSaveManager.Instance;

            if (dialogueSaveManager == null)
                return 0;

            var sections = BuildCharacterSections(dialogueSaveManager);
            foreach (var section in sections)
            {
                total += section.unlockedCGs.Count;
            }

            return total;
        }

        public int GetTotalCGCount()
        {
            int total = 0;
            foreach (var npcData in npcChatDataList)
            {
                if (npcData?.allCGAddressableKeys != null)
                {
                    total += npcData.allCGAddressableKeys.Count;
                }
            }
            return total;
        }

        public float GetUnlockPercentage()
        {
            int total = GetTotalCGCount();
            if (total == 0) return 0f;

            int unlocked = GetTotalUnlockedCount();
            return (unlocked / (float)total) * 100f;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ CLEANUP
        // ═══════════════════════════════════════════════════════════

        private void ClearGallery()
        {
            foreach (var obj in galleryObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            galleryObjects.Clear();
        }

        private void OnDestroy()
        {
            // Stop all loading coroutines to prevent null reference errors
            StopAllCoroutines();

            Log(Category.ChatManager, $"[CGGallery] Releasing {loadedSprites.Count} loaded sprites");

            foreach (var kvp in loadedSprites)
            {
                if (kvp.Value.IsValid())
                    Addressables.Release(kvp.Value);
            }
            loadedSprites.Clear();
        }

        // ═══════════════════════════════════════════════════════════
        // ░ DEBUG METHODS
        // ═══════════════════════════════════════════════════════════

#if UNITY_EDITOR
        [ContextMenu("Debug/Refresh Gallery")]
        private void DebugRefreshGallery()
        {
            RefreshGallery();
            Debug.Log("✅ Gallery refreshed!");
        }
        
        [ContextMenu("Debug/Unlock All CGs")]
        private void DebugUnlockAllCGs()
        {
            var dialogueSaveManager = DialogueSaveManager.Instance;
            if (dialogueSaveManager == null) return;
            
            foreach (var npcData in npcChatDataList)
            {
                if (npcData == null) continue;
                
                var chatState = dialogueSaveManager.GetChatState(npcData.ChatID);
                if (chatState == null) continue;
                
                foreach (var cgPath in npcData.allCGAddressableKeys)
                {
                    if (!chatState.unlockedCGs.Contains(cgPath))
                        chatState.unlockedCGs.Add(cgPath);
                }
                
                dialogueSaveManager.SaveChatState(npcData.ChatID, chatState);
            }
            
            dialogueSaveManager.ForceSave();
            RefreshGallery();
            Debug.Log("✅ All CGs unlocked!");
        }
        
        [ContextMenu("Debug/Clear All CGs")]
        private void DebugClearAllCGs()
        {
            var dialogueSaveManager = DialogueSaveManager.Instance;
            if (dialogueSaveManager == null) return;
            
            foreach (var npcData in npcChatDataList)
            {
                if (npcData == null) continue;
                
                var chatState = dialogueSaveManager.GetChatState(npcData.ChatID);
                if (chatState?.unlockedCGs != null)
                {
                    chatState.unlockedCGs.Clear();
                    dialogueSaveManager.SaveChatState(npcData.ChatID, chatState);
                }
            }
            
            dialogueSaveManager.ForceSave();
            RefreshGallery();
            Debug.Log("🔒 All CGs cleared!");
        }
        
        [ContextMenu("Debug/Print Gallery Stats")]
        private void DebugPrintStats()
        {
            var dialogueSaveManager = DialogueSaveManager.Instance;
            if (dialogueSaveManager == null) return;
            
            Debug.Log("╔═══════════════ GALLERY STATS ═══════════════╗");
            
            var sections = BuildCharacterSections(dialogueSaveManager);
            foreach (var section in sections)
            {
                int unlocked = section.unlockedCGs.Count;
                int total = section.npcData?.allCGAddressableKeys?.Count ?? 0;
                float percentage = total > 0 ? (unlocked / (float)total) * 100f : 0f;
                
                Debug.Log($"║ {section.displayName} ({section.npcData?.ChatID})");
                Debug.Log($"║   {unlocked}/{total} ({percentage:F1}%)");
                
                if (unlocked > 0)
                {
                    Debug.Log($"║   Unlocked: {string.Join(", ", section.unlockedCGs)}");
                }
                
                Debug.Log("╠═════════════════════════════════════════════╣");
            }
            
            int totalUnlocked = GetTotalUnlockedCount();
            int totalCGs = GetTotalCGCount();
            float totalPercentage = GetUnlockPercentage();
            
            Debug.Log($"║ TOTAL: {totalUnlocked}/{totalCGs} ({totalPercentage:F1}%)");
            Debug.Log("╚═════════════════════════════════════════════╝");
        }
        
        [ContextMenu("Debug/Validate NPC Data References")]
        private void DebugValidateReferences()
        {
            Debug.Log("╔═══════ VALIDATING NPC DATA ═══════╗");
            
            if (npcChatDataList == null || npcChatDataList.Count == 0)
            {
                Debug.LogError("║ ❌ No NPCChatData assigned!");
                Debug.LogError("║ → Drag your .asset files into the inspector");
                Debug.Log("╚════════════════════════════════════╝");
                return;
            }
            
            bool allValid = true;
            for (int i = 0; i < npcChatDataList.Count; i++)
            {
                var npcData = npcChatDataList[i];
                
                if (npcData == null)
                {
                    Debug.LogError($"║ [{i}] ❌ NULL reference!");
                    allValid = false;
                    continue;
                }
                
                Debug.Log($"║ [{i}] ✅ {npcData.characterName}");
                Debug.Log($"║     Chat ID: {npcData.ChatID}");
                Debug.Log($"║     CGs Defined: {npcData.allCGAddressableKeys?.Count ?? 0}");
                
                if (npcData.allCGAddressableKeys == null || npcData.allCGAddressableKeys.Count == 0)
                {
                    Debug.LogWarning($"║     ⚠️ No CGs defined! Right-click the .asset → CG Tools → Add New CG Slot");
                }
            }
            
            Debug.Log("╚════════════════════════════════════╝");
            
            if (allValid)
            {
                Debug.Log($"✅ All {npcChatDataList.Count} NPC references are valid!");
            }
        }
#endif

        // ═══════════════════════════════════════════════════════════
        // ░ HELPER CLASS
        // ═══════════════════════════════════════════════════════════

        private class CharacterSection
        {
            public NPCChatData npcData;
            public string displayName;
            public HashSet<string> unlockedCGs;
        }
    }
}
