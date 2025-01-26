using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class StaticEvent : GameEvent
{
    private List<GameEventListener> eventListeners = new List<GameEventListener>();
    private List<Action> scriptEventListeners = new List<Action>();

    public override void Invoke() {
        for (int i = 0; i < scriptEventListeners.Count; i++)
            scriptEventListeners[i].Invoke();
        for (int i = eventListeners.Count - 1; i >= 0; i--)
            eventListeners[i].Dispatch();
    }

    public override void AddListener(Action listener) {
        scriptEventListeners.Add(listener);
    }

    public override void RemoveListener(Action listener) {
        scriptEventListeners.Remove(listener);
    }

    public override void AddListener(GameEventListener listener) {
        if (!eventListeners.Contains(listener))
            eventListeners.Add(listener);
    }

    public override void RemoveListener(GameEventListener listener) {
        if (!eventListeners.Contains(listener))
            eventListeners.Remove(listener);
    }
}
