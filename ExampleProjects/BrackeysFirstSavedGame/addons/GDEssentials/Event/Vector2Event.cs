using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class Vector2Event : ParamEvent<Vector2>
{
    [Export] private Vector2 Value {
        get => DefaultParameter;
        set => DefaultParameter = value;
    }
}
