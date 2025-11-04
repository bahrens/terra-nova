using TerraNova.Shared;

namespace TerraNova.GameLogic;

/// <summary>
/// Platform-agnostic game engine that manages the game loop, world state, and rendering.
/// This is the single source of truth for game logic - both desktop and web clients use this.
/// </summary>
public class GameEngine
{
    private readonly IRenderer _renderer;
    private World? _world;
    private bool _worldChanged = false;

    public GameEngine(IRenderer renderer)
    {
        _renderer = renderer;
    }

    /// <summary>
    /// Get the current world state (may be null if not loaded yet)
    /// </summary>
    public World? World => _world;

    /// <summary>
    /// Set the world data (called when receiving world from server)
    /// </summary>
    public void SetWorld(World world)
    {
        _world = world;
        _worldChanged = true;
    }

    /// <summary>
    /// Notify that a block has been updated
    /// </summary>
    public void NotifyBlockUpdate(int x, int y, int z, BlockType blockType)
    {
        if (_world != null)
        {
            _world.SetBlock(x, y, z, blockType);
            _worldChanged = true;
        }
    }

    /// <summary>
    /// Update game state (called every frame)
    /// </summary>
    public void Update(double deltaTime)
    {
        // If world changed, regenerate chunk meshes
        if (_worldChanged && _world != null)
        {
            RegenerateChunkMeshes();
            _worldChanged = false;
        }
    }

    private void RegenerateChunkMeshes()
    {
        if (_world == null) return;

        // Build and send meshes for each chunk
        foreach (var chunk in _world.GetAllChunks())
        {
            var meshData = ChunkMeshBuilder.BuildChunkMesh(chunk, _world);
            _renderer.UpdateChunk(chunk.ChunkPosition, meshData);
        }
    }
}
