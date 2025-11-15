using TerraNova.Core;
using TerraNova.Shared;

namespace TerraNova.Physics;

/// <summary>
/// Voxel physics implementation of IPhysicsShape.
/// Stores AABB dimensions for collision detection.
/// </summary>
public class VoxelPhysicsShape : IPhysicsShape
{
    /// <summary>
    /// Half-extents of the shape (half-width, half-height, half-depth).
    /// Full size = halfExtents * 2
    /// </summary>
    public Vector3 HalfExtents { get; }

    /// <summary>
    /// The type of shape (Box, Capsule, Sphere).
    /// </summary>
    public ShapeType Type { get; }

    /// <summary>
    /// For capsules: the radius of the capsule.
    /// </summary>
    public float Radius { get; }

    /// <summary>
    /// For capsules: the height of the cylindrical portion (excluding hemispheres).
    /// </summary>
    public float Height { get; }

    private VoxelPhysicsShape(ShapeType type, Vector3 halfExtents, float radius = 0, float height = 0)
    {
        Type = type;
        HalfExtents = halfExtents;
        Radius = radius;
        Height = height;
    }

    /// <summary>
    /// Create a box shape.
    /// </summary>
    public static VoxelPhysicsShape CreateBox(Vector3 halfExtents)
    {
        return new VoxelPhysicsShape(ShapeType.Box, halfExtents);
    }

    /// <summary>
    /// Create a sphere shape.
    /// </summary>
    public static VoxelPhysicsShape CreateSphere(float radius)
    {
        // Sphere represented as AABB with equal half-extents
        Vector3 halfExtents = new Vector3(radius, radius, radius);
        return new VoxelPhysicsShape(ShapeType.Sphere, halfExtents, radius);
    }

    /// <summary>
    /// Create a capsule shape (cylinder with hemispherical ends).
    /// </summary>
    /// <param name="radius">Radius of the capsule</param>
    /// <param name="height">Height of the cylindrical portion (excluding hemispheres)</param>
    public static VoxelPhysicsShape CreateCapsule(float radius, float height)
    {
        // Capsule represented as AABB with height + 2*radius in Y
        // Total height = height (cylinder) + 2*radius (hemispheres)
        Vector3 halfExtents = new Vector3(radius, (height + 2 * radius) / 2, radius);
        return new VoxelPhysicsShape(ShapeType.Capsule, halfExtents, radius, height);
    }
}

/// <summary>
/// Shape type enumeration for voxel physics.
/// </summary>
public enum ShapeType
{
    Box,
    Sphere,
    Capsule
}
