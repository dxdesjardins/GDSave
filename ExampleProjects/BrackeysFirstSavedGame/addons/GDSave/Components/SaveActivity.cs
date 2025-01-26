using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;
using Chomp.Save.Internal;

namespace Chomp.Save.Components;

[Tool]
public partial class SaveActivity : Node, ISaveable
{
    public Activity savedActivity = new();
    private bool saved = false;

    [ExportGroup("Saving")]
    [Export] public string SaveableId { get; set; }

    public struct Activity {
        public ProcessModeEnum ProcessMode { get; set; }
        public bool PhysicsProcessing { get; set; }
        public bool Visible { get; set; }
    }

    private Activity GetActivity() {
        Node parent = this.GetParent();
        return new Activity() {
            ProcessMode = parent.ProcessMode,
            PhysicsProcessing = parent.IsPhysicsProcessing(),
            Visible = parent is CanvasItem canvasItem ? canvasItem.Visible : false
        };
    }

    public void OnLoad(string data) {
        saved = true;
        savedActivity = GDS.Deserialize<Activity>(data);
        Node parent = this.GetParent();
        parent.ProcessMode = savedActivity.ProcessMode;
        parent.SetPhysicsProcess(savedActivity.PhysicsProcessing);
        if (parent is CanvasItem canvasItem)
            canvasItem.Visible = savedActivity.Visible;
    }

    public string OnSave() {
        saved = true;
        savedActivity = GetActivity();
        return GDS.Serialize(savedActivity);
    }

    public bool OnSaveCondition() {
        return !saved || !savedActivity.Equals(GetActivity());
    }
}
