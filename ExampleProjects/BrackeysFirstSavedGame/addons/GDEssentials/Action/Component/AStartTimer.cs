using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class AStartTimer : ParamAction<float>
{
    [ExportGroup("Target")]
    [Export] NodePath timer;
    [ExportGroup("Parameters")]
    [Export] float time = -1;

    public override void Invoke(float param, Node node) {
        if (timer.IsEmpty)
            node.GetParent<Timer>().Start(param);
        else
            node.GetNode<Timer>(timer).Start(param);
    }

    public override void Invoke(Node node) => Invoke(time, node);
}
