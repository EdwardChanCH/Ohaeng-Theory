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
    public NodePath KiteSpritePath { get; private set; } = new NodePath();
    private Sprite _KiteSprite;

    [Export]
    public NodePath ElementPath { get; private set; } = new NodePath();
    private ElementCircle _elementCircle;

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
    public float SlowMoveSpeed { get; set; } = 200.0f;
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

    // The zone the player can move in
    [Export]
    public NodePath MinMovementBoundPath { get; set; }
    [Export]
    public NodePath MaxMovementBoundPath { get; set; }

    public Vector2 _minMovementBoundVector { get; private set; } = Vector2.Zero;
    public Vector2 _maxMovementBoundVector { get; private set; } = Vector2.Zero;


    public Vector2 TargetLocation { get; private set; }

    public Vector2 Velocity { get; private set; }

    private bool _shouldShoot = false;

    private bool _ShouldSlowMovement = false;


    private Globals.Element _currentElement = Globals.Element.Water;

    private int _CurrentPattern = 0;
    private int _MaxPattern = 3;

    private Dictionary<string, Bullet> _bulletTemplates = new Dictionary<string, Bullet>();

    private float _firingAudioDelay = 0.1f;
    private float _firingAudioTimer = 1f;

    private float _elementCircleHideDelay = 1.0f;
    private float _elementCircleTimer = 0;

    [Export]
    public Vector2 ShootOffset = Vector2.Zero;

    public override void _EnterTree()
    {
        base._EnterTree();
        GameplayScreen.PlayerRef = this;
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
            bullet.Damage = 5;
            bullet.Friendly = true;
            bullet.MovementNode.Direction = Vector2.Right;
            bullet.MovementNode.Speed = 2000; // TODO tune speed
        }

        // - - - Initialize Player Bullet Templates - - -






    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if(GameplayScreen.PlayerRef == this)
            GameplayScreen.PlayerRef = null;

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
        _elementCircle = GetNode<ElementCircle>(ElementPath);
        _KiteSprite = GetNode<Sprite>(KiteSpritePath);

        var minBound = GetNode<Node2D>(MinMovementBoundPath);
        var maxbound = GetNode<Node2D>(MaxMovementBoundPath);

        if (minBound != null && maxbound != null)
        {
            _minMovementBoundVector = minBound.GlobalPosition;
            _maxMovementBoundVector = maxbound.GlobalPosition;
        }
        else
        {
            _minMovementBoundVector = GetViewportRect().Position;
            _maxMovementBoundVector = GetViewportRect().End;
        }

        _elementCircle.SetElement(_currentElement);

        AudioManager.SetSFXChannelVolume("res://assets/sfx/test/bang.wav", 0.2f);

        //AudioManager.SetSFXChannelVolume("res://assets/sfx/rpg_essentials_free/8_Atk_Magic_SFX/22_Water_02.wav", 0.2f);
        //AudioManager.SetSFXChannelVolume("res://assets/sfx/rpg_essentials_free/8_Atk_Magic_SFX/25_Wind_01.wav", 0.2f);
        //AudioManager.SetSFXChannelVolume("res://assets/sfx/rpg_essentials_free/8_Atk_Magic_SFX/04_Fire_explosion_04_medium.wav", 0.2f);
        //AudioManager.SetSFXChannelVolume("res://assets/sfx/rpg_essentials_free/8_Atk_Magic_SFX/30_Earth_02.wav", 0.2f);
        //AudioManager.SetSFXChannelVolume("res://assets/sfx/rpg_essentials_free/8_Atk_Magic_SFX/13_Ice_explosion_01.wav", 0.2f);
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
            _elementCircle.SetElement(_currentElement);
            _elementCircleTimer = _elementCircleHideDelay;
            AudioManager.PlaySFX("res://assets/sfx/rpg_essentials_free/10_UI_Menu_SFX/092_Pause_04.wav");
        }

        if (@event.IsActionPressed("Next_Element"))
        {
            _currentElement = Globals.NextElement(_currentElement);
            _elementCircle.SetElement(_currentElement);
            _elementCircleTimer = _elementCircleHideDelay;
            AudioManager.PlaySFX("res://assets/sfx/rpg_essentials_free/10_UI_Menu_SFX/092_Pause_04.wav");
        }

        if (@event.IsActionPressed("Open_Setting_Menu"))
        {
            ScreenManager.AddPopupToScreen(ScreenManager.SettingsScreenPath);
        }

        if (@event.IsActionPressed("Switch_Pattern"))
        {
            _CurrentPattern++;
            if (_CurrentPattern >= _MaxPattern)
            {
                _CurrentPattern = 0;
            }
            AudioManager.PlaySFX("res://assets/sfx/rpg_essentials_free/10_UI_Menu_SFX/098_Unpause_04.wav");
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (UseMouseDirectedInput)
        {
            var mouseLocation = GetGlobalMousePosition();
            float targetY = mouseLocation.y;
            float targetX = mouseLocation.x;

            if(_minMovementBoundVector.y >= targetY)
                targetY = _minMovementBoundVector.y;
            if(_maxMovementBoundVector.y <= targetY)
                targetY = _maxMovementBoundVector.y;
            if(_minMovementBoundVector.x >= targetX)
                targetX = _minMovementBoundVector.x;
            if(_maxMovementBoundVector.x <= targetX)
                targetX = _maxMovementBoundVector.x;

            TargetLocation = new Vector2(targetX, targetY);
            MoveDirection = Position.DirectionTo(TargetLocation); // Normalized
        }
        else
        {
            float yAxisMovement = 0;
            float xAxisMovement = 0;


            if (Input.IsActionPressed("Move_Up") && _minMovementBoundVector.y < Position.y)
            {
                yAxisMovement -= 1;
            }
            if (Input.IsActionPressed("Move_Down") && _maxMovementBoundVector.y > Position.y)
            {
                yAxisMovement += 1;
            }
            if (Input.IsActionPressed("Move_Left") && _minMovementBoundVector.x < Position.x)
            {
                xAxisMovement -= 1;
            }
            if (Input.IsActionPressed("Move_Right") && _maxMovementBoundVector.x > Position.x)
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
        _KiteSprite.GlobalRotationDegrees = Mathf.Lerp(_KiteSprite.GlobalRotationDegrees, SpriteTilt * -MoveDirection.x, delta * SpriteTiltSpeed);

        _elementCircleTimer -= delta;
        _elementCircle.SetAlpha(Mathf.Clamp(_elementCircleTimer, 0, _elementCircleHideDelay) / _elementCircleHideDelay);

        //GD.Print($"{_xAxisMovement} , {_yAxisMovement}"); // TODO test
        //GD.Print($"{MoveDirection.x} , {MoveDirection.y}"); // TODO test
    }

    public override void _PhysicsProcess(float delta)
    {
        float moveSpeed;
        if (_ShouldSlowMovement)
        {
            moveSpeed = SlowMoveSpeed;
        }
        else
        {
            moveSpeed = DefaultMoveSpeed;
        }


        // Calculate player velocity
        if (UseMouseDirectedInput)
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

        MoveAndSlide(Velocity); // Should be the last line in _PhysicsProcess()
    }

    // Called when other hitbody has enter the body
    public void _OnHitboxBodyEntered(Node body)
    {
        if (body is IHarmful harmful && !harmful.IsFriendly() && harmful.IsActive())
        {
            PlayerHealthComponent.ApplyDamage(harmful.GetDamage());
            AudioManager.PlaySFX("res://assets/sfx/rpg_essentials_free/12_Player_Movement_SFX/61_Hit_03.wav");
            harmful.Kill(); // Works on Bullet, Enemy, and Lesser Enemy
        }
    }

    // Called when health value got change
    public void _OnHealthUpdate(int newHealth)
    {
        //GD.Print("Take damage");
        _healthBar.Value = (float)newHealth / (float)PlayerHealthComponent.MaxHealth;
    }

    // Called when health is deplated
    public void _OnHealthDepleted()
    {
        EmitSignal("PlayerDeath");
    }
    
    public void _OnWaveComplete()
    {
        PlayerHealthComponent.SetHealth(PlayerHealthComponent.MaxHealth);
        Globals.SetScore(Globals.Score + 2000);
    }

    // Called when any setting got change
    private void UpdateSetting(string key, string value)
    {
        GD.Print(key);
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
        switch (_CurrentPattern)
        {
            case 0:
                ProjectileManager.EmitBulletWall(_bulletTemplates[$"Player_{_currentElement}_Bullet"], GetTree().Root, GlobalPosition + ShootOffset, 4, 10);
                break;
            case 1:
                ProjectileManager.EmitBulletConeNarrow(_bulletTemplates[$"Player_{_currentElement}_Bullet"], GetTree().Root, GlobalPosition + ShootOffset, 6, Mathf.Deg2Rad(45));
                break;
            case 2:
                ProjectileManager.EmitBulletConeWide(_bulletTemplates[$"Player_{_currentElement}_Bullet"], GetTree().Root, GlobalPosition + ShootOffset, 6, Mathf.Deg2Rad(180));
                break;
            default:
                GD.PrintErr($"Error: Player cannot fire {_currentElement} bullet.");
                break;
        }
        
        if(_firingAudioTimer >= _firingAudioDelay)
        {

            switch (_currentElement)
            {
                case Globals.Element.None:
                    AudioManager.PlaySFX("res://assets/sfx/test/bang.wav");
                    break;

                case Globals.Element.Water:
                    AudioManager.PlaySFX("res://assets/sfx/rpg_essentials_free/8_Atk_Magic_SFX/22_Water_02.wav");
                    break;

                case Globals.Element.Wood:
                    AudioManager.PlaySFX("res://assets/sfx/rpg_essentials_free/8_Atk_Magic_SFX/25_Wind_01.wav");
                    break;

                case Globals.Element.Fire:
                    AudioManager.PlaySFX("res://assets/sfx/rpg_essentials_free/8_Atk_Magic_SFX/04_Fire_explosion_04_medium.wav");
                    break;

                case Globals.Element.Earth:
                    AudioManager.PlaySFX("res://assets/sfx/rpg_essentials_free/8_Atk_Magic_SFX/30_Earth_02.wav");
                    break;

                case Globals.Element.Metal:
                    AudioManager.PlaySFX("res://assets/sfx/rpg_essentials_free/8_Atk_Magic_SFX/13_Ice_explosion_01.wav");
                    break;
            }


            _firingAudioTimer = 0;
        }
    }
}
