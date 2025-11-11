using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TerraNova.Configuration;
using TerraNova.GameLogic;
using TerraNova.Shared;
using OpenTKVector3 = OpenTK.Mathematics.Vector3;

namespace TerraNova;

/// <summary>
/// Main game class that manages the window and game loop
/// </summary>
public class Game : GameWindow
{
    private Camera _camera = null!;
    private bool _firstMove = true;
    private Vector2 _lastMousePos;

    private readonly INetworkClient _networkClient;
    private readonly NetworkSettings _networkSettings;
    private readonly CameraSettings _cameraSettings;
    private readonly ILogger<Game> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private Crosshair _crosshair = null!;
    private Hotbar _hotbar = null!;

    // New architecture: GameEngine + OpenTKRenderer
    private World _world = null!;
    private OpenTKRenderer _renderer = null!;
    private GameEngine _gameEngine = null!;

    // Player controller for input handling and interaction
    private PlayerController? _playerController = null;

    // FPS tracking
    private double _fpsUpdateTimer = 0.0;
    private int _frameCount = 0;
    private double _fps = 0.0;

    public Game(
        IOptions<GameSettings> gameSettings,
        IOptions<NetworkSettings> networkSettings,
        IOptions<CameraSettings> cameraSettings,
        INetworkClient networkClient,
        ILogger<Game> logger,
        ILoggerFactory loggerFactory)
        : base(GameWindowSettings.Default,
               new NativeWindowSettings()
               {
                   ClientSize = (gameSettings.Value.WindowWidth, gameSettings.Value.WindowHeight),
                   Title = gameSettings.Value.WindowTitle,
                   Flags = ContextFlags.ForwardCompatible // Use modern OpenGL
               })
    {
        _networkClient = networkClient;
        _networkSettings = networkSettings.Value;
        _cameraSettings = cameraSettings.Value;
        _logger = logger;
        _loggerFactory = loggerFactory;
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

        // Initialize camera with configured settings (spawn position from config)
        _camera = new Camera(new OpenTKVector3(_cameraSettings.SpawnX, _cameraSettings.SpawnY, _cameraSettings.SpawnZ))
        {
            Speed = _cameraSettings.MovementSpeed,
            Sensitivity = _cameraSettings.MouseSensitivity,
            Fov = MathHelper.DegreesToRadians(_cameraSettings.FieldOfView)
        };

        // Capture and hide the mouse cursor for FPS controls
        CursorState = CursorState.Grabbed;

        // Note: GameEngine and Renderer will be created in OnUpdateFrame
        // after receiving world data from the network client

        // Initialize PlayerController early (before UI) so we can access hotbar blocks
        _playerController = new PlayerController(_camera, _networkClient, _loggerFactory.CreateLogger<PlayerController>());

        // Initialize UI overlays
        _crosshair = new Crosshair();
        _crosshair.Initialize(ClientSize.X, ClientSize.Y);

        _hotbar = new Hotbar(_playerController.HotbarBlocks);
        _hotbar.Initialize(ClientSize.X, ClientSize.Y, _playerController.SelectedHotbarSlot);

        // Connect to server using configured settings
        _networkClient.Connect(_networkSettings.ServerHost, _networkSettings.ServerPort, _networkSettings.PlayerName);
        _logger.LogInformation("Connecting to {Host}:{Port}...", _networkSettings.ServerHost, _networkSettings.ServerPort);

        _logger.LogInformation("OpenGL Version: {Version}", GL.GetString(StringName.Version));

        // Check max line width supported
        float[] lineWidthRange = new float[2];
        GL.GetFloat(GetPName.AliasedLineWidthRange, lineWidthRange);
        _logger.LogInformation("Line width range: {Min} - {Max}", lineWidthRange[0], lineWidthRange[1]);

        _logger.LogInformation("Terra Nova initialized!");
        _logger.LogInformation("Controls: WASD to move, Space/Shift for up/down, Mouse to look, F11 for fullscreen, ESC to exit");
    }

    /// <summary>
    /// Called every frame to update game logic (physics, input, etc.)
    /// </summary>
    /// <param name="args">Contains delta time (time since last frame)</param>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // Update FPS counter
        _frameCount++;
        _fpsUpdateTimer += args.Time;
        if (_fpsUpdateTimer >= 0.5) // Update every 0.5 seconds
        {
            _fps = _frameCount / _fpsUpdateTimer;
            Title = $"Terra Nova - FPS: {_fps:F0}";
            _frameCount = 0;
            _fpsUpdateTimer = 0.0;
        }

