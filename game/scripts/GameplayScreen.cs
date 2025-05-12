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

    [Export]
    public NodePath PlayerPath = new NodePath();
    public static PlayerCharacter PlayerRef;



    public override void _EnterTree()
    {
        _gameplayUI = GetNode<GameplayUI>(GameplayUIPath);
        EnemyManager = GetNode<EnemyManager>(EnemyManagerPath);
        PlayerRef = GetNode<PlayerCharacter>(PlayerPath);

        EnemyManager.Connect("WaveComplete", PlayerRef, "_OnWaveComplete"); 
        //EnemyManager.Connect("WaveBegin", this, "_OnWaveBegin");
        AudioManager.PlayBMG("res://assets/sfx/bgm/unwritten_return_fast.wav", 0.25f);
    }


    public void _OnWaveBegin()
    {
        Globals.SetScore(0);
        _gameplayUI.ToggleHintVisible(false);
    }

    public void _OnWaveCancel()
    {
        _gameplayUI.ToggleHintVisible(true);
    }

    public void _OnWaveComplete()
    {
        _gameplayUI.ToggleHintVisible(true);
    }

    public override void _Ready()
    {
        _gameplayUI.UpdateScoreLabel();
        _gameplayUI.ToggleHintVisible(true);
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
