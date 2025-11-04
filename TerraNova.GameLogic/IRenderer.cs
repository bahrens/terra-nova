using TerraNova.Shared;

namespace TerraNova.GameLogic;

/// <summary>
/// Platform-agnostic interface for rendering the voxel world.
/// Implemented by OpenTKRenderer (desktop) and ThreeJsRenderer (web).
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Update or create a chunk mesh at the specified position
    /// </summary>
    Task UpdateChunk(Vector3i chunkPos, ChunkMeshData meshData);

    /// <summary>
    /// Remove a chunk mesh from the scene
    /// </summary>
    void RemoveChunk(Vector3i chunkPos);

    /// <summary>
    /// Update the camera position and rotation
    /// </summary>
    void SetCamera(Vector3 position, Vector3 rotation);

    /// <summary>
    /// Highlight a block at the specified position (for selection)
    /// </summary>
    void HighlightBlock(Vector3i blockPos, bool highlight);
}
