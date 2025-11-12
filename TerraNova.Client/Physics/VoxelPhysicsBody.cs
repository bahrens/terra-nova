using TerraNova.Core;
using TerraNova.Shared;

namespace TerraNova.Physics;

/// <summary>
/// Voxel physics implementation of IPhysicsBody.
/// Stores physics state for a body and integrates with VoxelCollisionSystem.
/// </summary>
public class VoxelPhysicsBody : IPhysicsBody
{
    private Vector3 _position;
    private Vector3 _velocity;
    private bool _affectedByGravity;
    private bool _isStatic;
    private VoxelPhysicsShape? _shape;
    private bool _isGrounded;

    public VoxelPhysicsBody()
    {
        _position = Vector3.Zero;
        _velocity = Vector3.Zero;
        _affectedByGravity = false;
        _isStatic = false;
        _isGrounded = false;
    }

    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }

    public Vector3 Velocity
    {
        get => _velocity;
        set => _velocity = value;
    }

    public bool AffectedByGravity
    {
        get => _affectedByGravity;
        set => _affectedByGravity = value;
    }

    public bool IsStatic
    {
        get => _isStatic;
        set => _isStatic = value;
    }

    public bool IsGrounded
    {
        get => _isGrounded;
        internal set => _isGrounded = value; // Set by VoxelCollisionSystem during collision detection
    }

    public void ApplyForce(Vector3 force)
    {
        // For voxel physics, we apply impulse directly to velocity
        // Force is treated as instantaneous impulse (force * deltaTime with deltaTime = 1 frame)
        // This works for jump impulses and similar instantaneous forces
        if (!_isStatic)
        {
            _velocity += force;
        }
    }

    public void SetShape(IPhysicsShape shape)
    {
        if (shape is not VoxelPhysicsShape voxelShape)
        {
            throw new ArgumentException("Shape must be VoxelPhysicsShape for voxel physics body", nameof(shape));
        }

        _shape = voxelShape;
    }

    /// <summary>
    /// Get the collision shape for this body.
    /// </summary>
    public VoxelPhysicsShape? Shape => _shape;
}
