using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using TerraNova.Shared;

namespace TerraNova;

/// <summary>
/// Renders a simple crosshair in the center of the screen
/// </summary>
public class Crosshair : IDisposable
{
    private int _vao;
    private int _vbo;
    private Shader _shader = null!;
    private int _windowWidth;
    private int _windowHeight;

    public void Initialize(int windowWidth, int windowHeight)
    {
        _windowWidth = windowWidth;
        _windowHeight = windowHeight;
        InitializeInternal();
    }

    private void InitializeInternal()
    {
        // Create shader for 2D rendering in pixel coordinates
        string vertexShaderSource = @"
#version 330 core
layout (location = 0) in vec2 aPosition;

void main()
{
    gl_Position = vec4(aPosition, 0.0, 1.0);
}
";

        string fragmentShaderSource = @"
#version 330 core
out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0, 1.0, 1.0, 0.8); // White with slight transparency
}
";

        _shader = new Shader(vertexShaderSource, fragmentShaderSource);

        // Convert pixel dimensions to normalized device coordinates
        // For odd pixel lengths, the crosshair is centered pixel-perfect
        float halfLength = UIConstants.Crosshair.Length / 2.0f;
        float halfThickness = UIConstants.Crosshair.Thickness / 2.0f;

        // Horizontal bar (in pixels relative to center)
        float hLeft = -halfLength;
        float hRight = halfLength;
        float hTop = halfThickness;
        float hBottom = -halfThickness;

        // Vertical bar (in pixels relative to center)
        float vLeft = -halfThickness;
        float vRight = halfThickness;
        float vTop = halfLength;
        float vBottom = -halfLength;

        // Convert to NDC (will be done in UpdateForResize)
        // For now, store in pixels - we'll convert when we know window size
        float[] vertices = new float[]
        {
            // Horizontal bar (two triangles)
            hLeft, hBottom,   // Bottom-left
            hRight, hBottom,  // Bottom-right
            hRight, hTop,     // Top-right
            hLeft, hBottom,   // Bottom-left
            hRight, hTop,     // Top-right
            hLeft, hTop,      // Top-left

            // Vertical bar (two triangles)
            vLeft, vBottom,   // Bottom-left
            vRight, vBottom,  // Bottom-right
            vRight, vTop,     // Top-right
            vLeft, vBottom,   // Bottom-left
            vRight, vTop,     // Top-right
            vLeft, vTop       // Top-left
        };

        // Convert pixel coordinates to NDC
        for (int i = 0; i < vertices.Length; i += 2)
        {
            vertices[i] = vertices[i] / (_windowWidth / 2.0f);     // X to NDC
            vertices[i + 1] = vertices[i + 1] / (_windowHeight / 2.0f); // Y to NDC
        }

        // Create VAO and VBO
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);
    }

    public void OnResize(int windowWidth, int windowHeight)
    {
        _windowWidth = windowWidth;
        _windowHeight = windowHeight;

        // Recreate the crosshair with new window dimensions
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        InitializeInternal();
    }

    public void Draw(float aspectRatio)
    {
        // Disable depth test for 2D overlay
        GL.Disable(EnableCap.DepthTest);

        _shader.Use();
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 12); // 12 vertices (2 triangles per bar * 2 bars)
        GL.BindVertexArray(0);

        // Re-enable depth test
        GL.Enable(EnableCap.DepthTest);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        _shader?.Dispose();
    }
}
