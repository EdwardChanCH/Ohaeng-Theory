using Godot;
using System;

public interface IHarmful
{
    [Export]
    int Damage { get; set; }
}
