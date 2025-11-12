# Physics Interfaces Reference Code

## Complete Interface Definitions for Task 2B.0

This document provides the exact code for all physics interfaces to be created in `TerraNova.Core` project during Task 2B.0.

---

## 1. IPhysicsWorld.cs

```csharp
using TerraNova.Shared;

namespace TerraNova.Core;

/// <summary>
/// Platform-agnostic interface for physics world simulation.
/// Abstracts physics engine operations from specific libraries (Jitter, Bullet, PhysX, etc.).
/// This interface manages the physics simulation world and all bodies within it.
/// </summary>
/// <remarks>
/// Implementations should encapsulate the physics engine's world/simulation instance
/// and provide thread-safe access if the engine requires it.
/// </remarks>
public interface IPhysicsWorld : IDisposable
{
    /// <summary>
    /// Step the physics simulation forward by the specified time delta.
    /// This advances the simulation state and resolves collisions.
    /// </summary>
    /// <param name="deltaTime">Time step in seconds (typically 1/60 for 60 FPS)</param>
    /// <remarks>
    /// Should be called once per frame in the update loop.
    /// Some engines may use fixed timestep internally regardless of deltaTime.
    /// </remarks>
    void Step(float deltaTime);

    /// <summary>
    /// Create a rigid body with the specified shape and properties.
    /// The body will participate in physics simulation (collision, gravity, forces).
    /// </summary>
    /// <param name="shape">Collision shape for the body (box, capsule, sphere)</param>
    /// <param name="position">Initial world position of the body center</param>
    /// <param name="isStatic">True if body is static (non-moving terrain), false if dynamic (player, items)</param>
    /// <returns>Handle to the created physics body for manipulation</returns>
    /// <remarks>
    /// Static bodies have infinite mass and do not respond to forces or gravity.
    /// Dynamic bodies simulate physics (gravity, collisions, forces).
    /// Caller is responsible for removing the body via RemoveBody() when done.
    /// </remarks>
    IPhysicsBody CreateBody(IPhysicsShape shape, Vector3 position, bool isStatic = false);

    /// <summary>
    /// Remove a physics body from the simulation and free its resources.
    /// The body will no longer participate in simulation or collision detection.
    /// </summary>
    /// <param name="body">Body to remove (must have been created by this world)</param>
    /// <remarks>
    /// After removal, the body handle should not be used.
    /// Implementations may dispose or pool the underlying physics body.
    /// </remarks>
    void RemoveBody(IPhysicsBody body);

    /// <summary>
    /// Set the global gravity vector for the physics world.
    /// Affects all bodies that have IsGravityEnabled = true.
    /// </summary>
    /// <param name="gravity">Gravity acceleration vector in m/s² (typically (0, -32, 0) for Minecraft-like gravity)</param>
    /// <remarks>
    /// Common gravity values:
    /// - Minecraft: (0, -32, 0) blocks/s²
    /// - Earth: (0, -9.81, 0) m/s²
    /// - Moon: (0, -1.62, 0) m/s²
    /// Gravity affects only dynamic bodies with IsGravityEnabled = true.
    /// </remarks>
    void SetGravity(Vector3 gravity);

    /// <summary>
    /// Perform a raycast from origin in direction for specified distance.
    /// Used for ground detection, line-of-sight checks, etc.
    /// </summary>
    /// <param name="origin">Start position of ray in world space</param>
    /// <param name="direction">Direction vector (should be normalized for accurate distance)</param>
    /// <param name="maxDistance">Maximum ray distance in world units</param>
    /// <param name="hitInfo">Output hit information if ray intersects a body</param>
    /// <returns>True if ray hit a body, false if ray missed all bodies</returns>
    /// <remarks>
    /// Direction should be normalized for accurate distance results.
    /// Returns first hit along ray (implementations may sort by distance).
    /// Used for ground detection (raycast downward from player capsule).
    /// </remarks>
    bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out PhysicsHitInfo hitInfo);
}
```

---

## 2. IPhysicsBody.cs

