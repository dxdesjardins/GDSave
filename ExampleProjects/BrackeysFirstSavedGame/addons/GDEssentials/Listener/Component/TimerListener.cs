using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public partial class TimerListener : Node
{
    [Export] private Timer timer;
    [Export] private GameAction[] timeoutActions;

    public override void _EnterTree() {
        RequestReady();
    }

    public override void _ExitTree() {
        timer.Timeout -= InvokeTimeoutActions;
    }

    public override void _Ready() {
        timer ??= this.GetParent<Timer>();
        timer.Timeout += InvokeTimeoutActions;
    }

    public void InvokeTimeoutActions() {
        timeoutActions.Invoke(this);
    }
}
