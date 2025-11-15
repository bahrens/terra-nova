using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TerraNova.Configuration;
using TerraNova.Core;
using TerraNova.Physics;
using TerraNova.Shared;
using Vector3 = OpenTK.Mathematics.Vector3;
using Vector3i = TerraNova.Shared.Vector3i;

namespace TerraNova;

/// <summary>
/// Main application coordinator for the Terra Nova client.
/// Orchestrates all game systems (rendering, input, networking, UI) and their interactions.
/// This class owns the game's business logic, while Game.cs acts as a thin OpenTK adapter.
/// </summary>
public class ClientApplication : IDisposable
{
    // Core game systems
    private readonly Camera _camera;
    private readonly PlayerController _playerController;
    private readonly NetworkCoordinator _networkCoordinator;
    private readonly UIManager _uiManager;
    private readonly WindowStateManager _windowStateManager;
    private readonly ILogger<ClientApplication> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IPhysicsShapeFactory _shapeFactory;

    // Renderer and game engine (created after world received)
    private IRenderer? _renderer;
    private GameEngine? _gameEngine;
    private World? _world;

    // Physics (created after world received)
    private IPhysicsWorld? _physicsWorld;

    // Frame counter for periodic diagnostics
    private int _frameCount = 0;

    // DeltaTime variance tracking
    private readonly List<double> _deltaTimeSamples = new();
    private const int MaxDeltaTimeSamples = 60;

    // Fixed timestep physics
    private const float FixedDeltaTime = 1.0f / 60.0f; // 60 Hz physics
    private float _physicsTimeAccumulator = 0f;

    public ClientApplication(
        INetworkClient networkClient,
        IOptions<NetworkSettings> networkSettings,
        IOptions<CameraSettings> cameraSettings,
        ILogger<ClientApplication> logger,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;

        // Initialize camera with configured spawn position
        _camera = new Camera(
            new Vector3(cameraSettings.Value.SpawnX, cameraSettings.Value.SpawnY, cameraSettings.Value.SpawnZ))
        {
            Speed = cameraSettings.Value.MovementSpeed,
            Sensitivity = cameraSettings.Value.MouseSensitivity,
            Fov = MathHelper.DegreesToRadians(cameraSettings.Value.FieldOfView)
        };

        // Initialize physics shape factory (used by player)
        _shapeFactory = new VoxelShapeFactory();
        _playerController = new PlayerController(_camera, networkClient, _shapeFactory, loggerFactory.CreateLogger<PlayerController>());

        // Initialize coordinators
        _networkCoordinator = new NetworkCoordinator(networkClient, networkSettings.Value, loggerFactory.CreateLogger<NetworkCoordinator>());
        _uiManager = new UIManager(_playerController, loggerFactory.CreateLogger<UIManager>());
        _windowStateManager = new WindowStateManager(loggerFactory.CreateLogger<WindowStateManager>());
    }

    /// <summary>
    /// Initialize all systems (called once from Game.OnLoad).
    /// </summary>
    /// <param name="windowWidth">Window width in pixels</param>
    /// <param name="windowHeight">Window height in pixels</param>
    public void Initialize(int windowWidth, int windowHeight)
    {
        _logger.LogInformation("Initializing ClientApplication...");

        // Initialize UI manager with window dimensions
        _uiManager.Initialize(windowWidth, windowHeight);

        // Connect to server
        _networkCoordinator.Connect();

        _logger.LogInformation("ClientApplication initialized!");
    }

