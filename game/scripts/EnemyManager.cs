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

    public List<EnemyCharacter> EnemyList = new List<EnemyCharacter>();
    public List<LesserEnemyCharacter> LesserEnemyList = new List<LesserEnemyCharacter>();

    public const int EnemyBaseHealth = 100; // when at rank = 1
    public const int LesserEnemyBaseHealth = 50; // always at rank = 1

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

        // TODO test only
        EnemyCharacter e = SpawnEnemy(this);
        e.Position = new Vector2(200, 500);
        e.HealthComponent.MaxHealth = RankOfEnemy(e) * EnemyBaseHealth;
        e.HealthComponent.SetHealth(RankOfEnemy(e) * EnemyBaseHealth);
        e.TargetLocation = new Vector2(100, 100);

        LesserEnemyCharacter f = SpawnLesserEnemy(this);
        f.Position = new Vector2(300, 600);
        f.HealthComponent.MaxHealth = RankOfLesserEnemy(f) * LesserEnemyBaseHealth;
        f.HealthComponent.SetHealth(RankOfLesserEnemy(f) * LesserEnemyBaseHealth);
    }

    public EnemyCharacter SpawnEnemy(Node2D parentNode)
    {
        EnemyCharacter instance = EnemyCharacterScene.Instance<EnemyCharacter>();

        parentNode.AddChild(instance); // Must be done first

        instance.Connect("Killed", this, nameof(_OnEnemyKilled));
        instance.Connect("SplitNeeded", this, nameof(_OnEnemySplitNeeded));
        instance.Connect("MergeNeeded", this, nameof(_OnEnemyMergeNeeded));

        instance.HealthComponent.SetHealth(EnemyBaseHealth * RankOfEnemy(instance));

        EnemyList.Add(instance);
        
        return instance;
    }

    public LesserEnemyCharacter SpawnLesserEnemy(Node2D parentNode)
    {
        LesserEnemyCharacter instance = LesserEnemyCharacterScene.Instance<LesserEnemyCharacter>();

        parentNode.AddChild(instance); // Must be done first

        instance.Connect("Killed", this, nameof(_OnLesserEnemyKilled));

        instance.HealthComponent.SetHealth(LesserEnemyBaseHealth);

        LesserEnemyList.Add(instance);
        
        return instance;
    }

    public void _OnEnemyKilled(EnemyCharacter source)
    {
        //GD.Print($"Enemy {source.GetInstanceId()} Killed."); // TODO

        // Prevent multiple calls from Area2D bug
        source.Disconnect("Killed", this, nameof(_OnEnemyKilled));
        EnemyList.Remove(source);
    }

    public void _OnLesserEnemyKilled(LesserEnemyCharacter source)
    {
        //GD.Print($"Lesser Enemy {source.GetInstanceId()} Killed."); // TODO

        // Prevent multiple calls from Area2D bug
        source.Disconnect("Killed", this, nameof(_OnLesserEnemyKilled));
        LesserEnemyList.Remove(source);
    }

    public void _OnEnemySplitNeeded(EnemyCharacter source)
    {
        //GD.Print($"Enemy {source.GetInstanceId()} Split Needed."); // TODO

        // Prevent multiple calls from Area2D bug
        source.Disconnect("SplitNeeded", this, nameof(_OnEnemySplitNeeded));

        // TODO unfinished
        //SplitEnemy(source, Globals.Element.None);
        CallDeferred("SplitEnemy", source, Globals.Element.None);
    }

    public void _OnEnemyMergeNeeded(EnemyCharacter source)
    {
        //GD.Print($"Enemy {source.GetInstanceId()} Merge Needed."); // TODO

        // Prevent multiple calls from Area2D bug
        source.Disconnect("MergeNeeded", this, nameof(_OnEnemyMergeNeeded));

        // TODO unfinished
    }

    public void SplitEnemy(EnemyCharacter mother, Globals.Element bulletElement)
    {
        // TODO requires new movement code

        // TODO test code
        RandomNumberGenerator rng = new RandomNumberGenerator();
        int offset;
        LesserEnemyCharacter lesser = null;

        rng.Randomize();
        offset = rng.RandiRange(-50, 50);

        // If countered, spawn an additional lesser enemy
        if (bulletElement != Globals.Element.None)
        {
            mother.SubtractFromElement(Globals.CounterByElement(bulletElement), 1);
            lesser = SpawnLesserEnemy(this);
            lesser.SetElement(bulletElement);
            lesser.Position = new Vector2(200 + offset, 500 + offset);
        }

        Dictionary<Globals.Element, int> halved = new Dictionary<Globals.Element, int>();

        // TODO has error
        foreach (Globals.Element key in mother.ElementalCount.Keys)
        {
            halved[key] = mother.ElementalCount[key] / 2; // Integer division
            //mother.SubtractFromElement(key, halved[key]); // TODO error
            
            if (mother.ElementalCount[key] % 2 == 1)
            {
                // Odd, spawn an extra lesser enemy

                //mother.SubtractFromElement(key, 1); // TODO error
                lesser = SpawnLesserEnemy(this);
                lesser.SetElement(key);

                // TODO test code
                rng.Randomize();
                offset = rng.RandiRange(-50, 50);

                lesser.Position = new Vector2(200 + offset, 500 + offset);
            }
        }

        rng.Randomize();
        offset = rng.RandiRange(-100, 100);

        EnemyCharacter daughter = SpawnEnemy(this);
        daughter.SetElementalCount(halved);
        daughter.Position = new Vector2(mother.Position.x, mother.Position.y + offset);
        daughter.HealthComponent.MaxHealth = mother.HealthComponent.MaxHealth / 2;
        daughter.HealthComponent.SetHealth(mother.HealthComponent.MaxHealth / 2);
        GD.Print($"{daughter.Position}");
        if (RankOfEnemy(daughter) <= 1)
        {
            daughter.Kill(); // TODO temp fix
        }
    }

    public void MergeEnemy(EnemyCharacter larger, EnemyCharacter smaller)
    {
        // TODO requires new movement code

        foreach (Globals.Element key in smaller.ElementalCount.Keys)
        {
            larger.AddToElement(key, smaller.ElementalCount[key]);
        }

        smaller.Kill();
    }

    public int RankOfEnemy(EnemyCharacter enemy)
    {
        int total = enemy.TotalElementalCount();

        if (total < 1)
        {
            GD.Print("Warning: EnemyCharacter has 0 elements and rank.");
            return 0; // Because log2(0) == -inf
        }

        int rank = (int)Math.Floor((Math.Log(total) / Math.Log(2)) + 1); // 1->1, 2->2, 3->2, 4->3, etc.
        return rank;
    }

    public int RankOfLesserEnemy(LesserEnemyCharacter lesser)
    {
        return 1; // TODO Hard coded for now; may switch to public constant
    }

    public void RepositionEnemies()
    {
        throw new NotImplementedException();
        // TODO missing code to reposition all the enemies at even spacing
    }


}
