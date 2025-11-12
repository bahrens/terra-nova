using TerraNova.Shared;

namespace TerraNova.Core;

/// <summary>
/// Platform-agnostic interface for physics world simulation.
/// Manages rigid bodies, collision detection, and simulation stepping.
/// </summary>
public interface IPhysicsWorld
{
    /// <summary>
    /// Step the physics simulation forward by the specified time delta.
    /// </summary>
    /// <param name="deltaTime">Time step in seconds</param>
    void Step(float deltaTime);

    /// <summary>
    /// Add a rigid body to the physics world.
    /// </summary>
    /// <param name="body">The physics body to add</param>
    void AddBody(IPhysicsBody body);

    /// <summary>
    /// Remove a rigid body from the physics world.
    /// </summary>
    /// <param name="body">The physics body to remove</param>
    void RemoveBody(IPhysicsBody body);

    /// <summary>
    /// Perform a raycast in the physics world.
    /// </summary>
    /// <param name="origin">Ray origin position</param>
    /// <param name="direction">Ray direction (should be normalized)</param>
    /// <param name="maxDistance">Maximum ray distance</param>
    /// <param name="hitInfo">Output hit information if ray hits something</param>
    /// <returns>True if ray hit a body, false otherwise</returns>
    bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out PhysicsHitInfo hitInfo);

    /// <summary>
    /// Set the gravity vector for the physics world.
    /// </summary>
    /// <param name="gravity">Gravity acceleration vector (e.g., (0, -9.81, 0) for Earth gravity)</param>
    void SetGravity(Vector3 gravity);

    /// <summary>
    /// Create a new physics body in this world.
    /// The body is automatically added to the world upon creation.
    /// </summary>
    /// <returns>A new physics body</returns>
    IPhysicsBody CreateBody();
}
