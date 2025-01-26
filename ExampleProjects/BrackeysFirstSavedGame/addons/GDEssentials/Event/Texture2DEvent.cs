using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class Texture2DEvent : ParamEvent<Texture2D>
{
    [Export] private Texture2D Value {
        get => DefaultParameter;
        set => DefaultParameter = value;
    }
}