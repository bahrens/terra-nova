using TerraNova.Shared;

namespace TerraNova.Core;

/// <summary>
/// Platform-agnostic interface for rendering the voxel world.
/// Abstracts rendering operations from specific graphics APIs (OpenGL, WebGL, Vulkan, etc.).
/// </summary>
public interface IRenderer : IDisposable
{
    /// <summary>
    /// Initialize graphics resources (must be called after graphics context is created)
    /// </summary>
    void Initialize();

    /// <summary>
    /// Set the camera reference for view matrix calculations.
    /// </summary>
    /// <param name="cameraView">Camera view interface providing position and direction</param>
    void SetCamera(ICameraView cameraView);

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

    /// <summary>
    /// Update renderer state per frame (e.g., chunk cleanup, animations)
    /// </summary>
    void Update(double deltaTime);

    /// <summary>
    /// Render the scene to the viewport
    /// </summary>
    void Render(int viewportWidth, int viewportHeight);
}
