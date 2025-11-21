namespace TerraNova.Client;

public record Viewport(int X, int Y, int Width, int Height)
{
    public static Viewport Fullscreen(int Width, int Height) => new Viewport(0, 0, Width, Height);
}