//=====================================
// PlayerSaveManager.cs
//=====================================

using System;
using UnityEngine;

namespace ChatDialogueSystem
{
    public class PlayerSaveManager : BaseSaveManager<PlayerData>
    {
        private static PlayerSaveManager _instance;
        
        public static PlayerSaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PlayerSaveManager>();

                    if (_instance == null)
                    {
                        Debug.LogError("[PlayerSaveManager] Instance not found in scene! " +
                                     "Make sure PlayerSaveManager exists in 01_Bootstrap scene.");
                    }
                }
                return _instance;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ SESSION TRACKING
        // ═══════════════════════════════════════════════════════════

        private float sessionStartTime;
        private float lastPauseTime = 0f;
        private const float PAUSE_SAVE_COOLDOWN = 1f;

        public event Action OnSaveCompleted;
        public event Action OnLoadCompleted;

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════

        public string PlayerName => data?.playerName ?? "User";
        public string FormattedPlayTime => FormatSeconds(data?.totalPlayTimeSeconds ?? 0);
        public int TotalPlayTimeSeconds => data?.totalPlayTimeSeconds ?? 0;
        public int PlayCount => data?.playCount ?? 0;
        public string LastPlayedDate => data?.lastSaveTime ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string GameVersion => data?.gameVersion ?? Application.version;
        public int SaveVersion => data?.saveVersion ?? PlayerData.CURRENT_VERSION;

