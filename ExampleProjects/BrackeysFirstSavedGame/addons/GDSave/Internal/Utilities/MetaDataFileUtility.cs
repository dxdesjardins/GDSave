using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace Chomp.Save.Internal;

public class MetaDataFileUtility
{
    private static string MetaDataExtentionName => SaveSettings.Instance.MetaDataExtentionName;
    private static bool ArchiveSaveFilesOnFullCorruption => SaveSettings.Instance.ArchiveSaveFilesOnFullCorruption;
    private static string SaveDataPath => string.Format("{0}/{1}", SaveDirectoryPath, SaveSettings.Instance.FileFolderName);
    private static string SaveDirectoryPath => SaveSettings.Instance.SaveDirectory == SaveDirectory.PersistentDataDirectory ? OS.GetUserDataDir() : ProjectSettings.GlobalizePath(SaveSettings.Instance.CustomDirectory);

    public class MetaData : IDisposable
    {
        private readonly string filePath;

        public MetaData(string filePath) {
            idData = new Dictionary<string, byte[]>();
            this.filePath = filePath;
            string extensionName = SaveSettings.Instance.BackupExtensionName;
            string altPath = FileUtility.GetAlternativeFilePath(filePath, extensionName);
            string newestFilePath = FileUtility.GetNewestFilePath(filePath, altPath);
            string otherPath = newestFilePath == filePath ? altPath : filePath;
            bool secondSaveExists = File.Exists(otherPath);
            if (string.IsNullOrEmpty(newestFilePath))
                return;
            bool archiveNewestPath = ReadMetaDataFile(newestFilePath) == false;
            bool archiveOtherPath = archiveNewestPath && secondSaveExists && ReadMetaDataFile(otherPath) == false;
            bool bothSavesCorrupt = archiveNewestPath && archiveOtherPath;
            if (bothSavesCorrupt) {
                if (!ArchiveSaveFilesOnFullCorruption)
                    return;
                FileUtility.ArchiveFileAsCorrupted(newestFilePath);
                FileUtility.ArchiveFileAsCorrupted(otherPath);
            }
            else {
                if (secondSaveExists || ArchiveSaveFilesOnFullCorruption) {
                    if (archiveNewestPath)
                        FileUtility.ArchiveFileAsCorrupted(newestFilePath);
                    if (archiveOtherPath)
                        FileUtility.ArchiveFileAsCorrupted(otherPath);
                }
            }
        }

        private bool ReadMetaDataFile(string filePath) {
            using (FileStream stream = new FileStream(filePath, FileMode.Open)) {
                using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII)) {
                    try {
                        int entries = reader.ReadInt32();
                        for (int i = 0; i < entries; i++) {
                            string key = reader.ReadString();
                            int byteLength = reader.ReadInt32();
                            byte[] bytes = reader.ReadBytes(byteLength);
                            idData.Add(key, bytes);
                        }
                    }
                    catch {
                        GDS.LogErr($"Failed to read MetaData file: {filePath}.");
                        return false;
                    }
                }
            }
            return true;
        }

        public void Dispose() {
            string extensionName = SaveSettings.Instance.BackupExtensionName;
            string altPath = FileUtility.GetAlternativeFilePath(filePath, extensionName);
            string savePath = FileUtility.GetOldestFilePath(filePath, altPath);
            if (string.IsNullOrEmpty(savePath))
                savePath = filePath;
            using (FileStream stream = new FileStream(savePath, FileMode.Create)) {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII)) {
                    writer.Write(idData.Count);
                    foreach (var item in idData) {
                        writer.Write(item.Key);
                        writer.Write(item.Value.Length);
                        writer.Write(item.Value);
                    }
                }
            }
        }

        public Dictionary<string, byte[]> idData;

        public void SetData(string id, byte[] bytes) {
            if (bytes == null)
                return;
            SetOrAddMetaData(id, bytes);
        }

        public void SetData(string id, Image tex) {
            if (tex == null)
                return;
            SetOrAddMetaData(id, tex.SavePngToBuffer());
        }

        public void SetData(string id, string data) {
            if (string.IsNullOrEmpty(data))
                return;
            SetOrAddMetaData(id, Encoding.UTF8.GetBytes(data));
        }

        private void SetOrAddMetaData(string id, byte[] bytes) {
            if (idData.ContainsKey(id))
                idData[id] = bytes;
            else
                idData.Add(id, bytes);
        }

        public bool GetData(string id, out byte[] data) {
            byte[] getData;
            if (idData.TryGetValue(id, out getData)) {
                data = getData;
                return true;
            }
            data = null;
            return false;
        }

        public bool GetData(string id, Image image) {
            byte[] getData;
            if (idData.TryGetValue(id, out getData)) {
                image.LoadPngFromBuffer(getData);
                return true;
            }
            image = null;
            return false;
        }

        public bool GetData(string id, out string data) {
            byte[] getData;
            if (idData.TryGetValue(id, out getData)) {
                data = System.Text.Encoding.UTF8.GetString(getData);
                return true;
            }
            data = "";
            return false;
        }

        public void RemoveData(string id) {
            if (idData.ContainsKey(id))
                idData.Remove(id);
        }
    }

    public static MetaData[] GetAllMetaData() {
        string[] filePaths = Directory.GetFiles(SaveDataPath);
        string[] savePaths = filePaths.Where(path => path.EndsWith(MetaDataExtentionName)).ToArray();
        int pathCount = savePaths.Length;
        MetaData[] metaDataArray = new MetaData[pathCount];
        for (int i = 0; i < pathCount; i++)
            metaDataArray[i] = new MetaData(savePaths[i]);
        return metaDataArray;
    }

    public static MetaData GetMetaData(string fileName) {
        return new MetaData($"{Path.Combine(SaveDataPath, fileName)}{MetaDataExtentionName}");
    }

    internal static void DeleteMetaData(string fileName) {
        string filePath = $"{Path.Combine(SaveDataPath, fileName)}{MetaDataExtentionName}";
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
