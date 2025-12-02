using System.Numerics;

namespace TerraNova.Client.Rendering;

public class Camera : ICameraView
{
    // Camera orientation is defined by yaw (rotation around the Y axis) and pitch (rotation around the X axis).
    private float _yaw = -90f; // Facing towards negative Z by default
    private float _pitch = 0f; // Level with the horizon by default

    // Cached direction vectors
    private Vector3 _front = Vector3.UnitZ * -1; // Initially facing negative Z
    private Vector3 _up = Vector3.UnitY; // World up
    private Vector3 _right = Vector3.UnitX; // Initially facing positive X

    public Vector3 Position { get; set; } = new Vector3(0, 0, 3);
    public Vector3 Front => _front;
    public Vector3 Up => _up;
    public Vector3 Right => _right;
    public float Fov { get; set; } = 60f;

    // Near/far clip planes for the projection
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 1000f;

    public float Yaw
    {
        get => _yaw;
        set
        {
            _yaw = value;
            UpdateVectors();
        }
    }

    public float Pitch
    {
        get => _pitch;
        set
        {
            // Constrain the pitch to prevent gimbal lock
            _pitch = System.Math.Clamp(value, -89f, 89f);
            UpdateVectors();
        }
    }

    public Matrix4x4 GetProjectionMatrix(float aspectRatio)
    {
        // Convert FOV from degrees to radians
        float fovRad = MathF.PI / 180f * Fov;
        return Matrix4x4.CreatePerspectiveFieldOfView(fovRad, aspectRatio, NearPlane, FarPlane);
    }

    public Matrix4x4 GetViewMatrix()
    {
        // LookAt matrix: Creates a view matrix from position, target and up vector
        return Matrix4x4.CreateLookAt(Position, Position + Front, _up);
    }

    private void UpdateVectors()
    {
        // Convert angles from degrees to radians
        var yawRad = MathF.PI / 180f * _yaw;
        var pitchRad = MathF.PI / 180f * _pitch;

        // calculate the new Front vector
        _front = Vector3.Normalize(new Vector3
        (
            MathF.Cos(yawRad) * MathF.Cos(pitchRad),
            MathF.Sin(pitchRad),
            MathF.Sin(yawRad) * MathF.Cos(pitchRad)
        ));
        // Recalculate right and up vectors
        _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
        _up = Vector3.Normalize(Vector3.Cross(_right, _front));
    }
}
