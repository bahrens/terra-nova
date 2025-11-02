using OpenTK.Mathematics;
using TerraNova.Shared;

namespace TerraNova;

/// <summary>
/// Performs raycasting to detect which block the player is looking at
/// </summary>
public static class Raycaster
{
    /// <summary>
    /// Casts a ray from the camera and returns the block being looked at
    /// </summary>
    /// <param name="world">The world to check against</param>
    /// <param name="origin">Ray origin (camera position)</param>
    /// <param name="direction">Ray direction (camera forward)</param>
    /// <param name="maxDistance">Maximum distance to check</param>
    /// <returns>Hit information if a block was hit, null otherwise</returns>
    public static RaycastHit? Cast(World world, Vector3 origin, Vector3 direction, float maxDistance = 10f)
    {
        direction = direction.Normalized();

        // DDA-style voxel traversal to find potential blocks
        // Start by stepping along the ray with a small step size
        float step = 0.1f;
        RaycastHit? closestHit = null;
        float closestDistance = float.MaxValue;

        for (float distance = 0; distance < maxDistance; distance += step)
        {
            Vector3 currentPosition = origin + direction * distance;

            // Convert to block coordinates
            int x = (int)Math.Round(currentPosition.X, MidpointRounding.AwayFromZero);
            int y = (int)Math.Round(currentPosition.Y, MidpointRounding.AwayFromZero);
            int z = (int)Math.Round(currentPosition.Z, MidpointRounding.AwayFromZero);

            // Check if there's a block at this position
            BlockType blockType = world.GetBlock(x, y, z);

            if (blockType != BlockType.Air)
            {
                // Found a solid block - do precise AABB intersection test
                var hit = IntersectAABB(origin, direction, new Vector3(x, y, z));
                if (hit.HasValue && hit.Value.Distance < closestDistance)
                {
                    closestDistance = hit.Value.Distance;
                    closestHit = new RaycastHit
                    {
                        BlockPosition = new TerraNova.Shared.Vector3i(x, y, z),
                        BlockType = blockType,
                        HitFace = hit.Value.Face,
                        Distance = hit.Value.Distance
                    };
                    // Found a hit, return it
                    return closestHit;
                }
            }
        }

        return closestHit;
    }

    /// <summary>
    /// Performs precise ray-AABB intersection test and determines which face was hit
    /// </summary>
    private static (float Distance, BlockFace Face)? IntersectAABB(Vector3 rayOrigin, Vector3 rayDirection, Vector3 blockCenter)
    {
        // Block extends from -0.5 to +0.5 around its center
        Vector3 boxMin = blockCenter - new Vector3(0.5f, 0.5f, 0.5f);
        Vector3 boxMax = blockCenter + new Vector3(0.5f, 0.5f, 0.5f);

        // Compute intersection distances for each slab
        float tMin = 0.0f;
        float tMax = float.MaxValue;
        BlockFace hitFace = BlockFace.Front;

        // X slab
        if (Math.Abs(rayDirection.X) > 0.0001f)
        {
            float t1 = (boxMin.X - rayOrigin.X) / rayDirection.X;
            float t2 = (boxMax.X - rayOrigin.X) / rayDirection.X;
            float tNear = Math.Min(t1, t2);
            float tFar = Math.Max(t1, t2);

            if (tNear > tMin)
            {
                tMin = tNear;
                hitFace = rayDirection.X > 0 ? BlockFace.Left : BlockFace.Right;
            }
            tMax = Math.Min(tMax, tFar);

            if (tMin > tMax) return null;
        }
        else if (rayOrigin.X < boxMin.X || rayOrigin.X > boxMax.X)
        {
            return null;
        }

        // Y slab
        if (Math.Abs(rayDirection.Y) > 0.0001f)
        {
            float t1 = (boxMin.Y - rayOrigin.Y) / rayDirection.Y;
            float t2 = (boxMax.Y - rayOrigin.Y) / rayDirection.Y;
            float tNear = Math.Min(t1, t2);
            float tFar = Math.Max(t1, t2);

            if (tNear > tMin)
            {
                tMin = tNear;
                hitFace = rayDirection.Y > 0 ? BlockFace.Bottom : BlockFace.Top;
            }
            tMax = Math.Min(tMax, tFar);

            if (tMin > tMax) return null;
        }
        else if (rayOrigin.Y < boxMin.Y || rayOrigin.Y > boxMax.Y)
        {
            return null;
        }

        // Z slab
        if (Math.Abs(rayDirection.Z) > 0.0001f)
        {
            float t1 = (boxMin.Z - rayOrigin.Z) / rayDirection.Z;
            float t2 = (boxMax.Z - rayOrigin.Z) / rayDirection.Z;
            float tNear = Math.Min(t1, t2);
            float tFar = Math.Max(t1, t2);

            if (tNear > tMin)
            {
                tMin = tNear;
                hitFace = rayDirection.Z > 0 ? BlockFace.Back : BlockFace.Front;
            }
            tMax = Math.Min(tMax, tFar);

            if (tMin > tMax) return null;
        }
        else if (rayOrigin.Z < boxMin.Z || rayOrigin.Z > boxMax.Z)
        {
            return null;
        }

        // If tMin is negative, the ray origin is inside the box
        if (tMin < 0) return null;

        return (tMin, hitFace);
    }
}

/// <summary>
/// Information about a raycast hit
/// </summary>
public class RaycastHit
{
    public TerraNova.Shared.Vector3i BlockPosition { get; set; }
    public BlockType BlockType { get; set; }
    public BlockFace HitFace { get; set; }
    public float Distance { get; set; }
}

/// <summary>
/// Represents the face of a block that was hit
/// </summary>
public enum BlockFace
{
    Front,
    Back,
    Left,
    Right,
    Top,
    Bottom
}
