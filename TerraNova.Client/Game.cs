using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TerraNova.Configuration;
using TerraNova.Shared;

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
    private Shader _borderedShader = null!;
    private readonly INetworkClient _networkClient;
    private readonly NetworkSettings _networkSettings;
    private readonly CameraSettings _cameraSettings;
    private readonly ILogger<Game> _logger;
    private List<ChunkMesh> _chunkMeshes = new();
    private Texture _grassTexture = null!;
    private bool _meshesGenerated = false;
    private Crosshair _crosshair = null!;

    // FPS tracking
    private double _fpsUpdateTimer = 0.0;
    private int _frameCount = 0;
    private double _fps = 0.0;

    public Game(
        IOptions<GameSettings> gameSettings,
        IOptions<NetworkSettings> networkSettings,
        IOptions<CameraSettings> cameraSettings,
        INetworkClient networkClient,
        ILogger<Game> logger)
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

        // Initialize camera with configured settings (start above and outside the world)
        _camera = new Camera(new Vector3(0.0f, 25.0f, 50.0f))
        {
            Speed = _cameraSettings.MovementSpeed,
            Sensitivity = _cameraSettings.MouseSensitivity,
            Fov = MathHelper.DegreesToRadians(_cameraSettings.FieldOfView)
        };

        // Capture and hide the mouse cursor for FPS controls
        CursorState = CursorState.Grabbed;

        // Load shaders
        string vertexShaderSource = File.ReadAllText("Shaders/basic.vert");
        string fragmentShaderSource = File.ReadAllText("Shaders/basic.frag");
        _shader = new Shader(vertexShaderSource, fragmentShaderSource);

        // Load bordered shader for selected blocks
        string borderedFragmentShaderSource = File.ReadAllText("Shaders/bordered.frag");
        _borderedShader = new Shader(vertexShaderSource, borderedFragmentShaderSource);

        // Generate procedural texture for grass block
        byte[] grassPixels = TextureGenerator.GenerateTexture(BlockType.Grass, 16);
        _grassTexture = new Texture(16, 16, grassPixels);

        // Initialize UI overlays
        _crosshair = new Crosshair();
        _crosshair.Initialize();

        // Connect to server using configured settings
        _networkClient.Connect(_networkSettings.ServerHost, _networkSettings.ServerPort, _networkSettings.PlayerName);
        _logger.LogInformation("Connecting to {Host}:{Port}...", _networkSettings.ServerHost, _networkSettings.ServerPort);

        _logger.LogInformation("OpenGL Version: {Version}", GL.GetString(StringName.Version));

        // Check max line width supported
        float[] lineWidthRange = new float[2];
        GL.GetFloat(GetPName.AliasedLineWidthRange, lineWidthRange);
        _logger.LogInformation("Line width range: {Min} - {Max}", lineWidthRange[0], lineWidthRange[1]);

        _logger.LogInformation("Terra Nova initialized!");
        _logger.LogInformation("Controls: WASD to move, Space/Shift for up/down, Mouse to look, ESC to exit");
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

        // Generate meshes once world data is received
        if (_networkClient.WorldReceived && !_meshesGenerated && _networkClient.World != null)
        {
            GenerateMeshesFromWorld(_networkClient.World);
            _meshesGenerated = true;
        }

        // Handle block interaction if world is loaded
        if (_networkClient.WorldReceived && _networkClient.World != null)
        {
            HandleBlockInteraction();

            // Regenerate meshes if world changed
            if (_networkClient.WorldChanged)
            {
                GenerateMeshesFromWorld(_networkClient.World);
                _networkClient.WorldChanged = false;
            }
        }
    }

    private void GenerateMeshesFromWorld(World world)
    {
        _logger.LogInformation("Generating chunk meshes from server world data...");

        // Clear any existing meshes
        foreach (var mesh in _chunkMeshes)
        {
            mesh.Dispose();
        }
        _chunkMeshes.Clear();

        // Generate one mesh per chunk (huge performance improvement!)
        foreach (var chunk in world.GetAllChunks())
        {
            var chunkMesh = new ChunkMesh(chunk, world);
            _chunkMeshes.Add(chunkMesh);
        }

        _logger.LogInformation("Generated {Count} chunk meshes (was {BlockCount} individual block meshes before optimization)",
            _chunkMeshes.Count, world.GetAllBlocks().Count());
    }

    private void HandleBlockInteraction()
    {
        // Left click - break block
        if (MouseState.IsButtonPressed(MouseButton.Left))
        {
            var hit = Raycaster.Cast(_networkClient.World!, _camera.Position, _camera.Front);
            if (hit != null)
            {
                _logger.LogInformation("Breaking block at ({X},{Y},{Z})",
                    hit.BlockPosition.X, hit.BlockPosition.Y, hit.BlockPosition.Z);

                // Send update to server (set to Air to break)
                _networkClient.SendBlockUpdate(
                    hit.BlockPosition.X,
                    hit.BlockPosition.Y,
                    hit.BlockPosition.Z,
                    BlockType.Air);
            }
        }

        // Right click - place block
        if (MouseState.IsButtonPressed(MouseButton.Right))
        {
            var hit = Raycaster.Cast(_networkClient.World!, _camera.Position, _camera.Front);
            if (hit != null)
            {
                // Calculate position to place the block (adjacent to hit face)
                TerraNova.Shared.Vector3i placePos = GetAdjacentBlockPosition(hit.BlockPosition, hit.HitFace);

                _logger.LogInformation("Placing block at ({X},{Y},{Z})",
                    placePos.X, placePos.Y, placePos.Z);

                // Send update to server (place a Grass block for now)
                _networkClient.SendBlockUpdate(placePos.X, placePos.Y, placePos.Z, BlockType.Grass);
            }
        }
    }

    private TerraNova.Shared.Vector3i GetAdjacentBlockPosition(TerraNova.Shared.Vector3i blockPos, BlockFace face)
    {
        return face switch
        {
            BlockFace.Front => new TerraNova.Shared.Vector3i(blockPos.X, blockPos.Y, blockPos.Z + 1),
            BlockFace.Back => new TerraNova.Shared.Vector3i(blockPos.X, blockPos.Y, blockPos.Z - 1),
            BlockFace.Left => new TerraNova.Shared.Vector3i(blockPos.X - 1, blockPos.Y, blockPos.Z),
            BlockFace.Right => new TerraNova.Shared.Vector3i(blockPos.X + 1, blockPos.Y, blockPos.Z),
            BlockFace.Top => new TerraNova.Shared.Vector3i(blockPos.X, blockPos.Y + 1, blockPos.Z),
            BlockFace.Bottom => new TerraNova.Shared.Vector3i(blockPos.X, blockPos.Y - 1, blockPos.Z),
            _ => blockPos
        };
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

        // Set view and projection matrices (same for all blocks)
        Matrix4 view = _camera.GetViewMatrix();
        float aspectRatio = (float)ClientSize.X / ClientSize.Y;
        Matrix4 projection = _camera.GetProjectionMatrix(aspectRatio);
        Matrix4 model = Matrix4.Identity; // Blocks are already positioned, so model is identity

        // Find which block is selected (if any)
        TerraNova.Shared.Vector3i? selectedBlockPos = null;
        if (_networkClient.WorldReceived && _networkClient.World != null)
        {
            var hit = Raycaster.Cast(_networkClient.World, _camera.Position, _camera.Front);
            if (hit != null)
            {
                selectedBlockPos = hit.BlockPosition;
            }
        }

        // Draw all chunks with normal shader
        _shader.Use();
        _grassTexture.Bind(0);
        _shader.SetInt("blockTexture", 0);
        _shader.SetMatrix4("view", view);
        _shader.SetMatrix4("projection", projection);
        _shader.SetMatrix4("model", model);

        foreach (var chunkMesh in _chunkMeshes)
        {
            chunkMesh.Draw();
        }

        // If a block is selected, draw it again with the bordered shader for highlighting
        if (selectedBlockPos.HasValue && _networkClient.World != null)
        {
            BlockType selectedBlockType = _networkClient.World.GetBlock(
                selectedBlockPos.Value.X,
                selectedBlockPos.Value.Y,
                selectedBlockPos.Value.Z);

            if (selectedBlockType != BlockType.Air)
            {
                BlockFaces visibleFaces = _networkClient.World.GetVisibleFaces(
                    selectedBlockPos.Value.X,
                    selectedBlockPos.Value.Y,
                    selectedBlockPos.Value.Z);

                // Create a temporary mesh for the selected block with bordered shader
                using var selectedBlockMesh = new CubeMesh(
                    new Vector3(selectedBlockPos.Value.X, selectedBlockPos.Value.Y, selectedBlockPos.Value.Z),
                    selectedBlockType,
                    visibleFaces);

                _borderedShader.Use();
                _grassTexture.Bind(0);
                _borderedShader.SetInt("blockTexture", 0);
                _borderedShader.SetMatrix4("view", view);
                _borderedShader.SetMatrix4("projection", projection);
                _borderedShader.SetMatrix4("model", model);

                selectedBlockMesh.Draw();
            }
        }

        // Draw crosshair on top of everything
        _crosshair.Draw(aspectRatio);

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
        _networkClient?.Disconnect();
        _shader?.Dispose();
        _borderedShader?.Dispose();
        _grassTexture?.Dispose();
        _crosshair?.Dispose();

        if (_chunkMeshes != null)
        {
            foreach (var mesh in _chunkMeshes)
            {
                mesh.Dispose();
            }
        }

        _logger.LogInformation("Terra Nova shutting down...");
    }
}