    /// <summary>
    /// Update all game systems (called every frame from Game.OnUpdateFrame).
    /// </summary>
    /// <param name="keyboardState">Current keyboard state</param>
    /// <param name="mouseState">Current mouse state</param>
    /// <param name="deltaTime">Time since last frame in seconds</param>
    public void Update(KeyboardState keyboardState, MouseState mouseState, double deltaTime)
    {
        // Update window state (FPS tracking)
        _windowStateManager.Update(deltaTime);

        // Poll network events
        _networkCoordinator.Update();

        // Initialize world/renderer/engine if world was just received
        if (_renderer == null && _networkCoordinator.WorldReceived && _networkCoordinator.World != null)
        {
            InitializeWorldSystems();
        }

        // CRITICAL FIX: Handle input BEFORE physics step to avoid 1-frame lag
        // Update player controller (input, raycasting, block interaction)
        int previousHotbarSlot = _playerController.SelectedHotbarSlot;
        _playerController.Update(keyboardState, mouseState, _world, deltaTime);

        // Step physics simulation (using current frame's input)
        if (_physicsWorld != null)
        {
            // Track deltaTime variance for diagnostics
            _deltaTimeSamples.Add(deltaTime);
            if (_deltaTimeSamples.Count > MaxDeltaTimeSamples)
                _deltaTimeSamples.RemoveAt(0);

            // FIXED TIMESTEP PHYSICS (eliminates jitter from variable deltaTime)
            // Accumulate frame time
            _physicsTimeAccumulator += (float)deltaTime;

            // Step physics in fixed increments for deterministic behavior
            int stepsThisFrame = 0;
            const int maxStepsPerFrame = 5; // Prevent "spiral of death" at low FPS
            while (_physicsTimeAccumulator >= FixedDeltaTime && stepsThisFrame < maxStepsPerFrame)
            {
                _physicsWorld.Step(FixedDeltaTime);
                _physicsTimeAccumulator -= FixedDeltaTime;
                stepsThisFrame++;
            }

            // Sync camera to physics body position (must happen AFTER all physics steps)
            _playerController.SyncCameraToPhysics();

            // Periodic diagnostic logging (every 60 frames to avoid spam)
            _frameCount++;
            if (_frameCount % 60 == 0)
            {
                var playerPos = _playerController.PhysicsBodyPosition;
                var playerVel = _playerController.PhysicsBodyVelocity;
                if (playerPos.HasValue && playerVel.HasValue)
                {
                    _logger.LogDebug("Player: Pos=({X:F2}, {Y:F2}, {Z:F2}), Vel=({VX:F2}, {VY:F2}, {VZ:F2})",
                        playerPos.Value.X, playerPos.Value.Y, playerPos.Value.Z,
                        playerVel.Value.X, playerVel.Value.Y, playerVel.Value.Z);
                }

                // Log deltaTime variance
                if (_deltaTimeSamples.Count >= 60)
                {
                    double avgDelta = _deltaTimeSamples.Average();
                    double minDelta = _deltaTimeSamples.Min();
                    double maxDelta = _deltaTimeSamples.Max();
                    double variance = _deltaTimeSamples.Average(d => Math.Pow(d - avgDelta, 2));
                    double stdDev = Math.Sqrt(variance);

                    _logger.LogDebug(
                        "[DELTATIME] avg={Avg:F3}ms min={Min:F3}ms max={Max:F3}ms stddev={StdDev:F3}ms " +
                        "variance={Variance:F1}% | PhysicsSteps={Steps} Accumulator={Accum:F3}ms",
                        avgDelta * 1000, minDelta * 1000, maxDelta * 1000, stdDev * 1000,
                        (stdDev / avgDelta * 100),
                        stepsThisFrame, _physicsTimeAccumulator * 1000
                    );
                }
            }
        }

        // Update UI if hotbar selection changed
        if (_playerController.SelectedHotbarSlot != previousHotbarSlot)
        {
            _uiManager.OnHotbarSelectionChanged(_playerController.SelectedHotbarSlot);
        }

        // Update player position for chunk loading
        if (_gameEngine != null)
        {
            var cameraPos = _camera.Position.ToShared();
            _gameEngine.UpdatePlayerPosition(cameraPos);
        }

        // Update game engine (mesh regeneration)
        _gameEngine?.Update(deltaTime);

        // Update renderer (chunk cleanup)
        _renderer?.Update(deltaTime);
    }

