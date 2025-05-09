using Godot;
using System;

public class GameplayUI : Node
{
    [Export]
    public NodePath ScoreLabelPath = new NodePath();
    private static Label _scoreLabel;

    public override void _Ready()
    {
        _scoreLabel = GetNode<Label>(ScoreLabelPath);
    }

    public void UpdateScoreLabel(int score)
    {
        _scoreLabel.Text = score.ToString();
    }
}
