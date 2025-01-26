using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class AInvokeGameActionsAsync : GameAction
{
    [Export] private DelayType delayType = DelayType.Milliseconds;
    [Export] private int delay;
    [Export] private GameAction[] gameActions;

    public enum DelayType
    {
        Milliseconds = 0,
        Seconds = 1,
        ProcessFrames = 2,
        PhysicsProcessFrames = 3,
    }

    public override async void Invoke(Node node) {
        switch (delayType) {
            case DelayType.Seconds:
                await Task.Delay(delay * 1000);
                gameActions.Invoke(node);
                break;
            case DelayType.Milliseconds:
                await Task.Delay(delay);
                gameActions.Invoke(node);
                break;
            case DelayType.ProcessFrames:
                await GDE.CallDeferred(() => gameActions.Invoke(node), delay);
                break;
            case DelayType.PhysicsProcessFrames:
                await GDE.CallDeferredPhysics(() => gameActions.Invoke(node), delay);
                break;
        }
    }
}
