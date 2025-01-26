using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ALoadStage : ParamAction<PackedScene>
{
    [Export] private ResourceWeakRef stageRef;

    public override void Invoke(PackedScene stage, Node node) {
        if (stageRef != null)
            stage = stageRef;
        StageManager.LoadStage(stage);
    }

    public override void Invoke(Node node) => Invoke(stageRef, node);
}
