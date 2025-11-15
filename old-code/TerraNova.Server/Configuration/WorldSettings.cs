namespace TerraNova.Server.Configuration;

/// <summary>
/// World generation configuration settings
/// </summary>
public class WorldSettings
{
    // World dimensions
    public int WorldSizeX { get; set; } = 80;
    public int WorldSizeY { get; set; } = 16;
    public int WorldSizeZ { get; set; } = 80;

    // Terrain generation parameters
    /// <summary>
    /// Controls terrain "smoothness" - lower = smoother/larger features (typical: 0.01-0.05)
    /// </summary>
    public float TerrainScale { get; set; } = 0.02f;

    /// <summary>
    /// How tall the hills/valleys are (vertical variation range)
    /// </summary>
    public float TerrainHeightMultiplier { get; set; } = 50.0f;

    /// <summary>
    /// Minimum terrain height (Y coordinate)
    /// </summary>
    public int TerrainBaseHeight { get; set; } = 20;

    /// <summary>
    /// Seed for procedural terrain generation (same seed = same world)
    /// </summary>
    public int TerrainSeed { get; set; } = 12345;
}
