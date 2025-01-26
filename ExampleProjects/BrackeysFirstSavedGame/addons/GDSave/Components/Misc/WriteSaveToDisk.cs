using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;

namespace Chomp.Save.Components;

public partial class WriteSaveToDisk : Node
{
    [Export] public Godot.Collections.Array<Trigger> saveTriggers = new() { Trigger.OnReady };

    public enum Trigger {
        OnEnterTree,
        OnReady,
    }

    public override void _EnterTree() {
        if (saveTriggers.Contains(Trigger.OnEnterTree))
            SaveManager.WriteActiveSaveToDisk();
    }

    public override void _Ready() {
        if (saveTriggers.Contains(Trigger.OnReady))
            SaveManager.WriteActiveSaveToDisk();
    }
}
