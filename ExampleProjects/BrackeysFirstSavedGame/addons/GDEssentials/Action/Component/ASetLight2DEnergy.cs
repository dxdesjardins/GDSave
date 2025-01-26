using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ASetLight2DEnergy : ParamAction<float>
{
    [ExportGroup("Target")]
    [Export] public NodePath light2D;
    [ExportGroup("Parameters")]
    [Export] public float energy;
    [Export] public float multiplier = 1;

    public override void Invoke(float param, Node node) {
        if (light2D.IsEmpty)
            node.GetParent<Light2D>().Energy = param * multiplier;
        else
            node.GetNode<Light2D>(light2D).Energy = param * multiplier;
    }

    public override void Invoke(Node node) => Invoke(energy, node);
}
