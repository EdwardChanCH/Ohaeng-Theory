using Godot;
using System;

public class EnemyCharacter : KinematicBody2D
{
    [Export]
    public NodePath HealthComponentPath { get; private set; } = new NodePath();
    private HealthComponent _healthComponent;

    [Export]
    public NodePath HealthBarPath { get; private set; } = new NodePath();
    private ProgressBar _healthBar;


    // Need a timer component
    private float _fireDelay;
    private float _fireTimer = 0.0f;

    public override void _Ready()
    {
        _healthComponent = GetNode<HealthComponent>(HealthComponentPath);
        _healthBar = GetNode<ProgressBar>(HealthBarPath);

        if (_healthComponent == null || _healthBar == null)
        {
            GD.PrintErr("Error: Enemy Controller Contrain Invalid Path");
            return;
        }

        _OnHealthUpdate(_healthComponent.CurrentHealth);

        _fireDelay = (float)GD.RandRange(1.0, 5.0);
    }

    public override void _Process(float delta)
    {
        _fireTimer += delta;
        if (_fireTimer >= _fireDelay)
        {
            _fireTimer = 0;
            //GD.Print("Shoot");
            TestShoot();
        }
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
    }

    public void _OnHealthDepleted()
    {
        QueueFree();
    }



    public void TestShoot()
    {
        // - - - Should be done by projectie manager - - -
        Bullet testBullet = GD.Load<PackedScene>("res://scenes/test_bullet.tscn").Instance<Bullet>();
        testBullet.Position = this.Position;
        testBullet.Damage = 1;
        testBullet.InitialDirection = Vector2.Left;
        testBullet.SetCollisionLayerBit(Globals.EnemyProjectileLayerBit, true);
        GetTree().Root.CallDeferred("add_child", testBullet);
        // - - - Should be done by projectie manager - - -
    }
}