using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public partial class VisibilityListener : Node
{
    [Export] private GameAction[] visibleActions;
    [Export] private GameAction[] invisibleActions;
    [Export] private bool invokeOnReady = false;
    [Export] private bool invokeOnExitTree = false;

    public override void _EnterTree() {
        this.GetParent<CanvasItem>().VisibilityChanged += ParentVisibilityChanged;
        RequestReady();
    }

    public override void _ExitTree() {
        this.GetParent<CanvasItem>().VisibilityChanged -= ParentVisibilityChanged;
        if (invokeOnExitTree)
            invisibleActions.Invoke(this);
    }

    public override void _Ready() {
        if (!invokeOnReady)
            return;
        if (this.GetParent<CanvasItem>().Visible == true)
            visibleActions.Invoke(this);
        else
            invisibleActions.Invoke(this);
    }

    private void ParentVisibilityChanged() {
        if (this.GetParent<CanvasItem>().Visible == true)
            visibleActions.Invoke(this);
        else
            invisibleActions.Invoke(this);
    }
}
