using Godot;
using System;
using System.Collections.Generic;

// This singleton class spawns projectiles.
// Note: Godot autoload requires Node type.
public class ProjectileManager : Node
{
    public static ProjectileManager Singleton { get; private set; }

    private static Dictionary<string, PackedScene> _cachedScenes = new Dictionary<string, PackedScene>();

    private static Dictionary<string, Stack<Node>> _objectPools = new Dictionary<string, Stack<Node>>();

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
    public static Node SpawnProjectile(string scenePath, Node parentNode)
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
        parentNode?.CallDeferred("add_child", projectile);

        // Caller should set the node owner when saving

        // Enable processing
        projectile.SetProcess(true);
        projectile.SetPhysicsProcess(true); // It reset to true in AddChild(projectile)

        // Initalize the projectile immediately, instead of waiting for _Ready()
        if (projectile is Bullet)
        {
            ((Bullet)projectile).Initalize(); // TODO need clean up
        }

        return projectile;
    }

    // Overload to use template
    public static Node SpawnProjectile(Node template, Node parentNode)
    {
        Node projectile = SpawnProjectile(template.Filename, parentNode);
        return projectile;
    }

    // Despawn at the end of frame
    public static void QueueDespawnProjectile(Node projectile)
    {
        Singleton.CallDeferred("DespawnProjectile", projectile);
    }

    // Recycle a projectile scene instance.
    // projectile:  Root node of a projectile scene
    private static void DespawnProjectile(Node projectile)
    {
        // Detatch from parent (if exist)
        projectile.GetParent()?.RemoveChild(projectile);

        // Prevent scenes from saving this node
        projectile.Owner = null;

        // Disable processing
        projectile.SetProcess(false);
        projectile.SetPhysicsProcess(false); // It reset to true in AddChild(projectile)

        // Add to object pool
        if (projectile.Filename.Length == 0)
        {
            GD.Print("Warning: ProjectileManager despawned a node that is not a scene instance.");
            projectile.QueueFree();
        }
        else
        {
            _objectPools[projectile.Filename].Push(projectile);
        }
        
    }

    // Called when the node enters the scene tree for the first time
    public override void _Ready()
    {
        Singleton = this;
    }

    // - - - Bullet Emitter Functions - - -

    // Sinful code; will refactor as an Emitter class later
    // Experimental

    // Emit one single bullet (based on template data)
    public static void EmitBulletLine(Bullet template, Node parentNode, Vector2 position)
    {
        Bullet bullet = (Bullet)SpawnProjectile(template, parentNode);
        Bullet.CopyData(template, bullet);
        bullet.Position = position;
    }

    // Emit one single bullet
    public static void EmitBulletSingle(Globals.Element element, Node parentNode, Vector2 position, Vector2 direction, int damage, bool fromPlayer)
    {
        Bullet bullet = (Bullet)SpawnProjectile(BulletScenePath[element], parentNode);
        bullet.Position = position;
        bullet.ChangeDirection(direction);
        bullet.Damage = damage;
        bullet.CollisionLayer = (uint)0;
        bullet.CollisionMask = (uint)0;
        if (fromPlayer)
        {
            bullet.SetCollisionLayerBit(Globals.PlayerProjectileLayerBit, true);
        }
        else
        {
            bullet.SetCollisionLayerBit(Globals.EnemyProjectileLayerBit, true);
        }

    }

    // public static void EmitBulletBeam(Globals.Element element, Node parentNode, Vector2 position, Vector2 direction, int damage, bool fromPlayer, int bulletCount, int width)

    // Emit bullets in a ring shape
    public static void EmitBulletRing(Globals.Element element, Node parentNode, Vector2 position, Vector2 direction, int damage, bool fromPlayer, int bulletCount)
    {
        if (bulletCount <= 0)
        {
            GD.PrintErr("Error: EmitBulletRing() must have bulletCount >= 1.");
            bulletCount = 1;
        }

        float angle = 2 * Mathf.Pi / bulletCount;

        for (int i = 0; i < bulletCount; i++)
        {
            EmitBulletSingle(element, parentNode, position, direction.Rotated(angle * i), damage, fromPlayer);
        }
    }

    // Emit bullets in a cone shape
    // Imagine diving a triangular pizza, the internal edges are bullet directions
    // If spawnEdgeBullets is true, two more bullets spawn on the outermost edges
    // maxSpread is in radian
    public static void EmitBulletCone(Globals.Element element, Node parentNode, Vector2 position, Vector2 direction, int damage, bool fromPlayer, int bulletCount, float maxSpread, bool spawnEdgeBullets)
    {
        if (bulletCount <= 0)
        {
            GD.PrintErr("Error: EmitBulletCone() must have bulletCount >= 1.");
            bulletCount = 1;
        }

        float angle = maxSpread / (bulletCount + 1);

        if (spawnEdgeBullets)
        {
            for (int i = 0; i < bulletCount + 2; i++)
            {
                EmitBulletSingle(element, parentNode, position, direction.Rotated(angle * i - maxSpread / 2), damage, fromPlayer);
            }
        }
        else
        {
            for (int i = 1; i < bulletCount + 1; i++)
            {
                EmitBulletSingle(element, parentNode, position, direction.Rotated(angle * i - maxSpread / 2), damage, fromPlayer);
            }
        }
    }

    // - - - Bullet Emitter Functions - - -

}