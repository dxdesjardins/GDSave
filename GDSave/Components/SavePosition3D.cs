using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;

namespace Chomp.Save.Components;

[Tool]
public partial class SavePosition3D : Node, ISaveable
{
    public Vector3 savedPosition = Vector3.Inf;

    [ExportGroup("Saving")]
    [Export] public string SaveableId { get; set; }

    public void OnLoad(string data) {
        var position = GDS.Deserialize<Vector3>(data);
        Node parent = this.GetParent();
        if (parent is Node3D node3D) {
            node3D.Position = position;
            // We set the position twice if the object is a physics body to prevent it from being reverted during the physics process.
            if (parent is PhysicsBody3D physicsBody3D)
                _ = GDE.CallDeferredPhysics(() => { physicsBody3D.Position = position; });
        }
        savedPosition = position;
    }

    public string OnSave() {
        Node parent = this.GetParent();
        if (parent is Node3D node3D)
            savedPosition = node3D.Position;
        return GDS.Serialize(savedPosition);
    }

    public bool OnSaveCondition() {
        Node parent = this.GetParent();
        if (parent is Node3D node3D)
            return savedPosition != node3D.Position;
        return false;
    }
}
