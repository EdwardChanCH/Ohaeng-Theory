using Godot;
using System;

public interface IHarmful
{
    // Return damage to be applied
    int GetDamage();

    // Return:
    // true  if it should harm Enemy/ Lesser Enemy
    // false if it should harm Player.
    bool IsFriendly();

    // Return false if collision check should be ignored.
    bool IsActive();

    // Remove the harmful object (Bullet class will QueueDespawn(), others will QueueFree()).
    // Deactivate bullet.
    void Kill();
}
