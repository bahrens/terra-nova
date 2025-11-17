namespace TerraNova.Configuration;

/// <summary>
/// Network connection configuration settings
/// </summary>
public class NetworkSettings
{
    public string ServerHost { get; set; } = "localhost";
    public int ServerPort { get; set; } = 9050;
    public string PlayerName { get; set; } = "Player";
}
