using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class ARemoveScene : GameAction
{
    public override void Invoke(Node node) {
        node.GetScene().Remove();
    }
}
