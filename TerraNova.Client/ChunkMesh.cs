using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using TerraNova.Shared;

namespace TerraNova;

/// <summary>
/// Generates and manages a single mesh for an entire chunk (16x16x16 blocks)
/// This is the key optimization: one draw call per chunk instead of per block
/// </summary>
public class ChunkMesh : IDisposable
{
    private int _vao; // Vertex Array Object
    private int _vbo; // Vertex Buffer Object (stores vertex data)
    private int _ebo; // Element Buffer Object (stores indices)
    private int _indexCount;
    private bool _disposed = false;

    public Chunk Chunk { get; private set; }

    public ChunkMesh(Chunk chunk, World world)
    {
        Chunk = chunk;
        GenerateMesh(world);
    }

    /// <summary>
    /// Generates a single combined mesh for all blocks in the chunk
    /// </summary>
    private void GenerateMesh(World world)
    {
        var vertexList = new List<float>();
        var indexList = new List<uint>();
        uint vertexCount = 0;

        // Iterate through all blocks in this chunk
        for (int x = 0; x < Chunk.ChunkSize; x++)
        {
            for (int y = 0; y < Chunk.ChunkSize; y++)
            {
                for (int z = 0; z < Chunk.ChunkSize; z++)
                {
                    BlockType blockType = Chunk.GetBlock(x, y, z);
                    if (blockType == BlockType.Air)
                        continue;

                    // Get world position for this block
                    TerraNova.Shared.Vector3i worldPos = Chunk.GetWorldPosition(x, y, z);
                    Vector3 pos = new Vector3(worldPos.X, worldPos.Y, worldPos.Z);

                    // Get block color
                    var color = BlockHelper.GetBlockColor(blockType);
                    float r = color.r, g = color.g, b = color.b;

                    // Determine which faces are visible
                    BlockFaces visibleFaces = world.GetVisibleFaces(worldPos.X, worldPos.Y, worldPos.Z);

                    // Add faces to the combined mesh
                    AddBlockFaces(pos, r, g, b, visibleFaces, vertexList, indexList, ref vertexCount);
                }
            }
        }

        // Convert lists to arrays
        float[] vertices = vertexList.ToArray();
        uint[] indices = indexList.ToArray();

        _indexCount = indices.Length;

        // Early exit if no faces to render (empty chunk)
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

        // Configure vertex attributes (stride is 8 floats per vertex)
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

        // Unbind
        GL.BindVertexArray(0);
    }

    /// <summary>
    /// Adds all visible faces of a single block to the mesh
    /// </summary>
    private void AddBlockFaces(Vector3 pos, float r, float g, float b, BlockFaces visibleFaces,
                               List<float> vertexList, List<uint> indexList, ref uint vertexCount)
    {
        // Helper to add a face's vertices and indices
        void AddFace(float[] faceVertices, ref uint currentVertexCount)
        {
            // Add vertices (each face has 4 vertices * 8 floats = 32 floats)
            foreach (float v in faceVertices)
            {
                vertexList.Add(v);
            }

            // Add indices (2 triangles per face)
            indexList.AddRange(new uint[] {
                currentVertexCount + 0, currentVertexCount + 1, currentVertexCount + 2,
                currentVertexCount + 2, currentVertexCount + 3, currentVertexCount + 0
            });

            currentVertexCount += 4;
        }

        // Front face (+Z)
        if ((visibleFaces & BlockFaces.Front) != 0)
        {
            AddFace(new float[] {
                pos.X - 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  0.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  1.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  1.0f, 1.0f,  r, g, b,
                pos.X - 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  0.0f, 1.0f,  r, g, b,
            }, ref vertexCount);
        }

        // Back face (-Z)
        if ((visibleFaces & BlockFaces.Back) != 0)
        {
            AddFace(new float[] {
                pos.X + 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  0.0f, 0.0f,  r, g, b,
                pos.X - 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  1.0f, 0.0f,  r, g, b,
                pos.X - 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  1.0f, 1.0f,  r, g, b,
                pos.X + 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  0.0f, 1.0f,  r, g, b,
            }, ref vertexCount);
        }

        // Right face (+X)
        if ((visibleFaces & BlockFaces.Right) != 0)
        {
            AddFace(new float[] {
                pos.X + 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  0.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  1.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  1.0f, 1.0f,  r, g, b,
                pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  0.0f, 1.0f,  r, g, b,
            }, ref vertexCount);
        }

        // Left face (-X)
        if ((visibleFaces & BlockFaces.Left) != 0)
        {
            AddFace(new float[] {
                pos.X - 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  0.0f, 0.0f,  r, g, b,
                pos.X - 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  1.0f, 0.0f,  r, g, b,
                pos.X - 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  1.0f, 1.0f,  r, g, b,
                pos.X - 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  0.0f, 1.0f,  r, g, b,
            }, ref vertexCount);
        }

        // Top face (+Y)
        if ((visibleFaces & BlockFaces.Top) != 0)
        {
            AddFace(new float[] {
                pos.X - 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  0.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f,  1.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  1.0f, 1.0f,  r, g, b,
                pos.X - 0.5f, pos.Y + 0.5f, pos.Z - 0.5f,  0.0f, 1.0f,  r, g, b,
            }, ref vertexCount);
        }

        // Bottom face (-Y)
        if ((visibleFaces & BlockFaces.Bottom) != 0)
        {
            AddFace(new float[] {
                pos.X - 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  0.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y - 0.5f, pos.Z - 0.5f,  1.0f, 0.0f,  r, g, b,
                pos.X + 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  1.0f, 1.0f,  r, g, b,
                pos.X - 0.5f, pos.Y - 0.5f, pos.Z + 0.5f,  0.0f, 1.0f,  r, g, b,
            }, ref vertexCount);
        }
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
