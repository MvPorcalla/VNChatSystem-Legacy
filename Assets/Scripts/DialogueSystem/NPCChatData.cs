//=====================================
// NPCChatData.cs - With CG Gallery Support
//=====================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ChatDialogueSystem
{
    [CreateAssetMenu(fileName = "NPCChatData", menuName = "Chat System/NPC Chat Data")]
    public class NPCChatData : ScriptableObject
    {
        [Header("Character Info")]
        public string characterName;
        public AssetReference profileImage;

        [Header("Unique Identifier")]
        [Tooltip("Auto-generated unique ID. DO NOT MODIFY.")]
        [SerializeField] private string chatID;

        [Header("Dialogue Chapters")]
        [Tooltip("List of .mugi files in chapter order")]
        public List<TextAsset> mugiChapters = new List<TextAsset>();

        [Header("CG Gallery")]
        [Tooltip("All CGs for this character (in display order). Enter Addressable keys manually.")]
        public List<string> allCGAddressableKeys = new List<string>();

        private void GenerateNewChatID()
        {
            string guid = System.Guid.NewGuid().ToString("N"); // No hyphens (32 chars)
            string storyUIID = guid.Substring(0, System.Math.Min(6, guid.Length));
            chatID = $"{storyUIID}_{characterName}";
        }

        public string ChatID
        {
            get
            {
#if UNITY_EDITOR
                if (string.IsNullOrEmpty(chatID))
                {
                    GenerateNewChatID();
                    UnityEditor.EditorUtility.SetDirty(this);
                }
#else
                // Runtime safety check
                if (string.IsNullOrEmpty(chatID))
                {
                    Debug.LogError($"ChatID is empty for {characterName}! This ScriptableObject may be corrupted.");
                    return $"INVALID_{characterName}"; // Fallback to prevent crashes
                }
#endif
                return chatID;
            }
        }

        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(chatID))
            {
                GenerateNewChatID();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}