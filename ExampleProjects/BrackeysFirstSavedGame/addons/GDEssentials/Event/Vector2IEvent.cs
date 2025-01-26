using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[Tool]
[GlobalClass]
public partial class Vector2IEvent : ParamEvent<Vector2I>
{
    [Export] private Vector2I Value {
        get => DefaultParameter;
        set => DefaultParameter = value;
    }
}
