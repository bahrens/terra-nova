using System.Numerics;

namespace TerraNova.Client;

public interface ICameraView
{
    Vector3 Position { get; }
    Vector3 Front { get; }
    Vector3 Up { get; }
    Vector3 Right { get; }
    float Fov { get; }

    Matrix4x4 GetViewMatrix();
    Matrix4x4 GetProjectionMatrix(float aspectRatio);
}