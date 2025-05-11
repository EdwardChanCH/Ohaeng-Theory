using Godot;
using System;
using System.Collections.Generic;
//using static Globals; // Please don't

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

    [Signal]
    public delegate void ReachedTarget(EnemyCharacter source);

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
    public Texture[] CharacterSpriteTexture { get; set; } = new Texture[0];

    [Export]
    public Globals.Element DominantElement = Globals.Element.Water; // None would have out-of-bound error in switch sprite

    [Export]
    public Dictionary<Globals.Element, int> ElementalCount { get; private set; } = new Dictionary<Globals.Element, int>();


    [Export]
    public float AttackBetweenDelay = 2.5f;
    private float _attackBetweenTimer;

    private float _fireDelay = 0.25f;
    private float _fireTimer;
    private bool _isAttacking = false;
    private int _attackCounter = 0;


    private Dictionary<string, Bullet> _bulletTemplates = new Dictionary<string, Bullet>();

    const int MAXPROJECTILEQUEUE = 4;
    private Queue<Bullet>[] _projectileQueue = new Queue<Bullet>[MAXPROJECTILEQUEUE];


    [Export]
    public bool UseSmoothedMovemment { get; set; } = true;

    public bool IsTargeting { get; private set; } = false; // Must be initialized to false
    private Vector2 _moveDirection = Vector2.Zero; // Always normalized
    private Vector2 _targetLocation = Vector2.Zero; // Global coordinate
    public Vector2 TargetLocation
    {
        get { return _targetLocation; }
        set
        {
            IsTargeting = _targetLocation != value; // Check if value changed
            _targetLocation = value;
            _moveDirection = GlobalPosition.DirectionTo(_targetLocation);
        }
    } 

    [Export]
    public float MaxMoveSpeed { get; set; } = 400.0f;

    public Vector2 Velocity { get; private set; } = Vector2.Zero;

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
            bullet.Position = Vector2.Zero;
            bullet.Damage = 1;
            bullet.Friendly = false;
            bullet.MovementNode.Direction = Vector2.Left;
            bullet.MovementNode.Speed = 200; // TODO tune speed
        }

        for (int i = 0; i < MAXPROJECTILEQUEUE; i++)
        {
            _projectileQueue[i] = new Queue<Bullet>();
        }
        // - - - Initialize Enemy Bullet Templates - - -
        _attackBetweenTimer = AttackBetweenDelay;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // Free the bullet templates
        foreach (Bullet bullet in _bulletTemplates.Values)
        {
            bullet.QueueFree();
        }

        // Free projectile queue
        foreach (Queue<Bullet> item in _projectileQueue)
        {
            item.Clear();
        }
        _projectileQueue = null;
    }

    public override void _Ready()
    {
        HealthComponent = GetNode<HealthComponent>(HealthComponentPath);
        CharacterSprite = GetNode<Sprite>(CharacterSpirtePath);
        _healthBar = GetNode<ProgressBar>(HealthBarPath);
        _healthText = GetNode<Label>(HealthTextPath);
        _damagePopup = GetNode<DamagePopup>(DamagePopupPath);


        //_OnHealthUpdate(HealthComponent.CurrentHealth);

        // TODO potentially dangerous
        foreach (Globals.Element element in Globals.AllElements)
        {
            ElementalCount[element] = 0;
            EmitSignal("UpdateElement", element, 0);
        }

        SwitchSprite(DominantElement);

        if (CharacterSpriteTexture.Length != 5)
        {
            GD.PrintErr("Error: EnemyCharacter has missing sprites.");
            return;
        }
    }

    public override void _Process(float delta)
    {

        // Shotting projectile from the queue
        if (_isAttacking)
        {
            _fireTimer += delta;
            if (_fireTimer >= _fireDelay)
            {
                _fireTimer = 0;
                bool areAllQueueEmpty = true;
                foreach (var queue in _projectileQueue)
                {
                    //GD.Print("Metal");
                    if(queue.Count >= 1)
                    {
                        areAllQueueEmpty = false;
                        var projectile = queue.Dequeue();
                        //GD.Print(projectile.MovementNode.Direction);
                        //ProjectileManager.EmitBulletLine(projectile, GetTree().Root, GlobalPosition);

                        switch (DominantElement)
                        {
                            case Globals.Element.Water:
                                ProjectileManager.EmitBulletLine(projectile, GetTree().Root, GlobalPosition);
                                break;

                            case Globals.Element.Wood:

                                // 45.0f
                                // 90.0f 
                                ProjectileManager.EmitBulletConeWide(projectile, GetTree().Root, GlobalPosition, 15, 180.0f);
                                break;

                            case Globals.Element.Fire:
                                ProjectileManager.EmitBulletLine(projectile, GetTree().Root, GlobalPosition);
                                break;

                            case Globals.Element.Earth:
                                //ProjectileManager.EmitBulletRing
                                ProjectileManager.EmitBulletConeWide(projectile, GetTree().Root, GlobalPosition, 5, 1.0f);
                                break;

                            case Globals.Element.Metal:
                                ProjectileManager.EmitBulletWall(projectile, GetTree().Root, GlobalPosition, 1 + _attackCounter, 80);
                                //ProjectileManager.EmitBulletLine(projectile, GetTree().Root, GlobalPosition);
                                break;
                        }
                        projectile.QueueFree();
                    }
                    
                }

                _attackCounter++;
                if (areAllQueueEmpty)
                {
                    _isAttacking = false;
                    _attackBetweenTimer = 0;
                    _attackCounter = 0;
                }
            }
        }
        else
        {
            _attackBetweenTimer += delta;
        }
            // Add projectile to the queue
        if (_attackBetweenTimer >= AttackBetweenDelay && !_isAttacking)
        {
            _isAttacking = true;

            //add a switch statment when got every pattern

            switch (DominantElement)
            {
                case Globals.Element.Water:
                    _fireDelay = 0.05f;
                    WavePattern(50, 4, 5);
                    break;
                case Globals.Element.Wood:
                    _fireDelay = 0.1f;
                    SpherePattern(24, 15, 8, 100);
                    break;
                case Globals.Element.Fire:
                    _fireDelay = 0.005f;
                    SpinnyPattern(180, 4.3f);
                    break;
                case Globals.Element.Earth:
                    _fireDelay = 0.1f;
                    WallPattern(15, 15f, 50);
                    break;
                case Globals.Element.Metal:
                    _fireDelay = 0.1f;
                    WallPattern(10, -10f, 250);
                    break;
            }


        }
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);

        // Target movement
        if (IsTargeting) {
            float distanceToTarget = GlobalPosition.DistanceTo(TargetLocation);
            float distanceAfter;

            if (UseSmoothedMovemment)
            {
                // Smoothed movement
                float smoothFactor = Mathf.Clamp(10 * distanceToTarget / MaxMoveSpeed, 0, 1); // Decelerate when close to target
                Velocity = _moveDirection * MaxMoveSpeed * smoothFactor;
                distanceAfter = MaxMoveSpeed * delta;
            }
            else
            {
                // Constant speed movement
                Velocity = _moveDirection * MaxMoveSpeed;
                distanceAfter = MaxMoveSpeed * delta;
            }

            // Check if it will overshoot
            if (distanceAfter >= distanceToTarget)
            {
                // TargetLocation == Position + Velocity * delta == Position + _moveDirection * distanceToTarget
                Velocity = _moveDirection * distanceToTarget / delta;
                MoveAndSlide(Velocity);

                GlobalPosition = TargetLocation; // Snap in place
                Velocity = Vector2.Zero;
                IsTargeting = false; // Stop moving

                EmitSignal("ReachedTarget", this);
            }
        }
        
        MoveAndSlide(Velocity); // Should be the last line in _PhysicsProcess()
    }

    public void _OnHitboxBodyEntered(Node body)
    {
        // Ignore bullets if physics is turned off
        if (!IsPhysicsProcessing())
        {
            return;
        }


        if (body is IHarmful harmful && harmful.IsFriendly() && harmful.IsActive())
        {
            float floatDamage = (float)harmful.GetDamage();
            float damageModifier = 1;

            // Do 50% damage if the element of the bullet is the same or counter by the _dominantElement
            if (harmful.GetElement() == DominantElement || Globals.CounterToElement(harmful.GetElement()) == DominantElement)
            {
                damageModifier = 0.5f;
            }

            // Do 200% damage if the element of the bullet count the _dominantElement
            if (harmful.GetElement() == Globals.CounterByElement(DominantElement))
            {
                damageModifier = 2f;
            }

            floatDamage *= damageModifier;
            var damage = Mathf.CeilToInt(floatDamage);

            GameplayScreen.Score += damage;

            HealthComponent.ApplyDamage(damage);
            _damagePopup.AddToCumulativeDamage(damage);
            harmful.Kill(); // Works on Bullet, Enemy, and Lesser Enemy
        }
    }

    public void _OnHealthUpdate(int newHealth)
    {
        _healthBar.Value = (float)newHealth / (float)HealthComponent.MaxHealth;
        _healthText.Text = newHealth.ToString() + " / " + HealthComponent.MaxHealth;

        if (newHealth < HealthComponent.MaxHealth / 2)
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
        GameplayScreen.Score += 1000;
        
        // Moved to _ExitTree()
        //foreach (var item in _projectileQueue)
        //{
        //    item.Clear();
        //}
    }

    public void AddToElement(Globals.Element element, int count)
    {
        if (ElementalCount.ContainsKey(element))
        {
            SetElementCount(element, ElementalCount[element] + count);
        }
        else // oldCount = 0
        {
            SetElementCount(element, count);
        }
    }

    public void SubtractFromElement(Globals.Element element, int count)
    {
        if (ElementalCount.ContainsKey(element))
        {
            SetElementCount(element, ElementalCount[element] - count);
        }
        else // oldCount = 0
        {
            SetElementCount(element, 0);
        }
    }

    public void SetElementCount(Globals.Element element, int newCount)
    {
        if (element == Globals.Element.None || newCount < 0)
        {
            GD.PrintErr($"Error: Cannot set {element} element to {newCount} count.");
            return;
        }

        if (newCount < 0)
        {
            newCount = 0;
        }

        ElementalCount[element] = newCount;
        EmitSignal("UpdateElement", element, ElementalCount[element]);

        DominantElement = Globals.DominantElement(ElementalCount);

        if (DominantElement == Globals.Element.None)
        {
            // No element left
            Kill();
            return;
        }

        SwitchSprite(DominantElement);
    }

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

        DominantElement = Globals.DominantElement(ElementalCount);

        if (DominantElement == Globals.Element.None)
        {
            GD.PrintErr($"Error: No remaining elemments in SetElementalCount().");
            return;
        }

        SwitchSprite(DominantElement);
    }

    public int SumElementalCount()
    {
        return Globals.SumElements(ElementalCount);
    }

    public void SwitchSprite(Globals.Element element)
    {
        if (element == Globals.Element.None)
        {
            GD.PrintErr($"Error: EnemyCharacter does not have texture for {element} element.");
            return;
        }

        CharacterSprite.Texture = CharacterSpriteTexture[(int)element - 1];
    }


    public void WavePattern(int spawnCount, float angle, float speedIncrease = 0)
    {
        var startingDirection = GlobalPosition.DirectionTo(GameplayScreen.PlayerRef.Position);

        for (int i = 0; i < spawnCount; i++)
        {
            var bulletCopy = MakeBulletCopy(DominantElement); ;
            var bulletAngle = (i - ((float)spawnCount / 2)) * angle;

            var direction = startingDirection.Rotated(Mathf.Deg2Rad(bulletAngle));
            bulletCopy.MovementNode.Direction = direction;
            bulletCopy.MovementNode.Speed += i * speedIncrease;
            AddToProjecileQueue(bulletCopy);
        }

        for (int i = 0; i < spawnCount; i++)
        {
            var bulletCopy = MakeBulletCopy(DominantElement); ;
            var bulletAngle = (i - ((float)spawnCount / 2)) * angle;

            var direction = startingDirection.Rotated(Mathf.Deg2Rad(-bulletAngle));
            bulletCopy.MovementNode.Direction = direction;
            bulletCopy.MovementNode.Speed += i * speedIncrease;
            AddToProjecileQueue(bulletCopy, 1);
        }
    }

    public void SpinnyPattern(int spawnCount, float angle = 45)
    {
        _isAttacking = true;
        var startingDirection = Vector2.Down;
        var anglePerI = 360.0f / angle;

        for (int i = 0; i < spawnCount; i++)
        {
            var bulletCopy = MakeBulletCopy(DominantElement);

            var direction = startingDirection.Rotated(Mathf.Deg2Rad(anglePerI * i));
            bulletCopy.MovementNode.Direction = direction;
            AddToProjecileQueue(bulletCopy);
        }
    }

    public void SpherePattern(int waves, float speedChangePerWave, float angle = 1, float startingSpeed = 150)
    {
        var startingDirection = GlobalPosition.DirectionTo(GameplayScreen.PlayerRef.Position);
        for (int i = 0; i < waves; i++)
        {
            var bulletCopy = MakeBulletCopy(DominantElement);
            var direction = startingDirection.Rotated(Mathf.Deg2Rad(i * angle)); ;

            bulletCopy.MovementNode.Direction = direction;
            bulletCopy.MovementNode.Speed = startingSpeed + i * speedChangePerWave;
            AddToProjecileQueue(bulletCopy);
        }
    }

    public void WallPattern(int waves, float speedChangePerWave, float startingSpeed = 100)
    {
        var startingDirection = GlobalPosition.DirectionTo(GameplayScreen.PlayerRef.Position);
        for (int i = 0; i < waves; i++)
        {
            var bulletCopy = MakeBulletCopy(DominantElement);
            //var direction = startingDirection.Rotated(Mathf.Deg2Rad(i)); ;

            bulletCopy.MovementNode.Direction = startingDirection;
            bulletCopy.MovementNode.Speed = startingSpeed + i * speedChangePerWave;
            AddToProjecileQueue(bulletCopy);
        }
    }

    public void SinglePattern(int waves, float speedChangePerWave, float startingSpeed = 100)
    {
        var startingDirection = GlobalPosition.DirectionTo(GameplayScreen.PlayerRef.Position);
        for (int i = 0; i < waves; i++)
        {
            var bulletCopy = MakeBulletCopy(DominantElement);
            bulletCopy.MovementNode.Direction = startingDirection;
            bulletCopy.MovementNode.Speed = startingSpeed + i * speedChangePerWave;
            AddToProjecileQueue(bulletCopy);
        }
    }


    public Bullet MakeBulletCopy(Globals.Element element)
    {
        var bulletCopy = (Bullet)ProjectileManager.LoadTemplate(ProjectileManager.BulletScenePath[element]);
        Bullet.CopyData(_bulletTemplates[$"Enemy_{element}_Bullet"], bulletCopy);
        return bulletCopy;
    }

    public void AddToProjecileQueue(Bullet projectile, int queue = 0)
    {
        if (MAXPROJECTILEQUEUE <= queue || queue <= -1)
            return;

        _projectileQueue[queue].Enqueue(projectile);
        //GD.Print("Added");
    }
}