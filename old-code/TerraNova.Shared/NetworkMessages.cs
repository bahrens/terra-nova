namespace TerraNova.Shared;

/// <summary>
/// Message type identifiers for network packets
/// </summary>
public enum MessageType : byte
{
    // Client -> Server
    ClientConnect = 1,
    ChunkRequest = 2,
    PlayerPosition = 3,

    // Server -> Client
    WorldData = 10,
    BlockUpdate = 11,
    ChunkData = 12,

    // Bidirectional
    Disconnect = 255
}

/// <summary>
/// Client connection request
/// </summary>
public struct ClientConnectMessage
{
    public string PlayerName;

    public ClientConnectMessage(string playerName)
    {
        PlayerName = playerName;
    }
}

/// <summary>
/// World data sent from server to client
/// Contains all blocks in a region
/// </summary>
public struct WorldDataMessage
{
    public BlockData[] Blocks;

    public WorldDataMessage(BlockData[] blocks)
    {
        Blocks = blocks;
    }
}

/// <summary>
/// Represents a single block in the world for network transmission
/// </summary>
public struct BlockData
{
    public int X;
    public int Y;
    public int Z;
    public BlockType Type;

    public BlockData(int x, int y, int z, BlockType type)
    {
        X = x;
        Y = y;
        Z = z;
        Type = type;
    }
}

/// <summary>
/// Single block update (placed or broken)
/// </summary>
public struct BlockUpdateMessage
{
    public int X;
    public int Y;
    public int Z;
    public BlockType NewType;

    public BlockUpdateMessage(int x, int y, int z, BlockType newType)
    {
        X = x;
        Y = y;
        Z = z;
        NewType = newType;
    }
}

/// <summary>
/// Client requests specific chunks from the server (2D positions)
/// </summary>
public struct ChunkRequestMessage
{
    public Vector2i[] ChunkPositions;

    public ChunkRequestMessage(Vector2i[] chunkPositions)
    {
        ChunkPositions = chunkPositions;
    }
}

/// <summary>
/// Server sends chunk data to client (2D column chunk)
/// </summary>
public struct ChunkDataMessage
{
    public Vector2i ChunkPosition;
    public BlockData[] Blocks;

    public ChunkDataMessage(Vector2i chunkPosition, BlockData[] blocks)
    {
        ChunkPosition = chunkPosition;
        Blocks = blocks;
    }
}

/// <summary>
/// Client notifies server of player position for chunk loading
/// </summary>
public struct PlayerPositionMessage
{
    public float X;
    public float Y;
    public float Z;

    public PlayerPositionMessage(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
