using System;
using System.Collections.Generic;
using System.Globalization;
using Chomp.Save.Components;
using Chomp.Save.Internal;
using Godot;
using Chomp.Essentials;
using System.Threading;
using System.Threading.Tasks;

namespace Chomp.Save;

public partial class SaveManager : NodeSingleton<SaveManager>
{
    /// <summary> Param 1 = newSlot | Param 2 = oldSlot. Called before saveables are written to disk during slot change. </summary>
    public static Action<int, int> SlotChangeBegin { get; set; } = delegate { };
    /// <summary> Param 1 = newSlot | Param 2 = oldSlot. Called after saveables are written to disk during slot change. </summary>
    public static Action<int, int> SlotChangeDone { get; set; } = delegate { };
    public static Action<int> SyncSaveBegin { get; set; } = delegate { };
    public static Action<int> SyncSaveDone { get; set; } = delegate { };
    public static Action<int> SyncLoadBegin { get; set; } = delegate { };
    public static Action<int> SyncLoadDone { get; set; } = delegate { };
    public static Action<int> LoadingFromDiskBegin { get;  set; } = delegate { };
    public static Action<int> LoadingFromDiskDone { get; set; } = delegate { };
    public static Action<int> LoadingFromDiskCorrupt { get; set; } = delegate { };
    public static Action<int> WritingToDiskBegin { get; set; } = delegate { };
    public static Action<int> WritingToDiskDone { get; set; } = delegate { };
    public static Action<int> DeletedSave { get; set; } = delegate { };
    public static Action DeletedAllSaves { get; set; } = delegate { };
    public static Action<PackedScene, Node> SpawnedSavedScene { get; set; } = delegate { };
    public static Action StartedSpawningThrottledSavedScenes { get; set; } = delegate { };
    public static Action<PackedScene, Node, int, int, float> SpawnedThrottledSavedScene { get; set; } = delegate { };
    public static Action FinishedSpawningThrottledSavedScenes { get; set; } = delegate { };

    /// <summary> string = FileName without extension | int = object hash </summary>
    private static Dictionary<string, int> loadedStageFileNames = new();
    private static Dictionary<int, StageInfo> loadedStageInfos = new();
    /// <summary> int = object hash | string = FileName without extension </summary>
    private static Dictionary<int, string> multiLoadedStageHashs = new();
    private static Dictionary<int, SavedSceneManager> savedSceneManagers = new();
    private static HashSet<Saver> savers = new();
    private static SaveGame activeSaveGame = null;
    private static int activeSlot = -1;
    private static bool invokedWritingToDiskEvent = false;
    private static bool disableAutoSave;
    private static bool autoSaveOnExit => SaveSettings.Instance.AutoSaveOnExit && !disableAutoSave;
    private static Dictionary<string, Func<string, Node>> customSceneSpawners = new();
    private CancellationTokenSource cts = new();

    internal struct StageInfo
    {
        public string fileName;
        public int hash;
        public bool isLoadedMultipleTimes;
    }

    ~SaveManager() => cts.CancelAndDispose();

    public override void _EnterTree() {
        var settings = SaveSettings.Instance;
        switch (settings.SlotLoadBehaviour) {
            case SlotLoadBehaviour.LoadDefaultSlot:
                SetSlot(settings.DefaultSlot, true);
                break;
            case SlotLoadBehaviour.LoadTemporarySlot:
                SetSlot(-2, true);
                break;
            case SlotLoadBehaviour.DontLoadSlot:
                break;
            default:
                break;
        }
        if (settings.TrackTimePlayed)
            IncrementTimePlayed();
        if (settings.SaveOnInterval)
            AutoSaveGame();
        if (settings.DisableAutoSaveDuringThrottle) {
            StartedSpawningThrottledSavedScenes += () => disableAutoSave = true;
            FinishedSpawningThrottledSavedScenes += () => disableAutoSave = false;
        }
    }

    public override void _Ready() {
        StageManager.StageLoaded += OnStageLoaded;
        StageManager.StageUnloaded += OnStageUnloaded;
        StageManager.ApplicationPaused += OnApplicationPause;
        StageManager.ApplicationQuitting += OnApplicationQuit;
    }

    public override void _ExitTree() {
        cts = cts.StopAllCoroutines();
    }

