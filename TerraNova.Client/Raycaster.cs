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

        // Step along the ray
        float step = 0.1f;
        Vector3 previousPosition = origin;

        for (float distance = 0; distance < maxDistance; distance += step)
        {
            Vector3 currentPosition = origin + direction * distance;

            // Convert to block coordinates
            int x = (int)Math.Floor(currentPosition.X);
            int y = (int)Math.Floor(currentPosition.Y);
            int z = (int)Math.Floor(currentPosition.Z);

            // Check if there's a block at this position
            BlockType blockType = world.GetBlock(x, y, z);

            if (blockType != BlockType.Air)
            {
                // We hit a block! Determine which face was hit
                BlockFace hitFace = DetermineHitFace(previousPosition, currentPosition);

                return new RaycastHit
                {
                    BlockPosition = new TerraNova.Shared.Vector3i(x, y, z),
                    BlockType = blockType,
                    HitFace = hitFace,
                    Distance = distance
                };
            }

            previousPosition = currentPosition;
        }

        return null; // No block hit
    }

    private static BlockFace DetermineHitFace(Vector3 previousPos, Vector3 hitPos)
    {
        // Calculate which axis changed the most
        Vector3 delta = hitPos - previousPos;
        float absX = Math.Abs(delta.X);
        float absY = Math.Abs(delta.Y);
        float absZ = Math.Abs(delta.Z);

        if (absX > absY && absX > absZ)
        {
            return delta.X > 0 ? BlockFace.Left : BlockFace.Right;
        }
        else if (absY > absX && absY > absZ)
        {
            return delta.Y > 0 ? BlockFace.Bottom : BlockFace.Top;
        }
        else
        {
            return delta.Z > 0 ? BlockFace.Back : BlockFace.Front;
        }
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
