using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class AQueueFree : ParamAction<Node>
{
    [ExportGroup("Target")]
    [Export] NodeReference nodeReference;
    [Export] NodePath nodePath;

    public override void Invoke(Node node) {
        Node tar;
        if (nodeReference?.Instance != null)
            tar = nodeReference.Instance;
        else if (nodePath != default)
            tar = node.GetNode(nodePath);
        else
            tar = node.GetParent();
        tar.QueueFree();
    }

    public override void Invoke(Node param, Node node) => Invoke(param);
}
