namespace TerraNova.Core;

/// <summary>
/// Platform-agnostic interface for collision shapes.
/// Shapes define the geometry used for collision detection (box, sphere, capsule, mesh, etc.).
/// </summary>
public interface IPhysicsShape
{
    // Marker interface - specific shape types (box, sphere, capsule) are created via IPhysicsShapeFactory
    // The underlying physics engine handles the actual shape data
}
