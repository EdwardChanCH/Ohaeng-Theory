using Godot;
using System;

public class GameplayScreen : Node2D
{
    [Export]
    public NodePath GameplayUIPath = new NodePath();
    private static GameplayUI _gameplayUI;


    public static Node2D PlayerRef;

    private static int _score;
    public static int Score 
    {
        get { return _score; } 
        set 
        {
            _score = value;
            _gameplayUI.UpdateScoreLabel(_score);
        } 
    }



    public override void _EnterTree()
    {
        _gameplayUI = GetNode<GameplayUI>(GameplayUIPath);
        AudioManager.PlayBMG("res://assets/sfx/bgm/unwritten_return_fast.wav", 0.25f);
    }

    public override void _Ready()
    {
        Score = 0;
    }

    public override void _ExitTree()
    {
        if(_gameplayUI == GetNode<GameplayUI>(GameplayUIPath))
        {
            _gameplayUI = null;
        }
        SaveScore();
    }

    public void _OnPlayerDeath()
    {
        SaveScore();
        //ScreenManager.AddPopupToScreen(ScreenManager.LoseScreenPath);
        CallDeferred("OpenLoseScreen");
    }

    public void OpenLoseScreen()
    {
        ScreenManager.AddPopupToScreen(ScreenManager.LoseScreenPath);
    }

    public void SaveScore()
    {
        Globals.TempData["CurrentScore"] = Score.ToString();

        if (!Globals.TempData.ContainsKey("HighScore"))
        {
            Globals.TempData.Add("HighScore", Score.ToString());
        }
        else
        {
            if (Globals.TempData["HighScore"].ToInt() < Score)
            {
                Globals.TempData["HighScore"] = Score.ToString();
            }
        }
    }

    public void _OnDespawnAreaBodyExited(Node body)
    {
        if (body is IHarmful harmful)
        {
            harmful.Kill(); // Works on Bullet, Enemy, and Lesser Enemy
        }
    }

}
