using Godot;
using System;

public class PlayerCharacter : KinematicBody2D
{
    [Export]
    public NodePath HealthComponentPath = new NodePath();
    private HealthComponent _healthComponent;

    [Export]
    public bool UseMouseDirectedInput = true;

    [Export]
    public float MoveSpeed { get; set; } = 100.0f;
    public Vector2 MoveDirection { get; private set; } = new Vector2();


    public Vector2 TargetLocation { get; private set; }

    public Vector2 Velocity { get; private set; }

    // Use to control movement at _PhysicsProcess()
    private float _yAxisMovement = 0;
    private float _xAxisMovement = 0;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _healthComponent = GetNode<HealthComponent>(HealthComponentPath);
        if (_healthComponent == null)
        {
            GD.PrintErr("Error: Player Controller Contrain Invalid Path");
            return;
        }
    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
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
        GD.Print(Velocity.Length());
        MoveAndSlide(Velocity);
    }

    public void _OnHitboxBodyEntered(Node body)
    {
        if (body is IHarmful damageSource)
        {
            _healthComponent.ApplyDamage(damageSource);
            GD.Print("Hurt");
        }
    }
}
