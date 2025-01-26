using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public partial class IntEventListener : ParamEventListener<int>
{
    [Export] private IntEvent eventObject;
    protected override ParamEvent<int> EventObject { get { return eventObject; } }

    public override void Dispatch(int parameter) {
        eventActions?.Invoke(parameter, this);
    }
}
