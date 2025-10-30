namespace TerraNova;

/// <summary>
/// Procedurally generates textures for block types
/// </summary>
public static class TextureGenerator
{
    private static readonly Random _random = new Random(42); // Fixed seed for consistency

    /// <summary>
    /// Generates a texture for the given block type
    /// </summary>
    /// <param name="blockType">Type of block to generate texture for</param>
    /// <param name="size">Texture size (width and height in pixels)</param>
    /// <returns>RGBA pixel data</returns>
    public static byte[] GenerateTexture(BlockType blockType, int size = 16)
    {
        return blockType switch
        {
            BlockType.Grass => GenerateGrassTexture(size),
            BlockType.Dirt => GenerateDirtTexture(size),
            BlockType.Stone => GenerateStoneTexture(size),
            BlockType.Wood => GenerateWoodTexture(size),
            BlockType.Sand => GenerateSandTexture(size),
            _ => GenerateSolidColorTexture(size, 255, 255, 255) // White default
        };
    }

    private static byte[] GenerateGrassTexture(int size)
    {
        // Green grass with slight variation
        return GenerateNoisyTexture(size, 34, 139, 34, 20); // Forest green
    }

    private static byte[] GenerateDirtTexture(int size)
    {
        // Brown dirt with noise
        return GenerateNoisyTexture(size, 139, 90, 43, 30); // Brown
    }

    private static byte[] GenerateStoneTexture(int size)
    {
        // Gray stone with darker spots
        return GenerateNoisyTexture(size, 128, 128, 128, 40); // Gray
    }

    private static byte[] GenerateWoodTexture(int size)
    {
        // Brown wood with vertical-ish grain
        byte[] pixels = new byte[size * size * 4];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = (y * size + x) * 4;

                // Base brown color
                int baseR = 139;
                int baseG = 69;
                int baseB = 19;

                // Add vertical grain pattern
                int grainVariation = (int)(Math.Sin(x * 0.5) * 15);
                int noise = _random.Next(-20, 20);

                pixels[index + 0] = ClampByte(baseR + grainVariation + noise);
                pixels[index + 1] = ClampByte(baseG + grainVariation + noise);
                pixels[index + 2] = ClampByte(baseB + grainVariation + noise);
                pixels[index + 3] = 255; // Alpha
            }
        }

        return pixels;
    }

    private static byte[] GenerateSandTexture(int size)
    {
        // Yellow-tan sand
        return GenerateNoisyTexture(size, 238, 214, 175, 25); // Tan
    }

    /// <summary>
    /// Generates a texture with random noise variations
    /// </summary>
    private static byte[] GenerateNoisyTexture(int size, int r, int g, int b, int variation)
    {
        byte[] pixels = new byte[size * size * 4];

        for (int i = 0; i < size * size; i++)
        {
            int baseIndex = i * 4;
            int noise = _random.Next(-variation, variation);

            pixels[baseIndex + 0] = ClampByte(r + noise);
            pixels[baseIndex + 1] = ClampByte(g + noise);
            pixels[baseIndex + 2] = ClampByte(b + noise);
            pixels[baseIndex + 3] = 255; // Alpha
        }

        return pixels;
    }

    /// <summary>
    /// Generates a solid color texture
    /// </summary>
    private static byte[] GenerateSolidColorTexture(int size, int r, int g, int b)
    {
        byte[] pixels = new byte[size * size * 4];

        for (int i = 0; i < size * size; i++)
        {
            int baseIndex = i * 4;
            pixels[baseIndex + 0] = (byte)r;
            pixels[baseIndex + 1] = (byte)g;
            pixels[baseIndex + 2] = (byte)b;
            pixels[baseIndex + 3] = 255; // Alpha
        }

        return pixels;
    }

    private static byte ClampByte(int value)
    {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return (byte)value;
    }
}
