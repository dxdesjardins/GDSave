using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chomp.Essentials;

[Tool]
public partial class StageManager : NodeSingleton<StageManager>
{
    [Export] private string stageDirectory = "res://Scene/Stage";
    [Export] private GameAction[] gameStartActions;
    [Export] private PackedScene[] gameStartStages;
    private static bool initialized = false;

    public static Node StageRoot { get; private set; }

    public static Dictionary<string, StageData> CachedStageData { get; private set; } = new();
    public static int StageCount { get; private set; }
    public static string LastActiveStageName { get; private set; }
    public static string LastActiveStageUid { get; private set; }
    public static bool IsQuittingGame { get; private set; }
    public static Node ActiveStage { get; private set; }
    public static bool IsTransitioning { get; private set; }
    public static List<Node> LoadedStages { get; private set; } = new();
    public static List<Node> TotalStages { get; private set; } = new();
    public static List<string> LoadingStages { get; private set; } = new();
    public static List<Node> UnloadingStages { get; private set; } = new();

    public static Action<Node> TransitionBeforeFadeOut { get; set; } = delegate { };
    public static Action<Node> TransitionAfterFadeOut { get; set; } = delegate { };
    public static Action<Node> TransitionBeforeFadeIn { get; set; } = delegate { };
    public static Action<Node> TransitionAfterFadeIn { get; set; } = delegate { };

    public static Action<Node> ActiveStageUnloading { get; set; } = delegate { };
    public static Action<Node> ActiveStageUnloaded { get; set; } = delegate { };
    public static Action<Node> ActiveStageLoaded { get; set; } = delegate { };
    public static Action<Node> StageUnloading { get; set; } = delegate { };
    public static Action<Node> StageUnloaded { get; set; } = delegate { };
    public static Action<Node> StageLoaded { get; set; } = delegate { };
    public static Action<Node> UnloadingComplete { get; set; } = delegate { };
    public static Action<Node> LoadingComplete { get; set; } = delegate { };

    public static Action<bool> ApplicationFocused { get; set; } = delegate { };
    public static Action<bool> ApplicationPaused { get; set; } = delegate { };
    public static Action ApplicationQuitting { get; set; } = delegate { };

    public struct StageData(string path, string fileName, PackedScene packedScene)
    {
        public string path = path;
        public string fileName = fileName;
        public PackedScene packedScene = packedScene;
    }

    private void CacheStageData() {
        foreach (var packedScene in GDE.GetResourcesInDirectory<PackedScene>(stageDirectory)) {
            string path = packedScene.GetPath();
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            string uid = packedScene.GetUidString();
            CachedStageData.Add(uid, new StageData(path, name, packedScene));
        }
    }

    public override void _Notification(int what) {
        if (Engine.IsEditorHint())
            return;
        switch (what) {
            case (int)NotificationEnterTree:
                Initialize();
                break;
            case (int)NotificationReady:
                gameStartActions.Invoke(this);
                if (gameStartStages != null) {
                    for (int i = 0; i < gameStartStages.Length; i++)
                        _ = LoadStage(gameStartStages[i]);
                }
                break;
            case (int)NotificationApplicationPaused:
                ApplicationPaused.Invoke(true);
                break;
            case (int)NotificationApplicationResumed:
                ApplicationPaused.Invoke(false);
                break;
            case (int)NotificationApplicationFocusIn:
                ApplicationFocused.Invoke(true);
                break;
            case (int)NotificationApplicationFocusOut:
                ApplicationFocused.Invoke(false);
                break;
            case (int)NotificationWMGoBackRequest:
            case (int)NotificationWMCloseRequest:
                IsQuittingGame = true;
                ApplicationQuitting.Invoke();
                break;
        }
    }

