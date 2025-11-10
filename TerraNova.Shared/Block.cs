namespace TerraNova.Shared;

/// <summary>
/// Represents different types of blocks in the world
/// </summary>
public enum BlockType
{
    Air,        // Empty space (not rendered)
    Grass,      // Grass block
    Dirt,       // Dirt block
    Stone,      // Stone block
    Wood,       // Wood block (log)
    Sand,       // Sand block
    Planks,     // Wooden planks
    Gravel,     // Gravel block
    Glass,      // Glass block
    Leaves,     // Leaf block
    CoalOre,    // Coal ore block
    IronOre,    // Iron ore block
    GoldOre,    // Gold ore block
    DiamondOre  // Diamond ore block
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
            BlockType.Grass => (0.4f, 0.65f, 0.35f),    // Darker grass green
            BlockType.Dirt => (0.55f, 0.42f, 0.30f),    // Minecraft brown
            BlockType.Stone => (0.6f, 0.6f, 0.6f),      // Lighter neutral gray
            BlockType.Wood => (0.6f, 0.3f, 0.1f),       // Dark brown (log)
            BlockType.Sand => (0.9f, 0.9f, 0.6f),       // Yellow-ish
            BlockType.Planks => (0.72f, 0.52f, 0.32f),  // Lighter brown (wooden planks)
            BlockType.Gravel => (0.53f, 0.53f, 0.53f),  // Medium gray
            BlockType.Glass => (0.7f, 0.9f, 1.0f),      // Light blue (transparent later)
            BlockType.Leaves => (0.2f, 0.6f, 0.2f),     // Dark green
            BlockType.CoalOre => (0.2f, 0.2f, 0.2f),    // Dark gray/black (coal)
            BlockType.IronOre => (0.7f, 0.6f, 0.5f),    // Tan/beige (iron)
            BlockType.GoldOre => (0.9f, 0.8f, 0.2f),    // Gold/yellow
            BlockType.DiamondOre => (0.3f, 0.8f, 0.9f), // Cyan/light blue (diamond)
            _ => (1.0f, 1.0f, 1.0f)                     // White (default)
        };
    }
}
