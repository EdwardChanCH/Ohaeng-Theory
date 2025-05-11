using Godot;
using System;
using System.Collections.Generic;
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

    private Globals.Element _dominantElement = Globals.Element.Water;

    private Dictionary<string, Bullet> _bulletTemplates = new Dictionary<string, Bullet>();

    private float _fireDelay = 1.5f;
    private float _fireTimer;

    public override void _Ready()
    {
        HealthComponent = GetNode<HealthComponent>(HealthComponentPath);
        CharacterSprite = GetNode<Sprite>(CharacterSpirtePath);
        _healthBar = GetNode<ProgressBar>(HealthBarPath);
        _healthText = GetNode<Label>(HealthTextPath);
        _damagePopup = GetNode<DamagePopup>(DamagePopupPath);
        MovementComponent = GetNode<IMovement>(MovementComponentPath);
        _OnHealthUpdate(HealthComponent.CurrentHealth);
        MovementComponent.Direction = Vector2.Left;


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
            bullet.Position = Vector2.Zero;
            bullet.Damage = 1;
            bullet.Friendly = false;
            bullet.MovementNode.Direction = Vector2.Left;
            bullet.MovementNode.Speed = 500; // TODO tune speed
        }

        _fireTimer = _fireDelay / 2.0f;
    }

    public override void _Process(float delta)
    {
        _fireTimer += delta;
        if (_fireTimer >= _fireDelay)
        {
            _fireTimer = 0;

            Vector2 targetDirection = GlobalPosition.DirectionTo(GameplayScreen.PlayerRef.GlobalPosition);

            if (targetDirection.x < 0)
            {
                // Cannot only fire to the left

                var bulletRef = MakeBulletCopy(_dominantElement);
                bulletRef.MovementNode.Direction = targetDirection;
                GD.Print($"{targetDirection}");
                ProjectileManager.EmitBulletLine(bulletRef, GetTree().Root, GlobalPosition);
                bulletRef.QueueFree();
            }
            
            //(Bullet)ProjectileManager.LoadTemplate(ProjectileManager.BulletScenePath[element]);
        }
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
        Globals.AddScore(Globals.LesserEnemyKillReward);
        
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

    public Bullet MakeBulletCopy(Globals.Element element)
    {
        var bulletCopy = (Bullet)ProjectileManager.LoadTemplate(ProjectileManager.BulletScenePath[element]);
        Bullet.CopyData(_bulletTemplates[$"Enemy_{element}_Bullet"], bulletCopy);
        return bulletCopy;
    }
}
