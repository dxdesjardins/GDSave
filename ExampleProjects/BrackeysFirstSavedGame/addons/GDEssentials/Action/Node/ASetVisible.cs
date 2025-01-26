using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ASetVisible: ParamAction<bool> {
    [Export] NodePath nodePath;
    [Export] bool state;

    public override void Invoke(bool param, Node node) {
        if (nodePath.IsEmpty)
            (node.GetParent() as CanvasItem).Visible = param;
        else
            (node.GetNode(nodePath) as CanvasItem).Visible = param;
    }

    public override void Invoke(Node node) => Invoke(state, node);
}
