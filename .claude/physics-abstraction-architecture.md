# Physics Abstraction Architecture Analysis

## Executive Summary

This document provides architectural guidance for implementing Jitter Physics 2 in Terra Nova with proper abstraction layers, following the same pattern established for OpenTK rendering abstraction (IRenderer, ICameraView).

## Current State Analysis

### Completed Abstractions (Reference Patterns)
- **IRenderer**: Platform-agnostic rendering interface (TerraNova.Core)
- **ICameraView**: Read-only camera state without OpenTK coupling (TerraNova.Core)
- **OpenTKRenderer**: Concrete implementation in Client project
- **Pattern**: Interfaces in Core, concrete implementations in Client

### Physics Requirements (Stage 2B)
Per ROADMAP.md Stage 2B tasks:
1. Integrate Jitter Physics 2 library
2. Implement player physics body (capsule)
3. Add terrain collision detection
4. Implement gravity and jumping

## Architectural Recommendation: Physics Abstraction Strategy

### Design Philosophy
**"Hide the physics engine behind abstractions so we can swap libraries in the future"**

Following the IRenderer pattern, we need:
- Physics interfaces in **TerraNova.Core** (platform-agnostic)
- Jitter2-specific implementation in **TerraNova.Client** (platform-specific)
- Clean separation between physics API and game logic

### Recommended Interface Hierarchy

#### 1. IPhysicsWorld (Core Project)
**Location**: `TerraNova.Core/IPhysicsWorld.cs`

**Purpose**: Manages the physics simulation world

```csharp
namespace TerraNova.Core;

/// <summary>
/// Platform-agnostic interface for physics world simulation.
/// Abstracts physics engine operations from specific libraries (Jitter, Bullet, PhysX, etc.).
/// </summary>
public interface IPhysicsWorld : IDisposable
{
    /// <summary>
    /// Step the physics simulation forward by the specified time delta.
    /// </summary>
    /// <param name="deltaTime">Time step in seconds</param>
    void Step(float deltaTime);

    /// <summary>
    /// Create a rigid body with the specified shape and properties.
    /// </summary>
    /// <param name="shape">Collision shape for the body</param>
    /// <param name="position">Initial world position</param>
    /// <param name="isStatic">True if body is static (non-moving), false if dynamic</param>
    /// <returns>Handle to the created physics body</returns>
    IPhysicsBody CreateBody(IPhysicsShape shape, Vector3 position, bool isStatic = false);

    /// <summary>
    /// Remove a physics body from the simulation.
    /// </summary>
    /// <param name="body">Body to remove</param>
    void RemoveBody(IPhysicsBody body);

    /// <summary>
    /// Set gravity vector for the physics world.
    /// </summary>
    /// <param name="gravity">Gravity acceleration vector (typically negative Y)</param>
    void SetGravity(Vector3 gravity);

    /// <summary>
    /// Perform a raycast from origin in direction for specified distance.
    /// </summary>
    /// <param name="origin">Start position of ray</param>
    /// <param name="direction">Direction vector (should be normalized)</param>
    /// <param name="maxDistance">Maximum ray distance</param>
    /// <param name="hitInfo">Output hit information if ray hits a body</param>
    /// <returns>True if ray hit a body, false otherwise</returns>
    bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out PhysicsHitInfo hitInfo);
}
```

**Rationale**:
- `Step()`: Core physics update loop
- `CreateBody()/RemoveBody()`: Body lifecycle management (terrain, player)
- `SetGravity()`: Configure world parameters
- `Raycast()`: Ground detection for jumping (Task 2B.4 requirement)

---

#### 2. IPhysicsBody (Core Project)
**Location**: `TerraNova.Core/IPhysicsBody.cs`

**Purpose**: Represents a rigid body in the physics simulation

