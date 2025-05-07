using Godot;
using System;

public interface IHarmful
{
    int CollisionFlag { get; set; }

    int GetDamage();
}
