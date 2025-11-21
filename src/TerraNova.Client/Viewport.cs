namespace TerraNova.Client;

public record Viewport(int X, int Y, int Width, int Height)
{
    public static Viewport Fullscreen(int width, int height) => new Viewport(0, 0, width, height);
}
