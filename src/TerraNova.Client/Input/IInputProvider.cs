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

    bool IsMouseButtonDown(MouseButtonCode mouseButton);

    bool IsMouseButtonPressed(MouseButtonCode mouseButton);

    bool IsMouseButtonReleased(MouseButtonCode mouseButton);

    Vector2 MousePosition { get; }

    Vector2 MouseDelta { get; }

    float MouseScrollDelta { get; }
}