    /// <summary>
    /// Render all game visuals (called every frame from Game.OnRenderFrame).
    /// </summary>
    /// <param name="viewportWidth">Viewport width in pixels</param>
    /// <param name="viewportHeight">Viewport height in pixels</param>
    /// <param name="aspectRatio">Viewport aspect ratio</param>
    public void Render(int viewportWidth, int viewportHeight, float aspectRatio)
    {
        // Get selected block from player controller for highlighting
        Vector3i? selectedBlockPos = _playerController.CachedRaycastHit?.BlockPosition;

        // Tell renderer which block to highlight
        if (_renderer != null)
        {
            if (selectedBlockPos.HasValue)
            {
                _renderer.HighlightBlock(selectedBlockPos.Value, true);
            }
            else
            {
                _renderer.HighlightBlock(new Vector3i(0, 0, 0), false);
            }

            // Render the world
            _renderer.Render(viewportWidth, viewportHeight);
        }

        // Draw UI overlays
        _uiManager.Draw(aspectRatio);
    }

    /// <summary>
    /// Handle window resize (called from Game.OnResize).
    /// </summary>
    /// <param name="width">New window width</param>
    /// <param name="height">New window height</param>
    public void OnResize(int width, int height)
    {
        _uiManager.OnResize(width, height);
    }

    /// <summary>
    /// Handle mouse movement (called from Game.OnMouseMove).
    /// </summary>
    /// <param name="deltaX">Horizontal mouse delta</param>
    /// <param name="deltaY">Vertical mouse delta</param>
    public void OnMouseMove(float deltaX, float deltaY)
    {
        _playerController.HandleMouseLook(deltaX, deltaY);
    }

    /// <summary>
    /// Get the current FPS for display in window title.
    /// </summary>
    public double CurrentFPS => _windowStateManager.CurrentFPS;

    /// <summary>
    /// Initialize world-related systems when world is received from server.
    /// </summary>
    private void InitializeWorldSystems()
    {
        _logger.LogInformation("World received! Initializing GameEngine, Renderer, and Physics...");

        _world = _networkCoordinator.World;
        _renderer = new OpenTKRenderer(_world!, _loggerFactory.CreateLogger<OpenTKRenderer>());
        _renderer.Initialize();
        _renderer.SetCamera(_camera);

        _gameEngine = new GameEngine(_renderer);

        // Hook up logging to GameEngine for debugging collision issues
        _gameEngine.Logger = (msg) => _logger.LogInformation(msg);

        // CRITICAL FIX: Set up network event handlers FIRST, then set world BEFORE physics initialization
        // This ensures: 1) Events are registered before ChunkLoader is created
        //               2) World is available when InitializePhysics creates terrain collision
        _networkCoordinator.InitializeWorld(_gameEngine, _world!);

        // Create voxel physics world with custom AABB collision
        var collisionLogger = _loggerFactory.CreateLogger<VoxelCollisionSystem>();
        _physicsWorld = new VoxelPhysicsWorld(collisionLogger);
        _physicsWorld.SetGravity(new Shared.Vector3(0, -9.81f, 0));

        // CRITICAL: Set world reference for voxel collision queries
        if (_physicsWorld is VoxelPhysicsWorld voxelPhysicsWorld)
        {
            voxelPhysicsWorld.SetWorld(_world!);
        }

        _logger.LogInformation("Initializing player physics at camera position ({X}, {Y}, {Z})",
            _camera.Position.X, _camera.Position.Y, _camera.Position.Z);

        // Initialize player physics
        _playerController.InitializePhysics(_physicsWorld);

        _logger.LogInformation("Chunk streaming and voxel physics enabled!");
    }

    public void Dispose()
    {
        _networkCoordinator?.Dispose();
        _renderer?.Dispose();
        _gameEngine?.Dispose();
        _uiManager?.Dispose();
    }
}
