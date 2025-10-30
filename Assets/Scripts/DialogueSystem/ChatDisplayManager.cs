//=====================================
// ChatDisplayManager.cs
//=====================================

using System;
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
    public class ChatDisplayManager : MonoBehaviour
    {
        private const string PLAYER_SPEAKER = "player";
        private const string END_RESET_TEXT = "End of Chapter — Reset Story";
        private const string ERROR_RESET_TEXT = "Something went wrong — Reset Story";

        [Header("Message Prefabs")]
        public GameObject systemMessagePrefab;
        public GameObject npcTextBubblePrefab;
        public GameObject npcImageBubblePrefab;
        public GameObject playerTextBubblePrefab;
        public GameObject playerImageBubblePrefab;

        [Header("Button Prefabs")]
        public GameObject choiceButtonPrefab;
        public GameObject pauseButtonPrefab;
        public GameObject resetButtonPrefab;

        [Header("Container References")]
        public Transform chatParent;
        public Transform choicesContainer;

        [Header("Profile Display")]
        public TextMeshProUGUI profileNameText;
        public Image profileImage;

        [Header("Memory Management")]
        [Tooltip("Maximum number of cached images before cleanup (0 = unlimited)")]
        [SerializeField] private int maxCachedImages = 50;

        [Header("Image Fade Settings")]
        [Tooltip("Duration of image fade-in effect (in seconds)")]
        [Range(0f, 1f)]
        [SerializeField] private float imageFadeDuration = 0.3f;

        private PoolingManager poolingManager;
        private List<GameObject> activeMessages = new List<GameObject>();
        private List<GameObject> activeChoices = new List<GameObject>();

        // Profile state
        private Coroutine currentProfileLoadCoroutine;
        private AssetReference currentProfileAssetReference;

        // Track active loads by component (not coroutine)
        private HashSet<Image> activeImageLoads = new HashSet<Image>();

        // LRU cache with proper ordering
        private Dictionary<string, AsyncOperationHandle<Sprite>> loadedMessageImages =
            new Dictionary<string, AsyncOperationHandle<Sprite>>();
        private LinkedList<string> imageCacheOrder = new LinkedList<string>();

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        public void Initialize(PoolingManager poolManager)
        {
            poolingManager = poolManager;
            
            if (poolingManager == null)
            {
                LogError(Category.ChatManager, "PoolingManager is null!");
                return;
            }

            ValidateReferences();
            Log(Category.ChatManager, "ChatDisplayManager initialized");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PREFAB ACCESSORS
        // ═══════════════════════════════════════════════════════════

        public GameObject GetSystemMessagePrefab() => systemMessagePrefab;
        public GameObject GetNPCTextBubblePrefab() => npcTextBubblePrefab;
        public GameObject GetNPCImageBubblePrefab() => npcImageBubblePrefab;
        public GameObject GetPlayerTextBubblePrefab() => playerTextBubblePrefab;
        public GameObject GetPlayerImageBubblePrefab() => playerImageBubblePrefab;
        public GameObject GetChoiceButtonPrefab() => choiceButtonPrefab;
        public GameObject GetPauseButtonPrefab() => pauseButtonPrefab;
        public GameObject GetResetButtonPrefab() => resetButtonPrefab;

        private void ValidateReferences()
        {
            if (chatParent == null)
                LogError(Category.ChatManager, "chatParent is not assigned!");
            if (choicesContainer == null)
                LogError(Category.ChatManager, "choicesContainer is not assigned!");
            if (choiceButtonPrefab == null)
                LogError(Category.ChatManager, "choiceButtonPrefab is not assigned!");

            if (choicesContainer != null)
            {
                choicesContainer.gameObject.SetActive(true);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PROFILE DISPLAY
        // ═══════════════════════════════════════════════════════════

        public void SetProfileName(string characterName)
        {
            if (profileNameText != null)
            {
                profileNameText.text = characterName;
            }
        }

        public void LoadProfileImage(AssetReference imageReference)
        {
            if (currentProfileLoadCoroutine != null)
            {
                StopCoroutine(currentProfileLoadCoroutine);
                currentProfileLoadCoroutine = null;
            }

            if (profileImage != null && imageReference != null)
            {
                currentProfileLoadCoroutine = StartCoroutine(LoadProfileImageCoroutine(imageReference));
            }
        }

        private IEnumerator LoadProfileImageCoroutine(AssetReference imageReference)
        {
            // Check if same reference AND already loaded
            if (currentProfileAssetReference == imageReference)
            {
                if (imageReference.IsValid() && imageReference.OperationHandle.IsDone)
                {
                    var existingHandle = imageReference.OperationHandle;
                    if (existingHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        Log(Category.Addressables, "Reusing profile image");
                        var sprite = existingHandle.Result as Sprite;

                        if (sprite != null && profileImage != null)
                        {
                            profileImage.sprite = sprite;
                            if (!profileImage.enabled)
                            {
                                yield return StartCoroutine(FadeInImage(profileImage));
                            }
                        }
                        yield break;
                    }
                    else
                    {
                        imageReference.ReleaseAsset();
                        currentProfileAssetReference = null;
                    }
                }
            }

            if (profileImage != null)
            {
                profileImage.enabled = false;
            }

            // Release old handle before loading new
            if (currentProfileAssetReference != null && currentProfileAssetReference != imageReference)
            {
                try
                {
                    currentProfileAssetReference.ReleaseAsset();
                }
                catch (System.Exception ex)
                {
                    LogError(Category.Addressables, $"Error releasing profile: {ex.Message}");
                }
            }

            currentProfileAssetReference = imageReference;

            // Check Addressables cache
            if (imageReference.IsValid() && imageReference.OperationHandle.IsDone)
            {
                var cachedHandle = imageReference.OperationHandle;
                if (cachedHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    var sprite = cachedHandle.Result as Sprite;
                    if (sprite != null && profileImage != null)
                    {
                        profileImage.sprite = sprite;
                        yield return StartCoroutine(FadeInImage(profileImage));
                    }
                    yield break;
                }
            }

            // Load new
            var handle = imageReference.LoadAssetAsync<Sprite>();
            yield return handle;

            if (currentProfileAssetReference != imageReference)
            {
                yield break;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (profileImage != null && handle.Result != null)
                {
                    profileImage.sprite = handle.Result;
                    yield return StartCoroutine(FadeInImage(profileImage));
                }
            }
            else
            {
                LogError(Category.Addressables, $"Failed to load profile. Status: {handle.Status}");
                if (currentProfileAssetReference == imageReference)
                {
                    currentProfileAssetReference = null;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ IMAGE HANDLE MANAGEMENT (LRU Cache)
        // ═══════════════════════════════════════════════════════════

        public void StoreImageHandle(string imagePath, AsyncOperationHandle<Sprite> handle)
        {
            if (string.IsNullOrEmpty(imagePath) || !handle.IsValid())
            {
                return;
            }

            // If exists, release old and update order
            if (loadedMessageImages.ContainsKey(imagePath))
            {
                var oldHandle = loadedMessageImages[imagePath];
                if (oldHandle.IsValid() && !AreHandlesEqual(oldHandle, handle))
                {
                    Addressables.Release(oldHandle);
                }
                imageCacheOrder.Remove(imagePath);
            }

            loadedMessageImages[imagePath] = handle;
            imageCacheOrder.AddLast(imagePath); // Most recent

            EnforceCacheLimit();
        }

        private void EnforceCacheLimit()
        {
            if (maxCachedImages <= 0) return;

            while (loadedMessageImages.Count > maxCachedImages && imageCacheOrder.Count > 0)
            {
                string oldestKey = imageCacheOrder.First.Value;
                imageCacheOrder.RemoveFirst();

                // Should always exist - assert if missing
                if (!loadedMessageImages.TryGetValue(oldestKey, out var oldHandle))
                {
                    LogError(Category.Addressables, $"Cache desync: Key '{oldestKey}' in order but not in dictionary!");
                    continue;
                }

                if (oldHandle.IsValid())
                {
                    Addressables.Release(oldHandle);
                }
                loadedMessageImages.Remove(oldestKey);
            }
        }

        private bool AreHandlesEqual(AsyncOperationHandle<Sprite> h1, AsyncOperationHandle<Sprite> h2)
        {
            if (h1.Status == AsyncOperationStatus.Succeeded &&
                h2.Status == AsyncOperationStatus.Succeeded)
            {
                return h1.Result == h2.Result;
            }
            return false;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ MESSAGE DISPLAY
        // ═══════════════════════════════════════════════════════════

        public void DisplayMessage(MessageData message)
        {
            if (message == null) return;
    
            GameObject messageBubble = GetMessageBubble(message);

            if (messageBubble != null)
            {
                ConfigureMessageBubble(messageBubble, message);
                messageBubble.SetActive(true);
                activeMessages.Add(messageBubble);
            }
        }

        private GameObject GetMessageBubble(MessageData message)
        {
            GameObject prefab = null;

            switch (message.type)
            {
                case MessageData.MessageType.System:
                    prefab = systemMessagePrefab;
                    break;
                case MessageData.MessageType.Text:
                    prefab = IsPlayerMessage(message) ? playerTextBubblePrefab : npcTextBubblePrefab;
                    break;
                case MessageData.MessageType.Image:
                    prefab = IsPlayerMessage(message) ? playerImageBubblePrefab : npcImageBubblePrefab;
                    break;
            }

            return prefab != null ? poolingManager.Get(prefab, chatParent, false) : null;
        }

        private void ConfigureMessageBubble(GameObject bubble, MessageData message)
        {
            if (bubble == null) return;

            if (message.type == MessageData.MessageType.Text || message.type == MessageData.MessageType.System)
            {
                var textComponent = bubble.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    var autoResize = textComponent.GetComponent<AutoResizeText>();
                    if (autoResize != null)
                    {
                        if (!autoResize.IsInitialized)
                        {
                            autoResize.ForceReinitialize();
                        }

                        autoResize.SetText(message.content);
                    }
                    else
                    {
                        textComponent.text = message.content;
                    }
                }
            }

            if (message.type == MessageData.MessageType.Image)
            {
                var cgBubble = bubble.GetComponent<CGBubble>();
                if (cgBubble != null && cgBubble.cgImage != null)
                {
                    StartTrackedImageLoad(cgBubble.cgImage, message.imagePath);
                }
            }
        }

        // Track by component, not coroutine
        private void StartTrackedImageLoad(Image imageComponent, string imagePath)
        {
            if (imageComponent == null) return;
            
            if (activeImageLoads.Contains(imageComponent))
            {
                Log(Category.Addressables, $"Load already in progress: {imagePath}");
                return;
            }
            
            activeImageLoads.Add(imageComponent);
            StartCoroutine(LoadMessageImage(imageComponent, imagePath));
        }

        private IEnumerator LoadMessageImage(Image imageComponent, string imagePath)
        {
            // Check cache
            if (loadedMessageImages.ContainsKey(imagePath))
            {
                var cachedHandle = loadedMessageImages[imagePath];

                if (cachedHandle.IsValid() && cachedHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    if (imageComponent != null)
                    {
                        imageComponent.sprite = cachedHandle.Result as Sprite;

                        var rectTransform = imageComponent.transform.parent as RectTransform;
                        if (rectTransform != null)
                        {
                            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                        }

                        yield return StartCoroutine(FadeInImage(imageComponent));
                    }
                    
                    // Remove from active loads
                    if (imageComponent != null)
                        activeImageLoads.Remove(imageComponent);
                    
                    yield break;
                }
                else
                {
                    if (cachedHandle.IsValid())
                    {
                        Addressables.Release(cachedHandle);
                    }
                    loadedMessageImages.Remove(imagePath);
                    imageCacheOrder.Remove(imagePath);
                }
            }

            // Load new
            var handle = Addressables.LoadAssetAsync<Sprite>(imagePath);
            yield return handle;

            // Remove from tracking immediately
            if (imageComponent != null)
                activeImageLoads.Remove(imageComponent);

            // Check if destroyed during load
            if (imageComponent == null || imageComponent.gameObject == null)
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    loadedMessageImages[imagePath] = handle;
                    imageCacheOrder.AddLast(imagePath);
                }
                else if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                yield break;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                imageComponent.sprite = handle.Result;

                var rectTransform = imageComponent.transform.parent as RectTransform;
                if (rectTransform != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                }

                loadedMessageImages[imagePath] = handle;
                imageCacheOrder.AddLast(imagePath);

                if (imageComponent != null && imageComponent.gameObject != null)
                {
                    yield return StartCoroutine(FadeInImage(imageComponent));
                }
            }
            else
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }

        private IEnumerator FadeInImage(Image imageComponent)
        {
            if (imageComponent == null) yield break;

            var canvasGroup = imageComponent.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = imageComponent.gameObject.AddComponent<CanvasGroup>();
            }

            imageComponent.enabled = true;
            canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < imageFadeDuration)
            {
                if (imageComponent == null || canvasGroup == null) yield break;

                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / imageFadeDuration);
                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ UI MANAGEMENT
        // ═══════════════════════════════════════════════════════════

        public void ClearMessages()
        {
            // STEP 1: Recycle tracked messages
            poolingManager.RecycleAll(activeMessages);
            activeMessages.Clear();

            // STEP 2: Recycle any orphaned objects in ChatContent
            if (chatParent != null)
            {
                for (int i = chatParent.childCount - 1; i >= 0; i--)
                {
                    Transform child = chatParent.GetChild(i);
                    if (child != null && child.gameObject != null)
                    {
                        poolingManager.Recycle(child.gameObject);
                    }
                }
            }

            Log(Category.ChatManager, "All messages cleared from UI");
        }

        public void ClearAllDisplay()
        {
            ClearMessages();
            ClearChoices();
        }

        public void AggressiveClear()
        {
            // Stop ongoing image loads
            activeImageLoads.Clear();

            // Clear tracking lists
            activeMessages.Clear();
            activeChoices.Clear();

            if (poolingManager == null) return;

            // Recycle all children in chatParent
            if (chatParent != null)
            {
                for (int i = chatParent.childCount - 1; i >= 0; i--)
                {
                    Transform child = chatParent.GetChild(i);
                    if (child != null && child.gameObject != null)
                    {
                        poolingManager.Recycle(child.gameObject);
                    }
                }
            }

            // Recycle all children in choicesContainer
            if (choicesContainer != null)
            {
                for (int i = choicesContainer.childCount - 1; i >= 0; i--)
                {
                    Transform child = choicesContainer.GetChild(i);
                    if (child != null && child.gameObject != null)
                    {
                        poolingManager.Recycle(child.gameObject);
                    }
                }
            }

            Log(Category.ChatManager, "Aggressive clear completed");
        }

        public int GetActiveMessageCount() => activeMessages.Count;

        public void SyncActiveMessages()
        {
            activeMessages.Clear();
            if (chatParent == null) return;

            for (int i = 0; i < chatParent.childCount; i++)
            {
                GameObject child = chatParent.GetChild(i).gameObject;
                if (child.activeInHierarchy && IsValidMessageBubble(child))
                {
                    activeMessages.Add(child);
                }
            }
        }

        private bool IsValidMessageBubble(GameObject obj)
        {
            return obj != null && (obj.GetComponentInChildren<TextMeshProUGUI>() != null || 
                                   obj.GetComponent<CGBubble>() != null);
        }

        private bool IsPlayerMessage(MessageData message)
        {
            return string.Equals(message.speaker, PLAYER_SPEAKER, StringComparison.OrdinalIgnoreCase);
        }

        // ═══════════════════════════════════════════════════════════
        // ░ BUTTONS
        // ═══════════════════════════════════════════════════════════

        public void ShowChoiceButtons(List<ChoiceData> choices, Action<ChoiceData> onChoiceSelected)
        {
            foreach (var choice in choices)
            {
                GameObject choiceButton = poolingManager.Get(choiceButtonPrefab, choicesContainer, false);
                SetupButton(choiceButton, choice.choiceText, () => onChoiceSelected(choice));
                activeChoices.Add(choiceButton);
            }
            StartCoroutine(RebuildChoiceLayout());
        }

        public void ShowPauseButton(string buttonText, UnityEngine.Events.UnityAction callback)
        {
            GameObject prefabToUse = pauseButtonPrefab ?? choiceButtonPrefab;
            GameObject pauseButton = poolingManager.Get(prefabToUse, choicesContainer, false);
            SetupButton(pauseButton, buttonText, callback);
            activeChoices.Add(pauseButton);
            StartCoroutine(RebuildChoiceLayout());
        }

        public void ShowResetButton(bool isContentError, UnityEngine.Events.UnityAction callback)
        {
            ClearChoices();
            GameObject prefabToUse = resetButtonPrefab ?? choiceButtonPrefab;
            GameObject resetButton = poolingManager.Get(prefabToUse, choicesContainer, false);
            string text = isContentError ? ERROR_RESET_TEXT : END_RESET_TEXT;
            SetupButton(resetButton, text, callback);
            activeChoices.Add(resetButton);
            StartCoroutine(RebuildChoiceLayout());
        }

        public void ClearChoices()
        {
            poolingManager.RecycleAll(activeChoices);
            activeChoices.Clear();
        }

        private void SetupButton(GameObject buttonObject, string buttonText, UnityEngine.Events.UnityAction callback)
        {
            if (buttonObject == null) return;

            var textComponent = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = buttonText;
            }

            var button = buttonObject.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(callback);
                button.interactable = true;
            }

            buttonObject.SetActive(true);
        }

        private IEnumerator RebuildChoiceLayout()
        {
            yield return new WaitForEndOfFrame();

            if (choicesContainer != null)
            {
                var rectTransform = choicesContainer as RectTransform;
                if (rectTransform != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                }
            }

            if (chatParent != null)
            {
                var chatRect = chatParent as RectTransform;
                if (chatRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(chatRect);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ CLEANUP
        // ═══════════════════════════════════════════════════════════

        public void ClearLoadedImages()
        {
            if (loadedMessageImages.Count == 0) return;

            Log(Category.Addressables, $"Clearing {loadedMessageImages.Count} loaded images");

            var imagesToClear = new List<KeyValuePair<string, AsyncOperationHandle<Sprite>>>(loadedMessageImages);

            foreach (var kvp in imagesToClear)
            {
                if (kvp.Value.IsValid())
                {
                    try
                    {
                        Addressables.Release(kvp.Value);
                    }
                    catch (System.Exception ex)
                    {
                        LogError(Category.Addressables, $"Failed to release {kvp.Key}: {ex.Message}");
                    }
                }
            }

            loadedMessageImages.Clear();
            imageCacheOrder.Clear(); // Clear order tracking
            activeImageLoads.Clear(); // Clear active loads
        }

        private void OnDestroy()
        {
            // Clear active loads (no need to stop coroutines)
            activeImageLoads.Clear();

            // Stop profile loading
            if (currentProfileLoadCoroutine != null)
            {
                StopCoroutine(currentProfileLoadCoroutine);
                currentProfileLoadCoroutine = null;
            }

            // Release profile image
            if (currentProfileAssetReference != null)
            {
                try
                {
                    currentProfileAssetReference.ReleaseAsset();
                }
                catch (System.Exception ex)
                {
                    LogError(Category.Addressables, $"Error releasing profile on destroy: {ex.Message}");
                }
                currentProfileAssetReference = null;
            }

            // Release all message images
            if (loadedMessageImages.Count > 0)
            {
                Log(Category.Addressables, $"Releasing {loadedMessageImages.Count} message image handles");

                foreach (var kvp in loadedMessageImages)
                {
                    if (kvp.Value.IsValid())
                    {
                        try
                        {
                            Addressables.Release(kvp.Value);
                        }
                        catch (System.Exception ex)
                        {
                            LogError(Category.Addressables, $"Failed to release {kvp.Key}: {ex.Message}");
                        }
                    }
                }

                loadedMessageImages.Clear();
                imageCacheOrder.Clear();
            }
        }
    }
}