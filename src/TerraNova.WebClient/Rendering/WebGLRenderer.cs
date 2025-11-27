using Microsoft.JSInterop;
using TerraNova.Client.Math;
using TerraNova.Client.Rendering;

namespace TerraNova.WebClient.Rendering;

public class WebGLRenderer : IRenderer
{
    private readonly IJSRuntime _jsRuntime;

    public WebGLRenderer(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Platform-specific async preparation for WebGL.
    /// Called by Game.razor BEFORE TerraNovaGame.Load().
    /// This handles async JS interop (shader compilation, buffer creation, etc.)
    /// </summary>
    public async Task PrepareAsync()
    {
        // WebGL context already initialized via JS terraNova.init()
        // Future: async shader compilation, texture loading, etc.
        await Task.CompletedTask;
    }

    public void Initialize()
    {
        // Synchronous state setup after PrepareAsync() completes
        // All async JS work is done, now just set initial state
    }

    public void SetCamera(ICameraView cameraView)
    {
        // TODO: Implement when camera is added
    }

    public void UpdateChunk(Vector2i chunkPosition, ChunkMeshData meshData)
    {
        // TODO: Implement when chunking is added
    }

    public void RemoveChunk(Vector2i chunkPosition)
    {
        // TODO: Implement when chunking is added
    }

    public void Render(ViewportInfo viewport)
    {
        // TODO: Implement when shaders are added
    }

    public void Update(double deltaTime)
    {
        // TODO: Implement for animated effects
    }

    public void Resize(ViewportInfo viewport)
    {
        // Handled by JS resizeCanvas() via resize listener
    }

    public void Dispose()
    {
        // C#-side cleanup only
        // JS cleanup (terraNova.cleanup) is handled by Game.razor.DisposeAsync()
    }
}
