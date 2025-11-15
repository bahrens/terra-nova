using TerraNova.Shared;

namespace TerraNova.Core;

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
        // Helper to calculate ambient occlusion for a vertex based on neighboring blocks
        // Returns AO factor (0.0 = fully occluded/darkest, 1.0 = no occlusion/brightest)
        float CalculateAO(bool side1, bool side2, bool corner)
        {
            // Count ALL solid neighbors (sides and corner)
            int solidCount = (side1 ? 1 : 0) + (side2 ? 1 : 0) + (corner ? 1 : 0);

            // Calculate occlusion: fewer solid blocks = brighter
            // 0 solid blocks = 3/3 = 1.0 (no occlusion)
            // 1 solid block  = 2/3 = 0.666
            // 2 solid blocks = 1/3 = 0.333
            // 3 solid blocks = 0/3 = 0.0 (fully occluded)
            return (3 - solidCount) / 3.0f;
        }

        // Helper to add a face's vertices and indices
        uint AddFace(float[] positions, float[] uvs, float[] vertexBrightness, uint currentVertexCount)
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

            // Add per-vertex brightness (1 float per vertex, 4 vertices per face = 4 floats)
            for (int i = 0; i < 4; i++)
            {
                brightness.Add(vertexBrightness[i]);
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
            float baseBrightness = RenderSettings.Lighting.SideFaceBrightness;
            int x = (int)posX;
            int y = (int)posY;
            int z = (int)posZ;

            float[] vertexBrightness = new float[4];
            if (world != null)
            {
                // Vertex 0: bottom-left (-X,-Y,+Z)
                bool v0_left = world.GetBlock(x - 1, y, z + 1) != BlockType.Air;
                bool v0_bottom = world.GetBlock(x, y - 1, z + 1) != BlockType.Air;
                bool v0_corner = world.GetBlock(x - 1, y - 1, z + 1) != BlockType.Air;
                float ao0 = CalculateAO(v0_left, v0_bottom, v0_corner);
                vertexBrightness[0] = baseBrightness * (1.0f - (1.0f - ao0) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 1: bottom-right (+X,-Y,+Z)
                bool v1_right = world.GetBlock(x + 1, y, z + 1) != BlockType.Air;
                bool v1_bottom = world.GetBlock(x, y - 1, z + 1) != BlockType.Air;
                bool v1_corner = world.GetBlock(x + 1, y - 1, z + 1) != BlockType.Air;
                float ao1 = CalculateAO(v1_right, v1_bottom, v1_corner);
                vertexBrightness[1] = baseBrightness * (1.0f - (1.0f - ao1) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 2: top-right (+X,+Y,+Z)
                bool v2_right = world.GetBlock(x + 1, y, z + 1) != BlockType.Air;
                bool v2_top = world.GetBlock(x, y + 1, z + 1) != BlockType.Air;
                bool v2_corner = world.GetBlock(x + 1, y + 1, z + 1) != BlockType.Air;
                float ao2 = CalculateAO(v2_right, v2_top, v2_corner);
                vertexBrightness[2] = baseBrightness * (1.0f - (1.0f - ao2) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 3: top-left (-X,+Y,+Z)
                bool v3_left = world.GetBlock(x - 1, y, z + 1) != BlockType.Air;
                bool v3_top = world.GetBlock(x, y + 1, z + 1) != BlockType.Air;
                bool v3_corner = world.GetBlock(x - 1, y + 1, z + 1) != BlockType.Air;
                float ao3 = CalculateAO(v3_left, v3_top, v3_corner);
                vertexBrightness[3] = baseBrightness * (1.0f - (1.0f - ao3) * RenderSettings.Lighting.AmbientOcclusionStrength);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    vertexBrightness[i] = baseBrightness;
            }

            vertexCount = AddFace(
                new float[] {
                    posX - 0.5f, posY - 0.5f, posZ + 0.5f,
                    posX + 0.5f, posY - 0.5f, posZ + 0.5f,
                    posX + 0.5f, posY + 0.5f, posZ + 0.5f,
                    posX - 0.5f, posY + 0.5f, posZ + 0.5f,
                },
                new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f },
                vertexBrightness,
                vertexCount
            );
        }

        // Back face (-Z) - Side face
        if ((visibleFaces & BlockFaces.Back) != 0)
        {
            float baseBrightness = RenderSettings.Lighting.SideFaceBrightness;
            int x = (int)posX;
            int y = (int)posY;
            int z = (int)posZ;

            float[] vertexBrightness = new float[4];
            if (world != null)
            {
                // Vertex 0: bottom-right (+X,-Y,-Z)
                bool v0_right = world.GetBlock(x + 1, y, z - 1) != BlockType.Air;
                bool v0_bottom = world.GetBlock(x, y - 1, z - 1) != BlockType.Air;
                bool v0_corner = world.GetBlock(x + 1, y - 1, z - 1) != BlockType.Air;
                float ao0 = CalculateAO(v0_right, v0_bottom, v0_corner);
                vertexBrightness[0] = baseBrightness * (1.0f - (1.0f - ao0) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 1: bottom-left (-X,-Y,-Z)
                bool v1_left = world.GetBlock(x - 1, y, z - 1) != BlockType.Air;
                bool v1_bottom = world.GetBlock(x, y - 1, z - 1) != BlockType.Air;
                bool v1_corner = world.GetBlock(x - 1, y - 1, z - 1) != BlockType.Air;
                float ao1 = CalculateAO(v1_left, v1_bottom, v1_corner);
                vertexBrightness[1] = baseBrightness * (1.0f - (1.0f - ao1) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 2: top-left (-X,+Y,-Z)
                bool v2_left = world.GetBlock(x - 1, y, z - 1) != BlockType.Air;
                bool v2_top = world.GetBlock(x, y + 1, z - 1) != BlockType.Air;
                bool v2_corner = world.GetBlock(x - 1, y + 1, z - 1) != BlockType.Air;
                float ao2 = CalculateAO(v2_left, v2_top, v2_corner);
                vertexBrightness[2] = baseBrightness * (1.0f - (1.0f - ao2) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 3: top-right (+X,+Y,-Z)
                bool v3_right = world.GetBlock(x + 1, y, z - 1) != BlockType.Air;
                bool v3_top = world.GetBlock(x, y + 1, z - 1) != BlockType.Air;
                bool v3_corner = world.GetBlock(x + 1, y + 1, z - 1) != BlockType.Air;
                float ao3 = CalculateAO(v3_right, v3_top, v3_corner);
                vertexBrightness[3] = baseBrightness * (1.0f - (1.0f - ao3) * RenderSettings.Lighting.AmbientOcclusionStrength);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    vertexBrightness[i] = baseBrightness;
            }

            vertexCount = AddFace(
                new float[] {
                    posX + 0.5f, posY - 0.5f, posZ - 0.5f,
                    posX - 0.5f, posY - 0.5f, posZ - 0.5f,
                    posX - 0.5f, posY + 0.5f, posZ - 0.5f,
                    posX + 0.5f, posY + 0.5f, posZ - 0.5f,
                },
                new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f },
                vertexBrightness,
                vertexCount
            );
        }

        // Right face (+X) - Side face
        if ((visibleFaces & BlockFaces.Right) != 0)
        {
            float baseBrightness = RenderSettings.Lighting.SideFaceBrightness;
            int x = (int)posX;
            int y = (int)posY;
            int z = (int)posZ;

            float[] vertexBrightness = new float[4];
            if (world != null)
            {
                // Vertex 0: bottom-front (+X,-Y,+Z)
                bool v0_bottom = world.GetBlock(x + 1, y - 1, z) != BlockType.Air;
                bool v0_front = world.GetBlock(x + 1, y, z + 1) != BlockType.Air;
                bool v0_corner = world.GetBlock(x + 1, y - 1, z + 1) != BlockType.Air;
                float ao0 = CalculateAO(v0_bottom, v0_front, v0_corner);
                vertexBrightness[0] = baseBrightness * (1.0f - (1.0f - ao0) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 1: bottom-back (+X,-Y,-Z)
                bool v1_bottom = world.GetBlock(x + 1, y - 1, z) != BlockType.Air;
                bool v1_back = world.GetBlock(x + 1, y, z - 1) != BlockType.Air;
                bool v1_corner = world.GetBlock(x + 1, y - 1, z - 1) != BlockType.Air;
                float ao1 = CalculateAO(v1_bottom, v1_back, v1_corner);
                vertexBrightness[1] = baseBrightness * (1.0f - (1.0f - ao1) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 2: top-back (+X,+Y,-Z)
                bool v2_top = world.GetBlock(x + 1, y + 1, z) != BlockType.Air;
                bool v2_back = world.GetBlock(x + 1, y, z - 1) != BlockType.Air;
                bool v2_corner = world.GetBlock(x + 1, y + 1, z - 1) != BlockType.Air;
                float ao2 = CalculateAO(v2_top, v2_back, v2_corner);
                vertexBrightness[2] = baseBrightness * (1.0f - (1.0f - ao2) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 3: top-front (+X,+Y,+Z)
                bool v3_top = world.GetBlock(x + 1, y + 1, z) != BlockType.Air;
                bool v3_front = world.GetBlock(x + 1, y, z + 1) != BlockType.Air;
                bool v3_corner = world.GetBlock(x + 1, y + 1, z + 1) != BlockType.Air;
                float ao3 = CalculateAO(v3_top, v3_front, v3_corner);
                vertexBrightness[3] = baseBrightness * (1.0f - (1.0f - ao3) * RenderSettings.Lighting.AmbientOcclusionStrength);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    vertexBrightness[i] = baseBrightness;
            }

            vertexCount = AddFace(
                new float[] {
                    posX + 0.5f, posY - 0.5f, posZ + 0.5f,
                    posX + 0.5f, posY - 0.5f, posZ - 0.5f,
                    posX + 0.5f, posY + 0.5f, posZ - 0.5f,
                    posX + 0.5f, posY + 0.5f, posZ + 0.5f,
                },
                new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f },
                vertexBrightness,
                vertexCount
            );
        }

        // Left face (-X) - Side face
        if ((visibleFaces & BlockFaces.Left) != 0)
        {
            float baseBrightness = RenderSettings.Lighting.SideFaceBrightness;
            int x = (int)posX;
            int y = (int)posY;
            int z = (int)posZ;

            float[] vertexBrightness = new float[4];
            if (world != null)
            {
                // Vertex 0: bottom-back (-X,-Y,-Z)
                bool v0_bottom = world.GetBlock(x - 1, y - 1, z) != BlockType.Air;
                bool v0_back = world.GetBlock(x - 1, y, z - 1) != BlockType.Air;
                bool v0_corner = world.GetBlock(x - 1, y - 1, z - 1) != BlockType.Air;
                float ao0 = CalculateAO(v0_bottom, v0_back, v0_corner);
                vertexBrightness[0] = baseBrightness * (1.0f - (1.0f - ao0) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 1: bottom-front (-X,-Y,+Z)
                bool v1_bottom = world.GetBlock(x - 1, y - 1, z) != BlockType.Air;
                bool v1_front = world.GetBlock(x - 1, y, z + 1) != BlockType.Air;
                bool v1_corner = world.GetBlock(x - 1, y - 1, z + 1) != BlockType.Air;
                float ao1 = CalculateAO(v1_bottom, v1_front, v1_corner);
                vertexBrightness[1] = baseBrightness * (1.0f - (1.0f - ao1) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 2: top-front (-X,+Y,+Z)
                bool v2_top = world.GetBlock(x - 1, y + 1, z) != BlockType.Air;
                bool v2_front = world.GetBlock(x - 1, y, z + 1) != BlockType.Air;
                bool v2_corner = world.GetBlock(x - 1, y + 1, z + 1) != BlockType.Air;
                float ao2 = CalculateAO(v2_top, v2_front, v2_corner);
                vertexBrightness[2] = baseBrightness * (1.0f - (1.0f - ao2) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 3: top-back (-X,+Y,-Z)
                bool v3_top = world.GetBlock(x - 1, y + 1, z) != BlockType.Air;
                bool v3_back = world.GetBlock(x - 1, y, z - 1) != BlockType.Air;
                bool v3_corner = world.GetBlock(x - 1, y + 1, z - 1) != BlockType.Air;
                float ao3 = CalculateAO(v3_top, v3_back, v3_corner);
                vertexBrightness[3] = baseBrightness * (1.0f - (1.0f - ao3) * RenderSettings.Lighting.AmbientOcclusionStrength);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    vertexBrightness[i] = baseBrightness;
            }

            vertexCount = AddFace(
                new float[] {
                    posX - 0.5f, posY - 0.5f, posZ - 0.5f,
                    posX - 0.5f, posY - 0.5f, posZ + 0.5f,
                    posX - 0.5f, posY + 0.5f, posZ + 0.5f,
                    posX - 0.5f, posY + 0.5f, posZ - 0.5f,
                },
                new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f },
                vertexBrightness,
                vertexCount
            );
        }

        // Top face (+Y) - Brightest (facing sun)
        if ((visibleFaces & BlockFaces.Top) != 0)
        {
            float baseBrightness = RenderSettings.Lighting.TopFaceBrightness;
            int x = (int)posX;
            int y = (int)posY;
            int z = (int)posZ;

            // Calculate AO for each vertex if world context is available
            // For top face, check blocks at Y+1 (above) that cast shadows down
            float[] vertexBrightness = new float[4];
            if (world != null)
            {
                // Vertex 0: bottom-left (-X, +Z) - check blocks above that cast shadows
                bool v0_left = world.GetBlock(x - 1, y + 1, z) != BlockType.Air;
                bool v0_front = world.GetBlock(x, y + 1, z + 1) != BlockType.Air;
                bool v0_corner = world.GetBlock(x - 1, y + 1, z + 1) != BlockType.Air;
                float ao0 = CalculateAO(v0_left, v0_front, v0_corner);
                vertexBrightness[0] = baseBrightness * (1.0f - (1.0f - ao0) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 1: bottom-right (+X, +Z)
                bool v1_right = world.GetBlock(x + 1, y + 1, z) != BlockType.Air;
                bool v1_front = world.GetBlock(x, y + 1, z + 1) != BlockType.Air;
                bool v1_corner = world.GetBlock(x + 1, y + 1, z + 1) != BlockType.Air;
                float ao1 = CalculateAO(v1_right, v1_front, v1_corner);
                vertexBrightness[1] = baseBrightness * (1.0f - (1.0f - ao1) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 2: top-right (+X, -Z)
                bool v2_right = world.GetBlock(x + 1, y + 1, z) != BlockType.Air;
                bool v2_back = world.GetBlock(x, y + 1, z - 1) != BlockType.Air;
                bool v2_corner = world.GetBlock(x + 1, y + 1, z - 1) != BlockType.Air;
                float ao2 = CalculateAO(v2_right, v2_back, v2_corner);
                vertexBrightness[2] = baseBrightness * (1.0f - (1.0f - ao2) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 3: top-left (-X, -Z)
                bool v3_left = world.GetBlock(x - 1, y + 1, z) != BlockType.Air;
                bool v3_back = world.GetBlock(x, y + 1, z - 1) != BlockType.Air;
                bool v3_corner = world.GetBlock(x - 1, y + 1, z - 1) != BlockType.Air;
                float ao3 = CalculateAO(v3_left, v3_back, v3_corner);
                vertexBrightness[3] = baseBrightness * (1.0f - (1.0f - ao3) * RenderSettings.Lighting.AmbientOcclusionStrength);
            }
            else
            {
                // No world context, use uniform brightness
                for (int i = 0; i < 4; i++)
                    vertexBrightness[i] = baseBrightness;
            }

            vertexCount = AddFace(
                new float[] {
                    posX - 0.5f, posY + 0.5f, posZ + 0.5f,
                    posX + 0.5f, posY + 0.5f, posZ + 0.5f,
                    posX + 0.5f, posY + 0.5f, posZ - 0.5f,
                    posX - 0.5f, posY + 0.5f, posZ - 0.5f,
                },
                new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f },
                vertexBrightness,
                vertexCount
            );
        }

        // Bottom face (-Y) - Darkest (no direct light)
        if ((visibleFaces & BlockFaces.Bottom) != 0)
        {
            float baseBrightness = RenderSettings.Lighting.BottomFaceBrightness;
            int x = (int)posX;
            int y = (int)posY;
            int z = (int)posZ;

            float[] vertexBrightness = new float[4];
            if (world != null)
            {
                // Vertex 0: back-left (-X,-Y,-Z)
                bool v0_left = world.GetBlock(x - 1, y - 1, z) != BlockType.Air;
                bool v0_back = world.GetBlock(x, y - 1, z - 1) != BlockType.Air;
                bool v0_corner = world.GetBlock(x - 1, y - 1, z - 1) != BlockType.Air;
                float ao0 = CalculateAO(v0_left, v0_back, v0_corner);
                vertexBrightness[0] = baseBrightness * (1.0f - (1.0f - ao0) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 1: back-right (+X,-Y,-Z)
                bool v1_right = world.GetBlock(x + 1, y - 1, z) != BlockType.Air;
                bool v1_back = world.GetBlock(x, y - 1, z - 1) != BlockType.Air;
                bool v1_corner = world.GetBlock(x + 1, y - 1, z - 1) != BlockType.Air;
                float ao1 = CalculateAO(v1_right, v1_back, v1_corner);
                vertexBrightness[1] = baseBrightness * (1.0f - (1.0f - ao1) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 2: front-right (+X,-Y,+Z)
                bool v2_right = world.GetBlock(x + 1, y - 1, z) != BlockType.Air;
                bool v2_front = world.GetBlock(x, y - 1, z + 1) != BlockType.Air;
                bool v2_corner = world.GetBlock(x + 1, y - 1, z + 1) != BlockType.Air;
                float ao2 = CalculateAO(v2_right, v2_front, v2_corner);
                vertexBrightness[2] = baseBrightness * (1.0f - (1.0f - ao2) * RenderSettings.Lighting.AmbientOcclusionStrength);

                // Vertex 3: front-left (-X,-Y,+Z)
                bool v3_left = world.GetBlock(x - 1, y - 1, z) != BlockType.Air;
                bool v3_front = world.GetBlock(x, y - 1, z + 1) != BlockType.Air;
                bool v3_corner = world.GetBlock(x - 1, y - 1, z + 1) != BlockType.Air;
                float ao3 = CalculateAO(v3_left, v3_front, v3_corner);
                vertexBrightness[3] = baseBrightness * (1.0f - (1.0f - ao3) * RenderSettings.Lighting.AmbientOcclusionStrength);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    vertexBrightness[i] = baseBrightness;
            }

            vertexCount = AddFace(
                new float[] {
                    posX - 0.5f, posY - 0.5f, posZ - 0.5f,
                    posX + 0.5f, posY - 0.5f, posZ - 0.5f,
                    posX + 0.5f, posY - 0.5f, posZ + 0.5f,
                    posX - 0.5f, posY - 0.5f, posZ + 0.5f,
                },
                new float[] { 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f },
                vertexBrightness,
                vertexCount
            );
        }

        return vertexCount;
    }
}
