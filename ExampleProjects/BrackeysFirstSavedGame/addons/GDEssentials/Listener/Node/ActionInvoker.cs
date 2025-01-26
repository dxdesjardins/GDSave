using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public partial class ActionInvoker : Node {
    [Export] private GameAction[] actions;

    public void Invoke() {
        actions?.Invoke(this);
    }
}