```csharp
namespace TerraNova.Core;

/// <summary>
/// Platform-agnostic interface for a physics rigid body.
/// Represents an object that participates in physics simulation.
/// </summary>
public interface IPhysicsBody
{
    /// <summary>
    /// Get or set the world-space position of the body.
    /// </summary>
    Vector3 Position { get; set; }

    /// <summary>
    /// Get or set the linear velocity of the body.
    /// </summary>
    Vector3 Velocity { get; set; }

    /// <summary>
    /// Get or set whether the body is affected by gravity.
    /// </summary>
    bool IsGravityEnabled { get; set; }

    /// <summary>
    /// Apply an impulse force to the body (instantaneous velocity change).
    /// Used for jumping (Task 2B.4).
    /// </summary>
    /// <param name="impulse">Impulse vector to apply</param>
    void ApplyImpulse(Vector3 impulse);

    /// <summary>
    /// Apply a continuous force to the body.
    /// Used for WASD movement (Task 2B.2).
    /// </summary>
    /// <param name="force">Force vector to apply</param>
    void ApplyForce(Vector3 force);

    /// <summary>
    /// Set body mass. Static bodies typically have infinite mass.
    /// </summary>
    float Mass { get; set; }

    /// <summary>
    /// Get whether this body is currently touching the ground.
    /// Used for jump validation (Task 2B.4).
    /// </summary>
    bool IsGrounded { get; }

    /// <summary>
    /// Tag object for user data association (e.g., "Player", "Terrain").
    /// </summary>
    object? Tag { get; set; }
}
```

**Rationale**:
- `Position/Velocity`: Essential state synchronization with camera/renderer
- `ApplyImpulse/ApplyForce`: Movement mechanics (Task 2B.2, 2B.4)
- `IsGravityEnabled`: Control gravity on player body
- `IsGrounded`: Jump validation (Task 2B.4 requirement)
- `Tag`: Identify body type in collision callbacks

---

#### 3. IPhysicsShape (Core Project)
**Location**: `TerraNova.Core/IPhysicsShape.cs`

**Purpose**: Defines collision shape geometry

```csharp
namespace TerraNova.Core;

/// <summary>
/// Platform-agnostic interface for physics collision shapes.
/// Abstracts shape geometry from specific physics engine representations.
/// </summary>
public interface IPhysicsShape
{
    /// <summary>
    /// Shape type identifier.
    /// </summary>
    PhysicsShapeType ShapeType { get; }
}

/// <summary>
/// Types of physics shapes supported.
/// </summary>
public enum PhysicsShapeType
{
    Box,      // For terrain blocks (Task 2B.3)
    Capsule,  // For player character (Task 2B.2)
    Sphere,   // Future use (items, projectiles)
}
```

**Rationale**:
- `Box`: Terrain block collisions (Task 2B.3 requirement)
- `Capsule`: Player physics body (Task 2B.2 requirement)
- `Sphere`: Future-proofing for items/projectiles
- Simple enum-based design (no complex mesh colliders needed for voxel game)

---

#### 4. IPhysicsShapeFactory (Core Project)
**Location**: `TerraNova.Core/IPhysicsShapeFactory.cs`

**Purpose**: Factory for creating physics shapes

```csharp
namespace TerraNova.Core;

/// <summary>
/// Factory interface for creating physics shapes.
/// Allows physics engine to create appropriate shape implementations.
/// </summary>
public interface IPhysicsShapeFactory
{
    /// <summary>
    /// Create a box shape with specified dimensions.
    /// Used for terrain block collisions (Task 2B.3).
    /// </summary>
    /// <param name="width">Box width (X dimension)</param>
    /// <param name="height">Box height (Y dimension)</param>
    /// <param name="depth">Box depth (Z dimension)</param>
    /// <returns>Box collision shape</returns>
    IPhysicsShape CreateBox(float width, float height, float depth);

    /// <summary>
    /// Create a capsule shape with specified radius and height.
    /// Used for player character (Task 2B.2).
    /// </summary>
    /// <param name="radius">Capsule radius</param>
    /// <param name="height">Capsule height (cylinder portion, excluding hemisphere caps)</param>
    /// <returns>Capsule collision shape</returns>
    IPhysicsShape CreateCapsule(float radius, float height);

    /// <summary>
    /// Create a sphere shape with specified radius.
    /// Future use for items, projectiles.
    /// </summary>
    /// <param name="radius">Sphere radius</param>
    /// <returns>Sphere collision shape</returns>
    IPhysicsShape CreateSphere(float radius);
}
```

**Rationale**:
- Factory pattern isolates shape creation from physics engine specifics
- Each physics engine (Jitter, Bullet, PhysX) can provide its own shape implementations
- Supports all Stage 2B requirements (Box for terrain, Capsule for player)

---

#### 5. PhysicsHitInfo (Core Project)
**Location**: `TerraNova.Core/PhysicsHitInfo.cs`

**Purpose**: Data structure for raycast results

