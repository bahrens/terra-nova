using Microsoft.Extensions.Logging;

namespace TerraNova;

/// <summary>
/// Manages window-level state (FPS tracking, title updates, fullscreen toggling).
/// Separates window management concerns from game logic.
/// </summary>
public class WindowStateManager
{
    private readonly ILogger<WindowStateManager> _logger;

    private double _fpsUpdateTimer = 0.0;
    private int _frameCount = 0;
    private double _currentFPS = 0.0;

    public double CurrentFPS => _currentFPS;

    public WindowStateManager(ILogger<WindowStateManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Update FPS tracking (called every frame).
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds</param>
    public void Update(double deltaTime)
    {
        _frameCount++;
        _fpsUpdateTimer += deltaTime;

        if (_fpsUpdateTimer >= 0.5) // Update every 0.5 seconds
        {
            _currentFPS = _frameCount / _fpsUpdateTimer;
            _frameCount = 0;
            _fpsUpdateTimer = 0.0;
        }
    }
}
