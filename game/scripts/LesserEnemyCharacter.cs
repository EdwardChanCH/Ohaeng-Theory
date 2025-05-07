using Godot;
using System;

public class LesserEnemyCharacter : KinematicBody2D, IHarmful, IProjectileInfo
{
    [Export]
    public NodePath HealthComponentPath { get; private set; } = new NodePath();
    public HealthComponent healthComponent { get; private set; }

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
    private IMovement _movementComponent;

    [Export]
    public int CollisionDamage { get; set; } = 2;

    [Export]
    public Vector2 MoveDirection { get; set; } = Vector2.Left;

    public int CollisionFlag { get; set; } = Globals.EnemyProjectileLayerBit;
    public int FriendlyCollisionFlag { get; set; } = Globals.EnemyProjectileLayerBit;

    public override void _Ready()
    {
        healthComponent = GetNode<HealthComponent>(HealthComponentPath);
        CharacterSprite = GetNode<Sprite>(CharacterSpirtePath);
        _healthBar = GetNode<ProgressBar>(HealthBarPath);
        _healthText = GetNode<Label>(HealthTextPath);
        _damagePopup = GetNode<DamagePopup>(DamagePopupPath);
        _movementComponent = GetNode<IMovement>(MovementComponentPath);

        if (healthComponent == null || _healthBar == null 
            || _movementComponent == null || _healthText == null 
            || _damagePopup == null || CharacterSprite == null)
        {
            GD.PrintErr("Error: Enemy Controller Contrain Invalid Path");
            return;
        }

        _OnHealthUpdate(healthComponent.CurrentHealth);

        _movementComponent.Direction = MoveDirection;
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
            HealthComponent.ApplyDamage(damageSource);
            _damagePopup.AddToCumulativeDamage(damageSource.GetDamage());
            //body.QueueFree();
        }

        if (body is Bullet)
        {
            ProjectileManager.QueueDespawnProjectile(body);
            //GD.Print("LesserEnemyCharacter despawn Bullet.");
        }
    }

    public void _OnHealthUpdate(int newHealth)
    {
        _healthBar.Value = (float)newHealth / (float)healthComponent.MaxHealth;
        _healthText.Text = newHealth.ToString() + " / " + healthComponent.MaxHealth;
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
