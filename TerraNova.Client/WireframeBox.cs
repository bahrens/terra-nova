using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace TerraNova;

/// <summary>
/// Renders a wireframe box around a block to highlight it
/// </summary>
public class WireframeBox : IDisposable
{
    private int _vao;
    private int _vbo;
    private Shader _shader = null!;

    public void Initialize()
    {
        // Create shader for wireframe rendering
        string vertexShaderSource = @"
#version 330 core
layout (location = 0) in vec3 aPosition;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    gl_Position = projection * view * model * vec4(aPosition, 1.0);
}
";

        string fragmentShaderSource = @"
#version 330 core
out vec4 FragColor;

void main()
{
    FragColor = vec4(0.0, 0.0, 0.0, 1.0); // Black outline
}
";

        _shader = new Shader(vertexShaderSource, fragmentShaderSource);

        // Define the 12 edges of a centered unit cube, slightly larger than the block
        // to prevent z-fighting with block faces
        float s = 0.501f; // Slightly larger than 0.5
        float[] vertices = new float[]
        {
            // Bottom face edges (y = -s)
            -s, -s, -s,  s, -s, -s, // Front bottom
            s, -s, -s,  s, -s, s, // Right bottom
            s, -s, s,  -s, -s, s, // Back bottom
            -s, -s, s,  -s, -s, -s, // Left bottom

            // Top face edges (y = s)
            -s, s, -s,  s, s, -s, // Front top
            s, s, -s,  s, s, s, // Right top
            s, s, s,  -s, s, s, // Back top
            -s, s, s,  -s, s, -s, // Left top

            // Vertical edges
            -s, -s, -s,  -s, s, -s, // Front left
            s, -s, -s,  s, s, -s, // Front right
            s, -s, s,  s, s, s, // Back right
            -s, -s, s,  -s, s, s  // Back left
        };

        // Create VAO and VBO
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);
    }

    public void Draw(Vector3 position, Matrix4 view, Matrix4 projection)
    {
        // Create model matrix to position the wireframe at the block position
        Matrix4 model = Matrix4.CreateTranslation(position);

        // Use polygon offset to render wireframe slightly in front to avoid z-fighting
        GL.Enable(EnableCap.PolygonOffsetLine);
        GL.PolygonOffset(-2.0f, -2.0f);

        // Set line width for better visibility
        GL.LineWidth(2.0f);

        _shader.Use();
        _shader.SetMatrix4("model", model);
        _shader.SetMatrix4("view", view);
        _shader.SetMatrix4("projection", projection);

        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Lines, 0, 24); // 12 edges * 2 vertices each
        GL.BindVertexArray(0);

        // Reset line width and polygon offset
        GL.LineWidth(1.0f);
        GL.Disable(EnableCap.PolygonOffsetLine);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        _shader?.Dispose();
    }
}
