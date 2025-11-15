using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using TerraNova.Shared;

namespace TerraNova;

/// <summary>
/// Renders the hotbar UI at the bottom of the screen showing block selection slots
/// </summary>
public class Hotbar : IDisposable
{
    private int _vao;
    private int _vbo;
    private Shader _shader = null!;
    private Shader _coloredShader = null!;
    private int _windowWidth;
    private int _windowHeight;
    private int _selectedSlot;
    private readonly BlockType[] _blocks;

    public Hotbar(BlockType[] blocks)
    {
        _blocks = blocks;
    }

    public void Initialize(int windowWidth, int windowHeight, int selectedSlot)
    {
        _windowWidth = windowWidth;
        _windowHeight = windowHeight;
        _selectedSlot = selectedSlot;
        InitializeShaders();
        InitializeGeometry();
    }

    private void InitializeShaders()
    {
        // Load shaders from files
        string vertexShaderSource = File.ReadAllText("Shaders/ui.vert");
        string fragmentShaderSource = File.ReadAllText("Shaders/ui_hotbar.frag");
        string coloredFragmentShaderSource = File.ReadAllText("Shaders/ui_colored.frag");

        _shader = new Shader(vertexShaderSource, fragmentShaderSource);
        _coloredShader = new Shader(vertexShaderSource, coloredFragmentShaderSource);
    }

    private void InitializeGeometry()
    {
        // Calculate total width of hotbar
        float totalWidth = UIConstants.Hotbar.SlotCount * UIConstants.Hotbar.SlotSize +
                          (UIConstants.Hotbar.SlotCount - 1) * UIConstants.Hotbar.SlotSpacing;

        // Position hotbar at bottom center
        float startX = (_windowWidth - totalWidth) / 2.0f;

        // We'll use a simple quad (2 triangles) for drawing rectangles
        // We'll update the vertices for each slot in the Draw method
        float[] vertices = new float[]
        {
            0, 0,  // Bottom-left
            0, 0,  // Bottom-right
            0, 0,  // Top-right
            0, 0,  // Bottom-left
            0, 0,  // Top-right
            0, 0   // Top-left
        };

        // Create VAO and VBO
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);
    }

    public void UpdateSelectedSlot(int selectedSlot)
    {
        _selectedSlot = selectedSlot;
    }

    public void OnResize(int windowWidth, int windowHeight)
    {
        _windowWidth = windowWidth;
        _windowHeight = windowHeight;
    }

    public void Draw()
    {
        // Disable depth test for 2D overlay
        GL.Disable(EnableCap.DepthTest);

        // Calculate total width of hotbar
        float totalWidth = UIConstants.Hotbar.SlotCount * UIConstants.Hotbar.SlotSize +
                          (UIConstants.Hotbar.SlotCount - 1) * UIConstants.Hotbar.SlotSpacing;

        // Position hotbar at bottom center
        float startX = (_windowWidth - totalWidth) / 2.0f;

        GL.BindVertexArray(_vao);

        // Draw each slot
        for (int i = 0; i < UIConstants.Hotbar.SlotCount; i++)
        {
            float slotX = startX + i * (UIConstants.Hotbar.SlotSize + UIConstants.Hotbar.SlotSpacing);
            float slotY = UIConstants.Hotbar.BottomMargin;

            bool isSelected = i == _selectedSlot;
            float borderThickness = isSelected ? UIConstants.Hotbar.SelectedBorderThickness : UIConstants.Hotbar.BorderThickness;

            // Draw slot background (block color)
            var blockColor = BlockHelper.GetBlockColor(_blocks[i]);
            DrawRectangle(slotX + borderThickness, slotY + borderThickness,
                         UIConstants.Hotbar.SlotSize - 2 * borderThickness,
                         UIConstants.Hotbar.SlotSize - 2 * borderThickness,
                         blockColor.r, blockColor.g, blockColor.b, true);

            // Draw slot border (white)
            DrawRectangleBorder(slotX, slotY, UIConstants.Hotbar.SlotSize, UIConstants.Hotbar.SlotSize, borderThickness);
        }

        GL.BindVertexArray(0);

        // Re-enable depth test
        GL.Enable(EnableCap.DepthTest);
    }

    private void DrawRectangle(float x, float y, float width, float height, float r, float g, float b, bool filled)
    {
        // Convert pixel coordinates to NDC
        float left = (x / _windowWidth) * 2.0f - 1.0f;
        float right = ((x + width) / _windowWidth) * 2.0f - 1.0f;
        float bottom = (y / _windowHeight) * 2.0f - 1.0f;
        float top = ((y + height) / _windowHeight) * 2.0f - 1.0f;

        float[] vertices = new float[]
        {
            left, bottom,   // Bottom-left
            right, bottom,  // Bottom-right
            right, top,     // Top-right
            left, bottom,   // Bottom-left
            right, top,     // Top-right
            left, top       // Top-left
        };

        // Update VBO with new vertices
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertices.Length * sizeof(float), vertices);

        // Use colored shader and set color uniform
        _coloredShader.Use();
        int colorLocation = GL.GetUniformLocation(GL.GetInteger(GetPName.CurrentProgram), "uColor");
        GL.Uniform3(colorLocation, r, g, b);

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    private void DrawRectangleBorder(float x, float y, float width, float height, float thickness)
    {
        // Draw 4 rectangles for the border (top, bottom, left, right)

        // Top border
        DrawBorderSegment(x, y + height - thickness, width, thickness);

        // Bottom border
        DrawBorderSegment(x, y, width, thickness);

        // Left border
        DrawBorderSegment(x, y, thickness, height);

        // Right border
        DrawBorderSegment(x + width - thickness, y, thickness, height);
    }

    private void DrawBorderSegment(float x, float y, float width, float height)
    {
        // Convert pixel coordinates to NDC
        float left = (x / _windowWidth) * 2.0f - 1.0f;
        float right = ((x + width) / _windowWidth) * 2.0f - 1.0f;
        float bottom = (y / _windowHeight) * 2.0f - 1.0f;
        float top = ((y + height) / _windowHeight) * 2.0f - 1.0f;

        float[] vertices = new float[]
        {
            left, bottom,   // Bottom-left
            right, bottom,  // Bottom-right
            right, top,     // Top-right
            left, bottom,   // Bottom-left
            right, top,     // Top-right
            left, top       // Top-left
        };

        // Update VBO with new vertices
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertices.Length * sizeof(float), vertices);

        // Use white shader for borders
        _shader.Use();
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        _shader?.Dispose();
        _coloredShader?.Dispose();
    }
}
