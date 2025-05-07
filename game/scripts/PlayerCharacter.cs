using Godot;
using System;
using System.Collections.Generic;

public class PlayerCharacter : KinematicBody2D
{
    [Export]
    public NodePath HealthComponentPath { get; private set; } = new NodePath();
    private HealthComponent _healthComponent;

    [Export]
    public NodePath HealthBarPath { get; private set; } = new NodePath();
    private ProgressBar _healthBar;

    [Export]
    public NodePath PlayerSpritePath { get; private set; } = new NodePath();
    private Sprite _playerSprite;

    [Export]
    public bool UseMouseDirectedInput { get; set; } = true;

    [Export]
    public bool UseToggleShootInput { get; set; } = true;

    [Export]
    public bool UseSmoothedMovemment { get; set; } = false;


    [Export]
    public float MaxMoveSpeed { get; set; } = 800.0f;
    public Vector2 MoveDirection { get; private set; } = Vector2.Zero; // Always normalized

    // Ever unit is 0.01 seonds
    [Export(PropertyHint.Range, "-100,100")]
    public int FireSpeed { get; set; } = 1;

    [Export(PropertyHint.Range, "0.01,100")]
    public float TimeSubtractionPerFireSpeedUnit { get; set; } = 0.01f;
   

    public Vector2 TargetLocation { get; private set; }

    public Vector2 Velocity { get; private set; }

    private bool _shouldShoot = false;

    // Need a timer component
    private float _fireDelay;
    private float _fireTimer = 0.0f;

    private Globals.Element _currentElement = Globals.Element.Water;

    private Dictionary<string, Bullet> _bulletTemplates = new Dictionary<string, Bullet>();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _healthComponent = GetNode<HealthComponent>(HealthComponentPath);
        _healthBar = GetNode<ProgressBar>(HealthBarPath);
        _playerSprite = GetNode<Sprite>(PlayerSpritePath);
  
        if (_healthComponent == null || _healthBar == null || _playerSprite == null)
        {
            GD.PrintErr("Error: PlayerController has invalid export variable path.");
            return;
        }

        _fireDelay = Mathf.Clamp(1.0f - (FireSpeed * TimeSubtractionPerFireSpeedUnit), 0.01f, 100.0f);
        AudioManager.SetSFXChannelVolume("res://assets/sfx/test/bang.wav", 0.2f);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (UseMouseDirectedInput)
        {
            TargetLocation = GetGlobalMousePosition();
            MoveDirection = Position.DirectionTo(TargetLocation); // Normalized
        }
        else
        {
            float yAxisMovement = 0;
            float xAxisMovement = 0;

            if (Input.IsActionPressed("Move_Up"))
            {
                yAxisMovement -= 1;
            }

            if (Input.IsActionPressed("Move_Down"))
            {
                yAxisMovement += 1;
            }

            if (Input.IsActionPressed("Move_Left"))
            {
                xAxisMovement -= 1;
            }

            if (Input.IsActionPressed("Move_Right") )
            {
                xAxisMovement += 1;
            }

            MoveDirection = new Vector2(xAxisMovement, yAxisMovement).Normalized(); // Normalized
        }

        if(!UseToggleShootInput)
        {
            _shouldShoot = Input.IsActionPressed("Shoot");
        }

        _fireTimer += delta;

        if(_fireTimer >= _fireDelay && _shouldShoot)
        {
            _fireTimer = 0;
            Shoot();
        }

        // TODO: Tilt character sprite, need lerp smoothing
        if (Velocity.x <= -300)
        {
            _playerSprite.RotationDegrees = -10;
        }
        else if (Velocity.x >= 300)
        {
            _playerSprite.RotationDegrees = 10;
        }
        else
        {
            _playerSprite.RotationDegrees = 0;
        }

