using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class AQueueFreeAllChildren : ParamAction<Node>
{
    [ExportGroup("Target")]
    [Export] NodeReference nodeReference;
    [Export] NodePath nodePath;

    public override void Invoke(Node node) {
        if (nodeReference?.Instance != null)
            node = nodeReference.Instance;
        else if (!nodePath.IsEmpty)
            node = node.GetNode(nodePath);
        if (node.GetChildCount() > 0)
            foreach (Node child in node.GetChildren())
                child.QueueFree();
    }

    public override void Invoke(Node param, Node node) => Invoke(param);
}
