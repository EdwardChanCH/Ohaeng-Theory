using Godot;
using System;

public class GameplayScreen : Node2D
{
    public static Node2D PlayerRef;

    public void _OnPlayerDeath()
    {
        ScreenManager.AddPopupToScreen(ScreenManager.LoseScreenPath);
    }

    public void _OnDespawnAreaBodyExited(Node body)
    {
        if (body is IHarmful harmful)
        {

            harmful.Kill(); // Works on Bullet, Enemy, and Lesser Enemy

/*          // Unnecessary code
            if (body is Bullet bullet)
            {
                ProjectileManager.QueueDespawnProjectile(bullet);
            }
            else if (body is LesserEnemyCharacter lesser)
            {
                lesser.QueueFree();
            } */
        }
    }

}
