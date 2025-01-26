using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public abstract partial class GameEvent : Resource
{
    public virtual void Invoke() { }

    public virtual void AddListener(Action listener) { }

    public virtual void RemoveListener(Action listener) { }

    public virtual void AddListener(GameEventListener listener) { }

    public virtual void RemoveListener(GameEventListener listener) { }
}
