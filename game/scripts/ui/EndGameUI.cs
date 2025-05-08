using Godot;
using System;

public class EndGameUI : Node
{
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
