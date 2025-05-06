using Godot;
using System;

// This class is the entry point of the game.
public class Main : Node2D
{
    // Note: Use this as the Main() method.
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ScreenManager.SwitchToNextScreen(ScreenManager.MainMenuScreenPath, GetTree().Root);
    }
}
