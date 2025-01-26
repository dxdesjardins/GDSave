using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class AInvokeFloatEvent : GameAction
{
    [Export] FloatEvent floatEvent;
    [Export] float param;

    public override void Invoke(Node node) {
        floatEvent.Invoke(param);
    }
}
