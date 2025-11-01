using LiteNetLib;
using LiteNetLib.Utils;
using TerraNova.Shared;

namespace TerraNova;

/// <summary>
/// Network client that connects to the game server and syncs world data
/// </summary>
public class NetworkClient : INetworkClient, INetEventListener
{
    private readonly NetManager _netManager;
    private NetPeer? _serverPeer;
    private World? _world;
    private bool _worldReceived = false;

    public bool IsConnected => _serverPeer != null && _serverPeer.ConnectionState == ConnectionState.Connected;
    public bool WorldReceived => _worldReceived;
    public World? World => _world;

    public NetworkClient()
    {
        _netManager = new NetManager(this);
    }

    public void Connect(string host, int port, string playerName)
    {
        _netManager.Start();
        _serverPeer = _netManager.Connect(host, port, "TerraNova");

        Console.WriteLine($"Connecting to {host}:{port}...");
    }

    public void Update()
    {
        _netManager.PollEvents();
    }

    public void Disconnect()
    {
        _netManager.Stop();
        Console.WriteLine("Disconnected from server");
    }

    // INetEventListener implementation
    public void OnPeerConnected(NetPeer peer)
    {
        Console.WriteLine("Connected to server!");

        // Send connection message
        var writer = new NetDataWriter();
        writer.Put((byte)MessageType.ClientConnect);
        writer.Put(new ClientConnectMessage("Player"));

        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine($"Disconnected from server: {disconnectInfo.Reason}");
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
                Console.WriteLine($"Block updated at ({blockUpdate.X},{blockUpdate.Y},{blockUpdate.Z})");
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
        // Not used
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        // Client doesn't accept connections
        request.Reject();
    }

    private void ReceiveWorldData(WorldDataMessage worldData)
    {
        Console.WriteLine($"Received world data: {worldData.Blocks.Length} blocks");

        // Create world and populate it
        _world = new World();

        foreach (var block in worldData.Blocks)
        {
            _world.SetBlock(block.X, block.Y, block.Z, block.Type);
        }

        _worldReceived = true;
        Console.WriteLine("World loaded from server!");
    }
}
