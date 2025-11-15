using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TerraNova.Server;
using TerraNova.Server.Configuration;

Console.WriteLine("=== Terra Nova Server ===");
Console.WriteLine();

// Build the web application with WebSocket support
var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Bind configuration sections
builder.Services.Configure<ServerSettings>(builder.Configuration.GetSection("ServerSettings"));
builder.Services.Configure<WorldSettings>(builder.Configuration.GetSection("WorldSettings"));

// Register services
builder.Services.AddSingleton<GameServer>();
builder.Services.AddSingleton<IGameServer>(sp => sp.GetRequiredService<GameServer>());
builder.Services.AddSingleton<WebSocketServer>();
builder.Services.AddHostedService<ServerWorker>();

var app = builder.Build();

// Wire up GameServer and WebSocketServer for cross-client broadcasting
var gameServer = app.Services.GetRequiredService<GameServer>();
var webSocketServer = app.Services.GetRequiredService<WebSocketServer>();
gameServer.SetWebSocketServer(webSocketServer);

// Enable WebSockets
app.UseWebSockets();

// Map WebSocket endpoint
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var wsServer = context.RequestServices.GetRequiredService<WebSocketServer>();
        await wsServer.HandleWebSocketAsync(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

// Run the application (which will start the ServerWorker)
await app.RunAsync();
