using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class APrint : ParamAction<string>
{
    [Export] private string message;

    public override void Invoke(string param, Node node) {
        if (string.IsNullOrEmpty(message))
            GDE.Log(message);
        else
            GDE.Log(param);
    }

    public override void Invoke(Node node) => Invoke(message, node);
}
