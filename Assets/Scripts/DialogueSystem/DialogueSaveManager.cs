//=====================================
// DialogueSaveManager.cs - COMPLETE FIXED VERSION
//=====================================

using System;
using System.Collections.Generic;
using UnityEngine;
using static ChatDialogueSystem.DebugHelper;

namespace ChatDialogueSystem
{
    public class DialogueSaveManager : BaseSaveManager<ChatSaveData>
    {
        private static DialogueSaveManager _instance;
        
        public static DialogueSaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DialogueSaveManager>();

                    if (_instance == null)
                    {
                        Debug.LogError("[DialogueSaveManager] Instance not found in scene! " +
                                     "Make sure DialogueSaveManager exists in 01_Bootstrap scene.");
                    }
                }
                return _instance;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ BASE CLASS IMPLEMENTATIONS
        // ═══════════════════════════════════════════════════════════

        protected override string GetDefaultSaveFileName() => "chat_data.json";
        protected override string GetDataFolderName() => "ChatData";
        protected override int GetDataVersion(ChatSaveData data) => data?.version ?? 1;
        protected override int GetCurrentVersion() => ChatSaveData.CURRENT_VERSION;
        protected override Category GetLogCategory() => Category.SaveManager;

        protected override void SetDataVersion(ChatSaveData data, int version)
        {
            if (data != null) data.version = version;
        }

        protected override bool MigrateData(ChatSaveData data, int fromVersion, int toVersion)
        {
            return false;
        }

        protected override ChatSaveData CreateDefaultData()
        {
            Log(Category.SaveManager, "📝 CreateDefaultData() called - creating fresh ChatSaveData");
            return new ChatSaveData
            {
                version = ChatSaveData.CURRENT_VERSION,
                chatStateList = new List<ChatStateEntry>()
            };
        }

        protected override void OnDataLoaded()
        {
            Log(Category.SaveManager, $"🔵 OnDataLoaded() CALLED | data is {(data == null ? "NULL" : "NOT NULL")}");
            
            if (data == null)
            {
                LogError(Category.SaveManager, "CRITICAL: Data is null after load! Creating emergency default.");
                data = CreateDefaultData();
                return;
            }
            
            if (data.chatStateList == null)
            {
                LogWarning(Category.SaveManager, "Loaded data had null chatStateList - initializing empty list (data may be corrupted)");
                data.chatStateList = new List<ChatStateEntry>();
            }
            
            Log(Category.SaveManager, $"✅ OnDataLoaded: {data.chatStateList.Count} chat states loaded from disk");
            
            if (data.chatStateList.Count > 0)
            {
                Log(Category.SaveManager, $"   📂 Loaded chats: {string.Join(", ", data.chatStateList.ConvertAll(e => e.chatID))}");
                
                var firstEntry = data.chatStateList[0];
                Log(Category.SaveManager, 
                    $"   📋 First chat details: ID='{firstEntry.chatID}' | " +
                    $"Messages={firstEntry.state?.chatHistory?.Count ?? 0} | " +
                    $"Character='{firstEntry.state?.characterName}'");
            }
            else
            {
                Log(Category.SaveManager, "   ⚠️ No chat states found in loaded data (new save or empty file)");
            }
        }

