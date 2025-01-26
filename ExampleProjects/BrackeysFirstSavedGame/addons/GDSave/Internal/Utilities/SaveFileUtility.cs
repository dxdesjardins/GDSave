using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Chomp.Essentials;

namespace Chomp.Save.Internal;

public class SaveFileUtility
{
    private static string FileExtentionName => SaveSettings.Instance.FileExtensionName;
    private static string FileName => SaveSettings.Instance.FileName;
    private static string MetaExtentionName => SaveSettings.Instance.MetaDataExtentionName;
    private static string SaveFolderPath => string.Format("{0}/{1}", SaveDirectoryPath, SaveSettings.Instance.FileFolderName);
    private static string BackupExtensionName => SaveSettings.Instance.BackupExtensionName;
    private static bool ArchiveSaveFilesOnFullCorruption => SaveSettings.Instance.ArchiveSaveFilesOnFullCorruption;
    private static string SaveDirectoryPath => SaveSettings.Instance.SaveDirectory == SaveDirectory.PersistentDataDirectory ? OS.GetUserDataDir() : ProjectSettings.GlobalizePath(SaveSettings.Instance.CustomDirectory);
    private static Dictionary<int, string> cachedSlotSaveFileNames;

    public static Dictionary<int, string> ObtainSlotSaveFileNames() {
        if (cachedSlotSaveFileNames != null)
            return cachedSlotSaveFileNames;
        Dictionary<int, string> slotSavePaths = new();
        if (!Directory.Exists(SaveFolderPath))
            Directory.CreateDirectory(SaveFolderPath);
        string[] filePaths = Directory.GetFiles(SaveFolderPath);
        string[] savePaths = filePaths.Where(path => path.EndsWith(FileExtentionName)).ToArray();
        int pathCount = savePaths.Length;
        for (int i = 0; i < pathCount; i++) {
            GDS.Log($"Found save file: {savePaths[i]}");
            int getSlotNumber;
            string slotName = savePaths[i].Substring(SaveFolderPath.Length + FileName.Length + 1);
            if (int.TryParse(slotName.Substring(0, slotName.LastIndexOf(".")), out getSlotNumber)) {
                string name = $"{FileName}{slotName}";
                slotSavePaths.Add(getSlotNumber, name);
            }
        }
        cachedSlotSaveFileNames = slotSavePaths;
        return slotSavePaths;
    }

    public static string GetSaveFilePath(string fileName) {
        if (string.IsNullOrEmpty(fileName))
            return "";
        return $"{Path.Combine(SaveFolderPath, fileName)}{FileExtentionName}";
    }

    private static bool GetFileStorageConfig(int slot, out StorageConfig storageConfig) {
        SaveManager.GetMetaData("storageconfig", out string data, slot);
        if (!string.IsNullOrEmpty(data)) {
            storageConfig = System.Text.Json.JsonSerializer.Deserialize<StorageConfig>(data);
            return true;
        }
        storageConfig = default;
        return false;
    }

    internal static SaveGame CreateSaveGameInstance(StorageType storageType) {
        return new SaveGame();
    }

    private static SaveGame LoadSave(string filePath, string fileName, int slot = -1, bool loadEmptyIfCorrupt = false) {
        SaveGame getSave = null;
        bool doesFileExist = Directory.Exists(SaveFolderPath) && File.Exists(filePath);
        GDS.Log($"Loading save file: {filePath}");
        if (doesFileExist) {
            StorageConfig getConfig;
            if (GetFileStorageConfig(slot, out getConfig)) {
                getSave = CreateSaveGameInstance(getConfig.StorageType);
                if (!getSave.ReadSaveFromPath(filePath, getConfig)) {
                    if (!loadEmptyIfCorrupt)
                        return null;
                    else
                        return CreateSaveGameInstance(SaveSettings.Instance.StorageType);
                }
            }
            else
                GDS.LogErr("Failed to get storage config of save file. Will attempt to load anyway.");
        }
        if (getSave == null) {
            getSave = CreateSaveGameInstance(SaveSettings.Instance.StorageType);
            if (doesFileExist) {
                bool tryLoad = getSave.ReadSaveFromPath(filePath, SaveSettings.Instance.StorageConfig);
                if (!tryLoad) {
                    GDE.LogErr($"File({filePath}) is corrupted.");
                    if (!loadEmptyIfCorrupt)
                        return null;
                }
            }
        }
        getSave.SetFileName(Path.GetFileNameWithoutExtension(fileName));
        getSave.OnAfterLoad();
        return getSave;
    }

