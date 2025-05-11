using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// This class manages the spawning, splitting, and merging of enemy and lesser enemy.
// Note: NOT a singleton
public class EnemyManager : Node2D
{
    [Signal]
    public delegate void WaveComplete();

    public const string EnemyCharacterPath = "res://scenes/enemy_character.tscn";
    public const string LesserEnemyCharacterPath = "res://scenes/lesser_enemy_character.tscn";
    public static PackedScene EnemyCharacterScene = null;
    public static PackedScene LesserEnemyCharacterScene = null;

    public bool WaveInProgress = false; // If the wave is still going on // TODO
    public int WavesCompleted { get; set; } = 0; // Number of waves successfully completed
    public float WaveTimer { get; set; } = 0.0f; // Time spent in the current wave
    public float WaveBuffer { get; set; } = 0.1f; // Delay at the start and end of each wave
    public string CurrentWaveEncoding { get; set; } = ""; // Current wave enemies in string encoding
    public float UpdateDelay { get; set; } = 1; // How many seconds to wait before game updates
    public float UpdateTimer { get; set; } = 0; // How many seconds has passed since the last game update

    [Export]
    public int LesserEnemyBaseHealth { get; set; } = 50; // always at rank = 1

    // The zone the enemy/ lesser enemy can spawn in
    [Export]
    public NodePath MinSpawnNodePath { get; set; }

    [Export]
    public NodePath MaxSpawnNodePath { get; set; }

    [Export]
    public int AutoMergeLimit { get; set; } = 3; // Maximum enemies before auto-merge is triggered

    private Node2D _minSpawnNode = null;
    private Node2D _maxSpawnNode = null;
    private Vector2 _minSpawnArea = Vector2.Zero;
    private Vector2 _maxSpawnArea = Vector2.Zero;
    private Vector2 _centerSpawnArea = Vector2.Zero;
    private float _widthSpawnArea = 0;
    private float _heightSpawnArea = 0;
    private Vector2 _hideVector = Vector2.Zero;
    private Vector2 _spawnLocation = Vector2.Zero;

    public List<EnemyCharacter> EnemyList = new List<EnemyCharacter>();
    public List<LesserEnemyCharacter> LesserEnemyList = new List<LesserEnemyCharacter>();
    public List<EnemyCharacter> MergeList = new List<EnemyCharacter>(); // Enemy waiting for another Enemy to merge with

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
        Clear();
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
        _centerSpawnArea = (_minSpawnArea + _maxSpawnArea) / 2;
        _widthSpawnArea = _maxSpawnArea.x - _minSpawnArea.x;
        _heightSpawnArea = _maxSpawnArea.y - _minSpawnArea.y;
        _hideVector = Vector2.Right * _widthSpawnArea * 0.5f;
        _spawnLocation = _centerSpawnArea + _hideVector;