        protected override void OnBeforeSave()
        {
            if (data != null)
            {
                data.version = ChatSaveData.CURRENT_VERSION;
                Log(Category.SaveManager, $"💾 OnBeforeSave: Preparing to save {data.chatStateList?.Count ?? 0} chat states");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        protected override void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                
                Log(Category.SaveManager, "DialogueSaveManager Awake - Starting initialization");
                
                try
                {
                    base.Awake(); // This calls LoadData() which sets 'data'
                    Log(Category.SaveManager, $"base.Awake() completed | data is {(data == null ? "NULL" : "NOT NULL")}");
                }
                catch (Exception e)
                {
                    LogError(Category.SaveManager, $"base.Awake() failed: {e.Message}\n{e.StackTrace}");
                }
                
                // Validate data after load
                if (data == null)
                {
                    LogError(Category.SaveManager, "⚠️ Data is null after base.Awake() - creating emergency default");
                    data = CreateDefaultData();
                }
                else if (data.chatStateList == null)
                {
                    LogWarning(Category.SaveManager, "⚠️ chatStateList was null - initializing empty list");
                    data.chatStateList = new List<ChatStateEntry>();
                }
                
                Log(Category.SaveManager, $"DialogueSaveManager initialized | States: {GetChatStateCount()}");
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void EnsureDataInitialized()
        {
            if (data == null)
            {
                LogWarning(Category.SaveManager, "⚠️ EnsureDataInitialized: data was null");
                data = CreateDefaultData();
            }
            
            if (data.chatStateList == null)
            {
                LogWarning(Category.SaveManager, "⚠️ EnsureDataInitialized: chatStateList was null - creating empty list");
                data.chatStateList = new List<ChatStateEntry>();
            }
            
            Log(Category.SaveManager, $"   Data validation complete: {data.chatStateList.Count} chat states present");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ LIFECYCLE - COMPLETE REDUNDANCY FOR ALL PLATFORMS
        // ═══════════════════════════════════════════════════════════

        protected override void OnApplicationQuit()
        {
            Log(Category.SaveManager, "Application quitting - final dialogue save");
            SaveData(true);
            base.OnApplicationQuit();
        }

        // Unity's last chance before destruction
        private void OnDestroy()
        {
            if (_instance == this && data != null && data.chatStateList != null)
            {
                Log(Category.SaveManager, "DialogueSaveManager destroyed - emergency save");
                SaveData(true);
            }
        }

        // Scene changes
        private void OnDisable()
        {
            if (data != null && data.chatStateList != null && data.chatStateList.Count > 0)
            {
                Log(Category.SaveManager, "DialogueSaveManager disabled - saving dialogue");
                SaveData(true);
            }
        }

        // ✅ MOBILE: Save when app goes to background
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _instance == this && data != null)
            {
                Log(Category.SaveManager, "App paused - saving dialogue (mobile safety)");
                SaveData(true);
            }
        }

        // ✅ MOBILE: Save when app loses focus
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _instance == this && data != null)
            {
                Log(Category.SaveManager, "App lost focus - saving dialogue (mobile safety)");
                SaveData(true);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════

        public ChatState GetChatState(string chatID)
        {
            if (string.IsNullOrEmpty(chatID))
            {
                LogError(Category.SaveManager, "Cannot get chat state: chatID is null or empty");
                return null;
            }

            if (data == null || data.chatStateList == null)
            {
                LogError(Category.SaveManager, "⚠️ GetChatState: Data is null! Attempting emergency initialization...");
                EnsureDataInitialized();
                
                if (data == null)
                {
                    LogError(Category.SaveManager, "CRITICAL: Failed to initialize data!");
                    return null;
                }
            }

            Log(Category.SaveManager, $"🔍 GetChatState: Looking for '{chatID}' in {data.chatStateList.Count} saved states");
            
            if (data.chatStateList.Count > 0)
            {
                Log(Category.SaveManager, $"   Available chat IDs: {string.Join(", ", data.chatStateList.ConvertAll(e => $"'{e.chatID}'"))}");
            }

            var entry = data.chatStateList.Find(e => e.chatID == chatID);
            
            if (entry != null)
            {
                Log(Category.SaveManager, $"✅ FOUND existing chat state: {chatID} (Messages: {entry.state.chatHistory?.Count ?? 0})");
                return entry.state;
            }

            Log(Category.SaveManager, $"❌ NOT FOUND - Creating NEW chat state: {chatID}");
            
            var newState = new ChatState(chatID)
            {
                chatHistory = new List<MessageData>(),
                readMessageIds = new List<string>()
            };

            data.chatStateList.Add(new ChatStateEntry(chatID, newState));
            SaveData(true); // ✅ Force save when creating new chat
            Log(Category.SaveManager, $"💾 Saved new chat state to disk");
            
            return newState;
        }

        public void SaveChatState(string chatID, ChatState state)
        {
            if (string.IsNullOrEmpty(chatID) || state == null)
            {
                LogError(Category.SaveManager, $"Cannot save: Invalid chatID or state is null");
                return;
            }

            if (data == null || data.chatStateList == null)
            {
                LogError(Category.SaveManager, "Data is null! Cannot save chat state");
                EnsureDataInitialized();
                if (data == null) return;
            }

            var entry = data.chatStateList.Find(e => e.chatID == chatID);
            if (entry != null)
            {
                entry.state = state;
                Log(Category.SaveManager, $"Updated existing chat state: {chatID} (Messages: {state.chatHistory?.Count ?? 0})");
            }
            else
            {
                data.chatStateList.Add(new ChatStateEntry(chatID, state));
                Log(Category.SaveManager, $"Added new chat state: {chatID}");
            }

            // ⚠️ Regular saves USE throttle to prevent spam during rapid dialogue updates
            SaveData();
        }

        public void ClearChatState(string chatID)
        {
            if (data == null || data.chatStateList == null)
            {
                LogError(Category.SaveManager, "Data is null! Cannot clear chat state");
                return;
            }

            int removed = data.chatStateList.RemoveAll(e => e.chatID == chatID);
            
            if (removed > 0)
            {
                SaveData(true); // ✅ Force save when clearing
                Log(Category.SaveManager, $"Cleared chat state: {chatID}");
            }
        }

        public void ClearAllChatStates()
        {
            if (data == null || data.chatStateList == null)
            {
                LogError(Category.SaveManager, "Data is null! Cannot clear all chat states");
                return;
            }

            int count = data.chatStateList.Count;
            data.chatStateList.Clear();
            SaveData(true); // ✅ Force save when clearing
            
            Log(Category.SaveManager, $"Cleared all chat states ({count} removed)");
        }

        public bool HasChatState(string chatID)
        {
            if (string.IsNullOrEmpty(chatID)) return false;
            if (data?.chatStateList == null) return false;
            return data.chatStateList.Exists(e => e.chatID == chatID);
        }

        public int GetChatStateCount()
        {
            return data?.chatStateList?.Count ?? 0;
        }

        /// <summary>
        /// Forces an immediate save, bypassing throttle.
        /// Use for critical save points (scene changes, quits, etc.)
        /// </summary>
        public void ForceSave()
        {
            SaveData(true);
            Log(Category.SaveManager, "Dialogue data force saved");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ DEBUG METHODS
        // ═══════════════════════════════════════════════════════════

#if UNITY_EDITOR
        [ContextMenu("Debug/Print All Chat States")]
        private void DebugPrintAllStates()
        {
            Debug.Log($"╔═══════ CHAT STATES ({GetChatStateCount()}) ═══════╗");
            
            if (data?.chatStateList != null && data.chatStateList.Count > 0)
            {
                foreach (var entry in data.chatStateList)
                {
                    var state = entry.state;
                    Debug.Log(
                        $"║ ChatID: '{entry.chatID}'\n" +
                        $"║   Character: {state.characterName}\n" +
                        $"║   Chapter {state.currentChapterIndex} | Node: {state.currentNodeName}\n" +
                        $"║   Messages: {state.chatHistory?.Count ?? 0} | Read: {state.readMessageIds?.Count ?? 0}\n" +
                        $"╠═════════════════════════════════════════╣"
                    );
                }
            }
            else
            {
                Debug.Log("║ No chat states saved");
            }
            
            Debug.Log("╚═════════════════════════════════════════╝");
        }

        [ContextMenu("Debug/Read Raw Save File")]
        private void DebugReadRawFile()
        {
            if (string.IsNullOrEmpty(savePath))
            {
                EnsurePathsInitialized();
            }
            
            if (!System.IO.File.Exists(savePath))
            {
                Debug.LogWarning($"Save file doesn't exist yet at: {savePath}");
                return;
            }

            string json = System.IO.File.ReadAllText(savePath);
            Debug.Log($"📄 RAW SAVE FILE CONTENTS ({json.Length} chars):\n{json}");
            
            try
            {
                var testData = JsonUtility.FromJson<ChatSaveData>(json);
                Debug.Log($"✅ Deserialization test: " +
                         $"Version={testData?.version} | " +
                         $"ListCount={testData?.chatStateList?.Count ?? -1}");
                
                if (testData?.chatStateList != null && testData.chatStateList.Count > 0)
                {
                    var first = testData.chatStateList[0];
                    Debug.Log($"   First entry: ID='{first.chatID}' | State null? {first.state == null}");
                    
                    if (first.state != null)
                    {
                        Debug.Log($"   State details: Messages={first.state.chatHistory?.Count ?? -1}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Deserialization test failed: {e.Message}");
            }
        }

        [ContextMenu("Debug/Force Save Now")]
        private void DebugForceSave()
        {
            SaveData(true);
            Debug.Log($"✅ Force saved {GetChatStateCount()} states to: {savePath}");
        }

        [ContextMenu("Debug/Reload From Disk")]
        private void DebugReload()
        {
            LoadData();
            Debug.Log($"✅ Reloaded! States: {GetChatStateCount()}");
        }

        [ContextMenu("Debug/Open Save Folder")]
        private void DebugOpenSaveFolder()
        {
            string folderPath = System.IO.Path.GetDirectoryName(savePath);
            
            if (string.IsNullOrEmpty(folderPath))
            {
                EnsurePathsInitialized();
                folderPath = System.IO.Path.GetDirectoryName(savePath);
            }
            
            if (System.IO.Directory.Exists(folderPath))
            {
                Application.OpenURL(folderPath);
                Debug.Log($"✅ Opened: {folderPath}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Folder doesn't exist yet: {folderPath}\nIt will be created when you first save.");
            }
        }
        
        [ContextMenu("Debug/Open Backup Folder")]
        private void DebugOpenBackup()
        {
            if (string.IsNullOrEmpty(backupFolder))
            {
                EnsurePathsInitialized();
            }
            
            if (System.IO.Directory.Exists(backupFolder))
            {
                Application.OpenURL(backupFolder);
                Debug.Log($"✅ Opened: {backupFolder}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Backup folder doesn't exist yet: {backupFolder}\nIt will be created on first backup.");
            }
        }

        [ContextMenu("Debug/Print Unlocked CGs")]
        private void DebugPrintUnlockedCGs()
        {
            Debug.Log($"╔═══════ UNLOCKED CGs ═══════╗");
            
            if (data?.chatStateList != null && data.chatStateList.Count > 0)
            {
                int totalCGs = 0;
                foreach (var entry in data.chatStateList)
                {
                    var state = entry.state;
                    if (state.unlockedCGs != null && state.unlockedCGs.Count > 0)
                    {
                        Debug.Log($"║ Chat: {entry.chatID} ({state.characterName})");
                        Debug.Log($"║   Unlocked: {state.unlockedCGs.Count} CGs");
                        foreach (var cg in state.unlockedCGs)
                        {
                            Debug.Log($"║     🎨 {cg}");
                        }
                        totalCGs += state.unlockedCGs.Count;
                        Debug.Log("╠═════════════════════════════════════════╣");
                    }
                }
                Debug.Log($"║ TOTAL: {totalCGs} CGs unlocked across all chats");
            }
            else
            {
                Debug.Log("║ No CGs unlocked yet");
            }
            
            Debug.Log("╚═════════════════════════════════════════╝");
        }

        [ContextMenu("Debug/Clear All Unlocked CGs")]
        private void DebugClearAllCGs()
        {
            if (data?.chatStateList != null)
            {
                int clearedCount = 0;
                foreach (var entry in data.chatStateList)
                {
                    if (entry.state?.unlockedCGs != null)
                    {
                        clearedCount += entry.state.unlockedCGs.Count;
                        entry.state.unlockedCGs.Clear();
                    }
                }
                
                SaveData(true);
                Debug.Log($"✅ Cleared {clearedCount} unlocked CGs and saved");
            }
        }
        
        [ContextMenu("Debug/Print All Paths")]
        private void DebugPrintPaths()
        {
            if (string.IsNullOrEmpty(savePath))
            {
                EnsurePathsInitialized();
            }
            
            Debug.Log($"╔═══════════════ SAVE PATHS ═══════════════╗\n" +
                     $"║ Save File Name: {saveFileName}\n" +
                     $"║ Data Folder: {GetDataFolderName()}\n" +
                     $"║\n" +
                     $"║ Full Save Path:\n" +
                     $"║ {savePath}\n" +
                     $"║\n" +
                     $"║ Backup Folder:\n" +
                     $"║ {backupFolder}\n" +
                     $"║\n" +
                     $"║ Save Exists: {System.IO.File.Exists(savePath)}\n" +
                     $"║ Backup Folder Exists: {System.IO.Directory.Exists(backupFolder)}\n" +
                     $"╚═══════════════════════════════════════════╝");
        }
#endif
    }
}