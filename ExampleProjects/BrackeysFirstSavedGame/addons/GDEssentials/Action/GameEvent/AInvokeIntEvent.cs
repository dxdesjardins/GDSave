using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class AInvokeIntEvent : GameAction
{
    [Export] IntEvent intEvent;
    [Export] int param;

    public override void Invoke(Node node) {
        intEvent.Invoke(param);
    }
}
