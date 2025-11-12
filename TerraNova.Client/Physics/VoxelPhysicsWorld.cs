using Microsoft.Extensions.Logging;
using TerraNova.Core;
using TerraNova.Shared;

namespace TerraNova.Physics;

/// <summary>
/// Voxel physics implementation of IPhysicsWorld.
/// Manages physics bodies and collision detection using custom AABB voxel collision.
/// </summary>
public class VoxelPhysicsWorld : IPhysicsWorld
{
    private readonly List<VoxelPhysicsBody> _bodies = new();
    private readonly ILogger<VoxelCollisionSystem>? _collisionLogger;
    private Vector3 _gravity = new Vector3(0, -9.81f, 0);
    private VoxelCollisionSystem? _collisionSystem;

    public VoxelPhysicsWorld(ILogger<VoxelCollisionSystem>? collisionLogger = null)
    {
        _collisionLogger = collisionLogger;
    }

    /// <summary>
    /// Set the World reference for voxel collision queries.
    /// Must be called before Step() for collision detection to work.
    /// </summary>
    public void SetWorld(World world)
    {
        _collisionSystem = new VoxelCollisionSystem(world, _collisionLogger);
    }

    public void Step(float deltaTime)
    {
        // Integrate physics for all dynamic bodies
        foreach (var body in _bodies)
        {
            if (body.IsStatic)
                continue;

            // Apply gravity if enabled
            if (body.AffectedByGravity)
            {
                body.Velocity += _gravity * deltaTime;
            }

            // Move body with collision detection
            if (_collisionSystem != null)
            {
                body.Position = _collisionSystem.MoveBody(body, deltaTime);
            }
            else
            {
                // Fallback: free-fall mode if collision system not initialized
                body.Position += body.Velocity * deltaTime;
            }
        }
    }

    public void AddBody(IPhysicsBody body)
    {
        if (body is not VoxelPhysicsBody voxelBody)
        {
            throw new ArgumentException("Body must be VoxelPhysicsBody for voxel physics world", nameof(body));
        }

        if (!_bodies.Contains(voxelBody))
        {
            _bodies.Add(voxelBody);
        }
    }

    public void RemoveBody(IPhysicsBody body)
    {
        if (body is VoxelPhysicsBody voxelBody)
        {
            _bodies.Remove(voxelBody);
        }
    }

    public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out PhysicsHitInfo hitInfo)
    {
        // Voxel physics doesn't handle raycasting - the game uses direct World.Raycast()
        // This is just a stub to satisfy the interface
        hitInfo = default;
        return false;
    }

    public void SetGravity(Vector3 gravity)
    {
        _gravity = gravity;
    }

    public IPhysicsBody CreateBody()
    {
        var body = new VoxelPhysicsBody();
        AddBody(body);
        return body;
    }
}
