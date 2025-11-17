using TerraNova.Core;
using TerraNova.Shared;

namespace TerraNova.Physics;

/// <summary>
/// Voxel physics implementation of IPhysicsShapeFactory.
/// Creates VoxelPhysicsShape instances for collision detection.
/// </summary>
public class VoxelShapeFactory : IPhysicsShapeFactory
{
    public IPhysicsShape CreateBox(Vector3 halfExtents)
    {
        return VoxelPhysicsShape.CreateBox(halfExtents);
    }

    public IPhysicsShape CreateSphere(float radius)
    {
        return VoxelPhysicsShape.CreateSphere(radius);
    }

    public IPhysicsShape CreateCapsule(float radius, float height)
    {
        return VoxelPhysicsShape.CreateCapsule(radius, height);
    }
}
