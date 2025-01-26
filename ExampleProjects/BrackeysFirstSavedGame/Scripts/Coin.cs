using Godot;

namespace FirstGame.Scripts;

public sealed partial class Coin : Area2D
{
	private static readonly StringName pickupAnimation = "pickup";
	private GameManager _gameManager = default!;
	private AnimationPlayer _animationPlayer = default!;
	
	public override void _Ready()
	{
		_gameManager = this.GetNodeOrThrow<GameManager>("%GameManager");
		_animationPlayer = this.GetNodeOrThrow<AnimationPlayer>("AnimationPlayer");
	}

	private void HandleBodyEntered(Node2D body)
	{
		_gameManager.AddPoint();
		_animationPlayer.Play(pickupAnimation);
	}
}