using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// This singleton class spawns projectiles.
// Note: Godot autoload requires Node type.
public class ProjectileManager : Node
{
    public static ProjectileManager Singleton { get; private set; }

    private static Dictionary<string, PackedScene> _cachedScenes = new Dictionary<string, PackedScene>();

    private static Dictionary<string, Stack<Node>> _objectPools = new Dictionary<string, Stack<Node>>();

    private static HashSet<ulong> _despawnPool = new HashSet<ulong>();

    public static readonly Dictionary<Globals.Element, string> BulletScenePath = new Dictionary<Globals.Element, string>()
    {
        {Globals.Element.None, "res://scenes/projectiles/none_bullet.tscn"},
        {Globals.Element.Water, "res://scenes/projectiles/water_bullet.tscn"},
        {Globals.Element.Wood, "res://scenes/projectiles/wood_bullet.tscn"},
        {Globals.Element.Fire, "res://scenes/projectiles/fire_bullet.tscn"},
        {Globals.Element.Earth, "res://scenes/projectiles/earth_bullet.tscn"},
        {Globals.Element.Metal, "res://scenes/projectiles/metal_bullet.tscn"}
    };

    // Return a non-moving, non-parented projectile as template.
    public static Node LoadTemplate(string scenePath)
    {
        // Check if that scene is cached
        if (!_cachedScenes.ContainsKey(scenePath))
        {
            _cachedScenes[scenePath] = GD.Load<PackedScene>(scenePath);
        }

        // Make a new projectile
        Node projectile = _cachedScenes[scenePath].Instance();

        // Disable processing
        projectile.SetProcess(false);
        projectile.SetPhysicsProcess(false); // It reset to true in AddChild(projectile)

        return projectile;
    }

    // Get a projectile scene instance. The caller needs to set up the projectile.
    // Set parentNode to GetTree().Root in most cases
    // Returns the root node of a projectile scene
    // Note: Remember to reset the collision layer and collision mask
    // Warning: do not call projectile.GetParent() in the same frame
    public static Node SpawnProjectile(string scenePath, Node parentNode, int collisionFlag)
    {
        Node projectile;

        // Check if object pool exist
        if (!_objectPools.ContainsKey(scenePath))
        {
            _objectPools[scenePath] = new Stack<Node>();
        }

        // Check if object pool is empty
        if (_objectPools[scenePath].Count > 0)
        {
            projectile = _objectPools[scenePath].Pop();
        }
        else
        {
            // Check if that scene is cached
            if (!_cachedScenes.ContainsKey(scenePath))
            {
                _cachedScenes[scenePath] = GD.Load<PackedScene>(scenePath);
            }

            // Make a new projectile
            projectile = _cachedScenes[scenePath].Instance();
        }

        // Attatch to parent at the end of frame (if exist)
        projectile.GetParent()?.RemoveChild(projectile);
        //parentNode?.CallDeferred("add_child", projectile);
        parentNode.AddChild(projectile);

        // Caller should set the node owner when saving

        // Enable processing
        projectile.SetProcess(true);
        projectile.SetPhysicsProcess(true); // It reset to true in AddChild(projectile)

        // Initalize the projectile immediately, instead of waiting for _Ready()
        if (projectile is Bullet bulletRef)
        {
            bulletRef.Initalize(); // TODO need clean up
            bulletRef.CollisionFlag = collisionFlag;
            //if(parentNode is IProjectileInfo info)
            //{
            //    bulletRef.CollisionFlag = info.FriendlyCollisionFlag;
            //}
        }



        return projectile;
    }

    // Overload to use template
    public static Node SpawnProjectile(Node template, Node parentNode, int collisionFlag)
    {
        Node projectile = SpawnProjectile(template.Filename, parentNode, collisionFlag);
        return projectile;
    }

    // Despawn at the end of frame
    public static void QueueDespawnProjectile(Node projectile)
    {
        // TODO temporary fix, can't find any solution to fix the Area2D bug
        projectile.QueueFree();
        return;



        ulong instanceID = projectile.GetInstanceId();

        // Check if repeated calls
        if (!_despawnPool.Contains(instanceID))
        {
            _despawnPool.Add(instanceID);
            GD.Print($"{projectile.GetInstanceId()} Locked");
            Singleton.CallDeferred("DespawnProjectile", projectile, instanceID);
        }
        else
        {
            GD.Print($"{projectile.GetInstanceId()} Blocked");
        }

        if (projectile is Bullet bullet)
        {
            bullet.CollisionLayer = (uint)0;
            bullet.CollisionMask = (uint)0;
            bullet.Position = Vector2.Zero;
            bullet.SetProcess(false);
            bullet.SetPhysicsProcess(false);
            GD.Print($"{bullet.GetInstanceId()} Reset");
            GD.Print($"{bullet.Position}");
        } // TODO
    }

