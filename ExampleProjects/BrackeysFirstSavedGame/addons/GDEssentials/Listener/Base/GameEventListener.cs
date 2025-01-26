using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public partial class GameEventListener : Node
{
    [Export] protected bool invokeOnEnable = false;
    [Export] protected bool invokeOnDisable = false;
    protected virtual StaticEvent EventObject { get; }
    protected virtual GameAction[] EventActions { get; }

    public override void _EnterTree() {
        RequestReady();
    }

    public override void _ExitTree() {
        EventObject?.RemoveListener(this);
        if (invokeOnDisable)
            Dispatch();
    }

    public override void _Ready() {
        EventObject?.AddListener(this);
        if (invokeOnEnable)
            Dispatch();

    }

    public virtual void Dispatch() {
        EventActions.Invoke(this);
    }
}
