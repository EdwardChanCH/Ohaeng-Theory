using Godot;
using System;

public class GameplayScreen : Node2D
{

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    public void _OnDespawnAreaBodyExited(Node body)
    {
        if (body is Bullet)
        {
            ProjectileManager.QueueDespawnProjectile(body);
            //GD.Print("DespawnArea despawn Bullet.");
        }
        else if (body is LesserEnemyCharacter)
        {
            body.QueueFree();
            //GD.Print("DespawnArea queue free LesserEnemyCharacter.");
        }
    }
}
