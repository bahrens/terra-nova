namespace TerraNova.Client;

public readonly struct Vector2i
{
    public int X { get; init; }
    public int Z { get; init; }

    public Vector2i(int x, int z)
    {
        X = x;
        Z = z;
    }
}
