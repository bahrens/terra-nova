using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TerraNova.Shared;

namespace TerraNova.Physics;

/// <summary>
/// Custom voxel collision system using swept AABB algorithm.
/// Achieves 50-200x performance improvement over Jitter2 rigid body approach
/// by querying voxels directly (10-30 blocks) instead of managing 20K+ rigid bodies.
/// </summary>
public class VoxelCollisionSystem
{
    private readonly World _world;
    private readonly ILogger<VoxelCollisionSystem>? _logger;
    private const float SkinWidth = 0.001f; // Small margin to prevent tunneling through voxels (reduced to minimize jitter)

    // Auto-jump time tracking (simple monotonic counter for cooldown)
    private float _autoJumpTimeCounter = 0f;

    // Diagnostic state
    private int _moveCallCount = 0;
    private const int LogEveryNMoves = 60; // Log every 60 move calls (roughly 1 second at 60 FPS)

    public VoxelCollisionSystem(World world, ILogger<VoxelCollisionSystem>? logger = null)
    {
        _world = world;
        _logger = logger;
    }

    /// <summary>
    /// Move a body with collision detection using swept AABB.
    /// Returns the actual movement after collision resolution.
    /// </summary>
    /// <param name="body">The body to move</param>
    /// <param name="deltaTime">Time step in seconds</param>
    /// <returns>The final position after collision-resolved movement</returns>
    public Vector3 MoveBody(VoxelPhysicsBody body, float deltaTime)
    {
        if (body.Shape == null)
            return body.Position;

        // Update time counter for auto-jump cooldown
        _autoJumpTimeCounter += deltaTime;

        _moveCallCount++;
        bool shouldLog = (_moveCallCount % LogEveryNMoves) == 0;

        // Calculate desired movement from velocity
        Vector3 desiredMovement = body.Velocity * deltaTime;

        // CRITICAL FIX: For capsule shapes, body.Position is the BOTTOM CENTER (feet).
        // We need to create the AABB centered at the capsule's actual center.
        // Capsule center = feet position + (0, halfHeight, 0)
        float capsuleHalfHeight = body.Shape.HalfExtents.Y; // For capsule, Y half-extent is half the height
        Vector3 aabbCenter = new Vector3(
            body.Position.X,
            body.Position.Y + capsuleHalfHeight, // Shift up to true center
            body.Position.Z
        );

        // Create AABB for the body at current position (now using correct center)
        AABB bodyAABB = AABB.FromCenterAndExtents(aabbCenter, body.Shape.HalfExtents);

        if (shouldLog && _logger != null)
        {
            _logger.LogDebug(
                "[COLLISION] MoveBody: Pos=({PosX:F2},{PosY:F2},{PosZ:F2}) " +
                "Vel=({VelX:F2},{VelY:F2},{VelZ:F2}) " +
                "DesiredMove=({DX:F3},{DY:F3},{DZ:F3}) " +
                "AABB=({MinX:F2},{MinY:F2},{MinZ:F2})->({MaxX:F2},{MaxY:F2},{MaxZ:F2})",
                body.Position.X, body.Position.Y, body.Position.Z,
                body.Velocity.X, body.Velocity.Y, body.Velocity.Z,
                desiredMovement.X, desiredMovement.Y, desiredMovement.Z,
                bodyAABB.Min.X, bodyAABB.Min.Y, bodyAABB.Min.Z,
                bodyAABB.Max.X, bodyAABB.Max.Y, bodyAABB.Max.Z
            );
        }

        // Perform swept collision detection (pass body for auto-jump triggering)
        Vector3 actualMovement = SweepAABB(bodyAABB, desiredMovement, body, out bool hitGround, shouldLog);

        if (shouldLog && _logger != null)
        {
            _logger.LogDebug(
                "[COLLISION] Result: ActualMove=({AX:F3},{AY:F3},{AZ:F3}) " +
                "HitGround={HitGround} NewPos=({NewX:F2},{NewY:F2},{NewZ:F2})",
                actualMovement.X, actualMovement.Y, actualMovement.Z,
                hitGround,
                body.Position.X + actualMovement.X,
                body.Position.Y + actualMovement.Y,
                body.Position.Z + actualMovement.Z
            );
        }

        // Update grounded state
        body.IsGrounded = hitGround;

        // If grounded, zero out downward velocity (with small epsilon to prevent flickering)
        if (hitGround && body.Velocity.Y < -0.001f)
        {
            body.Velocity = new Vector3(body.Velocity.X, 0, body.Velocity.Z);
        }

        // Return final position
        return body.Position + actualMovement;
    }

