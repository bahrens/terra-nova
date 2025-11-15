using TerraNova.Shared;

namespace TerraNova.Core;

/// <summary>
/// Platform-agnostic interface for a rigid body in the physics simulation.
/// Represents an object with mass, velocity, position, and collision shape.
/// </summary>
public interface IPhysicsBody
{
    /// <summary>
    /// Get or set the position of the rigid body.
    /// </summary>
    Vector3 Position { get; set; }

    /// <summary>
    /// Get or set the linear velocity of the rigid body.
    /// </summary>
    Vector3 Velocity { get; set; }

    /// <summary>
    /// Get or set whether the body is affected by gravity.
    /// </summary>
    bool AffectedByGravity { get; set; }

    /// <summary>
    /// Get or set whether the body is static (immovable, infinite mass).
    /// Static bodies do not move or rotate, useful for terrain.
    /// </summary>
    bool IsStatic { get; set; }

    /// <summary>
    /// Get whether the body is currently on the ground (collision detected below).
    /// Used for jump mechanics and movement state detection.
    /// </summary>
    bool IsGrounded { get; }

    /// <summary>
    /// Apply an instantaneous force to the body (e.g., jump impulse).
    /// </summary>
    /// <param name="force">Force vector to apply</param>
    void ApplyForce(Vector3 force);

    /// <summary>
    /// Set the collision shape for this body.
    /// </summary>
    /// <param name="shape">The collision shape</param>
    void SetShape(IPhysicsShape shape);
}
