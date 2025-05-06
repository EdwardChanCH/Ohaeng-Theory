using Godot;
using System;

public class MainMenuUI : Node
{
    public void _OnPlayButtonPressed()
    {
        ScreenManager.SwitchToNextScreen(ScreenManager.GameplayScreenPath, GetTree().Root);
    }

    public void _OnSettingButtonPressed()
    {
        
    }
}
