using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ASetModulateAlpha : ParamAction<float>
{
    [ExportGroup("Target")]
    [Export] public NodePath canvasItem;
    [ExportGroup("Parameters")]
    [Export] public float alpha = 0;

    public override void Invoke(float param, Node node) {
        if (canvasItem.IsEmpty) {
            Color color = node.GetParent<CanvasItem>().Modulate;
            node.GetParent<CanvasItem>().Modulate = new Color(color.R, color.G, color.B, param);
        }
        else {
            Color mod = node.GetNode<CanvasItem>(canvasItem).Modulate;
            node.GetNode<CanvasItem>(canvasItem).Modulate = new Color(mod.R, mod.G, mod.B, param);
        }
    }

    public override void Invoke(Node node) => Invoke(alpha, node);
}
