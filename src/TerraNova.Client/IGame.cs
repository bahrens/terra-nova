using TerraNova.Client.Math;

namespace TerraNova.Client;

public interface IGame
{
    void Load(ViewportInfo viewport);

    void Update(double deltaTime);

    void Render();

    void Unload();

    void Resize(ViewportInfo viewport);
}
