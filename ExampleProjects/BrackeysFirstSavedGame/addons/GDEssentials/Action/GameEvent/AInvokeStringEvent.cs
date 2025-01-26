using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class AInvokeStringEvent : GameAction
{
    [Export] StringEvent stringEvent;
    [Export] string param;

    public override void Invoke(Node node) {
        stringEvent.Invoke(param);
    }
}
