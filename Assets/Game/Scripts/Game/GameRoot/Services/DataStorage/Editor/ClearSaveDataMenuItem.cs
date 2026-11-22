#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.GameRoot.Services.DataStorage.Editor
{
    public static class ClearSaveDataMenuItem
    {
        private const string MenuPath = "Game/Clear Save Data";
        private const string FileName = "game_data.json";

        [MenuItem(MenuPath)]
        private static void ClearSaveData()
        {
            var filePath = Path.Combine(Application.persistentDataPath, FileName);

            if (!File.Exists(filePath))
            {
                Debug.Log($"[ClearSaveData] No save file found at: {filePath}");
                return;
            }

            var confirmed = EditorUtility.DisplayDialog(
                "Clear Save Data",
                $"Delete save file?\n{filePath}",
                "Delete",
                "Cancel"
            );

            if (!confirmed) return;

            File.Delete(filePath);
            Debug.Log($"[ClearSaveData] Save file deleted: {filePath}");
        }
    }
}
#endif

