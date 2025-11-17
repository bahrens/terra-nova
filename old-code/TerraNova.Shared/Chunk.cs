namespace TerraNova.Shared;

/// <summary>
/// Represents a 16x16 column of blocks spanning the full world height (Minecraft-style).
/// Chunks are the fundamental unit for rendering and world management.
/// </summary>
public class Chunk
{
    public const int ChunkSize = 16; // 16x16 horizontal size
    public const int WorldHeight = 128; // Full vertical range (can be made configurable later)

    /// <summary>
    /// Position of this chunk in 2D chunk coordinates (X, Z only - no Y coordinate)
    /// Block position = (ChunkPosition.X * ChunkSize, Y, ChunkPosition.Z * ChunkSize)
    /// </summary>
    public Vector2i ChunkPosition { get; }

    /// <summary>
    /// 3D array storing block types [x, y, z] where x and z are 0-15, y is 0-127
    /// Using array instead of dictionary for dense storage efficiency
    /// </summary>
    private readonly BlockType[,,] _blocks;

    public Chunk(Vector2i chunkPosition)
    {
        ChunkPosition = chunkPosition;
        _blocks = new BlockType[ChunkSize, WorldHeight, ChunkSize];

        // Initialize all blocks to Air
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < WorldHeight; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    _blocks[x, y, z] = BlockType.Air;
                }
            }
        }
    }

    /// <summary>
    /// Gets a block at local chunk coordinates (x,z: 0-15; y: 0-127)
    /// </summary>
    public BlockType GetBlock(int localX, int localY, int localZ)
    {
        if (!IsValidLocalPosition(localX, localY, localZ))
            return BlockType.Air;

        return _blocks[localX, localY, localZ];
    }

    /// <summary>
    /// Sets a block at local chunk coordinates (x,z: 0-15; y: 0-127)
    /// </summary>
    public void SetBlock(int localX, int localY, int localZ, BlockType blockType)
    {
        if (!IsValidLocalPosition(localX, localY, localZ))
            return;

        _blocks[localX, localY, localZ] = blockType;
    }

    /// <summary>
    /// Checks if a block position is within chunk bounds
    /// </summary>
    private bool IsValidLocalPosition(int x, int y, int z)
    {
        return x >= 0 && x < ChunkSize &&
               y >= 0 && y < WorldHeight &&
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
            localY, // Y coordinate is absolute (not relative to chunk)
            ChunkPosition.Z * ChunkSize + localZ
        );
    }

    /// <summary>
    /// Converts world coordinates to 2D chunk coordinates (X, Z only)
    /// </summary>
    public static Vector2i WorldToChunkPosition(int worldX, int worldZ)
    {
        return new Vector2i(
            (int)Math.Floor((double)worldX / ChunkSize),
            (int)Math.Floor((double)worldZ / ChunkSize)
        );
    }

    /// <summary>
    /// Converts world coordinates to local chunk coordinates
    /// For X and Z: 0-15 range (horizontal position within chunk)
    /// For Y: unchanged (absolute world Y coordinate 0-127)
    /// </summary>
    public static Vector3i WorldToLocalPosition(int worldX, int worldY, int worldZ)
    {
        // Modulo operation to get 0-15 range for X and Z
        int localX = ((worldX % ChunkSize) + ChunkSize) % ChunkSize;
        int localZ = ((worldZ % ChunkSize) + ChunkSize) % ChunkSize;

        // Y is absolute in 2D column chunks
        return new Vector3i(localX, worldY, localZ);
    }
}
