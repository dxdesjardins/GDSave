using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;
using Chomp.Save.Internal;
using System.Linq;

namespace Chomp.Save.Components;

// saveIdentification = saverId-saveableId
// saverId = stageFileName-sceneFileName-guid
// saveableId = typeName-guid

[Tool]
public partial class Saver : Node
{
    [ExportGroup("Configuration")]
    [Export] private bool loadOnce;
    [Export] public bool manualSaveLoad;
    [Export] private bool loadAfterParentReady = true;

    [ExportGroup("Initialization")]
    [Export] public bool SaveWhenRemoved { get; private set; }
    [Export] private bool removeAfterInitialized;

    [ExportGroup("Identification")]
    [Export] private StringVariable saverId = new() { ResourceLocalToScene = true };

    [ExportGroup("Components")]
    [Export] private Godot.Collections.Array<Node> externalSaveables = new();
    [Export] private Godot.Collections.Array<Node> excludedSaveables = new();
    [Export] public Godot.Collections.Array<CachedSaveableData> CachedSaveableData { get; private set; } = new();

    private HashSet<ISaveable> loadedSaveables = new();
    private static Dictionary<string, Saver> cachedSavers = new();
    private List<string> saveIdentifications = new();
    private List<ISaveable> saveables = new();

    private string stageFileName;
    private string sceneFileName;
    private ulong removeFrame;
    private Node stage;
    public Node Stage => stage ??= this.GetStage();
    public bool HasLoaded { get; private set; }
    public bool HasStateReset { get; private set; }
    public bool HasSaverId => !string.IsNullOrEmpty(saverId.Value);
    internal bool HasLoadedAnyComponents { get; private set; }
    internal bool HasSavedAnyComponents { get; private set; }

    public string SaverId {
        get { return saverId.Value; }
        set {
            bool wasEmpty = string.IsNullOrEmpty(saverId.Value);
            saverId.Value = value;
            if (wasEmpty && HasSaverId)
                SaveManager.ReloadListener(this);
        }
    }

    public override void _Notification(int what) {
        if (Engine.IsEditorHint()) {
            if (this.IsInternal()) {
                GDS.Log("Warning: Saver failed to cache data due to being an internal node.");
                return;
            }
            switch (what) {
                case (int)NotificationEditorPreSave:
                    Initialize();
                    UpdateSaverId();
                    UpdateCachedSaveableData();
                    break;
                case (int)NotificationExitTree:
                    cachedSavers.Remove(saverId.Value);
                    break;
            }
            return;
        }
        switch (what) {
            case (int)NotificationReady:
                if (loadAfterParentReady && !this.GetParent().IsNodeReady())
                    this.GetParent().Ready += PerformRuntimeSetup;
                else
                    PerformRuntimeSetup();
                break;
        }
    }

    private void Initialize() {
        string stagePath = Stage?.SceneFilePath;
        if (!string.IsNullOrEmpty(stagePath))
            stageFileName = System.IO.Path.GetFileNameWithoutExtension(stagePath);
        if (!string.IsNullOrEmpty(this.SceneFilePath))
            sceneFileName = System.IO.Path.GetFileNameWithoutExtension(this.SceneFilePath);
        else if (!string.IsNullOrEmpty(sceneFileName))
            sceneFileName = stageFileName;
        else
            sceneFileName = System.IO.Path.GetFileNameWithoutExtension(this.GetScene().SceneFilePath);
    }

    private void PerformRuntimeSetup() {
        Initialize();
        this.GetParent().TreeExited += () => {
            removeFrame = Engine.GetProcessFrames();
            if (!manualSaveLoad)
                SaveManager.RemoveListener(this);
        };
        // Store the component identifiers into a dictionary for performant retrieval.
        for (int i = 0; i < CachedSaveableData.Count; i++) {
            saveIdentifications.Add(string.Format("{0}-{1}", saverId.Value, CachedSaveableData[i].saveableId.Value));
            saveables.Add(this.GetNode(CachedSaveableData[i].nodePath) as ISaveable);
        }
        if (removeAfterInitialized)
            this.RemoveFirstParent();
        if (!manualSaveLoad)
            SaveManager.AddListener(this);
    }

