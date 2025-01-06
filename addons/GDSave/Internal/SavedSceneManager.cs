using Godot;
using System;
using System.Collections.Generic;
using Chomp.Save.Components;
using Chomp.Essentials;
using System.Text.Json.Serialization;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Chomp.Save.Internal;

public partial class SavedSceneManager : Node
{
    private Dictionary<SavedScene, SpawnInfo> spawnInfo = new();
    private HashSet<string> loadedSaverIds = new();
    private static Dictionary<long, CachedSceneData> cachedSavedSceneInfo = new();
    private int spawnedSceneCount;
    private int changesMade;
    private List<SpawnInfo> throttledSpawnList = new();
    private bool spawnThrottled;
    private CancellationTokenSource cts = new();

    // Set by the SaveManager
    public string StageId { get; internal set; }
    public string SaveIdentification { get; internal set; }
    public Node Stage { get; internal set; }

    private SaveData saveData = new() {
        InfoCollection = new List<SpawnInfo>(),
        SpawnedSceneCount = 0
    };

    public class SaveData
    {
        public List<SpawnInfo> InfoCollection { get; set; }
        public int SpawnedSceneCount { get; set; }
    }

    public class CachedSceneData
    {
        public bool valid;
        public PackedScene packedScene;
        public string[] saveableIds;
        public Node instance;
    }

    public struct SpawnInfo
    {
        public string Uid { get; set; }
        public string SaverId { get; set; }
        public string Tag { get; set; }
        [JsonIgnore] public SpawnSource spawnSource;

        public bool IsValidData() {
            bool uidValid = GDE.IsUidValid(Uid);
            bool saveIdEmpty = string.IsNullOrEmpty(SaverId);
            return uidValid && !saveIdEmpty;
        }
    }

    public enum SpawnSource
    {
        FromSave,
        FromUser,
    }

    public void RemoveAndWipeAllSavedScenes() {
        List<SavedScene> scenes = new();
        foreach (var item in spawnInfo) {
            if (item.Key != null)
                scenes.Add(item.Key);
        }
        int instanceCount = scenes.Count;
        for (int i = 0; i < instanceCount; i++)
            scenes[i].RemoveAndWipeScene();
        spawnInfo.Clear();
        loadedSaverIds.Clear();
        spawnedSceneCount = 0;
        changesMade++;
    }

    public void ClearData() {
        spawnInfo.Clear();
        loadedSaverIds.Clear();
        spawnedSceneCount = 0;
        changesMade++;
    }

    public void WipeCachedSceneData(SavedScene savedScene, Saver saveable) {
        if (spawnInfo.ContainsKey(savedScene)) {
            spawnInfo.Remove(savedScene);
            loadedSaverIds.Remove(saveable.SaverId);
            changesMade++;
        }
    }

    public Node SpawnSavedScene(PackedScene packedScene, bool addToTree = true, string tag = "", SpawnSource spawnSource = SpawnSource.FromUser, string saverId = "") {
        long uid = packedScene.GetUid();
        var sceneData = GetSceneData(uid);
        if (!sceneData.valid)
            return null;
        changesMade++;
        Node instance;
        if (sceneData.instance != null) {
            instance = sceneData.instance;
            sceneData.instance = null;
        }
        else {
            instance = packedScene.Instantiate<Node>();
            instance.SetUniqueName();
        }
        Saver saver = instance.GetComponent<Saver>();
        if (saver == null) {
            GDS.LogErr("No saver added to spawned object. Scanning for saveables during runtime is inefficient.");
            Node saveNode = instance.InstantiateChild<Saver>();
            instance.MoveChild(saveNode, 0);
            saver.GetAndAddLocalSaveables();
        }
        SavedScene savedScene = instance.InstantiateChild<SavedScene>();
        SpawnInfo info = new() {
            Uid = GDE.UidToString(uid),
            SaverId = saver.SaverId,
            Tag = tag,
            spawnSource = spawnSource
        };
        if (string.IsNullOrEmpty(saverId))
            saver.SaverId = string.Format("{0}-{1}-{2}", StageId, System.IO.Path.GetFileNameWithoutExtension(packedScene.ResourcePath), spawnedSceneCount);
        else
            saver.SaverId = saverId;
        info.SaverId = saver.SaverId;
        loadedSaverIds.Add(saver.SaverId);
        spawnedSceneCount++;
        spawnInfo.Add(savedScene, info);
        savedScene.Configure(saver, this, info);
        if (addToTree)
            this.Stage.AddChild(instance);
        SaveManager.SpawnedSavedScene.Invoke(packedScene, instance);
        return instance;
    }

