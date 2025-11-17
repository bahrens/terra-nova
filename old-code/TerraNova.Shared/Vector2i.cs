namespace TerraNova.Shared;

/// <summary>
/// Simple 2D integer vector for chunk column positions (X, Z only)
/// Used for Minecraft-style 2D chunk columns instead of 3D chunks
/// </summary>
public struct Vector2i : IEquatable<Vector2i>
{
    public int X { get; set; }
    public int Z { get; set; }

    public Vector2i(int x, int z)
    {
        X = x;
        Z = z;
    }

    public bool Equals(Vector2i other)
    {
        return X == other.X && Z == other.Z;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector2i other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Z);
    }

    public static bool operator ==(Vector2i left, Vector2i right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector2i left, Vector2i right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"({X}, {Z})";
    }
}
