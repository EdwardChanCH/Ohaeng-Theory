using Godot;
using System;

public class GameplayUI : Node
{
    [Export]
    public NodePath HintLabelPath = new NodePath();
    private Label _hintLabel;

    [Export]
    public NodePath ScoreLabelPath = new NodePath();
    private Label _scoreLabel;

    [Export]
    public NodePath WaveLabelPath = new NodePath();
    private Label _waveLabel;

    [Export]
    public NodePath HighestWaveLabelPath = new NodePath();
    private Label _highestWaveLabel;

    [Export]
    public NodePath PlayerHealthBarPath = new NodePath();
    private ProgressBar _playerHealthBar;

    [Export]
    public NodePath EnemyProgressBarPath = new NodePath();
    private ProgressBar _enemyProgressBar;


    public override void _EnterTree()
    {
        _hintLabel = GetNode<Label>(HintLabelPath);
        _scoreLabel = GetNode<Label>(ScoreLabelPath);
        _waveLabel = GetNode<Label>(WaveLabelPath);
        _highestWaveLabel = GetNode<Label>(HighestWaveLabelPath);
        _playerHealthBar = GetNode<ProgressBar>(PlayerHealthBarPath);
        _enemyProgressBar = GetNode<ProgressBar>(EnemyProgressBarPath);
    }

    public override void _Ready()
    {
        base._Ready();

        Globals.Singleton.Connect("ScoreChanged", this, nameof(UpdateScoreLabel));

        GameplayScreen.PlayerRef.Connect("HealthUpdate", this, "_OnHealthUpdate");
        GameplayScreen.EnemyManager.Connect("WaveProgressChanged", this, "_WaveProgressChanged");


    }


    public void UpdateScoreLabel()
    {
        _scoreLabel.Text = $"{Globals.Score}";

        GD.Print("Score label updated");
    }

    public void _OnEnemyManagerWaveNumberChanged(int waveNumber)
    {
        _waveLabel.Text = $"{waveNumber}";

        string highestWave = Globals.GameData["HighestCompletedWave"];
        if (highestWave == "-1")
        {
            highestWave = "n/a";
        }
        _highestWaveLabel.Text = $"{highestWave}";
    }

    public void _OnHealthUpdate(int newHealth)
    {
        _playerHealthBar.Value = (float)newHealth / (float)GameplayScreen.PlayerRef.PlayerHealthComponent.MaxHealth;
    }

    public void _WaveProgressChanged(int currentHealth, int maxHealth)
    {
        _enemyProgressBar.Value = (float)currentHealth / (float)maxHealth;
    }

    public void ToggleHintVisible(bool visible)
    {
        _hintLabel.Visible = visible;
    }
}
