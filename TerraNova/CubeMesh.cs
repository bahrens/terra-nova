using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace TerraNova;

/// <summary>
/// Generates and manages mesh data for rendering a cube
/// </summary>
public class CubeMesh : IDisposable
{
    private int _vao; // Vertex Array Object
    private int _vbo; // Vertex Buffer Object (stores vertex data)
    private int _ebo; // Element Buffer Object (stores indices)
    private int _indexCount;
    private bool _disposed = false;

    public CubeMesh(Vector3 position, BlockType blockType)
    {
        GenerateCube(position, blockType);
    }

    /// <summary>
    /// Generates vertex data for a cube at the given position
    /// </summary>
    private void GenerateCube(Vector3 pos, BlockType blockType)
    {
        var color = BlockHelper.GetBlockColor(blockType);
        float r = color.r, g = color.g, b = color.b;

        // Each vertex has: 3 floats position + 2 floats UV + 3 floats color = 8 floats total
        // We need 24 vertices (4 per face * 6 faces) because each face needs unique UV coords
        float[] vertices = {
            // Position (x,y,z)              // UV (u,v)  // Color (r,g,b)

            // Front face (+Z)
            pos.X - 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  0.0f, 0.0f,  r, g, b,  // 0
            pos.X + 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  1.0f, 0.0f,  r, g, b,  // 1
            pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  1.0f, 1.0f,  r, g, b,  // 2
            pos.X - 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  0.0f, 1.0f,  r, g, b,  // 3

            // Back face (-Z)
            pos.X + 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  0.0f, 0.0f,  r, g, b,  // 4
            pos.X - 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  1.0f, 0.0f,  r, g, b,  // 5
            pos.X - 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  1.0f, 1.0f,  r, g, b,  // 6
            pos.X + 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  0.0f, 1.0f,  r, g, b,  // 7

            // Right face (+X)
            pos.X + 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  0.0f, 0.0f,  r, g, b,  // 8
            pos.X + 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  1.0f, 0.0f,  r, g, b,  // 9
            pos.X + 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  1.0f, 1.0f,  r, g, b,  // 10
            pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  0.0f, 1.0f,  r, g, b,  // 11

            // Left face (-X)
            pos.X - 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  0.0f, 0.0f,  r, g, b,  // 12
            pos.X - 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  1.0f, 0.0f,  r, g, b,  // 13
            pos.X - 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  1.0f, 1.0f,  r, g, b,  // 14
            pos.X - 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  0.0f, 1.0f,  r, g, b,  // 15

            // Top face (+Y)
            pos.X - 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  0.0f, 0.0f,  r, g, b,  // 16
            pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  1.0f, 0.0f,  r, g, b,  // 17
            pos.X + 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  1.0f, 1.0f,  r, g, b,  // 18
            pos.X - 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  0.0f, 1.0f,  r, g, b,  // 19

            // Bottom face (-Y)
            pos.X - 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  0.0f, 0.0f,  r, g, b,  // 20
            pos.X + 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  1.0f, 0.0f,  r, g, b,  // 21
            pos.X + 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  1.0f, 1.0f,  r, g, b,  // 22
            pos.X - 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  0.0f, 1.0f,  r, g, b,  // 23
        };

        // Define triangles using vertex indices (6 faces * 2 triangles * 3 vertices)
        uint[] indices = {
            0, 1, 2,   2, 3, 0,      // Front
            4, 5, 6,   6, 7, 4,      // Back
            8, 9, 10,  10, 11, 8,    // Right
            12, 13, 14, 14, 15, 12,  // Left
            16, 17, 18, 18, 19, 16,  // Top
            20, 21, 22, 22, 23, 20   // Bottom
        };

        _indexCount = indices.Length;

        // Create and bind VAO (stores the vertex attribute configuration)
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        // Create and upload VBO (vertex data)
        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float),
                      vertices, BufferUsageHint.StaticDraw);

        // Create and upload EBO (index data)
        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint),
                      indices, BufferUsageHint.StaticDraw);

        // Configure vertex attributes (stride is now 8 floats per vertex)
        int stride = 8 * sizeof(float);

        // Position attribute (location = 0, 3 floats, offset = 0)
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);

        // UV attribute (location = 1, 2 floats, offset = 3 floats)
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // Color attribute (location = 2, 3 floats, offset = 5 floats)
        GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, 5 * sizeof(float));
        GL.EnableVertexAttribArray(2);

        // Unbind (not strictly necessary, but good practice)
        GL.BindVertexArray(0);
    }

    /// <summary>
    /// Renders the cube
    /// </summary>
    public void Draw()
    {
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
