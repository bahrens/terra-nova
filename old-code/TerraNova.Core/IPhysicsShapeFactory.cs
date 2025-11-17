using TerraNova.Shared;

namespace TerraNova.Core;

/// <summary>
/// Platform-agnostic factory for creating physics collision shapes.
/// Abstracts shape creation from specific physics engine implementations.
/// </summary>
public interface IPhysicsShapeFactory
{
    /// <summary>
    /// Create a box collision shape.
    /// </summary>
    /// <param name="halfExtents">Half-extents of the box (half-width, half-height, half-depth)</param>
    /// <returns>Box collision shape</returns>
    IPhysicsShape CreateBox(Vector3 halfExtents);

    /// <summary>
    /// Create a sphere collision shape.
    /// </summary>
    /// <param name="radius">Radius of the sphere</param>
    /// <returns>Sphere collision shape</returns>
    IPhysicsShape CreateSphere(float radius);

    /// <summary>
    /// Create a capsule collision shape (cylinder with hemispherical ends).
    /// Ideal for player characters.
    /// </summary>
    /// <param name="radius">Radius of the capsule</param>
    /// <param name="height">Height of the cylindrical portion (excluding hemispheres)</param>
    /// <returns>Capsule collision shape</returns>
    IPhysicsShape CreateCapsule(float radius, float height);
}