```csharp
namespace TerraNova.Core;

/// <summary>
/// Information about a physics raycast hit.
/// </summary>
public struct PhysicsHitInfo
{
    /// <summary>
    /// The body that was hit by the raycast.
    /// </summary>
    public IPhysicsBody Body { get; init; }

    /// <summary>
    /// World-space position where the ray hit the body.
    /// </summary>
    public Vector3 HitPoint { get; init; }

    /// <summary>
    /// Surface normal at the hit point.
    /// </summary>
    public Vector3 Normal { get; init; }

    /// <summary>
    /// Distance from ray origin to hit point.
    /// </summary>
    public float Distance { get; init; }
}
```

**Rationale**:
- Standard raycast result structure
- Needed for ground detection (Task 2B.4)
- Immutable struct for value semantics

---

### Implementation Strategy

#### Phase 1: Create Abstraction Layer (Before Jitter Integration)
**Estimated Time**: 45-60 minutes

**Tasks**:
1. Create `TerraNova.Core/IPhysicsWorld.cs` (10 min)
2. Create `TerraNova.Core/IPhysicsBody.cs` (10 min)
3. Create `TerraNova.Core/IPhysicsShape.cs` (5 min)
4. Create `TerraNova.Core/IPhysicsShapeFactory.cs` (10 min)
5. Create `TerraNova.Core/PhysicsHitInfo.cs` (5 min)
6. Build and verify Core project compiles (5 min)
7. Add XML documentation to all interfaces (15 min)

**Success Criteria**:
- All physics interfaces exist in TerraNova.Core
- Core project builds with 0 warnings, 0 errors
- Interfaces follow same pattern as IRenderer/ICameraView
- No references to Jitter2 or any specific physics engine in Core project

---

#### Phase 2: Implement Jitter2 Adapter (Client Project)
**Estimated Time**: 90-120 minutes

**Tasks**:
1. Install Jitter2 NuGet package in TerraNova.Client (5 min)
2. Create `TerraNova.Client/JitterPhysicsWorld.cs` : IPhysicsWorld (30 min)
3. Create `TerraNova.Client/JitterPhysicsBody.cs` : IPhysicsBody (30 min)
4. Create `TerraNova.Client/JitterPhysicsShape.cs` : IPhysicsShape (15 min)
5. Create `TerraNova.Client/JitterShapeFactory.cs` : IPhysicsShapeFactory (20 min)
6. Implement coordinate system conversions (Jitter2 uses different conventions) (15 min)
7. Build and verify Client project compiles (5 min)

**Success Criteria**:
- Jitter2 package installed successfully
- All adapter classes implement Core interfaces correctly
- No Jitter2 types leak into public API (encapsulated in Client)
- Client project builds with 0 warnings, 0 errors

---

#### Phase 3: Integrate into PlayerController (Stage 2B Tasks)
**Estimated Time**: 120-180 minutes (matches original ROADMAP estimates)

**Tasks**:
1. Update PlayerController to use IPhysicsWorld interface (20 min)
2. Create player capsule body via IPhysicsShapeFactory (30 min)
3. Implement force-based WASD movement via IPhysicsBody (40 min)
4. Add terrain collision shapes for nearby blocks (60 min)
5. Implement gravity and jumping with ground detection (30 min)

**Success Criteria**:
- PlayerController depends on IPhysicsWorld (not JitterPhysicsWorld)
- All Stage 2B functionality working (movement, collision, jumping)
- Physics implementation completely hidden behind abstractions

---

### Project Structure

```
TerraNova.Core/
├── IRenderer.cs              (existing - reference pattern)
├── ICameraView.cs            (existing - reference pattern)
├── IPhysicsWorld.cs          (new - main physics interface)
├── IPhysicsBody.cs           (new - rigid body interface)
├── IPhysicsShape.cs          (new - shape interface)
├── IPhysicsShapeFactory.cs   (new - shape creation)
└── PhysicsHitInfo.cs         (new - raycast results)

TerraNova.Client/
├── OpenTKRenderer.cs         (existing - IRenderer implementation)
├── Camera.cs                 (existing - ICameraView implementation)
├── JitterPhysicsWorld.cs     (new - IPhysicsWorld implementation)
├── JitterPhysicsBody.cs      (new - IPhysicsBody implementation)
├── JitterPhysicsShape.cs     (new - IPhysicsShape implementation)
├── JitterShapeFactory.cs     (new - IPhysicsShapeFactory implementation)
└── PlayerController.cs       (existing - will use IPhysicsWorld)
```

