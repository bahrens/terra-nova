using System.Numerics;

namespace TerraNova.Client.Rendering;

public static class TestMeshGenerator
{
    /// <summary>
    /// Creates a simple triangle facing the camera (for basic rendering tests).
    /// </summary>
    public static ChunkMeshData CreateTriangle()
    {
        // Triangle vertices: bottom-left, bottom-right, top-center
        // All facing +Z (toward camera at origin looking at -Z)
        var normal = new Vector3(0, 0, 1);

        float[] vertices =
        [
            // Position (3)         Normal (3)              TexCoord (2)
            -0.5f, -0.5f, 0.0f,   normal.X, normal.Y, normal.Z,    0.0f, 0.0f,  // bottom-left
            0.5f, -0.5f, 0.0f,    normal.X, normal.Y, normal.Z,    1.0f, 0.0f,  // bottom-right
            0.0f,  0.5f, 0.0f,    normal.X, normal.Y, normal.Z,    0.5f, 1.0f,  // top-center
        ];

        ushort[] indices = [0, 2, 1];

        return new ChunkMeshData(vertices, indices);
    }

    /// <summary>
    /// Creates a unit cube centered at origin (for 3D rendering tests).
    /// </summary>
    public static ChunkMeshData CreateCube()
    {
        var vertices = new List<float>();
        var indices = new List<ushort>();

        // Each face has 4 vertices and 2 triangles (6 indices)
        // Faces: +X, -X, +Y, -Y, +Z, -Z

        AddFace(vertices, indices,
            new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(1, 0, 0)); // +X face

        AddFace(vertices, indices,
            new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-1, 0, 0)); // -X face

        AddFace(vertices, indices,
            new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0, 1, 0)); // +Y face

        AddFace(vertices, indices,
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0, -1, 0)); // -Y face

        AddFace(vertices, indices,
            new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(0, 0, 1)); // +Z face

        AddFace(vertices, indices,
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(0, 0, -1)); // -Z face

        return new ChunkMeshData(vertices.ToArray(), indices.ToArray());
    }

    private static void AddFace(List<float> vertices, List<ushort> indices,
          Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal)
    {
        ushort startIndex = (ushort)(vertices.Count / ChunkMeshData.VertexStride);

        // UV coordinates for the quad
        Vector2[] uvs = [new(0, 0), new(0, 1), new(1, 1), new(1, 0)];
        Vector3[] positions = [v0, v1, v2, v3];

        for (int i = 0; i < 4; i++)
        {
            vertices.Add(positions[i].X);
            vertices.Add(positions[i].Y);
            vertices.Add(positions[i].Z);
            vertices.Add(normal.X);
            vertices.Add(normal.Y);
            vertices.Add(normal.Z);
            vertices.Add(uvs[i].X);
            vertices.Add(uvs[i].Y);
        }

        // Two triangles: 0-1-2 and 0-2-3
        indices.Add(startIndex);
        indices.Add((ushort)(startIndex + 1));
        indices.Add((ushort)(startIndex + 2));
        indices.Add(startIndex);
        indices.Add((ushort)(startIndex + 2));
        indices.Add((ushort)(startIndex + 3));
    }
}
