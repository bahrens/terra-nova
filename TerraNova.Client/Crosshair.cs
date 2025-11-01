using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace TerraNova;

/// <summary>
/// Renders a simple crosshair in the center of the screen
/// </summary>
public class Crosshair : IDisposable
{
    private int _vao;
    private int _vbo;
    private Shader _shader = null!;
    private const float CrosshairSize = 0.02f; // Size in normalized device coordinates

    public void Initialize()
    {
        // Create shader for 2D rendering
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

        // Define crosshair lines (two lines forming a +)
        float[] vertices = new float[]
        {
            // Horizontal line
            -CrosshairSize, 0.0f,
            CrosshairSize, 0.0f,

            // Vertical line
            0.0f, -CrosshairSize,
            0.0f, CrosshairSize
        };

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

    public void Draw()
    {
        // Disable depth test for 2D overlay
        GL.Disable(EnableCap.DepthTest);

        _shader.Use();
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Lines, 0, 4);
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
