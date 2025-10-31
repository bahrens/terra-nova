namespace TerraNova.Shared;

/// <summary>
/// Manages the voxel world and block placement
/// </summary>
public class World
{
    // Simple dictionary to store blocks at integer positions
    private readonly Dictionary<Vector3i, BlockType> _blocks = new();

    /// <summary>
    /// Sets a block at the given position
    /// </summary>
    public void SetBlock(int x, int y, int z, BlockType blockType)
    {
        var pos = new Vector3i(x, y, z);

        if (blockType == BlockType.Air)
        {
            _blocks.Remove(pos);
        }
        else
        {
            _blocks[pos] = blockType;
        }
    }

    /// <summary>
    /// Gets the block at the given position (returns Air if no block exists)
    /// </summary>
    public BlockType GetBlock(int x, int y, int z)
    {
        var pos = new Vector3i(x, y, z);
        return _blocks.TryGetValue(pos, out var blockType) ? blockType : BlockType.Air;
    }

    /// <summary>
    /// Checks if a solid block exists at the given position
    /// </summary>
    public bool IsSolid(int x, int y, int z)
    {
        return GetBlock(x, y, z) != BlockType.Air;
    }

    /// <summary>
    /// Gets all block positions and types
    /// </summary>
    public IEnumerable<(Vector3i position, BlockType blockType)> GetAllBlocks()
    {
        foreach (var kvp in _blocks)
        {
            yield return (kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Checks which faces of a block should be rendered (face culling)
    /// Returns a flags enum indicating which faces are visible
    /// </summary>
    public BlockFaces GetVisibleFaces(int x, int y, int z)
    {
        BlockFaces visibleFaces = BlockFaces.None;

        // Check each neighbor and mark face as visible if neighbor is air
        if (!IsSolid(x, y, z + 1)) visibleFaces |= BlockFaces.Front;   // +Z
        if (!IsSolid(x, y, z - 1)) visibleFaces |= BlockFaces.Back;    // -Z
        if (!IsSolid(x + 1, y, z)) visibleFaces |= BlockFaces.Right;   // +X
        if (!IsSolid(x - 1, y, z)) visibleFaces |= BlockFaces.Left;    // -X
        if (!IsSolid(x, y + 1, z)) visibleFaces |= BlockFaces.Top;     // +Y
        if (!IsSolid(x, y - 1, z)) visibleFaces |= BlockFaces.Bottom;  // -Y

        return visibleFaces;
    }
}

/// <summary>
/// Flags enum for cube faces
/// </summary>
[Flags]
public enum BlockFaces
{
    None   = 0,
    Front  = 1 << 0,  // +Z
    Back   = 1 << 1,  // -Z
    Right  = 1 << 2,  // +X
    Left   = 1 << 3,  // -X
    Top    = 1 << 4,  // +Y
    Bottom = 1 << 5,  // -Y
    All    = Front | Back | Right | Left | Top | Bottom
}
