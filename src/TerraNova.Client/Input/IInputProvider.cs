using System.Numerics;

namespace TerraNova.Client.Input;   

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

    void PollInput();
}
