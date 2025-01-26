using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

[GlobalClass]
[Tool]
public partial class APlayAnimation : ParamAction<string>
{
    [ExportGroup("Target")]
    [Export] public NodePath animationPlayer;
    [ExportGroup("Parameters")]
    [Export] public string animation;
    [Export] public float customSpeed = 1;
    [Export] public bool randomPlayBackwards = false;

    public override void Invoke(string param, Node node) {
        AnimationPlayer animationPlayer;
        if (!this.animationPlayer.IsEmpty)
            animationPlayer = node.GetNode<AnimationPlayer>(this.animationPlayer);
        else
            animationPlayer = node.GetParent<AnimationPlayer>();
        animationPlayer.Stop();
        if (randomPlayBackwards && new Random().NextDouble() < 0.5)
            animationPlayer.Play(param, default, -customSpeed, true);
        else
            animationPlayer.Play(param, default, customSpeed);
    }

    public override void Invoke(Node node) => Invoke(animation, node);
}
