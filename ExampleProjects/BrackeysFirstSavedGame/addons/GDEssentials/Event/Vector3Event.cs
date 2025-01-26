using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class Vector3Event : ParamEvent<Vector3>
{
    [Export] private Vector3 Value {
        get => DefaultParameter;
        set => DefaultParameter = value;
    }
}