    public override void _Input(InputEvent _input) {
        var settings = SaveSettings.Instance;
        if (!settings.UseHotkeys)
            return;
        if (_input is InputEventKey keyInput) {
            if (keyInput.IsJustPressed(settings.DeleteActiveSave)) {
                ClearActiveSaveData();
            }
            if (keyInput.IsJustPressed(settings.SaveAndWriteToDiskKey)) {
                var stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                WriteActiveSaveToDisk();
                stopWatch.Stop();
                GDS.Log($"Synced objects and wrote game to disk. MS: {stopWatch.ElapsedMilliseconds}");
            }
            if (keyInput.IsJustPressed(settings.SyncSaveGameKey)) {
                var stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                SyncSave();
                stopWatch.Stop();
                GDS.Log($"Save Synced objects. MS: {stopWatch.ElapsedMilliseconds}");
            }
            if (keyInput.IsJustPressed(settings.SyncLoadGameKey)) {
                var stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                SyncLoad();
                stopWatch.Stop();
                GDS.Log($"Load Synced objects. MS: {stopWatch.ElapsedMilliseconds}");
            }
        }
    }

    public static string GetStageFileName(int stageHash) {
        if (loadedStageInfos.TryGetValue(stageHash, out StageInfo stageInfo))
            return stageInfo.fileName;
        GDS.LogErr($"StageInfo for stage hash({stageHash}) was not found.");
        return "";
    }

    public static void AddCustomSceneSpawner(string id, Func<string, Node> function) {
        customSceneSpawners.TryAdd(id, function);
    }

    public static void RemoveCustomSceneSpawner(string id) {
        customSceneSpawners.Remove(id);
    }

    internal static Node SpawnCustomScene(string id, string funcInput) {
        if (customSceneSpawners.TryGetValue(id, out Func<string, Node> sceneGenerator))
            return sceneGenerator.Invoke(funcInput);
        else {
            GDS.LogErr($"Failed to find custom scene spawner for id: {id}");
            return null;
        }
    }


    /// <summary> Note: you will need to manually SpawnSavedSceneManager for stages loaded multiple times and set a custom stageId. </summary>
    private static void OnStageLoaded(Node stage) {
        int stageHash = stage.GetHashCode();
        string stageFileName = stage.GetFileName();
        StageInfo stageInfo = new() {
            hash = stageHash,
            fileName = stageFileName
        };
        if (!loadedStageFileNames.TryAdd(stageFileName, stageHash)) {
            stageInfo.isLoadedMultipleTimes = true;
            multiLoadedStageHashs.Add(stageHash, stageFileName);
        }
        loadedStageInfos.Add(stageHash, stageInfo);
        if (activeSaveGame == null || string.IsNullOrEmpty(activeSaveGame.Get($"SaveMaster-{stageFileName}-SSM")))
            return;
        if (!savedSceneManagers.ContainsKey(stage.GetHashCode()) && !stageInfo.isLoadedMultipleTimes)
            SpawnSavedSceneManager(stage);
    }

    private static void OnStageUnloaded(Node stage) {
        int stageHash = stage.GetHashCode();
        StageInfo stageInfo;
        if (!loadedStageInfos.TryGetValue(stageHash, out stageInfo)) {
            GDS.LogErr($"Unloading stage({stage.Name}) has no StageInfo.");
            return;
        }
        if (stageInfo.isLoadedMultipleTimes) {
            multiLoadedStageHashs.Remove(stageHash);
            if (!multiLoadedStageHashs.ContainsValue(stageInfo.fileName))
                stageInfo.isLoadedMultipleTimes = false;
        }
        else
            loadedStageFileNames.Remove(stageInfo.fileName);
        loadedStageInfos.Remove(stageHash);
        if (activeSaveGame == null)
            return;
        if (savedSceneManagers.TryGetValue(stageHash, out SavedSceneManager instanceManager)) {
            if (activeSaveGame != null)
                instanceManager.OnSave(activeSaveGame);
            savedSceneManagers.Remove(stageHash);
        }
    }

    /// <summary> Note: you will need to manually spawn a SavedSceneManager for stages loaded multiple times and set a custom stageId. </summary>
    public static SavedSceneManager SpawnSavedSceneManager(Node stage, string stageId = "") {
        int stageHash = stage.GetHashCode();
        StageInfo stageInfo;
        if (!loadedStageInfos.TryGetValue(stageHash, out stageInfo)) {
            OnStageLoaded(stage);
            loadedStageInfos.TryGetValue(stageHash, out stageInfo);
        }
        SavedSceneManager savedSceneManager;
        if (savedSceneManagers.TryGetValue(stageHash, out savedSceneManager))
            return savedSceneManager;
        savedSceneManager = stage.InstantiateChild<SavedSceneManager>();
        savedSceneManagers.Add(stageHash, savedSceneManager);
        string stageID = string.IsNullOrEmpty(stageId) ? stageInfo.fileName : $"{stageInfo.fileName}-{stageId}";
        string saveIdentification = $"SaveMaster-{stageID}-SSM";
        savedSceneManager.SaveIdentification = saveIdentification;
        savedSceneManager.StageId = stageID;
        savedSceneManager.Stage = stage;
        savedSceneManager.OnLoad(activeSaveGame);
        return savedSceneManager;
    }

