using Godot;
using System;
using System.Collections.Generic;

// This class manages the spawning, splitting, and merging of enemy and lesser enemy.
// Note: NOT a singleton
public class EnemyManager : Node2D
{
    public const string EnemyCharacterPath = "res://scenes/enemy_character.tscn";
    public const string LesserEnemyCharacterPath = "res://scenes/lesser_enemy_character.tscn";
    public static PackedScene EnemyCharacterScene = null;
    public static PackedScene LesserEnemyCharacterScene = null;

    // The zone the enemy/ lesser enemy can spawn in
    [Export]
    public NodePath MinSpawnNodePath { get; set; }
    [Export]
    public NodePath MaxSpawnNodePath { get; set; }
    private Node2D _minSpawnNode = null;
    private Node2D _maxSpawnNode = null;
    private Vector2 _minSpawnArea = Vector2.Zero;
    private Vector2 _maxSpawnArea = Vector2.Zero;

    public List<EnemyCharacter> EnemyList = new List<EnemyCharacter>();
    public List<LesserEnemyCharacter> LesserEnemyList = new List<LesserEnemyCharacter>();

    public Queue<EnemyCharacter> MergeQueue = new Queue<EnemyCharacter>();

    public const int EnemyBaseHealth = 100; // when at rank = 1
    public const int LesserEnemyBaseHealth = 100; // always at rank = 1

