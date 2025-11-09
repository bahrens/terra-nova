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
        var brightness = new List<float>();
        var indices = new List<uint>();
        uint vertexCount = 0;

        // Iterate through all blocks in this chunk
        for (int x = 0; x < Chunk.ChunkSize; x++)
        {
            for (int y = 0; y < Chunk.WorldHeight; y++)
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
                        world,
                        vertices, colors, texCoords, brightness, indices,
                        vertexCount);
                }
            }
        }

        return new ChunkMeshData(
            vertices.ToArray(),
            colors.ToArray(),
            texCoords.ToArray(),
            brightness.ToArray(),
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
        var brightness = new List<float>();
        var indices = new List<uint>();
        uint vertexCount = 0;

        var color = BlockHelper.GetBlockColor(blockType);

        // No world context for single block, use simple directional lighting
        vertexCount = AddBlockFaces(
            blockPos.X, blockPos.Y, blockPos.Z,
            color.r, color.g, color.b,
            visibleFaces,
            null, // No world context for highlighted block
            vertices, colors, texCoords, brightness, indices,
            vertexCount);

        return new ChunkMeshData(
            vertices.ToArray(),
            colors.ToArray(),
            texCoords.ToArray(),
            brightness.ToArray(),
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
        World? world,
        List<float> vertices, List<float> colors, List<float> texCoords, List<float> brightness, List<uint> indices,
        uint vertexCount)
    {
        // Helper to add a face's vertices and indices
        uint AddFace(float[] positions, float[] uvs, float faceBrightness, uint currentVertexCount)
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

            // Add brightness (1 float per vertex, 4 vertices per face = 4 floats)
            for (int i = 0; i < 4; i++)
            {
                brightness.Add(faceBrightness);
            }

            // Add indices (2 triangles per face = 6 indices)
            indices.AddRange(new uint[] {
                currentVertexCount + 0, currentVertexCount + 1, currentVertexCount + 2,
                currentVertexCount + 2, currentVertexCount + 3, currentVertexCount + 0
            });

            return currentVertexCount + 4;
        }

        // Front face (+Z) - Side face
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
                RenderSettings.Lighting.SideFaceBrightness,
                vertexCount
            );
        }

        // Back face (-Z) - Side face
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
                RenderSettings.Lighting.SideFaceBrightness,
                vertexCount
            );
        }

        // Right face (+X) - Side face
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
                RenderSettings.Lighting.SideFaceBrightness,
                vertexCount
            );
        }

        // Left face (-X) - Side face
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
                RenderSettings.Lighting.SideFaceBrightness,
                vertexCount
            );
        }

        // Top face (+Y) - Brightest (facing sun)
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
                RenderSettings.Lighting.TopFaceBrightness,
                vertexCount
            );
        }

        // Bottom face (-Y) - Darkest (no direct light)
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
                RenderSettings.Lighting.BottomFaceBrightness,
                vertexCount
            );
        }

        return vertexCount;
    }
}
