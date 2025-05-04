using Godot;
using System;

public class HealthComponent : Node
{
    [Signal]
    public delegate void DamageApplied(int damage);

    [Signal]
    public delegate void HealApplied(int heal);

    [Signal]
    public delegate void HealthDepleted();

    [Export]
    public int MaxHealth { get; set; } = 10;

    public int CurrentHealth { get; private set; }

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }

    public void ApplyDamage(int damage)
    {
        CurrentHealth -= damage;
        EmitSignal("DamageApplied", damage);

        if(CurrentHealth <= 0)
        {
            EmitSignal("HealthDepleted");
        }
    }

    public void ApplyDamage(IHarmful source)
    {
        CurrentHealth -= source.GetDamage();
        EmitSignal("DamageApplied", source.GetDamage());

        if (CurrentHealth <= 0)
        {
            EmitSignal("HealthDepleted");
        }
    }

    public void ApplyHeal(int heal)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + heal, 0, MaxHealth);
    }
}
