using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ARemoveNode : ParamAction<Node>
{
    [ExportGroup("Target")]
    [Export] private NodeReference nodeReference;
    [Export] private NodePath nodePath;

    public override void Invoke(Node param, Node node) {
        if (nodeReference?.Instance != null)
            node = nodeReference.Instance;
        else if (!nodePath.IsEmpty)
            node = node.GetNode(nodePath);
        else if (param != null)
            node = param;
        else
            node = node.GetParent();
        node.GetParent()?.RemoveChild(node);
    }

    public override void Invoke(Node node) => Invoke(null, node);
}
