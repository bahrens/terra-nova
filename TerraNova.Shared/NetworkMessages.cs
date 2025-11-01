namespace TerraNova.Shared;

/// <summary>
/// Message type identifiers for network packets
/// </summary>
public enum MessageType : byte
{
    // Client -> Server
    ClientConnect = 1,

    // Server -> Client
    WorldData = 10,
    BlockUpdate = 11,

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
