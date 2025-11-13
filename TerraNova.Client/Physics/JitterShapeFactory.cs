using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using Microsoft.Extensions.Logging;
using TerraNova.Core;
using TerraNova.Shared;

namespace TerraNova.Physics;

/// <summary>
/// Jitter2 implementation of IPhysicsShapeFactory.
/// Creates Jitter2-specific collision shapes wrapped in platform-agnostic adapters.
/// </summary>
public class JitterShapeFactory : IPhysicsShapeFactory
{
    private readonly ILogger<JitterShapeFactory>? _logger;

    public JitterShapeFactory(ILogger<JitterShapeFactory>? logger = null)
    {
        _logger = logger;
    }
    public IPhysicsShape CreateBox(Vector3 halfExtents)
    {
        // BoxShape constructor takes SIZE (full dimensions)
        // We receive half-extents, so multiply by 2
        float sizeX = halfExtents.X * 2;
        float sizeY = halfExtents.Y * 2;
        float sizeZ = halfExtents.Z * 2;

        _logger?.LogDebug("Creating BoxShape: halfExtents=({HalfX},{HalfY},{HalfZ}) → size=({SizeX},{SizeY},{SizeZ})",
            halfExtents.X, halfExtents.Y, halfExtents.Z, sizeX, sizeY, sizeZ);

        var boxShape = new BoxShape(sizeX, sizeY, sizeZ);
        return new JitterPhysicsShape(boxShape);
    }

    public IPhysicsShape CreateSphere(float radius)
    {
        var sphereShape = new SphereShape(radius);
        return new JitterPhysicsShape(sphereShape);
    }

    public IPhysicsShape CreateCapsule(float radius, float height)
    {
        _logger?.LogDebug("Creating CapsuleShape with radius={Radius}, height={Height}", radius, height);
        // NOTE: CapsuleShape constructor parameter order - need to verify which comes first
        var capsuleShape = new CapsuleShape(height, radius);
        _logger?.LogDebug("CapsuleShape created successfully");
        return new JitterPhysicsShape(capsuleShape);
    }
}
