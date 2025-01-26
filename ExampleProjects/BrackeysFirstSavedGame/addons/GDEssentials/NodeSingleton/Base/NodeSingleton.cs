using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public abstract partial class NodeSingleton<T> : Node2D where T : NodeSingleton<T>
{
    private static T instance;
    protected virtual bool SearchTreeForInstance { get; set; } = true;
    protected virtual bool AddToTree { get; set; } = true;

    public static T Instance {
        get {
            if (instance == null)
                CreateInstance();
            return instance;
        }
        set { instance = value; }
    }

    public NodeSingleton() {
        if (instance == null || Engine.IsEditorHint()) {
            instance = this as T;
			_ = GDE.CallDeferred(() => this.TreeExited += () => instance = null);
        }
        else {
            GDE.LogErr($"{typeof(T).Name} exists multiple times in violation of singleton pattern. Destroying copy.");
            this.QueueFree();
        }
    }

    protected static T CreateInstance() {
        if (Engine.IsEditorHint())
            return null;
        Window root = (Engine.GetMainLoop() as SceneTree).Root;
        T _instance = (T)Activator.CreateInstance(typeof(T));
        _instance.Name = typeof(T).Name;
        if (_instance.SearchTreeForInstance) {
            GDE.Log($"Searching entire tree for instance of: {typeof(T).Name}.");
            T[] singletons = root.GetComponentsInChildren<T>();
            if (singletons.Length != 0) {
                if (singletons.Length == 1) {
                    instance = singletons[0];
                    instance.Name = typeof(T).Name;
                    return instance;
                }
                else {
                    GDE.LogErr($"{typeof(T).Name} exists multiple times in violation of singleton pattern. Destroying all copies.");
                    foreach (T singleton in singletons)
                        singleton.QueueFree();
                }
            }
        }
        if (_instance.AddToTree)
            root.SafeAddChild(_instance);
        return instance;
    }
}
