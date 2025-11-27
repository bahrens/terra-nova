using TerraNova.Client.Math;

namespace TerraNova.Client;

public interface IGame
{
    Task LoadAsync(ViewportInfo viewport);

    void Update(double deltaTime);

    void Render();

    Task UnloadAsync();

    void Resize(ViewportInfo viewport);
}
