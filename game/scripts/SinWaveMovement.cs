using Godot;
using System;

// This class is the entry point of the game.
public class SinWaveMovement : BaseMovement
{
    private Vector2 _crossDirection = Vector2.Zero;

    [Export]
    public override Vector2 Direction
    {
        get { return _direction; }
        set
        {
            _direction = value.Normalized();
            _crossDirection = _direction.Rotated(Mathf.Pi / 2); // 2D cross product
        }
    }

    private float _phase = 0;
    private float _cycle = 0;

    [Export]
    public float Phase {
        get {return _phase; }
        set
        {
            _phase = value; // in degrees out-of-phase
            _cycle = Mathf.Deg2Rad(_phase); // initial sin wave offset
        }
    }

    // Magnitude of the sin wave
    [Export]
    public float Magnitude { get; set; } = 500; // in pixels

    // Period of the sin wave
    [Export]
    public float Period { get; set; } = 0.5f; // in seconds (1 / frequency)

    public override Vector2 CalculateVector(float delta)
    {
        _cycle = (_cycle + delta) % Period;

        // Note: Does not need to multiply by delta
        return (_direction * Speed) + (_crossDirection * Magnitude * Mathf.Cos(2 * Mathf.Pi * _cycle / Period));
    }

}
