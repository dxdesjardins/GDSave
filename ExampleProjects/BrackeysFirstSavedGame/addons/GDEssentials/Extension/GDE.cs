using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

namespace Chomp.Essentials;

public static class GDE
{
    public static T GetComponentInTree<T>() {
        Window root = (Engine.GetMainLoop() as SceneTree).Root;
        return root.GetComponentInChildren<T>(false);
    }

    public static T LoadFromUid<T>(long resourceUid) where T : Resource {
        return GD.Load<T>(ResourceUid.GetIdPath(resourceUid));
    }

    public static T LoadFromUid<T>(string resourceUid) where T : Resource => LoadFromUid<T>(ResourceUid.TextToId(resourceUid));
    public static Resource LoadFromUid(long resourceUid) => LoadFromUid<Resource>(resourceUid);
    public static Resource LoadFromUid(string resourceUid) => LoadFromUid<Resource>(resourceUid);

    public static long UidToLong(string uid) {
        return ResourceUid.TextToId(uid);
    }

    public static string UidToString(long uid) {
        return ResourceUid.IdToText(uid);
    }

    public static T UidToResource<T>(long uid) where T : Resource {
        return GD.Load<T>(ResourceUid.GetIdPath(uid));
    }

    public static T UidToResource<T>(string uid) where T : Resource {
        return UidToResource<T>(UidToLong(uid));
    }

    public static string UidToPath(long uid) {
        return ResourceUid.GetIdPath(uid);
    }

    public static string UidToPath(string uid) {
        return UidToPath(UidToLong(uid));
    }

    public static long PathToUid(string path) {
        return ResourceLoader.GetResourceUid(path);
    }

    public static string PathToUidString(string path) {
        return ResourceUid.IdToText(ResourceLoader.GetResourceUid(path));
    }

    public static bool IsUidValid(long uid) {
        return ResourceUid.HasId(uid);
    }

    public static bool IsUidValid(string uid) {
        return ResourceUid.HasId(ResourceUid.TextToId(uid));
    }

    public static Resource UidToResource(long uid) => UidToResource<Resource>(uid);
    public static Resource UidToResource(string uid) => UidToResource<Resource>(uid);

    public static Task Yield(int frames = 1, CancellationToken token = default) {
        SceneTree tree = Engine.GetMainLoop() as SceneTree;
        var tcs = new TaskCompletionSource();
        void Receive() {
            frames -= 1;
            if (frames <= 0) {
                tcs.TrySetResult();
                tree.ProcessFrame -= Receive;
            }
        }
        tree.ProcessFrame += Receive;
        if (token.CanBeCanceled)
            token.Register(() => tcs.TrySetCanceled(token));
        return tcs.Task;
    }

    public static Task YieldPhysics(int frames = 1, CancellationToken token = default) {
        SceneTree tree = Engine.GetMainLoop() as SceneTree;
        var tcs = new TaskCompletionSource();
        void Receive() {
            frames -= 1;
            if (frames <= 0) {
                tcs.TrySetResult();
                tree.PhysicsFrame -= Receive;
            }
        }
        tree.PhysicsFrame += Receive;
        if (token.CanBeCanceled)
            token.Register(() => tcs.TrySetCanceled(token));
        return tcs.Task;
    }

    public static async Task CallDeferred(Action action, int frames = 1, CancellationToken token = default) {
        await Yield(frames, token);
        action.Invoke();
    }

    public static async Task CallDeferredPhysics(Action action, int frames = 1, CancellationToken token = default) {
        await YieldPhysics(frames, token);
        action.Invoke();
    }

    public static void Log(string text, int frame = 1) {
        var stackFrame = new System.Diagnostics.StackTrace(true).GetFrame(frame);
        string callerClassName = stackFrame.GetMethod().DeclaringType.Name;
        int callerLine = stackFrame.GetFileLineNumber();
        text = $"[{callerClassName}:{callerLine}] {text}";
        GD.Print(text);
        Console.WriteLine(text);
    }

    public static void LogErr(string text, int frame = 1) {
        var stackFrame = new System.Diagnostics.StackTrace(true).GetFrame(frame);
        string callerClassName = stackFrame.GetMethod().DeclaringType.Name;
        int callerLine = stackFrame.GetFileLineNumber();
        text = $"[{callerClassName}:{callerLine}] {text}";
        GD.PrintErr(text);
        Console.Error.WriteLine(text);
    }

    public static List<T> GetResourcesInDirectory<T>(string path) where T : Resource {
        path = ProjectSettings.GlobalizePath(path);
        if (!Directory.Exists(path)) {
            GDE.LogErr($"Directory path({path}) does not exist", 2);
            return null;
        }
        string extension;
        if (typeof(T) == typeof(PackedScene))
            extension = "*.tscn";
        else
            extension = "*.tres";
        List<string> files = new(Directory.GetFiles(path, extension, SearchOption.AllDirectories));
        List<T> resources = new();
        for (int i = 0; i < files.Count; i++) {
            if (GD.Load(files[i]) is T resource)
                resources.Add(resource);
        }
        return resources;
    }

    // This is a temporary workaround since the Engine changing the Uid of a resource via ResourceUid and ResourceSaver is currently broken.
    public static void ChangeResourceUid(string filePath, string newUid) {
        filePath = ProjectSettings.GlobalizePath(filePath);
        string[] lines = File.ReadAllLines(filePath);
        string pattern = @"uid=""uid://[^""]+""";
        string replacement = $"uid=\"{newUid}\"";
        if (Regex.IsMatch(lines[0], pattern))
            lines[0] = Regex.Replace(lines[0], pattern, replacement);
        File.WriteAllLines(filePath, lines);
    }
}
