using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class AUnloadStage : ParamAction<PackedScene>
{
    [Export] private ResourceWeakRef stage;

    public override async void Invoke(PackedScene stage, Node node) {
        Node target;
        if (this.stage == null)
            target = node.GetStage();
        else
            target = StageManager.GetLoadedStage(this.stage.GetUidString());
        await StageManager.UnloadStage(target);
    }

    public override void Invoke(Node node) => Invoke(stage, node);
}
