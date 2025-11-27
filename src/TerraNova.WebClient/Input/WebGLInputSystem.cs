using System.Numerics;
using Microsoft.JSInterop;
using TerraNova.Client.Input;

namespace TerraNova.WebClient.Input;

public class WebGLInputSystem : IInputSystem
{
    private readonly IJSRuntime _jsRuntime;

    // Keyboard state tracking
    private readonly HashSet<KeyCode> _currentKeys = new();
    private readonly HashSet<KeyCode> _previousKeys = new();

    // Mouse state tracking
    private readonly HashSet<MouseButtonCode> _currentMouseButtons = new();
    private readonly HashSet<MouseButtonCode> _previousMouseButtons = new();

    private Vector2 _currentMousePosition;
    private Vector2 _previousMousePosition;
    private float _mouseScrollDelta;
    private float _accumulatedScrollDelta;

    public WebGLInputSystem(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public Vector2 MousePosition => _currentMousePosition;
    public Vector2 MouseDelta => _currentMousePosition - _previousMousePosition;
    public float MouseScrollDelta => _mouseScrollDelta;

    public void BeginFrame()
    {
        // Copy current to previous
        _previousKeys.Clear();
        foreach (var key in _currentKeys)
            _previousKeys.Add(key);

        _previousMouseButtons.Clear();
        foreach (var button in _currentMouseButtons)
            _previousMouseButtons.Add(button);

        _previousMousePosition = _currentMousePosition;

        // Transfer accumulated scroll to this frame's delta, then reset accumulator
        _mouseScrollDelta = _accumulatedScrollDelta;
        _accumulatedScrollDelta = 0f;
    }

    public bool IsKeyDown(KeyCode keyCode) => _currentKeys.Contains(keyCode);
    public bool IsKeyPressed(KeyCode keyCode) => _currentKeys.Contains(keyCode) && !_previousKeys.Contains(keyCode);
    public bool IsKeyReleased(KeyCode keyCode) => !_currentKeys.Contains(keyCode) && _previousKeys.Contains(keyCode);

    public bool IsMouseButtonDown(MouseButtonCode button) => _currentMouseButtons.Contains(button);
    public bool IsMouseButtonPressed(MouseButtonCode button) => _currentMouseButtons.Contains(button) && !_previousMouseButtons.Contains(button);
    public bool IsMouseButtonReleased(MouseButtonCode button) => !_currentMouseButtons.Contains(button) && _previousMouseButtons.Contains(button);

    // Called from JS to update key state
    [JSInvokable]
    public void OnKeyDown(string key)
    {
        var keyCode = MapJsKey(key);
        if (keyCode != KeyCode.None)
            _currentKeys.Add(keyCode);
    }

    [JSInvokable]
    public void OnKeyUp(string key)
    {
        var keyCode = MapJsKey(key);
        if (keyCode != KeyCode.None)
            _currentKeys.Remove(keyCode);
    }

    [JSInvokable]
    public void OnMouseMove(float x, float y)
    {
        _currentMousePosition = new Vector2(x, y);
    }

    [JSInvokable]
    public void OnMouseDown(int button)
    {
        var btn = MapJsMouseButton(button);
        _currentMouseButtons.Add(btn);
    }

    [JSInvokable]
    public void OnMouseUp(int button)
    {
        var btn = MapJsMouseButton(button);
        _currentMouseButtons.Remove(btn);
    }

    [JSInvokable]
    public void OnMouseWheel(float delta)
    {
        _accumulatedScrollDelta += delta;
    }

    private static KeyCode MapJsKey(string key) => key switch
    {
        "w" or "W" => KeyCode.W,
        "a" or "A" => KeyCode.A,
        "s" or "S" => KeyCode.S,
        "d" or "D" => KeyCode.D,
        "q" or "Q" => KeyCode.Q,
        "e" or "E" => KeyCode.E,
        "r" or "R" => KeyCode.R,
        "t" or "T" => KeyCode.T,
        "j" or "J" => KeyCode.J,
        " " => KeyCode.Space,
        "Shift" => KeyCode.LeftShift,
        "Control" => KeyCode.LeftControl,
        "Escape" => KeyCode.Escape,
        "Enter" => KeyCode.Enter,
        "Tab" => KeyCode.Tab,
        "Backspace" => KeyCode.Backspace,
        "F1" => KeyCode.F1,
        "F2" => KeyCode.F2,
        "F3" => KeyCode.F3,
        "F4" => KeyCode.F4,
        "F5" => KeyCode.F5,
        "F11" => KeyCode.F11,
        "0" => KeyCode.D0,
        "1" => KeyCode.D1,
        "2" => KeyCode.D2,
        "3" => KeyCode.D3,
        "4" => KeyCode.D4,
        "5" => KeyCode.D5,
        "6" => KeyCode.D6,
        "7" => KeyCode.D7,
        "8" => KeyCode.D8,
        "9" => KeyCode.D9,
        _ => KeyCode.None
    };

    private static MouseButtonCode MapJsMouseButton(int button) => button switch
    {
        0 => MouseButtonCode.Left,
        1 => MouseButtonCode.Middle,
        2 => MouseButtonCode.Right,
        _ => MouseButtonCode.Left
    };
}
