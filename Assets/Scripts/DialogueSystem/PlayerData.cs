//=====================================
// PlayerData.cs - All Player Persistent Data
//=====================================

using System;
using UnityEngine;

namespace ChatDialogueSystem
{

    // ═══════════════════════════════════════════════════════════
    // ░ FUTURE PROGRESSION FLAGS (Examples)
    // ═══════════════════════════════════════════════════════════

    // public bool hasSeenTutorial = false;
    // public bool hasCompletedChapter1 = false;
    // public bool hasCompletedAllChats = false;
    // public bool hasSeenTrueEnding = false;

    // ═══════════════════════════════════════════════════════════
    // ░ PLAYER DATA STRUCTURE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Root container for all player persistent data.
    /// Tracks player info, play stats, and metadata.
    /// Serialized to profile.json by PlayerSaveManager.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public const int CURRENT_VERSION = 1;
        public int saveVersion = CURRENT_VERSION;
        public string playerName = "User";
        public int totalPlayTimeSeconds = 0;
        public int playCount = 0;
        public string gameVersion = "1.0.0";
        public string lastSaveTime;
    }
}