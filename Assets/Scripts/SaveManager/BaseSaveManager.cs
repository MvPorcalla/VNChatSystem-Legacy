//=====================================
// BaseSaveManager.cs
//=====================================

using System;
using System.IO;
using UnityEngine;

namespace ChatDialogueSystem
{
    public abstract class BaseSaveManager<T> : MonoBehaviour where T : class, new()
    {
        // ═══════════════════════════════════════════════════════════
        // ░ CONFIGURATION
        // ═══════════════════════════════════════════════════════════

        [Header("Save Settings")]
        [SerializeField] protected string saveFileName;

        [Header("Backup Settings")]
        [SerializeField] protected bool createBackups = true;
        [SerializeField] protected int maxBackups = 3;

        [Header("Performance")]
        [Tooltip("Prevents excessive saves from rapid state changes")]
        [SerializeField] protected bool enableSaveThrottling = true;
        [Tooltip("Minimum seconds between saves (0 = no throttling)")]
        [SerializeField] protected float saveCooldown = 3f;

        // ═══════════════════════════════════════════════════════════
        // ░ CONSTANTS
        // ═══════════════════════════════════════════════════════════

        private const string ROOT_FOLDER_NAME = "SaveData";
        private const string BACKUP_FOLDER_NAME = "Backups";

        // ═══════════════════════════════════════════════════════════
        // ░ PROTECTED STATE
        // ═══════════════════════════════════════════════════════════

        protected T data;
        protected string savePath;
        protected string backupFolder;
        protected float lastSaveTime = 0f;

        // ═══════════════════════════════════════════════════════════
        // ░ NEW: ABSTRACT METHOD FOR FILENAME
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Returns the default filename for this save manager.
        /// This ensures the filename is always correct even if Unity serialization fails.
        /// </summary>
        protected abstract string GetDefaultSaveFileName();

        // ═══════════════════════════════════════════════════════════
        // ░ ABSTRACT METHODS (Override in derived classes)
        // ═══════════════════════════════════════════════════════════

        protected abstract void OnDataLoaded();
        protected abstract void OnBeforeSave();
        protected abstract T CreateDefaultData();
        protected virtual bool MigrateData(T data, int fromVersion, int toVersion) => false;
        protected virtual int GetDataVersion(T data) => 1;
        protected virtual void SetDataVersion(T data, int version) { }
        protected virtual int GetCurrentVersion() => 1;
        protected virtual DebugHelper.Category GetLogCategory() => DebugHelper.Category.SaveManager;
        protected abstract string GetDataFolderName();

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        protected virtual void Awake()
        {
            InitializeSaveSystem();
        }

