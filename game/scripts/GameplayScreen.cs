using Godot;
using System;

public class GameplayScreen : Node2D
{
/*     // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    } */

    public void _OnDespawnAreaBodyExited(Node body)
    {
        if (body is IHarmful harmful)
        {
            if (body is Bullet bullet)
            {
                ProjectileManager.QueueDespawnProjectile(bullet);
            }
            else if (body is LesserEnemyCharacter lesser)
            {
                lesser.QueueFree();
            }
        }
    }

}
