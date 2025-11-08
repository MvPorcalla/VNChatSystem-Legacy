//=====================================
// CharacterFolderCreator.cs
//=====================================

using UnityEngine;
using UnityEditor;
using System.IO;

namespace ChatDialogueSystem
{
    public static class CharacterFolderCreator
    {
        [MenuItem("Assets/Create/Chat System/New Character Folder", false, 101)]
        public static void CreateCharacterFolder()
        {
            string parentPath = "Assets";

            // If user has a folder selected, create inside that
            if (Selection.activeObject != null)
            {
                string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (AssetDatabase.IsValidFolder(selectedPath))
                    parentPath = selectedPath;
                else
                    parentPath = Path.GetDirectoryName(selectedPath);
            }

            // Main character folder name
            string characterFolderName = "NewCharacter";
            string characterFolderPath = Path.Combine(parentPath, characterFolderName);

            if (!AssetDatabase.IsValidFolder(characterFolderPath))
                AssetDatabase.CreateFolder(parentPath, characterFolderName);

            // Subfolders
            string[] subfolders = { "CG", "Dialogue", "Profile" };
            foreach (var sub in subfolders)
            {
                string path = Path.Combine(characterFolderPath, sub);
                if (!AssetDatabase.IsValidFolder(path))
                    AssetDatabase.CreateFolder(characterFolderPath, sub);
            }

            AssetDatabase.Refresh();

            DebugHelper.Log(DebugHelper.Category.UI, $"✅ Created '{characterFolderName}' with CG, Dialogue, and Profile folders inside '{parentPath}'");
        }
    }
}
