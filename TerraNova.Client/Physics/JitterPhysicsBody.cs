using Jitter2.Dynamics;
using Jitter2.LinearMath;
using TerraNova.Core;
using TerraNova.Shared;

namespace TerraNova.Physics;

/// <summary>
/// Jitter2 implementation of IPhysicsBody.
/// Wraps Jitter2.Dynamics.RigidBody to provide platform-agnostic rigid body interface.
/// </summary>
public class JitterPhysicsBody : IPhysicsBody
{
    private readonly RigidBody _body;

    public JitterPhysicsBody(RigidBody body)
    {
        _body = body;
    }

    public Vector3 Position
    {
        get
        {
            var pos = _body.Position;
            return new Vector3(pos.X, pos.Y, pos.Z);
        }
        set
        {
            _body.Position = new JVector(value.X, value.Y, value.Z);
        }
    }

    public Vector3 Velocity
    {
        get
        {
            var vel = _body.Velocity;
            return new Vector3(vel.X, vel.Y, vel.Z);
        }
        set
        {
            _body.Velocity = new JVector(value.X, value.Y, value.Z);
        }
    }

    public bool AffectedByGravity
    {
        get => _body.AffectedByGravity;
        set => _body.AffectedByGravity = value;
    }

    public bool IsStatic
    {
        get => _body.MotionType == MotionType.Static;
        set
        {
            var oldMotionType = _body.MotionType;
            if (value)
            {
                _body.MotionType = MotionType.Static;
                // Note: IsActive is read-only in Jitter2. Static bodies are automatically
                // activated when MotionType is set to Static and shapes are added.
            }
            else
            {
                _body.MotionType = MotionType.Dynamic;
            }

            Console.WriteLine($"[JitterPhysicsBody] IsStatic set to {value}: MotionType changed from {oldMotionType} to {_body.MotionType}, IsActive={_body.IsActive}");
        }
    }

    public bool IsGrounded
    {
        get
        {
            // Stub implementation for Jitter2 - always returns false
            // Jitter2 doesn't have built-in ground detection, would require custom contact tracking
            // For voxel physics, this will be properly implemented in VoxelPhysicsBody
            return false;
        }
    }

    public void ApplyForce(Vector3 force)
    {
        _body.AddForce(new JVector(force.X, force.Y, force.Z));
    }

    public void SetShape(IPhysicsShape shape)
    {
        if (shape is not JitterPhysicsShape jitterShape)
            throw new ArgumentException("Shape must be JitterPhysicsShape for Jitter2 body", nameof(shape));

        // CRITICAL: In Jitter2, the body's position must be set BEFORE adding shapes
        // to prevent spatial tree corruption. AddShape() registers the shape in the
        // collision system's spatial tree at the body's CURRENT position.
        // If position is set after AddShape(), the shape remains registered at (0,0,0),
        // causing collision detection to fail.
        // Source: https://jitterphysics.com/docs/documentation/bodies/
        // "Registering many objects at (0,0,0) must be prevented by specifying
        // the rigid body position, before adding shapes."

        // Debug logging
        Console.WriteLine($"[JitterPhysicsBody] SetShape() called at position ({_body.Position.X}, {_body.Position.Y}, {_body.Position.Z}), " +
                         $"MotionType={_body.MotionType}, IsActive={_body.IsActive}");

        _body.AddShape(jitterShape.InternalShape);

        // Log after adding shape
        Console.WriteLine($"[JitterPhysicsBody] Shape added. Body now has {_body.Shapes.Count} shape(s)");
    }

    /// <summary>
    /// Internal access to Jitter2 RigidBody for adapter classes.
    /// </summary>
    internal RigidBody InternalBody => _body;
}
