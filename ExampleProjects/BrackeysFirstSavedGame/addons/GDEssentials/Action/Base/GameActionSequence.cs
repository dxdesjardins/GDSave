using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class GameActionSequence : GameAction
{
    [Export] GameAction[] gameActions;

    public override void Invoke(Node node) {
        gameActions.Invoke(node);
    }
}
