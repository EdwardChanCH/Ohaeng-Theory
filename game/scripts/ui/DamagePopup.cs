using Godot;
using System;

public class DamagePopup : Label
{
    [Export]
    public float ResetDelay = 0.3f;
    private float _resetTimer = 0.0f;

    private int _cumulativeDamage;

    public override void _Ready()
    {
        ResetDamage();
    }

    public override void _Process(float delta)
    {
        _resetTimer += delta;
        if(_resetTimer >= ResetDelay)
        {
            _resetTimer = 0.0f;
            ResetDamage();
        }
    }


    private void ResetDamage()
    {
        Visible = false;
        _cumulativeDamage = 0;
        Text = string.Empty;
    }

    public void AddToCumulativeDamage(int damage)
    {
        Visible = true;
        _cumulativeDamage += damage;
        Text = _cumulativeDamage.ToString();
        _resetTimer = 0.0f;
    }
}
