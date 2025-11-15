using Jitter2.Collision.Shapes;
using TerraNova.Core;

namespace TerraNova.Physics;

/// <summary>
/// Jitter2 implementation of IPhysicsShape.
/// Wraps Jitter2.Collision.Shapes.RigidBodyShape to provide platform-agnostic collision shape interface.
/// </summary>
public class JitterPhysicsShape : IPhysicsShape
{
    private readonly RigidBodyShape _shape;

    public JitterPhysicsShape(RigidBodyShape shape)
    {
        _shape = shape;
    }

    /// <summary>
    /// Internal access to Jitter2 RigidBodyShape for adapter classes.
    /// </summary>
    internal RigidBodyShape InternalShape => _shape;
}
