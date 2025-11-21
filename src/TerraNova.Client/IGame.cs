namespace TerraNova.Client;

public interface IGame
{
    void Load(Viewport viewport);

    void Update(double deltaTime);

    void Render();

    void Unload();

    void Resize(Viewport viewport);
}
