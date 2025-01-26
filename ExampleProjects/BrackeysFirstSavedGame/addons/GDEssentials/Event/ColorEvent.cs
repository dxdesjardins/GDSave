using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ColorEvent : ParamEvent<Color>
{
    [Export] private Color Value {
        get => DefaultParameter;
        set => DefaultParameter = value;
    }
}
