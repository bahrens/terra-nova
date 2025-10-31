using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using TerraNova.Shared;

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

    public CubeMesh(Vector3 position, BlockType blockType, BlockFaces visibleFaces = BlockFaces.All)
    {
        GenerateCube(position, blockType, visibleFaces);
    }

    /// <summary>
    /// Generates vertex data for a cube at the given position, only including visible faces
    /// </summary>
    private void GenerateCube(Vector3 pos, BlockType blockType, BlockFaces visibleFaces)
    {
        var color = BlockHelper.GetBlockColor(blockType);
        float r = color.r, g = color.g, b = color.b;

        // Dynamically build vertex and index lists based on visible faces
        var vertexList = new List<float>();
        var indexList = new List<uint>();
        uint vertexCount = 0;

        // Helper to add a face's vertices and indices
        void AddFace(float[] faceVertices)
        {
            // Add vertices (each face has 4 vertices * 8 floats = 32 floats)
            foreach (float v in faceVertices)
            {
                vertexList.Add(v);
            }

            // Add indices (2 triangles per face)
            indexList.AddRange(new uint[] {
                vertexCount + 0, vertexCount + 1, vertexCount + 2,
                vertexCount + 2, vertexCount + 3, vertexCount + 0
            });

            vertexCount += 4;
        }

        // Front face (+Z)
        if ((visibleFaces & BlockFaces.Front) != 0)
        {
            AddFace(new float[] {
                pos.X - 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  0.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  1.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  1.0f, 1.0f,  r, g, b,
                pos.X - 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  0.0f, 1.0f,  r, g, b,
            });
        }

        // Back face (-Z)
        if ((visibleFaces & BlockFaces.Back) != 0)
        {
            AddFace(new float[] {
                pos.X + 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  0.0f, 0.0f,  r, g, b,
                pos.X - 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  1.0f, 0.0f,  r, g, b,
                pos.X - 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  1.0f, 1.0f,  r, g, b,
                pos.X + 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  0.0f, 1.0f,  r, g, b,
            });
        }

        // Right face (+X)
        if ((visibleFaces & BlockFaces.Right) != 0)
        {
            AddFace(new float[] {
                pos.X + 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  0.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  1.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  1.0f, 1.0f,  r, g, b,
                pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  0.0f, 1.0f,  r, g, b,
            });
        }

        // Left face (-X)
        if ((visibleFaces & BlockFaces.Left) != 0)
        {
            AddFace(new float[] {
                pos.X - 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  0.0f, 0.0f,  r, g, b,
                pos.X - 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  1.0f, 0.0f,  r, g, b,
                pos.X - 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  1.0f, 1.0f,  r, g, b,
                pos.X - 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  0.0f, 1.0f,  r, g, b,
            });
        }

        // Top face (+Y)
        if ((visibleFaces & BlockFaces.Top) != 0)
        {
            AddFace(new float[] {
                pos.X - 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  0.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  1.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  1.0f, 1.0f,  r, g, b,
                pos.X - 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  0.0f, 1.0f,  r, g, b,
            });
        }

        // Bottom face (-Y)
        if ((visibleFaces & BlockFaces.Bottom) != 0)
        {
            AddFace(new float[] {
                pos.X - 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  0.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  1.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  1.0f, 1.0f,  r, g, b,
                pos.X - 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  0.0f, 1.0f,  r, g, b,
            });
        }

        // Convert lists to arrays
        float[] vertices = vertexList.ToArray();
        uint[] indices = indexList.ToArray();

        _indexCount = indices.Length;

        // Early exit if no faces to render
        if (_indexCount == 0)
        {
            return;
        }

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
        if (_indexCount == 0) return; // Nothing to draw

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