**Abstraction Boundaries**:
- **Core Project**: Interfaces only, no implementation, no Jitter2 references
- **Client Project**: Concrete Jitter2 implementations, encapsulated adapters
- **Shared Project**: Common data types (Vector3, Vector2i) used by interfaces

---

### Updated Stage 2B Task Breakdown

#### NEW Task 2B.0: Create Physics Abstraction Layer
**Status**: Ready (MUST DO BEFORE 2B.1)
**Priority**: CRITICAL
**Estimated Time**: 45-60 minutes
**Complexity**: Simple to Moderate

**Description**: Create physics interfaces in Core project to abstract physics engine implementation.

**Implementation Steps**:
1. Create IPhysicsWorld interface with Step(), CreateBody(), RemoveBody(), SetGravity(), Raycast()
2. Create IPhysicsBody interface with Position, Velocity, ApplyForce(), ApplyImpulse(), IsGrounded
3. Create IPhysicsShape interface with ShapeType enum
4. Create IPhysicsShapeFactory interface with CreateBox(), CreateCapsule(), CreateSphere()
5. Create PhysicsHitInfo struct for raycast results
6. Add comprehensive XML documentation to all interfaces
7. Build Core project and verify 0 warnings, 0 errors

**Completion Criteria**:
- All 5 physics interfaces/types exist in TerraNova.Core
- Interfaces follow same pattern as IRenderer/ICameraView (reference existing abstractions)
- No Jitter2 or any physics engine references in Core project
- XML documentation explains abstraction purpose
- Core project builds successfully with 0 warnings, 0 errors

**Files to Create**:
- `TerraNova.Core/IPhysicsWorld.cs` (~60 lines with docs)
- `TerraNova.Core/IPhysicsBody.cs` (~80 lines with docs)
- `TerraNova.Core/IPhysicsShape.cs` (~25 lines with docs)
- `TerraNova.Core/IPhysicsShapeFactory.cs` (~40 lines with docs)
- `TerraNova.Core/PhysicsHitInfo.cs` (~30 lines with docs)

**Rationale**: Creating abstractions BEFORE integrating Jitter2 ensures clean architecture from day 1. Refactoring after Jitter2 is integrated is much harder and riskier.

---

#### UPDATED Task 2B.1: Integrate Jitter Physics 2 Library
**Status**: Blocked (requires Task 2B.0)
**Priority**: HIGH
**Estimated Time**: 60-90 minutes (increased from 30-45 due to adapter implementation)
**Complexity**: Moderate (increased from Simple)

**Description**: Install Jitter2 and create adapter classes implementing Core physics interfaces.

**Implementation Steps**:
1. Add Jitter2 NuGet package to TerraNova.Client project
2. Create JitterPhysicsWorld class implementing IPhysicsWorld
   - Wrap Jitter2.World instance
   - Implement Step() by calling world.Step(deltaTime)
   - Implement CreateBody() by creating Jitter RigidBody and wrapping in JitterPhysicsBody
   - Implement RemoveBody() by removing from Jitter world
   - Implement SetGravity() by setting Jitter world gravity
   - Implement Raycast() by using Jitter ray query and converting results
3. Create JitterPhysicsBody class implementing IPhysicsBody
   - Wrap Jitter2.RigidBody instance
   - Implement Position/Velocity by accessing Jitter body properties
   - Implement ApplyForce/ApplyImpulse by calling Jitter body methods
   - Implement IsGrounded by raycasting downward
4. Create JitterPhysicsShape class implementing IPhysicsShape
   - Wrap Jitter2.Shape instances (Box, Capsule, Sphere)
5. Create JitterShapeFactory class implementing IPhysicsShapeFactory
   - Implement CreateBox/CreateCapsule/CreateSphere using Jitter shape constructors
   - Return JitterPhysicsShape wrappers
6. Add ILogger dependency to JitterPhysicsWorld for diagnostics
7. Add physics world instance to PlayerController constructor (IPhysicsWorld interface type)
8. Initialize physics world in PlayerController constructor
9. Call physicsWorld.Step(deltaTime) in PlayerController.Update()
10. Build and verify functionality

