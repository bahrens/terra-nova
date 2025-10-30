namespace TerraNova;

/// <summary>
/// Represents different types of blocks in the world
/// </summary>
public enum BlockType
{
    Air,        // Empty space (not rendered)
    Grass,      // Grass block
    Dirt,       // Dirt block
    Stone,      // Stone block
    Wood,       // Wood block
    Sand        // Sand block
}

/// <summary>
/// Contains block-related utility methods
/// </summary>
public static class BlockHelper
{
    /// <summary>
    /// Returns the color for a given block type
    /// For now we use simple colors, later we'll add textures
    /// </summary>
    public static (float r, float g, float b) GetBlockColor(BlockType blockType)
    {
        return blockType switch
        {
            BlockType.Grass => (0.2f, 0.8f, 0.2f),  // Green
            BlockType.Dirt => (0.6f, 0.4f, 0.2f),   // Brown
            BlockType.Stone => (0.5f, 0.5f, 0.5f),  // Gray
            BlockType.Wood => (0.6f, 0.3f, 0.1f),   // Dark brown
            BlockType.Sand => (0.9f, 0.9f, 0.6f),   // Yellow-ish
            _ => (1.0f, 1.0f, 1.0f)                  // White (default)
        };
    }
}
