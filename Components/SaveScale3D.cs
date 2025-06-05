using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;

namespace Chomp.Save.Components;

// Note: Godot 4.3 does not support scaling of physics bodies as is documented here:
// https://docs.godotengine.org/en/stable/tutorials/physics/troubleshooting_physics_issues.html

[Tool]
public partial class SaveScale3D : Node, ISaveable
{
    private Vector3 savedScale = Vector3.Inf;

    [ExportGroup("Saving")]
    [Export] public string SaveableId { get; set; }

    public void OnLoad(string data) {
        var scale = GDS.Deserialize<Vector3>(data);
        Node parent = this.GetParent();
        if (parent is Node3D node3D)
            node3D.Scale = scale;
        savedScale = scale;
    }

    public string OnSave() {
        Node parent = this.GetParent();
        if (parent is Node3D node3D)
            savedScale = node3D.Scale;
        return GDS.Serialize(savedScale);
    }

    public bool OnSaveCondition() {
        Node parent = this.GetParent();
        if (parent is Node3D node3D)
            return savedScale != node3D.Scale;
        return false;
    }
}