**Completion Criteria**:
- Jitter2 NuGet package installed successfully in Client project
- All 4 adapter classes (JitterPhysicsWorld, JitterPhysicsBody, JitterPhysicsShape, JitterShapeFactory) implemented
- Adapter classes fully implement Core interfaces (no missing methods)
- PlayerController depends on IPhysicsWorld interface (not JitterPhysicsWorld concrete type)
- Physics simulation steps each frame without errors
- Log output confirms physics world is updating
- No Jitter2 types exposed in PlayerController (fully encapsulated)
- Build: 0 warnings, 0 errors

**Files to Create**:
- `TerraNova.Client/JitterPhysicsWorld.cs` (~150 lines)
- `TerraNova.Client/JitterPhysicsBody.cs` (~120 lines)
- `TerraNova.Client/JitterPhysicsShape.cs` (~50 lines)
- `TerraNova.Client/JitterShapeFactory.cs` (~80 lines)

**Files to Modify**:
- `TerraNova.Client/TerraNova.Client.csproj` (add Jitter2 NuGet reference)
- `TerraNova.Client/PlayerController.cs` (add IPhysicsWorld dependency)

**Notes**:
- Jitter2 coordinate system may differ from game coordinate system - handle conversions in adapter
- JitterPhysicsWorld owns Jitter2.World instance lifecycle (dispose properly)
- Keep all Jitter2-specific code in Client project - Core project stays clean
- This is now the foundational task for physics integration (abstractions in place)

---

#### Task 2B.2: Implement Player Physics Body (No changes needed)
**Status**: Blocked (requires Task 2B.1)
**Priority**: HIGH
**Estimated Time**: 45-60 minutes
**Complexity**: Medium

**Description**: Add physics body (capsule) to PlayerController using IPhysicsWorld interface.

**Implementation Steps**:
1. Use IPhysicsShapeFactory.CreateCapsule() to create player shape
2. Use IPhysicsWorld.CreateBody() to create dynamic capsule body
3. Position body at camera position using IPhysicsBody.Position
4. Store IPhysicsBody reference in PlayerController
5. Implement force-based WASD movement using IPhysicsBody.ApplyForce()
6. Synchronize camera position with IPhysicsBody.Position each frame
7. Disable previous direct position manipulation

**Completion Criteria**:
- Player represented by capsule physics body (via IPhysicsBody interface)
- WASD movement works via IPhysicsBody.ApplyForce()
- Camera position follows IPhysicsBody.Position
- Player can move around (floating is OK - no terrain collision yet)
- No direct Jitter2 dependencies in PlayerController (uses interfaces only)
- No crashes or physics exceptions
- Build: 0 warnings, 0 errors

**Files to Modify**:
- `TerraNova.Client/PlayerController.cs` (add player body, force-based movement)

**Notes**:
- PlayerController depends on IPhysicsWorld and IPhysicsBody (not Jitter types)
- All physics operations through interface methods
- Physics engine is completely swappable without changing PlayerController code

---

#### Task 2B.3: Add Terrain Collision Detection (Updated for abstraction)
**Status**: Blocked (requires Task 2B.2)
**Priority**: HIGH
**Estimated Time**: 60-90 minutes
**Complexity**: High

**Description**: Create physics collision shapes for terrain blocks using IPhysicsWorld interface.

**Implementation Steps**:
1. Use IPhysicsShapeFactory.CreateBox() to create block shapes (1x1x1 units)
2. For all non-Air blocks within interaction range (e.g., 16 blocks):
   - Create static physics body using IPhysicsWorld.CreateBody(shape, position, isStatic: true)
   - Store IPhysicsBody reference for cleanup
3. Implement chunk loading/unloading integration:
   - Subscribe to chunk loaded events
   - Add collision shapes for solid blocks in new chunks
   - Remove collision shapes when chunks unload (IPhysicsWorld.RemoveBody())
4. Implement block break/place synchronization:
   - When block broken: Remove collision shape
   - When block placed: Add collision shape
5. Test collision detection with various terrain configurations

**Completion Criteria**:
- Player collides with terrain blocks and cannot walk through them
- Player can walk on top of blocks
- Collision shapes load/unload with chunks properly (no memory leaks)
- Block breaking/placing updates collision shapes synchronously
- No performance issues with collision detection
- Player cannot fall through floor or clip through walls
- No direct Jitter2 dependencies in terrain collision code (uses IPhysicsWorld only)
- Build: 0 warnings, 0 errors

