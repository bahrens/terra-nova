using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using TerraNova.Shared;

namespace TerraNova;

/// <summary>
/// Network client that connects to the game server and syncs world data
/// </summary>
public class NetworkClient : INetworkClient, INetEventListener
{
    private readonly NetManager _netManager;
    private readonly ILogger<NetworkClient> _logger;
    private NetPeer? _serverPeer;
    private World? _world;
    private bool _worldReceived = false;

    public bool IsConnected => _serverPeer != null && _serverPeer.ConnectionState == ConnectionState.Connected;
    public bool WorldReceived => _worldReceived;
    public World? World => _world;
    public bool WorldChanged { get; set; }

    public NetworkClient(ILogger<NetworkClient> logger)
    {
        _logger = logger;
        _netManager = new NetManager(this);
    }

    public void Connect(string host, int port, string playerName)
    {
        _netManager.Start();
        _serverPeer = _netManager.Connect(host, port, "TerraNova");

        _logger.LogInformation("Connecting to {Host}:{Port}...", host, port);
    }

    public void Update()
    {
        _netManager.PollEvents();
    }

    public void Disconnect()
    {
        _netManager.Stop();
        _logger.LogInformation("Disconnected from server");
    }

    public void SendBlockUpdate(int x, int y, int z, BlockType blockType)
    {
        if (_serverPeer == null || !IsConnected)
        {
            _logger.LogWarning("Cannot send block update: not connected to server");
            return;
        }

        var blockUpdate = new BlockUpdateMessage(x, y, z, blockType);
        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.BlockUpdate);
        writer.Put(blockUpdate);

        _serverPeer.Send(writer, DeliveryMethod.ReliableOrdered);
        _logger.LogDebug("Sent block update: ({X},{Y},{Z}) -> {BlockType}", x, y, z, blockType);
    }

    // INetEventListener implementation
    public void OnPeerConnected(NetPeer peer)
    {
        _logger.LogInformation("Connected to server!");

        // Send connection message
        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.ClientConnect);
        writer.Put(new ClientConnectMessage("Player"));

        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.LogInformation("Disconnected from server: {Reason}", disconnectInfo.Reason);
        _serverPeer = null;
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        var messageType = (MessageType)reader.GetByte();

        switch (messageType)
        {
            case MessageType.WorldData:
                var worldData = reader.GetWorldDataMessage();
                ReceiveWorldData(worldData);
                break;

            case MessageType.BlockUpdate:
                var blockUpdate = reader.GetBlockUpdateMessage();
                _world?.SetBlock(blockUpdate.X, blockUpdate.Y, blockUpdate.Z, blockUpdate.NewType);
                WorldChanged = true; // Flag that meshes need to be regenerated
                _logger.LogInformation("Block updated at ({X},{Y},{Z})", blockUpdate.X, blockUpdate.Y, blockUpdate.Z);
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
        // Not used
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        // Client doesn't accept connections
        request.Reject();
    }

    private void ReceiveWorldData(WorldDataMessage worldData)
    {
        _logger.LogInformation("Received world data: {BlockCount} blocks", worldData.Blocks.Length);

        // Create world and populate it
        _world = new World();

        foreach (var block in worldData.Blocks)
        {
            _world.SetBlock(block.X, block.Y, block.Z, block.Type);
        }

        _worldReceived = true;
        _logger.LogInformation("World loaded from server!");
    }
}
