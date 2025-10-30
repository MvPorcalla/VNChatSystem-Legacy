//=====================================
// ContactButton.cs
//=====================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;
using static ChatDialogueSystem.DebugHelper;

namespace ChatDialogueSystem
{
    public class ContactButton : MonoBehaviour
    {
        [Header("Contact Profile UI References")]
        public Image profileImage;
        public TextMeshProUGUI ProfileName;
        public GameObject badge;
        
        [Header("Optional: Loading Indicator")]
        public GameObject loadingSpinner; // Optional
        
        private NPCChatData chatData;
        private System.Action<NPCChatData> onContactSelected;
        
        // Store handle for cleanup
        private AsyncOperationHandle<Sprite> profileImageHandle;
        
        // Track coroutine for cleanup
        private Coroutine loadCoroutine;

        public void Initialize(NPCChatData data, System.Action<NPCChatData> callback)
        {
            chatData = data;
            onContactSelected = callback;
            
            if (ProfileName != null)
            {
                ProfileName.text = data.characterName;
            }
            
            // Clean up any previous image load
            CleanupProfileImage();
            
            if (data.profileImage != null && profileImage != null)
            {
                loadCoroutine = StartCoroutine(LoadProfileImage());
            }
            else if (profileImage != null)
            {
                // No image assigned - hide or show placeholder
                profileImage.enabled = false;
            }
        }

        private IEnumerator LoadProfileImage()
        {            
            // Optional: Show loading indicator
            if (loadingSpinner != null)
            {
                loadingSpinner.SetActive(true);
            }
            
            // Hide image while loading
            if (profileImage != null)
            {
                profileImage.enabled = false;
            }

            // Check if already cached by Addressables
            if (chatData.profileImage.IsValid() && chatData.profileImage.OperationHandle.IsDone)
            {
                var cachedHandle = chatData.profileImage.OperationHandle;
                if (cachedHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Log(Category.UI, $"[ContactButton] Reusing cached profile for {chatData.characterName}");

                    // Convert the handle properly
                    profileImageHandle = chatData.profileImage.OperationHandle.Convert<Sprite>();
                    var sprite = profileImageHandle.Result;

                    if (sprite != null && profileImage != null)
                    {
                        profileImage.sprite = sprite;
                        profileImage.enabled = true;
                    }

                    if (loadingSpinner != null)
                    {
                        loadingSpinner.SetActive(false);
                    }

                    yield break;
                }
            }
            
            // Load new image
            profileImageHandle = chatData.profileImage.LoadAssetAsync<Sprite>();
            yield return profileImageHandle;
            
            // Optional: Hide loading indicator
            if (loadingSpinner != null)
            {
                loadingSpinner.SetActive(false);
            }
            
            if (profileImageHandle.Status == AsyncOperationStatus.Succeeded)
            {
                if (profileImage != null && profileImageHandle.Result != null)
                {
                    profileImage.sprite = profileImageHandle.Result;
                    profileImage.enabled = true;

                    Log(Category.UI, $"[ContactButton] Loaded profile for {chatData.characterName}");
                }
            }
            else
            {
                LogError(Category.UI, $"[ContactButton] Failed to load profile for {chatData.characterName}. Status: {profileImageHandle.Status}");
                
                // Show placeholder or hide image
                if (profileImage != null)
                {
                    profileImage.enabled = false;
                }
            }
        }

        public void OnContactClicked()
        {
            onContactSelected?.Invoke(chatData);
        }
        
        // Cleanup method
        private void CleanupProfileImage()
        {
            // Stop any ongoing load
            if (loadCoroutine != null)
            {
                StopCoroutine(loadCoroutine);
                loadCoroutine = null;
            }
        }
        
        private void OnDestroy()
        {
            CleanupProfileImage();
            
            // Clear references
            chatData = null;
            onContactSelected = null;
        }
    }
}