**Files to Modify**:
- `TerraNova.Client/PlayerController.cs` (manage terrain collision shapes via IPhysicsWorld)

**Files to Potentially Create**:
- `TerraNova.Client/TerrainCollisionManager.cs` (optional helper class using IPhysicsWorld)

**Notes**:
- All terrain collision management through IPhysicsWorld interface
- Block shapes created via IPhysicsShapeFactory.CreateBox()
- Static bodies for terrain (isStatic: true parameter)
- Physics engine abstraction fully maintained

---

#### Task 2B.4: Implement Gravity and Jumping (Updated for abstraction)
**Status**: Blocked (requires Task 2B.3)
**Priority**: HIGH
**Estimated Time**: 30-45 minutes
**Complexity**: Simple

**Description**: Add gravity and jumping using IPhysicsBody and IPhysicsWorld interfaces.

**Implementation Steps**:
1. Call IPhysicsWorld.SetGravity(new Vector3(0, -32f, 0)) to enable gravity
2. Enable gravity on player body using IPhysicsBody.IsGravityEnabled = true
3. Implement ground detection:
   - Use IPhysicsWorld.Raycast() downward from player position
   - Or use IPhysicsBody.IsGrounded property (if adapter implements it)
4. Add jump mechanic:
   - Check if grounded (via raycast or IsGrounded)
   - When spacebar pressed AND grounded: IPhysicsBody.ApplyImpulse(new Vector3(0, jumpForce, 0))
5. Tune gravity strength (Minecraft: ~32 m/s²) via SetGravity()
6. Tune jump force for Minecraft-like jump height (~1.25 blocks)
7. Prevent bunny-hopping (jumping while airborne)

**Completion Criteria**:
- Player falls downward when not on solid ground
- Player lands on terrain and stops falling
- Spacebar makes player jump when on ground (via IPhysicsBody.ApplyImpulse)
- Cannot jump while already in air (ground detection working)
- Jump height and gravity feel similar to Minecraft
- Smooth, responsive controls
- No direct Jitter2 dependencies (uses IPhysicsWorld/IPhysicsBody only)
- Build: 0 warnings, 0 errors

**Files to Modify**:
- `TerraNova.Client/PlayerController.cs` (add gravity, jumping logic, ground detection)

**Notes**:
- Ground detection via IPhysicsWorld.Raycast() abstraction
- Jump impulse via IPhysicsBody.ApplyImpulse() abstraction
- Gravity configured via IPhysicsWorld.SetGravity() abstraction
- Physics engine remains completely swappable

---

## Future-Proofing Benefits

### Easy Physics Engine Migration
With this abstraction layer, switching from Jitter2 to another engine (e.g., Bullet, PhysX, Bepu) requires:
1. Create new adapter classes implementing Core interfaces (e.g., BulletPhysicsWorld, BepuPhysicsWorld)
2. Update dependency injection to use new adapter
3. **No changes to PlayerController or any game logic**

### Testing Benefits
- Can create mock IPhysicsWorld for unit testing PlayerController
- Can test game logic without physics engine dependency
- Follows same pattern as IRenderer (already proven testable)

### Platform Portability
- Core project remains platform-agnostic (no physics engine coupling)
- Different platforms can use different physics engines (e.g., web might use different engine)
- Abstractions enable maximum portability

### Maintainability
- Physics engine implementation changes don't affect game logic
- Clear separation of concerns (interfaces vs. implementation)
- Follows established project patterns (IRenderer, ICameraView)

---

## Comparison: With vs. Without Abstraction

### Without Abstraction (Anti-pattern)
```csharp
// PlayerController.cs directly depends on Jitter2
using Jitter2;
using Jitter2.Dynamics;
using Jitter2.Collision.Shapes;

public class PlayerController
{
    private World _jitterWorld;  // Direct Jitter dependency
    private RigidBody _playerBody;  // Direct Jitter type

    public PlayerController()
    {
        _jitterWorld = new World();  // Tightly coupled
        var shape = new CapsuleShape(0.5f, 1.8f);  // Jitter-specific
        _playerBody = _jitterWorld.CreateRigidBody();  // Jitter API
        // ... more Jitter-specific code
    }
}
```

**Problems**:
- Jitter2 types throughout PlayerController
- Cannot swap physics engine without rewriting PlayerController
- Cannot unit test without Jitter2 dependency
- Violates dependency inversion principle

---

