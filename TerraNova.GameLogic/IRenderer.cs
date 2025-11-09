using TerraNova.Shared;

namespace TerraNova.GameLogic;

/// <summary>
/// Platform-agnostic interface for rendering the voxel world.
/// Implemented by OpenTKRenderer (desktop) and ThreeJsRenderer (web).
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Update or create a chunk mesh at the specified 2D position (X, Z only)
    /// </summary>
    Task UpdateChunk(Vector2i chunkPos, ChunkMeshData meshData);

    /// <summary>
    /// Remove a chunk mesh from the scene at the specified 2D position (X, Z only)
    /// </summary>
    void RemoveChunk(Vector2i chunkPos);

    /// <summary>
    /// Highlight a block at the specified position (for selection)
    /// </summary>
    void HighlightBlock(Vector3i blockPos, bool highlight);
}
