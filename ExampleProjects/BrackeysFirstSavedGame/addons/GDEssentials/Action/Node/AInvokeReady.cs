using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class AInvokeReady : ParamAction<Node>
{
    [ExportGroup("Target")]
    [Export] NodeReference nodeReference;
    [Export] NodePath nodePath;

    public override void Invoke(Node node) {
        if (nodeReference?.Instance != null)
            node = nodeReference.Instance;
        else if (!nodePath.IsEmpty)
            node = node.GetNode(nodePath);
        node._Ready();
    }

    public override void Invoke(Node param, Node node) => Invoke(param);
}
