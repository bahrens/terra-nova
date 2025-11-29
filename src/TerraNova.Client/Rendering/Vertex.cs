using System.Numerics;
using System.Runtime.InteropServices;

namespace TerraNova.Client.Rendering;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3 Position; // locatin 0: aPosition
    public Vector3 Normal;  // location 1: aNormal
    public Vector2 TexCoord; // location 2: aTexCoord

    public static readonly int SizeInBytes = Marshal.SizeOf<Vertex>();
}
