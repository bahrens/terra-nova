using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TerraNova.Configuration;

namespace TerraNova;

/// <summary>
/// Thin OpenTK adapter layer that bridges OpenTK events to ClientApplication.
/// This class handles only OpenTK-specific concerns (window lifecycle, input events).
/// All game logic is delegated to ClientApplication.
/// </summary>
public class Game : GameWindow
{
    private readonly ClientApplication _application;
    private readonly ILogger<Game> _logger;

    // Mouse tracking state
    private bool _firstMove = true;
    private Vector2 _lastMousePos;

    public Game(
        IOptions<GameSettings> gameSettings,
        ClientApplication application,
        ILogger<Game> logger)
        : base(GameWindowSettings.Default,
               new NativeWindowSettings()
               {
                   ClientSize = (gameSettings.Value.WindowWidth, gameSettings.Value.WindowHeight),
                   Title = gameSettings.Value.WindowTitle,
                   Flags = ContextFlags.ForwardCompatible
               })
    {
        _application = application;
        _logger = logger;
    }

    /// <summary>
    /// Called once when the window is created. Initializes OpenGL and delegates to ClientApplication.
    /// </summary>
    protected override void OnLoad()
    {
        base.OnLoad();

        // OpenGL setup
        GL.ClearColor(0.53f, 0.81f, 0.92f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);

        // Capture and hide the mouse cursor for FPS controls
        CursorState = CursorState.Grabbed;

        // Initialize application
        _application.Initialize(ClientSize.X, ClientSize.Y);

        _logger.LogInformation("OpenGL Version: {Version}", GL.GetString(StringName.Version));

        // Check max line width supported
        float[] lineWidthRange = new float[2];
        GL.GetFloat(GetPName.AliasedLineWidthRange, lineWidthRange);
        _logger.LogInformation("Line width range: {Min} - {Max}", lineWidthRange[0], lineWidthRange[1]);

        _logger.LogInformation("Terra Nova initialized!");
        _logger.LogInformation("Controls: WASD to move, Space/Shift for up/down, Mouse to look, F11 for fullscreen, ESC to exit");
        _logger.LogInformation("Debug: F3 for position diagnostics, J to toggle auto-jump");
    }

    /// <summary>
    /// Called every frame to update game logic. Delegates to ClientApplication.
    /// </summary>
    /// <param name="args">Contains delta time (time since last frame)</param>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // Update window title with FPS
        Title = $"Terra Nova - FPS: {_application.CurrentFPS:F0}";

        // Handle ESC to close
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        // Toggle fullscreen with F11
        if (KeyboardState.IsKeyPressed(Keys.F11))
        {
            if (WindowState == WindowState.Fullscreen)
            {
                WindowState = WindowState.Normal;
                _logger.LogInformation("Switched to windowed mode");
            }
            else
            {
                WindowState = WindowState.Fullscreen;
                _logger.LogInformation("Switched to fullscreen mode");
            }
        }

        // Delegate all game logic to application
        _application.Update(KeyboardState, MouseState, args.Time);
    }

    /// <summary>
    /// Called every frame to render graphics. Delegates to ClientApplication.
    /// </summary>
    /// <param name="args">Contains delta time (time since last frame)</param>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        // Clear the screen and depth buffer
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Delegate rendering to application
        float aspectRatio = (float)ClientSize.X / ClientSize.Y;
        _application.Render(ClientSize.X, ClientSize.Y, aspectRatio);

        // Swap buffers
        SwapBuffers();
    }

    /// <summary>
    /// Called when the window is resized. Delegates to ClientApplication.
    /// </summary>
    /// <param name="args">Contains new window size</param>
    protected override void OnResize(ResizeEventArgs args)
    {
        base.OnResize(args);

        // Update OpenGL viewport
        GL.Viewport(0, 0, args.Width, args.Height);

        // Delegate to application
        _application.OnResize(args.Width, args.Height);
    }

    /// <summary>
    /// Called when the mouse moves. Delegates to ClientApplication.
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

        // Delegate to application
        _application.OnMouseMove(deltaX, deltaY);
    }

    /// <summary>
    /// Called when the window is about to close. Cleans up resources.
    /// </summary>
    protected override void OnUnload()
    {
        base.OnUnload();

        _application?.Dispose();
        _logger.LogInformation("Terra Nova shutting down...");
    }
}
