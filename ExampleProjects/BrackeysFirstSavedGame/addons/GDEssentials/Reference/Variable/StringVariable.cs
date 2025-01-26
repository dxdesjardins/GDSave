using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class StringVariable : GameVariable<string>
{
    public StringVariable() {
        defaultValue = "";
    }

    [Export] public string DefaultValue {
        get => defaultValue;
        set => defaultValue = value;
    }
}
