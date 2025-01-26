using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class IntVariable : GameVariable<int>
{
    [Export] public int DefaultValue {
        get => defaultValue;
        set => defaultValue = value;
    }
}
