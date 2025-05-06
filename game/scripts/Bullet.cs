using Godot;
using System;

// This class is the entry point of the game.
public class Bullet : KinematicBody2D, IHarmful
{
    [Export]
    public Globals.Element Element { get; set; } = Globals.Element.None;

    [Export]
    public int Damage { get; set; } = 0;

    [Export]
    public Vector2 InitialDirection { get; set; } = Vector2.Zero;

    [Export]
    public NodePath MovementComponentPath { get; set; }

    private IMovement _movementComponent;

    public int GetDamage()
    {
        return Damage;
    }

    // Copy data from another source bullet
    public void CopyData(Bullet other)
    {
        this.Position = other.Position;
        this.Damage = other.Damage;
        this.InitialDirection = other.InitialDirection;
        this.CollisionLayer = other.CollisionLayer;
        this.CollisionMask = other.CollisionMask;
    }

    // Note: Use this as the Main() method.
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _movementComponent = GetNode<IMovement>(MovementComponentPath);
        _movementComponent.ChangeDirection(InitialDirection); // required
    }

    // Called every physics tick. 'delta' is the elapsed time since the previous tick.
    public override void _PhysicsProcess(float delta)
    {
        MoveAndSlide(_movementComponent.CalculateVector(delta)); // Should be the last line in _PhysicsProcess()
    }

}
