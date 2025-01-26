using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class NodeEvent : ParamEvent<Node>
{
    private new WeakReference<Node> lastParameter;
    public override Node LastParameter {
        get {
            if (lastParameter != null && lastParameter.TryGetTarget(out Node target))
                return target;
            return InvokingParam;
        }
    }

    public override void Invoke(Node param) {
        IsInvoking = true;
        InvokingParam = param;
        for (int i = scriptEventListeners.Count - 1; i >= 0; i--)
            scriptEventListeners[i].Invoke(param);
        for (int i = eventListeners.Count - 1; i >= 0; i--)
            eventListeners[i].Dispatch(param);
        lastParameter = new WeakReference<Node>(param);
        hasParameter = true;
        IsInvoking = false;
        InvokingParam = default;
    }

    public override void AddListener(Action listener) {
        Action<Node> paramAction = (_) => { listener.Invoke(); };
        AddListener(paramAction);
    }

    public override void AddListener(Action<Node> listener) {
        scriptEventListeners.Add(listener);
        if (dispatchLastStateOnAdd && hasParameter)
            listener.Invoke(LastParameter);
    }

    public override void RemoveListener(Action<Node> listener) {
        scriptEventListeners.Remove(listener);
    }

    public override void AddListener(ParamEventListener<Node> listener) {
        if (!eventListeners.Contains(listener))
            eventListeners.Add(listener);
        if (dispatchLastStateOnAdd && hasParameter)
            listener.Dispatch(LastParameter);
    }

    public override void RemoveListener(ParamEventListener<Node> listener) {
        eventListeners.Remove(listener);
    }
}