    public static bool IsComponentLoaded(ISaveable saveable) {
        return Saver.IsSaveableLoaded(saveable);
    }

    /// <summary> -1 means no slot is loaded </summary>
    public static int GetActiveSlot() {
        return activeSlot;
    }

    public static bool IsSlotLoaded() {
        return !(activeSlot == -1 || activeSaveGame == null);
    }

    public static bool HasUnusedSlots() {
        return SaveFileUtility.GetAvailableSaveSlot() != -1;
    }

    public static int[] GetUsedSlots() {
        return SaveFileUtility.GetUsedSlots();
    }

    public static bool IsSlotUsed(int slot) {
        return SaveFileUtility.IsSlotUsed(slot);
    }

    /// <summary> Returns if the slot exceeds the max slot capacity </summary>
    public static bool IsSlotValid(int slot) {
        return slot + 1 <= SaveSettings.Instance.MaxSaveSlotCount;
    }

    /// <summary> Reloads the active save file without saving it. Useful if you have a save point system. </summary>
    public static void ReloadActiveSaveFromDisk() {
        int activeSlot = GetActiveSlot();
        ClearSlot(false);
        SetSlot(activeSlot, true);
    }

    /// <summary> Returns -1 if no slot is not found or is no longer valid. </summary>
    public static int GetLastUsedValidSlot() {
        int lastUsedSlot = PreferenceManager.GetPreference("SM-LastUsedSlot", -1).As<int>();
        if (lastUsedSlot < 0)
            return -1;
        bool slotValid = SaveFileUtility.IsSlotUsed(lastUsedSlot);
        if (!slotValid)
            return -1;
        return lastUsedSlot;
    }

    public static bool TrySetSlotToLastUsed(bool reloadSaveables) {
        int lastUsedSlot = GetLastUsedValidSlot();
        if (lastUsedSlot == -1)
            return false;
        else {
            SetSlot(lastUsedSlot, reloadSaveables);
            return true;
        }
    }

    public static bool TrySetSlotToNew(bool notifyListeners, out int slot) {
        int availableSlot = SaveFileUtility.GetAvailableSaveSlot();
        if (availableSlot == -1) {
            slot = -1;
            return false;
        }
        else {
            SetSlot(availableSlot, notifyListeners);
            slot = availableSlot;
            return true;
        }
    }

    public static void ClearSlot(bool clearAllListeners = true, bool syncSave = true) {
        if (clearAllListeners)
            RemoveAllListeners(syncSave);
        activeSlot = -1;
        activeSaveGame?.Dispose();
        activeSaveGame = null;
    }

    public static void SetSlotWithoutSaving(int slot) {
        SlotChangeBegin.Invoke(slot, activeSlot);
        int fromSlot = activeSlot;
        activeSlot = slot;
        LoadingFromDiskBegin(slot);
        activeSaveGame = SaveFileUtility.LoadSave(slot, true, () => { LoadingFromDiskCorrupt(slot); }, SaveSettings.Instance.CreateNewSaveFileOnCorruption);
        LoadingFromDiskDone(slot);
        SyncReset();
        SyncSave();
        SlotChangeDone.Invoke(slot, fromSlot);
    }

    public static void SetSlotToTemporary(bool reloadSaveables, bool keepActiveSaveData = false) {
        SetSlot(-2, reloadSaveables, keepActiveSaveData: keepActiveSaveData);
    }

    public static TimeSpan? GetTimeSinceLastSave() {
        if (activeSaveGame != null && activeSlot != -1)
            return DateTime.Now - activeSaveGame.modificationDate;
        else {
            GDS.Log("Failed to get time since last save. No slot is loaded.");
            return null;
        }
    }

    public static bool IsActiveSaveNew() {
        if (activeSaveGame != null && activeSlot != -1)
            return activeSaveGame.creationDate == default;
        else {
            GDS.Log("Failed to check if active save is new. No slot is loaded.");
            return false;
        }
    }

