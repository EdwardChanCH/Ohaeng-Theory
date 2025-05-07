using Godot;
using System;

// This class is the entry point of the game.
public class Bullet : KinematicBody2D, IHarmful
{
    [Export]
    public Globals.Element Element { get; set; } = Globals.Element.None;

    [Export]
    public int Damage { get; set; } = 0;

    [Export]
    public bool Friendly { get; set; } = true;

    // - - - False-positive collision workaround - - -
    // How many physics ticks to wait before collision becomes active
    private const int _physicsTicksTimer = 1; // Must be > 0 or Godot will freak out
    private int _physicsTicks = -1;
    private bool _active = false;
    [Export]
    public bool Active
    {
        get { return _active; }
        set
        {
            if (value)
            {
                _physicsTicks = 0;
            }
            else
            {
                _active = false;
                _physicsTicks = -1;
            }
        }
    }
    // - - - False-positive collision workaround - - -

    // - - - Node Paths - - -

    [Export]
    public NodePath MovementNodePath { get; set; }

    public IMovement MovementNode;

    [Export]
    public NodePath SpriteNodePath { get; set; }
    public Sprite SpriteNode;

    [Export]
    public NodePath CollisionShape2DNodePath { get; set; }
    public CollisionShape2D CollisionShape2DNode;

    // - - - Node Paths - - -

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Initalize();
    }

    // Called every physics tick. 'delta' is the elapsed time since the previous tick.
    public override void _PhysicsProcess(float delta)
    {
        // Start a timer to enable collision check
        if (_physicsTicks <= -1)
        {
            // Disabled, do nothing
        }
        else if (_physicsTicks >= _physicsTicksTimer)
        {
            _active = true;
        }
        else
        {
            _physicsTicks += 1;
        }

        MoveAndSlide(MovementNode.CalculateVector(delta)); // Should be the last line in _PhysicsProcess()
    }

    public void Initalize()
    {
        MovementNode = GetNode<IMovement>(MovementNodePath);
        SpriteNode = GetNode<Sprite>(SpriteNodePath);
        CollisionShape2DNode = GetNode<CollisionShape2D>(CollisionShape2DNodePath);

        if (MovementNode == null || SpriteNode == null || CollisionShape2DNode == null)
        {
            GD.PrintErr("Error: Invalid export node path in Bullet.");
            return;
        }
    }

    // Copy data from another source bullet
    // Note: Export nodes are not copied, parent is not copied
    public static void CopyData(Bullet template, Bullet other)
    {
        other.Position = template.Position;
        other.Element = template.Element;
        other.Damage = template.Damage;
        other.Friendly = template.Friendly;
        other.Active = template.Active;

        other.MovementNode.Direction = template.MovementNode.Direction;
        other.MovementNode.Speed = template.MovementNode.Speed;

        // Defined in scene file, no need to copy
        //other.MovementNode;
        //other.SpriteNode.Texture;
        //other.CollisionShape2DNode;
    }

    // Need to call this once to move the bullet
    public void ChangeDirection(Vector2 direction)
    {
        MovementNode.Direction = direction;
    }

    public int GetDamage()
    {
        return Damage;
    }

    public bool IsFriendly()
    {
        return Friendly;
    }

    public bool IsActive()
    {
        return Active;
    }

    public void Kill()
    {
        Active = false;
        ProjectileManager.QueueDespawnProjectile(this); // Return to object pool
    }

    public Globals.Element GetElement()
    {
        return Element;
    }
}
