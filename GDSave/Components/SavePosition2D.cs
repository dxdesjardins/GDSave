using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Chomp.Save.Components;

[Tool]
public partial class SavePosition2D : Node, ISaveable
{
    public Vector2 savedPosition = Vector2.Inf;

    [ExportGroup("Saving")]
    [Export] public string SaveableId { get; set; }

    public void OnLoad(string data) {
        var position = GDS.Deserialize<Vector2>(data);
        Node parent = this.GetParent();
        if (parent is Node2D node2D) {
            node2D.Position = position;
            // We set the position twice if the object is a physics body to prevent it from being reverted during the physics process.
            if (parent is PhysicsBody2D physicsBody2D)
                _ = GDE.CallDeferredPhysics(() => { physicsBody2D.Position = position; });
            else if (parent is Camera2D camera2D)
                _ = GDE.CallDeferred(camera2D.ResetSmoothing);
        }
        else if (parent is Control control)
            control.Position = position;
        savedPosition = position;
    }

    public string OnSave() {
        Node parent = this.GetParent();
        if (parent is Node2D node2D)
            savedPosition = node2D.Position;
        else if (parent is Control control)
            savedPosition = control.Position;
        return GDS.Serialize(savedPosition);
    }

    public bool OnSaveCondition() {
        Node parent = this.GetParent();
        if (parent is Node2D node2D)
            return savedPosition != node2D.Position;
        else if (parent is Control control)
            return savedPosition != control.Position;
        return false;
    }
}
