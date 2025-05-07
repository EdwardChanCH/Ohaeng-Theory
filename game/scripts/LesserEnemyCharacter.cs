using Godot;
using System;

public class LesserEnemyCharacter : KinematicBody2D, IHarmful
{
    [Export]
    public NodePath HealthComponentPath { get; private set; } = new NodePath();
    public HealthComponent HealthComponent { get; private set; }

    [Export]
    public NodePath HealthBarPath { get; private set; } = new NodePath();
    private ProgressBar _healthBar;

    [Export]
    public NodePath CharacterSpirtePath = new NodePath();
    public Sprite CharacterSprite { get; private set; }

    [Export]
    public NodePath HealthTextPath { get; private set; } = new NodePath();
    private Label _healthText;

    [Export]
    public NodePath DamagePopupPath { get; private set; } = new NodePath();
    private DamagePopup _damagePopup;

    [Export]
    public NodePath MovementComponentPath { get; set; }
    public IMovement MovementComponent;

    [Export]
    public int CollisionDamage { get; set; } = 2;

    public override void _Ready()
    {
        HealthComponent = GetNode<HealthComponent>(HealthComponentPath);
        CharacterSprite = GetNode<Sprite>(CharacterSpirtePath);
        _healthBar = GetNode<ProgressBar>(HealthBarPath);
        _healthText = GetNode<Label>(HealthTextPath);
        _damagePopup = GetNode<DamagePopup>(DamagePopupPath);
        MovementComponent = GetNode<IMovement>(MovementComponentPath);

        if (HealthComponent == null || _healthBar == null 
            || MovementComponent == null || _healthText == null 
            || _damagePopup == null || CharacterSprite == null)
        {
            GD.PrintErr("Error: Enemy Controller Contains Invalid Path");
            return;
        }

        _OnHealthUpdate(HealthComponent.CurrentHealth);

        MovementComponent.Direction = Vector2.Left;
    }

    public override void _PhysicsProcess(float delta)
    {
        MoveAndSlide(MovementComponent.CalculateVector(delta)); // Should be the last line in _PhysicsProcess()
    }

    public void _OnHitboxBodyEntered(Node body)
    {
        // TODO unfinished
        if (body is IHarmful harmful)
        {
            if (body is Bullet bullet)
            {
                HealthComponent.ApplyDamage(harmful.GetDamage());
                _damagePopup.AddToCumulativeDamage(harmful.GetDamage());
                ProjectileManager.QueueDespawnProjectile(body);
            }
        }
    }

    public void _OnHealthUpdate(int newHealth)
    {
        _healthBar.Value = (float)newHealth / (float)HealthComponent.MaxHealth;
        _healthText.Text = newHealth.ToString() + " / " + HealthComponent.MaxHealth;
    }

    public void _OnHealthDepleted()
    {
        QueueFree(); // TODO Add a publlic Kill() function
    }

    public int GetDamage()
    {
        return CollisionDamage;
    }

}