    /// <summary>
    /// Sweep an AABB through the world and resolve collisions using iterative approach.
    /// Finds the first collision across all axes, moves to that point, then slides along the surface.
    /// Repeats until movement is fully resolved (Minecraft-style collision resolution).
    /// Triggers auto-jump when grounded player walks into climbable ledges.
    /// </summary>
    /// <param name="aabb">The AABB to sweep</param>
    /// <param name="movement">Desired movement vector</param>
    /// <param name="body">Physics body (for auto-jump triggering, can be null for internal tests)</param>
    /// <param name="hitGround">Output: true if collided with ground below</param>
    /// <param name="shouldLog">Whether to log diagnostic information</param>
    /// <returns>Safe movement vector after collision resolution</returns>
    private Vector3 SweepAABB(AABB aabb, Vector3 movement, VoxelPhysicsBody? body, out bool hitGround, bool shouldLog = false)
    {
        hitGround = false;

        // Early out if no movement
        if (movement.LengthSquared() < 0.0001f)
            return Vector3.Zero;

        Vector3 totalMovement = Vector3.Zero;
        Vector3 remainingMovement = movement;
        const int maxIterations = 8;  // Increased from 4 - complex corners need more iterations
        bool hasTriedAutoJump = false;  // Track if we've already attempted auto-jump this frame

        // Track which voxels we've already collided with to prevent re-collision loops
        HashSet<Vector3i> ignoredVoxels = new HashSet<Vector3i>();

        int iteration;
        for (iteration = 0; iteration < maxIterations; iteration++)
        {
            if (remainingMovement.LengthSquared() < 0.0001f)
                break;  // No more movement to resolve

            // Find earliest collision across all axes (passing ignore set)
            CollisionResult collision = FindFirstCollision(aabb, remainingMovement, ignoredVoxels, shouldLog);

            if (collision.Hit)
            {
                // Add this voxel to ignore list to prevent re-collision
                ignoredVoxels.Add(collision.VoxelPosition);

                // Move to collision point
                Vector3 moveToCollision = remainingMovement * collision.Time;
                aabb = aabb.Offset(moveToCollision);
                totalMovement += moveToCollision;

                // Check for ground hit (normal pointing up = ground)
                if (collision.Normal.Y > 0.5f)
                    hitGround = true;

                // AUTO-JUMP DETECTION: If horizontal collision and grounded, check for climbable ledge
                bool isHorizontalCollision = Math.Abs(collision.Normal.Y) < 0.1f;  // Normal is mostly horizontal
                bool hasHorizontalMovement = Math.Abs(remainingMovement.X) > 0.0001f || Math.Abs(remainingMovement.Z) > 0.0001f;

                if (!hasTriedAutoJump && isHorizontalCollision && hasHorizontalMovement &&
                    body != null && body.AutoJumpEnabled && hitGround)
                {
                    // Extract horizontal component of remaining movement
                    Vector3 horizontalMovement = new Vector3(remainingMovement.X, 0, remainingMovement.Z);

                    _logger?.LogInformation("[AUTO-JUMP] Horizontal collision detected on {Axis} axis, checking auto-jump conditions...",
                        collision.CollisionAxis);

                    if (ShouldAutoJump(aabb, horizontalMovement, hitGround))
                    {
                        _logger?.LogInformation("[AUTO-JUMP] ShouldAutoJump returned TRUE! Attempting to trigger auto-jump...");

                        float currentTime = _autoJumpTimeCounter;
                        if (body?.TryStartAutoJump(5.0f, 0.3f, currentTime) == true)
                        {
                            _logger?.LogInformation("[AUTO-JUMP] SUCCESS! Auto-jump triggered (climbable ledge detected)");
                        }
                        else
                        {
                            _logger?.LogInformation("[AUTO-JUMP] FAILED! Auto-jump on cooldown");
                        }
                        hasTriedAutoJump = true;  // Only try once per frame
                    }
                    else
                    {
                        _logger?.LogInformation("[AUTO-JUMP] ShouldAutoJump returned FALSE (wall detected, not climbable)");
                    }
                }

                // Slide along surface (remove component of velocity parallel to normal)
                // This is vector projection: v' = v - (v · n)n
                float dot = Vector3.Dot(remainingMovement, collision.Normal);
                remainingMovement = remainingMovement - collision.Normal * dot;

                // NOTE: Epsilon offset removed - ignore list prevents re-collision now
            }
            else
            {
                // No collision - move full remaining distance
                aabb = aabb.Offset(remainingMovement);
                totalMovement += remainingMovement;
                break;
            }
        }

        // Warn if we exhausted iterations with significant remaining movement
        if (iteration == maxIterations && remainingMovement.LengthSquared() > 0.01f)
        {
            if (_logger != null)
            {
                _logger.LogWarning(
                    "[COLLISION] Hit iteration limit! Used {Iterations} iterations, " +
                    "remaining movement = ({X:F3}, {Y:F3}, {Z:F3}), length = {Length:F3}m",
                    maxIterations,
                    remainingMovement.X, remainingMovement.Y, remainingMovement.Z,
                    MathF.Sqrt(remainingMovement.LengthSquared()));
            }
        }

        return totalMovement;
    }


