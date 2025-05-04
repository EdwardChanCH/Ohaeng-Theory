using Godot;
using System;
using System.Collections.Generic;

// This singleton class manages the transition between game screens.
// Note: Godot autoload requires Node type.
public class ScreenManager : Node
{
    // Screen paths
    public const string TitleScreenPath = "TODO";
    public const string GameplayScreenPath = "res://scenes/screens/gameplay_screen.tscn";
    public const string PauseScreenPath = "TODO"; // popup?
    public const string WinScreenPath = "TODO"; // popup?
    public const string LoseScreenPath = "TODO"; // popup?

    public const string SettingsScreenPath = "TODO"; // popup?

    public static ScreenManager Singleton { get; private set; }

    // The current loaded screen (not a popup) (may add popups as children)
    public static Node CurrentScreen { get; private set; } = null;

    // Optimization
    private static Dictionary<string, PackedScene> _cachedScenes = new Dictionary<string, PackedScene>();

    // History of loaded screens
    private static Stack<string> _screenHistory = new Stack<string>();

    // Load a new screen or popup.
    // Note: this does not update the current screen or screen history.
    public static Node Load(string scenePath, Node attachTo)
    {
        // Check if that scene is cached
        if (!_cachedScenes.ContainsKey(scenePath))
        {
            _cachedScenes[scenePath] = GD.Load<PackedScene>(scenePath);
        }

        // Instanciate the scene
        Node newScreen = _cachedScenes[scenePath].Instance();

        // Attatch to parent (at the end of frame)
        attachTo.CallDeferred("add_child", newScreen);

        return newScreen;
    }

    // Load a new screen and mark as current screen, unload the previous screen and update screen history.
    public static void SwitchToNextScreen(string scenePath, Node attachTo)
    {
        CurrentScreen?.QueueFree();

        _screenHistory.Push(scenePath);

        CurrentScreen = Load(scenePath, attachTo);
    }

    // Load the previous screen and mark as current screen, unload the current screen and update screen history.
    public static void SwitchToPreviousScreen(Node attachTo)
    {
        if (_screenHistory.Count <= 1)
        {
            GD.Print("Warning: Cannot switch screen; previous screen does not exist.");
            return;
        }

        CurrentScreen?.QueueFree();

        _screenHistory.Pop();

        CurrentScreen = Load(_screenHistory.Peek(), attachTo);
    }

    // Load a new screen and mark as current screen, unload the current screen and clears screen history.
    public static void RebaseScreen(string scenePath, Node attachTo)
    {
        // Free the current scene (if exist)
        CurrentScreen?.QueueFree();

        _screenHistory.Clear();
        _screenHistory.Push(scenePath);

        CurrentScreen = Load(scenePath, attachTo);
    }

    // Reload the current scene.
    public static void ReloadCurrentScreen()
    {
        if (CurrentScreen == null)
        {
            GD.Print("Warning: Cannot reload screen; current screen does not exist.");
            return;
        }

        Node screenParent = CurrentScreen.GetParent();

        CurrentScreen?.QueueFree();

        CurrentScreen = Load(_screenHistory.Peek(), screenParent);
    }

    // Load a new popup and attach it to the current scene
    // Returns the popup node (can be null) (useful for auto-close via timer)
    public static Node AddPopupToScreen(string scenePath)
    {
        if (CurrentScreen == null)
        {
            GD.Print("Warning: Cannot attach popup; current screen does not exist.");
            return null;
        }

        return Load(scenePath, CurrentScreen);
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Singleton = this;
    }
}
