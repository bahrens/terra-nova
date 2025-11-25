namespace TerraNova.Client.Rendering;

public class ChunkMeshData
{
    public float[] Vertices { get; init; }
    public uint[] Indices { get; init; }

    public ChunkMeshData(float[] vertices, uint[] indices)
    {
        Vertices = vertices;
        Indices = indices;
    }
}