    public override void _EnterTree()
    {
        base._EnterTree();

        if (EnemyCharacterScene == null)
        {
            EnemyCharacterScene = GD.Load<PackedScene>(EnemyCharacterPath);
        }

        if (LesserEnemyCharacterScene == null)
        {
            LesserEnemyCharacterScene = GD.Load<PackedScene>(LesserEnemyCharacterPath);
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // Free objects, prevent memory leak
        EnemyList.Clear();
        EnemyList = null;
        LesserEnemyList.Clear();
        LesserEnemyList = null;
    }

    public override void _Ready()
    {
        base._Ready();

        _minSpawnNode = GetNode<Node2D>(MinSpawnNodePath);
        _maxSpawnNode = GetNode<Node2D>(MaxSpawnNodePath);

        if (_minSpawnNode == null || _maxSpawnNode == null)
        {
            GD.PrintErr("Error: EnemyManager has missing export properties.");
            return;
        }

        _minSpawnArea = _minSpawnNode.GlobalPosition;
        _maxSpawnArea = _maxSpawnNode.GlobalPosition;

        // TODO test only
/*         EnemyCharacter e1 = SpawnEnemy(this);
        e1.GlobalPosition = new Vector2(200, 500);
        e1.HealthComponent.MaxHealth = RankOfEnemy(e1) * EnemyBaseHealth;
        e1.HealthComponent.SetHealth(RankOfEnemy(e1) * EnemyBaseHealth);
        e1.TargetLocation = new Vector2(100, 100); */

        EnemyCharacter e2 = SpawnEnemy(this);
        e2.SetElementalCount(Globals.DecodeAllElement("15,0,0,0,0"));
        e2.GlobalPosition = new Vector2(1500, 500);
        e2.HealthComponent.MaxHealth = RankOfEnemy(e2) * EnemyBaseHealth;
        e2.HealthComponent.SetHealth(RankOfEnemy(e2) * EnemyBaseHealth);
        e2.TargetLocation = new Vector2(1600, 600);

        /* LesserEnemyCharacter f = SpawnLesserEnemy(this);
        f.SetElement(Globals.Element.Water);
        f.GlobalPosition = new Vector2(1600, 600);
        f.HealthComponent.MaxHealth = LesserEnemyBaseHealth;
        f.HealthComponent.SetHealth(LesserEnemyBaseHealth); */
        
    }

    // Encode all enemy on screen as a string
    public string EncodeAllAliveEnemy()
    {
        string encoding = "";

        foreach (EnemyCharacter enemy in EnemyList)
        {
            encoding += $"/{Globals.EncodeAllElement(enemy.ElementalCount)}";
        }

        if (encoding.StartsWith("/"))
        {
            encoding = encoding.Remove(0, 1);
        }

        return encoding;
    }

    // Decode string and spawn all enemy on screen
    public List<EnemyCharacter> DecodeAllSpawnEnemy(string encoding)
    {
        List<EnemyCharacter> enemySpawned = new List<EnemyCharacter>();

        // - - - Start parsing data - - -

        String[] parts = encoding.Split("/");

        if (parts.Length == 0)
        {
            GD.Print($"Warning: Failed to parse enemy data '{encoding}'.");
            return enemySpawned;
        }

        foreach (string part in parts)
        {
            if (part.Length > 0)
            {
                EnemyCharacter enemy = SpawnEnemy(this);
                enemySpawned.Add(enemy);
                enemy.SetElementalCount(Globals.DecodeAllElement(part));
            }
        }

        return enemySpawned;
    }

    // Disable all enemy and lesser enemy movement
    public void DisableAllEnemy(bool disable)
    {
        foreach (EnemyCharacter enemy in EnemyList)
        {
            enemy.SetProcess(!disable);
            enemy.SetPhysicsProcess(!disable);
        }

        foreach (LesserEnemyCharacter lesser in LesserEnemyList)
        {
            lesser.SetProcess(!disable);
            lesser.SetPhysicsProcess(!disable);
        }
    }

    public EnemyCharacter SpawnEnemy(Node2D parentNode)
    {
        EnemyCharacter instance = EnemyCharacterScene.Instance<EnemyCharacter>();

        parentNode.AddChild(instance); // Must be done first

        instance.Connect("Killed", this, nameof(_OnEnemyKilled));
        instance.Connect("SplitNeeded", this, nameof(_OnEnemySplitNeeded));
        instance.Connect("MergeNeeded", this, nameof(_OnEnemyMergeNeeded));

        instance.HealthComponent.MaxHealth = EnemyBaseHealth;
        instance.HealthComponent.SetHealth(EnemyBaseHealth);

        EnemyList.Add(instance);
        
        return instance;
    }

    public LesserEnemyCharacter SpawnLesserEnemy(Node2D parentNode)
    {
        LesserEnemyCharacter instance = LesserEnemyCharacterScene.Instance<LesserEnemyCharacter>();

        parentNode.AddChild(instance); // Must be done first

        instance.Connect("Killed", this, nameof(_OnLesserEnemyKilled));

        instance.HealthComponent.MaxHealth = LesserEnemyBaseHealth;
        instance.HealthComponent.SetHealth(LesserEnemyBaseHealth);

        LesserEnemyList.Add(instance);
        
        return instance;
    }

    public void _OnEnemyKilled(EnemyCharacter source)
    {
        // Prevent multiple calls from Area2D bug
        source.Disconnect("Killed", this, nameof(_OnEnemyKilled));

        EnemyList.Remove(source);
    }

    public void _OnLesserEnemyKilled(LesserEnemyCharacter source)
    {
        // Prevent multiple calls from Area2D bug
        source.Disconnect("Killed", this, nameof(_OnLesserEnemyKilled));

        LesserEnemyList.Remove(source);
    }

    public void _OnEnemySplitNeeded(EnemyCharacter source)
    {
        // Prevent multiple calls from Area2D bug
        source.Disconnect("SplitNeeded", this, nameof(_OnEnemySplitNeeded));

        CallDeferred("SplitEnemy", source);
    }

    public void _OnEnemyMergeNeeded(EnemyCharacter source)
    {
        // Prevent multiple calls from Area2D bug
        source.Disconnect("MergeNeeded", this, nameof(_OnEnemyMergeNeeded));

        MergeQueue.Enqueue(source);
    }

    public void SplitEnemy(EnemyCharacter mother)
    {
        Dictionary<Globals.Element, int> motherElements = new Dictionary<Globals.Element, int>(); // Temporary copy
        Dictionary<Globals.Element, int> daughterElements = new Dictionary<Globals.Element, int>();
        int daughterElementsSum = 0;
        LesserEnemyCharacter lesser;
        EnemyCharacter daughter;

        // Copy the mother elements (because cannot update dictionary values inside foreach loop)
        foreach (Globals.Element element in mother.ElementalCount.Keys)
        {
            motherElements[element] = mother.ElementalCount[element];
        }

        // Calculate mother/ daughter elements
        foreach (Globals.Element element in mother.ElementalCount.Keys)
        {
            // Spawn a lesser enemy if odd
            if (motherElements[element] % 2 == 1)
            {
                motherElements[element] -= 1;

                lesser = SpawnLesserEnemy(this);
                lesser.SetElement(element);
                // 72deg * n
                lesser.GlobalPosition = mother.GlobalPosition + 100 * Vector2.Up.Rotated(2 * Mathf.Pi * ((int)element - 1) / 5.0f);
                lesser.HealthComponent.MaxHealth = LesserEnemyBaseHealth;
                lesser.HealthComponent.SetHealth(LesserEnemyBaseHealth);
            }

            int half = motherElements[element] / 2; // Integer division, round towards zero
            
            daughterElements[element] = half;
            daughterElementsSum += half;

            motherElements[element] -= daughterElements[element];
        }

        // Set the new values
        mother.SetElementalCount(motherElements);

        // Check if mother downgrades to lesser enemy
        if (mother.SumElementalCount() <= 0)
        {
            mother.Kill();
            mother = null;
        }
        else if (mother.SumElementalCount() == 1)
        {
            lesser = SpawnLesserEnemy(this);
            lesser.SetElement(Globals.DominantElement(daughterElements));
            lesser.GlobalPosition = mother.GlobalPosition;
            lesser.HealthComponent.MaxHealth = LesserEnemyBaseHealth;
            lesser.HealthComponent.SetHealth(LesserEnemyBaseHealth);

            mother.Kill();
            mother = null;
        }
        else // > 1
        {
            mother.HealthComponent.MaxHealth = RankOfEnemy(mother) * EnemyBaseHealth;
            mother.HealthComponent.SetHealth(RankOfEnemy(mother) * EnemyBaseHealth);
            mother.Scale /= 2; // Half the size
        }

        // Spawn daughter
        if (daughterElementsSum <= 0)
        {
            // Do nothing
            daughter = null;
        }
        else if (daughterElementsSum == 1)
        {
            lesser = SpawnLesserEnemy(this);
            lesser.SetElement(Globals.DominantElement(daughterElements));
            // 72deg * n + 36deg
            lesser.GlobalPosition = mother.GlobalPosition + 100 * Vector2.Up.Rotated(2 * Mathf.Pi * ((int)lesser.GetElement() - 1 + 0.5f) / 5.0f);
            lesser.HealthComponent.MaxHealth = LesserEnemyBaseHealth;
            lesser.HealthComponent.SetHealth(LesserEnemyBaseHealth);

            daughter = null;
        }
        else // > 1
        {
            daughter = SpawnEnemy(this);
            daughter.SetElementalCount(daughterElements);
            daughter.HealthComponent.MaxHealth = RankOfEnemy(daughter) * EnemyBaseHealth;
            daughter.HealthComponent.SetHealth(RankOfEnemy(daughter) * EnemyBaseHealth);
            daughter.Scale /= 2; // Half the size
            daughter.GlobalPosition = mother.GlobalPosition;
            
            //daughter.TargetLocation = new Vector2(100, 100);
        }

        // Move the mother and daughter
        if (mother != null && daughter != null)
        {
            mother.TargetLocation = mother.GlobalPosition + Vector2.Up * 100; // TODO
            daughter.TargetLocation = mother.GlobalPosition + Vector2.Down * 100; // TODO
        }
    }

    public void MergeEnemy(EnemyCharacter larger, EnemyCharacter smaller)
    {
        foreach (Globals.Element key in smaller.ElementalCount.Keys)
        {
            larger.AddToElement(key, smaller.ElementalCount[key]);
        }

        smaller.Kill();
    }

    public static int RankOfEnemy(EnemyCharacter enemy)
    {
        int total = enemy.SumElementalCount();

        if (total < 1)
        {
            GD.Print($"Warning: EnemyCharacter has {total} elements, rank = 0.");
            return 0; // Because log2(0) == -inf
        }

        int rank = (int)Math.Floor((Math.Log(total) / Math.Log(2)) + 1); // 1->1, 2->2, 3->2, 4->3, etc.
        return rank;
    }

    public void RepositionEnemies()
    {
        throw new NotImplementedException();
        // TODO missing code to reposition all the enemies at even spacing
    }

}
