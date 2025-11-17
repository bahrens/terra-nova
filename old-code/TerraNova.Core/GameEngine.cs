using TerraNova.Shared;

namespace TerraNova.Core;

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

    // Physics for terrain collision - DISABLED in favor of voxel AABB collision
    // Keeping code for easy rollback if needed
    // private IPhysicsWorld? _physicsWorld;
    // private IPhysicsShapeFactory? _shapeFactory;
    // // Map of chunk position -> list of physics bodies for that chunk's blocks
    // private readonly Dictionary<Vector2i, List<IPhysicsBody>> _chunkCollisionBodies = new();

    // Logger for debugging collision issues
    private Action<string>? _logger;

    /// <summary>
    /// Callback for requesting chunks from server.
    /// Set this on the client side to handle network communication.
    /// </summary>
    public Action<Vector2i[]>? OnChunkRequestNeeded { get; set; }

    /// <summary>
    /// Optional logger callback for debugging.
    /// Set this to receive log messages from the game engine.
    /// </summary>
    public Action<string>? Logger
    {
        get => _logger;
        set => _logger = value;
    }

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
        // DISABLED: Terrain collision removed in favor of voxel AABB collision
        // _chunkLoader.OnChunkUnloaded = RemoveChunkCollision;

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
    /// DISABLED: Terrain collision physics removed in favor of voxel AABB collision.
    /// Keeping this method commented out for easy rollback if needed.
    /// </summary>
    // public void InitializePhysics(IPhysicsWorld physicsWorld, IPhysicsShapeFactory shapeFactory, Vector3 initialPlayerPosition)
    // {
    //     _physicsWorld = physicsWorld;
    //     _shapeFactory = shapeFactory;
    //
    //     _logger?.Invoke("[GameEngine] Physics initialized, creating initial collision around player");
    //
    //     // CRITICAL: Create initial collision around player position BEFORE physics starts stepping
    //     // This prevents the player from falling through terrain on first frame
    //     UpdateCollisionAroundPlayer(initialPlayerPosition);
    //
    //     _logger?.Invoke($"[GameEngine] Initial collision created: {_chunkCollisionBodies.Count} chunks with collision");
    // }

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

            // DISABLED: Terrain collision removed in favor of voxel AABB collision
            // UpdateBlockCollision(x, y, z);
        }
    }

    /// <summary>
    /// Update player position for chunk loading (called when player moves)
    /// </summary>
    public void UpdatePlayerPosition(Vector3 playerPosition)
    {
        _chunkLoader?.Update(playerPosition);

        // DISABLED: Terrain collision removed in favor of voxel AABB collision
        // UpdateCollisionAroundPlayer(playerPosition);
    }

    // DISABLED: Terrain collision removed in favor of voxel AABB collision
    // private Vector3? _lastPlayerPosition;
    // private const int CollisionRadius = 3; // Only create collision for chunks within 3 chunk radius (7×7 = 49 chunks, ~20K bodies max)

    /// <summary>
    /// DISABLED: Terrain collision removed in favor of voxel AABB collision.
    /// </summary>
    /*
    private void UpdateCollisionAroundPlayer(Vector3 playerPosition)
    {
        if (_physicsWorld == null || _world == null)
        {
            _logger?.Invoke("[GameEngine] UpdateCollisionAroundPlayer: Physics world or world is null, skipping");
            return;
        }

        // Only update if player moved to a different chunk
        Vector2i playerChunkPos = new Vector2i(
            (int)Math.Floor(playerPosition.X / Chunk.ChunkSize),
            (int)Math.Floor(playerPosition.Z / Chunk.ChunkSize)
        );

        Vector2i? lastPlayerChunkPos = null;
        if (_lastPlayerPosition.HasValue)
        {
            lastPlayerChunkPos = new Vector2i(
                (int)Math.Floor(_lastPlayerPosition.Value.X / Chunk.ChunkSize),
                (int)Math.Floor(_lastPlayerPosition.Value.Z / Chunk.ChunkSize)
            );
        }

        if (lastPlayerChunkPos.HasValue && playerChunkPos == lastPlayerChunkPos.Value)
            return; // Still in same chunk, no update needed

        _logger?.Invoke($"[GameEngine] Player moved to chunk ({playerChunkPos.X}, {playerChunkPos.Z}), updating collision");
        _lastPlayerPosition = playerPosition;

        // Remove collision from chunks that are now too far
        var chunksToRemove = new List<Vector2i>();
        foreach (var chunkPos in _chunkCollisionBodies.Keys)
        {
            int dx = Math.Abs(chunkPos.X - playerChunkPos.X);
            int dz = Math.Abs(chunkPos.Z - playerChunkPos.Z);
            if (dx > CollisionRadius || dz > CollisionRadius)
            {
                chunksToRemove.Add(chunkPos);
            }
        }

        foreach (var chunkPos in chunksToRemove)
        {
            RemoveChunkCollision(chunkPos);
        }

        // Add collision for chunks that are now nearby
        int chunksAdded = 0;
        for (int x = playerChunkPos.X - CollisionRadius; x <= playerChunkPos.X + CollisionRadius; x++)
        {
            for (int z = playerChunkPos.Z - CollisionRadius; z <= playerChunkPos.Z + CollisionRadius; z++)
            {
                Vector2i chunkPos = new Vector2i(x, z);

                // Skip if collision already exists for this chunk
                if (_chunkCollisionBodies.ContainsKey(chunkPos))
                    continue;

                // Only create collision if chunk is loaded
                if (_world.GetChunk(chunkPos) != null)
                {
                    CreateChunkCollision(chunkPos);
                    chunksAdded++;
                }
            }
        }

        if (chunksAdded > 0)
        {
            _logger?.Invoke($"[GameEngine] Added collision for {chunksAdded} chunks around player");
        }
    }
    */

    /// <summary>
    /// Notify that a chunk has been received from the server and added to the world
    /// </summary>
    public void NotifyChunkReceived(Vector2i chunkPos)
    {
        _chunkLoader?.MarkChunkLoaded(chunkPos);
        _dirtyChunks.Add(chunkPos);

        // Mark all 4 neighboring chunks as dirty so they rebuild their meshes
        // with proper face culling now that this chunk has loaded
        _dirtyChunks.Add(new Vector2i(chunkPos.X - 1, chunkPos.Z)); // West
        _dirtyChunks.Add(new Vector2i(chunkPos.X + 1, chunkPos.Z)); // East
        _dirtyChunks.Add(new Vector2i(chunkPos.X, chunkPos.Z - 1)); // South
        _dirtyChunks.Add(new Vector2i(chunkPos.X, chunkPos.Z + 1)); // North

        // DISABLED: Terrain collision removed in favor of voxel AABB collision
        // Create collision if chunk is within collision radius AND physics is initialized
        // This handles the case where chunks are streamed from server after InitializePhysics()
        // if (_physicsWorld != null && _shapeFactory != null && _lastPlayerPosition.HasValue)
        // {
        //     // Calculate player chunk position
        //     Vector2i playerChunkPos = new Vector2i(
        //         (int)Math.Floor(_lastPlayerPosition.Value.X / Chunk.ChunkSize),
        //         (int)Math.Floor(_lastPlayerPosition.Value.Z / Chunk.ChunkSize)
        //     );
        //
        //     // Check if this chunk is within collision radius
        //     int dx = Math.Abs(chunkPos.X - playerChunkPos.X);
        //     int dz = Math.Abs(chunkPos.Z - playerChunkPos.Z);
        //
        //     if (dx <= CollisionRadius && dz <= CollisionRadius)
        //     {
        //         // Chunk is close enough - create collision
        //         if (!_chunkCollisionBodies.ContainsKey(chunkPos))
        //         {
        //             CreateChunkCollision(chunkPos);
        //         }
        //     }
        // }
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

    /// <summary>
    /// DISABLED: Terrain collision removed in favor of voxel AABB collision.
    /// </summary>
    /*
    private void CreateChunkCollision(Vector2i chunkPos)
    {
        if (_physicsWorld == null || _shapeFactory == null || _world == null)
            return;

        var chunk = _world.GetChunk(chunkPos);
        if (chunk == null)
            return;

        int blockCount = 0;
        var bodies = new List<IPhysicsBody>();

        // Iterate through all blocks in the chunk
        for (int x = 0; x < Chunk.ChunkSize; x++)
        {
            for (int z = 0; z < Chunk.ChunkSize; z++)
            {
                for (int y = 0; y < Chunk.WorldHeight; y++)
                {
                    BlockType blockType = chunk.GetBlock(x, y, z);

                    // Skip air blocks
                    if (blockType == BlockType.Air)
                        continue;

                    // Optimization: Only create collision for surface blocks (blocks with air above)
                    // This reduces physics bodies from thousands to hundreds per chunk
                    bool isSurface = (y == Chunk.WorldHeight - 1) || chunk.GetBlock(x, y + 1, z) == BlockType.Air;

                    if (!isSurface)
                        continue; // Skip buried blocks

                    // Create 1x1x1 box collision shape (half-extents = 0.5)
                    IPhysicsShape boxShape = _shapeFactory.CreateBox(new Vector3(0.5f, 0.5f, 0.5f));

                    // Calculate world position of this block (centered)
                    int worldX = chunkPos.X * Chunk.ChunkSize + x;
                    int worldZ = chunkPos.Z * Chunk.ChunkSize + z;
                    Vector3 blockPosition = new Vector3(worldX + 0.5f, y + 0.5f, worldZ + 0.5f);

                    // Create static physics body (infinite mass, doesn't move)
                    // CRITICAL FIX: Set position BEFORE calling SetShape() per Jitter2 docs
                    IPhysicsBody body = _physicsWorld.CreateBody();
                    body.Position = blockPosition;
                    body.AffectedByGravity = false;  // Set gravity before shape
                    body.IsStatic = true;             // Set motion type before shape
                    body.SetShape(boxShape);          // Add shape LAST
                    // Note: CreateBody() already adds to world, no need for AddBody()

                    bodies.Add(body);
                    blockCount++;
                }
            }
        }

        // Store bodies for this chunk so we can remove them later
        _chunkCollisionBodies[chunkPos] = bodies;

        if (blockCount > 0)
        {
            int totalBodies = _chunkCollisionBodies.Values.Sum(list => list.Count);
            _logger?.Invoke($"[GameEngine] Created {blockCount} collision bodies for chunk ({chunkPos.X}, {chunkPos.Z}). Total bodies across all chunks: {totalBodies}");
        }
    }

    private void RemoveChunkCollision(Vector2i chunkPos)
    {
        if (_physicsWorld == null || !_chunkCollisionBodies.TryGetValue(chunkPos, out var bodies))
            return;

        int removedCount = bodies.Count;

        // Remove all physics bodies for this chunk
        foreach (var body in bodies)
        {
            _physicsWorld.RemoveBody(body);
        }

        _chunkCollisionBodies.Remove(chunkPos);
        int totalBodies = _chunkCollisionBodies.Values.Sum(list => list.Count);
        _logger?.Invoke($"[GameEngine] Removed {removedCount} collision bodies for chunk ({chunkPos.X}, {chunkPos.Z}). Remaining: {_chunkCollisionBodies.Count} chunks, {totalBodies} total bodies");
    }

    private void UpdateBlockCollision(int worldX, int worldY, int worldZ)
    {
        if (_physicsWorld == null || _shapeFactory == null || _world == null)
            return;

        // Calculate chunk position
        Vector2i chunkPos = Chunk.WorldToChunkPosition(worldX, worldZ);

        // Rebuild the entire chunk's collision
        // This is simple but potentially inefficient - could be optimized later
        RemoveChunkCollision(chunkPos);
        CreateChunkCollision(chunkPos);
    }
    */

    public void Dispose()
    {
        _meshBuilder?.Dispose();

        // DISABLED: Terrain collision removed in favor of voxel AABB collision
        // Clean up all physics collision bodies
        // if (_physicsWorld != null)
        // {
        //     foreach (var bodies in _chunkCollisionBodies.Values)
        //     {
        //         foreach (var body in bodies)
        //         {
        //             _physicsWorld.RemoveBody(body);
        //         }
        //     }
        //     _chunkCollisionBodies.Clear();
        // }
    }
}
