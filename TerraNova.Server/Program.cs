using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TerraNova.Server;
using TerraNova.Server.Configuration;

Console.WriteLine("=== Terra Nova Server ===");
Console.WriteLine();

// Build the host with dependency injection
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        // Bind configuration sections
        services.Configure<ServerSettings>(context.Configuration.GetSection("ServerSettings"));
        services.Configure<WorldSettings>(context.Configuration.GetSection("WorldSettings"));

        // Register services
        services.AddSingleton<IGameServer, GameServer>();
        services.AddHostedService<ServerWorker>();
    })
    .Build();

// Run the host (which will start the ServerWorker)
await host.RunAsync();
