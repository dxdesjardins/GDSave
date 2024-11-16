using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;

namespace Chomp.Save.Components;

[Tool]
public partial class SaveState : Node, ISaveable
{
    private bool savedInTree = false;
    private bool isInTree = false;
    private bool saved;

    [ExportGroup("Saving")]
    [Export] public string SaveableId { get; set; }

    public override void _EnterTree() {
        if (!Engine.IsEditorHint())
            isInTree = true;
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
