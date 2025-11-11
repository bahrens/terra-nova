using Microsoft.Extensions.Logging;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TerraNova.GameLogic;
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
    private readonly ILogger<PlayerController> _logger;

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
    /// Creates a new PlayerController with the specified dependencies
    /// </summary>
    /// <param name="camera">The camera for raycasting and positioning</param>
    /// <param name="networkClient">Network client for sending block updates</param>
    /// <param name="logger">Logger for diagnostic output</param>
    public PlayerController(Camera camera, INetworkClient networkClient, ILogger<PlayerController> logger)
    {
        _camera = camera;
        _networkClient = networkClient;
        _logger = logger;
    }

    /// <summary>
    /// Updates player state based on input and world state.
    /// Should be called once per frame during the game update loop.
    /// </summary>
    /// <param name="keyboardState">Current keyboard state</param>
    /// <param name="mouseState">Current mouse state</param>
    /// <param name="world">The game world for raycasting and block interaction</param>
    /// <param name="deltaTime">Time elapsed since last frame in seconds</param>
    public void Update(KeyboardState keyboardState, MouseState mouseState, World world, double deltaTime)
    {
        // TODO: Implement in Task 2.2 - Move camera input handling here
        // TODO: Implement in Task 2.3 - Call UpdateRaycast, HandleHotbarSelection, HandleBlockInteraction
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
}
