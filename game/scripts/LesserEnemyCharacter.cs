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
    public NodePath HealthTextPath { get; private set; } = new NodePath();
    private Label _healthText;

    [Export]
    public NodePath DamagePopupPath { get; private set; } = new NodePath();
    private DamagePopup _damagePopup;

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
        _healthText = GetNode<Label>(HealthTextPath);
        _damagePopup = GetNode<DamagePopup>(DamagePopupPath);
        _movementComponent = GetNode<IMovement>(MovementComponentPath);

        if (_healthComponent == null || _healthBar == null 
            || _movementComponent == null || _healthText == null || _damagePopup == null)
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
            _damagePopup.AddToCumulativeDamage(damageSource.GetDamage());
            body.QueueFree();
        }
    }

    public void _OnHealthUpdate(int newHealth)
    {
        _healthBar.Value = (float)newHealth / (float)_healthComponent.MaxHealth;
        _healthText.Text = newHealth.ToString() + " / " + _healthComponent.MaxHealth;
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