    public static int[] GetUsedSlots() {
        int[] saves = new int[ObtainSlotSaveFileNames().Count];
        int counter = 0;
        foreach (int item in ObtainSlotSaveFileNames().Keys) {
            saves[counter] = item;
            counter++;
        }
        return saves;
    }

    public static int GetSaveSlotCount() {
        return ObtainSlotSaveFileNames().Count;
    }

    public static SaveGame LoadSave(int slot, bool createIfEmpty = false, Action isCorrupted = null, bool overwriteIfCorrupt = false) {
        if (slot < 0 && slot != -2) {
            GDS.LogErr("Attempted to load a negative slot.");
            return null;
        }
        SaveManager.LoadingFromDiskBegin(slot);
        string fileName;
        if (ObtainSlotSaveFileNames().TryGetValue(slot, out fileName)) {
            SaveGame saveGame;
            string savePath = Path.Combine(SaveFolderPath, fileName);
            string fileWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string metaPath = Path.Combine(SaveFolderPath, $"{fileWithoutExtension}{MetaExtentionName}");
            string altPath = FileUtility.GetAlternativeFilePath(savePath, BackupExtensionName);
            string newestPath = FileUtility.GetNewestFilePath(savePath, altPath);
            if (string.IsNullOrEmpty(newestPath))
                saveGame = LoadSave(savePath, fileName, slot, true);
            else {
                saveGame = LoadSave(newestPath, fileName, slot);
                string usedPath = newestPath;
                string otherPath = newestPath == savePath ? altPath : savePath;
                bool secondSaveExists = File.Exists(otherPath);
                bool archiveNewestPath = false;
                bool archiveOtherPath = false;
                if (saveGame == null)
                    archiveNewestPath = true;
                if (saveGame == null && secondSaveExists) {
                    saveGame = LoadSave(otherPath, fileName, slot);
                    usedPath = otherPath;
                    if (saveGame == null)
                        archiveOtherPath = true;
                }
                bool bothSavesCorrupt = archiveNewestPath && archiveOtherPath;
                if (bothSavesCorrupt) {
                    if (ArchiveSaveFilesOnFullCorruption) {
                        GDE.LogErr($"Archiving corrupted file: {newestPath}");
                        GDE.LogErr($"Archiving corrupted file: {otherPath}");
                        FileUtility.ArchiveFileAsCorrupted(newestPath);
                        FileUtility.ArchiveFileAsCorrupted(otherPath);
                    }
                }
                else {
                    if (secondSaveExists || ArchiveSaveFilesOnFullCorruption) {
                        if (archiveNewestPath) {
                            GDE.LogErr($"Archiving corrupted file: {newestPath}");
                            FileUtility.ArchiveFileAsCorrupted(newestPath);
                        }
                        if (archiveOtherPath) {
                            GDE.LogErr($"Archiving corrupted file: {otherPath}");
                            FileUtility.ArchiveFileAsCorrupted(otherPath);
                        }
                    }
                    if ((archiveNewestPath || archiveOtherPath) && Path.GetExtension(usedPath) != FileExtentionName)
                        File.Copy(usedPath, usedPath == savePath ? altPath : savePath);
                }
            }

            if (saveGame == null) {
                GDE.LogErr($"Save file(s) are corrupted at slot: {slot}");
                if (ArchiveSaveFilesOnFullCorruption) {
                    ObtainSlotSaveFileNames().Remove(slot);
                    FileUtility.ArchiveFileAsCorrupted(metaPath);
                    FileUtility.ArchiveFileAsCorrupted($"{metaPath}{BackupExtensionName}");
                }
                if (isCorrupted != null)
                    isCorrupted();
                return !overwriteIfCorrupt ? null : LoadSave(savePath, fileName, slot, true);
            }
            GDS.Log($"Successful load at slot: {slot}");
            return saveGame;
        }

        if (!createIfEmpty)
            GDS.Log($"Could not load game at slot {slot}");
        else {
            GDS.Log($"Creating save at slot {slot}");
            SaveGame saveGame = new();
            return saveGame;
        }
        return null;
    }

