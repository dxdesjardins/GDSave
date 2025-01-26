using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
public partial class BoolVariable : GameVariable<bool>
{
    [Export] public bool DefaultValue {
        get => defaultValue;
        set => defaultValue = value;
    }
}