```csharp
using TerraNova.Shared;

namespace TerraNova.Core;

/// <summary>
/// Platform-agnostic interface for a physics rigid body.
/// Represents an object that participates in physics simulation
/// (collision detection, gravity, forces, etc.).
/// </summary>
/// <remarks>
/// Bodies can be static (terrain, walls) or dynamic (player, items).
/// Static bodies do not move and have infinite mass.
/// Dynamic bodies respond to forces, gravity, and collisions.
/// </remarks>
public interface IPhysicsBody
{
    /// <summary>
    /// Get or set the world-space position of the body's center of mass.
    /// Setting position teleports the body (no collision resolution during move).
    /// </summary>
    /// <remarks>
    /// For player synchronization: camera.Position = playerBody.Position + eyeOffset
    /// Setting position directly bypasses physics simulation (use for initialization or teleportation).
    /// </remarks>
    Vector3 Position { get; set; }

    /// <summary>
    /// Get or set the linear velocity of the body in m/s.
    /// Velocity changes automatically from forces, gravity, and collisions.
    /// </summary>
    /// <remarks>
    /// Setting velocity directly overrides current velocity (use sparingly).
    /// Velocity is affected by forces (ApplyForce), impulses (ApplyImpulse), gravity, and collisions.
    /// </remarks>
    Vector3 Velocity { get; set; }

    /// <summary>
    /// Get or set whether the body is affected by gravity.
    /// Only applies to dynamic bodies (static bodies ignore this).
    /// </summary>
    /// <remarks>
    /// Set to true for player/items (normal gravity).
    /// Set to false for flying entities or testing.
    /// Gravity vector is set globally via IPhysicsWorld.SetGravity().
    /// </remarks>
    bool IsGravityEnabled { get; set; }

    /// <summary>
    /// Get or set the body mass in kilograms.
    /// Static bodies typically have infinite mass (mass setting ignored).
    /// </summary>
    /// <remarks>
    /// Higher mass = more inertia (harder to accelerate/decelerate).
    /// Affects force response: acceleration = force / mass.
    /// Typical player mass: 70-100 kg.
    /// </remarks>
    float Mass { get; set; }

    /// <summary>
    /// Get whether this body is currently touching the ground.
    /// Used for jump validation (prevent air jumping).
    /// </summary>
    /// <remarks>
    /// Implementation may use collision contacts or downward raycast.
    /// Returns true if body is resting on static geometry (terrain, blocks).
    /// Use for jump logic: if (IsGrounded &amp;&amp; jumpKeyPressed) ApplyImpulse(jumpForce);
    /// </remarks>
    bool IsGrounded { get; }

    /// <summary>
    /// Apply an instantaneous impulse force to the body (immediate velocity change).
    /// Used for jumping, explosions, knockback.
    /// </summary>
    /// <param name="impulse">Impulse vector in kg⋅m/s (mass × velocity change)</param>
    /// <remarks>
    /// Impulse immediately changes velocity: newVelocity = oldVelocity + (impulse / mass).
    /// Use for discrete events: jumping (upward impulse), explosions, collisions.
    /// For continuous forces (WASD movement), use ApplyForce() instead.
    /// Example: Jump impulse = (0, 8, 0) for ~1.25 block jump height with gravity -32.
    /// </remarks>
    void ApplyImpulse(Vector3 impulse);

    /// <summary>
    /// Apply a continuous force to the body over time (gradual acceleration).
    /// Used for WASD movement, propulsion, wind.
    /// </summary>
    /// <param name="force">Force vector in Newtons (kg⋅m/s²)</param>
    /// <remarks>
    /// Force causes acceleration: acceleration = force / mass.
    /// Velocity changes gradually over multiple frames: velocity += (force / mass) * deltaTime.
    /// Use for continuous input: WASD movement forces applied each frame.
    /// Example: Movement force = direction * speed * mass for consistent acceleration.
    /// </remarks>
    void ApplyForce(Vector3 force);

    /// <summary>
    /// Tag object for user data association (e.g., "Player", "Terrain", "Item").
    /// Physics engine ignores this - purely for game logic use.
    /// </summary>
    /// <remarks>
    /// Useful for collision filtering or identification in callbacks.
    /// Example: Tag player body with "Player" to identify in collision events.
    /// Can store any object (string, enum, custom data structure).
    /// </remarks>
    object? Tag { get; set; }
}
```

---

## 3. IPhysicsShape.cs