    public static void WriteSave(SaveGame saveGame, int saveSlot = -1, string fileName = "") {
        string savePath = "";
        if (saveSlot != -1)
            savePath = string.Format("{0}/{1}{2}{3}", SaveFolderPath, SaveFileUtility.FileName, saveSlot.ToString(), FileExtentionName);
        else {
            if (!string.IsNullOrEmpty(fileName))
                savePath = string.Format("{0}/{1}{2}", SaveFolderPath, fileName, FileExtentionName);
            else {
                GDE.LogErr("Specified file name is empty");
                return;
            }
        }
        var directoryName = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(directoryName))
            Directory.CreateDirectory(directoryName);
        Dictionary<int, string> getFileNames = ObtainSlotSaveFileNames();
        if (!getFileNames.ContainsKey(saveSlot))
            getFileNames.Add(saveSlot, savePath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(savePath);
        string altPath = FileUtility.GetAlternativeFilePath(savePath, BackupExtensionName);
        savePath = FileUtility.GetOldestFilePath(savePath, altPath);
        StorageType storageType = SaveSettings.Instance.StorageType;
        GDS.Log($"Saving game slot {saveSlot.ToString()} to : {savePath}. Using storage type: {storageType}");
        saveGame.SetFileName(fileNameWithoutExtension);
        saveGame.OnBeforeWrite();
        saveGame.WriteSaveFile(saveGame, savePath);
    }

    public static void DeleteSave(int slot) {
        string filePath = string.Format("{0}/{1}{2}{3}", SaveFolderPath, FileName, slot, FileExtentionName);
        string metaDataFilePath = string.Format("{0}/{1}{2}{3}", SaveFolderPath, FileName, slot, MetaExtentionName);
        string alternativeFilePath = FileUtility.GetAlternativeFilePath(filePath, BackupExtensionName);
        string alternativeMetaPath = FileUtility.GetAlternativeFilePath(metaDataFilePath, BackupExtensionName);
        File.Delete(alternativeFilePath);
        File.Delete(alternativeMetaPath);
        File.Delete(filePath);
        File.Delete(metaDataFilePath);
        if (ObtainSlotSaveFileNames().ContainsKey(slot))
            ObtainSlotSaveFileNames().Remove(slot);
        GDS.Log($"Deleted save data for slot: {slot}");
    }

    public static void DeleteAllSaveFiles() {
        if (!Directory.Exists(SaveFolderPath))
            return;
        string[] filePaths = Directory.GetFiles(SaveFolderPath);
        foreach (string path in filePaths.Where(path => path.EndsWith(FileExtentionName)).ToArray())
            File.Delete(path);
        foreach (string path in filePaths.Where(path => path.EndsWith(MetaExtentionName)).ToArray())
            File.Delete(path);
        foreach (string path in filePaths.Where(path => path.EndsWith(BackupExtensionName)).ToArray())
            File.Delete(path);
        GDS.Log("Successfully deleted all save files and metadata");
    }

    public static bool IsSlotUsed(int index) {
        return ObtainSlotSaveFileNames().ContainsKey(index);
    }

    public static bool IsSaveFileNameUsed(string fileName) {
        string filePath = string.Format("{0}/{1}{2}", SaveFolderPath, fileName, FileExtentionName);
        return File.Exists(filePath);
    }

    public static int GetAvailableSaveSlot() {
        int slotCount = SaveSettings.Instance.MaxSaveSlotCount;
        for (int i = 0; i < slotCount; i++)
            if (!ObtainSlotSaveFileNames().ContainsKey(i))
                return i;
        return -1;
    }

    public static string ObtainSlotFileName(int slot) {
        string fileName = "";
        ObtainSlotSaveFileNames().TryGetValue(slot, out fileName);
        if (!string.IsNullOrEmpty(fileName))
            fileName = Path.GetFileNameWithoutExtension(fileName);
        else
            return string.Format("{0}{1}", SaveFileUtility.FileName, slot);
        return fileName;
    }
}