    public void OnSave(SaveGame saveGame) {
        SaveSettings settings = SaveSettings.Instance;
        if (changesMade > 0) {
            changesMade = 0;
            int spawnedInstances = spawnInfo.Count;
            if (spawnedInstances == 0) {
                saveGame.Remove(SaveIdentification);
                return;
            }
            saveData.InfoCollection.Clear();
            saveData.SpawnedSceneCount = this.spawnedSceneCount;
            SaveData data = new() {
                InfoCollection = new List<SpawnInfo>(),
                SpawnedSceneCount = this.spawnedSceneCount
            };
            foreach (var item in spawnInfo) {
                SavedScene scene = item.Key;
                Saver saver = scene.Saveable;
                if (scene.DontSaveScene) {
                    SaveManager.WipeSaveable(saver);
                    continue;
                }
                if (settings.IgnoreDatalessSavedScenes) {
                    if (!saver.HasLoadedAnyComponents && !saver.HasSavedAnyComponents)
                        continue;
                }
                data.InfoCollection.Add(item.Value);
            }
            GDS.Log($"Saved Scenes: {data.InfoCollection.Count}");
            string json = GDS.Serialize(data);
            saveGame.Set(SaveIdentification, json, StageId);
        }
    }

    public void OnLoad(SaveGame saveGame) {
        SaveSettings settings = SaveSettings.Instance;
        SaveData saveData = saveGame?.Get(SaveIdentification) != null ? GDS.Deserialize<SaveData>(saveGame.Get(SaveIdentification)) : null;
        spawnThrottled = CanSpawnThrottled();
        if (saveData != null && saveData.InfoCollection != null) {
            spawnedSceneCount = saveData.SpawnedSceneCount;
            int itemCount = saveData.InfoCollection.Count;
            for (int i = 0; i < itemCount; i++) {
                SpawnInfo savedSpawnInfo = saveData.InfoCollection[i];
                if (!saveData.InfoCollection[i].IsValidData()) {
                    changesMade++;
                    continue;
                }
                if (loadedSaverIds.Contains(savedSpawnInfo.SaverId))
                    return;
                var sceneData = GetSceneData(GDE.UidToLong(savedSpawnInfo.Uid));
                if (!sceneData.valid) {
                    GDS.LogErr($"Unable to spawn saved scene because uid({savedSpawnInfo.Uid}) is invalid.");
                    changesMade++;
                    continue;
                }
                if (settings.IgnoreDatalessSavedScenes) {
                    bool hasExistingData = false;
                    int componentCount = sceneData.saveableIds.Length;
                    for (int j = 0; j < componentCount; j++) {
                        string dataString = saveGame.Get(string.Format("{0}-{1}", savedSpawnInfo.SaverId, sceneData.saveableIds[j]));
                        if (!string.IsNullOrEmpty(dataString)) {
                            hasExistingData = true;
                            break;
                        }
                    }
                    if (!hasExistingData) {
                        changesMade++;
                        continue;
                    }
                }
                if (spawnThrottled) {
                    throttledSpawnList.Add(new SpawnInfo() {
                        SaverId = savedSpawnInfo.SaverId,
                        Uid = savedSpawnInfo.Uid,
                        Tag = savedSpawnInfo.Tag,
                        spawnSource = SpawnSource.FromSave
                    });
                }
                else
                    SpawnSavedScene(sceneData.packedScene, true, savedSpawnInfo.Tag, SpawnSource.FromSave, savedSpawnInfo.SaverId);
            }
            if (spawnThrottled) {
                cts = cts.StopAllCoroutines();
                _ = SpawnSavedScenesThrottled();
            }
        }
    }

