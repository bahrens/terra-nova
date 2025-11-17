using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TerraNova.Shared;

namespace TerraNova.Server;

/// <summary>
/// WebSocket server for browser-based clients
/// </summary>
public class WebSocketServer
{
    private readonly ILogger<WebSocketServer> _logger;
    private readonly GameServer _gameServer;
    private readonly List<WebSocketClient> _clients = new();
    private readonly object _clientsLock = new();

    public WebSocketServer(ILogger<WebSocketServer> logger, GameServer gameServer)
    {
        _logger = logger;
        _gameServer = gameServer;
    }

    public async Task HandleWebSocketAsync(WebSocket webSocket)
    {
        var client = new WebSocketClient(webSocket);

        lock (_clientsLock)
        {
            _clients.Add(client);
        }

        _logger.LogInformation("WebSocket client connected (Total: {Count})", _clients.Count);

        try
        {
            // Wait for chunk requests instead of sending all world data
            _logger.LogInformation("Waiting for chunk requests from WebSocket client");

            // Receive messages (handle multi-frame messages)
            var messageBuffer = new List<byte>();
            var buffer = new byte[4096];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    // Accumulate message fragments
                    messageBuffer.AddRange(buffer.Take(result.Count));

                    // Only process when we have the complete message
                    if (result.EndOfMessage)
                    {
                        var messageJson = Encoding.UTF8.GetString(messageBuffer.ToArray());
                        await HandleMessageAsync(client, messageJson);
                        messageBuffer.Clear();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket error");
        }
        finally
        {
            lock (_clientsLock)
            {
                _clients.Remove(client);
            }
            _logger.LogInformation("WebSocket client disconnected (Remaining: {Count})", _clients.Count);
        }
    }

    private async Task HandleMessageAsync(WebSocketClient client, string messageJson)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(messageJson);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
                return;

            var messageType = typeElement.GetString();

            switch (messageType)
            {
                case "ClientConnect":
                    var playerName = root.GetProperty("playerName").GetString() ?? "Unknown";
                    client.PlayerName = playerName;
                    _logger.LogInformation("Player joined: {PlayerName}", playerName);
                    break;

                case "ChunkRequest":
                    var chunkPositions = root.GetProperty("chunkPositions");
                    var chunks = new List<Vector2i>();

                    foreach (var chunkPos in chunkPositions.EnumerateArray())
                    {
                        var chunkX = chunkPos.GetProperty("x").GetInt32();
                        var chunkZ = chunkPos.GetProperty("z").GetInt32();
                        chunks.Add(new Vector2i(chunkX, chunkZ));
                    }

                    // Send requested chunks
                    await SendChunksAsync(client, chunks);
                    break;

                case "BlockUpdate":
                    var x = root.GetProperty("x").GetInt32();
                    var y = root.GetProperty("y").GetInt32();
                    var z = root.GetProperty("z").GetInt32();
                    var blockType = (BlockType)root.GetProperty("blockType").GetByte();

                    // Update world state
                    _gameServer.SetBlock(x, y, z, blockType);

                    // Broadcast to all WebSocket clients
                    await BroadcastBlockUpdateToWebSocketClients(x, y, z, blockType);

                    // Also broadcast to LiteNetLib clients via GameServer
                    _gameServer.BroadcastBlockUpdateToLiteNetLibClients(x, y, z, blockType);

                    _logger.LogInformation("Block updated at ({X},{Y},{Z}) to {BlockType} (broadcasted to all clients)", x, y, z, blockType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WebSocket message: {Message}", messageJson);
        }
    }

    private async Task SendChunksAsync(WebSocketClient client, List<Vector2i> chunkPositions)
    {
        foreach (var chunkPos in chunkPositions)
        {
            var blocks = _gameServer.GetChunkBlocks(chunkPos);

            var message = new
            {
                type = "ChunkData",
                chunkX = chunkPos.X,
                chunkZ = chunkPos.Z,
                blocks = blocks.Select(b => new
                {
                    x = b.X,
                    y = b.Y,
                    z = b.Z,
                    type = (byte)b.Type
                }).ToArray()
            };

            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);

            await client.Socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }

        _logger.LogInformation("Sent {ChunkCount} chunks to WebSocket client", chunkPositions.Count);
    }

    public async Task BroadcastBlockUpdateToWebSocketClients(int x, int y, int z, BlockType blockType)
    {
        var message = new
        {
            type = "BlockUpdate",
            x,
            y,
            z,
            blockType = (byte)blockType
        };

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);

        List<WebSocketClient> clientsCopy;
        lock (_clientsLock)
        {
            clientsCopy = new List<WebSocketClient>(_clients);
        }

        foreach (var client in clientsCopy)
        {
            try
            {
                if (client.Socket.State == WebSocketState.Open)
                {
                    await client.Socket.SendAsync(
                        new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting to WebSocket client");
            }
        }
    }

    private class WebSocketClient
    {
        public WebSocket Socket { get; }
        public string PlayerName { get; set; } = "Unknown";

        public WebSocketClient(WebSocket socket)
        {
            Socket = socket;
        }
    }
}
