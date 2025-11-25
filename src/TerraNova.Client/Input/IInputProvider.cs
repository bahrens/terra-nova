using System.Numerics;

namespace TerraNova.Client.Input;

/// <summary>
/// Platform-agnostic input query interface.
/// Provides readonly access to current frame's input state.
/// Game logic should depend on this interface.
/// </summary>
public interface IInputProvider
{
    bool IsKeyDown(KeyCode keyCode);

    bool IsKeyPressed(KeyCode keyCode);

    bool IsKeyReleased(KeyCode keyCode);

    bool IsMouseButtonDown(MouseButton mouseButton);

    bool IsMouseButtonPressed(MouseButton mouseButton);

    bool IsMouseButtonReleased(MouseButton mouseButton);

    Vector2 MousePosition { get; }

    Vector2 MouseDelta { get; }

    float MouseScrollDelta { get; }
}
