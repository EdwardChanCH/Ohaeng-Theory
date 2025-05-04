using Godot;
using System;

// This class is the entry point of the game.
public class Bullet : KinematicBody2D, IHarmful
{
    private static int _spawnCounter = 0; // TODO test only

    [Export]
    public int Damage { get; set; } = 0;

    [Export]
    public Vector2 InitialDirection { get; set; } = Vector2.Zero;

    [Export]
    public NodePath MovementComponentPath { get; set; }

    private IMovement _movementComponent;

    // Note: Use this as the Main() method.
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _spawnCounter += 1; // TODO test only
        GD.Print($"Bullet #{_spawnCounter} spawned.");

        _movementComponent = GetNode<IMovement>(MovementComponentPath);
        _movementComponent.ChangeDirection(InitialDirection); // required
    }

    // Called every physics tick. 'delta' is the elapsed time since the previous tick.
    public override void _PhysicsProcess(float delta)
    {
        MoveAndSlide(_movementComponent.CalculateVector(delta)); // Should be the last line in _PhysicsProcess()
    }

    public int GetDamage()
    {
        return Damage;
    }
}
