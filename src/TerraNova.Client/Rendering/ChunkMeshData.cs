namespace TerraNova.Client.Rendering;

/// <summary>
/// Mesh data for a single chunk.
///
/// Vertex format (interleaved):
///   - Position: vec3 (12 bytes, offset 0)
///   - Normal: vec3 (12 bytes, offset 12)
///   - TexCoord: vec2 (8 bytes, offset 24)
///
/// Total stride: 32 bytes (8 floats per vertex)
/// </summary>
public class ChunkMeshData
{
    /// <summary>
    /// Interleaved vertex data: [pos.x, pos.y, pos.z, normal.x, normal.y, normal.z, uv.x, uv.y, ...]
    /// </summary>
    public float[] Vertices { get; init; }

    /// <summary>
    /// Triangle indices (max 65535 vertices per chunk).
    /// </summary>
    public ushort[] Indices { get; init; }

    public const int VertexStride = 8; // floats per vertex
    public const int VertexStrideBytes = 32; // bytes per vertex

    public ChunkMeshData(float[] vertices, ushort[] indices)
    {
        Vertices = vertices;
        Indices = indices;
    }
}
