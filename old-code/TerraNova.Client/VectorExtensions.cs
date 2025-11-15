using OpenTK.Mathematics;

namespace TerraNova;

/// <summary>
/// Extension methods for converting between OpenTK and shared vector types
/// </summary>
public static class VectorExtensions
{
    /// <summary>
    /// Converts OpenTK Vector3 to shared Vector3
    /// </summary>
    public static Shared.Vector3 ToShared(this Vector3 v)
    {
        return new Shared.Vector3(v.X, v.Y, v.Z);
    }

    /// <summary>
    /// Converts shared Vector3 to OpenTK Vector3
    /// </summary>
    public static Vector3 ToOpenTK(this Shared.Vector3 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }
}
