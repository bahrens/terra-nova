using TerraNova.Client.Math;

namespace TerraNova.Client.Rendering;

public interface IRenderer
{
    void Initialize();
    void SetCamera(ICameraView cameraView);
    void UpdateChunk(Vector2i chunkPosition, ChunkMeshData meshData);
    void RemoveChunk(Vector2i chunkPosition);
    void Render(ViewportInfo viewport);
    void Update(double deltaTime);
}
