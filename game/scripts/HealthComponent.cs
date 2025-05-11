using Godot;
using System;

public class HealthComponent : Node
{
    [Signal]
    public delegate void HealthUpdate(int newHealth);

    [Signal]
    public delegate void HealthDepleted();

    [Export]
    public int MaxHealth { get; set; } = 100;

    public int CurrentHealth { get; private set; } = 100;

    public override void _Ready()
    {
        CurrentHealth = MaxHealth;
    }

    public void ApplyDamage(int damage)
    {
        SetHealth(CurrentHealth - damage);
    }

    public void ApplyHeal(int heal)
    {
        SetHealth(CurrentHealth + heal);
    }

    public void SetHealth(int newHealth)
    {
        if (newHealth <= 0)
        {
            EmitSignal("HealthDepleted");
        }
        else if (newHealth > MaxHealth)
        {
            GD.PrintErr($"Error: New health {newHealth} cannot exceed max health {MaxHealth}.");
            CurrentHealth = MaxHealth;
            EmitSignal("HealthUpdate", CurrentHealth);
        }
        else
        {
            CurrentHealth = newHealth;
            EmitSignal("HealthUpdate", CurrentHealth);
        }
    }
}