    public static void SetSlot(int slot, bool reloadSaveables, SaveGame saveGame = null, bool keepActiveSaveData = false, bool writeToDiskAfterChange = false) {
        if (keepActiveSaveData && activeSaveGame == null) {
            GDS.Log("Unable to keep active save data. No save slot loaded.");
            return;
        }
        if (!keepActiveSaveData) {
            if (SaveSettings.Instance.AutoSaveOnSlotSwitch && !disableAutoSave && activeSaveGame != null)
                WriteActiveSaveToDisk();
            if (SaveSettings.Instance.ClearSavedScenesOnSlotSwitch)
                RemoveAndWipeActiveSavedStages();
        }
        if ((slot < 0 && slot != -2) || slot > SaveSettings.Instance.MaxSaveSlotCount) {
            GDS.LogErr($"Attempted to set invalid slot: {slot}");
            return;
        }
        if (slot != activeSlot)
            SlotChangeBegin.Invoke(slot, activeSlot);
        int fromSlot = activeSlot;
        activeSlot = slot;
        if (!keepActiveSaveData) {
            activeSaveGame?.Dispose();
            // If slot is not temp
            if (slot != -2 || saveGame != null) {
                LoadingFromDiskBegin(slot);
                if (saveGame == null)
                    activeSaveGame = SaveFileUtility.LoadSave( slot, true, () => { LoadingFromDiskCorrupt(slot); }, SaveSettings.Instance.CreateNewSaveFileOnCorruption);
                else
                    activeSaveGame = saveGame;
                LoadingFromDiskDone(slot);
            }
            // Create a temp save
            else
                activeSaveGame = SaveFileUtility.CreateSaveGameInstance(SaveSettings.Instance.StorageType);
            if (reloadSaveables)
                SyncLoad();
            SyncReset();
        }
        else
            activeSaveGame.SetFileName(SaveFileUtility.ObtainSlotFileName(slot));
        if (writeToDiskAfterChange)
            WriteActiveSaveToDisk();
        if (slot >= 0)
            PreferenceManager.SetPreference("SM-LastUsedSlot", slot);
        SlotChangeDone.Invoke(slot, fromSlot);
    }

    public static DateTime GetSaveCreationTime(int slot) {
        if (slot == activeSlot)
            return activeSaveGame.creationDate;
        if (!IsSlotUsed(slot))
            return new DateTime();
        SaveGame getSave = GetSave(slot, true);
        return getSave != null ? getSave.creationDate : default;
    }

    public static DateTime GetSaveCreationTime() {
        return GetSaveCreationTime(activeSlot);
    }

    public static TimeSpan GetSaveTimePlayed(int slot) {
        if (slot == activeSlot)
            return activeSaveGame.timePlayed;
        if (!IsSlotUsed(slot))
            return new TimeSpan();
        SaveGame getSave = GetSave(slot, true);
        return getSave != null ? getSave.timePlayed : default;
    }

    public static TimeSpan GetSaveTimePlayed() {
        return GetSaveTimePlayed(activeSlot);
    }

    public static float GetGameVersion(int slot) {
        if (slot == activeSlot)
            return activeSaveGame.gameVersion;
        if (!IsSlotUsed(slot))
            return -1;
        SaveGame getSave = GetSave(slot, true);
        return getSave != null ? getSave.gameVersion : -1;
    }

    public static float GetGameVersion() {
        return GetGameVersion(activeSlot);
    }

    private static SaveGame GetSave(int slot, bool createIfEmpty = true) {
        if (slot == activeSlot)
            return activeSaveGame;
        return SaveFileUtility.LoadSave(slot, createIfEmpty);
    }

    public static void WriteToOtherSlot(int slot, bool syncSaveables = true) {
        if (activeSaveGame == null) {
            GDS.Log("Tried to save without an active slot.");
            return;
        }
        int lastSlot = activeSlot;
        activeSlot = slot;
        activeSaveGame.SetFileName(SaveFileUtility.ObtainSlotFileName(slot));
        if (!invokedWritingToDiskEvent) {
            WritingToDiskBegin.Invoke(slot);
            invokedWritingToDiskEvent = true;
        }
        if (syncSaveables)
            SyncSave();
        SaveFileUtility.WriteSave(activeSaveGame, slot);
        var culture = SaveSettings.Instance.UseInvariantCulture ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
        SetMetaDataMultiple( metaData => {
            metaData.SetData("timeplayed", activeSaveGame.timePlayed.ToString());
            metaData.SetData("gameversion", activeSaveGame.gameVersion.ToString());
            metaData.SetData("creationdate", activeSaveGame.creationDate.ToString(culture));
            metaData.SetData("lastsavedtime", DateTime.Now.ToString(culture));
            metaData.SetData("storageconfig", System.Text.Json.JsonSerializer.Serialize(SaveSettings.Instance.StorageConfig));
        }, slot);
        WritingToDiskDone.Invoke(slot);
        invokedWritingToDiskEvent = false;
        activeSlot = lastSlot;
        activeSaveGame.SetFileName(SaveFileUtility.ObtainSlotFileName(lastSlot));
    }

