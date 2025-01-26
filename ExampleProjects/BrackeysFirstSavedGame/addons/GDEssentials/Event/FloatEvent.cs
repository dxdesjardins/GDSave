using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class FloatEvent : ParamEvent<float>
{
    [Export] private float Value {
        get => DefaultParameter;
        set => DefaultParameter = value;
    }
}
