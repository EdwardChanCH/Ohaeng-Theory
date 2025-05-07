using Godot;
using System;
using System.Collections.Generic;

public class PlayerCharacter : KinematicBody2D
{
    [Export]
    public NodePath HealthComponentPath { get; private set; } = new NodePath();
    public HealthComponent HealthComponent;

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
    public bool UseToggleSlowInput { get; set; } = true;

    [Export]
    public bool UseSmoothedMovemment { get; set; } = false;


    [Export]
    public float MaxMoveSpeed { get; set; } = 800.0f;
    public Vector2 MoveDirection { get; private set; } = Vector2.Zero; // Always normalized

    // Bullet per second
    // DON'T SET THIS TO 0
    private int _fireSpeed = 60;
    private float _fireDelay = 1;
    [Export]
    public int FireSpeed {
        get { return _fireSpeed; }
        set
        {
            if (value <= 0)
            {
                GD.PrintErr("Error: FireSpeed must be > 0.");
                return;
            }

            _fireSpeed = value;
            _fireDelay = 1.0f / value;
        }
    }

    public Vector2 TargetLocation { get; private set; }

    public Vector2 Velocity { get; private set; }

    private bool _shouldShoot = false;

    // Need a timer component
    private float _fireTimer = 0.0f;

    private Globals.Element _currentElement = Globals.Element.Water;

    private Dictionary<string, Bullet> _bulletTemplates = new Dictionary<string, Bullet>();

    private static PlayerCharacter _instance; // TODO Move this to GameplayScreen.cs

    public override void _EnterTree()
    {
        base._EnterTree();

        _instance = this; // TODO Move this to GameplayScreen.cs

        Globals.Singleton.Connect("GameDataChanged", this, "UpdateSetting");
        UseMouseDirectedInput = Globals.String2Bool(Globals.GameData["UseMouseDirectedInput"]);
        UseToggleShootInput = Globals.String2Bool(Globals.GameData["ToggleAttack"]);
        UseToggleSlowInput = Globals.String2Bool(Globals.GameData["ToggleSlow"]);

        // - - - Initialize Player Bullet Templates - - -

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
            bullet.Position = Vector2.Zero;
            bullet.Damage = 1;
            bullet.Friendly = true;
            bullet.MovementNode.Direction = Vector2.Right;
            bullet.MovementNode.Speed = 1000; // TODO tune speed
        }

        // - - - Initialize Player Bullet Templates - - -
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if(_instance == this)
        {
            _instance = null; // TODO Move this to GameplayScreen.cs
        }

        // Free the bullet templates
        foreach (Bullet bullet in _bulletTemplates.Values)
        {
            bullet.QueueFree();
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        HealthComponent = GetNode<HealthComponent>(HealthComponentPath);
        _healthBar = GetNode<ProgressBar>(HealthBarPath);
        _playerSprite = GetNode<Sprite>(PlayerSpritePath);
  
        if (HealthComponent == null || _healthBar == null || _playerSprite == null)
        {
            GD.PrintErr("Error: PlayerController has invalid export variable path.");
            return;
        }

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

        if (@event.IsActionPressed("Open_Setting_Menu"))
        {
            ScreenManager.AddPopupToScreen(ScreenManager.SettingsScreenPath);
        }
    }

    public override void _PhysicsProcess(float delta)
    {
        // TODO test only
        //GD.Print(_bulletTemplates[$"Player_{Globals.Element.Water}_Bullet"].Position);

        // Calculate player velocity
        if (UseMouseDirectedInput) // TODO use Globals.GameData
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
        if (body is IHarmful harmful && !harmful.IsFriendly() && harmful.IsActive())
        {
            HealthComponent.ApplyDamage(harmful.GetDamage());
            harmful.Kill();
        }
    }

    public void _OnHealthUpdate(int newHealth)
    {
        //GD.Print("Hurt");
        _healthBar.Value = (float)newHealth / (float)HealthComponent.MaxHealth;
    }

    public void _OnHealthDepleted()
    {
        //QueueFree(); // TODO Add a publlic Kill() function
        DisableInput();
        ScreenManager.AddPopupToScreen(ScreenManager.LoseScreenPath);

    }

    private void UpdateSetting(string key, string value)
    {
        _shouldShoot = false;

        UseMouseDirectedInput = Globals.String2Bool(Globals.GameData["UseMouseDirectedInput"]);
        UseToggleShootInput = Globals.String2Bool(Globals.GameData["ToggleAttack"]);
        UseToggleSlowInput = Globals.String2Bool(Globals.GameData["ToggleSlow"]);
    }

    public static void EnableInput() // TODO Move this to GameplayScreen.cs
    {
        _instance?.SetProcess(true);
        _instance?.SetPhysicsProcess(true);
        _instance?.SetProcessInput(true);
    }

    public static void DisableInput() // TODO Move this to GameplayScreen.cs
    {
        _instance?.SetProcess(false);
        _instance?.SetPhysicsProcess(false);
        _instance?.SetProcessInput(false);
    }

    private void Shoot()
    {
        // TODO All of these function calls can be stored in Callable()
        switch (_currentElement)
        {
            case Globals.Element.Water:
                // Edit the bullet template instead of the function parameters
                ProjectileManager.EmitBulletLine(_bulletTemplates[$"Player_{_currentElement}_Bullet"], GetTree().Root, Position);
                break;
            case Globals.Element.Wood:
                // Edit the bullet template instead of the function parameters
                ProjectileManager.EmitBulletWall(_bulletTemplates[$"Player_{_currentElement}_Bullet"], GetTree().Root, Position, 5, 10);
                break;
            case Globals.Element.Fire:
                // Edit the bullet template instead of the function parameters
                ProjectileManager.EmitBulletRing(_bulletTemplates[$"Player_{_currentElement}_Bullet"], GetTree().Root, Position, 8);
                break;
            case Globals.Element.Earth:
                // Edit the bullet template instead of the function parameters
                ProjectileManager.EmitBulletConeNarrow(_bulletTemplates[$"Player_{_currentElement}_Bullet"], GetTree().Root, Position, 5, Mathf.Deg2Rad(90));
                break;
            case Globals.Element.Metal:
                // Edit the bullet template instead of the function parameters
                ProjectileManager.EmitBulletConeWide(_bulletTemplates[$"Player_{_currentElement}_Bullet"], GetTree().Root, Position, 5, Mathf.Deg2Rad(90));
                break;
            default:
                GD.PrintErr($"Error: Player cannot fire {_currentElement} bullet.");
            break;
        }
        
        AudioManager.PlaySFX("res://assets/sfx/test/bang.wav");
    }

}
