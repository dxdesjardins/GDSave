using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ASetScale : ParamAction<Vector2>
{
    [ExportGroup("Target")]
    [Export] NodePath target;
    [ExportGroup("Parameters")]
    [Export] Vector2 value = new Vector2(1, 1);

    public override void Invoke(Vector2 param, Node node) {
        Node tar;
        if (target.IsEmpty)
            tar = node.GetParent();
        else
            tar = node.GetNode(target);
        if (tar is Control control)
            control.Scale = param;
        else if (tar is Node2D node2D)
            node2D.Scale = param;
        else
            GDE.LogErr("SetScaleAction Failed: Parent is not a Control or Node2D");
    }

    public override void Invoke(Node node) => Invoke(value, node);
}
