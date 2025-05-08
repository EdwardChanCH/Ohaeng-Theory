using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class EnemyCharacter : KinematicBody2D
{
    [Signal]
    public delegate void Killed(EnemyCharacter source);

    [Signal]
    public delegate void SplitNeeded(EnemyCharacter source);

    [Signal]
    public delegate void MergeNeeded(EnemyCharacter source);

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

    [Export]
    public Texture[] CharacterSpriteTexture { get; private set; } = new Texture[0];

    // TODO Need a timer component
    [Export]
    public float AttackBetweenDelay = 0.5f;
    private float _attackBetweenTimer;

    private float _fireDelay = 0.25f;
    private float _fireTimer;
    private bool _isAttacking = false;

    public Dictionary<Globals.Element, int> ElementalCount { get; private set; } = new Dictionary<Globals.Element, int>();
    private Globals.Element _dominantElement = Globals.Element.None;

    private Dictionary<string, Bullet> _bulletTemplates = new Dictionary<string, Bullet>();


    private Queue<Bullet> _projectileQueue = new Queue<Bullet>();

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


        _OnHealthUpdate(HealthComponent.CurrentHealth);

        //FireDelay = (float)GD.RandRange(1.0, 5.0); // TODO for testing remove later

        foreach (Globals.Element element in Globals.AllElements)
        {
            ElementalCount[element] = 0;
            EmitSignal("UpdateElement", element, 0);
        }

        _dominantElement = Globals.DominantElement(ElementalCount);

        _fireTimer = _fireDelay;
        _attackBetweenTimer = AttackBetweenDelay;



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
        if(_isAttacking)
        {
            _fireTimer += delta;
            if (_fireTimer >= _fireDelay)
            {
                _fireTimer = 0;
                var projectile = _projectileQueue.Dequeue();
                GD.Print(projectile.MovementNode.Direction);


                ProjectileManager.EmitBulletLine(projectile, GetTree().Root, GlobalPosition);


                if(_projectileQueue.Count <= 0)
                {
                    _isAttacking = false;
                    _attackBetweenTimer = 0;
                }
            }
        }

        _attackBetweenTimer += delta;
        if (_attackBetweenTimer >= AttackBetweenDelay && !_isAttacking)
        {
            _isAttacking = true;

            SpherePattern(30);
            //this.CallDeferred("SpherePattern", 15);
            GD.Print("Does thing");
        }


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

        // TODO test code
        if (newHealth < HealthComponent.MaxHealth)
        {
            EmitSignal("SplitNeeded", this);
        }
    }

    public void _OnHealthDepleted()
    {
        Kill();
    }

    public void Kill()
    {
        EmitSignal("Killed", this);
        QueueFree();
    }

    public void AddToElement(Globals.Element element, int count)
    {
        if (element == 0 || element == Globals.Element.None)
        {
            return;
        }

        ElementalCount[element] += count;
        EmitSignal("UpdateElement", element, ElementalCount[element]);
        if (Globals.DominantElement(ElementalCount) != _dominantElement)
        {
            _dominantElement = Globals.DominantElement(ElementalCount);
            SwitchSprite(_dominantElement);
        }
    }

    public void SubtractFromElement(Globals.Element element, int count)
    {
        if (element == 0 || element == Globals.Element.None)
        {
            return;
        }

        ElementalCount[element] -= count;
        if (ElementalCount[element] < 0)
        {
            ElementalCount[element] = 0;
        }
        EmitSignal("UpdateElement", element, ElementalCount[element]);
        _dominantElement = Globals.DominantElement(ElementalCount);
    }

    // TODO Make a public void SetElementalCount(Globals.Element element, int value)
    // Just like the SetHealth() function newly added in HealthComponent

    public void SetElementalCount(Dictionary<Globals.Element, int> values)
    {
        if (values.ContainsKey(Globals.Element.None))
        {
            values.Remove(Globals.Element.None);
        }

        foreach (Globals.Element key in values.Keys)
        {
            ElementalCount[key] = values[key];
            EmitSignal("UpdateElement", key, ElementalCount[key]);
        }

        _dominantElement = Globals.DominantElement(ElementalCount);
        SwitchSprite(_dominantElement);
    }

    public int TotalElementalCount()
    {
        int total = 0;

        foreach (int count in ElementalCount.Values)
        {
            total += count;
        }

        return total;
    }

    public void SwitchSprite(Globals.Element element)
    {
        if (CharacterSpriteTexture.Length >= 5)
            CharacterSprite.Texture = CharacterSpriteTexture[(int)element - 1];
    }



    public void WavePattern(int spawnCount, float angle, float speedIncrease)
    {
        var startingDirection = Vector2.Left;
        for (int i = 0; i < spawnCount; i++)
        {

            var bulletCopy = _bulletTemplates[$"Enemy_{_dominantElement}_Bullet"];
            var direction = startingDirection.Rotated(Mathf.Deg2Rad(i - ((float)spawnCount / 2) * angle));

            bulletCopy.MovementNode.Direction = direction;
            bulletCopy.MovementNode.Speed += i * speedIncrease;

            _projectileQueue.Enqueue(bulletCopy);

        }
    }

    public void SpherePattern(int spawnCount)
    {
        _isAttacking = true;
        var startingDirection = Vector2.Left;
        for (int i = 0; i < spawnCount; i++)
        {
            var bulletCopy = (Bullet)ProjectileManager.SpawnProjectile(_bulletTemplates[$"Enemy_{_dominantElement}_Bullet"], GetTree().Root);


            var direction = startingDirection.Rotated(i);
            bulletCopy.MovementNode.Direction = direction;
            _projectileQueue.Enqueue(bulletCopy);
        }
    }





}