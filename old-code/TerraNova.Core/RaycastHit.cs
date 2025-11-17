using TerraNova.Shared;

namespace TerraNova.Core;

/// <summary>
/// Information about a raycast hit
/// </summary>
public class RaycastHit
{
    public Vector3i BlockPosition { get; set; }
    public BlockType BlockType { get; set; }
    public BlockFace HitFace { get; set; }
    public float Distance { get; set; }
}
