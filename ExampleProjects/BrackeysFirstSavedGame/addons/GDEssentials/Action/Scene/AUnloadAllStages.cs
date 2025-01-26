using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class AUnloadAllStages : GameAction
{
    [Export] bool includePersistant = false;

    public override async void Invoke(Node node) {
        await StageManager.UnloadAllStages(includePersistant);
    }
}
