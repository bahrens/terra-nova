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
    private readonly HashSet<Vector2i> _dirtyChunks = new();

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
        // Mark all chunks as dirty when world is loaded
        _dirtyChunks.Clear();
        foreach (var chunk in _world.GetAllChunks())
        {
            _dirtyChunks.Add(chunk.ChunkPosition);
        }
    }

    /// <summary>
    /// Notify that a block has been updated
    /// </summary>
    public void NotifyBlockUpdate(int x, int y, int z, BlockType blockType)
    {
        if (_world != null)
        {
            _world.SetBlock(x, y, z, blockType);

            // Mark the chunk column containing this block as dirty (2D position only)
            var chunkPos = Chunk.WorldToChunkPosition(x, z);
            _dirtyChunks.Add(chunkPos);

            // Also mark adjacent chunk columns if the block is on a chunk boundary
            // (for proper face culling at chunk edges)
            // Note: In 2D column chunks, we only mark horizontal adjacents (X and Z), not vertical
            if (x % Chunk.ChunkSize == 0)
                _dirtyChunks.Add(new Vector2i(chunkPos.X - 1, chunkPos.Z));
            if (x % Chunk.ChunkSize == Chunk.ChunkSize - 1)
                _dirtyChunks.Add(new Vector2i(chunkPos.X + 1, chunkPos.Z));

            if (z % Chunk.ChunkSize == 0)
                _dirtyChunks.Add(new Vector2i(chunkPos.X, chunkPos.Z - 1));
            if (z % Chunk.ChunkSize == Chunk.ChunkSize - 1)
                _dirtyChunks.Add(new Vector2i(chunkPos.X, chunkPos.Z + 1));
        }
    }

    /// <summary>
    /// Update game state (called every frame)
    /// </summary>
    public void Update(double deltaTime)
    {
        // If there are dirty chunks, regenerate only those chunk meshes
        if (_dirtyChunks.Count > 0 && _world != null)
        {
            RegenerateDirtyChunkMeshes();
            _dirtyChunks.Clear();
        }
    }

    private void RegenerateDirtyChunkMeshes()
    {
        if (_world == null) return;

        // Build and send meshes only for dirty chunks
        foreach (var chunkPos in _dirtyChunks)
        {
            var chunk = _world.GetChunk(chunkPos);
            if (chunk != null)
            {
                var meshData = ChunkMeshBuilder.BuildChunkMesh(chunk, _world);
                _renderer.UpdateChunk(chunk.ChunkPosition, meshData);
            }
        }
    }
}
