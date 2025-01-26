using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public partial class Vector3EventListener : ParamEventListener<Vector3>
{
    [Export] private Vector3Event eventObject;
    protected override ParamEvent<Vector3> EventObject { get { return eventObject; } }

    public override void Dispatch(Vector3 parameter) {
        eventActions?.Invoke(parameter, this);
    }
}
