using Godot;
using System;

public class EndGameUI : Node
{
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
