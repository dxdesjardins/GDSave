using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public abstract partial class ParamEventListener<T> : Node
{
    [Export] protected bool invokeOnEnable = false;
    [Export] protected bool invokeOnDisable = false;
    protected virtual ParamEvent<T> EventObject { get; }
    [Export] protected GameAction[] eventActions;

    public override void _EnterTree() {
        EventObject?.AddListener(this);
        if (invokeOnEnable)
            ReDispatch();
    }

    public override void _ExitTree() {
        EventObject?.RemoveListener(this);
        if (invokeOnDisable)
            ReDispatch();
    }

    public void ReDispatch() {
        if (EventObject.IsInvoking)
            Dispatch(EventObject.InvokingParam);
        else if (EventObject.HasParameter)
            Dispatch(EventObject.LastParameter);
    }

    public virtual void Dispatch(T parameter) {
        eventActions?.Invoke<T>(parameter, this);
    }
}
