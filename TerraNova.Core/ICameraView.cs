using TerraNova.Shared;

namespace TerraNova.Core;

/// <summary>
/// Provides read-only access to camera state without OpenTK coupling.
/// This interface allows the renderer to access camera position and direction
/// without depending on platform-specific camera implementations.
/// </summary>
public interface ICameraView
{
    /// <summary>
    /// Gets the current position of the camera in world space.
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// Gets the normalized front direction vector of the camera.
    /// Indicates the direction the camera is facing.
    /// </summary>
    Vector3 Front { get; }
}
