using OpenTK.Mathematics;
using TerraNova.Core;

namespace TerraNova;

/// <summary>
/// First-person camera with keyboard and mouse controls
/// </summary>
public class Camera : ICameraView
{
    // Camera position and orientation (OpenTK vectors for internal use)
    public Vector3 Position { get; set; }
    public Vector3 Front => _front;  // Public accessor for camera direction
    public Vector3 Right => _right;  // Public accessor for right direction
    private Vector3 _front = -Vector3.UnitZ;  // Direction camera is facing
    private Vector3 _up = Vector3.UnitY;      // Up direction
    private Vector3 _right = Vector3.UnitX;   // Right direction

    // ICameraView implementation (platform-agnostic vectors for external use)
    Shared.Vector3 ICameraView.Position => new Shared.Vector3(Position.X, Position.Y, Position.Z);
    Shared.Vector3 ICameraView.Front => new Shared.Vector3(_front.X, _front.Y, _front.Z);

    // Rotation angles (in radians)
    private float _pitch = 0.0f;  // Up/down rotation
    private float _yaw = -MathHelper.PiOver2;  // Left/right rotation (starts facing -Z)

    // Camera settings
    public float Speed { get; set; } = 5.0f;      // Movement speed (units per second)
    public float Sensitivity { get; set; } = 0.002f; // Mouse sensitivity
    public float Fov { get; set; } = MathHelper.DegreesToRadians(70.0f); // Field of view (stored in radians)

    public Camera(Vector3 position)
    {
        Position = position;
        UpdateVectors();
    }

    /// <summary>
    /// Returns the view matrix for the shader
    /// </summary>
    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Position + _front, _up);
    }

    /// <summary>
    /// Returns the projection matrix for the shader
    /// </summary>
    public Matrix4 GetProjectionMatrix(float aspectRatio)
    {
        return Matrix4.CreatePerspectiveFieldOfView(Fov, aspectRatio, 0.1f, 200.0f);
    }

    /// <summary>
    /// Process keyboard input for camera movement
    /// </summary>
    public void ProcessKeyboard(CameraMovement direction, float deltaTime)
    {
        float velocity = Speed * deltaTime;

        // For horizontal movement (WASD), use front vector without pitch
        // This keeps movement on the horizontal plane regardless of where the camera is looking
        Vector3 frontHorizontal = new Vector3(_front.X, 0, _front.Z);
        if (frontHorizontal.LengthSquared > 0)
        {
            frontHorizontal = Vector3.Normalize(frontHorizontal);
        }

        Vector3 rightHorizontal = new Vector3(_right.X, 0, _right.Z);
        if (rightHorizontal.LengthSquared > 0)
        {
            rightHorizontal = Vector3.Normalize(rightHorizontal);
        }

        switch (direction)
        {
            case CameraMovement.Forward:
                Position += frontHorizontal * velocity;
                break;
            case CameraMovement.Backward:
                Position -= frontHorizontal * velocity;
                break;
            case CameraMovement.Left:
                Position -= rightHorizontal * velocity;
                break;
            case CameraMovement.Right:
                Position += rightHorizontal * velocity;
                break;
            case CameraMovement.Up:
                Position += Vector3.UnitY * velocity;
                break;
            case CameraMovement.Down:
                Position -= Vector3.UnitY * velocity;
                break;
        }
    }

    /// <summary>
    /// Process mouse movement for camera rotation
    /// </summary>
    public void ProcessMouseMovement(float xOffset, float yOffset, bool constrainPitch = true)
    {
        xOffset *= Sensitivity;
        yOffset *= Sensitivity;

        _yaw += xOffset;
        _pitch += yOffset;

        // Constrain pitch to prevent camera flipping
        if (constrainPitch)
        {
            if (_pitch > MathHelper.DegreesToRadians(89.0f))
                _pitch = MathHelper.DegreesToRadians(89.0f);
            if (_pitch < MathHelper.DegreesToRadians(-89.0f))
                _pitch = MathHelper.DegreesToRadians(-89.0f);
        }

        UpdateVectors();
    }

    /// <summary>
    /// Process mouse scroll for FOV changes (zoom)
    /// </summary>
    public void ProcessMouseScroll(float yOffset)
    {
        Fov -= MathHelper.DegreesToRadians(yOffset);
        if (Fov < MathHelper.DegreesToRadians(1.0f))
            Fov = MathHelper.DegreesToRadians(1.0f);
        if (Fov > MathHelper.DegreesToRadians(120.0f))
            Fov = MathHelper.DegreesToRadians(120.0f);
    }

    /// <summary>
    /// Update camera direction vectors based on yaw and pitch
    /// </summary>
    private void UpdateVectors()
    {
        // Calculate the new front vector
        Vector3 front;
        front.X = MathF.Cos(_yaw) * MathF.Cos(_pitch);
        front.Y = MathF.Sin(_pitch);
        front.Z = MathF.Sin(_yaw) * MathF.Cos(_pitch);
        _front = Vector3.Normalize(front);

        // Recalculate right and up vectors
        _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
        _up = Vector3.Normalize(Vector3.Cross(_right, _front));
    }
}

/// <summary>
/// Camera movement directions
/// </summary>
public enum CameraMovement
{
    Forward,
    Backward,
    Left,
    Right,
    Up,
    Down
}
