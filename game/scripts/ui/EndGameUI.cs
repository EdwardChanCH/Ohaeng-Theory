using Godot;
using System;

public class EndGameUI : Node
{
    [Export]
    public NodePath ScoreLabelPath = new NodePath();
    private Label _scoreLabel;

    public override void _EnterTree()
    {
        _scoreLabel = GetNode<Label>(ScoreLabelPath);
        if (!Globals.TempData.ContainsKey("HighScore"))
        {
            _scoreLabel.Text = Globals.TempData["HighScore"];
        }
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
