using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public partial class Vector2IEventListener : ParamEventListener<Vector2I>
{
    [Export] private Vector2IEvent eventObject;
    protected override ParamEvent<Vector2I> EventObject { get { return eventObject; } }

    public override void Dispatch(Vector2I parameter) {
        eventActions?.Invoke(parameter, this);
    }
}
