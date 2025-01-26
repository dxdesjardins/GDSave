using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;

namespace Chomp.Save.Components;

[Tool]
public partial class SaveRotation3D : Node, ISaveable
{
    private Vector3 savedRotation = Vector3.Inf;

    [ExportGroup("Saving")]
    [Export] public string SaveableId { get; set; }

    public void OnLoad(string data) {
        var rotation = GDS.Deserialize<Vector3>(data);
        Node parent = this.GetParent();
        if (parent is Node3D node3D) {
            node3D.Rotation = rotation;
            // We set the rotation twice if the object is a physics body to prevent it from being reverted during the physics process.
            if (parent is PhysicsBody3D physicsBody3D)
                _ = GDE.CallDeferredPhysics(() => { physicsBody3D.Rotation = rotation; });
        }
        savedRotation = rotation;
    }

    public string OnSave() {
        Node parent = this.GetParent();
        if (parent is Node3D node3D)
            savedRotation = node3D.Rotation;
        return GDS.Serialize(savedRotation);
    }

    public bool OnSaveCondition() {
        Node parent = this.GetParent();
        if (parent is Node3D node3D)
            return savedRotation != node3D.Rotation;
        return false;
    }
}