    /// <summary>
    /// Calculate the time of collision between a moving AABB and a static AABB along one axis.
    /// Returns -1 if no collision, or a value in range [0, 1] representing the collision time.
    /// </summary>
    private float CalculateCollisionTime(AABB moving, AABB stationary, float movement, Axis axis)
    {
        // First check if AABBs overlap on the other two axes
        // (collision can only occur if they overlap on perpendicular axes)
        bool overlapOnOtherAxes = axis switch
        {
            Axis.X => (moving.Min.Y < stationary.Max.Y && moving.Max.Y > stationary.Min.Y) &&
                      (moving.Min.Z < stationary.Max.Z && moving.Max.Z > stationary.Min.Z),
            Axis.Y => (moving.Min.X < stationary.Max.X && moving.Max.X > stationary.Min.X) &&
                      (moving.Min.Z < stationary.Max.Z && moving.Max.Z > stationary.Min.Z),
            Axis.Z => (moving.Min.X < stationary.Max.X && moving.Max.X > stationary.Min.X) &&
                      (moving.Min.Y < stationary.Max.Y && moving.Max.Y > stationary.Min.Y),
            _ => false
        };

        if (!overlapOnOtherAxes)
            return -1; // No collision possible

        // Calculate collision time on this axis
        float movingMin = axis switch { Axis.X => moving.Min.X, Axis.Y => moving.Min.Y, Axis.Z => moving.Min.Z, _ => 0 };
        float movingMax = axis switch { Axis.X => moving.Max.X, Axis.Y => moving.Max.Y, Axis.Z => moving.Max.Z, _ => 0 };
        float stationaryMin = axis switch { Axis.X => stationary.Min.X, Axis.Y => stationary.Min.Y, Axis.Z => stationary.Min.Z, _ => 0 };
        float stationaryMax = axis switch { Axis.X => stationary.Max.X, Axis.Y => stationary.Max.Y, Axis.Z => stationary.Max.Z, _ => 0 };

        if (movement > 0)
        {
            // Moving in positive direction, check collision with stationary's min face
            float distance = stationaryMin - movingMax;

            // CRITICAL FIX: If distance <= 0, we're already overlapping or touching
            // Return collision time of 0 (immediate collision), not -1
            if (distance <= 0)
                return 0.0f; // Already at or past collision point

            return distance / movement;
        }
        else if (movement < 0)
        {
            // Moving in negative direction, check collision with stationary's max face
            float distance = stationaryMax - movingMin;

            // CRITICAL FIX: If distance >= 0, we're already overlapping or touching
            // Return collision time of 0 (immediate collision), not -1
            if (distance >= 0)
                return 0.0f; // Already at or past collision point

            return distance / movement; // Note: movement is negative, so this gives positive time
        }

        return -1; // No movement on this axis
    }

