using TerraNova.Shared;

namespace TerraNova.GameLogic;

/// <summary>
/// Handles procedural terrain generation with multi-octave noise, caves, and ores
/// </summary>
public class TerrainGenerator
{
    private readonly int _seed;
    private readonly Random _random;

    // Terrain height parameters
    private readonly float _baseHeight;
    private readonly float _heightMultiplier;
    private readonly float _scale;

    // Cave generation parameters
    private readonly float _caveThreshold = 0.55f;     // Threshold for cave generation
    private readonly float _caveScale = 0.04f;          // Equal scale for X, Y, Z for organic caves

    public TerrainGenerator(int seed, float baseHeight = 32f, float heightMultiplier = 32f, float scale = 0.01f)
    {
        _seed = seed;
        _random = new Random(seed);
        _baseHeight = baseHeight;
        _heightMultiplier = heightMultiplier;
        _scale = scale;

        // Set SimplexNoise seed for height map generation
        SimplexNoise.Noise.Seed = seed;
    }

    /// <summary>
    /// Generates terrain for a specific column (x, z) and fills the world
    /// </summary>
    public void GenerateColumn(World world, int x, int z, int maxHeight)
    {
        // Place bedrock layer at bottom (Y=0 is always bedrock)
        world.SetBlock(x, 0, z, BlockType.Bedrock);

        // Calculate terrain height using multi-octave noise
        int terrainHeight = CalculateTerrainHeight(x, z);
        terrainHeight = Math.Min(terrainHeight, maxHeight - 1);

        // Generate column from Y=1 (above bedrock) to terrain height
        for (int y = 1; y <= terrainHeight; y++)
        {
            // Check if this position should be a cave
            if (IsCave(x, y, z, terrainHeight))
            {
                continue; // Leave as air (cave)
            }

            // Determine block type based on depth from surface
            BlockType blockType = DetermineBlockType(y, terrainHeight);

            // Replace stone with ores at certain depths
            if (blockType == BlockType.Stone)
            {
                blockType = GenerateOre(x, y, z);
            }

            world.SetBlock(x, y, z, blockType);
        }
    }

    /// <summary>
    /// Calculates terrain height using multi-octave Perlin/Simplex noise
    /// </summary>
    private int CalculateTerrainHeight(int x, int z)
    {
        float height = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxValue = 0f;

        // Use 4 octaves for natural-looking terrain
        for (int octave = 0; octave < 4; octave++)
        {
            // Sample noise at different frequencies
            float scale = _scale * frequency;

            // Get noise value (0-255) and normalize to 0-1
            float noiseValue = (float)SimplexNoise.Noise.CalcPixel2D(x, z, scale);
            noiseValue = noiseValue / 255.0f; // Convert 0-255 to 0-1

            height += noiseValue * amplitude;
            maxValue += amplitude;

            amplitude *= 0.5f;  // Each octave has half the impact
            frequency *= 2.0f;  // Each octave has double the frequency (more detail)
        }

        // Normalize height to 0-1 range
        height = height / maxValue;

        // Apply to terrain height
        return (int)_baseHeight + (int)(height * _heightMultiplier);
    }

    /// <summary>
    /// Determines if a position should be a cave using multi-octave 3D Perlin noise
    /// Uses equal XYZ scaling with multi-octave detail for organic cave structures
    /// </summary>
    private bool IsCave(int x, int y, int z, int terrainHeight)
    {
        // Don't generate caves in top 10 blocks or bottom 5 blocks
        if (y > 118 || y < 5)
            return false;

        // Don't generate caves within 5 blocks of the surface (prevents exposed caves)
        if (y > terrainHeight - 5)
            return false;

        // Use single 3D noise with different horizontal/vertical scaling
        // This creates horizontal winding tunnels with varied ceiling heights
        float caveNoise = CalculateCaveNoise(x, y, z, 0);

        // Cave exists where noise is above threshold
        return caveNoise > _caveThreshold;
    }

    /// <summary>
    /// Calculates multi-octave 3D cave noise with equal scaling for organic caves
    /// </summary>
    private float CalculateCaveNoise(int x, int y, int z, int offset)
    {
        float noise = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxValue = 0f;

        // Use 4 octaves for natural cave variation
        for (int octave = 0; octave < 4; octave++)
        {
            // Equal scale for all dimensions creates organic caves
            float scale = _caveScale * frequency;

            float noiseValue = Get3DNoise(
                x * scale,
                y * scale,   // Equal scaling for all dimensions
                z * scale,
                offset + octave * 1000
            );

            noise += noiseValue * amplitude;
            maxValue += amplitude;

            amplitude *= 0.5f;  // Each octave has half the impact
            frequency *= 2.0f;  // Each octave has double the frequency (more detail)
        }

        // Normalize to 0-1 range
        return noise / maxValue;
    }

    /// <summary>
    /// Real 3D simplex noise using the library's CalcPixel3D method
    /// </summary>
    private float Get3DNoise(float x, float y, float z, int offset)
    {
        // Use real 3D simplex noise - this creates truly organic 3D structures
        float noiseValue = SimplexNoise.Noise.CalcPixel3D(
            (int)x + offset,
            (int)y + offset,
            (int)z + offset,
            1.0f);

        // Normalize to 0-1 range (CalcPixel3D returns 0-255)
        return noiseValue / 255f;
    }

    /// <summary>
    /// Determines block type based on depth from surface
    /// </summary>
    private BlockType DetermineBlockType(int y, int terrainHeight)
    {
        int depthFromSurface = terrainHeight - y;

        // Top layer: grass
        if (depthFromSurface == 0)
        {
            return BlockType.Grass;
        }
        // Next 1-4 layers: dirt (vary depth using Y coordinate)
        else if (depthFromSurface <= 3 + (y % 2))
        {
            return BlockType.Dirt;
        }
        // Everything below: stone
        else
        {
            return BlockType.Stone;
        }
    }

    /// <summary>
    /// Randomly generates ore veins at certain depths
    /// Returns the ore type or Stone if no ore should spawn
    /// </summary>
    private BlockType GenerateOre(int x, int y, int z)
    {
        // Use position-based seeded random for consistent ore placement
        float oreNoise = Get3DNoise(x * 0.1f, y * 0.1f, z * 0.1f, _seed);

        // Coal ore: Y 5-64, common (5% chance)
        if (y >= 5 && y <= 64 && oreNoise > 0.95f)
        {
            return BlockType.CoalOre;
        }

        // Iron ore: Y 5-54, uncommon (3% chance)
        if (y >= 5 && y <= 54 && oreNoise > 0.97f)
        {
            return BlockType.IronOre;
        }

        // Gold ore: Y 5-29, rare (1.5% chance)
        if (y >= 5 && y <= 29 && oreNoise > 0.985f)
        {
            return BlockType.GoldOre;
        }

        // Diamond ore: Y 5-12, very rare (0.5% chance)
        if (y >= 5 && y <= 12 && oreNoise > 0.995f)
        {
            return BlockType.DiamondOre;
        }

        // No ore, return stone
        return BlockType.Stone;
    }
}
