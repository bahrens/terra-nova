using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TerraNova.Server.Configuration;
using TerraNova.Shared;

namespace TerraNova.Server;

/// <summary>
/// Authoritative game server that manages world state and client connections
/// </summary>
public class GameServer : IGameServer, INetEventListener
{
    private readonly NetManager _netManager;
    private readonly World _world;
    private readonly ServerSettings _serverSettings;
    private readonly WorldSettings _worldSettings;
    private readonly ILogger<GameServer> _logger;
    private WebSocketServer? _webSocketServer;

    // Track each client's state (loaded chunks, position)
    private readonly Dictionary<NetPeer, ClientState> _clientStates = new();

    /// <summary>
    /// Per-client state tracking for chunk streaming
    /// </summary>
    private class ClientState
    {
        public HashSet<Vector2i> LoadedChunks { get; } = new();
        public Vector3? PlayerPosition { get; set; }
    }

    public GameServer(IOptions<ServerSettings> serverSettings, IOptions<WorldSettings> worldSettings, ILogger<GameServer> logger)
    {
        _serverSettings = serverSettings.Value;
        _worldSettings = worldSettings.Value;
        _logger = logger;
        _netManager = new NetManager(this);
        _world = new World();

        // Generate initial world using configured dimensions
        InitializeWorld();
    }

    private void InitializeWorld()
    {
        _logger.LogInformation("Generating procedural terrain...");

        // Calculate world bounds from center
        int halfX = _worldSettings.WorldSizeX / 2;
        int halfZ = _worldSettings.WorldSizeZ / 2;

        // Simplex noise parameters for terrain generation
        float scale = 0.05f;  // Controls terrain "smoothness" - lower = smoother/larger features
        float heightMultiplier = 8.0f;  // How tall the hills/valleys are
        int baseHeight = 4;  // Minimum terrain height

        // Use a fixed seed for consistent terrain generation
        SimplexNoise.Noise.Seed = 12345;

        int blocksGenerated = 0;

        // Generate terrain using height map
        for (int x = -halfX; x <= halfX; x++)
        {
            for (int z = -halfZ; z <= halfZ; z++)
            {
                // Sample Simplex noise to get terrain height at this X,Z position
                // Noise returns value between 0 and 255
                float noiseValue = (float)SimplexNoise.Noise.CalcPixel2D(x, z, scale);

                // Convert noise (0-255) to height value
                int terrainHeight = baseHeight + (int)((noiseValue / 255.0f) * heightMultiplier);

                // Clamp height to world bounds
                terrainHeight = Math.Min(terrainHeight, _worldSettings.WorldSizeY - 1);

                // Fill column from y=0 up to terrain height with layered blocks
                for (int y = 0; y <= terrainHeight; y++)
                {
                    BlockType blockType;

                    // Top layer: grass
                    if (y == terrainHeight)
                    {
                        blockType = BlockType.Grass;
                    }
                    // Next 3 layers: dirt
                    else if (y >= terrainHeight - 3)
                    {
                        blockType = BlockType.Dirt;
                    }
                    // Everything below: stone
                    else
                    {
                        blockType = BlockType.Stone;
                    }

                    _world.SetBlock(x, y, z, blockType);
                    blocksGenerated++;
                }
            }
        }

        _logger.LogInformation("Procedural terrain generated: {BlockCount} blocks with varied heights",
            blocksGenerated);
    }

    // Public methods for WebSocket server
    public (Vector3i position, BlockType blockType)[] GetAllBlocks()
    {
        return _world.GetAllBlocks().ToArray();
    }

    public void SetBlock(int x, int y, int z, BlockType blockType)
    {
        _world.SetBlock(x, y, z, blockType);
    }

    public void SetWebSocketServer(WebSocketServer webSocketServer)
    {
        _webSocketServer = webSocketServer;
        _logger.LogInformation("WebSocketServer reference set for cross-client broadcasting");
    }

    public void Start()
    {
        _netManager.Start(_serverSettings.Port);
        _logger.LogInformation("Server started on port {Port}", _serverSettings.Port);
        _logger.LogInformation("Max clients: {MaxClients}", _serverSettings.MaxClients);
        _logger.LogInformation("Tick rate: {TickRate} Hz", _serverSettings.TickRate);
        _logger.LogInformation("Waiting for clients...");
    }

    public void Update()
    {
        // Poll network events
        _netManager.PollEvents();
    }

    public void Stop()
    {
        _netManager.Stop();
        _logger.LogInformation("Server stopped");
    }

