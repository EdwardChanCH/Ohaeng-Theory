using Godot;
using System;

public class MainMenuScreen : Node2D
{

    [Export]
    public NodePath MCPath { get; private set; } = new NodePath();
    public Node2D MCNode { get; private set; }

    private float _cycle = 0;
    private float _pi2 = (float)Math.PI * 2;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        MCNode = GetNode<Node2D>(MCPath);
    }

    public override void _Process(float delta)
    {
        MCNode.GlobalPosition += Vector2.Up * 0.25f * (float)Math.Sin(_cycle);

        _cycle += delta;
        if (_cycle > _pi2)
        {
            _cycle -= _pi2;
        }
    }
}
