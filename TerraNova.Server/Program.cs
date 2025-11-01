using TerraNova.Server;

Console.WriteLine("=== Terra Nova Server ===");
Console.WriteLine();

var server = new GameServer(port: 9050);
server.Start();

Console.WriteLine("Press Ctrl+C to stop the server");
Console.WriteLine();

// Server update loop
var running = true;
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    running = false;
    Console.WriteLine("\nShutting down...");
};

while (running)
{
    server.Update();
    Thread.Sleep(15); // ~60 updates per second
}

server.Stop();
