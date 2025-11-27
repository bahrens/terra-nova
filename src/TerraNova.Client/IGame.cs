using TerraNova.Client.Math;

namespace TerraNova.Client;

public interface IGame : IDisposable
{
    void Load(ViewportInfo viewport);

    void Update(double deltaTime);

    void Render();

    void Resize(ViewportInfo viewport);
}
