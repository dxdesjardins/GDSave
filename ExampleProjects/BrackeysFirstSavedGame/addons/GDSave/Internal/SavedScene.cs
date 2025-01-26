using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;
using Chomp.Save.Components;

namespace Chomp.Save.Internal;

public partial class SavedScene : Node
{
    private SavedSceneManager savedSceneManager;
    public Saver Saveable { get; private set; }
    public SavedSceneManager.SpawnInfo SpawnInfo { get; private set; }
    private bool wipeDataWhenRemoved = true;
    private ulong removeFrame = 0;
    public bool DontSaveScene => !Saveable.SaveWhenRemoved && removeFrame != 0 && removeFrame != Engine.GetProcessFrames();

    public void Configure(Saver saveable, SavedSceneManager instanceManager, SavedSceneManager.SpawnInfo spawnInfo) {
        this.Saveable = saveable;
        this.savedSceneManager = instanceManager;
        this.SpawnInfo = spawnInfo;
    }

    public override void _EnterTree() {
        removeFrame = 0;
    }

    public override void _ExitTree() {
        removeFrame = Engine.GetProcessFrames();
        if (this.IsRemovedExplicitly() && Saveable.SaveWhenRemoved) {
            if (wipeDataWhenRemoved) {
                SaveManager.WipeSaveable(Saveable);
                savedSceneManager.WipeCachedSceneData(this, Saveable);
            }
        }
    }

    public void RemoveAndWipeScene() {
        Saveable.manualSaveLoad = true;
        wipeDataWhenRemoved = false;
        SaveManager.RemoveListener(Saveable);
        if (Saveable.IsParent())
            Saveable.Remove();
        else
            Saveable.RemoveParent();
    }
}