### With Abstraction (Recommended)
```csharp
// PlayerController.cs depends on interfaces only
using TerraNova.Core;  // Interfaces only

public class PlayerController
{
    private readonly IPhysicsWorld _physicsWorld;  // Interface dependency
    private IPhysicsBody _playerBody;  // Interface type

    public PlayerController(IPhysicsWorld physicsWorld, IPhysicsShapeFactory shapeFactory)
    {
        _physicsWorld = physicsWorld;  // Injected abstraction
        var shape = shapeFactory.CreateCapsule(0.5f, 1.8f);  // Factory abstraction
        _playerBody = _physicsWorld.CreateBody(shape, position, false);  // Interface API
        // ... all code uses interface methods
    }
}
```

**Benefits**:
- No Jitter2 types in PlayerController
- Can swap to BulletPhysics by injecting BulletPhysicsWorld
- Can mock IPhysicsWorld for unit testing
- Follows dependency inversion principle
- Matches IRenderer pattern (proven architecture)

---

## Risk Assessment

### Risk: Abstraction Overhead
**Likelihood**: Low
**Mitigation**: Thin adapter pattern (minimal performance impact), follows proven IRenderer pattern

### Risk: Incomplete Interface Design
**Likelihood**: Medium
**Mitigation**: Interface designed based on Stage 2B requirements (movement, collision, jumping), can extend later if needed

### Risk: Jitter2 Feature Exposure
**Likelihood**: Low
**Mitigation**: Careful encapsulation in adapter classes, code review to prevent leakage

### Risk: Increased Initial Development Time
**Likelihood**: High
**Impact**: Mitigated by long-term maintainability benefits

**Analysis**:
- Additional time: ~45-60 minutes for abstraction layer creation
- Time saved: Future physics engine changes (hours/days), testing improvements (hours)
- Net benefit: Positive over project lifetime

---

## Implementation Timeline

### Total Estimated Time: 4.5-5.5 hours
- **Task 2B.0 (Abstraction Layer)**: 45-60 minutes [NEW]
- **Task 2B.1 (Jitter2 Integration)**: 60-90 minutes [INCREASED]
- **Task 2B.2 (Player Body)**: 45-60 minutes [UNCHANGED]
- **Task 2B.3 (Terrain Collision)**: 60-90 minutes [UNCHANGED]
- **Task 2B.4 (Gravity/Jumping)**: 30-45 minutes [UNCHANGED]

**Time Investment Analysis**:
- Original ROADMAP estimate: 3-4 hours (no abstraction)
- New estimate with abstraction: 4.5-5.5 hours
- Additional investment: 1-1.5 hours
- ROI: Future physics engine swap (saved 8-16 hours), testing improvements (saved 4-8 hours)

---

## Architectural Decision Record (ADR)

### Decision: Create Physics Abstraction Layer Before Jitter2 Integration

**Context**:
- User requested: "Hide physics behind right abstractions in case we need different library"
- Project already has proven abstraction patterns (IRenderer, ICameraView)
- Stage 2B requires physics for player movement, collision, gravity

**Decision**:
Create IPhysicsWorld, IPhysicsBody, IPhysicsShape, IPhysicsShapeFactory interfaces in Core project before integrating Jitter2

**Rationale**:
1. Follows existing project patterns (IRenderer proves this works)
2. Enables physics engine swapping without game logic changes
3. Improves testability (can mock physics)
4. Maintains platform-agnostic Core project
5. Relatively low time investment (1-1.5 hours) for significant long-term benefits

**Consequences**:
- Positive: Clean architecture, swappable physics, better tests, follows patterns
- Negative: Slightly longer initial implementation (1-1.5 hours added)
- Trade-off: Accept short-term time cost for long-term maintainability

**Status**: Recommended for Stage 2B implementation

---

## Conclusion

This abstraction strategy:
1. Follows established project patterns (IRenderer, ICameraView)
2. Hides Jitter2 completely behind interfaces
3. Enables future physics engine migration with minimal code changes
4. Improves testability and maintainability
5. Adds ~1-1.5 hours to Stage 2B timeline (reasonable trade-off)
6. Maintains clean architecture principles throughout implementation

**Recommendation**: Implement abstraction layer BEFORE integrating Jitter2 (new Task 2B.0)

**Next Step**: Game-dev-implementer should review this architecture and create Task 2B.0 in ROADMAP.md before proceeding with Jitter2 integration.