```csharp
namespace TerraNova.Core;

/// <summary>
/// Platform-agnostic interface for physics collision shapes.
/// Abstracts shape geometry from specific physics engine representations.
/// </summary>
/// <remarks>
/// Shapes define the collision geometry of physics bodies.
/// Create shapes via IPhysicsShapeFactory, not directly.
/// Shapes can be reused for multiple bodies (create once, use many times).
/// </remarks>
public interface IPhysicsShape
{
    /// <summary>
    /// Shape type identifier (Box, Capsule, Sphere).
    /// Useful for debugging or collision filtering.
    /// </summary>
    PhysicsShapeType ShapeType { get; }
}

/// <summary>
/// Types of physics shapes supported by the abstraction layer.
/// </summary>
/// <remarks>
/// Box: Rectangular cuboid (terrain blocks, crates) - 3 dimensions (width, height, depth)
/// Capsule: Cylinder with hemisphere caps (player character) - 2 dimensions (radius, height)
/// Sphere: Perfect sphere (projectiles, items) - 1 dimension (radius)
/// </remarks>
public enum PhysicsShapeType
{
    /// <summary>
    /// Box shape (rectangular cuboid) for terrain blocks, crates, walls.
    /// Created with width, height, depth dimensions.
    /// Exact collision (no rounding) - good for block-aligned geometry.
    /// </summary>
    Box,

    /// <summary>
    /// Capsule shape (cylinder with hemisphere caps) for characters.
    /// Created with radius and height (cylinder portion, excluding caps).
    /// Smooth collision (rounded ends) - prevents getting stuck on edges.
    /// Standard for FPS player characters.
    /// </summary>
    Capsule,

    /// <summary>
    /// Sphere shape (perfect sphere) for projectiles, items, rolling objects.
    /// Created with radius.
    /// Smoothest collision (no edges) - rolls naturally.
    /// </summary>
    Sphere,
}
```

---

## 4. IPhysicsShapeFactory.cs

```csharp
namespace TerraNova.Core;

/// <summary>
/// Factory interface for creating physics shapes.
/// Allows physics engine to create appropriate shape implementations
/// without exposing engine-specific types.
/// </summary>
/// <remarks>
/// Inject this factory into classes that create physics bodies.
/// Factory pattern isolates shape creation from physics engine specifics.
/// Each physics engine (Jitter, Bullet, Bepu) provides its own factory implementation.
/// Shapes can be cached and reused (create once, use for multiple bodies).
/// </remarks>
public interface IPhysicsShapeFactory
{
    /// <summary>
    /// Create a box (rectangular cuboid) shape with specified dimensions.
    /// Used for terrain block collisions (Task 2B.3).
    /// </summary>
    /// <param name="width">Box width in meters (X dimension)</param>
    /// <param name="height">Box height in meters (Y dimension)</param>
    /// <param name="depth">Box depth in meters (Z dimension)</param>
    /// <returns>Box collision shape implementing IPhysicsShape</returns>
    /// <remarks>
    /// Dimensions represent full size, not half-extents (some engines use half-extents internally).
    /// Example: CreateBox(1, 1, 1) creates a 1×1×1 meter cube for voxel blocks.
    /// Box shape provides exact collision (no edge rounding).
    /// </remarks>
    IPhysicsShape CreateBox(float width, float height, float depth);

    /// <summary>
    /// Create a capsule (cylinder with hemisphere caps) shape.
    /// Used for player character (Task 2B.2) - standard FPS collision shape.
    /// </summary>
    /// <param name="radius">Capsule radius in meters (cylinder and hemisphere radius)</param>
    /// <param name="height">Capsule height in meters (cylinder portion only, excluding hemisphere caps)</param>
    /// <returns>Capsule collision shape implementing IPhysicsShape</returns>
    /// <remarks>
    /// Total capsule height = height + 2 * radius (includes both hemisphere caps).
    /// Example: CreateCapsule(0.3, 1.6) creates a ~1.8m tall player capsule (typical human height).
    /// Capsule shape prevents getting stuck on edges (smooth hemisphere bottoms).
    /// Oriented along Y axis (vertical) by default.
    /// </remarks>
    IPhysicsShape CreateCapsule(float radius, float height);

    /// <summary>
    /// Create a sphere shape with specified radius.
    /// Future use for items, projectiles, rolling objects.
    /// </summary>
    /// <param name="radius">Sphere radius in meters</param>
    /// <returns>Sphere collision shape implementing IPhysicsShape</returns>
    /// <remarks>
    /// Example: CreateSphere(0.2) creates a 0.4m diameter sphere for small items.
    /// Sphere shape provides smoothest collision (no edges, rolls naturally).
    /// Uniform collision from all directions.
    /// </remarks>
    IPhysicsShape CreateSphere(float radius);
}
```

