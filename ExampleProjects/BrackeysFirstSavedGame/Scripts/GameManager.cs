using Godot;
using Chomp.Essentials;
using Chomp.Save;

namespace FirstGame.Scripts;

[Tool]
public sealed partial class GameManager : Node, ISaveable
{
    private int _score;
    private Label _scoreLabel = default!;

    [Export] public string SaveableId { get; set; }

    public override void _Ready()
    {
        _score = 0;
        _scoreLabel = this.GetNodeOrThrow<Label>("ScoreLabel");
    }

    public void AddPoint()
    {
        _score++;
        _scoreLabel.Text = $"You collected {_score} coins.";
    }

    public string OnSave() {
        return GDS.Serialize(_score);
    }

    public void OnLoad(string data) {
        _score = GDS.Deserialize<int>(data);
        _scoreLabel.Text = $"You collected {_score} coins.";
    }
}
