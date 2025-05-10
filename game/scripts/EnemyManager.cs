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

    public bool WaveInProgress = false; // If the wave is still going on
    public int WaveNumber { get; set; } = 0; // Number of waves successfully completed
    public float TimeElapsed { get; set; } = 0.0f; // Time spent in the current wave
    private float _waveCooldownTimer { get; set; } = 0.0f; // Delay at the start and end of each wave
    public float WaveCooldown { get; set; } = 1.0f; // Delay at the start and end of each wave
    public string CurrentWaveEncoding { get; set; } = ""; // Current wave enemies in string encoding

    [Export]
    public int EnemyBaseHealth { get; set; } = 200; // when at rank = 1
    [Export]
    public int LesserEnemyBaseHealth { get; set; } = 50; // always at rank = 1

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
        e2.SetElementalCount(Globals.DecodeAllElement("15,0,0,0,0")); // 
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

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);

        if (WaveInProgress)
        {
            TimeElapsed += delta;
        }
    }

    // Free all enemy and lesser enemy
    public void Clear()
    {
        foreach (EnemyCharacter enemy in EnemyList)
        {
            enemy?.Kill();
        }

        foreach (LesserEnemyCharacter lesser in LesserEnemyList)
        {
            lesser?.Kill();
        }

        MergeQueue.Clear();
    }

    public void LoadWave()
    {
        GD.Print("Wave loading.");
    }

    public void StartWave()
    {
        GD.Print("Wave started.");
    }

    public void StopWave()
    {
        GD.Print("Wave ended.");
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
    public List<EnemyCharacter> DecodeAllSpawnEnemy(string encoding, Node parentNode)
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

        // - - - Initialize each enemy - - -
        foreach (EnemyCharacter enemy in enemySpawned)
        {
            enemy.HealthComponent.MaxHealth = RankOfEnemy(enemy) * EnemyBaseHealth;
            enemy.HealthComponent.SetHealth(RankOfEnemy(enemy) * EnemyBaseHealth);
            //enemy.GlobalPosition = enemy.GlobalPosition; // TODO
            //enemy.Scale = ; // Reduce the size // TODO
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

        EnemyList.Add(instance);
        
        return instance;
    }

    public LesserEnemyCharacter SpawnLesserEnemy(Node2D parentNode)
    {
        LesserEnemyCharacter instance = LesserEnemyCharacterScene.Instance<LesserEnemyCharacter>();

        parentNode.AddChild(instance); // Must be done first

        instance.Connect("Killed", this, nameof(_OnLesserEnemyKilled));

        LesserEnemyList.Add(instance);
        
        return instance;
    }

    public void _OnEnemyKilled(EnemyCharacter source)
    {
        if (source == null)
        {
            GD.PrintErr("Error: Enemy killed is null.");
            return;
        }

        // Prevent multiple calls from Area2D bug
        source.Disconnect("Killed", this, nameof(_OnEnemyKilled));

        EnemyList.Remove(source);
    }

    public void _OnLesserEnemyKilled(LesserEnemyCharacter source)
    {
        if (source == null)
        {
            GD.PrintErr("Error: Lesser enemy killed is null.");
            return;
        }

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
        if (mother == null)
        {
            GD.PrintErr("Error: Failed to split null enemy.");
            return;
        }

        // Reconnect the signal
        mother.Connect("SplitNeeded", this, nameof(_OnEnemySplitNeeded));

        bool motherAsEnemy; // If mother is still Enemy after split
        bool daughterAsEnemy; // If daughter is still Enemy after split
        LesserEnemyCharacter lesser;
        EnemyCharacter daughter;

        // Copy the mother elements
        // (because dictionary values cannot be modified inside foreach loop)
        Dictionary<Globals.Element, int> motherElements = Globals.CopyElements(mother.ElementalCount);
        Dictionary<Globals.Element, int> daughterElements = Globals.CopyElements();

        // Calculate mother/ daughter elements
        foreach (Globals.Element element in Globals.AllElements)
        {
            if (!mother.ElementalCount.ContainsKey(element))
            {
                // Assume it is 0
                continue;
            }

            if (motherElements[element] % 2 == 1)
            {
                // Odd number, spawn a lesser enemy
                lesser = SpawnLesserEnemy(this);
                lesser.SetElement(element);
                lesser.HealthComponent.MaxHealth = LesserEnemyBaseHealth;
                lesser.HealthComponent.SetHealth(LesserEnemyBaseHealth);
                // angle = (360 / 5) * n, radius = 100
                lesser.GlobalPosition = mother.GlobalPosition + 100 * Vector2.Up.Rotated(2 * Mathf.Pi * ((int)element - 1) / 5.0f);

                motherElements[element] -= 1;
            }

            daughterElements[element] = motherElements[element] / 2; // Integer division, round towards zero
            motherElements[element] -= daughterElements[element];
        }

        // Update mother
        Vector2 newScale = mother.Scale * 0.8f; // Reduce the size
        mother.SetElementalCount(motherElements);

        if (mother.SumElementalCount() <= 0)
        {
            // Mother despawn (this should never happen here?)
            mother.Kill();
            motherAsEnemy = false;
        }
        else if (mother.SumElementalCount() == 1)
        {
            // Mother downgrades to lesser enemy
            lesser = SpawnLesserEnemy(this);
            lesser.SetElement(Globals.DominantElement(daughterElements));
            lesser.HealthComponent.MaxHealth = LesserEnemyBaseHealth;
            lesser.HealthComponent.SetHealth(LesserEnemyBaseHealth);
            lesser.GlobalPosition = mother.GlobalPosition;

            mother.Kill();
            motherAsEnemy = false;
        }
        else // > 1
        {
            mother.HealthComponent.MaxHealth = RankOfEnemy(mother) * EnemyBaseHealth;
            mother.HealthComponent.SetHealth(RankOfEnemy(mother) * EnemyBaseHealth);
            mother.Scale = newScale; // Reduce the size

            motherAsEnemy = true;
        }

        // Spawn daughter
        if (Globals.SumElements(daughterElements) <= 0)
        {
            // Do nothing
            daughter = null;
            daughterAsEnemy = false;
        }
        else if (Globals.SumElements(daughterElements) == 1)
        {
            // Daughter downgrades to lesser enemy
            Globals.Element newElement = Globals.DominantElement(daughterElements);

            lesser = SpawnLesserEnemy(this);
            lesser.SetElement(newElement);
            lesser.HealthComponent.MaxHealth = LesserEnemyBaseHealth;
            lesser.HealthComponent.SetHealth(LesserEnemyBaseHealth);
            // angle = (360 / 5) * n, radius = 100
            lesser.GlobalPosition = mother.GlobalPosition + 150 * Vector2.Up.Rotated(2 * Mathf.Pi * ((int)newElement - 1) / 5.0f);

            daughter = null;
            daughterAsEnemy = false;
        }
        else // > 1
        {
            daughter = SpawnEnemy(this);
            daughter.SetElementalCount(daughterElements);
            daughter.HealthComponent.MaxHealth = RankOfEnemy(daughter) * EnemyBaseHealth;
            daughter.HealthComponent.SetHealth(RankOfEnemy(daughter) * EnemyBaseHealth);
            daughter.GlobalPosition = mother.GlobalPosition;
            daughter.Scale = newScale; // Reduce the size
            
            daughterAsEnemy = true;
        }

        // Move the mother and daughter apart
        if (motherAsEnemy && daughterAsEnemy)
        {
            mother.TargetLocation = mother.GlobalPosition + Vector2.Up * 100;
            daughter.TargetLocation = mother.GlobalPosition + Vector2.Down * 100;
        }
        AudioManager.PlaySFX("res://assets/sfx/rpg_essentials_free/10_Battle_SFX/22_Slash_04.wav");
    }

    public void MergeEnemy(EnemyCharacter enemyA, EnemyCharacter enemyB)
    {
        if (enemyA == null || enemyB == null)
        {
            GD.PrintErr("Error: Failed to merge null enemy.");
            return;
        }

        // Reconnect the signal
        enemyA.Connect("MergeNeeded", this, nameof(_OnEnemyMergeNeeded));
        enemyB.Connect("MergeNeeded", this, nameof(_OnEnemyMergeNeeded));

        EnemyCharacter larger;
        EnemyCharacter smaller;

        if (enemyA.SumElementalCount() >= enemyB.SumElementalCount())
        {
            larger = enemyA;
            smaller = enemyB;
        }
        else
        {
            smaller = enemyA;
            larger = enemyB;
        }

        foreach (Globals.Element element in Globals.AllElements)
        {
            if (smaller.ElementalCount.ContainsKey(element))
            {
                larger.AddToElement(element, smaller.ElementalCount[element]);
            }
        }
        AudioManager.PlaySFX("res://assets/sfx/rpg_essentials_free/10_Battle_SFX/77_flesh_02.wav");
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
