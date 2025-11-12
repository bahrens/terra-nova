using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using TerraNova.Core;
using TerraNova.Shared;

namespace TerraNova.Physics;

/// <summary>
/// Jitter2 implementation of IPhysicsShapeFactory.
/// Creates Jitter2-specific collision shapes wrapped in platform-agnostic adapters.
/// </summary>
public class JitterShapeFactory : IPhysicsShapeFactory
{
    public IPhysicsShape CreateBox(Vector3 halfExtents)
    {
        // BoxShape constructor takes SIZE (full dimensions)
        // We receive half-extents, so multiply by 2
        float sizeX = halfExtents.X * 2;
        float sizeY = halfExtents.Y * 2;
        float sizeZ = halfExtents.Z * 2;

        Console.WriteLine($"[JitterShapeFactory] Creating BoxShape: halfExtents=({halfExtents.X},{halfExtents.Y},{halfExtents.Z}) → size=({sizeX},{sizeY},{sizeZ})");

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
        Console.WriteLine($"[JitterShapeFactory] Creating CapsuleShape with radius={radius}, height={height}");
        // NOTE: CapsuleShape constructor parameter order - need to verify which comes first
        var capsuleShape = new CapsuleShape(height, radius);
        Console.WriteLine($"[JitterShapeFactory] CapsuleShape created successfully");
        return new JitterPhysicsShape(capsuleShape);
    }
}
