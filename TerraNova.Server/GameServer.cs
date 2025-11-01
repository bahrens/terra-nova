using LiteNetLib;
using LiteNetLib.Utils;
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

    public GameServer(IOptions<ServerSettings> serverSettings, IOptions<WorldSettings> worldSettings)
    {
        _serverSettings = serverSettings.Value;
        _worldSettings = worldSettings.Value;
        _netManager = new NetManager(this);
        _world = new World();

        // Generate initial world using configured dimensions
        InitializeWorld();
    }

    private void InitializeWorld()
    {
        Console.WriteLine("Generating world...");

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
        Console.WriteLine($"World generated: {totalBlocks} blocks ({_worldSettings.WorldSizeX}x{_worldSettings.WorldSizeY}x{_worldSettings.WorldSizeZ})");
    }

    public void Start()
    {
        _netManager.Start(_serverSettings.Port);
        Console.WriteLine($"Server started on port {_serverSettings.Port}");
        Console.WriteLine($"Max clients: {_serverSettings.MaxClients}");
        Console.WriteLine($"Tick rate: {_serverSettings.TickRate} Hz");
        Console.WriteLine("Waiting for clients...");
    }

    public void Update()
    {
        // Poll network events
        _netManager.PollEvents();
    }

    public void Stop()
    {
        _netManager.Stop();
        Console.WriteLine("Server stopped");
    }

    // INetEventListener implementation
    public void OnPeerConnected(NetPeer peer)
    {
        Console.WriteLine($"Client connected: {peer.Address}");
        SendWorldData(peer);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine($"Client disconnected: {peer.Address} (Reason: {disconnectInfo.Reason})");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        var messageType = (MessageType)reader.GetByte();

        switch (messageType)
        {
            case MessageType.ClientConnect:
                var connectMsg = reader.GetClientConnectMessage();
                Console.WriteLine($"Player joined: {connectMsg.PlayerName}");
                break;

            case MessageType.BlockUpdate:
                var blockUpdate = reader.GetBlockUpdateMessage();
                _world.SetBlock(blockUpdate.X, blockUpdate.Y, blockUpdate.Z, blockUpdate.NewType);
                // Broadcast to all clients (TODO)
                Console.WriteLine($"Block updated at ({blockUpdate.X},{blockUpdate.Y},{blockUpdate.Z}) to {blockUpdate.NewType}");
                break;
        }

        reader.Recycle();
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
    {
        Console.WriteLine($"Network error: {socketError}");
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

        Console.WriteLine($"Sent {blocks.Length} blocks to client {peer.Address}");
    }
}
