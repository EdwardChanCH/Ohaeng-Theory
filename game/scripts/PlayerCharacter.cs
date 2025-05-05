using Godot;
using System;

public class PlayerCharacter : KinematicBody2D
{
    [Export]
    public NodePath HealthComponentPath { get; private set; } = new NodePath();
    private HealthComponent _healthComponent;

    [Export]
    public NodePath HealthBarPath { get; private set; } = new NodePath();
    private ProgressBar _healthBar;

    [Export]
    public bool UseMouseDirectedInput { get; set; }= true;

    [Export]
    public bool UseToggleShootInput { get; set; } = true;


    [Export]
    public float MoveSpeed { get; set; } = 100.0f;
    public Vector2 MoveDirection { get; private set; } = new Vector2();

    // Ever unit is 0.01 seonds
    [Export(PropertyHint.Range, "-100,100")]
    public int FireSpeed { get; set; } = 1;

    [Export(PropertyHint.Range, "0.01,100")]
    public float TimeSubtractionPerFireSpeedUnit { get; set; } = 0.01f;
   

    public Vector2 TargetLocation { get; private set; }

    public Vector2 Velocity { get; private set; }

    // Use to control movement at _PhysicsProcess()
    private float _yAxisMovement = 0;
    private float _xAxisMovement = 0;

    private bool _shouldShoot = false;

    // Need a timer component
    private float _fireDelay;
    private float _fireTimer = 0.0f;

    private Globals.Element _currentElement = Globals.Element.Metal;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _healthComponent = GetNode<HealthComponent>(HealthComponentPath);
        _healthBar = GetNode<ProgressBar>(HealthBarPath);
        if (_healthComponent == null || _healthBar == null)
        {
            GD.PrintErr("Error: Player Controller Contrain Invalid Path");
            return;
        }

        _fireDelay = Mathf.Clamp(1.0f - (FireSpeed * TimeSubtractionPerFireSpeedUnit), 0.01f, 100.0f);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (UseMouseDirectedInput)
        {
            TargetLocation = GetViewport().GetMousePosition();
            MoveDirection = Position.DirectionTo(TargetLocation);
        }
        else
        {
            _yAxisMovement = 0;
            _xAxisMovement = 0;

            if (Input.IsActionPressed("Move_Up"))
            {
                _yAxisMovement -= 1;
            }

            if (Input.IsActionPressed("Move_Down"))
            {
                _yAxisMovement += 1;
            }

            if (Input.IsActionPressed("Move_Left"))
            {
                _xAxisMovement -= 1;
            }

            if (Input.IsActionPressed("Move_Right") )
            {
                _xAxisMovement += 1;
            }

            MoveDirection = new Vector2(_xAxisMovement, _yAxisMovement);
        }

        if(!UseToggleShootInput)
        {
            _shouldShoot = Input.IsActionPressed("Shoot");
        }
            //GD.Print(_shouldShoot);


        _fireTimer += delta;
        if(_fireTimer >= _fireDelay && _shouldShoot)
        {
            _fireTimer = 0;
            //GD.Print("Shoot");
            TestShoot();
        }
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
            if((int)_currentElement <= 1)
            {
                _currentElement = Globals.Element.Earth;
                return;
            }
            _currentElement--;
        }

        if (@event.IsActionPressed("Next_Element"))
        {
            if ((int)_currentElement >= 5)
            {
                _currentElement = Globals.Element.Metal;
                return;
            }
            _currentElement++;
        }

    }

    public override void _PhysicsProcess(float delta)
    {
        Velocity = Vector2.Zero;
        // TD: Clean this
        if(UseMouseDirectedInput)
        {
            var distance = Position.DistanceTo(TargetLocation);
            Velocity = MoveDirection * (MoveSpeed * Mathf.Clamp(distance * 10 / MoveSpeed, 0, 1));
        }
        else
        {
            Velocity = MoveDirection * MoveSpeed;
            Velocity = Velocity.LimitLength(MoveSpeed);
        }
        //GD.Print(_currentElement);
        MoveAndSlide(Velocity);
    }

    public void _OnHitboxBodyEntered(Node body)
    {
        if (body is IHarmful damageSource)
        {
            _healthComponent.ApplyDamage(damageSource);
            body.QueueFree();
            //GD.Print("Hurt");
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



    public void TestShoot()
    {
        // - - - Should be done by projectie manager - - -
        Bullet testBullet = GD.Load<PackedScene>("res://scenes/test_bullet_2.tscn").Instance<Bullet>();
        testBullet.Position = this.Position;
        testBullet.Damage = 1;
        testBullet.InitialDirection = Vector2.Right;
        testBullet.SetCollisionLayerBit(Globals.PlayerProjectileLayerBit, true);
        GetTree().Root.CallDeferred("add_child", testBullet);
        // - - - Should be done by projectie manager - - -
    }
}