        // TODO test only
        for (int i = 0; i < 20; i++)
        {
            GD.Print($"Wave {i} : {GenerateWaveEncoding(i)}");
        }
        //LoadWave($"{(1<<4)-1},0,0,0,0/0,{(1<<4)-1},0,0,0/0,0,{(1<<4)-1},0,0/0,0,0,{(1<<4)-1},0/0,0,0,0,{(1<<4)-1}");
        LoadWave(GenerateWaveEncoding(3));
        StartWave();
        //CancelWave();
    }

    // - - - Wave Loop - - -

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);

        //GD.Print($"{EnemyList.Count} {LesserEnemyList.Count}");

        if (WaveInProgress)
        {
            WaveTimer += delta;
        }

        if (WaveTimer <= WaveBuffer)
        {
            // Do nothing, to prevent any Godot _Ready() sync issues
            return; // Early exit
        }

        if (EnemyList.Count == 0 && LesserEnemyList.Count == 0 && WaveInProgress)
        {
            // - - - Wave actually completes - - -
            WaveInProgress = false;
            WavesCompleted += 1;
            EmitSignal("WaveComplete");

            ProjectileManager.ClearBullets();
            GD.Print($"Wave {WavesCompleted} completed!");

            return; // Early exit

            // - - - Wave actually completes - - -
        }

        UpdateTimer += delta;
        if (UpdateTimer > UpdateDelay)
        {
            UpdateTimer -= UpdateDelay;

            // - - - Update game states - - -
            
            // Auto-Merge All Enemies
            AutoMergeEnemies();

            // Reposition All Enemies
            RepositionEnemies();

            // - - - Update game states - - -
        }
    }

    // - - - Wave Loop - - -

    // Free all enemy and lesser enemy
    public void Clear()
    {
        KillAllEnemy();
        EnemyList = null;
        LesserEnemyList = null;
        MergeList = null;

        // TODO free the other export properties here
    }

    // Cancel the current wave and removes all enemies
    public void CancelWave()
    {
        WaveInProgress = false;
        WaveTimer = 0;
        UpdateTimer = 0;
        KillAllEnemy();
    }

    // Remember to CancelWave() first 
    public void LoadWave(string encoding)
    {
        CurrentWaveEncoding = encoding;
        DecodeAllSpawnEnemy(encoding, this);
        DisableAllEnemy(true);
        RepositionEnemies(true); // Preview mode
    }

    // Used after LoadWave()
    public void StartWave()
    {
        WaveInProgress = true;
        WaveTimer = 0;
        UpdateTimer = 0;
        DisableAllEnemy(false);
    }

    public void KillAllEnemy()
    {
        while (EnemyList.Count > 0)
        {
            if (EnemyList[0] == null)
            {
                GD.PrintErr("Error: Cannot kill null enemy in list.");
                break;
            }

            EnemyList[0].Kill(); // Calls Remove()
        }   
        EnemyList.Clear();

        while (LesserEnemyList.Count > 0)
        {
            if (LesserEnemyList[0] == null)
            {
                GD.PrintErr("Error: Cannot kill null lesser enemy in list.");
                break;
            }

            LesserEnemyList[0].Kill(); // Calls Remove()
        }   
        LesserEnemyList.Clear();

        MergeList.Clear();
    }

    // Deterministic, infinitely procedural
    // Returns a wave encoding for loading
    public string GenerateWaveEncoding(int wavesCompleted)
    {
        string encoding = "";

        Dictionary<Globals.Element, int> elementCounts;
        Globals.Element nextBigElement = (Globals.Element)(1 + wavesCompleted % 5);
        Globals.Element nextSmallElement;
        
        int difficulty = Math.Max(1, wavesCompleted + 2);
        int maxEnemies = AutoMergeLimit * 2;
        int scale = (int)(Math.Log(wavesCompleted) / Math.Log(2)) + 1; // = floor(log2(x-1)) + 1
        int numEnemies = Math.Max(1, scale);
        int numElements;
        int maxElements;
        if (numEnemies < 3)
        {
            maxElements = (1 << difficulty) - 1; // 2^n-1 for odd split at every split
        }
        else
        {
            maxElements = wavesCompleted * 2;
        }

        // Generate enemies from smallest to largest
        for (int i = 0; i < numEnemies; i++)
        {
            elementCounts = Globals.CopyElements(null);
            nextSmallElement = nextBigElement;
            numElements = maxElements / (i+1);
            //GD.Print($"i={i} bigE={nextBigElement} maxE={maxElements} numE={numElements}");

            // Decide how many elements to add
            for (int j = 0; j < numEnemies; j++)
            {
                elementCounts[nextSmallElement] += numElements;
                //GD.Print($"j={j} smaE={nextSmallElement} numE={numElements}");

                numElements = numElements / 2;
                nextSmallElement = Globals.NextElement(nextSmallElement);
            }

            if (i > 0)
            {
                encoding += "/";
            }
            encoding += Globals.EncodeAllElement(elementCounts);

            nextBigElement = Globals.CounterToElement(nextBigElement); // For more merge variaty
        }

        return encoding;
    }

    // Use CallDeferred()
    // Disable/ Enable all enemy and lesser enemy movement
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
            enemy.HealthComponent.MaxHealth = MaxHealthOfEnemy(enemy.ElementalCount);
            enemy.HealthComponent.SetHealth(MaxHealthOfEnemy(enemy.ElementalCount));
            enemy.GlobalPosition = _spawnLocation;
        }

        return enemySpawned;
    }

    public EnemyCharacter SpawnEnemy(Node2D parentNode)
    {
        EnemyCharacter instance = EnemyCharacterScene.Instance<EnemyCharacter>();

        parentNode.AddChild(instance); // Must be done first

        instance.Connect("Killed", this, nameof(_OnEnemyKilled));
        instance.Connect("SplitNeeded", this, nameof(_OnEnemySplitNeeded));
        instance.Connect("MergeNeeded", this, nameof(_OnEnemyMergeNeeded));
        instance.Connect("ReachedTarget", this, nameof(_OnEnemyReachedTarget));

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

    // Use CallDeferred()
    public void SplitEnemy(EnemyCharacter mother)
    {
        if (mother == null)
        {
            GD.Print("Warning: Failed to split null enemy.");
            return;
        }

        // Reconnect the signal
        if (!mother.IsConnected("SplitNeeded", this, nameof(_OnEnemySplitNeeded)))
        {
            mother.Connect("SplitNeeded", this, nameof(_OnEnemySplitNeeded));
        }

        bool motherAsEnemy; // If mother is still Enemy after split
        bool daughterAsEnemy; // If daughter is still Enemy after split
        int halfHealth = mother.HealthComponent.CurrentHealth / 2;
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

        if (Globals.SumElements(motherElements) <= 0)
        {
            // Mother despawn (this should never happen here?)
            mother.Kill();
            motherAsEnemy = false;
        }
        else if (Globals.SumElements(motherElements) == 1)
        {
            // Mother downgrades to lesser enemy
            lesser = SpawnLesserEnemy(this);
            lesser.SetElement(Globals.DominantElement(motherElements));
            lesser.HealthComponent.MaxHealth = LesserEnemyBaseHealth;
            lesser.HealthComponent.SetHealth(LesserEnemyBaseHealth);
            lesser.GlobalPosition = mother.GlobalPosition;

            mother.Kill();
            motherAsEnemy = false;
        }
        else // > 1
        {
            mother.SetElementalCount(motherElements);
            mother.HealthComponent.MaxHealth = Math.Min(halfHealth, MaxHealthOfEnemy(motherElements));
            mother.HealthComponent.SetHealth(Math.Min(halfHealth, MaxHealthOfEnemy(motherElements)));
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
            // angle = (360 / 5) * 1.5n, radius = 100
            lesser.GlobalPosition = mother.GlobalPosition + 100 * Vector2.Up.Rotated(2 * Mathf.Pi * ((int)newElement - 1 + 0.5f) / 5.0f);

            daughter = null;
            daughterAsEnemy = false;
        }
        else // > 1
        {
            daughter = SpawnEnemy(this);
            daughter.SetElementalCount(daughterElements);
            daughter.HealthComponent.MaxHealth = Math.Min(halfHealth, MaxHealthOfEnemy(daughterElements));
            daughter.HealthComponent.SetHealth(Math.Min(halfHealth, MaxHealthOfEnemy(daughterElements)));
            daughter.GlobalPosition = mother.GlobalPosition;
            daughter.Scale = newScale; // Reduce the sprite size
            
            daughterAsEnemy = true;
        }

        // Move the mother and daughter apart
        if (motherAsEnemy && daughterAsEnemy)
        {
            mother.TargetLocation = mother.GlobalPosition + Vector2.Up * 200;
            daughter.TargetLocation = mother.GlobalPosition + Vector2.Down * 200;
        }

        AudioManager.PlaySFX("res://assets/sfx/rpg_essentials_free/10_Battle_SFX/22_Slash_04.wav");
    }

    public void InitMergeEnemy(EnemyCharacter source)
    {
        if (source == null || !IsInstanceValid(source))
        {
            GD.Print("Warning: Cannot init merge null enemy.");
            return;
        }

        // Add to merge waiting list
        if (MergeList.Contains(source))
        {
            return; // Ignore repeated calls
        }
        MergeList.Add(source);

        if (MergeList.Count >= 2 && MergeList.Count % 2 == 0)
        {
            // Even number of enemies waiting to merge
            EnemyCharacter partner = MergeList[MergeList.Count - 2]; // The enemy before source

            if (partner == null || !IsInstanceValid(partner))
            {
                GD.Print("Warning: Cannot init merge null partner.");
                return;
            }

            Vector2 midpoint = (partner.GlobalPosition + source.GlobalPosition) / 2;

            partner.TargetLocation = midpoint;
            source.TargetLocation = midpoint;
        }
    }

    // Use CallDeferred()
    public void MergeEnemy(EnemyCharacter source)
    {
        if (source == null)
        {
            GD.PrintErr("Error: Failed to merge null enemy.");
            return;
        }

        if (!MergeList.Contains(source))
        {
            GD.Print("Warning: Enemy no longer in the merge list.");
            return;
        }
        MergeList.Remove(source);

        // Find partner (could be killed already)
        EnemyCharacter partner = null;

        for (int i = 0; i < MergeList.Count; i++)
        {
            if (MergeList[i] == null)
            {
                GD.PrintErr($"Error: MergeList[{i}] is null.");
                return;
            }

            if (MergeList[i].TargetLocation == source.TargetLocation)
            {
                partner = MergeList[i];
                break; // Early exit
            }

            //index += 1; // Dear god... I forgot this
        }

        if (partner == null)
        {
            GD.Print("Warning: Merge partner not found or no longer exists.");
            return;
        }
        MergeList.Remove(partner);
        
        // Decide the larger/ smaller enemy
        EnemyCharacter larger;
        EnemyCharacter smaller;
        if (source.SumElementalCount() >= partner.SumElementalCount())
        {
            larger = source;
            smaller = partner;
        }
        else
        {
            larger = partner;
            smaller = source;
        }

        // Extract element from smaller one
        foreach (Globals.Element element in Globals.AllElements)
        {
            if (smaller.ElementalCount.ContainsKey(element))
            {
                larger.AddToElement(element, smaller.ElementalCount[element]);
            }
        }

        // Update health
        int combinedHealth = larger.HealthComponent.CurrentHealth + smaller.HealthComponent.CurrentHealth;
        larger.HealthComponent.MaxHealth = combinedHealth;
        larger.HealthComponent.SetHealth(combinedHealth);

        // Restore the sprite size (1 / 0.8f)
        Vector2 newScale = larger.Scale * 1.25f;
        larger.Scale = newScale;

        // Remove the smaller one
        smaller.Kill();

        // Reconnect the signal
        if (!larger.IsConnected("MergeNeeded", this, nameof(_OnEnemyMergeNeeded)))
        {
            larger.Connect("MergeNeeded", this, nameof(_OnEnemyMergeNeeded));
        }

        AudioManager.PlaySFX("res://assets/sfx/rpg_essentials_free/10_Battle_SFX/77_flesh_02.wav");
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

    // Calculate enemy health
    public int MaxHealthOfEnemy(Dictionary<Globals.Element, int> elementCounts)
    {
        return Globals.SumElements(elementCounts) * LesserEnemyBaseHealth;
    }

    public void RepositionEnemies(bool previewMode=false)
    {
        // Sorted list in ascending y position (top to bottom)
        // Expensive
        List<EnemyCharacter> tempList = new List<EnemyCharacter>();
        EnemyCharacter tempEnemy;

        foreach (EnemyCharacter enemy in EnemyList)
        {
            if (enemy.IsTargeting)
            {
                // Ignore enemy with a target already
                break;
            }

            tempList.Add(enemy);
            
            // Insertion sort
            for (int i = tempList.Count - 1; i > 0; i--)
            {
                if (tempList[i-1].GlobalPosition.y > tempList[i].GlobalPosition.y)
                {
                    // Swap
                    tempEnemy = tempList[i-1];
                    tempList[i-1] = tempList[i];
                    tempList[i] = tempEnemy;
                }
            }
        }

        // Move all the enemy
        Vector2 xOffset = Vector2.Right * (_widthSpawnArea / 3); // 2 columns
        Vector2 yOffset = Vector2.Down * (_heightSpawnArea / (tempList.Count() + 1)); // N rows
        Vector2 target;
        for (int i = 0; i < tempList.Count(); i++)
        {
            if (i % 2 == 0)
            {
                // 1st column for odd
                 target = _minSpawnArea + xOffset + (i + 1) * yOffset;
            }
            else
            {
                // 2nd column for even
                target = _minSpawnArea + 2 * xOffset + (i + 1) * yOffset;
            }

            tempList[i].TargetLocation = target;

            if (previewMode)
            {
                tempList[i].GlobalPosition = target;
            }
        }
    }

    // Check if there is too many enemies on screen, and merge the excessive big ones together.
    // Hopefully this will save the game performance
    public void AutoMergeEnemies()
    {
        // Check if too many enemies
        if (EnemyList.Count <= AutoMergeLimit)
        {
            // Do nothing
            return;
        }

        // Sorted list in ascending element counts (smallest to largest)
        // Expensive
        List<EnemyCharacter> tempList = new List<EnemyCharacter>();
        EnemyCharacter tempEnemy;

        foreach (EnemyCharacter enemy in EnemyList)
        {
            tempList.Add(enemy);
            
            // Insertion sort
            for (int i = tempList.Count - 1; i > 0; i--)
            {
                if (Globals.SumElements(tempList[i-1].ElementalCount) < Globals.SumElements(tempList[i].ElementalCount))
                {
                    // Swap
                    tempEnemy = tempList[i-1];
                    tempList[i-1] = tempList[i];
                    tempList[i] = tempEnemy;
                }
            }
        }

        // Merge enemies
        for (int i = tempList.Count - 1; i >= AutoMergeLimit; i--)
        {
            CallDeferred("InitMergeEnemy", tempList[i]);
        }

        if (AutoMergeLimit > 1 && AutoMergeLimit % 2 == 1)
        {
            // If odd, find a merge partner
            CallDeferred("InitMergeEnemy", tempList[AutoMergeLimit - 1]);
        }
    }

    // - - - Signal Receivers - - -

    public void _OnEnemyKilled(EnemyCharacter source)
    {
        if (source == null)
        {
            GD.PrintErr("Error: Enemy killed is null.");
            return;
        }

        // Prevent multiple calls from Area2D bug
        if (source.IsConnected("Killed", this, nameof(_OnEnemyKilled)))
        {
            source.Disconnect("Killed", this, nameof(_OnEnemyKilled));
        }

        EnemyList.Remove(source);

        MergeList.Remove(source); // Beware of this
    }

    public void _OnLesserEnemyKilled(LesserEnemyCharacter source)
    {
        if (source == null)
        {
            GD.PrintErr("Error: Lesser enemy killed is null.");
            return;
        }

        // Prevent multiple calls from Area2D bug
        if (source.IsConnected("Killed", this, nameof(_OnLesserEnemyKilled)))
        {
            source.Disconnect("Killed", this, nameof(_OnLesserEnemyKilled));
        }

        LesserEnemyList.Remove(source);
    }

    public void _OnEnemySplitNeeded(EnemyCharacter source)
    {
        if (source == null)
        {
            GD.PrintErr("Error: Enemy to be split is null.");
            return;
        }

        // Prevent multiple calls from Area2D bug
        if (source.IsConnected("SplitNeeded", this, nameof(_OnEnemySplitNeeded)))
        {
            source.Disconnect("SplitNeeded", this, nameof(_OnEnemySplitNeeded));
        }

        CallDeferred("SplitEnemy", source);
    }

    public void _OnEnemyMergeNeeded(EnemyCharacter source)
    {
        if (source == null)
        {
            GD.PrintErr("Error: Enemy to be merged is null.");
            return;
        }

        // Prevent multiple calls from Area2D bug
        if (source.IsConnected("MergeNeeded", this, nameof(_OnEnemyMergeNeeded)))
        {
            source.Disconnect("MergeNeeded", this, nameof(_OnEnemyMergeNeeded));
        }

        CallDeferred("InitMergeEnemy", source);
    }

    public void _OnEnemyReachedTarget(EnemyCharacter source)
    {
        if (source == null)
        {
            GD.PrintErr("Error: Enemy reached target but is null.");
            return;
        }

        if (MergeList.Contains(source))
        {
            CallDeferred("MergeEnemy", source);
        }
    }
}
