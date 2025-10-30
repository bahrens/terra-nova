using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace TerraNova;

/// <summary>
/// Main game class that manages the window and game loop
/// </summary>
public class Game : GameWindow
{
    private Camera _camera = null!;
    private bool _firstMove = true;
    private Vector2 _lastMousePos;

    private Shader _shader = null!;
    private CubeMesh _testCube = null!;
    private Texture _grassTexture = null!;

    public Game(int width, int height, string title)
        : base(GameWindowSettings.Default,
               new NativeWindowSettings()
               {
                   ClientSize = (width, height),
                   Title = title,
                   Flags = ContextFlags.ForwardCompatible // Use modern OpenGL
               })
    {
    }

    /// <summary>
    /// Called once when the window is created. Use this for initialization.
    /// </summary>
    protected override void OnLoad()
    {
        base.OnLoad();

        // Set the clear color (background color) to sky blue
        GL.ClearColor(0.53f, 0.81f, 0.92f, 1.0f);

        // Enable depth testing so closer objects appear in front of farther ones
        GL.Enable(EnableCap.DepthTest);

        // Enable backface culling to improve performance
        // (don't render faces that are facing away from the camera)
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);

        // Initialize camera (start at position 0, 5, 10)
        _camera = new Camera(new Vector3(0.0f, 5.0f, 10.0f));

        // Capture and hide the mouse cursor for FPS controls
        CursorState = CursorState.Grabbed;

        // Load shaders
        string vertexShaderSource = File.ReadAllText("Shaders/basic.vert");
        string fragmentShaderSource = File.ReadAllText("Shaders/basic.frag");
        _shader = new Shader(vertexShaderSource, fragmentShaderSource);

        // Generate procedural texture for grass block
        byte[] grassPixels = TextureGenerator.GenerateTexture(BlockType.Grass, 16);
        _grassTexture = new Texture(16, 16, grassPixels);

        // Create a test cube at the origin
        _testCube = new CubeMesh(Vector3.Zero, BlockType.Grass);

        Console.WriteLine("OpenGL Version: " + GL.GetString(StringName.Version));
        Console.WriteLine("Terra Nova initialized!");
        Console.WriteLine("Controls: WASD to move, Space/Shift for up/down, Mouse to look, ESC to exit");
    }

    /// <summary>
    /// Called every frame to update game logic (physics, input, etc.)
    /// </summary>
    /// <param name="args">Contains delta time (time since last frame)</param>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // Close window when Escape is pressed
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        // Camera keyboard controls
        float deltaTime = (float)args.Time;

        if (KeyboardState.IsKeyDown(Keys.W))
            _camera.ProcessKeyboard(CameraMovement.Forward, deltaTime);
        if (KeyboardState.IsKeyDown(Keys.S))
            _camera.ProcessKeyboard(CameraMovement.Backward, deltaTime);
        if (KeyboardState.IsKeyDown(Keys.A))
            _camera.ProcessKeyboard(CameraMovement.Left, deltaTime);
        if (KeyboardState.IsKeyDown(Keys.D))
            _camera.ProcessKeyboard(CameraMovement.Right, deltaTime);
        if (KeyboardState.IsKeyDown(Keys.Space))
            _camera.ProcessKeyboard(CameraMovement.Up, deltaTime);
        if (KeyboardState.IsKeyDown(Keys.LeftShift))
            _camera.ProcessKeyboard(CameraMovement.Down, deltaTime);
    }

    /// <summary>
    /// Called every frame to render graphics
    /// </summary>
    /// <param name="args">Contains delta time (time since last frame)</param>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        // Clear the screen and depth buffer
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Activate our shader
        _shader.Use();

        // Bind the texture
        _grassTexture.Bind(0);
        _shader.SetInt("blockTexture", 0);

        // Set transformation matrices
        Matrix4 model = Matrix4.Identity; // Model matrix (no transformation for now)
        Matrix4 view = _camera.GetViewMatrix();
        Matrix4 projection = _camera.GetProjectionMatrix((float)ClientSize.X / ClientSize.Y);

        // Send matrices to shader
        _shader.SetMatrix4("model", model);
        _shader.SetMatrix4("view", view);
        _shader.SetMatrix4("projection", projection);

        // Draw the test cube
        _testCube.Draw();

        // Swap the front and back buffers (double buffering)
        SwapBuffers();
    }

    /// <summary>
    /// Called when the window is resized
    /// </summary>
    /// <param name="args">Contains new window size</param>
    protected override void OnResize(ResizeEventArgs args)
    {
        base.OnResize(args);

        // Update the OpenGL viewport to match the new window size
        GL.Viewport(0, 0, args.Width, args.Height);
    }

    /// <summary>
    /// Called when the mouse moves
    /// </summary>
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        if (_firstMove)
        {
            _lastMousePos = new Vector2(e.X, e.Y);
            _firstMove = false;
        }

        // Calculate mouse movement delta
        float deltaX = e.X - _lastMousePos.X;
        float deltaY = _lastMousePos.Y - e.Y; // Reversed: y-coordinates go from bottom to top

        _lastMousePos = new Vector2(e.X, e.Y);

        // Update camera rotation
        _camera.ProcessMouseMovement(deltaX, deltaY);
    }

    /// <summary>
    /// Called when the window is about to close
    /// </summary>
    protected override void OnUnload()
    {
        base.OnUnload();

        // Clean up resources
        _shader?.Dispose();
        _testCube?.Dispose();
        _grassTexture?.Dispose();

        Console.WriteLine("Terra Nova shutting down...");
    }
}
