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


        if (Globals.TempData.ContainsKey("HighScore"))
        {
            _highScoreLabel.Text = Globals.TempData["HighScore"];
        }
        _CurrentScoreLabel.Text = Globals.TempData["CurrentScore"];
    }

    public override void _Ready()
    {
        GetTree().Paused = true;

    }

    public override void _ExitTree()
    {
        GetTree().Paused = false;
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
