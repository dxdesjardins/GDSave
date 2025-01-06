using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;

namespace Chomp.Save.Components;

[Tool]
public partial class SaveTransform3D : Node, ISaveable
{
    private Transform3D savedTransform = Transform3D.Identity;
    private bool saved;

    [ExportGroup("Saving")]
    [Export] public string SaveableId { get; set; }

    public void OnLoad(string data) {
        saved = true;
        var transform = GDS.Deserialize<Transform3D>(data);
        Node parent = this.GetParent();
        if (parent is Node3D node3D) {
            node3D.Transform = transform;
            // We set the transform twice if the object is a physics body to prevent it from being reverted during the physics process.
            if (parent is PhysicsBody3D physicsBody3D)
                _ = GDE.CallDeferredPhysics(() => { physicsBody3D.Transform = transform; });
        }
        savedTransform = transform;
    }

    public string OnSave() {
        saved = true;
        Node parent = this.GetParent();
        if (parent is Node3D node3D)
            savedTransform = node3D.Transform;
        return GDS.Serialize(savedTransform);
    }

    public bool OnSaveCondition() {
        Node parent = this.GetParent();
        if (parent is Node3D node3D)
            return !saved || savedTransform != node3D.Transform;
        return false;
    }
}
