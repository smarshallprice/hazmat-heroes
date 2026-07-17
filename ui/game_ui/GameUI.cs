using Godot;

namespace MatchTastic;

[GlobalClass]
public partial class GameUI : CanvasLayer
{
    [Export]
    public EnemyManager EnemyManager { get; set; }

    private Label _timerLabel;
    private Label _roundLabel;

    public override void _Ready()
    {
        _timerLabel = GetNode<Label>("%TimerLabel");
        _roundLabel = GetNode<Label>("%RoundLabel");
        EnemyManager.RoundChanged += OnRoundBegan;
    }

    public override void _Process(double _delta)
    {
        _timerLabel.Text = Mathf.CeilToInt(EnemyManager.GetRoundTimeRemaining()).ToString();
    }

    private void OnRoundBegan(int roundCount)
    {
        _roundLabel.Text = $"Round {roundCount}";
    }
}
