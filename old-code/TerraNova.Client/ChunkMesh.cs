using OpenTK.Graphics.OpenGL4;
using TerraNova.Core;

namespace TerraNova;

/// <summary>
/// Manages OpenGL buffers for a chunk mesh (VAO/VBO/EBO).
/// Takes pre-generated ChunkMeshData from ChunkMeshBuilder.
/// </summary>
public class ChunkMesh : IDisposable
{
    private int _vao; // Vertex Array Object
    private int _vbo; // Vertex Buffer Object (stores vertex data)
    private int _ebo; // Element Buffer Object (stores indices)
    private int _indexCount;
    private bool _disposed = false;

    public ChunkMesh(ChunkMeshData meshData)
    {
        GenerateMesh(meshData);
    }

    /// <summary>
    /// Creates OpenGL buffers from platform-agnostic ChunkMeshData
    /// </summary>
    private void GenerateMesh(ChunkMeshData meshData)
    {
        _indexCount = meshData.Indices.Length;

        // Early exit if no faces to render (empty chunk)
        if (_indexCount == 0)
        {
            return;
        }

        // Convert ChunkMeshData (separate arrays) to interleaved format
        // Format: position(3) + texCoord(2) + color(3) + brightness(1) = 9 floats per vertex
        int vertexCount = meshData.Vertices.Length / 3;
        float[] interleavedData = new float[vertexCount * 9];

        for (int i = 0; i < vertexCount; i++)
        {
            int srcVertIdx = i * 3;
            int srcTexIdx = i * 2;
            int srcColorIdx = i * 3;
            int dstIdx = i * 9;

            // Position (3 floats)
            interleavedData[dstIdx + 0] = meshData.Vertices[srcVertIdx + 0];
            interleavedData[dstIdx + 1] = meshData.Vertices[srcVertIdx + 1];
            interleavedData[dstIdx + 2] = meshData.Vertices[srcVertIdx + 2];

            // TexCoords (2 floats)
            interleavedData[dstIdx + 3] = meshData.TexCoords[srcTexIdx + 0];
            interleavedData[dstIdx + 4] = meshData.TexCoords[srcTexIdx + 1];

            // Color (3 floats)
            interleavedData[dstIdx + 5] = meshData.Colors[srcColorIdx + 0];
            interleavedData[dstIdx + 6] = meshData.Colors[srcColorIdx + 1];
            interleavedData[dstIdx + 7] = meshData.Colors[srcColorIdx + 2];

            // Brightness (1 float)
            interleavedData[dstIdx + 8] = meshData.Brightness[i];
        }

        // Create and bind VAO (stores the vertex attribute configuration)
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        // Create and upload VBO (vertex data)
        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, interleavedData.Length * sizeof(float),
                      interleavedData, BufferUsageHint.StaticDraw);

        // Create and upload EBO (index data)
        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, meshData.Indices.Length * sizeof(uint),
                      meshData.Indices, BufferUsageHint.StaticDraw);

        // Configure vertex attributes (stride is 9 floats per vertex)
        int stride = 9 * sizeof(float);

        // Position attribute (location = 0, 3 floats, offset = 0)
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);

        // UV attribute (location = 1, 2 floats, offset = 3 floats)
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // Color attribute (location = 2, 3 floats, offset = 5 floats)
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, 5 * sizeof(float));
        GL.EnableVertexAttribArray(2);

        // Brightness attribute (location = 3, 1 float, offset = 8 floats)
        GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, stride, 8 * sizeof(float));
        GL.EnableVertexAttribArray(3);

        // Unbind
        GL.BindVertexArray(0);
    }

    /// <summary>
    /// Renders the entire chunk in one draw call
    /// </summary>
    public void Draw()
    {
        if (_indexCount == 0) return; // Nothing to draw (empty chunk)

        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteVertexArray(_vao);
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
