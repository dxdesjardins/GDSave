using Godot;
using Chomp.Essentials;
using Chomp.Save;

namespace FirstGame.Scripts;

public sealed partial class KillZone : Area2D
{
    private const double NormalTimeScale = 1D;
    private const double SlowTimeScale = NormalTimeScale / 2D;
    private Timer _timer = default!;
    
    public override void _Ready()
    {
        _timer = this.GetNodeOrThrow<Timer>("Timer");
    }
    
    private async void HandleTimerTimeout()
    {
        Engine.TimeScale = NormalTimeScale;
        //GetTree().ReloadCurrentScene();
        await StageManager.UnloadAllStages();
        SaveManager.ClearActiveSaveData();
        StageManager.LoadStage("uid://cuv4vme4rwno0");
    }
    
    private void HandleBodyEntered(Node2D body)
    {
        GD.Print("You died!");
        Engine.TimeScale = SlowTimeScale;
        body.GetNode<CollisionShape2D>("CollisionShape2D").QueueFree();
        _timer.Start();
    }
}
