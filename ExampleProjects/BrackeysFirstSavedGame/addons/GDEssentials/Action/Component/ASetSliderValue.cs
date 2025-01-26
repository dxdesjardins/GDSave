using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ASetSliderValue : ParamAction<float>
{
    [ExportGroup("Target")]
    [Export] NodePath slider;
    [ExportGroup("Parameters")]
    [Export] float value;

    public override void Invoke(float param, Node node) {
        if (slider.IsEmpty)
            node.GetParent<Slider>().Value = param;
        else
            node.GetNode<Slider>(slider).Value = param;
    }

    public override void Invoke(Node node) => Invoke(value, node);
}
