using System;
using System.IO;
using Newtonsoft.Json;
using SlotsLogic.Match3Slots.Scripts.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlotsLogic.Match3Slots.Scripts.Combinations
{
    public class DataStorage : IDataStorage
    {
        private readonly string _sceneFolderPath;
        private string _jsonFilePath;

        public DataStorage()
        {
            var sceneName = FindSlotSceneName();
            if (string.IsNullOrEmpty(sceneName))
                throw new Exception("No scene containing 'slot' found in loaded scenes");

            _sceneFolderPath = Path.Combine(Application.streamingAssetsPath, sceneName);
            FindJsonFile();
        }

        private string FindSlotSceneName()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name.IndexOf("slot", StringComparison.OrdinalIgnoreCase) >= 0)
                    return scene.name;
            }

            return "DefaultSlotScene";
        }

        private void FindJsonFile()
        {
            try
            {
                if (!Directory.Exists(_sceneFolderPath))
                    Directory.CreateDirectory(_sceneFolderPath);

                var files = Directory.GetFiles(_sceneFolderPath, "*.json");
                if (files.Length > 0)
                    _jsonFilePath = files[0];
                else
                    _jsonFilePath = Path.Combine(_sceneFolderPath, "math.json");
            }
            catch (Exception e)
            {
                Debug.LogError("Error finding JSON file: " + e.Message);
            }
        }

        public bool HasExistingData()
        {
            if (!File.Exists(_jsonFilePath))
                return false;

            try
            {
                var json = File.ReadAllText(_jsonFilePath);
                var data = JsonConvert.DeserializeObject<AllColumnsData>(json);

                return data != null
                       && data.reelsLength != null
                       && data.reelSymbols != null
                       && data.reelSymbols.Count > 0
                       && data.reelsLength.Count == data.reelSymbols.Count
                       && data.paytable != null
                       && data.paytable.Count > 0;
            }
            catch (Exception e)
            {
                Debug.LogError("Error validating existing data: " + e.Message);
                return false;
            }
        }

        public void Save(AllColumnsData data)
        {
            try
            {
                if (!Directory.Exists(_sceneFolderPath))
                    Directory.CreateDirectory(_sceneFolderPath);

                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(_jsonFilePath, json);
                Debug.Log("Data saved to: " + _jsonFilePath);
            }
            catch (Exception e)
            {
                Debug.LogError("Save failed: " + e.Message);
            }
        }

        public AllColumnsData Load()
        {
            if (!HasExistingData())
                return null;
            try
            {
                var json = File.ReadAllText(_jsonFilePath);
                return JsonConvert.DeserializeObject<AllColumnsData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("Load failed: " + e.Message);
                return null;
            }
        }
    }
}