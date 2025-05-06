using Godot;
using System;

public class MainMenuUI : Node
{
    public void _OnPlayButtonPressed()
    {
        ScreenManager.RebaseScreen(ScreenManager.GameplayScreenPath, GetTree().Root);
    }

    public void _OnSettingButtonPressed()
    {

    }
}
