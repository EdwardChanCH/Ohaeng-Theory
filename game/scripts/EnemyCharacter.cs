using Godot;
using System;

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


    // Need a timer component
    private float _fireDelay;
    private float _fireTimer = 0.0f;

    private Globals.Element _currentElement = Globals.Element.None;
    public System.Collections.Generic.Dictionary<Globals.Element, int> ElementalCount { get; private set; } = 
        new System.Collections.Generic.Dictionary<Globals.Element, int>();

    public override void _EnterTree()
    {
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

        _fireDelay = (float)GD.RandRange(1.0, 5.0);

        for (int i = 1; i <= 5; i++)
        {
            ElementalCount.Add((Globals.Element)i, 0);
            EmitSignal("UpdateElement", (Globals.Element)i, 0);
        }


        // for testing remove later
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        int loop = rng.RandiRange(1, 3);
        for (int i = 1; i < loop; i++)
        {
            rng.Randomize();
            int randomInt = rng.RandiRange(1, 5);
            rng.Randomize();
            AddToElement((Globals.Element)randomInt, rng.RandiRange(1, 10));
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
            HealthComponent.ApplyDamage(damageSource.GetDamage());
            _damagePopup.AddToCumulativeDamage(damageSource.GetDamage());
            body.QueueFree();
            //GD.Print("Hurt");
        }
    }

    public void _OnHealthUpdate(int newHealth)
    {
        _healthBar.Value = (float)newHealth / (float)HealthComponent.MaxHealth;
        _healthText.Text = newHealth.ToString() + " / " + HealthComponent.MaxHealth;
    }

    public void _OnHealthDepleted()
    {
        QueueFree();
    }

    public void Shoot()
    {
        ProjectileManager.EmitBulletRing(_currentElement, GetTree().Root, Position, Vector2.Left, 1, false, 12);
        AudioManager.PlaySFX("res://assets/sfx/test/bang.wav");
    }

    public void AddToElement(Globals.Element element, int count)
    {
        if (element == 0)
            return;

        ElementalCount[element] += count;
        EmitSignal("UpdateElement", element, ElementalCount[element]);
    }

    public void SubtractFromElement(Globals.Element element, int count)
    {
        if (element == 0)
            return;

        ElementalCount[element] = Mathf.Clamp(ElementalCount[element] - count, 0, 255);
        EmitSignal("UpdateElement", element, ElementalCount[element]);
    }
}