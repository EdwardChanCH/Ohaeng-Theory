using Godot;
using System;

public class ProjectileDespawnArea : Area2D
{
    public void _OnBodyExited(Node body)
    {
        if (body is IHarmful damageSource)
        {
            ProjectileManager.QueueDespawnProjectile(body);
        }
    }
}
