namespace TerraNova.Shared;

/// <summary>
/// Simple 3D integer vector for block positions
/// </summary>
public struct Vector3i : IEquatable<Vector3i>
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public Vector3i(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public bool Equals(Vector3i other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector3i other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    public static bool operator ==(Vector3i left, Vector3i right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector3i left, Vector3i right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
}
