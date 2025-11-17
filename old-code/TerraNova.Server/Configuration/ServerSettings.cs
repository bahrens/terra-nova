namespace TerraNova.Server.Configuration;

/// <summary>
/// Server configuration settings
/// </summary>
public class ServerSettings
{
    public int Port { get; set; } = 9050;
    public int MaxClients { get; set; } = 10;
    public int TickRate { get; set; } = 60;
}
