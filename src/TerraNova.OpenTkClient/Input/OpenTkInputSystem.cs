using System.Numerics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TerraNova.Client.Input;

namespace TerraNova.OpenTkClient.Input;

public class OpenTkInputSystem : IInputSystem
{
    private static readonly KeyCode[] AllKeyCodes = Enum.GetValues<KeyCode>()
        .Where(k => k != KeyCode.None)
        .ToArray();

    private readonly GameWindow _window;

    // Keyboard state tracking
    private readonly HashSet<KeyCode> _currentKeys = new();
    private readonly HashSet<KeyCode> _previousKeys = new();

    // Mouse state tracking
    private readonly HashSet<MouseButtonCode> _currentMouseButtons = new();
    private readonly HashSet<MouseButtonCode> _previousMouseButtons = new();

    // Mouse position tracking
    private Vector2 _currentMousePosition;
    private Vector2 _previousMousePosition;
    private float _mouseScrollDelta;

    public OpenTkInputSystem(GameWindow window)
    {
        _window = window;
    }

    public Vector2 MousePosition => _currentMousePosition;

    public Vector2 MouseDelta => _currentMousePosition - _previousMousePosition;

    public float MouseScrollDelta => _mouseScrollDelta;

    public void BeginFrame()
    {
        // Copy current state to previous state
        _previousKeys.Clear();
        foreach (var key in _currentKeys)
        {
            _previousKeys.Add(key);
        }

        _previousMouseButtons.Clear();
        foreach (var button in _currentMouseButtons)
        {
            _previousMouseButtons.Add(button);
        }

        _previousMousePosition = _currentMousePosition;
        _mouseScrollDelta = 0f;

        // Sample current state from OpenTK
        _currentKeys.Clear();
        var keyboardState = _window.KeyboardState;
        foreach (var keyCode in AllKeyCodes)
        {
            var openTkKey = MapKeyCode(keyCode);
            if (keyboardState.IsKeyDown(openTkKey))
            {
                _currentKeys.Add(keyCode);
            }
        }

        // Sample mouse state
        _currentMouseButtons.Clear();
        var mouseState = _window.MouseState;
        if (mouseState.IsButtonDown(MouseButton.Left))
        {
            _currentMouseButtons.Add(MouseButtonCode.Left);
        }
        if (mouseState.IsButtonDown(MouseButton.Right))
        {
            _currentMouseButtons.Add(MouseButtonCode.Right);
        }
        if (mouseState.IsButtonDown(MouseButton.Middle))
        {
            _currentMouseButtons.Add(MouseButtonCode.Middle);
        }

        _currentMousePosition = new Vector2(mouseState.X, mouseState.Y);
        _mouseScrollDelta = mouseState.ScrollDelta.Y;
    }

    public bool IsKeyDown(KeyCode keyCode) => _currentKeys.Contains(keyCode);
    public bool IsKeyPressed(KeyCode keyCode) => _currentKeys.Contains(keyCode) && !_previousKeys.Contains(keyCode);
    public bool IsKeyReleased(KeyCode keyCode) => !_currentKeys.Contains(keyCode) && _previousKeys.Contains(keyCode);

    public bool IsMouseButtonDown(MouseButtonCode mouseButton) => _currentMouseButtons.Contains(mouseButton);
    public bool IsMouseButtonPressed(MouseButtonCode mouseButton) => _currentMouseButtons.Contains(mouseButton) && !_previousMouseButtons.Contains(mouseButton);
    public bool IsMouseButtonReleased(MouseButtonCode mouseButton) => !_currentMouseButtons.Contains(mouseButton) && _previousMouseButtons.Contains(mouseButton);

    private static Keys MapKeyCode(KeyCode keyCode) => keyCode switch
    {
        KeyCode.W => Keys.W,
        KeyCode.A => Keys.A,
        KeyCode.S => Keys.S,
        KeyCode.D => Keys.D,
        KeyCode.Q => Keys.Q,
        KeyCode.E => Keys.E,
        KeyCode.R => Keys.R,
        KeyCode.T => Keys.T,
        KeyCode.J => Keys.J,
        KeyCode.Space => Keys.Space,
        KeyCode.LeftShift => Keys.LeftShift,
        KeyCode.LeftControl => Keys.LeftControl,
        KeyCode.Escape => Keys.Escape,
        KeyCode.Enter => Keys.Enter,
        KeyCode.Tab => Keys.Tab,
        KeyCode.Backspace => Keys.Backspace,
        KeyCode.F1 => Keys.F1,
        KeyCode.F2 => Keys.F2,
        KeyCode.F3 => Keys.F3,
        KeyCode.F4 => Keys.F4,
        KeyCode.F5 => Keys.F5,
        KeyCode.F11 => Keys.F11,
        KeyCode.D0 => Keys.D0,
        KeyCode.D1 => Keys.D1,
        KeyCode.D2 => Keys.D2,
        KeyCode.D3 => Keys.D3,
        KeyCode.D4 => Keys.D4,
        KeyCode.D5 => Keys.D5,
        KeyCode.D6 => Keys.D6,
        KeyCode.D7 => Keys.D7,
        KeyCode.D8 => Keys.D8,
        KeyCode.D9 => Keys.D9,
        _ => Keys.Unknown
    };
}
