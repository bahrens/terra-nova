namespace TerraNova.GameLogic;

/// <summary>
/// Data transfer object containing mesh data for a chunk.
/// Platform-agnostic representation that can be consumed by any renderer.
/// </summary>
public record ChunkMeshData(
    float[] Vertices,   // xyz coordinates (3 floats per vertex)
    float[] Colors,     // rgb colors (3 floats per vertex)
    float[] TexCoords,  // uv texture coordinates (2 floats per vertex)
    uint[] Indices      // triangle indices
);
