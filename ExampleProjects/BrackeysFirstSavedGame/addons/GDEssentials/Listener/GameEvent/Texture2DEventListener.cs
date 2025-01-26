using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public partial class Texture2DEventListener : ParamEventListener<Texture2D>
{
    [Export] private Texture2DEvent eventObject;
    protected override ParamEvent<Texture2D> EventObject { get { return eventObject; } }

    public override void Dispatch(Texture2D parameter) {
        eventActions?.Invoke(parameter, this);
    }
}
