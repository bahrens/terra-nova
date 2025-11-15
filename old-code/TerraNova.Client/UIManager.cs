using Microsoft.Extensions.Logging;

namespace TerraNova;

/// <summary>
/// Manages all UI overlays (crosshair, hotbar, HUD elements).
/// Centralizes UI lifecycle and rendering logic.
/// </summary>
public class UIManager : IDisposable
{
    private readonly PlayerController _playerController;
    private readonly ILogger<UIManager> _logger;

    private Crosshair _crosshair = null!;
    private Hotbar _hotbar = null!;

    public UIManager(PlayerController playerController, ILogger<UIManager> logger)
    {
        _playerController = playerController;
        _logger = logger;
    }

    /// <summary>
    /// Initialize all UI elements.
    /// </summary>
    /// <param name="windowWidth">Window width in pixels</param>
    /// <param name="windowHeight">Window height in pixels</param>
    public void Initialize(int windowWidth, int windowHeight)
    {
        _crosshair = new Crosshair();
        _crosshair.Initialize(windowWidth, windowHeight);

        _hotbar = new Hotbar(_playerController.HotbarBlocks);
        _hotbar.Initialize(windowWidth, windowHeight, _playerController.SelectedHotbarSlot);

        _logger.LogInformation("UI Manager initialized");
    }

    /// <summary>
    /// Handle hotbar selection change from PlayerController.
    /// </summary>
    /// <param name="newSlot">New selected slot (0-8)</param>
    public void OnHotbarSelectionChanged(int newSlot)
    {
        _hotbar.UpdateSelectedSlot(newSlot);
    }

    /// <summary>
    /// Handle window resize.
    /// </summary>
    /// <param name="width">New window width</param>
    /// <param name="height">New window height</param>
    public void OnResize(int width, int height)
    {
        _crosshair.OnResize(width, height);
        _hotbar.OnResize(width, height);
    }

    /// <summary>
    /// Draw all UI elements.
    /// </summary>
    /// <param name="aspectRatio">Window aspect ratio for crosshair rendering</param>
    public void Draw(float aspectRatio)
    {
        _crosshair.Draw(aspectRatio);
        _hotbar.Draw();
    }

    public void Dispose()
    {
        _crosshair?.Dispose();
        _hotbar?.Dispose();
    }
}
