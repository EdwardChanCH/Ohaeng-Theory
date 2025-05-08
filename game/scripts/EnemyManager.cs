using Godot;
using System;
using System.Collections.Generic;

// This class manages the spawning, splitting, and merging of enemy and lesser enemy.
// Note: NOT a singleton
public class EnemyManager : Node2D
{
    public static List<EnemyCharacter> EnemyList = new List<EnemyCharacter>();

    

    public override void _EnterTree()
    {
        base._EnterTree();

        
    }

    public override void _ExitTree()
    {
        base._ExitTree();


    }

    public override void _Ready()
    {
        base._Ready();


    }

    public void Spawn()
    {
        
    }

    public void Split(EnemyCharacter enemy)
    {
        // TODO
    }

    public void Merge(EnemyCharacter enemyA, EnemyCharacter enemyB)
    {
        // TODO
    }


}
