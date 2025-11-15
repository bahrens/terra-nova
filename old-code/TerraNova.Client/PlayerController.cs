using Microsoft.Extensions.Logging;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TerraNova.Core;
using TerraNova.Shared;

namespace TerraNova;

/// <summary>
/// Manages player input, actions, and state (hotbar, block interaction, raycasting).
/// Responsible for handling player-related logic separate from window lifecycle management.
/// </summary>
public class PlayerController
{
    private readonly Camera _camera;
    private readonly INetworkClient _networkClient;
    private readonly IPhysicsShapeFactory? _shapeFactory;
    private readonly ILogger<PlayerController> _logger;

    // Physics state (initialized later via InitializePhysics)
    private IPhysicsWorld? _physicsWorld;
    private IPhysicsBody? _physicsBody;

    // Hotbar state
    private int _selectedHotbarSlot = 0; // 0-8 for slots 1-9
    private readonly BlockType[] _hotbarBlocks = new BlockType[9]
    {
        BlockType.Grass,   // Slot 1
        BlockType.Dirt,    // Slot 2
        BlockType.Stone,   // Slot 3
        BlockType.Wood,    // Slot 4
        BlockType.Planks,  // Slot 5
        BlockType.Sand,    // Slot 6
        BlockType.Gravel,  // Slot 7
        BlockType.Glass,   // Slot 8
        BlockType.Leaves   // Slot 9
    };

    // Cached raycast state (to avoid duplicate raycasts per frame)
    private RaycastHit? _cachedRaycastHit = null;

    // Diagnostic logging state
    private double _lastDiagnosticLogTime = 0;
    private const double DiagnosticLogInterval = 1.0; // Log every 1 second

    /// <summary>
    /// Gets the currently selected hotbar slot (0-8)
    /// </summary>
    public int SelectedHotbarSlot => _selectedHotbarSlot;

    /// <summary>
    /// Gets the block type of the currently selected hotbar slot
    /// </summary>
    public BlockType SelectedBlockType => _hotbarBlocks[_selectedHotbarSlot];

    /// <summary>
    /// Gets the array of block types in the hotbar
    /// </summary>
    public BlockType[] HotbarBlocks => _hotbarBlocks;

    /// <summary>
    /// Gets the cached raycast hit from the most recent frame
    /// </summary>
    public RaycastHit? CachedRaycastHit => _cachedRaycastHit;

    /// <summary>
    /// Gets the current physics body position (null if physics not initialized)
    /// </summary>
    public Shared.Vector3? PhysicsBodyPosition => _physicsBody?.Position;

    /// <summary>
    /// Gets the current physics body velocity (null if physics not initialized)
    /// </summary>
    public Shared.Vector3? PhysicsBodyVelocity => _physicsBody?.Velocity;

