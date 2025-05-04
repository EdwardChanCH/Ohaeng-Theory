using Godot;
using System;

public interface IMovement
{
    // Initialize/ change bullet direction
    void ChangeDirection(Vector2 newDirection);

    // Get a new movement vector in each physic tick
    // for calling MoveAndSlide(Vector2) in character
    Vector2 CalculateVector(float delta);
}