    // INetEventListener implementation
    public void OnPeerConnected(NetPeer peer)
    {
        _logger.LogInformation("Client connected: {Address}", peer.Address);

        // Initialize client state for chunk streaming
        _clientStates[peer] = new ClientState();

        // Don't send all world data immediately - wait for chunk requests from client
        _logger.LogInformation("Waiting for chunk requests from client {Address}", peer.Address);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.LogInformation("Client disconnected: {Address} (Reason: {Reason})", peer.Address, disconnectInfo.Reason);

        // Clean up client state
        _clientStates.Remove(peer);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        var messageType = (MessageType)reader.GetByte();

        switch (messageType)
        {
            case MessageType.ClientConnect:
                var connectMsg = reader.GetClientConnectMessage();
                _logger.LogInformation("Player joined: {PlayerName}", connectMsg.PlayerName);
                break;

            case MessageType.ChunkRequest:
                var chunkRequest = reader.GetChunkRequestMessage();
                HandleChunkRequest(peer, chunkRequest);
                break;

            case MessageType.PlayerPosition:
                var positionMsg = reader.GetPlayerPositionMessage();
                if (_clientStates.TryGetValue(peer, out var clientState))
                {
                    clientState.PlayerPosition = new Vector3(positionMsg.X, positionMsg.Y, positionMsg.Z);
                }
                break;

            case MessageType.BlockUpdate:
                var blockUpdate = reader.GetBlockUpdateMessage();
                _world.SetBlock(blockUpdate.X, blockUpdate.Y, blockUpdate.Z, blockUpdate.NewType);

                // Broadcast to all LiteNetLib clients
                BroadcastBlockUpdate(blockUpdate);

                // Also broadcast to WebSocket clients
                if (_webSocketServer != null)
                {
                    _ = _webSocketServer.BroadcastBlockUpdateToWebSocketClients(
                        blockUpdate.X, blockUpdate.Y, blockUpdate.Z, blockUpdate.NewType);
                }

                _logger.LogInformation("Block updated at ({X},{Y},{Z}) to {BlockType} (broadcasted to all clients)",
                    blockUpdate.X, blockUpdate.Y, blockUpdate.Z, blockUpdate.NewType);
                break;
        }

        reader.Recycle();
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
    {
        _logger.LogError("Network error: {SocketError}", socketError);
    }

    public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        // Not used
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Not used for now
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        request.Accept();
    }

    private void BroadcastBlockUpdate(BlockUpdateMessage blockUpdate)
    {
        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.BlockUpdate);
        writer.Put(blockUpdate);

        // Send to all connected peers
        _netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
    }

    /// <summary>
    /// Public method for WebSocketServer to broadcast block updates to LiteNetLib clients
    /// </summary>
    public void BroadcastBlockUpdateToLiteNetLibClients(int x, int y, int z, BlockType blockType)
    {
        var blockUpdate = new BlockUpdateMessage(x, y, z, blockType);
        BroadcastBlockUpdate(blockUpdate);
    }

    /// <summary>
    /// Handle chunk request from a client - send requested chunks
    /// </summary>
    private void HandleChunkRequest(NetPeer peer, ChunkRequestMessage request)
    {
        if (!_clientStates.TryGetValue(peer, out var clientState))
            return;

        foreach (var chunkPos in request.ChunkPositions)
        {
            // Skip if client already has this chunk
            if (clientState.LoadedChunks.Contains(chunkPos))
                continue;

            // Get blocks in this chunk column from the world
            var chunkBlocks = GetChunkBlocks(chunkPos);

            // Send chunk data to client
            SendChunkData(peer, chunkPos, chunkBlocks);

            // Mark chunk as loaded for this client
            clientState.LoadedChunks.Add(chunkPos);
        }

        _logger.LogInformation("Sent {ChunkCount} chunks to client {Address}",
            request.ChunkPositions.Length, peer.Address);
    }

    /// <summary>
    /// Get all blocks in a chunk column (2D position)
    /// </summary>
    public BlockData[] GetChunkBlocks(Vector2i chunkPos)
    {
        var blocks = new List<BlockData>();
        var chunk = _world.GetChunk(chunkPos);

        if (chunk == null)
            return Array.Empty<BlockData>();

        int chunkWorldX = chunkPos.X * Chunk.ChunkSize;
        int chunkWorldZ = chunkPos.Z * Chunk.ChunkSize;

        // Iterate through entire chunk column (full height)
        for (int x = 0; x < Chunk.ChunkSize; x++)
        {
            for (int y = 0; y < Chunk.WorldHeight; y++)
            {
                for (int z = 0; z < Chunk.ChunkSize; z++)
                {
                    var blockType = chunk.GetBlock(x, y, z);
                    if (blockType != BlockType.Air)
                    {
                        int worldX = chunkWorldX + x;
                        int worldZ = chunkWorldZ + z;
                        blocks.Add(new BlockData(worldX, y, worldZ, blockType));
                    }
                }
            }
        }

        return blocks.ToArray();
    }

    /// <summary>
    /// Send chunk data to a specific client
    /// </summary>
    private void SendChunkData(NetPeer peer, Vector2i chunkPos, BlockData[] blocks)
    {
        var chunkDataMsg = new ChunkDataMessage(chunkPos, blocks);

        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.ChunkData);
        writer.Put(chunkDataMsg);

        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
}
