using Jitter2;
using Jitter2.Collision;
using Jitter2.LinearMath;
using Microsoft.Extensions.Logging;
using TerraNova.Core;
using TerraNova.Shared;

namespace TerraNova.Physics;

/// <summary>
/// Jitter2 implementation of IPhysicsWorld.
/// Wraps Jitter2.World to provide platform-agnostic physics simulation.
/// </summary>
public class JitterPhysicsWorld : IPhysicsWorld
{
    private readonly Jitter2.World _world;
    private readonly ILogger<JitterPhysicsWorld>? _logger;

    public JitterPhysicsWorld(ILogger<JitterPhysicsWorld>? logger = null)
    {
        _world = new Jitter2.World();
        // Default gravity (can be changed via SetGravity)
        _world.Gravity = new JVector(0, -9.81f, 0);
        _logger = logger;
    }

    private int _stepCount = 0;

    public void Step(float deltaTime)
    {
        _stepCount++;
        if (_stepCount % 60 == 0) // Log every 60 steps (~1 second at 60fps)
        {
            _logger?.LogDebug("Step #{StepCount}, dt={DeltaTime:F4}, RigidBodies={RigidBodyCount}",
                _stepCount, deltaTime, _world.RigidBodies.Count);
        }
        _world.Step(deltaTime);
    }

    public void AddBody(IPhysicsBody body)
    {
        if (body is not JitterPhysicsBody jitterBody)
            throw new ArgumentException("Body must be JitterPhysicsBody for Jitter2 physics world", nameof(body));

        // In Jitter2, bodies are created by the World and automatically added
        // This method is a no-op if the body already belongs to this world
        // Note: Jitter2 bodies are automatically added when created via CreateRigidBody()
    }

    public void RemoveBody(IPhysicsBody body)
    {
        if (body is not JitterPhysicsBody jitterBody)
            throw new ArgumentException("Body must be JitterPhysicsBody for Jitter2 physics world", nameof(body));

        _world.Remove(jitterBody.InternalBody);
    }

    public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out PhysicsHitInfo hitInfo)
    {
        // Convert Shared.Vector3 to Jitter's JVector
        JVector jOrigin = new JVector(origin.X, origin.Y, origin.Z);
        JVector jDirection = new JVector(direction.X, direction.Y, direction.Z);

        // Jitter2 API: RayCast moved to DynamicTree
        // We'll implement a simplified version for now - proper implementation needs callback
        // TODO: Implement proper raycast with DynamicTree callbacks
        hitInfo = default;
        return false;
    }

    public void SetGravity(Vector3 gravity)
    {
        _world.Gravity = new JVector(gravity.X, gravity.Y, gravity.Z);
    }

    /// <summary>
    /// Internal access to Jitter2 world for adapter classes.
    /// </summary>
    internal Jitter2.World InternalWorld => _world;

    /// <summary>
    /// Create a new physics body in this world.
    /// This is the proper way to create bodies in Jitter2.
    ///
    /// IMPORTANT: The body is created at origin (0,0,0).
    /// You MUST set the Position property BEFORE calling SetShape(),
    /// otherwise the shape will be registered in the spatial tree at (0,0,0)
    /// causing collision detection to fail.
    ///
    /// Correct order:
    /// 1. var body = physicsWorld.CreateBody();
    /// 2. body.Position = actualPosition;  // Set position first!
    /// 3. body.IsStatic = true/false;
    /// 4. body.SetShape(shape);  // Add shape last!
    /// </summary>
    public IPhysicsBody CreateBody()
    {
        var rigidBody = _world.CreateRigidBody();
        _logger?.LogDebug("Created RigidBody at position ({X},{Y},{Z}), MotionType={MotionType}, IsActive={IsActive}",
            rigidBody.Position.X, rigidBody.Position.Y, rigidBody.Position.Z, rigidBody.MotionType, rigidBody.IsActive);
        return new JitterPhysicsBody(rigidBody);
    }
}
