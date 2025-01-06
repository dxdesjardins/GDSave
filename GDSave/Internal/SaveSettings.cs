using System;
using Godot;
using Chomp.Essentials;
using System.Text.Json;

namespace Chomp.Save.Internal;

[GlobalClass]
[Tool]
[ResourceUid("uid://b0hv83ktjmpk8")]
public partial class SaveSettings : ResourceReference<SaveSettings, SaveSettings>
{
    [ExportGroup("Configuration")]
    [Export] public float GameVersion { get; private set; } = 1.0f;
    [Export] public SlotLoadBehaviour SlotLoadBehaviour { get; private set; } = SlotLoadBehaviour.LoadDefaultSlot;
    [Export(PropertyHint.Range, "0,299,")] public int DefaultSlot { get; private set; } = 0;
    [Export(PropertyHint.Range, "1,100,")] public int MaxSaveSlotCount { get; private set; } = 100;
    [Export] public bool TrackTimePlayed { get; private set; } = true;
    ///<summary> If true, the format of dates will be the same regardless of real-life location. </summary>
    [Export] public bool UseInvariantCulture { get; private set; } = false;

    [ExportGroup("Save File Settings")]
    [Export] public SaveDirectory SaveDirectory { get; private set; } = SaveDirectory.PersistentDataDirectory;
    [Export] public string CustomDirectory { get; private set; } = "res://";
    [Export] public string FileExtensionName { get; private set; } = ".save";
    [Export] public string FileFolderName { get; private set; } = "SaveData";
    [Export] public string TemporaryFolderName { get; private set; } = "TempSaveData";
    [Export] public string FileName { get; private set; } = "slot";
    [Export] public string MetaDataExtentionName { get; private set; } = ".info";
    [Export] public string BackupExtensionName { get; private set; } = ".backup";
    ///<summary> If save and backup are not loadable, save will be archived and slot removed. </summary>
    [Export] public bool ArchiveSaveFilesOnFullCorruption { get; private set; } = false;
    ///<summary> If save and backup are not loadable, load a new empty save. Happens after archival. </summary>
    [Export] public bool CreateNewSaveFileOnCorruption { get; private set; } = false;

    [ExportSubgroup("Storage Type")]
    [Export] public StorageType StorageType { get; private set; } = StorageType.JSON;
    [Export] public bool UseEncryption { get; private set; } = false;

    [ExportSubgroup("Json Serializer Options")]
    [Export] public bool WriteIndented { get; private set; } = true;
    [Export] public bool IncludeFields { get; private set; } = true;
    [Export] public bool IncludePrivateFields { get; private set; } = false;
    [Export] public bool UnsafeRelaxedJsonEscaping { get; private set; } = true;

    [ExportGroup("Auto Save")]
    [Export] public bool AutoSaveOnExit { get; private set; } = false;
    [Export] public bool AutoSaveOnSlotSwitch { get; private set; } = false;
    [Export] public bool SaveOnInterval { get; private set; } = false;
    [Export] public int SaveIntervalSeconds { get; private set; } = 60;

    [ExportGroup("Saver Configuration")]
    [Export] public bool ResetSaverIdOnDuplicate { get; private set; } = true;
    [Export] public bool ResetSaverIdOnNewStage { get; private set; } = false;
    [Export(PropertyHint.Range, "5,36,")] public int SaverGuidLength { get; private set; } = 5;
    [Export(PropertyHint.Range, "5,36,")] public int SaveableGuidLength { get; private set; } = 5;

    [ExportGroup("Saved Scenes")]
    ///<summary> Automatically remove and wipe data from saved scenes when changing slots. </summary>
    [Export] public bool ClearSavedScenesOnSlotSwitch { get; private set; } = true;
    ///<summary> Do not save or load saved scenes that have no data. </summary>
    [Export] public bool IgnoreDatalessSavedScenes { get; private set; } = true;

    [ExportSubgroup("Throttled Spawning")]
    [Export] public bool SpawnThrottled { get; private set; }
    [Export(PropertyHint.Range, "0,120,")] public int ThrottleTargetFramerate { get; private set; } = 30;
    [Export] public bool DontAddScenesToTreeUntilDone { get; private set; }
    [Export] public bool DisableAutoSaveDuringThrottle { get; private set; } = true;
    [Export] public bool OnlyThrottleSpecificStages { get; private set; }
    [Export] public PackedScene[] ThrottledStages { get; private set; }

    [ExportGroup("Hotkeys")]
    [Export] public bool UseHotkeys { get; private set; } = false;
    [Export] public Key SaveAndWriteToDiskKey { get; private set; } = Key.F4;
    [Export] public Key SyncSaveGameKey { get; private set; } = Key.F5;
    [Export] public Key SyncLoadGameKey { get; private set; } = Key.F6;
    [Export] public Key DeleteActiveSave { get; private set; } = Key.F7;

    [ExportGroup("Debug")]
    [Export] public bool EnableLogging { get; private set; } = false;

    private StorageConfig? storageConfig;
    public StorageConfig StorageConfig {
        get {
            return storageConfig ??= new StorageConfig() {
                StorageType = StorageType,
                WriteIndented = WriteIndented,
                IncludeFields = IncludeFields,
                IncludePrivateFields = IncludePrivateFields,
                UnsafeRelaxedJsonEscaping = UnsafeRelaxedJsonEscaping,
                UseEncryption = UseEncryption
            };
        }
        private set { storageConfig = value; }
    }
}
