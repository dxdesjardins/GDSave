using Godot;

namespace FirstGame.Scripts;

public sealed partial class Player : CharacterBody2D
{
    private const float JumpVelocity = -300F;
    private const float Speed = 130F;
    private static readonly StringName jump = "jump";
    private static readonly StringName moveLeft = "move_left";
    private static readonly StringName moveRight = "move_right";
    private static readonly StringName idle = "idle";
    private static readonly StringName run = "run";
    private AnimatedSprite2D _animatedSprite = default!;

    private float _gravity = 0F;

    public override void _Ready()
    {
        _animatedSprite = this.GetNodeOrThrow<AnimatedSprite2D>("AnimatedSprite2D");
        _gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    }

    public override void _PhysicsProcess(double delta)
    {
        var isOnFloor = IsOnFloor();
        var velocity = Velocity;
        if (!isOnFloor)
        {
            velocity = velocity with
            {
                Y = velocity.Y + _gravity * (float) delta
            };
        }
        else if (Input.IsActionJustPressed(jump))
        {
            velocity = velocity with
            {
                Y = JumpVelocity
            };
        }

        var direction = Input.GetAxis(moveLeft, moveRight);
        _animatedSprite.FlipH = direction switch
                                {
                                    > 0 => false,
                                    < 0 => true,
                                    _   => _animatedSprite.FlipH
                                };

        if (isOnFloor)
        {
            _animatedSprite.Play(direction == 0F ? idle : run);
        }
        else
        {
            _animatedSprite.Play(jump);
        }

        if (direction != 0F)
        {
            velocity = velocity with
            {
                X = direction * Speed
            };
        }
        else
        {
            velocity = velocity with
            {
                X = Mathf.MoveToward(velocity.X, 0, Speed)
            };
        }

        Velocity = velocity;
        MoveAndSlide();
    }
}
