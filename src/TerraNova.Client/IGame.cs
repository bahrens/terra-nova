namespace TerraNova.Client;

public interface IGame
{
    void Load();

    void Update(double deltaTime);

    void Render();

    void Unload();
}