    public static void LoadFromOtherSlot(int otherSlot, bool reloadSaveables = true) {
        if (activeSaveGame == null) {
            GDS.Log("Tried to load other slot without an active slot.");
            return;
        }
        int slot = activeSlot;
        activeSlot = -5;
        SetSlot(slot, reloadSaveables, SaveFileUtility.LoadSave(otherSlot));
        SlotChangeDone(-5, slot);
        SyncReset();
        SyncLoad();
    }

    public static void WriteActiveSaveToDisk(bool syncActiveSaveables = true) {
        if (activeSlot == -2)
            return;
        if (activeSaveGame != null) {
            if (!invokedWritingToDiskEvent) {
                WritingToDiskBegin.Invoke(activeSlot);
                invokedWritingToDiskEvent = true;
            }
            if (syncActiveSaveables)
                SyncSave();
            SaveFileUtility.WriteSave(activeSaveGame, activeSlot);
            var culture = SaveSettings.Instance.UseInvariantCulture ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
            SetMetaDataMultiple(metaData => {
                metaData.SetData("timeplayed", activeSaveGame.timePlayed.ToString());
                metaData.SetData("gameversion", activeSaveGame.gameVersion.ToString());
                metaData.SetData("creationdate", activeSaveGame.creationDate.ToString(culture));
                metaData.SetData("lastsavedtime", DateTime.Now.ToString(culture));
                metaData.SetData("storageconfig", System.Text.Json.JsonSerializer.Serialize(SaveSettings.Instance.StorageConfig));
            }, activeSlot);
            WritingToDiskDone.Invoke(activeSlot);
            invokedWritingToDiskEvent = false;
        }
        else {
            var settings = SaveSettings.Instance;
            if (Engine.GetProcessFrames() != 0 && !(settings.SlotLoadBehaviour != SlotLoadBehaviour.DontLoadSlot && autoSaveOnExit))
                GDS.Log("Attempted to save with no save game loaded.");
        }
    }

    /// <summary> If wipeSaveables is false, saveables can save again. </summary>
    public static void WipeStageData(string stageName, bool wipeSaveables = true) {
        if (activeSaveGame == null) {
            GDS.Log("Failed to wipe stage data. No save game is loaded.");
            return;
        }
        if (wipeSaveables) {
            List<Saver> wipedSaveables = new();
            foreach (var saveable in savers) {
                if (saveable.GetStage().Name == stageName) {
                    saveable.WipeData(activeSaveGame, false);
                    wipedSaveables.Add(saveable);
                }
            }
            for (int i = 0; i < wipedSaveables.Count; i++) {
                wipedSaveables[i].manualSaveLoad = true;
                RemoveListener(wipedSaveables[i], false);
            }
            Node getScene = StageManager.GetLoadedStage(stageName);
            if (getScene != null) {
                if (savedSceneManagers.TryGetValue(getScene.GetHashCode(), out SavedSceneManager instanceManager))
                    instanceManager.ClearData();
            }
        }
        activeSaveGame.WipeStageData(stageName);
    }

    public static void WipeSaveable(Saver saver, bool stopSaving = true) {
        if (activeSaveGame == null)
            return;
        saver.WipeData(activeSaveGame, stopSaving);
    }

    /// <summary> Clears all saveable components that are listening to the Save Master </summary>
    public static void RemoveAllListeners(bool syncSave) {
        if (syncSave && activeSaveGame != null) {
            foreach (var saveable in savers)
                saveable.OnSaveRequest(activeSaveGame);
        }
        savers.Clear();
    }

    public static void RemoveListeners(bool syncSave, Node stage) {
        int saveableCount = savers.Count;
        if (saveableCount == 0)
            return;
        List<Saver> markedForRemoval = new();
        bool shouldSave = syncSave && activeSaveGame != null;
        foreach (var saveable in savers) {
            if (saveable.Stage != stage)
                continue;
            if (shouldSave)
                saveable.OnSaveRequest(activeSaveGame);
            markedForRemoval.Add(saveable);
        }
        foreach (var saveable in markedForRemoval)
            savers.Remove(saveable);
    }

    /// <summary> Manual function for saving a saver. Use if you have a saver set to manual saving. </summary>
    public static void SaveListener(Saver saver) {
        if (saver != null && activeSaveGame != null)
            saver.OnSaveRequest(activeSaveGame);
    }

    /// <summary> Manual function for loading a saver. Use if you have a saver set to manual loading </summary>
    public static void LoadListener(Saver saver) {
        if (saver != null && activeSaveGame != null)
            saver.OnLoadRequest(activeSaveGame);
    }