---

## 5. PhysicsHitInfo.cs

```csharp
using TerraNova.Shared;

namespace TerraNova.Core;

/// <summary>
/// Information about a physics raycast hit.
/// Returned by IPhysicsWorld.Raycast() when ray intersects a body.
/// </summary>
/// <remarks>
/// Use for ground detection, line-of-sight checks, interaction raycasts.
/// Immutable struct for value semantics (copy by value, no reference issues).
/// </remarks>
public struct PhysicsHitInfo
{
    /// <summary>
    /// The body that was hit by the raycast.
    /// Can query body properties (Position, Tag) to identify what was hit.
    /// </summary>
    /// <remarks>
    /// Use body.Tag to identify body type (e.g., "Terrain", "Player", "Item").
    /// Body reference valid until removed from physics world.
    /// </remarks>
    public IPhysicsBody Body { get; init; }

    /// <summary>
    /// World-space position where the ray hit the body surface.
    /// Exact intersection point between ray and collision shape.
    /// </summary>
    /// <remarks>
    /// Useful for visual feedback (spawn particles at hit point).
    /// Distance from ray origin to hit point = hitInfo.Distance.
    /// </remarks>
    public Vector3 HitPoint { get; init; }

    /// <summary>
    /// Surface normal vector at the hit point (perpendicular to surface).
    /// Always unit length (magnitude = 1), points away from body.
    /// </summary>
    /// <remarks>
    /// Useful for reflection calculations (bouncing projectiles).
    /// For ground detection: normal should point upward (0, 1, 0) for flat ground.
    /// Indicates surface orientation at hit point.
    /// </remarks>
    public Vector3 Normal { get; init; }

    /// <summary>
    /// Distance from ray origin to hit point in world units (meters).
    /// Always positive and less than or equal to raycast maxDistance parameter.
    /// </summary>
    /// <remarks>
    /// Useful for determining closest hit in multiple raycasts.
    /// For ground detection: small distance (e.g., &lt; 0.1m) indicates player is on ground.
    /// Distance = Vector3.Distance(rayOrigin, hitPoint).
    /// </remarks>
    public float Distance { get; init; }
}
```

---

## Usage Examples

### Example 1: Creating Player Capsule (Task 2B.2)

```csharp
public class PlayerController
{
    private readonly IPhysicsWorld _physicsWorld;
    private readonly IPhysicsShapeFactory _shapeFactory;
    private IPhysicsBody _playerBody;

    public PlayerController(
        IPhysicsWorld physicsWorld,
        IPhysicsShapeFactory shapeFactory,
        Camera camera)
    {
        _physicsWorld = physicsWorld;
        _shapeFactory = shapeFactory;

        // Create player capsule shape (0.3m radius, 1.6m tall)
        var capsuleShape = _shapeFactory.CreateCapsule(radius: 0.3f, height: 1.6f);

        // Create dynamic physics body at camera position
        _playerBody = _physicsWorld.CreateBody(capsuleShape, camera.Position, isStatic: false);
        _playerBody.IsGravityEnabled = true;
        _playerBody.Mass = 80f;  // 80 kg player
        _playerBody.Tag = "Player";
    }

    public void Update(float deltaTime)
    {
        // Step physics simulation
        _physicsWorld.Step(deltaTime);

        // Synchronize camera with physics body (add eye offset)
        camera.Position = _playerBody.Position + new Vector3(0, 1.5f, 0);
    }
}
```

### Example 2: WASD Movement (Task 2B.2)

```csharp
public void HandleMovementInput(KeyboardState keyboard, float deltaTime)
{
    var moveDirection = Vector3.Zero;

    if (keyboard.IsKeyDown(Keys.W)) moveDirection.Z -= 1;
    if (keyboard.IsKeyDown(Keys.S)) moveDirection.Z += 1;
    if (keyboard.IsKeyDown(Keys.A)) moveDirection.X -= 1;
    if (keyboard.IsKeyDown(Keys.D)) moveDirection.X += 1;

    if (moveDirection != Vector3.Zero)
    {
        moveDirection = Vector3.Normalize(moveDirection);

        // Apply force for movement (not direct position change)
        float moveSpeed = 500f;  // Force magnitude
        var moveForce = moveDirection * moveSpeed * _playerBody.Mass;
        _playerBody.ApplyForce(moveForce);
    }
}
```