        //GD.Print($"{_xAxisMovement} , {_yAxisMovement}"); // TODO test
        //GD.Print($"{MoveDirection.x} , {MoveDirection.y}"); // TODO test
    }

    public override void _Input(InputEvent @event)
    {
        //base._Input(@event);

        //_shouldShoot = @event.IsAction("Shoot");
        if(@event.IsActionPressed("Shoot") && UseToggleShootInput)
        {
            _shouldShoot = !_shouldShoot;
        }

        if (@event.IsActionPressed("Previous_Element"))
        {
            _currentElement = Globals.PreviousElement(_currentElement);
        }

        if (@event.IsActionPressed("Next_Element"))
        {
            _currentElement = Globals.NextElement(_currentElement);
        }

    }

    public override void _PhysicsProcess(float delta)
    {
        // TODO test only
        //GD.Print(_bulletTemplates[$"Player_{Globals.Element.Water}_Bullet"].Position);

        // Calculate player velocity
        if (UseMouseDirectedInput)
        {
            // Mouse control
            if (UseSmoothedMovemment)
            {
                // Smoothed movement
                float distanceToTarget = Position.DistanceTo(TargetLocation);
                float smoothFactor = Mathf.Clamp(10 * distanceToTarget / MaxMoveSpeed, 0, 1); // Decelerate when close to target
                Velocity = MoveDirection * MaxMoveSpeed * smoothFactor;
            }
            else
            {
                // Constant speed movement
                Velocity = MoveDirection * MaxMoveSpeed;
                float distanceToTarget = Position.DistanceTo(TargetLocation);
                float distanceAfter = MaxMoveSpeed * delta;

                // Check if it will overshoot
                if (distanceAfter > distanceToTarget)
                {
                    // TargetLocation == Position + Velocity * delta == Position + MoveDirection * distanceToTarget
                    Velocity = MoveDirection * distanceToTarget / delta;
                    //GD.Print("Overshoot prevented."); // TODO test
                }
            }
        }
        else
        {
            // Keyboard control
            Velocity = MoveDirection * MaxMoveSpeed;
        }

        MoveAndSlide(Velocity);
    }

    public void _OnHitboxBodyEntered(Node body)
    {
        if (body is IHarmful damageSource)
        {
            _healthComponent.ApplyDamage(damageSource);
            body.QueueFree();
        }
    }

    public void _OnHealthUpdate(int newHealth)
    {
        GD.Print("Hurt");
        _healthBar.Value = (float)newHealth / (float)_healthComponent.MaxHealth;
    }

    public void _OnHealthDepleted()
    {
        QueueFree();
    }

    private void UpdateSetting()
    {
        _shouldShoot = false;
    }

    private void Shoot()
    {
        // TODO All of these function calls can be stored in Callable()
        switch (_currentElement)
        {
            case Globals.Element.Water:
                ProjectileManager.EmitBulletLine(_bulletTemplates[$"Player_{_currentElement}_Bullet"], GetTree().Root, Position);
                break;
            case Globals.Element.Wood:
                ProjectileManager.EmitBulletWall(_bulletTemplates[$"Player_{_currentElement}_Bullet"], GetTree().Root, Position, 5, 10);
                break;
            case Globals.Element.Fire:
                ProjectileManager.EmitBulletRing(_bulletTemplates[$"Player_{_currentElement}_Bullet"], GetTree().Root, Position, 8);
                break;
            case Globals.Element.Earth:
                ProjectileManager.EmitBulletConeNarrow(_bulletTemplates[$"Player_{_currentElement}_Bullet"], GetTree().Root, Position, 5, Mathf.Deg2Rad(90));
                break;
            case Globals.Element.Metal:
                ProjectileManager.EmitBulletConeWide(_bulletTemplates[$"Player_{_currentElement}_Bullet"], GetTree().Root, Position, 5, Mathf.Deg2Rad(90));
                break;
            default:
                GD.PrintErr($"Error: Player cannot fire {_currentElement} bullet.");
            break;
        }
        
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
            _bulletTemplates[$"Player_{element}_Bullet"] = template;
        }

        foreach (Bullet bullet in _bulletTemplates.Values)
        {
            // Warning: DO NOT attach template nodes to a parent
            bullet.Initalize();
            bullet.SetCollisionLayerBit(Globals.PlayerProjectileLayerBit, true);
            bullet.MovementNode.Direction = Vector2.Right;
            bullet.MovementNode.Speed = 10000; // TODO tune speed
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
