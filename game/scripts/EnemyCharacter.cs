using Godot;
using System;
using System.Collections.Generic;

public class EnemyCharacter : KinematicBody2D
{
    [Signal]
    public delegate void UpdateElement(Globals.Element element, int newCount);

    [Export]
    public NodePath HealthComponentPath { get; private set; } = new NodePath();
    public HealthComponent HealthComponent { get; private set; }

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

    // TODO Need a timer component
    private float _fireDelay;
    private float _fireTimer = 0.0f;

    private Globals.Element _currentElement = Globals.Element.None;
    public Dictionary<Globals.Element, int> ElementalCount { get; private set; } = new Dictionary<Globals.Element, int>();

    private Dictionary<string, Bullet> _bulletTemplates = new Dictionary<string, Bullet>();

    public override void _EnterTree()
    {
        base._EnterTree();

        // - - - Initialize Enemy Bullet Templates - - -

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
            bullet.Position = Vector2.Zero;
            bullet.Damage = 1;
            bullet.Friendly = false;
            bullet.MovementNode.Direction = Vector2.Left;
            bullet.MovementNode.Speed = 200; // TODO tune speed
        }

        // - - - Initialize Enemy Bullet Templates - - -
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

    public override void _Ready()
    {
        HealthComponent = GetNode<HealthComponent>(HealthComponentPath);
        CharacterSprite = GetNode<Sprite>(CharacterSpirtePath);
        _healthBar = GetNode<ProgressBar>(HealthBarPath);
        _healthText = GetNode<Label>(HealthTextPath);
        _damagePopup = GetNode<DamagePopup>(DamagePopupPath);
        if (HealthComponent == null || _healthBar == null 
            || _healthText == null || _damagePopup == null || CharacterSprite == null)
        {
            GD.PrintErr("Error: Enemy Controller Contrain Invalid Path");
            return;
        }

        _OnHealthUpdate(HealthComponent.CurrentHealth);

        _fireDelay = (float)GD.RandRange(1.0, 5.0); // TODO for testing remove later

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
        if (body is IHarmful harmful && harmful.IsFriendly() && harmful.IsActive())
        {
            GD.Print($"{body.GetInstanceId()} {harmful.IsFriendly()} {harmful.IsActive()}"); // TODO debug
            HealthComponent.ApplyDamage(harmful.GetDamage());
            _damagePopup.AddToCumulativeDamage(harmful.GetDamage());
            harmful.Kill();
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

    public void AddToElement(Globals.Element element, int count)
    {
        if (element == 0) { return; }    

        ElementalCount[element] += count;
        EmitSignal("UpdateElement", element, ElementalCount[element]);
        _currentElement = Globals.DominantElement(ElementalCount);
    }

    public void SubtractFromElement(Globals.Element element, int count)
    {
        if (element == 0) { return; }

        ElementalCount[element] -= count;
        if (ElementalCount[element] < 0)
        {
            ElementalCount[element] = 0;
        }
        EmitSignal("UpdateElement", element, ElementalCount[element]);
        _currentElement = Globals.DominantElement(ElementalCount);
    }

    public void ResetElementalCount(Dictionary<Globals.Element, int> newValue)
    {
        foreach (Globals.Element key in newValue.Keys)
        {
            ElementalCount[key] = newValue[key];
            EmitSignal("UpdateElement", key, ElementalCount[key]);
        }

        _currentElement = Globals.DominantElement(ElementalCount);
    }

    public void Shoot()
    {
        // Edit the bullet template instead of the function parameters
        ProjectileManager.EmitBulletLine(_bulletTemplates[$"Enemy_{_currentElement}_Bullet"], GetTree().Root, Position);
        AudioManager.PlaySFX("res://assets/sfx/test/bang.wav");
    }

}