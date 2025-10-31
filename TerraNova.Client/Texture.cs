using OpenTK.Graphics.OpenGL4;

namespace TerraNova;

/// <summary>
/// Manages an OpenGL texture
/// </summary>
public class Texture : IDisposable
{
    private readonly int _handle;
    private bool _disposed = false;

    public int Width { get; }
    public int Height { get; }

    /// <summary>
    /// Creates a texture from RGBA pixel data
    /// </summary>
    /// <param name="width">Texture width in pixels</param>
    /// <param name="height">Texture height in pixels</param>
    /// <param name="pixels">RGBA pixel data (4 bytes per pixel)</param>
    public Texture(int width, int height, byte[] pixels)
    {
        Width = width;
        Height = height;

        // Generate texture
        _handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _handle);

        // Upload pixel data
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                      width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

        // Set texture parameters (for pixelated Minecraft-like look)
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    /// <summary>
    /// Binds this texture for rendering
    /// </summary>
    /// <param name="unit">Texture unit to bind to (0-31)</param>
    public void Bind(int unit = 0)
    {
        GL.ActiveTexture(TextureUnit.Texture0 + unit);
        GL.BindTexture(TextureTarget.Texture2D, _handle);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            GL.DeleteTexture(_handle);
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