    public bool CanSpawnThrottled() {
        var settings = SaveSettings.Instance;
        if (!settings.SpawnThrottled)
            return false;
        if (!settings.OnlyThrottleSpecificStages)
            return true;
        for (int i = 0; i < settings.ThrottledStages.Length; i++) {
            if (settings.ThrottledStages[i].ResourcePath == Stage.SceneFilePath)
                return true;
        }
        return false;
    }

    private async Task SpawnSavedScenesThrottled() {
        CancellationToken token = cts.Token;
        var settings = SaveSettings.Instance;
        int targetSpawnFramerate = settings.ThrottleTargetFramerate;
        bool addToTree = !settings.DontAddScenesToTreeUntilDone;
        int spawnTotal = throttledSpawnList.Count;
        int spawnCount = 0;
        Stopwatch sw = new();
        List<Node> removedObjects = new();
        SaveManager.StartedSpawningThrottledSavedScenes.Invoke();
        for (int i = spawnTotal - 1; i >= 0; i--) {
            SpawnInfo spawnInfo = throttledSpawnList[i];
            if (!sw.IsRunning)
                sw.Start();
            PackedScene packedScene = GDE.UidToResource<PackedScene>(spawnInfo.Uid);
            Node savedScene = SpawnSavedScene(packedScene, addToTree, spawnInfo.Tag, spawnInfo.spawnSource, spawnInfo.SaverId);
            if (!addToTree)
                removedObjects.Add(savedScene);
            throttledSpawnList.RemoveAt(i);
            spawnCount++;
            SaveManager.SpawnedSavedScene.Invoke(packedScene, savedScene);
            SaveManager.SpawnedThrottledSavedScene.Invoke(packedScene, savedScene, spawnCount, spawnTotal, (float)spawnCount / spawnTotal);
            bool underTargetFramerate = (int)(1 / this.GetProcessDeltaTime()) < targetSpawnFramerate;
            if (underTargetFramerate) {
                await GDE.Yield(1, token);
                if (token.IsCancellationRequested)
                    return;
                sw.Reset();
                sw.Stop();
            }
        }
        for (int i = 0; i < removedObjects.Count; i++)
            Stage.AddChild(removedObjects[i]);
        SaveManager.FinishedSpawningThrottledSavedScenes.Invoke();
    }

    // TODO: PackedScene MetaData Not Working: https://github.com/godotengine/godot/pull/82532
    public CachedSceneData GetSceneData(long uid) {
        if (!GDE.IsUidValid(uid))
            return new CachedSceneData { valid = false };
        CachedSceneData cachedSceneData;
        if (cachedSavedSceneInfo.TryGetValue(uid, out cachedSceneData))
            return cachedSceneData;
        else {
            PackedScene packedScene = GDE.UidToResource<PackedScene>(uid);
            cachedSceneData = new() { packedScene = packedScene };
            Node instance = cachedSceneData.packedScene.Instantiate();
            instance.SetUniqueName();
            cachedSceneData.instance = instance;
            Saver saver = instance.GetComponent<Saver>();
            if (saver == null) {
                GDS.LogErr($"Scene at path({packedScene.ResourcePath}) has no saver component.");
                cachedSceneData.valid = false;
                return cachedSceneData;
            }
            var cachedSavableData = saver.CachedSaveableData;
            int saveableCount = cachedSavableData.Count;
            cachedSceneData.saveableIds = new string[saveableCount];
            for (int i = 0; i < saveableCount; i++)
                cachedSceneData.saveableIds[i] = cachedSavableData[i].saveableId.Value;
            cachedSceneData.valid = true;
            cachedSavedSceneInfo.Add(uid, cachedSceneData);
            return cachedSceneData;
        }
    }
}
