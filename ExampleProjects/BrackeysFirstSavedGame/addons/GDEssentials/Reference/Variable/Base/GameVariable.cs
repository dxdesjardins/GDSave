using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public abstract partial class GameVariable<T> : Resource
{
    protected T defaultValue;
    private T runtimeValue;
    private bool initialized;

    public T Value {
        get	{
            if (Engine.IsEditorHint())
                return defaultValue;
            if (!initialized) {
                runtimeValue = defaultValue;
                initialized = true;
            }
            return runtimeValue;
        }
        set {
            if (Engine.IsEditorHint()) {
                defaultValue = value;
                return;
            }
            initialized = true;
            runtimeValue = value;
        }
    }

    public void SetValue (T value) {
        this.Value = value;
    }
}