    // Recycle a projectile scene instance.
    // projectile:  Root node of a projectile scene
    private static void DespawnProjectile(Node projectile, ulong instanceID)
    {
        GD.Print($"{projectile.GetInstanceId()} Despawned");
        if (projectile is Bullet e)
        {
            GD.Print($"{e.Position}");
        }
        
        if (projectile == null)
        {
            GD.PrintErr("Error: Cannot despawn a null projectile.");
            return;
        }

        if (projectile.Filename.Length == 0)
        {
            GD.PrintErr("Warning: ProjectileManager despawned a node that is not a scene instance.");
            return;
        }

        // Disable processing
        projectile.SetProcess(false);
        projectile.SetPhysicsProcess(false); // It reset to true in AddChild(projectile)

        // Detatch from parent (if exist)
        projectile.GetParent().RemoveChild(projectile);

        // Prevent scenes from saving this node
        projectile.Owner = null;

        // Add to object pool
        _objectPools[projectile.Filename].Push(projectile);
        _despawnPool.Remove(instanceID);
        GD.Print($"{projectile.GetInstanceId()} Unlocked");
    }

    // Called when the node enters the scene tree for the first time
    public override void _Ready()
    {
        Singleton = this;
    }

    // - - - Bullet Emitter Functions - - -

    // Emit a bullet in a line shape
    public static void EmitBulletLine(Bullet template, Node parentNode, int collisionFlag, Vector2 position)
    {
        Bullet bullet = (Bullet)SpawnProjectile(template, parentNode, collisionFlag);
        Bullet.CopyData(template, bullet);
        bullet.Position = position + Vector2.Right * 100;
        // bullet.MovementNode.Direction = template.MovementNode.Direction;
        GD.Print($"{bullet.GetInstanceId()} Emit");
        GD.Print($"{bullet.Position}");
    }

    // Emit multiple bullet in a wall shape
    public static void EmitBulletWall(Bullet template, Node parentNode, int collisionFlag, Vector2 position, int count, float separation)
    {
        if (count < 1)
        {
            GD.PrintErr("Error: EmitBulletRing() must have count >= 1.");
            return;
        }

        float width = (count - 1) * separation;
        float half = width / 2;
        Vector2 cross = template.MovementNode.Direction.Rotated(Mathf.Pi / 2); // 2D cross product

        for (int i = 0; i < count; i++)
        {
            Bullet bullet = (Bullet)SpawnProjectile(template, parentNode, collisionFlag);
            Bullet.CopyData(template, bullet);
            bullet.Position = position + (i * separation - half) * cross;
            // bullet.MovementNode.Direction = template.MovementNode.Direction;
        }

    }

    // Emit multiple bullets in a ring shape
    public static void EmitBulletRing(Bullet template, Node parentNode, int collisionFlag, Vector2 position, int count)
    {
        if (count < 1)
        {
            GD.PrintErr("Error: EmitBulletRing() must have count >= 1.");
            return;
        }

        float angle = 2 * Mathf.Pi / count;

        for (int i = 0; i < count; i++)
        {
            Bullet bullet = (Bullet)SpawnProjectile(template, parentNode, collisionFlag);
            Bullet.CopyData(template, bullet);
            bullet.Position = position;
            bullet.MovementNode.Direction = template.MovementNode.Direction.Rotated(i * angle);
        }
    }

    // Emit multiple bullets in a narrow cone shape
    // i.e. left/ right edges don't spawn bullets
    // spread is in radian
    // Note: Imagine dividing a triangular pizza, the internal edges are bullet tracks
    public static void EmitBulletConeNarrow(Bullet template, Node parentNode, int collisionFlag, Vector2 position, int count, float spread)
    {
        if (count < 1)
        {
            GD.PrintErr("Error: EmitBulletConeNarrow() must have count >= 1.");
            return;
        }

        float angle = spread / (count + 1);
        float half = spread / 2;

        for (int i = 0; i < count; i++)
        {
            Bullet bullet = (Bullet)SpawnProjectile(template, parentNode, collisionFlag);
            Bullet.CopyData(template, bullet);
            bullet.Position = position;
            bullet.MovementNode.Direction = template.MovementNode.Direction.Rotated((i + 1) * angle - half);
        }
    }

    // Emit multiple bullets in a wide cone shape
    // i.e. left/ right edges always spawn bullets
    // spread is in radian
    // Note: Imagine dividing a triangular pizza, all edges are bullet tracks
    public static void EmitBulletConeWide(Bullet template, Node parentNode, int collisionFlag, Vector2 position, int count, float spread)
    {
        if (count < 3)
        {
            GD.PrintErr("Error: EmitBulletConeNarrow() must have count >= 3.");
            return;
        }
        count -= 2;

        float angle = spread / (count + 1);
        float half = spread / 2;

        for (int i = 0; i < count + 2; i++)
        {
            Bullet bullet = (Bullet)SpawnProjectile(template, parentNode, collisionFlag);
            Bullet.CopyData(template, bullet);
            bullet.Position = position;
            bullet.MovementNode.Direction = template.MovementNode.Direction.Rotated(i * angle - half);
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        foreach (string key in _objectPools.Keys)
        {
            GD.Print("");
            GD.Print(key);
            while (_objectPools[key].Count > 0)
            {
                GD.Print(_objectPools[key].Pop().GetInstanceId());
            }
            GD.Print("--- Stack Bottom ---");
            GD.Print("");
        }

    }

}