using System;
using System.IO;
using Gotchi.Data;
using UnityEngine;

namespace Gotchi.Persistence
{
    // MVP persistence: a JSON file in Application.persistentDataPath. Replace with
    // SupabaseSaveService once the backend exists; the rest of the game only sees ISaveService.
    public class LocalJsonSaveService : ISaveService
    {
        private const string FileName = "gotchi_save.json";

        private static string FilePath => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public bool HasSave() => File.Exists(FilePath);

        public PetSaveData Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                string json = File.ReadAllText(FilePath);
                var data = JsonUtility.FromJson<PetSaveData>(json);
                return string.IsNullOrEmpty(data?.petId) ? null : data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to load: {e.Message}");
                return null;
            }
        }

        public void Save(PetSaveData data)
        {
            try
            {
                data.lastSavedUtcTicks = DateTime.UtcNow.Ticks;
                string json = JsonUtility.ToJson(data, true);
                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, json);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(temp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to save: {e.Message}");
            }
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to delete: {e.Message}");
            }
        }
    }
}