    /// <summary> Use if ISaveable components have been added to a saver at runtime. </summary>
    public static void ReloadListener(Saver saver) {
        if (saver != null && activeSaveGame != null) {
            saver.ResetState();
            saver.OnLoadRequest(activeSaveGame);
        }
    }

    /// <summary> Add saver from the notification list. It will recieve load/save requests. </summary>
    public static void AddListener(Saver saver) {
        if (saver != null && activeSaveGame != null)
            saver.OnLoadRequest(activeSaveGame);
        savers.Add(saver);
    }

    /// <summary> Add saver from the notification list. It will recieve load/save requests. </summary>
    public static void AddListener(Saver saver, bool loadData) {
        if (loadData)
            AddListener(saver);
        else
            savers.Add(saver);
    }

    /// <summary> Remove saver from the notification list. It no longers recieves load/save requests. </summary>
    public static void RemoveListener(Saver saver) {
        if (savers.Remove(saver)) {
            if (StageManager.IsQuittingGame && !autoSaveOnExit)
                return;
            if (saver != null && activeSaveGame != null)
                saver.OnSaveRequest(activeSaveGame);
        }
        if (!StageManager.IsQuittingGame || !autoSaveOnExit)
            return;
        if (savers.Count == 0 && activeSaveGame != null) {
            WriteActiveSaveToDisk();
            activeSaveGame.Dispose();
            activeSaveGame = null;
        }
    }

    /// <summary> Remove saver from the notification list. So it no longers recieves load/save requests. </summary>
    public static void RemoveListener(Saver saver, bool saveData) {
        if (saveData)
            RemoveListener(saver);
        else
            savers.Remove(saver);
    }

    public static async void ClearActiveSaveData(bool removeListeners = true, bool reloadActiveStages = false) {
        if (activeSlot != -1) {
            int slot = activeSlot;
            DeleteSave(activeSlot);
            if (removeListeners || reloadActiveStages)
                ClearSlot();
            SetSlot(slot, false);
            if (reloadActiveStages) {
                List<Node> loadedStages = StageManager.LoadedStages;
                List<string> loadedStageNames = new();
                for (int i = 0; i < loadedStages.Count; i++)
                    loadedStageNames.Add(loadedStages[i].Name);
                await StageManager.UnloadAllStages();
                for (int i = 0; i < loadedStageNames.Count; i++)
                    StageManager.LoadStage(loadedStageNames[i]);
            }
        }
    }

    public static void DeleteAllSaves() {
        activeSlot = -1;
        activeSaveGame.Dispose();
        activeSaveGame = null;
        if (SaveSettings.Instance.SlotLoadBehaviour == SlotLoadBehaviour.LoadTemporarySlot)
            SetSlotToTemporary(true, false);
        SaveFileUtility.DeleteAllSaveFiles();
        PreferenceManager.DeletePreference("SM-LastUsedSlot");
        DeletedAllSaves.Invoke();
    }

    public static void DeleteSave(int slot) {
        if (slot == activeSlot) {
            activeSlot = -1;
            activeSaveGame.Dispose();
            activeSaveGame = null;
            if (SaveSettings.Instance.SlotLoadBehaviour == SlotLoadBehaviour.LoadTemporarySlot)
                SetSlotToTemporary(true, false);
        }
        SaveFileUtility.DeleteSave(slot);
        DeletedSave.Invoke(slot);
    }

    public static void DeleteSave() {
        DeleteSave(activeSlot);
    }

    public static void SyncSave() {
        SyncSaveBegin.Invoke(activeSlot);
        if (activeSaveGame == null) {
            GDS.LogErr("Sync save failed. No active SaveGame exists.");
            return;
        }
        foreach (var saver in savers)
            saver.OnSaveRequest(activeSaveGame);
        foreach (var item in savedSceneManagers)
            item.Value.OnSave(activeSaveGame);
        SyncSaveDone.Invoke(activeSlot);
    }

    public static void SyncLoad() {
        SyncLoadBegin.Invoke(activeSlot);
        if (activeSaveGame == null) {
            GDS.LogErr("Sync load failed. No active SaveGame exists.");
            return;
        }
        foreach (var saver in savers)
            saver.OnLoadRequest(activeSaveGame);
        foreach (var item in savedSceneManagers)
            item.Value.OnLoad(activeSaveGame);
        SyncLoadDone.Invoke(activeSlot);
    }

    /// <summary> Resets the state of the savers as if they have never loaded or saved. </summary>
    public static void SyncReset() {
        if (activeSaveGame == null) {
            GDS.LogErr("Request Load Failed. No active SaveGame has been set. Be sure to call SetSlot(index)");
            return;
        }
        foreach (var saver in savers)
            saver.ResetState();
    }

