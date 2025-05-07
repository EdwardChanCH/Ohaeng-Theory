using Godot;
using System;

// This class is the entry point of the game.
public class Bullet : KinematicBody2D, IHarmful
{
    [Export]
    public Globals.Element Element { get; set; } = Globals.Element.None;

    [Export]
    public int Damage { get; set; } = 0;

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

    // Copy data from another source bullet
    // Note: Export nodes are not copied, parent is not copied
    public static void CopyData(Bullet template, Bullet other)
    {
        other.CollisionLayer = template.CollisionLayer;
        other.CollisionMask = template.CollisionMask;
        other.Position = template.Position;
        other.Element = template.Element;
        other.Damage = template.Damage;

        other.MovementNode.Direction = template.MovementNode.Direction;
        other.MovementNode.Speed = template.MovementNode.Speed;

        // Defined in scene file, no need to copy
        //other.MovementNode;
        //other.SpriteNode.Texture;
        //other.CollisionShape2DNode;
    }

    public int GetDamage()
    {
        return Damage;
    }

    // Need to call this once to move the bullet
    public void ChangeDirection(Vector2 direction)
    {
        MovementNode.Direction = direction;
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

    // Note: Use this as the Main() method.
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Initalize();
    }

    // Called every physics tick. 'delta' is the elapsed time since the previous tick.
    public override void _PhysicsProcess(float delta)
    {
        MoveAndSlide(MovementNode.CalculateVector(delta)); // Should be the last line in _PhysicsProcess()
    }

}
