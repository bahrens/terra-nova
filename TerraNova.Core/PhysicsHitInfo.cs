using TerraNova.Shared;

namespace TerraNova.Core;

/// <summary>
/// Information about a physics raycast hit.
/// </summary>
public struct PhysicsHitInfo
{
    /// <summary>
    /// The body that was hit by the raycast.
    /// </summary>
    public IPhysicsBody? Body { get; set; }

    /// <summary>
    /// The world-space position where the ray hit the body.
    /// </summary>
    public Vector3 HitPoint { get; set; }

    /// <summary>
    /// The surface normal at the hit point.
    /// </summary>
    public Vector3 Normal { get; set; }

    /// <summary>
    /// The distance from the ray origin to the hit point.
    /// </summary>
    public float Distance { get; set; }
}
