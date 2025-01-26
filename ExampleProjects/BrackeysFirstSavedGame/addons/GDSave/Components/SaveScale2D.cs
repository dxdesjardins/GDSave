using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;

namespace Chomp.Save.Components;

// Note: Godot 4.3 does not support scaling of physics bodies as is documented here:
// https://docs.godotengine.org/en/stable/tutorials/physics/troubleshooting_physics_issues.html

[Tool]
public partial class SaveScale2D : Node, ISaveable
{
    private Vector2 savedScale = Vector2.Inf;

    [ExportGroup("Saving")]
    [Export] public string SaveableId { get; set; }

    public void OnLoad(string data) {
        var scale = GDS.Deserialize<Vector2>(data);
        Node parent = this.GetParent();
        if (parent is Node2D node2D)
            node2D.Scale = scale;
        else if (parent is Control control)
            control.Scale = scale;
        savedScale = scale;
    }

    public string OnSave() {
        Node parent = this.GetParent();
        if (parent is Node2D node2D)
            savedScale = node2D.Scale;
        else if (parent is Control control)
            savedScale = control.Scale;
        return GDS.Serialize(savedScale);
    }

    public bool OnSaveCondition() {
        Node parent = this.GetParent();
        if (parent is Node2D node2D)
            return savedScale != node2D.Scale;
        else if (parent is Control control)
            return savedScale != control.Scale;
        return false;
    }
}
