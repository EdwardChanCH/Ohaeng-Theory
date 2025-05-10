using Godot;
using System;

public class MainMenuUI : Node
{
    public override void _Ready()
    {
        AudioManager.PlayBMG("res://assets/sfx/bgm/infinite_perspective_short.wav", 0.25f);
        ProjectileManager.Clear();
        //Score = 0;
    }

    public void _OnPlayButtonPressed()
    {
        ScreenManager.SwitchToNextScreen(ScreenManager.GameplayScreenPath, GetTree().Root);
    }

    public void _OnSettingButtonPressed()
    {
        ScreenManager.AddPopupToScreen(ScreenManager.SettingsScreenPath);
    }
}
