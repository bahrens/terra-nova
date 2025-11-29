using System.Numerics;
using System.Runtime.InteropServices;

namespace TerraNova.Client.Rendering;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Vertex
{
    public Vertex(Vector3 position, Vector3 normal, Vector2 texCoord)
    {
        Position = position;
        Normal = normal;
        TexCoord = texCoord;
    }

    public readonly Vector3 Position; // location 0: aPosition
    public readonly Vector3 Normal;  // location 1: aNormal
    public readonly Vector2 TexCoord; // location 2: aTexCoord

    public static readonly int SizeInBytes = Marshal.SizeOf<Vertex>();
    public static readonly int PositionOffset = Marshal.OffsetOf<Vertex>(nameof(Position)).ToInt32();
    public static readonly int NormalOffset = Marshal.OffsetOf<Vertex>(nameof(Normal)).ToInt32();
    public static readonly int TexCoordOffset = Marshal.OffsetOf<Vertex>(nameof(TexCoord)).ToInt32();
}
