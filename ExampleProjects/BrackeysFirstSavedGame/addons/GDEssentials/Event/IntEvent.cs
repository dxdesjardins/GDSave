using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class IntEvent : ParamEvent<int>
{
    [Export] private int Value {
        get => DefaultParameter;
        set => DefaultParameter = value;
    }
}
