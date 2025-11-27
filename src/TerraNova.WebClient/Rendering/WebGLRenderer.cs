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

    public Task InitializeAsync()
    {
        // WebGL context already initialized via JS terraNova.init()
        // Shaders will be loaded in Phase 1.6
        return Task.CompletedTask;
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

    public async ValueTask DisposeAsync()
    {
        await _jsRuntime.InvokeVoidAsync("terraNova.cleanup");
    }
}
