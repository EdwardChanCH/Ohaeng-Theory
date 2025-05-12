using Godot;
using System;

public interface IMovement
{
    // Initial direction (should be normalized)
    Vector2 Direction { get; set; }

    // Initial speed
    float Speed { get; set; }

    // Get a new movement vector in each physic tick
    // for calling MoveAndSlide(Vector2) in any character
    Vector2 CalculateVector(float delta);

    // Reset any movement related data
    void Reset();
}
