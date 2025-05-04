using Godot;
using System;

public class LesserEnemyCharacter : KinematicBody2D, IHarmful
{
    [Export]
    public NodePath HealthComponentPath { get; private set; } = new NodePath();
    private HealthComponent _healthComponent;

    [Export]
    public NodePath HealthBarPath { get; private set; } = new NodePath();
    private ProgressBar _healthBar;

    [Export]
    public NodePath MovementComponentPath { get; set; }
    private IMovement _movementComponent;

    [Export]
    public int CollisionDamage { get; set; } = 2;

    [Export]
    public Vector2 MoveDirection { get; set; } = Vector2.Left;

    public override void _Ready()
    {
        _healthComponent = GetNode<HealthComponent>(HealthComponentPath);
        _healthBar = GetNode<ProgressBar>(HealthBarPath);
        _movementComponent = GetNode<IMovement>(MovementComponentPath);

        if (_healthComponent == null || _healthBar == null || _movementComponent == null)
        {
            GD.PrintErr("Error: Enemy Controller Contrain Invalid Path");
            return;
        }

        _OnHealthUpdate(_healthComponent.CurrentHealth);

        _movementComponent.ChangeDirection(MoveDirection);
        SetCollisionLayerBit(Globals.EnemyProjectileLayerBit, true);
    }

    public override void _PhysicsProcess(float delta)
    {
        MoveAndSlide(_movementComponent.CalculateVector(delta)); // Should be the last line in _PhysicsProcess()
    }

    public void _OnHitboxBodyEntered(Node body)
    {
        if (body is IHarmful damageSource)
        {
            _healthComponent.ApplyDamage(damageSource);
            body.QueueFree();
            //GD.Print("Hurt");
        }
    }

    public void _OnHealthUpdate(int newHealth)
    {
        _healthBar.Value = (float)newHealth / (float)_healthComponent.MaxHealth;
        //GD.Print(newHealth);
    }

    public void _OnHealthDepleted()
    {
        QueueFree();
    }

    public int GetDamage()
    {
        return CollisionDamage;
    }
}
