using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public partial class ColorEventListener : ParamEventListener<Color>
{
    [Export] private ColorEvent eventObject;
    protected override ParamEvent<Color> EventObject { get { return eventObject; } }

    public override void Dispatch(Color parameter) {
        eventActions?.Invoke(parameter, this);
    }
}
