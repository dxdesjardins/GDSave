using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public partial class ButtonListener : Button
{
    [Export] private GameAction[] buttonPressedActions;
    [Export] private GameAction[] buttonToggledActionsBool;
    [Export] private GameAction[] buttonDownActions;
    [Export] private GameAction[] buttonUpActions;
    [Export] private GameAction[] mouseEnteredAction;
    [Export] private GameAction[] mouseExitedAction;

    public override void _EnterTree() {
        RequestReady();
    }

    public override void _ExitTree() {
        this.ButtonDown -= InvokeButtonDownActions;
        this.ButtonUp -= InvokeButtonUpActions;
        this.Pressed -= InvokeButtonPressedActions;
        this.Toggled -= InvokeButtonToggledActionsBool;
        this.MouseEntered -= InvokeMouseEnteredActions;
        this.MouseExited -= InvokeMouseExitedActions;
    }

    public override void _Ready() {
        this.ButtonDown += InvokeButtonDownActions;
        this.ButtonUp += InvokeButtonUpActions;
        this.Pressed += InvokeButtonPressedActions;
        this.Toggled += InvokeButtonToggledActionsBool;
        this.MouseEntered += InvokeMouseEnteredActions;
        this.MouseExited += InvokeMouseExitedActions;
    }

    public void InvokeButtonDownActions() {
        buttonDownActions.Invoke(this);
    }

    public void InvokeButtonUpActions() {
        buttonUpActions.Invoke(this);
    }

    public void InvokeButtonPressedActions() {
        buttonPressedActions.Invoke(this);
    }

    public void InvokeButtonToggledActionsBool(bool state) {
        buttonToggledActionsBool.Invoke<bool>(state, this);
    }

    public void InvokeMouseEnteredActions() {
        mouseEnteredAction.Invoke(this);
    }

    public void InvokeMouseExitedActions() {
        mouseExitedAction.Invoke(this);
    }
}
