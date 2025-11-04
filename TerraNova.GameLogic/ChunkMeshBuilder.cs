using TerraNova.Shared;

namespace TerraNova.GameLogic;

/// <summary>
/// Platform-agnostic chunk mesh builder.
/// Generates vertex and index data from chunk blocks without OpenGL-specific code.
/// </summary>
public static class ChunkMeshBuilder
{
    /// <summary>
    /// Builds mesh data for an entire chunk
    /// </summary>
    public static ChunkMeshData BuildChunkMesh(Chunk chunk, World world)
    {
        var vertices = new List<float>();
        var colors = new List<float>();
        var texCoords = new List<float>();
        var indices = new List<uint>();
        uint vertexCount = 0;

        // Iterate through all blocks in this chunk
        for (int x = 0; x < Chunk.ChunkSize; x++)
        {
            for (int y = 0; y < Chunk.ChunkSize; y++)
            {
                for (int z = 0; z < Chunk.ChunkSize; z++)
                {
                    BlockType blockType = chunk.GetBlock(x, y, z);
                    if (blockType == BlockType.Air)
                        continue;

                    // Get world position for this block
                    Vector3i worldPos = chunk.GetWorldPosition(x, y, z);

                    // Get block color
                    var color = BlockHelper.GetBlockColor(blockType);

                    // Determine which faces are visible
                    BlockFaces visibleFaces = world.GetVisibleFaces(worldPos.X, worldPos.Y, worldPos.Z);

                    // Add faces to the combined mesh
                    vertexCount = AddBlockFaces(
                        worldPos.X, worldPos.Y, worldPos.Z,
                        color.r, color.g, color.b,
                        visibleFaces,
                        vertices, colors, texCoords, indices,
                        vertexCount);
                }
            }
        }

        return new ChunkMeshData(
            vertices.ToArray(),
            colors.ToArray(),
            texCoords.ToArray(),
            indices.ToArray()
        );
    }

    /// <summary>
    /// Builds mesh data for a single block (used for highlighting selected blocks)
    /// </summary>
    public static ChunkMeshData BuildSingleBlockMesh(Vector3i blockPos, BlockType blockType, BlockFaces visibleFaces)
    {
        var vertices = new List<float>();
        var colors = new List<float>();
        var texCoords = new List<float>();
        var indices = new List<uint>();
        uint vertexCount = 0;

        var color = BlockHelper.GetBlockColor(blockType);

        vertexCount = AddBlockFaces(
            blockPos.X, blockPos.Y, blockPos.Z,
            color.r, color.g, color.b,
            visibleFaces,
            vertices, colors, texCoords, indices,
            vertexCount);

        return new ChunkMeshData(
            vertices.ToArray(),
            colors.ToArray(),
            texCoords.ToArray(),
            indices.ToArray()
        );
    }

    /// <summary>
    /// Adds all visible faces of a single block to the mesh lists
    /// </summary>
    private static uint AddBlockFaces(
        float posX, float posY, float posZ,
        float r, float g, float b,
        BlockFaces visibleFaces,
        List<float> vertices, List<float> colors, List<float> texCoords, List<uint> indices,
        uint vertexCount)
    {
        // Helper to add a face's vertices and indices
        uint AddFace(float[] positions, float[] uvs, uint currentVertexCount)
        {
            // Add position vertices (3 floats per vertex, 4 vertices per face = 12 floats)
            foreach (float v in positions)
            {
                vertices.Add(v);
            }

            // Add colors (3 floats per vertex, 4 vertices per face = 12 floats)
            for (int i = 0; i < 4; i++)
            {
                colors.Add(r);
                colors.Add(g);
                colors.Add(b);
            }

            // Add texture coordinates (2 floats per vertex, 4 vertices per face = 8 floats)
            foreach (float uv in uvs)
            {
                texCoords.Add(uv);
            }

            // Add indices (2 triangles per face = 6 indices)
            indices.AddRange(new uint[] {
                currentVertexCount + 0, currentVertexCount + 1, currentVertexCount + 2,
                currentVertexCount + 2, currentVertexCount + 3, currentVertexCount + 0
            });

            return currentVertexCount + 4;
        }

        // Front face (+Z)
        if ((visibleFaces & BlockFaces.Front) != 0)
        {
            vertexCount = AddFace(
                new float[] {
                    posX - 0.5f, posY - 0.5f, posZ + 0.5f,
                    posX + 0.5f, posY - 0.5f, posZ + 0.5f,
                    posX + 0.5f, posY + 0.5f, posZ + 0.5f,
                    posX - 0.5f, posY + 0.5f, posZ + 0.5f,
                },
                new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f },
                vertexCount
            );
        }

        // Back face (-Z)
        if ((visibleFaces & BlockFaces.Back) != 0)
        {
            vertexCount = AddFace(
                new float[] {
                    posX + 0.5f, posY - 0.5f, posZ - 0.5f,
                    posX - 0.5f, posY - 0.5f, posZ - 0.5f,
                    posX - 0.5f, posY + 0.5f, posZ - 0.5f,
                    posX + 0.5f, posY + 0.5f, posZ - 0.5f,
                },
                new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f },
                vertexCount
            );
        }

        // Right face (+X)
        if ((visibleFaces & BlockFaces.Right) != 0)
        {
            vertexCount = AddFace(
                new float[] {
                    posX + 0.5f, posY - 0.5f, posZ + 0.5f,
                    posX + 0.5f, posY - 0.5f, posZ - 0.5f,
                    posX + 0.5f, posY + 0.5f, posZ - 0.5f,
                    posX + 0.5f, posY + 0.5f, posZ + 0.5f,
                },
                new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f },
                vertexCount
            );
        }

        // Left face (-X)
        if ((visibleFaces & BlockFaces.Left) != 0)
        {
            vertexCount = AddFace(
                new float[] {
                    posX - 0.5f, posY - 0.5f, posZ - 0.5f,
                    posX - 0.5f, posY - 0.5f, posZ + 0.5f,
                    posX - 0.5f, posY + 0.5f, posZ + 0.5f,
                    posX - 0.5f, posY + 0.5f, posZ - 0.5f,
                },
                new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f },
                vertexCount
            );
        }

        // Top face (+Y)
        if ((visibleFaces & BlockFaces.Top) != 0)
        {
            vertexCount = AddFace(
                new float[] {
                    posX - 0.5f, posY + 0.5f, posZ + 0.5f,
                    posX + 0.5f, posY + 0.5f, posZ + 0.5f,
                    posX + 0.5f, posY + 0.5f, posZ - 0.5f,
                    posX - 0.5f, posY + 0.5f, posZ - 0.5f,
                },
                new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f },
                vertexCount
            );
        }

        // Bottom face (-Y)
        if ((visibleFaces & BlockFaces.Bottom) != 0)
        {
            vertexCount = AddFace(
                new float[] {
                    posX - 0.5f, posY - 0.5f, posZ - 0.5f,
                    posX + 0.5f, posY - 0.5f, posZ - 0.5f,
                    posX + 0.5f, posY - 0.5f, posZ + 0.5f,
                    posX - 0.5f, posY - 0.5f, posZ + 0.5f,
                },
                new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f },
                vertexCount
            );
        }

        return vertexCount;
    }
}
