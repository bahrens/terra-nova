using TerraNova.Shared;

namespace TerraNova;

/// <summary>
/// Interface for network client communication
/// </summary>
public interface INetworkClient
{
    /// <summary>
    /// Whether the client is currently connected to a server
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Whether the world data has been received from the server
    /// </summary>
    bool WorldReceived { get; }

    /// <summary>
    /// The world data received from the server
    /// </summary>
    World? World { get; }

    /// <summary>
    /// Whether the world has changed and needs mesh regeneration
    /// </summary>
    bool WorldChanged { get; set; }

    /// <summary>
    /// Connect to a game server
    /// </summary>
    void Connect(string host, int port, string playerName);

    /// <summary>
    /// Poll for network events (should be called every frame)
    /// </summary>
    void Update();

    /// <summary>
    /// Disconnect from the server
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Send a block update to the server
    /// </summary>
    void SendBlockUpdate(int x, int y, int z, BlockType blockType);
}
