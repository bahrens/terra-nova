namespace TerraNova.Client
{
    public record Viewport(int x, int y, int width, int height)
    {
        public static Viewport Fullscreen(int width, int height) => new Viewport(0, 0, width, height);
    }
}
