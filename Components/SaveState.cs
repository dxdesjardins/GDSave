using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;

namespace Chomp.Save.Components;

[Tool]
public partial class SaveState : Node, ISaveable
{
    /// <summary> Stops physics events from happening before loading. </summary>
    [Export] private bool disableFirstFrame = true;
    private bool savedInTree = false;
    private bool isInTree = false;
    private bool saved;

    [ExportGroup("Saving")]
    [Export] public string SaveableId { get; set; }

    public override void _EnterTree() {
        if (Engine.IsEditorHint())
            return;
        isInTree = true;
        if (disableFirstFrame) {
            Node parent = this.GetParent();
            ProcessModeEnum processMode = parent.ProcessMode;
            parent.ProcessMode = ProcessModeEnum.Disabled;
            _ = GDE.CallDeferred(() => { this.GetParent().ProcessMode = processMode; });
        }
    }

    public override void _ExitTree() {
        if (!Engine.IsEditorHint() && !StageManager.IsStageUnloading(this.GetStage()) && !StageManager.IsQuittingGame)
            isInTree = false;
    }

    public void OnLoad(string data) {
        saved = true;
        savedInTree = data == "1";
        isInTree = savedInTree;
        if (!savedInTree)
            _ = this.SafeRemoveParent();
    }

    public string OnSave() {
        saved = true;
        savedInTree = isInTree;
        return savedInTree ? "1" : "0";
    }

    public bool OnSaveCondition() {
        return !saved || savedInTree != isInTree;
    }
}
