using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TerraNova.Server.Configuration;

namespace TerraNova.Server;

/// <summary>
/// Background service that runs the game server update loop
/// </summary>
public class ServerWorker : BackgroundService
{
    private readonly IGameServer _gameServer;
    private readonly ServerSettings _serverSettings;

    public ServerWorker(IGameServer gameServer, IOptions<ServerSettings> serverSettings)
    {
        _gameServer = gameServer;
        _serverSettings = serverSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Starting server...");
        _gameServer.Start();

        Console.WriteLine("Press Ctrl+C to stop the server");
        Console.WriteLine();

        // Calculate update interval based on tick rate
        int updateIntervalMs = 1000 / _serverSettings.TickRate;

        while (!stoppingToken.IsCancellationRequested)
        {
            _gameServer.Update();
            await Task.Delay(updateIntervalMs, stoppingToken);
        }

        Console.WriteLine("\nShutting down...");
        _gameServer.Stop();
    }
}
