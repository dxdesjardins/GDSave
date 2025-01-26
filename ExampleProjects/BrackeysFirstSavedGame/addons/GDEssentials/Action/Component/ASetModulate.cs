using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ASetModulate : ParamAction<Color>
{
    [Export] private NodePath canvasItem;
    [Export] private Color modulate;

    public override void Invoke(Color param, Node node) {
        if (canvasItem.IsEmpty)
            node.GetParent<CanvasItem>().Modulate = param;
        else
            node.GetNode<CanvasItem>(canvasItem).Modulate = param;
    }

    public override void Invoke(Node node) => Invoke(modulate, node);
}
