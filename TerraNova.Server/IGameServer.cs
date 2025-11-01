namespace TerraNova.Server;

/// <summary>
/// Interface for the game server
/// </summary>
public interface IGameServer
{
    /// <summary>
    /// Start the server and begin listening for connections
    /// </summary>
    void Start();

    /// <summary>
    /// Update server state (should be called every frame)
    /// </summary>
    void Update();

    /// <summary>
    /// Stop the server and disconnect all clients
    /// </summary>
    void Stop();
}
