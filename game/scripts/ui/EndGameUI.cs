using Godot;
using System;

public class EndGameUI : Node
{
    [Export]
    public NodePath HighScoreLabelPath = new NodePath();
    private Label _highScoreLabel;

    [Export]
    public NodePath CurrentScoreLabelPath = new NodePath();
    private Label _CurrentScoreLabel;

    public override void _EnterTree()
    {
        _highScoreLabel = GetNode<Label>(HighScoreLabelPath);
        _CurrentScoreLabel = GetNode<Label>(CurrentScoreLabelPath);

        _highScoreLabel.Text = $"{Globals.HighestScore}";
        _CurrentScoreLabel.Text = $"{Globals.Score}";
    }

    public override void _Ready()
    {
        GetTree().Paused = true;

    }

    public override void _ExitTree()
    {
        GetTree().Paused = false;
        _highScoreLabel = null;
        _CurrentScoreLabel = null;
    }

    public void _OnRestartButtonPressed()
    {
        ScreenManager.ReloadCurrentScreen();
        QueueFree();
    }

    public void _OnMainMenuButtonPressed()
    {
        ScreenManager.SwitchToNextScreen(ScreenManager.MainMenuScreenPath, GetTree().Root);
    }
}