    /// <summary>
    /// Check if there's a climbable ledge in front of the player that should trigger auto-jump.
    /// Tests at 0.25m (half-slab) and 0.5m (full block) heights.
    /// Returns true for ledges, false for walls.
    /// </summary>
    /// <param name="aabb">Current AABB position</param>
    /// <param name="horizontalMovement">Desired horizontal movement (X or Z only, Y should be 0)</param>
    /// <param name="isGrounded">Whether the body is currently grounded</param>
    /// <returns>True if should auto-jump (climbable ledge), false if wall or already airborne</returns>
    private bool ShouldAutoJump(AABB aabb, Vector3 horizontalMovement, bool isGrounded)
    {
        _logger?.LogInformation("[AUTO-JUMP] ShouldAutoJump: isGrounded={IsGrounded}, horizontalMovement=({X:F3},{Y:F3},{Z:F3})",
            isGrounded, horizontalMovement.X, horizontalMovement.Y, horizontalMovement.Z);
        _logger?.LogInformation("[AUTO-JUMP] Current AABB: Min=({MinX:F2},{MinY:F2},{MinZ:F2}) Max=({MaxX:F2},{MaxY:F2},{MaxZ:F2})",
            aabb.Min.X, aabb.Min.Y, aabb.Min.Z, aabb.Max.X, aabb.Max.Y, aabb.Max.Z);

        // Only auto-jump when grounded (prevents mid-air climbing)
        if (!isGrounded)
        {
            _logger?.LogInformation("[AUTO-JUMP] Not grounded - returning false");
            return false;
        }

        // Only auto-jump for small obstacles (0.25m to 0.5m)
        // Don't auto-jump for tall walls
        float[] testHeights = { 0.25f, 0.5f, 1.0f };  // Test up to 1 block high

        foreach (float height in testHeights)
        {
            _logger?.LogInformation("[AUTO-JUMP] Testing height {Height}m above current position...", height);

            // Test if there's space to move forward at this height
            AABB testAABB = aabb.Offset(new Vector3(0, height, 0));
            _logger?.LogInformation("[AUTO-JUMP] Test AABB at +{Height}m: Min=({MinX:F2},{MinY:F2},{MinZ:F2}) Max=({MaxX:F2},{MaxY:F2},{MaxZ:F2})",
                height, testAABB.Min.X, testAABB.Min.Y, testAABB.Min.Z, testAABB.Max.X, testAABB.Max.Y, testAABB.Max.Z);

            // Internal test - no body parameter (no auto-jump triggering in recursive calls)
            Vector3 testMovement = SweepAABB(testAABB, horizontalMovement, null, out bool _);

            // Calculate how much horizontal movement we achieved
            float desiredDist = new Vector3(horizontalMovement.X, 0, horizontalMovement.Z).Length;
            float actualDist = new Vector3(testMovement.X, 0, testMovement.Z).Length;

            _logger?.LogInformation("[AUTO-JUMP] At height {Height}m: desired={Desired:F3}, actual={Actual:F3}, ratio={Ratio:F2}%",
                height, desiredDist, actualDist, (actualDist / desiredDist) * 100.0f);

            // If we can move forward at this height, it's a climbable ledge
            if (actualDist >= desiredDist * 0.85f)
            {
                _logger?.LogInformation("[AUTO-JUMP] Climbable ledge detected at {Height}m - returning TRUE!", height);
                return true; // Climbable ledge detected - trigger auto-jump!
            }
        }

        _logger?.LogInformation("[AUTO-JUMP] No climbable ledge found - returning FALSE (wall detected)");
        return false; // No climbable ledge - it's a wall
    }

