using Godot;
using System;
using System.Collections.Generic;

// Note: NOT a singleton
public class PlayerCharacter : KinematicBody2D
{
    [Signal]
    public delegate void PlayerDeath();

    [Export]
    public NodePath HealthComponentPath { get; private set; } = new NodePath();
    public HealthComponent PlayerHealthComponent;

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
    public float DefaultMoveSpeed { get; set; } = 800.0f;
    [Export]
    public float SlowMoveSpeed { get; set; } = 400.0f;
    public Vector2 MoveDirection { get; private set; } = Vector2.Zero; // Always normalized

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
    private int _fireSpeed = 60;
    private float _fireDelay = 1;
    private float _fireTimer = 0.0f;

    [Export]
    public float SpriteTilt = 10;

    [Export]
    public float SpriteTiltSpeed = 10.0f;

    public Vector2 TargetLocation { get; private set; }

    public Vector2 Velocity { get; private set; }

    private bool _shouldShoot = false;

    private bool _ShouldSlowMovement = false;


    private Globals.Element _currentElement = Globals.Element.Water;

    private Dictionary<string, Bullet> _bulletTemplates = new Dictionary<string, Bullet>();

    private float _firingAudioDelay = 0.1f;
    private float _firingAudioTimer = 1f;

    public override void _EnterTree()
    {
        base._EnterTree();

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
            bullet.Damage = 5; // The damage should be decided on the scene end
            bullet.Friendly = true;
            bullet.MovementNode.Direction = Vector2.Right;
            bullet.MovementNode.Speed = 1000; // TODO tune speed
        }




        // - - - Initialize Player Bullet Templates - - -
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

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        PlayerHealthComponent = GetNode<HealthComponent>(HealthComponentPath);
        _healthBar = GetNode<ProgressBar>(HealthBarPath);
        _playerSprite = GetNode<Sprite>(PlayerSpritePath);
  
        if (PlayerHealthComponent == null || _healthBar == null || _playerSprite == null)
        {
            GD.PrintErr("Error: PlayerController has invalid export variable path.");
            return;
        }

        AudioManager.SetSFXChannelVolume("res://assets/sfx/test/bang.wav", 0.2f);
    }
    public override void _Input(InputEvent @event)
    {
        if(@event.IsActionPressed("Shoot") && UseToggleShootInput)
        {
            _shouldShoot = !_shouldShoot;
        }

        if (@event.IsActionPressed("Slow_Down") && UseToggleSlowInput)
        {
            _ShouldSlowMovement = !_ShouldSlowMovement;
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

        if (!UseToggleSlowInput)
        {
            _ShouldSlowMovement = Input.IsActionPressed("Slow_Down");
        }

        _fireTimer += delta;
        _firingAudioTimer += delta;
        if (_fireTimer >= _fireDelay && _shouldShoot)
        {
            _fireTimer = 0;
            Shoot();
        }


        _playerSprite.RotationDegrees = Mathf.Lerp(_playerSprite.RotationDegrees, SpriteTilt * MoveDirection.x, delta * SpriteTiltSpeed);


        //GD.Print($"{_xAxisMovement} , {_yAxisMovement}"); // TODO test
        //GD.Print($"{MoveDirection.x} , {MoveDirection.y}"); // TODO test
    }

    public override void _PhysicsProcess(float delta)
    {
        // TODO test only
        //GD.Print(_bulletTemplates[$"Player_{Globals.Element.Water}_Bullet"].Position);
        float moveSpeed;
        if (!_ShouldSlowMovement)
        {
            moveSpeed = DefaultMoveSpeed;
        }
        else
        {
            moveSpeed = SlowMoveSpeed;
        }



        // Calculate player velocity
        if (UseMouseDirectedInput) // TODO use Globals.GameData
        {
            // Mouse control
            if (UseSmoothedMovemment)
            {
                // Smoothed movement
                float distanceToTarget = Position.DistanceTo(TargetLocation);
                float smoothFactor = Mathf.Clamp(10 * distanceToTarget / moveSpeed, 0, 1); // Decelerate when close to target
                Velocity = MoveDirection * moveSpeed * smoothFactor;
            }
            else
            {
                // Constant speed movement
                Velocity = MoveDirection * moveSpeed;
                float distanceToTarget = Position.DistanceTo(TargetLocation);
                float distanceAfter = moveSpeed * delta;

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
            Velocity = MoveDirection * moveSpeed;
        }

        MoveAndSlide(Velocity);
    }

    // Called when other hitbody has enter the body
    public void _OnHitboxBodyEntered(Node body)
    {
        if (body is IHarmful harmful && !harmful.IsFriendly() && harmful.IsActive())
        {
            PlayerHealthComponent.ApplyDamage(harmful.GetDamage());
            harmful.Kill();
        }
    }

    // Called when health value got change
    public void _OnHealthUpdate(int newHealth)
    {
        _healthBar.Value = (float)newHealth / (float)PlayerHealthComponent.MaxHealth;
    }

    // Called when health is deplated
    public void _OnHealthDepleted()
    {
        EmitSignal("PlayerDeath");
    }
    
    // Called when any setting got change
    private void UpdateSetting(string key, string value)
    {
        if(key == "ToggleSlow")
        {
            _ShouldSlowMovement = false;
        }
        if(key == "ToggleAttack")
        {
            _shouldShoot = false;
        }


        UseMouseDirectedInput = Globals.String2Bool(Globals.GameData["UseMouseDirectedInput"]);
        UseToggleShootInput = Globals.String2Bool(Globals.GameData["ToggleAttack"]);
        UseToggleSlowInput = Globals.String2Bool(Globals.GameData["ToggleSlow"]);
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
        
        if(_firingAudioTimer >= _firingAudioDelay)
        {
            AudioManager.PlaySFX("res://assets/sfx/test/bang.wav");
            _firingAudioTimer = 0;
        }
    }

}
