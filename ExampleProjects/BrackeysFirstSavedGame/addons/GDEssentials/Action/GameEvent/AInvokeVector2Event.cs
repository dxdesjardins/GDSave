using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class AInvokeVector2Event : GameAction
{
    [Export] Vector2Event vector2Event;
    [Export] Vector2 param;

    public override void Invoke(Node node) {
        vector2Event.Invoke(param);
    }
}
