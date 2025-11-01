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
        _logger.LogInformation("Generating world...");

        // Calculate world bounds from center
        int halfX = _worldSettings.WorldSizeX / 2;
        int halfZ = _worldSettings.WorldSizeZ / 2;

        for (int x = -halfX; x <= halfX; x++)
        {
            for (int y = 0; y < _worldSettings.WorldSizeY; y++)
            {
                for (int z = -halfZ; z <= halfZ; z++)
                {
                    _world.SetBlock(x, y, z, BlockType.Grass);
                }
            }
        }

        int totalBlocks = _worldSettings.WorldSizeX * _worldSettings.WorldSizeY * _worldSettings.WorldSizeZ;
        _logger.LogInformation("World generated: {TotalBlocks} blocks ({SizeX}x{SizeY}x{SizeZ})",
            totalBlocks, _worldSettings.WorldSizeX, _worldSettings.WorldSizeY, _worldSettings.WorldSizeZ);
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
        SendWorldData(peer);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.LogInformation("Client disconnected: {Address} (Reason: {Reason})", peer.Address, disconnectInfo.Reason);
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

            case MessageType.BlockUpdate:
                var blockUpdate = reader.GetBlockUpdateMessage();
                _world.SetBlock(blockUpdate.X, blockUpdate.Y, blockUpdate.Z, blockUpdate.NewType);
                // Broadcast to all clients (TODO)
                _logger.LogInformation("Block updated at ({X},{Y},{Z}) to {BlockType}",
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

    private void SendWorldData(NetPeer peer)
    {
        // Convert World blocks to BlockData array
        var blocks = _world.GetAllBlocks()
            .Select(b => new BlockData(b.position.X, b.position.Y, b.position.Z, b.blockType))
            .ToArray();

        var worldDataMsg = new WorldDataMessage(blocks);

        // Send to client
        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.WorldData);
        writer.Put(worldDataMsg);

        peer.Send(writer, DeliveryMethod.ReliableOrdered);

        _logger.LogInformation("Sent {BlockCount} blocks to client {Address}", blocks.Length, peer.Address);
    }
}
