using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ASetActive : ParamAction<bool>
{
    [Export] NodePath nodePath;
    [Export] bool state;

    public override void Invoke(bool param, Node node) {
        if (nodePath.IsEmpty)
            node.GetParent().SetActive(param);
        else
            node.GetNode(nodePath).SetActive(param);
    }

    public override void Invoke(Node node) => Invoke(state, node);
}
