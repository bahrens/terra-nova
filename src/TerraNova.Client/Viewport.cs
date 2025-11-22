namespace TerraNova.Client;

public record Viewport
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }


    public Viewport(int x, int y, int width, int height)
    {
        if (width < 0)
        {
            throw new ArgumentException("Width cannot be negative.", nameof(width));
        }

        if (height < 0)
        {
            throw new ArgumentException("Height cannot be negative.", nameof(height));
        }

        if (x < 0)
        {
            throw new ArgumentException("X cannot be negative.", nameof(x));
        }

        if (y < 0)
        {
            throw new ArgumentException("Y cannot be negative.", nameof(y));
        }
    }

    public static Viewport Fullscreen(int width, int height) => new Viewport(0, 0, width, height);
}
