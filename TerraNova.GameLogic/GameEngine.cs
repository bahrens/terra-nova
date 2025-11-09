using TerraNova.Shared;

namespace TerraNova.GameLogic;

/// <summary>
/// Platform-agnostic game engine that manages the game loop, world state, and rendering.
/// This is the single source of truth for game logic - both desktop and web clients use this.
/// </summary>
public class GameEngine : IDisposable
{
    private readonly IRenderer _renderer;
    private World? _world;
    private readonly HashSet<Vector2i> _dirtyChunks = new();
    private ChunkLoader? _chunkLoader;
    private AsyncChunkMeshBuilder? _meshBuilder;

    /// <summary>
    /// Callback for requesting chunks from server.
    /// Set this on the client side to handle network communication.
    /// </summary>
    public Action<Vector2i[]>? OnChunkRequestNeeded { get; set; }

    public GameEngine(IRenderer renderer)
    {
        _renderer = renderer;
    }

    /// <summary>
    /// Get the current world state (may be null if not loaded yet)
    /// </summary>
    public World? World => _world;

    /// <summary>
    /// Set the world data (called when receiving world from server or initializing)
    /// </summary>
    public void SetWorld(World world)
    {
        _world = world;

        // Initialize ChunkLoader for dynamic chunk loading
        _chunkLoader = new ChunkLoader(_world);
        _chunkLoader.OnChunkRequestNeeded = OnChunkRequestNeeded;

        // Initialize AsyncChunkMeshBuilder
        _meshBuilder = new AsyncChunkMeshBuilder(_world);

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
    /// Update player position for chunk loading (called when player moves)
    /// </summary>
    public void UpdatePlayerPosition(Vector3 playerPosition)
    {
        _chunkLoader?.Update(playerPosition);
    }

    /// <summary>
    /// Notify that a chunk has been received from the server and added to the world
    /// </summary>
    public void NotifyChunkReceived(Vector2i chunkPos)
    {
        _chunkLoader?.MarkChunkLoaded(chunkPos);
        _dirtyChunks.Add(chunkPos);
    }

    /// <summary>
    /// Update game state (called every frame)
    /// </summary>
    public void Update(double deltaTime)
    {
        // If there are dirty chunks, enqueue them for mesh building
        if (_dirtyChunks.Count > 0 && _world != null && _meshBuilder != null)
        {
            foreach (var chunkPos in _dirtyChunks)
            {
                _meshBuilder.EnqueueChunk(chunkPos);
            }
            _dirtyChunks.Clear();
        }

        // Always process completed meshes (they may be ready from previous frames)
        if (_meshBuilder != null)
        {
            _meshBuilder.ProcessCompletedMeshes(_renderer);
        }
    }

    public void Dispose()
    {
        _meshBuilder?.Dispose();
    }
}
