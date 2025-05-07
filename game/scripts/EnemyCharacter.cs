using Godot;
using System;
using System.Collections.Generic;

public class EnemyCharacter : KinematicBody2D, IProjectileInfo
{
    [Signal]
    public delegate void UpdateElement(Globals.Element element, int newCount);

    [Export]
    public NodePath HealthComponentPath { get; private set; } = new NodePath();
    public HealthComponent healthComponent { get; private set; }

    [Export]
    public NodePath CharacterSpirtePath = new NodePath();
    public Sprite CharacterSprite { get; private set; }

    [Export]
    public NodePath HealthBarPath { get; private set; } = new NodePath();
    private ProgressBar _healthBar;

    [Export]
    public NodePath HealthTextPath { get; private set; } = new NodePath();
    private Label _healthText;

    [Export]
    public NodePath DamagePopupPath { get; private set; } = new NodePath();
    private DamagePopup _damagePopup;


    // Need a timer component
    private float _fireDelay;
    private float _fireTimer = 0.0f;

    private Globals.Element _currentElement = Globals.Element.None;
    public Dictionary<Globals.Element, int> ElementalCount { get; private set; } = new Dictionary<Globals.Element, int>();

    private Dictionary<string, Bullet> _bulletTemplates = new Dictionary<string, Bullet>();

    public int FriendlyCollisionFlag { get; set; } = Globals.EnemyProjectileLayerBit;

    public override void _Ready()
    {
        healthComponent = GetNode<HealthComponent>(HealthComponentPath);
        CharacterSprite = GetNode<Sprite>(CharacterSpirtePath);
        _healthBar = GetNode<ProgressBar>(HealthBarPath);
        _healthText = GetNode<Label>(HealthTextPath);
        _damagePopup = GetNode<DamagePopup>(DamagePopupPath);
        if (healthComponent == null || _healthBar == null 
            || _healthText == null || _damagePopup == null || CharacterSprite == null)
        {
            GD.PrintErr("Error: Enemy Controller Contrain Invalid Path");
            return;
        }

        _OnHealthUpdate(healthComponent.CurrentHealth);

        _fireDelay = (float)GD.RandRange(1.0, 5.0);

        foreach (Globals.Element element in Globals.AllElements)
        {
            ElementalCount[element] = 0;
            EmitSignal("UpdateElement", element, 0);
        }

        _currentElement = Globals.DominantElement(ElementalCount);

        // TODO for testing remove later
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        int loop = rng.RandiRange(1, 3);
        for (int i = 0; i < loop; i++)
        {
            rng.Randomize();
            int randomElement = rng.RandiRange(1, 5);
            rng.Randomize();
            AddToElement((Globals.Element)randomElement, rng.RandiRange(1, 100));
        }
    }

    public override void _Process(float delta)
    {
        _fireTimer += delta;
        if (_fireTimer >= _fireDelay)
        {
            _fireTimer = 0;
            Shoot();
        }
    }

    public void _OnHitboxBodyEntered(Node body)
    {
        if (body is IHarmful damageSource)
        {
            GD.Print(damageSource.CollisionFlag);
            if (damageSource.CollisionFlag != Globals.EnemyProjectileLayerBit)
            {
                healthComponent.ApplyDamage(damageSource);
                _damagePopup.AddToCumulativeDamage(damageSource.GetDamage());
                body.QueueFree();
            }
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

    public void AddToElement(Globals.Element element, int count)
    {
        if (element == 0)
            return;

        ElementalCount[element] += count;
        EmitSignal("UpdateElement", element, ElementalCount[element]);
        _currentElement = Globals.DominantElement(ElementalCount);
    }

    public void SubtractFromElement(Globals.Element element, int count)
    {
        if (element == 0)
            return;

        ElementalCount[element] = Mathf.Clamp(ElementalCount[element] - count, 0, 255);
        EmitSignal("UpdateElement", element, ElementalCount[element]);
        _currentElement = Globals.DominantElement(ElementalCount);
    }

    public void Shoot()
    {
        ProjectileManager.EmitBulletLine(_bulletTemplates[$"Enemy_{_currentElement}_Bullet"], GetTree().Root, FriendlyCollisionFlag, Position);
        AudioManager.PlaySFX("res://assets/sfx/test/bang.wav");
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        // - - - Initialize Bullet Templates - - -

        Bullet template;

        foreach (Globals.Element element in Globals.AllElements)
        {
            template = (Bullet)ProjectileManager.LoadTemplate(ProjectileManager.BulletScenePath[element]);
            template.Element = element;
            _bulletTemplates[$"Enemy_{element}_Bullet"] = template;
        }

        foreach (Bullet bullet in _bulletTemplates.Values)
        {
            // Warning: DO NOT attach template nodes to a parent
            bullet.Initalize();
            bullet.SetCollisionLayerBit(Globals.EnemyProjectileLayerBit, true);
            bullet.MovementNode.Direction = Vector2.Left;
            bullet.MovementNode.Speed = 200; // TODO tune speed
        }

        // - - - Initialize Bullet Templates - - -
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // Free the bullet templates
        foreach (Bullet bullet in _bulletTemplates.Values)
        {
            bullet.QueueFree(); 
        }
    }
}