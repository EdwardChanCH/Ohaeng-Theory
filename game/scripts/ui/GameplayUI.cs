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

    [Export]
    public NodePath HighestWaveLabelPath = new NodePath();
    private static Label _highestWaveLabel;

    public override void _Ready()
    {
        base._Ready();

        Globals.Singleton.Connect("ScoreChanged", this, nameof(UpdateScoreLabel));
    }

    public override void _EnterTree()
    {
        _scoreLabel = GetNode<Label>(ScoreLabelPath);
        _waveLabel = GetNode<Label>(WaveLabelPath);
        _highestWaveLabel = GetNode<Label>(HighestWaveLabelPath);

        if (_scoreLabel == null || _waveLabel == null || _highestWaveLabel == null)
        {
            GD.PrintErr("Error: GameplayUI is missing export variables.");
            return;
        }
    }

    public void UpdateScoreLabel()
    {
        _scoreLabel.Text = $"{Globals.Score}";

        GD.Print("Score label updated");
    }

    public void _OnEnemyManagerWaveNumberChanged(int waveNumber)
    {
        _waveLabel.Text = $"Wave {waveNumber}";

        string highestWave = Globals.GameData["HighestCompletedWave"];
        if (highestWave == "-1")
        {
            highestWave = "n/a";
        }
        _highestWaveLabel.Text = $"Best: {highestWave}";
    }
}
