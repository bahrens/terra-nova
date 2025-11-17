using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ServerWorker> _logger;

    public ServerWorker(IGameServer gameServer, IOptions<ServerSettings> serverSettings, ILogger<ServerWorker> logger)
    {
        _gameServer = gameServer;
        _serverSettings = serverSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting server...");
        _gameServer.Start();

        _logger.LogInformation("Press Ctrl+C to stop the server");
        _logger.LogInformation("");

        // Calculate update interval based on tick rate
        int updateIntervalMs = 1000 / _serverSettings.TickRate;

        while (!stoppingToken.IsCancellationRequested)
        {
            _gameServer.Update();
            await Task.Delay(updateIntervalMs, stoppingToken);
        }

        _logger.LogInformation("Shutting down...");
        _gameServer.Stop();
    }
}
