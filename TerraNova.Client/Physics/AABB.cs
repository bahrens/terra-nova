using TerraNova.Shared;

namespace TerraNova.Physics;

/// <summary>
/// Axis-Aligned Bounding Box for collision detection.
/// Represents a box aligned with world axes (no rotation).
/// </summary>
public struct AABB
{
    /// <summary>
    /// Minimum corner of the AABB (smallest X, Y, Z coordinates).
    /// </summary>
    public Vector3 Min;

    /// <summary>
    /// Maximum corner of the AABB (largest X, Y, Z coordinates).
    /// </summary>
    public Vector3 Max;

    /// <summary>
    /// Create an AABB from min/max corners.
    /// </summary>
    public AABB(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Create an AABB from center position and half-extents.
    /// </summary>
    public static AABB FromCenterAndExtents(Vector3 center, Vector3 halfExtents)
    {
        return new AABB(
            new Vector3(center.X - halfExtents.X, center.Y - halfExtents.Y, center.Z - halfExtents.Z),
            new Vector3(center.X + halfExtents.X, center.Y + halfExtents.Y, center.Z + halfExtents.Z)
        );
    }

    /// <summary>
    /// Get the center position of the AABB.
    /// </summary>
    public Vector3 Center => new Vector3(
        (Min.X + Max.X) * 0.5f,
        (Min.Y + Max.Y) * 0.5f,
        (Min.Z + Max.Z) * 0.5f
    );

    /// <summary>
    /// Get the size (full width/height/depth) of the AABB.
    /// </summary>
    public Vector3 Size => new Vector3(
        Max.X - Min.X,
        Max.Y - Min.Y,
        Max.Z - Min.Z
    );

    /// <summary>
    /// Get the half-extents of the AABB.
    /// </summary>
    public Vector3 HalfExtents => new Vector3(
        (Max.X - Min.X) * 0.5f,
        (Max.Y - Min.Y) * 0.5f,
        (Max.Z - Min.Z) * 0.5f
    );

    /// <summary>
    /// Test if this AABB intersects with another AABB.
    /// </summary>
    public bool Intersects(AABB other)
    {
        // AABBs intersect if they overlap on all three axes
        return (Min.X <= other.Max.X && Max.X >= other.Min.X) &&
               (Min.Y <= other.Max.Y && Max.Y >= other.Min.Y) &&
               (Min.Z <= other.Max.Z && Max.Z >= other.Min.Z);
    }

    /// <summary>
    /// Test if this AABB contains a point.
    /// </summary>
    public bool Contains(Vector3 point)
    {
        return point.X >= Min.X && point.X <= Max.X &&
               point.Y >= Min.Y && point.Y <= Max.Y &&
               point.Z >= Min.Z && point.Z <= Max.Z;
    }

    /// <summary>
    /// Expand this AABB to include another AABB.
    /// </summary>
    public AABB Union(AABB other)
    {
        return new AABB(
            new Vector3(
                Math.Min(Min.X, other.Min.X),
                Math.Min(Min.Y, other.Min.Y),
                Math.Min(Min.Z, other.Min.Z)
            ),
            new Vector3(
                Math.Max(Max.X, other.Max.X),
                Math.Max(Max.Y, other.Max.Y),
                Math.Max(Max.Z, other.Max.Z)
            )
        );
    }

    /// <summary>
    /// Offset (translate) this AABB by a vector.
    /// </summary>
    public AABB Offset(Vector3 offset)
    {
        return new AABB(
            new Vector3(Min.X + offset.X, Min.Y + offset.Y, Min.Z + offset.Z),
            new Vector3(Max.X + offset.X, Max.Y + offset.Y, Max.Z + offset.Z)
        );
    }

    /// <summary>
    /// Expand this AABB by a margin on all sides.
    /// </summary>
    public AABB Expand(float margin)
    {
        return new AABB(
            new Vector3(Min.X - margin, Min.Y - margin, Min.Z - margin),
            new Vector3(Max.X + margin, Max.Y + margin, Max.Z + margin)
        );
    }

    public override string ToString()
    {
        return $"AABB(Min: {Min}, Max: {Max})";
    }
}
