namespace TerraNova.Shared;

/// <summary>
/// Represents a 16x16x16 region of blocks in the world.
/// Chunks are the fundamental unit for rendering and world management.
/// </summary>
public class Chunk
{
    public const int ChunkSize = 16; // 16x16x16 blocks per chunk

    /// <summary>
    /// Position of this chunk in chunk coordinates (not block coordinates)
    /// Block position = ChunkPosition * ChunkSize
    /// </summary>
    public Vector3i ChunkPosition { get; }

    /// <summary>
    /// 3D array storing block types [x, y, z] where each dimension is 0-15
    /// Using array instead of dictionary for dense storage efficiency
    /// </summary>
    private readonly BlockType[,,] _blocks;

    /// <summary>
    /// Tracks if this chunk has been modified since last mesh generation
    /// </summary>
    public bool IsDirty { get; set; }

    public Chunk(Vector3i chunkPosition)
    {
        ChunkPosition = chunkPosition;
        _blocks = new BlockType[ChunkSize, ChunkSize, ChunkSize];

        // Initialize all blocks to Air
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    _blocks[x, y, z] = BlockType.Air;
                }
            }
        }

        IsDirty = true;
    }

    /// <summary>
    /// Gets a block at local chunk coordinates (0-15 for each axis)
    /// </summary>
    public BlockType GetBlock(int localX, int localY, int localZ)
    {
        if (!IsValidLocalPosition(localX, localY, localZ))
            return BlockType.Air;

        return _blocks[localX, localY, localZ];
    }

    /// <summary>
    /// Sets a block at local chunk coordinates (0-15 for each axis)
    /// </summary>
    public void SetBlock(int localX, int localY, int localZ, BlockType blockType)
    {
        if (!IsValidLocalPosition(localX, localY, localZ))
            return;

        _blocks[localX, localY, localZ] = blockType;
        IsDirty = true;
    }

    /// <summary>
    /// Checks if a block position is within chunk bounds
    /// </summary>
    private bool IsValidLocalPosition(int x, int y, int z)
    {
        return x >= 0 && x < ChunkSize &&
               y >= 0 && y < ChunkSize &&
               z >= 0 && z < ChunkSize;
    }

    /// <summary>
    /// Checks if a block at local coordinates is solid (not Air)
    /// </summary>
    public bool IsSolid(int localX, int localY, int localZ)
    {
        return GetBlock(localX, localY, localZ) != BlockType.Air;
    }

    /// <summary>
    /// Gets the world position of a block from local chunk coordinates
    /// </summary>
    public Vector3i GetWorldPosition(int localX, int localY, int localZ)
    {
        return new Vector3i(
            ChunkPosition.X * ChunkSize + localX,
            ChunkPosition.Y * ChunkSize + localY,
            ChunkPosition.Z * ChunkSize + localZ
        );
    }

    /// <summary>
    /// Converts world coordinates to chunk coordinates
    /// </summary>
    public static Vector3i WorldToChunkPosition(int worldX, int worldY, int worldZ)
    {
        return new Vector3i(
            (int)Math.Floor((double)worldX / ChunkSize),
            (int)Math.Floor((double)worldY / ChunkSize),
            (int)Math.Floor((double)worldZ / ChunkSize)
        );
    }

    /// <summary>
    /// Converts world coordinates to local chunk coordinates (0-15)
    /// </summary>
    public static Vector3i WorldToLocalPosition(int worldX, int worldY, int worldZ)
    {
        // Modulo operation to get 0-15 range
        int localX = ((worldX % ChunkSize) + ChunkSize) % ChunkSize;
        int localY = ((worldY % ChunkSize) + ChunkSize) % ChunkSize;
        int localZ = ((worldZ % ChunkSize) + ChunkSize) % ChunkSize;

        return new Vector3i(localX, localY, localZ);
    }
}
