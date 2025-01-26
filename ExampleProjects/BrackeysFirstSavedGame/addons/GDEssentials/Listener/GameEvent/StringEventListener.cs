using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public partial class StringEventListener : ParamEventListener<string>
{
    [Export] private StringEvent eventObject;
    protected override ParamEvent<string> EventObject { get { return eventObject; } }

    public override void Dispatch(string parameter) {
        eventActions?.Invoke(parameter, this);
    }
}
