using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;

namespace Chomp.Save.Internal;

[Serializable]
public class SaveGame : IDisposable
{
    [JsonIgnore] [NonSerialized] public float gameVersion;
    [JsonIgnore] [NonSerialized] public DateTime creationDate;
    [JsonIgnore] [NonSerialized] public DateTime modificationDate;
    [JsonIgnore] [NonSerialized] public TimeSpan timePlayed;

    public SaveMetaData MetaData { get; set; }
    public List<SaveGameData> SaveData { get; set; } = new();
    [JsonIgnore] [NonSerialized] internal Dictionary<string, int> saveDataCache = new(StringComparer.Ordinal);
    [JsonIgnore] [NonSerialized] private Dictionary<string, List<string>> stageObjectIDS = new();
    [JsonIgnore] [NonSerialized] public string fileName;

    // This struct is needed to store metadata using serialable compatible types.
    [Serializable]
    public struct SaveMetaData
    {
        public float GameVersion { get; set; }
        public string CreationDate { get; set; }
        public string TimePlayed { get; set; }
        public string ModificationDate { get; set; }
    }

    [Serializable]
    public struct SaveGameData
    {
        public string Guid { get; set; }
        public string Stage { get; set; }
        public string Data { get; set; }

        public SaveGameData() {
            Guid = "";
            Stage = "";
            Data = "";
        }
    }

    public SaveGame() {
        gameVersion = SaveSettings.Instance.GameVersion;
    }

    public void WriteSaveFile(SaveGame saveGame, string savePath) {
        try {
            byte[] fileData = default;
            if (SaveSettings.Instance.StorageType == StorageType.JSON) {
                string json = GDS.Serialize(saveGame);
                fileData = Encoding.UTF8.GetBytes(json);
            }
            else if (SaveSettings.Instance.StorageType == StorageType.Binary) {
                using MemoryStream memoryStream = new();
                using BinaryWriter writer = new(memoryStream, Encoding.UTF8);
                writer.Write(MetaData.GameVersion);
                writer.Write(MetaData.CreationDate);
                writer.Write(MetaData.ModificationDate);
                writer.Write(MetaData.TimePlayed);
                int dataCount = SaveData.Count;
                writer.Write(dataCount);
                for (int i = 0; i < dataCount; i++) {
                    writer.Write(SaveData[i].Guid);
                    writer.Write(SaveData[i].Stage);
                    writer.Write(SaveData[i].Data);
                }
                fileData = memoryStream.ToArray();
            }
            if (SaveSettings.Instance.UseEncryption)
                fileData = FileUtility.UnsecureEncrypt(fileData);
            File.WriteAllBytes(savePath, fileData);
        }
        catch (Exception ex) {
            GDS.LogErr($"Failed to write save file: {ex.Message}");
        }
    }

    public bool ReadSaveFromPath(string savePath, StorageConfig storageConfig) {
        byte[] fileData = File.ReadAllBytes(savePath);
        if (storageConfig.UseEncryption)
            fileData = FileUtility.UnsecureDecrypt(fileData);
        if (storageConfig.StorageType == StorageType.JSON) {
            try {
                string data = Encoding.UTF8.GetString(fileData);
                if (string.IsNullOrEmpty(data)) {
                    GDS.LogErr($"Json save file({savePath}) is empty. It will be automatically deleted.");
                    File.Delete(savePath);
                    return false;
                }
                SaveGame saveGame = JsonSerializer.Deserialize<SaveGame>(data, GDS.GetSerializerOptions(storageConfig));
                MetaData = saveGame.MetaData;
                SaveData = saveGame.SaveData;
            }
            catch (Exception ex) {
                GDS.LogErr($"Failed to read Json save file: {ex.Message}");
                return false;
            }
        }
        else if (storageConfig.StorageType == StorageType.Binary) {
            using MemoryStream stream = new(fileData);
            using BinaryReader reader = new(stream, Encoding.UTF8);
            try {
                MetaData = new SaveMetaData() {
                    GameVersion = reader.ReadSingle(),
                    CreationDate = reader.ReadString(),
                    ModificationDate = reader.ReadString(),
                    TimePlayed = reader.ReadString(),
                };
                int dataCount = reader.ReadInt32();
                for (int i = 0; i < dataCount; i++) {
                    SaveData.Add(new SaveGameData() {
                        Guid = reader.ReadString(),
                        Stage = reader.ReadString(),
                        Data = reader.ReadString(),
                    });
                }
            }
            catch (Exception ex) {
                GDS.LogErr($"Failed to read binary save file: {ex.Message}");
                return false;
            }
        }
        return true;
    }

