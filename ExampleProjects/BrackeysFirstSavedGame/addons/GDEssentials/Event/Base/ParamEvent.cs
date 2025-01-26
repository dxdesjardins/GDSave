using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public abstract partial class ParamEvent<T> : GameEvent
{
    [Export] protected bool dispatchLastStateOnAdd = false;
    protected List<ParamEventListener<T>> eventListeners = new();
    protected List<Action<T>> scriptEventListeners = new();
    private T defaultParameter;
    public T DefaultParameter {
        get => defaultParameter;
        protected set {
            defaultParameter = value;
            lastParameter = value;
        }
    }
    protected T lastParameter;
    public virtual T LastParameter => (lastParameter != null) ? lastParameter : InvokingParam;
    protected bool hasParameter;
    public bool HasParameter { get { return hasParameter || IsInvoking; } }
    public bool IsInvoking { get; protected set; }
    public T InvokingParam { get; protected set; }

    public virtual void Invoke(T param) {
        IsInvoking = true;
        InvokingParam = param;
        for (int i = scriptEventListeners.Count - 1; i >= 0; i--)
            scriptEventListeners[i].Invoke(param);
        for (int i = eventListeners.Count - 1; i >= 0; i--)
            eventListeners[i].Dispatch(param);
        lastParameter = param;
        hasParameter = true;
        IsInvoking = false;
        InvokingParam = default;
    }

    public override void AddListener(Action listener) {
        Action<T> paramAction = (_) => { listener.Invoke(); };
        AddListener(paramAction);
    }

    public virtual void AddListener(Action<T> listener) {
        scriptEventListeners.Add(listener);
        if (dispatchLastStateOnAdd && hasParameter)
            listener.Invoke(lastParameter);
    }

    public virtual void RemoveListener(Action<T> listener) {
        scriptEventListeners.Remove(listener);
    }

    public virtual void AddListener(ParamEventListener<T> listener) {
        if (!eventListeners.Contains(listener))
            eventListeners.Add(listener);
        if (dispatchLastStateOnAdd && hasParameter)
            listener.Dispatch(lastParameter);
    }

    public virtual void RemoveListener(ParamEventListener<T> listener) {
        eventListeners.Remove(listener);
    }
}
