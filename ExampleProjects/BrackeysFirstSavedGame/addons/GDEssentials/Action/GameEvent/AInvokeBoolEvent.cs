using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class AInvokeBoolEvent : GameAction
{
    [Export] BoolEvent boolEvent;
    [Export] bool state;

    public override void Invoke(Node node) {
        boolEvent.Invoke(state);
    }
}
