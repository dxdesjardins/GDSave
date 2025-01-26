using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class NodeReference : Resource
{
    [Export] private PackedScene packedScene;
    private Action<Node> dispatchEvent;
    private Node instance;

    public Node Instance {
        get {
            if (instance == null) {
                if (packedScene != null) {
                    instance = packedScene.Instantiate<Node>();
                    return instance;
                }
                else
                    return null;
            }
            else
                return instance;
        }
        set {
            instance = value;
            if (value != null)
                dispatchEvent?.Invoke(value);
        }
    }

    public void AddListener(Action<Node> listener) {
        dispatchEvent += listener;
        if (instance != null)
            listener.Invoke(instance);
    }

    public void RemoveListener(Action<Node> listener) {
        dispatchEvent -= listener;
    }
}