        protected override string GetDefaultSaveFileName()
        {
            return "player_data.json";
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
                
                base.Awake();
                sessionStartTime = Time.realtimeSinceStartup;
                
                if (data == null)
                {
                    Debug.LogError("[PlayerSaveManager] Data is null after base.Awake() - creating emergency default");
                    data = CreateDefaultData();
                }
                
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ BASE CLASS OVERRIDES
        // ═══════════════════════════════════════════════════════════

        protected override PlayerData CreateDefaultData()
        {
            return new PlayerData
            {
                saveVersion = PlayerData.CURRENT_VERSION,
                playerName = "User",
                totalPlayTimeSeconds = 0,
                playCount = 0,
                gameVersion = Application.version,
                lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        protected override void OnDataLoaded()
        {
            if (data == null)
            {
                Debug.LogError("[PlayerSaveManager] CRITICAL: Data is null in OnDataLoaded!");
                return;
            }

            data.playCount++;
            data.lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            OnLoadCompleted?.Invoke();

            Debug.Log($"[PlayerSaveManager] Loaded player data: {data.playerName} (Session #{data.playCount})");
        }

        protected override void OnBeforeSave()
        {
            if (data == null) return;

            data.lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            data.gameVersion = Application.version;
            data.saveVersion = PlayerData.CURRENT_VERSION;
            
            OnSaveCompleted?.Invoke();
            Debug.Log($"[PlayerSaveManager] Saved: {data.playerName} | PlayTime: {FormattedPlayTime}");
        }

        protected override int GetDataVersion(PlayerData data) => data?.saveVersion ?? 1;

        protected override void SetDataVersion(PlayerData data, int version)
        {
            if (data != null)
                data.saveVersion = version;
        }

        protected override int GetCurrentVersion() => PlayerData.CURRENT_VERSION;

        protected override DebugHelper.Category GetLogCategory() => DebugHelper.Category.SaveManager;

        protected override string GetDataFolderName() => "PlayerData";

        protected override bool MigrateData(PlayerData data, int fromVersion, int toVersion)
        {
            return false;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════

        public void SetPlayerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Debug.LogWarning("[PlayerSaveManager] Cannot set empty player name");
                return;
            }

            if (data == null)
            {
                Debug.LogError("[PlayerSaveManager] Cannot set player name: data is null");
                return;
            }

            data.playerName = name.Trim();
            SaveData();
            Debug.Log($"[PlayerSaveManager] Player name updated: {data.playerName}");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PLAY TIME TRACKING
        // ═══════════════════════════════════════════════════════════

        private string FormatSeconds(int seconds)
        {
            if (seconds < 60) return "< 1m";

            int hours = seconds / 3600;
            int minutes = (seconds % 3600) / 60;

            return hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
        }

        private void UpdatePlayTime()
        {
            if (data == null) return;

            int sessionTime = Mathf.FloorToInt(Time.realtimeSinceStartup - sessionStartTime);
            data.totalPlayTimeSeconds += sessionTime;
            sessionStartTime = Time.realtimeSinceStartup;

            Debug.Log($"[PlayerSaveManager] Session time: +{sessionTime}s | Total: {FormattedPlayTime}");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ LIFECYCLE EVENTS
        // ═══════════════════════════════════════════════════════════

        protected override void OnApplicationQuit()
        {
            UpdatePlayTime();
            base.OnApplicationQuit();
            Debug.Log("[PlayerSaveManager] Application quit - final save completed");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                float now = Time.realtimeSinceStartup;
                if (now - lastPauseTime > PAUSE_SAVE_COOLDOWN)
                {
                    UpdatePlayTime();
                    SaveData();
                    lastPauseTime = now;
                }
            }
            else
            {
                sessionStartTime = Time.realtimeSinceStartup;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                float now = Time.realtimeSinceStartup;
                if (now - lastPauseTime > PAUSE_SAVE_COOLDOWN)
                {
                    UpdatePlayTime();
                    SaveData();
                    lastPauseTime = now;
                }
            }
            else
            {
                sessionStartTime = Time.realtimeSinceStartup;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ DEBUG TOOLS
        // ═══════════════════════════════════════════════════════════

#if UNITY_EDITOR

        [ContextMenu("Debug/Force Save")]
        private void DebugForceSave()
        {
            SaveData(true);
            Debug.Log($"✅ PlayerSaveManager: Force saved!");
        }

        [ContextMenu("Debug/Reload From Disk")]
        private void DebugReload()
        {
            LoadData();
            Debug.Log($"✅ PlayerSaveManager: Reloaded!");
        }

        [ContextMenu("Debug/Restore From Backup")]
        private void DebugRestore()
        {
            bool success = RestoreFromBackup();
            Debug.Log(success ? "✅ Backup restored!" : "❌ Restore failed");
        }

        [ContextMenu("Debug/Open Save Folder")]
        private void DebugOpenSave()
        {
            // ✅ FIX: Use the actual save folder path
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
                Debug.LogWarning($"⚠️ Folder doesn't exist yet: {folderPath}");
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
                Debug.LogWarning($"⚠️ Backup folder doesn't exist yet: {backupFolder}");
            }
        }

        [ContextMenu("Debug/Print Player Data")]
        private void DebugPrintPlayerData()
        {
            if (data == null)
            {
                Debug.Log("⚠️ No player data loaded");
                return;
            }

            Debug.Log($"╔══════════════ PLAYER DATA ══════════════╗\n" +
                     $"║ Name: {data.playerName,-31} ║\n" +
                     $"║ Play Time: {FormattedPlayTime,-28} ║\n" +
                     $"║ Play Count: {data.playCount,-29} ║\n" +
                     $"║ Save Version: v{data.saveVersion,-26} ║\n" +
                     $"║ Game Version: {data.gameVersion,-27} ║\n" +
                     $"║ Last Played: {data.lastSaveTime,-26} ║\n" +
                     $"╠══════════════════════════════════════════╣\n" +
                     $"║ Save Path: {System.IO.Path.GetFileName(savePath),-28} ║\n" +
                     $"║ Throttling: {(enableSaveThrottling ? "ON" : "OFF"),-29} ║\n" +
                     $"╚══════════════════════════════════════════╝");
        }

        [ContextMenu("Debug/Reset Player Data")]
        private void DebugResetPlayerData()
        {
            DeleteSave();
            Debug.Log("✅ Player data reset!");
        }

        [ContextMenu("Debug/Add 1 Hour Play Time")]
        private void DebugAddPlayTime()
        {
            if (data != null)
            {
                data.totalPlayTimeSeconds += 3600;
                SaveData(true);
                Debug.Log($"✅ Added 1 hour | Total: {FormattedPlayTime}");
            }
        }
#endif
    }
}