    public static Node SpawnSavedScene(PackedScene packedScene, bool addToTree = true, Node stage = null, string tag = "") {
        if (packedScene == null) {
            GDS.LogErr("Failed to spawn saved scene. PackedScene is null.");
            return null;
        }
        stage ??= StageManager.ActiveStage;
        SavedSceneManager savedSceneManager;
        if (!savedSceneManagers.TryGetValue(stage.GetHashCode(), out savedSceneManager))
            savedSceneManager = SpawnSavedSceneManager(stage);
        Node instance = savedSceneManager.SpawnSavedScene(packedScene, addToTree, tag, SavedSceneManager.SpawnSource.FromUser, default);
        if (instance == null)
            return null;
        return instance;
    }

    public static Node SpawnSavedScene(string filePath, bool addToTree = true, Node stage = default, string tag = "") => SpawnSavedScene(GD.Load<PackedScene>(filePath), addToTree, stage, tag);

    private static string GetSaveFileName(int slot, string fileName) {
        if ((slot == -1 && string.IsNullOrEmpty(fileName)) || !IsSlotUsed(slot))
            return activeSaveGame != null ? activeSaveGame.fileName : "";
        if (slot != -1) {
            string slotFileName = SaveFileUtility.ObtainSlotFileName(slot);
            if (!string.IsNullOrEmpty(slotFileName))
                return slotFileName;
            else
                return "";
        }
        else if (!string.IsNullOrEmpty(fileName)) {
            if (SaveFileUtility.IsSaveFileNameUsed(fileName))
                return fileName;
            else
                return "";
        }
        return "";
    }

    public static bool GetMetaData(string id, Image tex, int slot = -1, string fileName = "") {
        string saveFileName = GetSaveFileName(slot, fileName);
        if (string.IsNullOrEmpty(saveFileName))
            return false;
        return MetaDataFileUtility.GetMetaData(saveFileName).GetData(id, tex);
    }

    public static bool GetMetaData(string id, out byte[] data, int slot = -1, string fileName = "") {
        string saveFileName = GetSaveFileName(slot, fileName);
        if (string.IsNullOrEmpty(saveFileName)) {
            data = null;
            return false;
        }
        return MetaDataFileUtility.GetMetaData(saveFileName).GetData(id, out data);
    }

    public static bool GetMetaData(string id, out string data, int slot = -1, string fileName = "") {
        string saveFileName = GetSaveFileName(slot, fileName);
        if (string.IsNullOrEmpty(saveFileName)) {
            data = null;
            return false;
        }
        return MetaDataFileUtility.GetMetaData(saveFileName).GetData(id, out data);
    }

    public static void SetMetaData(string id, string data, int slot = -1, string fileName = "") {
        string saveFileName = GetSaveFileName(slot, fileName);
        if (string.IsNullOrEmpty(saveFileName))
            return;
        using var metaData = MetaDataFileUtility.GetMetaData(saveFileName);
        metaData.SetData(id, data);
    }

    public static void SetMetaData(string id, Image data, int slot = -1, string fileName = "") {
        string saveFileName = GetSaveFileName(slot, fileName);
        if (string.IsNullOrEmpty(saveFileName))
            return;
        using var metaData = MetaDataFileUtility.GetMetaData(saveFileName);
        metaData.SetData(id, data);
    }

    public static void SetMetaData(string id, byte[] data, int slot = -1, string fileName = "") {
        string saveFileName = GetSaveFileName(slot, fileName);
        if (string.IsNullOrEmpty(saveFileName))
            return;
        using var metaData = MetaDataFileUtility.GetMetaData(saveFileName);
        metaData.SetData(id, data);
    }

    public static void SetMetaDataMultiple(Action<MetaDataFileUtility.MetaData> metaData, int slot = -1, string fileName = "") {
        string saveFileName = GetSaveFileName(slot, fileName);
        if (string.IsNullOrEmpty(saveFileName))
            return;
        using var m = MetaDataFileUtility.GetMetaData(saveFileName);
        metaData(m);
    }

    public static bool GetSaveableData<T>(int slot, string saveableId, string componentId, out T data) {
        if (IsSlotUsed(slot) == false) {
            data = default;
            return false;
        }
        SaveGame saveGame = GetSave(slot, false);
        if (saveGame == null) {
            data = default;
            return false;
        }
        string dataString = saveGame.Get(string.Format("{0}-{1}", saveableId, componentId));
        if (!string.IsNullOrEmpty(dataString)) {
            data = GDS.Deserialize<T>(dataString);
            if (data != null)
                return true;
        }
        data = default;
        return false;
    }

