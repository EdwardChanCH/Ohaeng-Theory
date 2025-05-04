using Godot;
using System;
using System.Collections.Generic;

// This singleton class contains global constants.
// Note: Godot autoload requires Node type.
public class Globals : Node
{
    // Collision Layers
    public const int GroundLayerBit = 0; // Layer 1
    public const int PlayerLayerBit = 1; // Layer 2
    public const int PlayerProjectileLayerBit = 2; // Layer 3
    public const int EnemyLayerBit = 3; // Layer 4
    public const int EnemyProjectileLayerBit = 4; // Layer 5

    public static Globals Singleton { get; private set; }

    public static Dictionary<string, string> GameData = new Dictionary<string, string>();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Singleton = this;
    }

}