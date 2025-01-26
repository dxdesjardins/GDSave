using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public partial class ActivationListener : Node {
    [Export] private GameAction[] enterTreeActions;
    [Export] private GameAction[] enterTreeReadyActions;
    [Export] private GameAction[] exitTreeActions;
    [Export] private GameAction[] readyActions;
    [Export] private GameAction[] invokeActions;
    private bool readyTriggered = false;

    public override void _EnterTree() {
        enterTreeActions?.Invoke(this);
        RequestReady();
    }

    public override void _ExitTree() {
        exitTreeActions?.Invoke(this);
    }

    public override void _Ready() {
        if (!readyTriggered) {
            readyTriggered = true;
            readyActions?.Invoke(this);
        }
        enterTreeReadyActions?.Invoke(this);
    }

    public void Invoke() {
        invokeActions?.Invoke(this);
    }
}