    /// <summary>
    /// Creates a new PlayerController with the specified dependencies
    /// </summary>
    /// <param name="camera">The camera for raycasting and positioning</param>
    /// <param name="networkClient">Network client for sending block updates</param>
    /// <param name="shapeFactory">Factory for creating physics collision shapes</param>
    /// <param name="logger">Logger for diagnostic output</param>
    public PlayerController(Camera camera, INetworkClient networkClient, IPhysicsShapeFactory shapeFactory, ILogger<PlayerController> logger)
    {
        _camera = camera;
        _networkClient = networkClient;
        _shapeFactory = shapeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Updates player state based on input and world state.
    /// Should be called once per frame during the game update loop.
    /// Orchestrates all player subsystems in the correct order.
    /// </summary>
    /// <param name="keyboardState">Current keyboard state</param>
    /// <param name="mouseState">Current mouse state</param>
    /// <param name="world">The game world for raycasting and block interaction (null if not received yet)</param>
    /// <param name="deltaTime">Time elapsed since last frame in seconds</param>
    public void Update(KeyboardState keyboardState, MouseState mouseState, World? world, double deltaTime)
    {
        // Phase 1: Handle all input
        HandleMovementInput(keyboardState, (float)deltaTime);
        HandleHotbarSelection(keyboardState);

        // Phase 2: Update world interaction (raycast and block interaction)
        UpdateRaycast(world);
        if (world != null)
        {
            HandleBlockInteraction(mouseState, world);
        }

        // Diagnostic logging (periodic)
        LogPhysicsDiagnostics(deltaTime);

        // In Stage 2B, additional phases will be added here:
        // Phase 3: Step physics world
        // Phase 4: Sync camera position to physics body
    }

    /// <summary>
    /// Initialize player physics body. Must be called after physics world is created.
    /// </summary>
    /// <param name="physicsWorld">The physics world to add the player body to</param>
    public void InitializePhysics(IPhysicsWorld physicsWorld)
    {
        _physicsWorld = physicsWorld;

        // Create capsule shape for player (radius: 0.3, height: 1.6)
        // Reduced from 0.4m to 0.3m for better fit through 1-block holes
        // This creates 0.6m wide x 2.2m tall AABB with 0.2m clearance per side
        IPhysicsShape capsuleShape = _shapeFactory!.CreateCapsule(0.3f, 1.6f);

        // Create physics body at current camera position
        // CRITICAL FIX: Set all properties BEFORE calling SetShape() per Jitter2 docs
        _physicsBody = _physicsWorld.CreateBody();
        _physicsBody.Position = new Shared.Vector3(
            _camera.Position.X,
            _camera.Position.Y,
            _camera.Position.Z
        );
        _physicsBody.AffectedByGravity = true;  // Set gravity before shape
        _physicsBody.IsStatic = false;          // Set motion type before shape (dynamic body)
        _physicsBody.SetShape(capsuleShape);    // Add shape LAST
        // Note: CreateBody() already adds to world, no need for AddBody()

        _logger.LogInformation("Player physics body initialized at position ({X}, {Y}, {Z}) - IsStatic={IsStatic}, Gravity={Gravity}",
            _physicsBody.Position.X, _physicsBody.Position.Y, _physicsBody.Position.Z,
            _physicsBody.IsStatic, _physicsBody.AffectedByGravity);
    }

    /// <summary>
    /// Handles player movement input from keyboard.
    /// Uses physics-based movement when physics is initialized, falls back to direct camera manipulation otherwise.
    /// </summary>
    /// <param name="keyboardState">Current keyboard state</param>
    /// <param name="deltaTime">Time since last frame in seconds</param>
    public void HandleMovementInput(KeyboardState keyboardState, float deltaTime)
    {
        if (_physicsBody == null)
        {
            // Fallback to old movement if physics not initialized
            if (keyboardState.IsKeyDown(Keys.W))
                _camera.ProcessKeyboard(CameraMovement.Forward, deltaTime);
            if (keyboardState.IsKeyDown(Keys.S))
                _camera.ProcessKeyboard(CameraMovement.Backward, deltaTime);
            if (keyboardState.IsKeyDown(Keys.A))
                _camera.ProcessKeyboard(CameraMovement.Left, deltaTime);
            if (keyboardState.IsKeyDown(Keys.D))
                _camera.ProcessKeyboard(CameraMovement.Right, deltaTime);
            if (keyboardState.IsKeyDown(Keys.Space))
                _camera.ProcessKeyboard(CameraMovement.Up, deltaTime);
            if (keyboardState.IsKeyDown(Keys.LeftShift))
                _camera.ProcessKeyboard(CameraMovement.Down, deltaTime);
            return;
        }

        // Physics-based movement
        float moveSpeed = 5.0f; // m/s
        Shared.Vector3 moveDirection = Shared.Vector3.Zero;

        // Get camera front/right in Shared.Vector3 format
        var cameraFront = new Shared.Vector3(_camera.Front.X, _camera.Front.Y, _camera.Front.Z);
        var cameraRight = new Shared.Vector3(_camera.Right.X, _camera.Right.Y, _camera.Right.Z);

        // Calculate horizontal movement (ignore Y component for ground movement)
        cameraFront.Y = 0;
        if (cameraFront.LengthSquared() > 0.01f)
            cameraFront = Shared.Vector3.Normalize(cameraFront);

        cameraRight.Y = 0;
        if (cameraRight.LengthSquared() > 0.01f)
            cameraRight = Shared.Vector3.Normalize(cameraRight);

        if (keyboardState.IsKeyDown(Keys.W))
            moveDirection += cameraFront;
        if (keyboardState.IsKeyDown(Keys.S))
            moveDirection -= cameraFront;
        if (keyboardState.IsKeyDown(Keys.A))
            moveDirection -= cameraRight;
        if (keyboardState.IsKeyDown(Keys.D))
            moveDirection += cameraRight;

        // Set horizontal velocity based on input (preserve vertical velocity for gravity/jumping)
        Shared.Vector3 currentVelocity = _physicsBody.Velocity;

        if (moveDirection.LengthSquared() > 0.01f)
        {
            // Normalize to prevent faster diagonal movement
            moveDirection = Shared.Vector3.Normalize(moveDirection);

            // Apply movement velocity
            _physicsBody.Velocity = new Shared.Vector3(
                moveDirection.X * moveSpeed,
                currentVelocity.Y, // Preserve vertical velocity
                moveDirection.Z * moveSpeed
            );
        }
        else
        {
            // No input: stop horizontal movement immediately (Minecraft-style instant stop)
            _physicsBody.Velocity = new Shared.Vector3(0, currentVelocity.Y, 0);
        }

        // Jumping with IsGrounded check - use smooth jump system
        if (keyboardState.IsKeyPressed(Keys.Space) && _physicsBody.IsGrounded)
        {
            // Cast to VoxelPhysicsBody to access smooth jump method
            if (_physicsBody is TerraNova.Physics.VoxelPhysicsBody voxelBody)
            {
                // Use same smooth jump as auto-jump (5.0 m/s reaches ~1.25m height)
                voxelBody.StartJump(5.0f, 0.3f);
            }
        }

        // Toggle auto-jump feature with J key
        if (keyboardState.IsKeyPressed(Keys.J))
        {
            if (_physicsBody is TerraNova.Physics.VoxelPhysicsBody voxelBody)
            {
                voxelBody.AutoJumpEnabled = !voxelBody.AutoJumpEnabled;
                _logger.LogInformation("Auto-jump {State}",
                    voxelBody.AutoJumpEnabled ? "enabled" : "disabled");
            }
        }

        // F3 key: Dump position diagnostics
        if (keyboardState.IsKeyPressed(Keys.F3))
        {
            LogPositionDiagnostics();
        }
    }

    /// <summary>
    /// Sync camera position to physics body position (call AFTER physics step).
    /// This must be called after the physics simulation has stepped to avoid visual lag.
    /// </summary>
    public void SyncCameraToPhysics()
    {
        if (_physicsBody == null)
            return;

        Shared.Vector3 bodyPos = _physicsBody.Position;
        // Camera eye height: 1.62m above feet (90% of 1.8m, Minecraft-style)
        // With new 0.3m radius, total height is 2.2m
        _camera.Position = new OpenTK.Mathematics.Vector3(bodyPos.X, bodyPos.Y + 1.62f, bodyPos.Z);
    }

    /// <summary>
    /// Log detailed position alignment diagnostics.
    /// Call this when user presses F3 to trigger diagnostic dump.
    /// </summary>
    public void LogPositionDiagnostics()
    {
        if (_physicsBody == null)
        {
            _logger.LogWarning("[DIAGNOSTICS] Physics not initialized - no position data available");
            return;
        }

        Shared.Vector3 bodyPos = _physicsBody.Position;
        OpenTK.Mathematics.Vector3 cameraPos = _camera.Position;

        // Cast to VoxelPhysicsBody to access shape information
        if (_physicsBody is TerraNova.Physics.VoxelPhysicsBody voxelBody && voxelBody.Shape != null)
        {
            var halfExtents = voxelBody.Shape.HalfExtents;
            float capsuleHalfHeight = halfExtents.Y;

            // Calculate AABB bounds (same calculation as MoveBody)
            Shared.Vector3 aabbCenter = new Shared.Vector3(
                bodyPos.X,
                bodyPos.Y + capsuleHalfHeight,
                bodyPos.Z
            );

            Shared.Vector3 aabbMin = new Shared.Vector3(
                aabbCenter.X - halfExtents.X,
                aabbCenter.Y - halfExtents.Y,
                aabbCenter.Z - halfExtents.Z
            );

            Shared.Vector3 aabbMax = new Shared.Vector3(
                aabbCenter.X + halfExtents.X,
                aabbCenter.Y + halfExtents.Y,
                aabbCenter.Z + halfExtents.Z
            );

            // Calculate which block coordinates the player is in/over
            int blockX = (int)Math.Floor(bodyPos.X);
            int blockY = (int)Math.Floor(bodyPos.Y);
            int blockZ = (int)Math.Floor(bodyPos.Z);

            _logger.LogInformation("========== POSITION DIAGNOSTICS ==========");
            _logger.LogInformation("[DIAGNOSTICS] Body Position (feet):  ({X:F3}, {Y:F3}, {Z:F3})",
                bodyPos.X, bodyPos.Y, bodyPos.Z);
            _logger.LogInformation("[DIAGNOSTICS] Camera Position (eyes): ({X:F3}, {Y:F3}, {Z:F3})",
                cameraPos.X, cameraPos.Y, cameraPos.Z);
            _logger.LogInformation("[DIAGNOSTICS] Capsule Radius: {Radius}m, Half-Height: {HalfHeight}m",
                halfExtents.X, capsuleHalfHeight);
            _logger.LogInformation("[DIAGNOSTICS] AABB Center:   ({X:F3}, {Y:F3}, {Z:F3})",
                aabbCenter.X, aabbCenter.Y, aabbCenter.Z);
            _logger.LogInformation("[DIAGNOSTICS] AABB Min:      ({X:F3}, {Y:F3}, {Z:F3})",
                aabbMin.X, aabbMin.Y, aabbMin.Z);
            _logger.LogInformation("[DIAGNOSTICS] AABB Max:      ({X:F3}, {Y:F3}, {Z:F3})",
                aabbMax.X, aabbMax.Y, aabbMax.Z);
            _logger.LogInformation("[DIAGNOSTICS] AABB Size:     ({X:F3} x {Y:F3} x {Z:F3})",
                aabbMax.X - aabbMin.X, aabbMax.Y - aabbMin.Y, aabbMax.Z - aabbMin.Z);
            _logger.LogInformation("[DIAGNOSTICS] Standing on block: ({X}, {Y}, {Z})",
                blockX, blockY, blockZ);
            _logger.LogInformation("[DIAGNOSTICS] Grounded: {Grounded}, Velocity: ({VX:F3}, {VY:F3}, {VZ:F3})",
                _physicsBody.IsGrounded, _physicsBody.Velocity.X, _physicsBody.Velocity.Y, _physicsBody.Velocity.Z);
            _logger.LogInformation("==========================================");
        }
    }

    /// <summary>
    /// Handles mouse look input for camera rotation.
    /// Processes mouse movement delta to rotate the camera view.
    /// </summary>
    /// <param name="deltaX">Horizontal mouse movement in pixels</param>
    /// <param name="deltaY">Vertical mouse movement in pixels</param>
    public void HandleMouseLook(float deltaX, float deltaY)
    {
        _camera.ProcessMouseMovement(deltaX, deltaY);
    }

    /// <summary>
    /// Performs a raycast from the camera and caches the result.
    /// Should be called once per frame to update the cached raycast hit.
    /// </summary>
    /// <param name="world">The game world to raycast against, or null if world not available</param>
    public void UpdateRaycast(World? world)
    {
        // Only perform raycast if world is available
        if (world != null)
        {
            _cachedRaycastHit = Raycaster.Cast(
                world,
                _camera.Position.ToShared(),
                _camera.Front.ToShared());
        }
        else
        {
            _cachedRaycastHit = null;
        }
    }

    /// <summary>
    /// Handles hotbar slot selection from keyboard input (keys 1-9).
    /// Updates the selected slot when number keys are pressed.
    /// </summary>
    /// <param name="keyboardState">Current keyboard state</param>
    /// <returns>True if the selected slot changed, false otherwise</returns>
    public bool HandleHotbarSelection(KeyboardState keyboardState)
    {
        int previousSlot = _selectedHotbarSlot;

        if (keyboardState.IsKeyPressed(Keys.D1)) _selectedHotbarSlot = 0;
        if (keyboardState.IsKeyPressed(Keys.D2)) _selectedHotbarSlot = 1;
        if (keyboardState.IsKeyPressed(Keys.D3)) _selectedHotbarSlot = 2;
        if (keyboardState.IsKeyPressed(Keys.D4)) _selectedHotbarSlot = 3;
        if (keyboardState.IsKeyPressed(Keys.D5)) _selectedHotbarSlot = 4;
        if (keyboardState.IsKeyPressed(Keys.D6)) _selectedHotbarSlot = 5;
        if (keyboardState.IsKeyPressed(Keys.D7)) _selectedHotbarSlot = 6;
        if (keyboardState.IsKeyPressed(Keys.D8)) _selectedHotbarSlot = 7;
        if (keyboardState.IsKeyPressed(Keys.D9)) _selectedHotbarSlot = 8;

        // Log and return true if selection changed
        if (_selectedHotbarSlot != previousSlot)
        {
            _logger.LogInformation("Hotbar slot changed to {Slot} (Block: {BlockType})",
                _selectedHotbarSlot + 1, // Display as 1-9 for user friendliness
                _hotbarBlocks[_selectedHotbarSlot]);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles block interaction (breaking and placing blocks) from mouse input.
    /// Uses the cached raycast hit to determine which block to interact with.
    /// Left mouse button breaks blocks (sets to Air).
    /// Right mouse button places the currently selected hotbar block.
    /// </summary>
    /// <param name="mouseState">Current mouse state</param>
    /// <param name="world">The game world for block updates</param>
    public void HandleBlockInteraction(MouseState mouseState, World world)
    {
        // Check if we have a valid raycast hit
        if (_cachedRaycastHit == null)
        {
            return;
        }

        var hit = _cachedRaycastHit;

        // Left click - break block
        if (mouseState.IsButtonPressed(MouseButton.Left))
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

        // Right click - place block
        if (mouseState.IsButtonPressed(MouseButton.Right))
        {
            // Calculate position to place the block (adjacent to hit face)
            Vector3i placePos = GetAdjacentBlockPosition(hit.BlockPosition, hit.HitFace);

            _logger.LogInformation("Placing {BlockType} at ({X},{Y},{Z})",
                SelectedBlockType, placePos.X, placePos.Y, placePos.Z);

            // Send update to server with the selected block type
            _networkClient.SendBlockUpdate(placePos.X, placePos.Y, placePos.Z, SelectedBlockType);
        }
    }

    /// <summary>
    /// Calculates the adjacent block position based on the hit face.
    /// Used for placing blocks next to the face that was hit by the raycast.
    /// </summary>
    /// <param name="blockPos">The position of the block that was hit</param>
    /// <param name="face">The face of the block that was hit</param>
    /// <returns>The position adjacent to the hit face</returns>
    private Vector3i GetAdjacentBlockPosition(Vector3i blockPos, BlockFace face)
    {
        return face switch
        {
            BlockFace.Front => new Vector3i(blockPos.X, blockPos.Y, blockPos.Z + 1),
            BlockFace.Back => new Vector3i(blockPos.X, blockPos.Y, blockPos.Z - 1),
            BlockFace.Left => new Vector3i(blockPos.X - 1, blockPos.Y, blockPos.Z),
            BlockFace.Right => new Vector3i(blockPos.X + 1, blockPos.Y, blockPos.Z),
            BlockFace.Top => new Vector3i(blockPos.X, blockPos.Y + 1, blockPos.Z),
            BlockFace.Bottom => new Vector3i(blockPos.X, blockPos.Y - 1, blockPos.Z),
            _ => blockPos
        };
    }

    /// <summary>
    /// Logs physics diagnostics periodically for debugging movement issues.
    /// Logs velocity, ground state, and position every second.
    /// </summary>
    private void LogPhysicsDiagnostics(double deltaTime)
    {
        if (_physicsBody == null)
            return;

        _lastDiagnosticLogTime += deltaTime;
        if (_lastDiagnosticLogTime >= DiagnosticLogInterval)
        {
            _lastDiagnosticLogTime = 0;

            Shared.Vector3 vel = _physicsBody.Velocity;
            Shared.Vector3 pos = _physicsBody.Position;
            float horizontalSpeed = MathF.Sqrt(vel.X * vel.X + vel.Z * vel.Z);

            _logger.LogDebug(
                "Physics: Pos=({PosX:F2}, {PosY:F2}, {PosZ:F2}) " +
                "Vel=({VelX:F2}, {VelY:F2}, {VelZ:F2}) " +
                "HSpeed={HSpeed:F2} m/s Grounded={Grounded}",
                pos.X, pos.Y, pos.Z,
                vel.X, vel.Y, vel.Z,
                horizontalSpeed, _physicsBody.IsGrounded);
        }
    }
}
