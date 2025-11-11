using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TerraNova;
using TerraNova.Configuration;

// Build the host with dependency injection
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        // Bind configuration sections
        services.Configure<GameSettings>(context.Configuration.GetSection("GameSettings"));
        services.Configure<NetworkSettings>(context.Configuration.GetSection("NetworkSettings"));
        services.Configure<CameraSettings>(context.Configuration.GetSection("CameraSettings"));

        // Register services
        services.AddSingleton<INetworkClient, NetworkClient>();
        services.AddSingleton<ClientApplication>();
        services.AddSingleton<Game>();
    })
    .Build();

// Get the game instance from DI container and run it
using (var game = host.Services.GetRequiredService<Game>())
{
    game.Run();
}
