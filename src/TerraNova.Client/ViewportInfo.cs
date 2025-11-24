namespace TerraNova.Client;

public readonly struct ViewportInfo
{
    public int Width { get; init; }
    public int Height { get; init; }
    public float AspectRatio => Width / (float)Height;

    public ViewportInfo(int width, int height)
    {
        Width = width;
        Height = height;
    }
}
