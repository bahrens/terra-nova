namespace TerraNova.Client;

public record Viewport
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }

    public Viewport(int x, int y, int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width cannot be negative or zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height cannot be negative or zero.");
        }

        if (x < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "X cannot be negative.");
        }

        if (y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(y), "Y cannot be negative.");
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public static Viewport Fullscreen(int width, int height) => new Viewport(0, 0, width, height);
}
