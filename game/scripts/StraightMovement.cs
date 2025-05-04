using Godot;
using System;

// This class is the entry point of the game.
public class StraightMovement : Node, IMovement
{
    private Vector2 _baseDirection = Vector2.Zero;

    [Export]
    public float Speed { get; set; } = 100; // in pixels per second

    public void ChangeDirection(Vector2 newDirection)
    {
        _baseDirection = newDirection.Normalized();
    }

    public Vector2 CalculateVector(float delta)
    {
        // Note: Does not need to multiply by delta
        return _baseDirection * Speed;
    }

}
