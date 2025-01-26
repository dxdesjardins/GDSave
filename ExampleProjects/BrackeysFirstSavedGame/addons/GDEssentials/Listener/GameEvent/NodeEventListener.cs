using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public partial class NodeEventListener : ParamEventListener<Node>
{
    [Export] private NodeEvent eventObject;
    protected override ParamEvent<Node> EventObject { get { return eventObject; } }

    public override void Dispatch(Node parameter) {
        eventActions?.Invoke(parameter, this);
    }
}
