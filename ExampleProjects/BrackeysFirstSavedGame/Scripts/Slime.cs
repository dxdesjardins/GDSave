using Godot;

namespace FirstGame.Scripts;

public sealed partial class Slime : Node2D
{
    private const int Speed = 60;
    
    private int _direction = 1;
    private RayCast2D _rayCaseRight = default!;
    private RayCast2D _rayCastLeft = default!;
    private AnimatedSprite2D _animatedSprite = default!;

    public override void _Ready()
    {
        _rayCaseRight = this.GetNodeOrThrow<RayCast2D>("RayCastRight");
        _rayCastLeft = this.GetNodeOrThrow<RayCast2D>("RayCastLeft");
        _animatedSprite = this.GetNodeOrThrow<AnimatedSprite2D>("AnimatedSprite2D");
    }

    public override void _Process(double delta)
    {
        if (_rayCaseRight.IsColliding())
        {
            _direction = -1;
            _animatedSprite.FlipH = true;
        }
        if (_rayCastLeft.IsColliding())
        {
            _direction = 1;
            _animatedSprite.FlipH = false;
        }

        Position = Position with
        {
            X = Position.X + Speed * _direction * (float) delta
        };
    }
}