    public string Get(string id) {
        int saveIndex;
        if (saveDataCache.TryGetValue(id, out saveIndex))
            return SaveData[saveIndex].Data;
        else
            return null;
    }

    public void OnAfterLoad() {
        gameVersion = SaveSettings.Instance.GameVersion;
        DateTime.TryParse(MetaData.CreationDate, out creationDate);
        TimeSpan.TryParse(MetaData.TimePlayed, out timePlayed);
        DateTime.TryParse(MetaData.ModificationDate, out modificationDate);
        if (SaveData.Count > 0) {
            // Remove empty data
            int dataCount = SaveData.Count;
            for (int i = dataCount - 1; i >= 0; i--) {
                if (string.IsNullOrEmpty(SaveData[i].Data))
                    SaveData.RemoveAt(i);
            }
            for (int i = 0; i < SaveData.Count; i++) {
                if (!saveDataCache.ContainsKey(SaveData[i].Guid)) {
                    saveDataCache.Add(SaveData[i].Guid, i);
                    AddStageID(SaveData[i].Stage, SaveData[i].Guid);
                }
            }
        }
    }

    public void OnBeforeWrite() {
        if (creationDate == default)
            creationDate = DateTime.Now;
        var culture = SaveSettings.Instance.UseInvariantCulture ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
        MetaData = new SaveMetaData() {
            CreationDate = creationDate.ToString(culture),
            GameVersion = gameVersion,
            TimePlayed = timePlayed.ToString(),
            ModificationDate = DateTime.Now.ToString(culture),
        };
        modificationDate = DateTime.Now;
    }

    public void Remove(string guid) {
        int saveIndex;
        if (saveDataCache.TryGetValue(guid, out saveIndex)) {
            // Empty string data, it will be removed during OnAfterLoad()
            SaveData[saveIndex] = new SaveGameData();
            saveDataCache.Remove(guid);
            stageObjectIDS.Remove(guid);
        }
    }

    public void Set(string guid, string data, string stage) {
        int saveIndex;
        if (saveDataCache.TryGetValue(guid, out saveIndex))
            SaveData[saveIndex] = new SaveGameData() { Guid = guid, Data = data, Stage = stage };
        else {
            SaveGameData newSaveData = new() { Guid = guid, Data = data, Stage = stage };
            SaveData.Add(newSaveData);
            saveDataCache.Add(guid, SaveData.Count - 1);
            AddStageID(stage, guid);
        }
    }

    public void WipeStageData(string stage) {
        List<string> value;
        if (stageObjectIDS.TryGetValue(stage, out value)) {
            int elementCount = value.Count;
            for (int i = elementCount - 1; i >= 0; i--) {
                Remove(value[i]);
                value.RemoveAt(i);
            }
        }
        else
            GDS.Log($"Attempted to wipe data on stage({stage}), but it was already wiped.");
    }

    protected void AddStageID(string stage, string id) {
        List<string> value;
        if (stageObjectIDS.TryGetValue(stage, out value))
            value.Add(id);
        else {
            List<string> list = new() { id };
            stageObjectIDS.Add(stage, list);
        }
    }

    public void Dispose() {
        SaveData.Clear();
        saveDataCache.Clear();
        stageObjectIDS.Clear();
    }

    public void SetFileName(string fileName) {
        this.fileName = fileName;
    }
}
