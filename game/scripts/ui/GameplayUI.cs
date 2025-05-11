using Godot;
using System;

public class GameplayUI : Node
{
    [Export]
    public NodePath ScoreLabelPath = new NodePath();
    private static Label _scoreLabel;

    [Export]
    public NodePath WaveLabelPath = new NodePath();
    private static Label _waveLabel;

    public override void _EnterTree()
    {
        _scoreLabel = GetNode<Label>(ScoreLabelPath);
        _waveLabel = GetNode<Label>(WaveLabelPath);

        if (_scoreLabel == null || _waveLabel == null)
        {
            GD.PrintErr("Error: GameplayUI is mmissing export variables.");
            return;
        }
    }

    public void UpdateScoreLabel(int score)
    {
        _scoreLabel.Text = $"{score}";
    }

    public void _OnEnemyManagerWaveNumberChanged(int waveNumber)
    {
        _waveLabel.Text = $"Wave {waveNumber}";
    }
}