        // ✅ EDITOR VALIDATION: Force correct filename when inspector loads
#if UNITY_EDITOR
        private void OnValidate()
        {
            string correctFilename = GetDefaultSaveFileName();
            if (saveFileName != correctFilename)
            {
                saveFileName = correctFilename;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        protected void InitializeSaveSystem()
        {
            // ✅ CRITICAL FIX: Always use the abstract method, never trust serialized value
            saveFileName = GetDefaultSaveFileName();

            // ✅ GUARD: Validate folder name
            string folderName = GetDataFolderName();
            if (string.IsNullOrEmpty(folderName))
            {
                string errorMsg = $"{GetType().Name} CRITICAL: GetDataFolderName() returned empty!";
                DebugHelper.LogError(GetLogCategory(), errorMsg);

#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayDialog(
                    "Save Manager Configuration Error",
                    $"{errorMsg}\n\nUsing fallback folder.",
                    "OK"
                );
#endif

                folderName = GetType().Name.Replace("SaveManager", "Data");
            }

            // Build paths properly with subfolders
            string rootFolder = Path.Combine(Application.persistentDataPath, ROOT_FOLDER_NAME);
            string dataFolder = Path.Combine(rootFolder, folderName);

            // Ensure data folder exists
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

            savePath = Path.Combine(dataFolder, saveFileName);
            backupFolder = Path.Combine(dataFolder, BACKUP_FOLDER_NAME);

            if (createBackups && !Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }

            LoadData();

            DebugHelper.Log(GetLogCategory(),
                $"{GetType().Name} initialized | File: {saveFileName} | Path: {savePath}");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ LOAD / SAVE
        // ═══════════════════════════════════════════════════════════

        protected void LoadData()
        {
            try
            {
                EnsurePathsInitialized();

                if (!File.Exists(savePath))
                {
                    DebugHelper.Log(GetLogCategory(), $"No save file found. Creating default data.");
                    data = CreateDefaultData();
                    OnDataLoaded();
                    SaveData(true);
                    return;
                }

                string json = File.ReadAllText(savePath);

                if (string.IsNullOrEmpty(json))
                {
                    DebugHelper.LogWarning(GetLogCategory(), "Save file is empty. Creating default data.");
                    data = CreateDefaultData();
                    OnDataLoaded();
                    SaveData(true);
                    return;
                }

                data = JsonUtility.FromJson<T>(json);

                if (data == null)
                {
                    DebugHelper.LogWarning(GetLogCategory(), "Failed to parse save data. Attempting backup restore...");

                    if (RestoreFromBackup())
                    {
                        DebugHelper.Log(GetLogCategory(), "Successfully restored from backup!");
                        return;
                    }

                    data = CreateDefaultData();
                    OnDataLoaded();
                    return;
                }

                // Version migration
                int currentVersion = GetCurrentVersion();
                int dataVersion = GetDataVersion(data);
                
                if (dataVersion < currentVersion)
                {
                    DebugHelper.Log(GetLogCategory(), $"Migrating data from v{dataVersion} to v{currentVersion}");
                    
                    if (MigrateData(data, dataVersion, currentVersion))
                    {
                        SetDataVersion(data, currentVersion);
                        SaveData(true);
                        DebugHelper.Log(GetLogCategory(), $"Migration complete: v{dataVersion} → v{currentVersion}");
                    }
                }

                OnDataLoaded();
                DebugHelper.Log(GetLogCategory(), $"Data loaded successfully | Version: {dataVersion}");
            }
            catch (Exception e)
            {
                DebugHelper.LogError(GetLogCategory(), $"Load failed: {e.Message}\n{e.StackTrace}");

                if (RestoreFromBackup())
                {
                    DebugHelper.Log(GetLogCategory(), "Recovered from backup after load failure!");
                    return;
                }

                data = CreateDefaultData();
                OnDataLoaded();
            }
        }

        protected void SaveData(bool forceSave = false)
        {
            if (!forceSave && enableSaveThrottling && saveCooldown > 0)
            {
                float timeSinceLastSave = Time.realtimeSinceStartup - lastSaveTime;
                if (timeSinceLastSave < saveCooldown)
                {
                    DebugHelper.Log(GetLogCategory(),
                        $"Save throttled ({timeSinceLastSave:F1}s < {saveCooldown}s)");
                    return;
                }
            }

            try
            {
                EnsurePathsInitialized();
                OnBeforeSave();

                string json = JsonUtility.ToJson(data, true);

                if (createBackups && File.Exists(savePath))
                {
                    CreateBackup();
                }

                File.WriteAllText(savePath, json);
                lastSaveTime = Time.realtimeSinceStartup;

                DebugHelper.Log(GetLogCategory(),
                    $"Saved successfully | Version: {GetDataVersion(data)}");
            }
            catch (Exception e)
            {
                DebugHelper.LogError(GetLogCategory(), $"Save failed: {e.Message}\n{e.StackTrace}");
            }
        }

        protected void EnsurePathsInitialized()
        {
            if (string.IsNullOrEmpty(savePath) || string.IsNullOrEmpty(backupFolder))
            {
                // ✅ CRITICAL: Force re-initialize with correct filename
                saveFileName = GetDefaultSaveFileName();
                
                string rootFolder = Path.Combine(Application.persistentDataPath, ROOT_FOLDER_NAME);
                string dataFolder = Path.Combine(rootFolder, GetDataFolderName());

                if (!Directory.Exists(dataFolder))
                {
                    Directory.CreateDirectory(dataFolder);
                }

                savePath = Path.Combine(dataFolder, saveFileName);
                backupFolder = Path.Combine(dataFolder, BACKUP_FOLDER_NAME);

                if (createBackups && !Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ BACKUP SYSTEM
        // ═══════════════════════════════════════════════════════════

        protected void CreateBackup()
        {
            try
            {
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string backupPath = Path.Combine(backupFolder, $"backup_{timestamp}_{saveFileName}");

                File.Copy(savePath, backupPath, overwrite: false);
                CleanOldBackups();

                DebugHelper.Log(GetLogCategory(), $"Backup created: {Path.GetFileName(backupPath)}");
            }
            catch (Exception e)
            {
                DebugHelper.LogWarning(GetLogCategory(), $"Backup failed: {e.Message}");
            }
        }

        protected void CleanOldBackups()
        {
            try
            {
                if (!Directory.Exists(backupFolder)) return;

                DirectoryInfo backupDir = new DirectoryInfo(backupFolder);
                FileInfo[] backupFiles = backupDir.GetFiles($"backup_*_{saveFileName}");

                if (backupFiles.Length > maxBackups)
                {
                    Array.Sort(backupFiles, (x, y) => x.CreationTime.CompareTo(y.CreationTime));

                    int toDelete = backupFiles.Length - maxBackups;
                    for (int i = 0; i < toDelete; i++)
                    {
                        backupFiles[i].Delete();
                    }
                }
            }
            catch (Exception e)
            {
                DebugHelper.LogWarning(GetLogCategory(), $"Backup cleanup failed: {e.Message}");
            }
        }

        public bool RestoreFromBackup()
        {
            try
            {
                if (!Directory.Exists(backupFolder))
                {
                    DebugHelper.LogWarning(GetLogCategory(), "Backup folder doesn't exist");
                    return false;
                }

                DirectoryInfo backupDir = new DirectoryInfo(backupFolder);
                FileInfo[] backupFiles = backupDir.GetFiles($"backup_*_{saveFileName}");

                if (backupFiles.Length == 0)
                {
                    DebugHelper.LogWarning(GetLogCategory(), "No backups found");
                    return false;
                }

                Array.Sort(backupFiles, (x, y) => y.CreationTime.CompareTo(x.CreationTime));
                FileInfo mostRecent = backupFiles[0];

                File.Copy(mostRecent.FullName, savePath, overwrite: true);
                LoadData();

                DebugHelper.Log(GetLogCategory(), $"Restored from backup: {mostRecent.Name}");
                return true;
            }
            catch (Exception e)
            {
                DebugHelper.LogError(GetLogCategory(), $"Restore failed: {e.Message}");
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════

        public bool SaveExists() => File.Exists(savePath);
        public string GetSavePath() => savePath;
        public string GetBackupPath() => backupFolder;

        public virtual bool DeleteSave()
        {
            try
            {
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    DebugHelper.Log(GetLogCategory(), "Save file deleted");
                }

                data = CreateDefaultData();
                OnDataLoaded();
                SaveData(true);
                return true;
            }
            catch (Exception e)
            {
                DebugHelper.LogError(GetLogCategory(), $"Delete failed: {e.Message}");
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ LIFECYCLE
        // ═══════════════════════════════════════════════════════════

        protected virtual void OnApplicationQuit()
        {
            DebugHelper.Log(GetLogCategory(), "Application quitting - final save");
            SaveData(true);
        }
    }
}