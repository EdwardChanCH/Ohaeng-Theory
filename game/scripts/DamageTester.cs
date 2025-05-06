using Godot;
using System;

public class DamageTester : Node, IHarmful
{
    public int Damage { get; set; } = 1;

    public int GetDamage()
    {
        return Damage;
    }
}
