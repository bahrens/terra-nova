namespace TerraNova.Shared;

/// <summary>
/// Simple 3D float vector for positions, directions, etc.
/// Platform-agnostic replacement for OpenTK.Mathematics.Vector3
/// </summary>
public struct Vector3 : IEquatable<Vector3>
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public readonly float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

    public readonly float LengthSquared() => X * X + Y * Y + Z * Z;

    public readonly Vector3 Normalized()
    {
        float length = Length;
        if (length > 0)
            return new Vector3(X / length, Y / length, Z / length);
        return this;
    }

    public static Vector3 Normalize(Vector3 vector)
    {
        return vector.Normalized();
    }

    /// <summary>
    /// Calculate the dot product of two vectors.
    /// </summary>
    public static float Dot(Vector3 left, Vector3 right)
    {
        return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
    }

    public static readonly Vector3 Zero = new Vector3(0, 0, 0);

    public readonly bool Equals(Vector3 other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Vector3 other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    public static bool operator ==(Vector3 left, Vector3 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector3 left, Vector3 right)
    {
        return !left.Equals(right);
    }

    public static Vector3 operator +(Vector3 left, Vector3 right)
    {
        return new Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    public static Vector3 operator -(Vector3 left, Vector3 right)
    {
        return new Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    public static Vector3 operator *(Vector3 vector, float scalar)
    {
        return new Vector3(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
    }

    public static Vector3 operator *(float scalar, Vector3 vector)
    {
        return vector * scalar;
    }

    public override readonly string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
}
