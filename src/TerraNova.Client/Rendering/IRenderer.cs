using TerraNova.Client.Math;

namespace TerraNova.Client.Rendering;

public interface IRenderer : IDisposable
{
    /// <summary>
    /// Initialize renderer and create GPU resources.
    /// Called once after construction, before first Render().
    /// Platform-specific async preparation must be complete before calling this.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Set active camera for rendering.
    /// Must be called before first Render(). Can be called every frame for moving cameras.
    /// </summary>
    void SetCamera(ICameraView cameraView);

    /// <summary>
    /// Upload or update mesh data for a chunk. Creates GPU buffers.
    /// </summary>
    void UpdateChunk(Vector2i chunkPosition, ChunkMeshData meshData);

    /// <summary>
    /// Remove chunk and free associated GPU resources.
    /// </summary>
    void RemoveChunk(Vector2i chunkPosition);

    /// <summary>
    /// Render all visible chunks to the current framebuffer.
    /// </summary>
    void Render(ViewportInfo viewport);

    /// <summary>
    /// Update renderer-internal state (e.g., animated water, particle effects).
    /// Called once per frame before Render().
    /// </summary>
    void Update(double deltaTime);

    /// <summary>
    /// Handle viewport dimension changes (update projection matrices, viewport, etc.).
    /// </summary>
    void Resize(ViewportInfo viewport);
}
