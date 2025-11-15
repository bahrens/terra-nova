namespace TerraNova.Configuration;

/// <summary>
/// Camera configuration settings
/// </summary>
public class CameraSettings
{
    public float MovementSpeed { get; set; } = 5.0f;
    public float MouseSensitivity { get; set; } = 0.002f;
    public float FieldOfView { get; set; } = 45.0f;

    // Camera spawn position
    public float SpawnX { get; set; } = 0.0f;
    public float SpawnY { get; set; } = 75.0f;
    public float SpawnZ { get; set; } = 50.0f;
}
