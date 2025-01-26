using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chomp.Essentials;

public partial class StageTransition : CanvasLayer
{
    [Export] private AnimationPlayer animationPlayer;
    public static bool IsTransitioning { get; private set; }

    public static async Task StartTransition(PackedScene stageTransition) {
        if (IsTransitioning) {
            GDE.LogErr("Failed to start stage transition. A transition is already in process.");
            return;
        }
        IsTransitioning = true;
        StageManager.StageRoot.InstantiateChild(stageTransition);
        TaskCompletionSource<Node> fadeOutTcs = new();
        void awaitFadeOut(Node node) {
            fadeOutTcs.TrySetResult(node);
            StageManager.TransitionAfterFadeOut -= awaitFadeOut;
        }
        StageManager.TransitionAfterFadeOut += awaitFadeOut;
        await fadeOutTcs.Task;
        IsTransitioning = false;
    }

    public override void _EnterTree() => RequestReady();

    public override async void _Ready() {
        animationPlayer ??= this.GetComponent<AnimationPlayer>();
        StageManager.TransitionBeforeFadeOut.Invoke(this);
        animationPlayer.Stop();
        animationPlayer.Play("fade_out");
        await ToSignal(animationPlayer, AnimationPlayer.SignalName.AnimationFinished);
        // It takes one extra frame for the animation to finish
        await GDE.Yield();
        StageManager.TransitionAfterFadeOut.Invoke(this);
        TaskCompletionSource<Node> tcs = new();
        void action(Node node) {
            tcs.TrySetResult(node);
            StageManager.LoadingComplete -= action;
        }
        StageManager.LoadingComplete += action;
        await tcs.Task;
        // Wait one frame for deferred stage processing
        await GDE.Yield();
        StageManager.TransitionBeforeFadeIn.Invoke(this);
        if (animationPlayer.HasAnimation("fade_in"))
            animationPlayer.Play("fade_in");
        else
            animationPlayer.Play("fade_out", default, -1, true);
        await ToSignal(animationPlayer, AnimationPlayer.SignalName.AnimationFinished);
        StageManager.TransitionAfterFadeIn.Invoke(this);
        this.Remove();
    }
}
