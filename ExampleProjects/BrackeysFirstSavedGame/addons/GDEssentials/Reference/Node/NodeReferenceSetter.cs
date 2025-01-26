using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public partial class NodeReferenceSetter : Node
{
    [Export] private Node target;
    [Export] private NodeReference nodeReference;

    public NodeReference NodeReference => nodeReference;

    public override void _EnterTree() {
        nodeReference.Instance = target ?? this.GetParent<Node>();
    }

    public override void _ExitTree() {
        nodeReference.Instance = null;
    }
}
