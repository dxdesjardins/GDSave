using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;

namespace Chomp.Save.Components;

[Tool]
public partial class SaveRotation2D : Node, ISaveable
{
    private float savedRotation = Mathf.Inf;

    [ExportGroup("Saving")]
    [Export] public string SaveableId { get; set; }

    public void OnLoad(string data) {
        float rotation = data.ToFloat();
        Node parent = this.GetParent();
        if (parent is Node2D node2D) {
            node2D.Rotation = rotation;
            // We set the rotation twice if the object is a physics body to prevent it from being reverted during the physics process.
            if (parent is PhysicsBody2D physicsBody2D)
                _ = GDE.CallDeferredPhysics(() => { physicsBody2D.Rotation = rotation; });
        }
        else if (parent is Control control)
            control.Rotation = rotation;
        savedRotation = rotation;
    }

    public string OnSave() {
        Node parent = this.GetParent();
        if (parent is Node2D node2D)
            savedRotation = node2D.Rotation;
        else if (parent is Control control)
            savedRotation = control.Rotation;
        return savedRotation.ToString();
    }

    public bool OnSaveCondition() {
        Node parent = this.GetParent();
        if (parent is Node2D node2D)
            return savedRotation != node2D.Rotation;
        else if (parent is Control control)
            return savedRotation != control.Rotation;
        return false;
    }
}
