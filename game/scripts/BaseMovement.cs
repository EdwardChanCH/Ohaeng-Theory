using Godot;
using System;

// This class is the entry point of the game.
public abstract class BaseMovement : Node, IMovement
{
    protected Vector2 _direction = Vector2.Zero;

    [Export]
    public virtual Vector2 Direction
    {
        get { return _direction; }
        set { _direction = value.Normalized(); }
    }

    [Export]
    public virtual float Speed { get; set; } = 1000; // in pixels per second

    public virtual Vector2 CalculateVector(float delta)
    {
        // Note: Does not need to multiply by delta
        return Direction * Speed;
    }

}
