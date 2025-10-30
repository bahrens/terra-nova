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

        // Define the 8 corners of a unit cube centered at 'pos'
        // Each vertex has: 3 floats for position + 3 floats for color = 6 floats total
        float[] vertices = {
            // Position (x,y,z)              // Color (r,g,b)
            // Front face (z = 0.5)
            pos.X - 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  r, g, b,  // 0: Bottom-left
            pos.X + 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  r, g, b,  // 1: Bottom-right
            pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  r, g, b,  // 2: Top-right
            pos.X - 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  r, g, b,  // 3: Top-left

            // Back face (z = -0.5)
            pos.X - 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  r, g, b,  // 4: Bottom-left
            pos.X + 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  r, g, b,  // 5: Bottom-right
            pos.X + 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  r, g, b,  // 6: Top-right
            pos.X - 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  r, g, b,  // 7: Top-left
        };

        // Define triangles using vertex indices
        // Each face = 2 triangles = 6 indices
        // Vertices are ordered counter-clockwise when viewed from outside
        uint[] indices = {
            // Front face
            0, 1, 2,  2, 3, 0,
            // Right face
            1, 5, 6,  6, 2, 1,
            // Back face
            5, 4, 7,  7, 6, 5,
            // Left face
            4, 0, 3,  3, 7, 4,
            // Top face
            3, 2, 6,  6, 7, 3,
            // Bottom face
            4, 5, 1,  1, 0, 4
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

        // Configure vertex attributes
        // Position attribute (location = 0, 3 floats, stride = 6 floats, offset = 0)
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false,
                               6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // Color attribute (location = 1, 3 floats, stride = 6 floats, offset = 3 floats)
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false,
                               6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

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
