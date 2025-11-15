using TerraNova.Shared;

namespace TerraNova.Core;

/// <summary>
/// Manages chunk loading and unloading based on player position.
/// Works with 2D chunk columns (X, Z only) following Minecraft's standard.
/// </summary>
public class ChunkLoader
{
    private readonly World _world;
    private readonly HashSet<Vector2i> _loadedChunks = new();
    private Vector3? _lastPlayerPosition;

    // Distance thresholds (in chunks)
    private const int RenderDistance = 8;  // Chunks to actively render
    private const int LoadDistance = 10;   // Preload buffer zone
    private const int UnloadDistance = 12; // Unload chunks beyond this

    // Callback for requesting chunks from server
    public Action<Vector2i[]>? OnChunkRequestNeeded { get; set; }

    // Callback for when chunks are unloaded (for cleanup like physics bodies)
    public Action<Vector2i>? OnChunkUnloaded { get; set; }

    public ChunkLoader(World world)
    {
        _world = world;
    }

    /// <summary>
    /// Update chunk loading based on current player position.
    /// Call this periodically (e.g., every frame or when player moves significantly).
    /// </summary>
    /// <param name="playerPosition">Current player world position</param>
    public void Update(Vector3 playerPosition)
    {
        // Only update if player moved significantly (at least 1 block)
        if (_lastPlayerPosition.HasValue)
        {
            float dx = playerPosition.X - _lastPlayerPosition.Value.X;
            float dy = playerPosition.Y - _lastPlayerPosition.Value.Y;
            float dz = playerPosition.Z - _lastPlayerPosition.Value.Z;
            float distanceMoved = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
            if (distanceMoved < 1.0f)
                return;
        }

        _lastPlayerPosition = playerPosition;

        // Calculate player's chunk position (2D: X, Z only)
        Vector2i playerChunkPos = Chunk.WorldToChunkPosition(
            (int)Math.Floor(playerPosition.X),
            (int)Math.Floor(playerPosition.Z)
        );

        // Determine which chunks should be loaded
        var chunksToLoad = new List<(Vector2i pos, float distance)>();
        for (int x = -LoadDistance; x <= LoadDistance; x++)
        {
            for (int z = -LoadDistance; z <= LoadDistance; z++)
            {
                Vector2i chunkPos = new Vector2i(playerChunkPos.X + x, playerChunkPos.Z + z);

                // Check if chunk is within load distance (2D circular distance)
                float distance = MathF.Sqrt(x * x + z * z);
                if (distance <= LoadDistance)
                {
                    // If chunk not already loaded, request it
                    if (!_loadedChunks.Contains(chunkPos) && _world.GetChunk(chunkPos) == null)
                    {
                        chunksToLoad.Add((chunkPos, distance));
                        _loadedChunks.Add(chunkPos);
                    }
                }
            }
        }

        // Sort chunks by distance from player (closest first) for more natural loading
        chunksToLoad.Sort((a, b) => a.distance.CompareTo(b.distance));

        // Request new chunks from server if any
        if (chunksToLoad.Count > 0 && OnChunkRequestNeeded != null)
        {
            OnChunkRequestNeeded(chunksToLoad.Select(c => c.pos).ToArray());
        }

        // Unload distant chunks to save memory
        UnloadDistantChunks(playerChunkPos);
    }

    /// <summary>
    /// Unload chunks that are beyond the unload distance
    /// </summary>
    private void UnloadDistantChunks(Vector2i playerChunkPos)
    {
        var chunksToUnload = new List<Vector2i>();

        foreach (var chunkPos in _loadedChunks)
        {
            // Calculate 2D distance from player chunk
            int deltaX = chunkPos.X - playerChunkPos.X;
            int deltaZ = chunkPos.Z - playerChunkPos.Z;
            float distance = MathF.Sqrt(deltaX * deltaX + deltaZ * deltaZ);

            if (distance > UnloadDistance)
            {
                chunksToUnload.Add(chunkPos);
            }
        }

        // Remove from tracking and world
        foreach (var chunkPos in chunksToUnload)
        {
            _loadedChunks.Remove(chunkPos);
            _world.RemoveChunk(chunkPos);

            // Notify listeners (e.g., GameEngine) to clean up physics bodies
            OnChunkUnloaded?.Invoke(chunkPos);
        }
    }

    /// <summary>
    /// Mark a chunk as loaded (called when chunk data is received from server)
    /// </summary>
    public void MarkChunkLoaded(Vector2i chunkPos)
    {
        _loadedChunks.Add(chunkPos);
    }

    /// <summary>
    /// Get all currently loaded chunk positions
    /// </summary>
    public IReadOnlySet<Vector2i> GetLoadedChunks()
    {
        return _loadedChunks;
    }
}