### Example 3: Terrain Collision (Task 2B.3)

```csharp
public class TerrainCollisionManager
{
    private readonly IPhysicsWorld _physicsWorld;
    private readonly IPhysicsShapeFactory _shapeFactory;
    private readonly Dictionary<Vector3i, IPhysicsBody> _blockBodies = new();

    public void AddBlockCollision(Vector3i blockPos)
    {
        // Create 1x1x1 box shape for block
        var boxShape = _shapeFactory.CreateBox(width: 1f, height: 1f, depth: 1f);

        // Create static body (non-moving terrain)
        var blockBody = _physicsWorld.CreateBody(
            boxShape,
            new Vector3(blockPos.X, blockPos.Y, blockPos.Z),
            isStatic: true
        );
        blockBody.Tag = "Terrain";

        _blockBodies[blockPos] = blockBody;
    }

    public void RemoveBlockCollision(Vector3i blockPos)
    {
        if (_blockBodies.TryGetValue(blockPos, out var body))
        {
            _physicsWorld.RemoveBody(body);
            _blockBodies.Remove(blockPos);
        }
    }
}
```

### Example 4: Jumping with Ground Detection (Task 2B.4)

```csharp
public void HandleJump(KeyboardState keyboard)
{
    // Configure gravity (called once at initialization)
    _physicsWorld.SetGravity(new Vector3(0, -32f, 0));  // Minecraft-like gravity

    // Ground detection via raycast
    bool isGrounded = _physicsWorld.Raycast(
        origin: _playerBody.Position,
        direction: new Vector3(0, -1, 0),  // Downward
        maxDistance: 0.1f,  // Slightly below capsule bottom
        out PhysicsHitInfo hitInfo
    );

    // Jump if grounded and spacebar pressed
    if (isGrounded && keyboard.IsKeyDown(Keys.Space))
    {
        // Apply upward impulse for jump
        var jumpImpulse = new Vector3(0, 8f, 0);  // ~1.25 block jump height
        _playerBody.ApplyImpulse(jumpImpulse);
    }
}
```

---

## Implementation Checklist for Task 2B.0

- [ ] Create `TerraNova.Core/IPhysicsWorld.cs` with all 5 methods
- [ ] Create `TerraNova.Core/IPhysicsBody.cs` with all properties/methods
- [ ] Create `TerraNova.Core/IPhysicsShape.cs` with enum and property
- [ ] Create `TerraNova.Core/IPhysicsShapeFactory.cs` with all 3 factory methods
- [ ] Create `TerraNova.Core/PhysicsHitInfo.cs` with all 4 fields
- [ ] Add comprehensive XML documentation to all interfaces (as shown above)
- [ ] Ensure all interfaces use `TerraNova.Shared.Vector3` (not OpenTK types)
- [ ] Verify Core project builds with 0 warnings, 0 errors
- [ ] Verify NO Jitter2 or physics engine references in Core project
- [ ] Review interfaces support all Stage 2B task requirements:
  - [ ] Task 2B.2: CreateCapsule(), CreateBody(), ApplyForce()
  - [ ] Task 2B.3: CreateBox(), CreateBody(isStatic: true), RemoveBody()
  - [ ] Task 2B.4: SetGravity(), Raycast(), ApplyImpulse(), IsGrounded

---

## Next Step After Task 2B.0

Once all interfaces are created and Core project builds successfully:

1. Proceed to **Task 2B.1**: Install Jitter2 NuGet (Client project only)
2. Create adapter classes implementing these interfaces
3. Inject `IPhysicsWorld` and `IPhysicsShapeFactory` into PlayerController
4. NEVER import Jitter2 namespaces in PlayerController - use only Core interfaces

**Architecture Goal**: PlayerController should compile successfully even if Jitter2 is removed and replaced with different physics engine (e.g., Bullet, Bepu) by swapping adapter implementations.