    public static bool GetSaveableData<T>(string saveableId, string componentId, out T data) {
        if (activeSlot == -1) {
            data = default;
            return false;
        }
        return GetSaveableData<T>(activeSlot, saveableId, componentId, out data);
    }

    public static void SetInt(string key, int value) {
        if (HasActiveSaveLogAction("Set Int") == false)
            return;
        activeSaveGame.Set(string.Format("IVar-{0}", key), value.ToString(), "Global");
    }

    public static int GetInt(string key, int defaultValue = -1) {
        if (HasActiveSaveLogAction("Get Int") == false)
            return defaultValue;
        var getData = activeSaveGame.Get(string.Format("IVar-{0}", key));
        return string.IsNullOrEmpty(getData) ? defaultValue : int.Parse(getData);
    }

    public static void SetFloat(string key, float value) {
        if (HasActiveSaveLogAction("Set Float") == false)
            return;
        activeSaveGame.Set(string.Format("FVar-{0}", key), value.ToString(), "Global");
    }

    public static float GetFloat(string key, float defaultValue = -1) {
        if (HasActiveSaveLogAction("Get Float") == false)
            return defaultValue;
        var getData = activeSaveGame.Get(string.Format("FVar-{0}", key));
        return string.IsNullOrEmpty(getData) ? defaultValue : float.Parse(getData);
    }

    public static void SetBool(string key, bool value) {
        if (HasActiveSaveLogAction("Set Bool") == false)
            return;
        activeSaveGame.Set(string.Format("BVar-{0}", key), value.ToString(), "Global");
    }

    public static bool GetBool(string key, bool defaultValue = false) {
        if (HasActiveSaveLogAction("Get Bool") == false)
            return defaultValue;
        var getData = activeSaveGame.Get(string.Format("BVar-{0}", key));
        return string.IsNullOrEmpty(getData) ? defaultValue : bool.Parse(getData);
    }

    public static void SetString(string key, string value) {
        if (HasActiveSaveLogAction("Set String") == false)
            return;
        activeSaveGame.Set(string.Format("SVar-{0}", key), value, "Global");
    }

    public static string GetString(string key, string defaultValue = "") {
        if (HasActiveSaveLogAction("Get String") == false)
            return defaultValue;
        var getData = activeSaveGame.Get(string.Format("SVar-{0}", key));
        return string.IsNullOrEmpty((getData)) ? defaultValue : getData;
    }

    private static bool HasActiveSaveLogAction(string action) {
        if (GetActiveSlot() == -1) {
            GDS.LogErr($"{action} Failed. No save slot set.");
            return false;
        }
        else return true;
    }

    private static void RemoveAndWipeActiveSavedStages() {
        List<Node> totalLoadedStages = StageManager.TotalStages;
        foreach (Node stage in totalLoadedStages) {
            SavedSceneManager saveIM;
            if (savedSceneManagers.TryGetValue(stage.GetHashCode(), out saveIM))
                saveIM.RemoveAndWipeAllSavedScenes();
        }
    }

    public static string GetActiveSaveFilePath() {
        if (activeSaveGame == null || activeSlot == -1) {
            GDS.Log("Failed to obtain save game path. Invalid slot or no savegame loaded.");
            return "";
        }
        return SaveFileUtility.GetSaveFilePath(activeSaveGame.fileName);
    }

    private async void AutoSaveGame() {
        CancellationToken token = cts.Token;
        while (true) {
            await Task.Delay(TimeSpan.FromSeconds(SaveSettings.Instance.SaveIntervalSeconds), token);
            if (token.IsCancellationRequested)
                return;
            WriteActiveSaveToDisk();
        }
    }

    private async void IncrementTimePlayed() {
        CancellationToken token = cts.Token;
        while (true) {
            if (activeSlot != -1 && activeSaveGame != null)
                activeSaveGame.timePlayed = activeSaveGame.timePlayed.Add(TimeSpan.FromSeconds(1));
            await Task.Delay(TimeSpan.FromSeconds(1), token);
            if (token.IsCancellationRequested)
                return;
        }
    }

    private void OnApplicationPause(bool state) {
        if (state && autoSaveOnExit)
            WriteActiveSaveToDisk();
    }

    private void OnApplicationQuit() {
        if (autoSaveOnExit) {
            if (savers.Count == 0) {
                WriteActiveSaveToDisk();
                activeSaveGame.Dispose();
                activeSaveGame = null;
            }
            else if (!invokedWritingToDiskEvent) {
                WritingToDiskBegin.Invoke(activeSlot);
                invokedWritingToDiskEvent = true;
            }
        }
    }
}
