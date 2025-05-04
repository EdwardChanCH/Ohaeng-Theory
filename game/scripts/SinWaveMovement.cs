using Godot;
using System;

// This class is the entry point of the game.
public class SinWaveMovement : Node, IMovement
{
    private Vector2 _baseDirection = Vector2.Zero;

    private Vector2 _crossDirection = Vector2.Zero;

    private float _cycle = 0;

    // Speed relative to direction
    [Export]
    public float Speed { get; set; } = 100; // in pixels per second

    // Magnitude of the sin wave
    [Export]
    public float Magnitude { get; set; } = 500; // in pixels

    // Period of the sin wave
    [Export]
    public float Period { get; set; } = 0.5f; // in seconds (= 1/frequency)

    public void ChangeDirection(Vector2 newDirection)
    {
        _baseDirection = newDirection.Normalized();
        _crossDirection = _baseDirection.Rotated(Mathf.Pi / 2); // 2D cross product
        _cycle = 0;
    }

    public Vector2 CalculateVector(float delta)
    {
        _cycle = (_cycle + delta) % Period;

        // Note: Does not need to multiply by delta
        return (_baseDirection * Speed) + (_crossDirection * Magnitude * Mathf.Cos(2 * Mathf.Pi * _cycle / Period));
    }

}
