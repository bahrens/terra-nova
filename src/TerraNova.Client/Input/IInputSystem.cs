namespace TerraNova.Client.Input;

/// <summary>
/// Lifecycle management interface for input systems.
/// Only game loop infrastructure should depend on this.
/// </summary>
public interface IInputSystem : IInputProvider
{
    /// <summary>
    /// Captures current frame input state. Must be called exactly once per frame
    /// BEFORE game Update(). Platform implementations use this to:
    /// - Snapshot accumulated event data (Blazor)
    /// - Update frame-to-frame comparison for Pressed/Released detection (OpenTK)
    /// - Clear previous frame state
    /// </summary>
    void BeginFrame();
}
