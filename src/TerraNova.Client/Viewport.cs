namespace TerraNova.Client;

public record Viewport(int X, int Y, int Width, int Height)
{
    public Viewport(int width, int height) : this(0, 0, width, height)
    {
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be non-negative.");
        }

        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be non-negative.");
        }
    }

    public static Viewport Fullscreen(int width, int height) => new Viewport(0, 0, width, height);
}
