using Godot;
using System;

public class LesserEnemyCharacter : KinematicBody2D, IHarmful
{
    [Signal]
    public delegate void Killed(LesserEnemyCharacter source);

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

    [Export]
    public Texture[] CharacterSpriteTexture { get; private set; } = new Texture[0];

    private Globals.Element _dominantElement = Globals.Element.None;

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
        if (body is IHarmful harmful && harmful.IsFriendly() && harmful.IsActive())
        {
            float floatDamage = (float)harmful.GetDamage();
            float damageModifier = 1;

            // Do 50% damage if the element of the bullet is the same or counter by the _dominantElement
            if (harmful.GetElement() == _dominantElement || Globals.CounterToElement(harmful.GetElement()) == _dominantElement)
            {
                damageModifier = 0.5f;
            }

            // Do 200% damage if the element of the bullet count the _dominantElement
            if (harmful.GetElement() == Globals.CounterByElement(_dominantElement))
            {
                damageModifier = 2f;
            }

            floatDamage *= damageModifier;
            var damage = Mathf.CeilToInt(floatDamage);
            HealthComponent.ApplyDamage(damage);
            _damagePopup.AddToCumulativeDamage(damage);
            harmful.Kill(); // Works on Bullet, Enemy, and Lesser Enemy
        }
    }

    public void _OnHealthUpdate(int newHealth)
    {
        _healthBar.Value = (float)newHealth / (float)HealthComponent.MaxHealth;
        _healthText.Text = newHealth.ToString() + " / " + HealthComponent.MaxHealth;
    }

    public void _OnHealthDepleted()
    {
        Kill();
    }

    public int GetDamage()
    {
        return CollisionDamage;
    }

    public bool IsFriendly()
    {
        return false; // Always not friendy
    }

    public bool IsActive()
    {
        return true; // Always active
    }

    public void Kill()
    {
        EmitSignal("Killed", this);
        QueueFree();
    }

    public void SwitchSprite(Globals.Element element)
    {
        if (CharacterSpriteTexture.Length >= 5)
            CharacterSprite.Texture = CharacterSpriteTexture[(int)element - 1];
    }

    public Globals.Element GetElement()
    {
        return _dominantElement;
    }

    public void SetElement(Globals.Element value)
    {
        if (value == Globals.Element.None)
        {
            GD.PrintErr("Error: LesserEnemyCharacter cannot set element as None.");
            return;
        }

        _dominantElement = value;
        SwitchSprite(value);
    }
}
