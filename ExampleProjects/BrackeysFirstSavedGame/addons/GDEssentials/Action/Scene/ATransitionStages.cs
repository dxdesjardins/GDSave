using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ATransitionStages : GameAction
{
    [Export] PackedScene transition;
    [Export] bool unloadPersistantStages = true;
    [Export] private ResourceWeakRef[] stages;

    public override async void Invoke(Node node) {
        if (transition != null)
            await StageTransition.StartTransition(transition);
        await StageManager.UnloadAllStages(unloadPersistantStages);
        for (int i = 0; i < stages.Length; i++)
            StageManager.LoadStage(stages[i]);
    }
}
