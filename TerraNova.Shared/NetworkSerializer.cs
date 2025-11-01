using LiteNetLib.Utils;

namespace TerraNova.Shared;

/// <summary>
/// Serialization extensions for network messages
/// </summary>
public static class NetworkSerializer
{
    // ClientConnectMessage
    public static void Put(this NetDataWriter writer, ClientConnectMessage message)
    {
        writer.Put(message.PlayerName);
    }

    public static ClientConnectMessage GetClientConnectMessage(this NetDataReader reader)
    {
        return new ClientConnectMessage(reader.GetString());
    }

    // WorldDataMessage
    public static void Put(this NetDataWriter writer, WorldDataMessage message)
    {
        writer.Put(message.Blocks.Length);
        foreach (var block in message.Blocks)
        {
            writer.Put(block);
        }
    }

    public static WorldDataMessage GetWorldDataMessage(this NetDataReader reader)
    {
        int count = reader.GetInt();
        var blocks = new BlockData[count];
        for (int i = 0; i < count; i++)
        {
            blocks[i] = reader.GetBlockData();
        }
        return new WorldDataMessage(blocks);
    }

    // BlockData
    public static void Put(this NetDataWriter writer, BlockData block)
    {
        writer.Put(block.X);
        writer.Put(block.Y);
        writer.Put(block.Z);
        writer.Put((byte)block.Type);
    }

    public static BlockData GetBlockData(this NetDataReader reader)
    {
        return new BlockData(
            reader.GetInt(),
            reader.GetInt(),
            reader.GetInt(),
            (BlockType)reader.GetByte()
        );
    }

    // BlockUpdateMessage
    public static void Put(this NetDataWriter writer, BlockUpdateMessage message)
    {
        writer.Put(message.X);
        writer.Put(message.Y);
        writer.Put(message.Z);
        writer.Put((byte)message.NewType);
    }

    public static BlockUpdateMessage GetBlockUpdateMessage(this NetDataReader reader)
    {
        return new BlockUpdateMessage(
            reader.GetInt(),
            reader.GetInt(),
            reader.GetInt(),
            (BlockType)reader.GetByte()
        );
    }
}
