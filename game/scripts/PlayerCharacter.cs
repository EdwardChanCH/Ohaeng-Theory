using Godot;
using System;

public class PlayerCharacter : KinematicBody2D
{
    [Export]
    public float MoveSpeed { get; set; } = 100.0f;
    public Vector2 MoveDirection { get; private set; } = new Vector2();


    // Use to control movement at _PhysicsProcess()
    private float _yAxisMovement = 0;
    private float _xAxisMovement = 0;

    // The zone the player can move in
    //[Export]
    //public Vector2 MinMovementBound { get; private set; } = Vector2.Zero;
    //[Export]
    //public Vector2 MaxMovementBound { get; private set; } = Vector2.Zero;

    [Export]
    public NodePath HealthComponentPath = new NodePath();
    private HealthComponent _healthComponent;


    protected int PlayerScore = 0;


    public Vector2 TargetLocation { get; private set; }


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
        TargetLocation = GetViewport().GetMousePosition();
        MoveDirection = Position.DirectionTo(TargetLocation);
    }

    public override void _PhysicsProcess(float delta)
    {
        var distance = Position.DistanceTo(TargetLocation);

        // TD: Clean this
        MoveAndSlide(MoveDirection * (MoveSpeed * Mathf.Clamp(distance * 10 / MoveSpeed, 0, 1)));
    }

    public void _OnHitboxBodyEntered(Node body)
    {
        if (body is IHarmful damageSource)
        {
            _healthComponent.ApplyDamage(damageSource);
            //EmitSignal("DamageTaken", 1);
            GD.Print("Hurt");
        }
    }
}
