using Godot;
using System;

public class GameplayScreen : Node2D
{
    [Export]
    public NodePath GameplayUIPath = new NodePath();
    private static GameplayUI _gameplayUI;

    [Export]
    public NodePath EnemyManagerPath = new NodePath();
    public static EnemyManager EnemyManager;

    public static Node2D PlayerRef;



    public override void _EnterTree()
    {
        _gameplayUI = GetNode<GameplayUI>(GameplayUIPath);
        EnemyManager = GetNode<EnemyManager>(EnemyManagerPath);
        AudioManager.PlayBMG("res://assets/sfx/bgm/unwritten_return_fast.wav", 0.25f);
    }

    public override void _Ready()
    {
        _gameplayUI.UpdateScoreLabel();
    }

    public override void _ExitTree()
    {
        if(_gameplayUI == GetNode<GameplayUI>(GameplayUIPath))
        {
            _gameplayUI = null;
        }
        //SaveScore();
    }

    public void _OnPlayerDeath()
    {
        //SaveScore();
        //ScreenManager.AddPopupToScreen(ScreenManager.LoseScreenPath);
        CallDeferred("OpenLoseScreen");
    }

    public void OpenLoseScreen()
    {
        ScreenManager.AddPopupToScreen(ScreenManager.LoseScreenPath);
    }

/*     public void SaveScore()
    {
        Globals.TempData["CurrentScore"] = Globals.Score.ToString();

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
    } */

    public void _OnDespawnAreaBodyExited(Node body)
    {
        if (body is IHarmful harmful)
        {
            harmful.Kill(); // Works on Bullet, Enemy, and Lesser Enemy
        }
    }

    public void _OnFailSafeBodyExited(Node body)
    {
        if (body is IHarmful harmful)
        {
            harmful.Kill(); // Works on Bullet, Enemy, and Lesser Enemy
        }
    }

}