    public void Initialize() {
        if (initialized)
            return;
        initialized = true;
        CacheStageData();
        if (this.IsAnAutoload())
            StageRoot = this.GetTree().Root;
        else
            StageRoot = this;
        StageRoot.ChildEnteredTree += (child) => {
            if (child is not IStage)
                return;
            if (ActiveStage == null && !child.IsInGroup("Persistant")) {
                ActiveStage = child;
                if (string.IsNullOrEmpty(LastActiveStageUid)) {
                    LastActiveStageName = child.Name;
                    LastActiveStageUid = child.GetUidString();
                }
                child.TreeExiting += () => {
                    if (ActiveStage == child)
                        ActiveStage = null;
                };
                child.Ready += () => ActiveStageLoaded.Invoke(child);
            }
            TotalStages.Add(child);
            StageCount++;
            child.Ready += () => {
                LoadingStages.Remove(child.GetFileName());
                LoadedStages.Add(child);
                StageLoaded.Invoke(child);
                if (LoadingStages.Count == 0)
                    LoadingComplete.Invoke(child);
            };
            child.TreeExited += () => {
                TotalStages.Remove(child);
                LoadedStages.Remove(child);
                UnloadingStages.Remove(child);
                StageCount--;
                if (ActiveStage == child) {
                    LastActiveStageName = child.Name;
                    LastActiveStageUid = child.GetUidString();
                    ActiveStageUnloaded.Invoke(child);
                }
                StageUnloaded.Invoke(child);
                if (StageCount == 0)
                    UnloadingComplete.Invoke(child);
            };
        };
    }

    public static async Task<Node> UnloadStage(Node stage) {
        UnloadingStages.Add(stage);
        if (ActiveStage == stage)
            ActiveStageUnloading.Invoke(stage);
        StageUnloading.Invoke(stage);
        stage.QueueFree();
        TaskCompletionSource<Node> unloadTcs = new();
        void awaitUnload(Node node) {
            if (node == stage) {
                unloadTcs.TrySetResult(node);
                StageUnloaded -= awaitUnload;
            }
        }
        StageUnloaded += awaitUnload;
        return await unloadTcs.Task;
    }

    public static async Task<Node> UnloadAllStages(bool includePersistant = false) {
        for (int i = LoadedStages.Count - 1; i >= 0; i--)
            if (includePersistant || !LoadedStages[i].IsInGroup("Persistant"))
                await UnloadStage(LoadedStages[i]);
        TaskCompletionSource<Node> unloadingCompleteTcs = new();
        void awaitUnloadingComplete(Node node) {
            unloadingCompleteTcs.TrySetResult(node);
            UnloadingComplete -= awaitUnloadingComplete;
        }
        UnloadingComplete += awaitUnloadingComplete;
        return await unloadingCompleteTcs.Task;
    }

    public static Node LoadStage(PackedScene packedScene) {
        LoadingStages.Add(packedScene.GetFileName());
        return StageRoot.InstantiateChild(packedScene, false);
    }

    public static Node LoadStage(string uid) => LoadStage(GDE.UidToResource<PackedScene>(uid));
    public static Node LoadStage(long uid) => LoadStage(GDE.UidToResource<PackedScene>(uid));

    public static bool IsStageUnloading(Node stage) {
        return UnloadingStages.Contains(stage);
    }

    public static void SetActiveStage(Node stage) {
        if (ActiveStage == stage)
            return;
        ActiveStage = stage;
        stage.TreeExiting += () => {
            if (ActiveStage == stage)
                ActiveStage = null;
        };
        ActiveStageLoaded.Invoke(stage);
    }

    public static void SetActiveStage(string stageUid) {
        foreach (var stage in LoadedStages) {
            if (stage.GetUidString() == stageUid) {
                SetActiveStage(stage);
                return;
            }
        }
    }

    public static Node GetLoadedStage(string stageUid) {
        foreach (var stage in LoadedStages) {
            if (stage.GetUidString() == stageUid)
                return stage;
        }
        return null;
    }

    public static StageData GetStageData(string stageUid) {
        if (CachedStageData.TryGetValue(stageUid, out StageData value))
            return value;
        return default;
    }

    public static bool StageExists(string stageUid) {
        return CachedStageData.ContainsKey(stageUid);
    }
}
