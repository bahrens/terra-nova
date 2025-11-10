namespace TerraNova.Shared;

/// <summary>
/// Manages the voxel world and block placement using chunk-based storage (2D column chunks)
/// </summary>
public class World
{
    // Dictionary to store chunk columns by their 2D position (X, Z)
    private readonly Dictionary<Vector2i, Chunk> _chunks = new();

    /// <summary>
    /// Sets a block at the given world position
    /// </summary>
    public void SetBlock(int x, int y, int z, BlockType blockType)
    {
        // Calculate which chunk column this block belongs to (X, Z only)
        Vector2i chunkPos = Chunk.WorldToChunkPosition(x, z);
        Vector3i localPos = Chunk.WorldToLocalPosition(x, y, z);

        // Get or create the chunk
        if (!_chunks.TryGetValue(chunkPos, out var chunk))
        {
            chunk = new Chunk(chunkPos);
            _chunks[chunkPos] = chunk;
        }

        // Set the block in the chunk
        chunk.SetBlock(localPos.X, localPos.Y, localPos.Z, blockType);

        // If setting to Air and chunk is now empty, could remove chunk (optimization for later)
        // For now, keep the chunk even if empty
    }

    /// <summary>
    /// Gets the block at the given world position (returns Air if no block exists)
    /// </summary>
    public BlockType GetBlock(int x, int y, int z)
    {
        // Calculate which chunk column this block belongs to (X, Z only)
        Vector2i chunkPos = Chunk.WorldToChunkPosition(x, z);
        Vector3i localPos = Chunk.WorldToLocalPosition(x, y, z);

        // Get the chunk (if it doesn't exist, return Air)
        if (!_chunks.TryGetValue(chunkPos, out var chunk))
            return BlockType.Air;

        return chunk.GetBlock(localPos.X, localPos.Y, localPos.Z);
    }

    /// <summary>
    /// Checks if a solid block exists at the given position
    /// </summary>
    public bool IsSolid(int x, int y, int z)
    {
        return GetBlock(x, y, z) != BlockType.Air;
    }

    /// <summary>
    /// Gets all block positions and types (iterates through all chunks)
    /// </summary>
    public IEnumerable<(Vector3i position, BlockType blockType)> GetAllBlocks()
    {
        foreach (var (chunkPos, chunk) in _chunks)
        {
            // Iterate through all blocks in this chunk
            for (int x = 0; x < Chunk.ChunkSize; x++)
            {
                for (int y = 0; y < Chunk.WorldHeight; y++)
                {
                    for (int z = 0; z < Chunk.ChunkSize; z++)
                    {
                        BlockType blockType = chunk.GetBlock(x, y, z);
                        if (blockType != BlockType.Air)
                        {
                            Vector3i worldPos = chunk.GetWorldPosition(x, y, z);
                            yield return (worldPos, blockType);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets all chunks in the world
    /// </summary>
    public IEnumerable<Chunk> GetAllChunks()
    {
        return _chunks.Values;
    }

    /// <summary>
    /// Gets a specific chunk at the given 2D chunk position (X, Z only)
    /// </summary>
    public Chunk? GetChunk(Vector2i chunkPosition)
    {
        _chunks.TryGetValue(chunkPosition, out var chunk);
        return chunk;
    }

    /// <summary>
    /// Checks if a chunk exists at the given position
    /// </summary>
    public bool HasChunk(Vector2i chunkPosition)
    {
        return _chunks.ContainsKey(chunkPosition);
    }

    /// <summary>
    /// Gets or creates a chunk at the given position
    /// </summary>
    public Chunk GetOrCreateChunk(Vector2i chunkPosition)
    {
        if (!_chunks.TryGetValue(chunkPosition, out var chunk))
        {
            chunk = new Chunk(chunkPosition);
            _chunks[chunkPosition] = chunk;
        }
        return chunk;
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
