using TerraNova.Client.Math;
using TerraNova.Client.Rendering;
using TerraNova.Client.Input;

namespace TerraNova.Client;

public class TerraNovaGame : IGame
{
    private readonly IRenderer _renderer;
    private readonly IInputProvider _inputProvider;
    private ViewportInfo _viewport;

    public TerraNovaGame(IRenderer renderer, IInputProvider inputProvider)
    {
        _renderer = renderer;
        _inputProvider = inputProvider;
    }

    public void Load(ViewportInfo viewport)
    {
        _viewport = viewport;
        _renderer.Initialize();
    }

    public void Render()
    {
        _renderer.Render(_viewport);
    }

    public void Resize(ViewportInfo viewport)
    {
        _viewport = viewport;
        _renderer.Resize(viewport);
    }

    public void Dispose()
    {
        _renderer.Dispose();
    }

    public void Update(double deltaTime)
    {
        if (_inputProvider.IsKeyPressed(KeyCode.Escape))
        {
            // TODO: Handle escape key press
        }

        _renderer.Update(deltaTime);
    }
}
