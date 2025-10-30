//=====================================
// PlayerData.cs - All Player Persistent Data
//=====================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChatDialogueSystem
{
    /// <summary>
    /// Root container for all player persistent data.
    /// Tracks player info, play stats, metadata, and CG unlocks.
    /// Serialized to player_data.json by PlayerSaveManager.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public const int CURRENT_VERSION = 2; // Increment for CG feature
        
        public int saveVersion = CURRENT_VERSION;
        public string playerName = "User";
        public int totalPlayTimeSeconds = 0;
        public int playCount = 0;
        public string gameVersion = "1.0.0";
        public string lastSaveTime;
        
        // ═══════════════════════════════════════════════════════════
        // ░ CG GALLERY SYSTEM
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// List of unlocked CG addressable paths.
        /// Persists even when chat dialogue is reset.
        /// </summary>
        public List<string> unlockedCGs = new List<string>();
        
        // ═══════════════════════════════════════════════════════════
        // ░ FUTURE PROGRESSION FLAGS (Examples)
        // ═══════════════════════════════════════════════════════════
        
        // public bool hasSeenTutorial = false;
        // public bool hasCompletedChapter1 = false;
        // public bool hasUnlockedGallery = false;
        // public bool hasCompletedAllChats = false;
        // public bool hasSeenTrueEnding = false;
    }
    
    /// <summary>
    /// CG metadata for gallery display.
    /// Store this in a ScriptableObject catalog (not in save data).
    /// </summary>
    [Serializable]
    public class CGMetadata
    {
        public string cgID;                    // Unique identifier (matches addressable path)
        public string displayName;             // UI display name
        public string description;             // Optional description
        public string characterName;           // Which character's CG
        public int chapterNumber;              // Chapter it appears in
        public Sprite thumbnailSprite;         // Preview image (optional)
        
        public CGMetadata(string id, string name, string character, int chapter)
        {
            cgID = id;
            displayName = name;
            characterName = character;
            chapterNumber = chapter;
        }
    }
}