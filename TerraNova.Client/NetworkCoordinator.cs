using Microsoft.Extensions.Logging;
using TerraNova.Configuration;
using TerraNova.GameLogic;
using TerraNova.Shared;

namespace TerraNova;

/// <summary>
/// Coordinates network communication and event handling.
/// Manages connection lifecycle and bridges network events to game systems.
/// CRITICAL FIX: Event handlers registered once (not in update loop).
/// </summary>
public class NetworkCoordinator : IDisposable
{
    private readonly INetworkClient _networkClient;
    private readonly NetworkSettings _networkSettings;
    private readonly ILogger<NetworkCoordinator> _logger;

    private bool _worldInitialized = false;

    public bool WorldReceived => _networkClient.WorldReceived;
    public World? World => _networkClient.World;

    /// <summary>
    /// Gets the underlying network client (for components not yet refactored).
    /// TODO: Remove this once PlayerController is refactored to use NetworkCoordinator.
    /// </summary>
    public INetworkClient GetNetworkClient() => _networkClient;

    public NetworkCoordinator(
        INetworkClient networkClient,
        NetworkSettings networkSettings,
        ILogger<NetworkCoordinator> logger)
    {
        _networkClient = networkClient;
        _networkSettings = networkSettings;
        _logger = logger;
    }

    /// <summary>
    /// Connect to the server (called once during initialization).
    /// </summary>
    public void Connect()
    {
        _networkClient.Connect(
            _networkSettings.ServerHost,
            _networkSettings.ServerPort,
            _networkSettings.PlayerName);

        _logger.LogInformation("Connecting to {Host}:{Port} as {PlayerName}...",
            _networkSettings.ServerHost,
            _networkSettings.ServerPort,
            _networkSettings.PlayerName);
    }

    /// <summary>
    /// Poll network events (called every frame).
    /// </summary>
    public void Update()
    {
        _networkClient.Update();
    }

    /// <summary>
    /// Initialize world-related systems when world is received.
    /// Called once by Game.cs when world becomes available.
    /// CRITICAL FIX: Event handlers registered once here, not in update loop.
    /// </summary>
    public void InitializeWorld(GameEngine gameEngine, World world)
    {
        if (_worldInitialized)
        {
            _logger.LogWarning("World already initialized, skipping duplicate initialization");
            return;
        }

        if (world == null)
        {
            _logger.LogError("Cannot initialize world - World is null");
            return;
        }

        _logger.LogInformation("World received! Initializing network event handlers...");

        // Hook up chunk loading system BEFORE calling SetWorld
        // CRITICAL FIX: These handlers are registered ONCE, not 60+ times per second
        gameEngine.OnChunkRequestNeeded = (chunks) =>
        {
            _logger.LogInformation("Requesting {Count} chunks from server", chunks.Length);
            _networkClient.RequestChunks(chunks);
        };

        _networkClient.OnChunkReceived += (chunkPos, blocks) =>
        {
            _logger.LogInformation("Chunk ({X},{Z}) received with {Count} blocks",
                chunkPos.X, chunkPos.Z, blocks.Length);
            gameEngine.NotifyChunkReceived(chunkPos);
        };

        _networkClient.OnBlockUpdate += (x, y, z, blockType) =>
        {
            _logger.LogInformation("Block update received at ({X},{Y},{Z}) -> {Type}",
                x, y, z, blockType);
            gameEngine.NotifyBlockUpdate(x, y, z, blockType);
        };

        // Now set the world (this creates ChunkLoader with the callback)
        gameEngine.SetWorld(world);

        _worldInitialized = true;
        _logger.LogInformation("Chunk streaming enabled!");
    }

    /// <summary>
    /// Request chunks from the server (used by PlayerController).
    /// </summary>
    public void RequestChunks(Vector2i[] chunks)
    {
        _networkClient.RequestChunks(chunks);
    }

    public void Dispose()
    {
        _networkClient?.Disconnect();
    }
}
