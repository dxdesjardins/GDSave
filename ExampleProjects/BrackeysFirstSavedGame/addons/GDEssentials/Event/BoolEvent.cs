using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class BoolEvent : ParamEvent<bool>
{
    [Export] private bool Value {
        get => DefaultParameter;
        set => DefaultParameter = value;
    }
}
