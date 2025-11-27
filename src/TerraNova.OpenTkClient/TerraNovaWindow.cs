using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TerraNova.Client;
using TerraNova.Client.Input;
using TerraNova.Client.Math;
using TerraNova.OpenTkClient.Input;
using TerraNova.OpenTkClient.Rendering;

namespace TerraNova.OpenTkClient;

public class TerraNovaWindow : GameWindow
{
    private readonly TerraNovaGame _game;
    private readonly IInputSystem _inputSystem;

    public TerraNovaWindow(
        GameWindowSettings gameWindowSettings,
        NativeWindowSettings nativeWindowSettings
    )
        : base(gameWindowSettings, nativeWindowSettings)
    {
        var renderer = new OpenTkRenderer();
        _inputSystem = new OpenTkInputSystem(this);
        _game = new TerraNovaGame(renderer, _inputSystem);
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        _game.LoadAsync(new ViewportInfo(Size.X, Size.Y)).GetAwaiter().GetResult();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        _inputSystem.BeginFrame();
        _game.Update(args.Time);

        if (_inputSystem.IsKeyPressed(KeyCode.Escape))
        {
            Close();
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        _game.Render();

        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs args)
    {
        base.OnResize(args);
        _game.Resize(new ViewportInfo(args.Width, args.Height));
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        _game.UnloadAsync().GetAwaiter().GetResult();
    }
}
