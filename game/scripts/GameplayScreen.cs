using Godot;
using System;

public class GameplayScreen : Node2D
{
    public void _OnPlayerDeath()
    {
        ScreenManager.AddPopupToScreen(ScreenManager.LoseScreenPath);
    }

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