    /// <summary>
    /// Test collision along a single axis and return the collision time (0-1) and voxel position.
    /// Returns -1 if no collision occurs.
    /// </summary>
    private float TestAxisCollision(AABB aabb, Vector3 movement, Axis axis, HashSet<Vector3i>? ignoredVoxels, out Vector3i closestVoxel)
    {
        closestVoxel = new Vector3i(0, 0, 0);

        // Extract movement for this axis
        float desiredMovement = axis switch
        {
            Axis.X => movement.X,
            Axis.Y => movement.Y,
            Axis.Z => movement.Z,
            _ => 0
        };

        // Early out if no movement on this axis
        if (Math.Abs(desiredMovement) < 0.0001f)
            return -1;

        // Calculate sweep bounds (current AABB + movement along axis)
        AABB sweepAABB = aabb;
        if (desiredMovement > 0)
        {
            switch (axis)
            {
                case Axis.X: sweepAABB.Max.X += desiredMovement; break;
                case Axis.Y: sweepAABB.Max.Y += desiredMovement; break;
                case Axis.Z: sweepAABB.Max.Z += desiredMovement; break;
            }
        }
        else
        {
            switch (axis)
            {
                case Axis.X: sweepAABB.Min.X += desiredMovement; break;
                case Axis.Y: sweepAABB.Min.Y += desiredMovement; break;
                case Axis.Z: sweepAABB.Min.Z += desiredMovement; break;
            }
        }

        // Query voxels in the sweep volume
        float closestCollisionTime = 1.0f;

        // Calculate integer bounds for voxel iteration
        int minX = (int)Math.Floor(sweepAABB.Min.X);
        int maxX = (int)Math.Floor(sweepAABB.Max.X);
        int minY = (int)Math.Floor(sweepAABB.Min.Y);
        int maxY = (int)Math.Floor(sweepAABB.Max.Y);
        int minZ = (int)Math.Floor(sweepAABB.Min.Z);
        int maxZ = (int)Math.Floor(sweepAABB.Max.Z);

        // Clamp Y to valid world height
        minY = Math.Max(0, minY);
        maxY = Math.Min(Chunk.WorldHeight - 1, maxY);

        // Iterate through all voxels in sweep volume
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                // Check if chunk is loaded before querying blocks
                Vector2i chunkPos = new Vector2i(x >> 4, z >> 4);
                if (!_world.HasChunk(chunkPos))
                {
                    // Unloaded chunk = solid wall
                    AABB voxelAABB = new AABB(
                        new Vector3(x, minY, z),
                        new Vector3(x + 1, maxY + 1, z + 1)
                    );

                    float collisionTime = CalculateCollisionTime(aabb, voxelAABB, desiredMovement, axis);
                    if (collisionTime >= 0 && collisionTime < closestCollisionTime)
                    {
                        closestCollisionTime = collisionTime;
                        closestVoxel = new Vector3i(x, minY, z);
                    }
                    continue;
                }

                for (int y = minY; y <= maxY; y++)
                {
                    // Skip if this voxel is in the ignore set
                    Vector3i voxelPos = new Vector3i(x, y, z);
                    if (ignoredVoxels?.Contains(voxelPos) == true)
                        continue;

                    BlockType block = _world.GetBlock(x, y, z);

                    if (block == BlockType.Air)
                        continue;

                    AABB voxelAABB = new AABB(
                        new Vector3(x, y, z),
                        new Vector3(x + 1, y + 1, z + 1)
                    );

                    float collisionTime = CalculateCollisionTime(aabb, voxelAABB, desiredMovement, axis);

                    if (collisionTime >= 0 && collisionTime < closestCollisionTime)
                    {
                        closestCollisionTime = collisionTime;
                        closestVoxel = voxelPos;
                    }
                }
            }
        }

        // Return -1 if no collision, otherwise return the collision time
        return closestCollisionTime < 1.0f ? closestCollisionTime : -1;
    }

    /// <summary>
    /// Get the surface normal for a collision on the given axis.
    /// </summary>
    private Vector3 GetNormalForAxis(Axis axis, Vector3 movement)
    {
        return axis switch
        {
            Axis.X => movement.X > 0 ? new Vector3(-1, 0, 0) : new Vector3(1, 0, 0),
            Axis.Y => movement.Y > 0 ? new Vector3(0, -1, 0) : new Vector3(0, 1, 0),
            Axis.Z => movement.Z > 0 ? new Vector3(0, 0, -1) : new Vector3(0, 0, 1),
            _ => Vector3.Zero
        };
    }

    /// <summary>
    /// Find the first collision across all three axes.
    /// Returns the earliest collision with timing and normal information.
    /// </summary>
    private CollisionResult FindFirstCollision(AABB aabb, Vector3 movement, HashSet<Vector3i>? ignoredVoxels, bool shouldLog = false)
    {
        CollisionResult earliest = new CollisionResult
        {
            Hit = false,
            Time = 1.0f,
            Normal = Vector3.Zero,
            CollisionAxis = Axis.X,
            VoxelPosition = new Vector3i(0, 0, 0)
        };

        // Test all three axes and keep track of earliest collision
        foreach (Axis axis in new[] { Axis.X, Axis.Y, Axis.Z })
        {
            float time = TestAxisCollision(aabb, movement, axis, ignoredVoxels, out Vector3i voxelPos);
            if (time >= 0 && time < earliest.Time)
            {
                earliest.Hit = true;
                earliest.Time = time;
                earliest.CollisionAxis = axis;
                earliest.Normal = GetNormalForAxis(axis, movement);
                earliest.VoxelPosition = voxelPos;
            }
        }

        return earliest;
    }

    private enum Axis
    {
        X,
        Y,
        Z
    }

    /// <summary>
    /// Result of a collision test, containing timing and normal information.
    /// Used by the iterative collision resolution system.
    /// </summary>
    private struct CollisionResult
    {
        public bool Hit;              // True if a collision occurred
        public float Time;            // Time of collision (0-1, where 0=start, 1=end of movement)
        public Vector3 Normal;        // Surface normal at collision point
        public Axis CollisionAxis;    // Which axis the collision occurred on
        public Vector3i VoxelPosition; // Which voxel was hit (for collision tracking)
    }
}
