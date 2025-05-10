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

    //private static HashSet<ulong> _despawnPool = new HashSet<ulong>();

    public static readonly Dictionary<Globals.Element, string> BulletScenePath = new Dictionary<Globals.Element, string>()
    {
        {Globals.Element.None, "res://scenes/projectiles/none_bullet.tscn"},
        {Globals.Element.Water, "res://scenes/projectiles/water_bullet.tscn"},
        {Globals.Element.Wood, "res://scenes/projectiles/wood_bullet.tscn"},
        {Globals.Element.Fire, "res://scenes/projectiles/fire_bullet.tscn"},
        {Globals.Element.Earth, "res://scenes/projectiles/earth_bullet.tscn"},
        {Globals.Element.Metal, "res://scenes/projectiles/metal_bullet.tscn"}
    };

    public override void _EnterTree()
    {
        base._EnterTree();

        Singleton = this;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
    }

    // Free all bullets
    public static void Clear()
    {
        foreach (Stack<Node> stack in _objectPools.Values)
        {
            foreach (Node node in stack)
            {
                if (node is Bullet bullet)
                {
                    bullet.Kill();
                }
            }

            stack.Clear();
        }
    }

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

        if (projectile is Bullet bullet)
        {
            bullet.Initalize();
        }

        return projectile;
    }

    // Get a projectile scene instance. The caller needs to set up the projectile.
    // Set parentNode to GetTree().Root in most cases
    // Returns the root node of a projectile scene
    // Warning: Do not call projectile.GetParent() in the same frame
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

        // Attatch to parent
        projectile.GetParent()?.RemoveChild(projectile);
        parentNode.AddChild(projectile); // Must be done first

        // Caller should set the node owner when saving

        // Enable processing
        projectile.SetProcess(true);
        projectile.SetPhysicsProcess(true); // It reset to true in AddChild(projectile)

        // Initalize the projectile immediately, instead of waiting for _Ready()
        if (projectile is Bullet bullet)
        {
            bullet.Initalize();
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
        // Check if acceptable projectile type
        if (!(projectile is Bullet bullet))
        {
            GD.Print($"Warning: {projectile.GetType()} type cannot be despawned by ProjectileManager.");
            return;
        }
        else
        {
            ulong instanceID = projectile.GetInstanceId();

            // Check if repeated calls
            //if (bullet.Active && !_despawnPool.Contains(instanceID))
            if (bullet.Active)
            {
                bullet.Active = false;

                //_despawnPool.Add(instanceID);

                Singleton.CallDeferred("DespawnProjectile", projectile, instanceID);
                
                //GD.Print($"{projectile.GetInstanceId()} Locked");
            }
        }
    }

    // Recycle a projectile scene instance.
    // projectile:  Root node of a projectile scene
    private static void DespawnProjectile(Node projectile, ulong instanceID)
    {
        //GD.Print($"{projectile.GetInstanceId()} Despawned");
        
        if (projectile == null)
        {
            GD.PrintErr("Error: Cannot despawn a null projectile.");
            return;
        }

        if (projectile.Filename.Length == 0)
        {
            GD.PrintErr("Warning: ProjectileManager cannot despawn a node that is not a scene instance.");
            return;
        }

        // Disable processing
        //projectile.SetProcess(false);
        //projectile.SetPhysicsProcess(false); // It reset to true in AddChild(projectile)

        // Detatch from parent (if exist)
        projectile.GetParent()?.RemoveChild(projectile);

        // Prevent scenes from saving this node
        projectile.Owner = null;

        // Add to object pool
        _objectPools[projectile.Filename].Push(projectile);

        //_despawnPool.Remove(instanceID);
        //GD.Print($"{projectile.GetInstanceId()} Unlocked");
    }

    // - - - Bullet Emitter Functions - - -

    // Emit a bullet in a line shape
    public static void EmitBulletLine(Bullet template, Node parentNode, Vector2 position)
    {
        Bullet bullet = (Bullet)SpawnProjectile(template, parentNode);
        Bullet.CopyData(template, bullet);
        bullet.Position = position + Vector2.Right;
        // bullet.MovementNode.Direction = template.MovementNode.Direction;
        bullet.Active = true;
        
        //GD.Print($"{bullet.GetInstanceId()} Emit");
    }

    // Emit multiple bullet in a wall shape
    public static void EmitBulletWall(Bullet template, Node parentNode, Vector2 position, int count, float separation)
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
            Bullet bullet = (Bullet)SpawnProjectile(template, parentNode);
            Bullet.CopyData(template, bullet);
            bullet.Position = position + (i * separation - half) * cross;
            // bullet.MovementNode.Direction = template.MovementNode.Direction;
            bullet.Active = true;
        }

    }

    // Emit multiple bullets in a ring shape
    public static void EmitBulletRing(Bullet template, Node parentNode, Vector2 position, int count)
    {
        if (count < 1)
        {
            GD.PrintErr("Error: EmitBulletRing() must have count >= 1.");
            return;
        }

        float angle = 2 * Mathf.Pi / count;

        for (int i = 0; i < count; i++)
        {
            Bullet bullet = (Bullet)SpawnProjectile(template, parentNode);
            Bullet.CopyData(template, bullet);
            bullet.Position = position;
            bullet.MovementNode.Direction = template.MovementNode.Direction.Rotated(i * angle);
            bullet.Active = true;
        }
    }

    // Emit multiple bullets in a narrow cone shape
    // i.e. left/ right edges don't spawn bullets
    // spread is in radian
    // Note: Imagine dividing a triangular pizza, the internal edges are bullet tracks
    public static void EmitBulletConeNarrow(Bullet template, Node parentNode, Vector2 position, int count, float spread)
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
            Bullet bullet = (Bullet)SpawnProjectile(template, parentNode);
            Bullet.CopyData(template, bullet);
            bullet.Position = position;
            bullet.MovementNode.Direction = template.MovementNode.Direction.Rotated((i + 1) * angle - half);
            bullet.Active = true;
        }
    }

    // Emit multiple bullets in a wide cone shape
    // i.e. left/ right edges always spawn bullets
    // spread is in radian
    // Note: Imagine dividing a triangular pizza, all edges are bullet tracks
    public static void EmitBulletConeWide(Bullet template, Node parentNode, Vector2 position, int count, float spread)
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
            Bullet bullet = (Bullet)SpawnProjectile(template, parentNode);
            Bullet.CopyData(template, bullet);
            bullet.Position = position;
            bullet.MovementNode.Direction = template.MovementNode.Direction.Rotated(i * angle - half);
            bullet.Active = true;
        }
    }

}