    private void UpdateSaverId() {
        if (!string.IsNullOrEmpty(stageFileName)) {
            saverId ??= new StringVariable() { ResourceLocalToScene = true };
            // If ident belongs to wrong stage, erase it
            if (SaveSettings.Instance.ResetSaverIdOnNewStage && !saverId.Value.StartsWith(stageFileName))
                saverId.Value = "";
            // If ident is a duplicate, erase it
            if (SaveSettings.Instance.ResetSaverIdOnDuplicate && !string.IsNullOrEmpty(saverId.Value)) {
                if (!string.IsNullOrEmpty(saverId.Value)) {
                    bool isDuplicate = cachedSavers.TryGetValue(saverId.Value, out Saver saveable);
                    if (!isDuplicate && saverId.Value != "")
                        cachedSavers.Add(saverId.Value, this);
                    else {
                        if (saveable == null) {
                            cachedSavers.Remove(saverId.Value);
                            cachedSavers.Add(saverId.Value, this);
                        }
                        else if (saveable != this) {
                            GDE.Log("Duplicate Saver ID reset");
                            saverId.Value = "";
                        }
                    }
                }
            }
            // If ident is empty, create a new one
            if (string.IsNullOrEmpty(saverId.Value)) {
                int guidLength = SaveSettings.Instance.SaverGuidLength;
                saverId.Value = string.Format("{0}-{1}-{2}", stageFileName, sceneFileName, System.Guid.NewGuid().ToString().Substring(0, guidLength));
                cachedSavers.Add(saverId.Value, this);
            }
        }
        else
            saverId.Value = "";
    }

    private void UpdateCachedSaveableData() {
        // Get all saveable components that should be cached
        var obtainSaveables = this.GetComponentsInChildren<ISaveable>(this.IsParent()).ToList();
        obtainSaveables.AddRange(externalSaveables
            .Where(listener => listener != null)
            .SelectMany(listener => listener.GetComponentsInChildren<ISaveable>()));
        // Remove excluded saveables
        obtainSaveables = obtainSaveables
            .Where(saveable => !excludedSaveables.Contains(saveable as Node))
            .ToList();
        // Create dictionary of SaveableId mapped to ISaveable Node. Assign SaveableId if needed.
        var saveablesToBeCached = obtainSaveables.ToDictionary(
                saveable => string.IsNullOrEmpty(saveable.SaveableId) ? CreateSaveableId(saveable) : saveable.SaveableId,
                saveable => saveable
            );
        // Update cached nodePaths and remove cachedSaveables that are no longer in the scene
        for (int i = CachedSaveableData.Count - 1; i >= 0; i--) {
            var saveableId = CachedSaveableData[i].saveableId.Value;
            if (saveablesToBeCached.TryGetValue(saveableId, out ISaveable value)) {
                CachedSaveableData[i].nodePath = GetPathTo(value as Node);
                saveablesToBeCached.Remove(saveableId);
            }
            else {
                GDS.Log($"Removed invalid cached saveable data: {saveableId}");
                CachedSaveableData.RemoveAt(i);
            }
        }
        // TODO: This is needed due to an engine bug where added saveables may not have the cache saved unless the entire array points to a new location.
        // From my experience, this caused in instances where the Saver node is part of an inherited scene. I could not find any issue reports on this,
        // but I don't plan to submit one because I know there are large overhauls planned for marking inheriting scene components as serializable.
        if (saveablesToBeCached.Count > 0)
            CachedSaveableData = CachedSaveableData.Duplicate();
        // Create and add missing cachedSaveables
        foreach (var saveableInstance in saveablesToBeCached) {
            var newSaveableData = new CachedSaveableData {
                nodePath = GetPathTo(saveableInstance.Value as Node),
                saveableId = { Value = saveableInstance.Key }
            };
            CachedSaveableData.Add(newSaveableData);
            //newSaveableData.ResourceLocalToScene = true;
            GDS.Log($"Added cached saveable data: {newSaveableData.saveableId.Value}");
        }
    }

    public static bool IsSaveableLoaded(ISaveable saveable) {
        for (int i = 0; i < cachedSavers.Count; i++) {
            if (cachedSavers.Values.ElementAt(i).loadedSaveables.Contains(saveable))
                return true;
        }
        return false;
    }

    public string CreateSaveableId(ISaveable saveable) {
        string typeName = (saveable as Node).GetType().Name;
        string saveableId;
        do {
            int guidLength = SaveSettings.Instance.SaveableGuidLength;
            saveableId = string.Format("{0}-{1}", typeName, System.Guid.NewGuid().ToString().Substring(0, guidLength));
        }
        while (!IsSaveableIdUnique(saveableId));
        saveable.SaveableId = saveableId;
        return saveableId;
    }

