using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class StringEvent : ParamEvent<string>
{
    [Export] private string Value {
        get => DefaultParameter;
        set => DefaultParameter = value;
    }
}