        // Close window when Escape is pressed
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

        // Hotbar selection (keys 1-9) - delegated to PlayerController
        if (_playerController?.HandleHotbarSelection(KeyboardState) == true)
        {
            _hotbar.UpdateSelectedSlot(_playerController.SelectedHotbarSlot);
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

        // Poll network events
        _networkClient.Update();

        // Initialize GameEngine and Renderer once world data is received
        if (_networkClient.WorldReceived && _gameEngine == null && _networkClient.World != null)
        {
            _logger.LogInformation("World received! Initializing GameEngine and Renderer...");
            _world = _networkClient.World;
            _renderer = new OpenTKRenderer(_world, _loggerFactory.CreateLogger<OpenTKRenderer>());
            _renderer.Initialize();
            _renderer.SetCameraReference(_camera);
            _gameEngine = new GameEngine(_renderer);

            // Hook up chunk loading system BEFORE calling SetWorld
            _gameEngine.OnChunkRequestNeeded = (chunks) =>
            {
                _logger.LogInformation("Requesting {Count} chunks from server", chunks.Length);
                _networkClient.RequestChunks(chunks);
            };

            _networkClient.OnChunkReceived += (chunkPos, blocks) =>
            {
                _logger.LogInformation("Chunk ({X},{Z}) received with {Count} blocks", chunkPos.X, chunkPos.Z, blocks.Length);
                _gameEngine.NotifyChunkReceived(chunkPos);
            };

            _networkClient.OnBlockUpdate += (x, y, z, blockType) =>
            {
                _logger.LogInformation("Block update received at ({X},{Y},{Z}) -> {Type}", x, y, z, blockType);
                _gameEngine.NotifyBlockUpdate(x, y, z, blockType);
            };

            // Now set the world (this creates ChunkLoader with the callback)
            _gameEngine.SetWorld(_world);

            _logger.LogInformation("Chunk streaming enabled!");
        }

        // Update player position for chunk loading
        if (_gameEngine != null)
        {
            var cameraPos = _camera.Position.ToShared();
            _gameEngine.UpdatePlayerPosition(cameraPos);
        }

        // Perform raycast and handle block interaction (delegated to PlayerController)
        if (_networkClient.WorldReceived && _networkClient.World != null && _gameEngine != null)
        {
            _playerController?.UpdateRaycast(_networkClient.World);
            _playerController?.HandleBlockInteraction(MouseState, _networkClient.World);
        }
        else
        {
            _playerController?.UpdateRaycast(null);
        }

        // Update GameEngine (regenerates meshes if needed)
        if (_gameEngine != null)
        {
            _gameEngine.Update(deltaTime);
        }

        // Update renderer (for periodic chunk cleanup)
        if (_renderer != null)
        {
            _renderer.Update(args.Time);
        }
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

        // Use cached raycast result from PlayerController for block highlighting
        TerraNova.Shared.Vector3i? selectedBlockPos = null;
        if (_playerController?.CachedRaycastHit != null)
        {
            selectedBlockPos = _playerController.CachedRaycastHit.BlockPosition;
        }

        // Tell renderer which block to highlight
        if (_renderer != null)
        {
            if (selectedBlockPos.HasValue)
            {
                _renderer.HighlightBlock(selectedBlockPos.Value, true);
            }
            else
            {
                // Clear highlight if no block selected
                _renderer.HighlightBlock(new TerraNova.Shared.Vector3i(0, 0, 0), false);
            }

            // Render the world (chunks + highlighted block)
            _renderer.Render(ClientSize.X, ClientSize.Y);
        }

        // Draw UI overlays on top of everything
        float aspectRatio = (float)ClientSize.X / ClientSize.Y;
        _crosshair.Draw(aspectRatio);
        _hotbar.Draw();

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

        // Update UI overlays for new window dimensions
        _crosshair?.OnResize(args.Width, args.Height);
        _hotbar?.OnResize(args.Width, args.Height);
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
        _networkClient?.Disconnect();
        _renderer?.Dispose();
        _crosshair?.Dispose();
        _hotbar?.Dispose();

        _logger.LogInformation("Terra Nova shutting down...");
    }
}