    /// <summary> Called by SavedSceneManager if SavedScene is being instantiated at runtime and was missing a Saver component. </summary>
    public void GetAndAddLocalSaveables() {
        ISaveable[] saveables = this.GetComponentsInChildren<ISaveable>(this.IsParent());
        for (int i = 0; i < saveables.Length; i++) {
            Node node = saveables[i] as Node;
            string saveableId = string.Format("Dyn-{0}-{1}", node.GetType().Name, i);
            AddSaveable(saveableId, saveables[i]);
            CachedSaveableData.Add(new CachedSaveableData() {
                saveableId = new StringVariable() { Value = saveableId },
            });
        }
        SaveManager.ReloadListener(this);
    }

    private bool IsSaveableIdUnique(string identifier) {
        if (string.IsNullOrEmpty(identifier))
            return false;
        return CachedSaveableData != null && !CachedSaveableData.Any(data => data.saveableId.Value == identifier);
    }

    public void AddSaveable(string saveableId, ISaveable saveable, bool reloadAllComponents = false) {
        saveIdentifications.Add(string.Format("{0}-{1}", saverId.Value, saveableId));
        saveables.Add(saveable);
        if (reloadAllComponents)
            SaveManager.ReloadListener(this);
    }

    public List<string> GetSaveIdentifications() {
        if (!Engine.IsEditorHint())
            return saveIdentifications;
        return CachedSaveableData
            .Select(data => $"{saverId.Value}-{data.saveableId.Value}")
            .ToList();
    }

    public void WipeData(SaveGame saveGame, bool stopSaving = true) {
        for (int i = saveIdentifications.Count - 1; i >= 0; i--)
            saveGame.Remove(saveIdentifications[i]);
        if (stopSaving) {
            manualSaveLoad = true;
            SaveManager.RemoveListener(this, false);
        }
    }

    public void ResetState() {
        HasLoaded = false;
        HasStateReset = true;
    }

    public void OnSaveRequest(SaveGame saveGame) {
        HasSavedAnyComponents = false;
        if (!HasSaverId || (!SaveWhenRemoved && !this.IsInsideTree() && removeFrame != Engine.GetProcessFrames()))
            return;
        for (int i = saveIdentifications.Count - 1; i >= 0; i--) {
            ISaveable saveable = saveables[i];
            string saveIdentification = saveIdentifications[i];
            if (saveable is not Node) {
                GDE.Log($"Failed to save saveable({saveIdentification}). Node is potentially destroyed.");
                saveIdentifications.RemoveAt(i);
                saveables.RemoveAt(i);
            }
            else {
                if (!HasStateReset && !saveable.OnSaveCondition())
                    continue;
                string saveData = saveable.OnSave();
                if (!string.IsNullOrEmpty(saveData)) {
                    saveGame.Set(saveIdentification, saveData, stageFileName);
                    HasSavedAnyComponents = true;
                }
            }
        }
        HasStateReset = false;
    }

    public void OnLoadRequest(SaveGame saveGame) {
        HasLoadedAnyComponents = false;
        if ((loadOnce && HasLoaded) || !HasSaverId)
            return;
        HasLoaded = true;
        for (int i = saveIdentifications.Count - 1; i >= 0; i--) {
            ISaveable saveable = saveables[i];
            string saveIdentification = saveIdentifications[i];
            if (saveable == null) {
                GDE.Log($"Failed to load saveable {saveIdentification}. Node is potentially destroyed.");
                saveIdentifications.RemoveAt(i);
                saveables.RemoveAt(i);
            }
            else {
                string loadData = saveGame.Get(saveIdentifications[i]);
                if (!string.IsNullOrEmpty(loadData)) {
                    saveable.OnLoad(loadData);
                    HasLoadedAnyComponents = true;
                    loadedSaveables.Add(saveable);
                }
            }
        }
    }

    /// <summary> Not intended to be used if Saveable was spawned from SaveMaster </summary>
    public Dictionary<string, string> ManualGetSaveData() {
        Dictionary<string, string> saveData = new();
        for (int i = 0; i < saveIdentifications.Count; i++)
            saveData.Add(saveIdentifications[i], saveables[i].OnSave());
        return saveData;
    }

    /// <summary> Not intended to be used if Saveable was spawned from SaveMaster </summary>
    public void ManualLoadSaveData(Dictionary<string, string> saveData) {
        for (int i = 0; i < saveIdentifications.Count; i++) {
            if (saveData.TryGetValue(saveIdentifications[i], out string loadData))
                saveables[i].OnLoad(loadData);
        }
    }
}
