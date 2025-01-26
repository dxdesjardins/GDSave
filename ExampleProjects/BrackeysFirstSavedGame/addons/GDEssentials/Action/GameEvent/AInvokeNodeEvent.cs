using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class AInvokeNodeEvent : GameAction
{
    [Export] NodeEvent nodeEvent;
    [Export] NodePath nodePath;

    public override void Invoke(Node node) {
        nodeEvent.Invoke(node.GetNode(nodePath));
    }
}
