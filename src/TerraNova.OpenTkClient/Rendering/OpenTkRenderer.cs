using OpenTK.Graphics.OpenGL4;
using TerraNova.Client.Math;
using TerraNova.Client.Rendering;

namespace TerraNova.OpenTkClient.Rendering;

public class OpenTkRenderer : IRenderer
{
    public Task InitializeAsync()
    {
        GL.ClearColor(0.2f, 0.4f, 0.8f, 1.0f);

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);

        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);

        return Task.CompletedTask;
    }

    public void RemoveChunk(Vector2i chunkPosition)
    {
        // TODO: Implement chunk removal logic
    }

    public void Render(ViewportInfo viewport)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void SetCamera(ICameraView cameraView)
    {
        // TODO: Implement camera setting logic
    }

    public void Update(double deltaTime)
    {
        // TODO: Implement update logic
    }

    public void UpdateChunk(Vector2i chunkPosition, ChunkMeshData meshData)
    {
        // TODO: Implement chunk update logic
    }

    public void Resize(ViewportInfo viewport)
    {
        GL.Viewport(0, 0, viewport.Width, viewport.Height);
    }

    public ValueTask DisposeAsync()
    {
        // TODO: Delete shader programs, buffers, textures when implemented
        return ValueTask.CompletedTask;
    }
}
