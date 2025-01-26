using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class AInvokeVector3Event : GameAction
{
    [Export] Vector3Event vector3Event;
    [Export] Vector3 param;

    public override void Invoke(Node node) {
        vector3Event.Invoke(param);
    }
}
