# Terra Nova Development Roadmap

## Phase 1: Core Stability and Desktop Foundation ✅ COMPLETED

**Goal:** Stabilize desktop client architecture and fix critical inconsistencies before adding new features.

**Completion Date:** 2025-11-10
**Status:** All tasks completed and tested successfully
**Build Status:** 0 Warnings, 0 Errors

### Task 1.1: Remove World.RemoveChunk() Violation in OpenTKRenderer ✅
**Status:** Completed
**Priority:** Critical
**Impact:** Eliminated race conditions between ChunkLoader and Renderer

**What Was Done:**
- Removed renderer's improper manipulation of World state (OpenTKRenderer.cs line 135)
- Updated comments and log messages to clarify ChunkLoader owns World data cleanup
- Renderer now only manages GPU mesh lifecycle

**Results:**
- Clean separation of concerns: ChunkLoader manages World, Renderer manages GPU resources
- No race conditions between concurrent World access
- Architecture now follows Single Responsibility Principle

### Task 1.2: Add ILogger to OpenTKRenderer ✅
**Status:** Completed
**Priority:** Medium
**Impact:** Professional logging infrastructure for debugging

**What Was Done:**
- Added ILogger dependency injection to OpenTKRenderer constructor
- Replaced all Console.WriteLine calls with structured logging
- Updated Game.cs to use ILoggerFactory pattern for dynamic logger instantiation
- All log messages use named parameters for better observability

**Results:**
- Consistent logging across entire codebase
- Better debugging capabilities with structured log data
- Follows .NET best practices for dependency injection

### Task 1.3: Cache Raycast Result to Eliminate Duplicates ✅
**Status:** Completed
**Priority:** Medium
**Impact:** 50% reduction in raycast operations per frame

**What Was Done:**
- Added `_cachedRaycastHit` field to Game.cs
- Implemented single raycast per frame in OnUpdateFrame
- Updated HandleBlockInteraction and OnRenderFrame to use cached result

**Results:**
- Performance improvement: 2 raycasts/frame → 1 raycast/frame
- Eliminated duplicate work
- Improved frame timing consistency

### Task 1.4: Fix Distance Metric Mismatch (DEFERRED TO PHASE 2)
**Status:** Deferred
**Priority:** LOW
**Reason:** Not critical for functionality; optimization issue only

**Issue Description:**
- ChunkLoader uses Euclidean distance (`MathF.Sqrt(deltaX² + deltaZ²)`)
- OpenTKRenderer uses Chebyshev distance (`Math.Max(distanceX, distanceZ)`)
- Both use threshold of 12 chunks
- Impact: Corner chunks (e.g., position 9,9) may have GPU meshes retained after World data unloaded

**Deferred Because:**
- Does not cause crashes or data corruption
- Memory impact is minimal (few chunks affected)
- Phase 2 physics work is higher priority
- Can be addressed as optimization later

---

## Phase 2: PlayerController & Physics System (IN PROGRESS)

**Goal:** Extract player logic into clean architecture, then implement Jitter Physics 2 for movement and collision detection.

**Prerequisites:** ✅ Phase 1 complete - stable foundation established

**Estimated Duration:** 4-6 hours total (1-2 hours Stage 2A + 3-4 hours Stage 2B)
**Priority:** HIGH - Core gameplay feature
**Architecture Decision:** Hybrid Sequential Approach (Plan C) - recommended by game-architecture-optimizer

### Architectural Rationale

**Why Two Stages?**
Phase 2 is split into two sequential stages to maintain clean architecture throughout:

1. **Stage 2A: PlayerController Extraction** (1-2 hours)
   - Moves existing player logic from Game.cs into proper PlayerController class
   - Reduces Game.cs from 431 lines to ~300 lines
   - Establishes separation of concerns BEFORE adding complexity
   - Sets foundation for physics to be added in architecturally correct location

2. **Stage 2B: Physics Implementation** (3-4 hours)
   - Adds Jitter Physics 2 to the properly structured PlayerController
   - Physics code goes in correct architectural location from day 1
   - No rework or refactoring required after physics is added

**Why Not Physics First?**
- Adding physics directly to Game.cs would create a "god object" (600+ lines)
- Physics belongs in PlayerController, not in window lifecycle management class
- Would require costly refactoring later (violates Phase 1 principles)
- Plan C saves development time and maintains clean architecture throughout

**Current State:**
- Player logic scattered in Game.cs (hotbar, block interaction, raycasting)
- Game.cs handles both window lifecycle AND player behavior (SRP violation)
- No physics or collision detection
- Movement uses direct position manipulation (noclip-style)

**Target State After Stage 2A:**
- PlayerController owns all player-related logic and state
- Game.cs focused on window lifecycle, initialization, and delegation
- Clean separation of concerns ready for physics integration
- All existing functionality preserved (hotbar, block breaking/placing)

**Target State After Stage 2B:**
- Physics-based player controller with collision detection
- Realistic gravity and jumping
- Player cannot walk through blocks
- Foundation ready for Phase 3 gameplay features

---

## Stage 2A: PlayerController Extraction (NEXT)

**Goal:** Move existing player logic from Game.cs to PlayerController for clean architecture.

**Estimated Duration:** 1-2 hours
**Complexity:** Simple to Moderate
**Why First:** Establishes proper separation of concerns before adding physics complexity

### Task 2A.1: Move Hotbar Selection Logic
**Status:** Ready
**Priority:** HIGH
**Estimated Time:** 20 minutes
**Complexity:** SIMPLE

**Description:** Extract hotbar selection from Game.cs to PlayerController.HandleHotbarSelection().

**Implementation Steps:**
1. Create `TerraNova.Client/PlayerController.cs` with basic structure
2. Add SelectedHotbarSlot property (int, range 0-8)
3. Implement HandleHotbarSelection(KeyboardState) method
4. Move hotbar logic from Game.cs lines 153-169
5. Return boolean indicating if selection changed

**Completion Criteria:**
- PlayerController.cs exists with HandleHotbarSelection() method
- Method handles Keys.D1 through Keys.D9 (map to slots 0-8)
- Returns true when selection changes, false otherwise
- Game.cs lines 153-169 can be safely removed in Task 2A.4
- Build: 0 warnings, 0 errors

**Files to Create:**
- `TerraNova.Client/PlayerController.cs` (new class)

**Files to Reference (Read Only):**
- `TerraNova.Client/Game.cs` (lines 153-169 contain hotbar logic to extract)

**Notes:**
- PlayerController will need Camera and GameEngine dependencies (add in constructor)
- Start with minimal constructor parameters; add more as needed in later tasks
- This task creates the foundation class; subsequent tasks add more methods

### Task 2A.2: Move Block Interaction Logic
**Status:** Blocked (requires Task 2A.1)
**Priority:** HIGH
**Estimated Time:** 30 minutes
**Complexity:** SIMPLE

**Description:** Extract block breaking and placement from Game.cs to PlayerController.HandleBlockInteraction().

**Implementation Steps:**
1. Add Inventory property to PlayerController (reference to existing inventory)
2. Implement HandleBlockInteraction(MouseState, RaycastHit?) method
3. Move block break logic from Game.cs lines 280-293
4. Move block place logic from Game.cs lines 295-317
5. Handle null raycast (no block targeted)

**Completion Criteria:**
- HandleBlockInteraction() method exists in PlayerController
- Left mouse button breaks blocks (same behavior as before)
- Right mouse button places blocks (same behavior as before)
- Uses passed RaycastHit from cached raycast
- Game.cs lines 280-317 can be safely removed in Task 2A.4
- Build: 0 warnings, 0 errors

**Files to Modify:**
- `TerraNova.Client/PlayerController.cs` (add HandleBlockInteraction method)

**Files to Reference (Read Only):**
- `TerraNova.Client/Game.cs` (lines 280-293: break, lines 295-317: place)

**Notes:**
- Method takes RaycastHit? (nullable) - handles case where no block is targeted
- Requires GameEngine reference (already added in Task 2A.1)
- Requires Inventory reference (add to constructor in this task)

### Task 2A.3: Move Raycast Caching Logic
**Status:** Blocked (requires Task 2A.2)
**Priority:** HIGH
**Estimated Time:** 15 minutes
**Complexity:** SIMPLE

**Description:** Extract raycast caching from Game.cs to PlayerController.UpdateRaycast().

**Implementation Steps:**
1. Add private _cachedRaycast field to PlayerController (RaycastHit?)
2. Add public CachedRaycast property (read-only access)
3. Implement UpdateRaycast() method
4. Move raycast logic from Game.cs lines 256-265
5. Store result in _cachedRaycast field

**Completion Criteria:**
- UpdateRaycast() method exists in PlayerController
- Performs raycast from camera position/direction (max 8 blocks)
- Stores result in _cachedRaycast (nullable RaycastHit)
- CachedRaycast property provides read-only access
- Game.cs lines 256-265 can be safely removed in Task 2A.4
- Build: 0 warnings, 0 errors

**Files to Modify:**
- `TerraNova.Client/PlayerController.cs` (add UpdateRaycast method and CachedRaycast property)

**Files to Reference (Read Only):**
- `TerraNova.Client/Game.cs` (lines 256-265 contain raycast logic)

**Notes:**
- Requires Camera reference (already added in Task 2A.1)
- This maintains the Phase 1 optimization (single raycast per frame)
- UpdateRaycast() will be called once per frame from PlayerController.Update()

### Task 2A.4: Update Game.cs to Delegate to PlayerController
**Status:** Blocked (requires Tasks 2A.1, 2A.2, 2A.3)
**Priority:** HIGH
**Estimated Time:** 20 minutes
**Complexity:** MODERATE

**Description:** Replace direct player logic in Game.cs with PlayerController.Update() delegation pattern.

**Implementation Steps:**
1. Add _playerController field to Game.cs
2. Initialize PlayerController in OnLoad (after Camera and GameEngine)
3. Add PlayerController.Update(KeyboardState, MouseState, float deltaTime) method
4. Call UpdateRaycast(), HandleHotbarSelection(), HandleBlockInteraction() from Update()
5. Replace Game.cs player logic sections with _playerController.Update() call
6. Remove extracted code blocks (lines 153-169, 256-265, 280-317)
7. Verify Game.cs line count drops to ~300 lines

**Completion Criteria:**
- PlayerController instantiated in Game.cs OnLoad
- PlayerController.Update() called once per frame from Game.cs OnUpdateFrame
- All hotbar, raycast, and block interaction logic removed from Game.cs
- Game.cs reduced from 431 lines to approximately 300 lines
- All existing functionality still works (hotbar, breaking, placing)
- Build: 0 warnings, 0 errors

**Files to Modify:**
- `TerraNova.Client/Game.cs` (add PlayerController integration, remove extracted logic)
- `TerraNova.Client/PlayerController.cs` (add Update() method to orchestrate all player logic)

**Notes:**
- Update() method in PlayerController orchestrates the three extracted methods
- Game.cs becomes thinner and more focused on window lifecycle
- This is the payoff task where architectural improvement becomes visible

### Task 2A.5: Update Hotbar UI Rendering
**Status:** Blocked (requires Task 2A.4)
**Priority:** HIGH
**Estimated Time:** 10 minutes
**Complexity:** SIMPLE

**Description:** Update hotbar rendering in Game.cs to query PlayerController state instead of local variable.

**Implementation Steps:**
1. Find hotbar rendering code in Game.cs OnRenderFrame
2. Replace _selectedHotbarSlot reference with _playerController.SelectedHotbarSlot
3. Remove local _selectedHotbarSlot field from Game.cs (now owned by PlayerController)
4. Verify hotbar selection visual feedback still works

**Completion Criteria:**
- Hotbar UI renders using PlayerController.SelectedHotbarSlot property
- Local _selectedHotbarSlot field removed from Game.cs
- Hotbar selection visual feedback works correctly (selected slot highlighted)
- Build: 0 warnings, 0 errors

**Files to Modify:**
- `TerraNova.Client/Game.cs` (update hotbar rendering, remove local field)

**Notes:**
- This is a small cleanup task that completes the PlayerController extraction
- Ensures Game.cs has no player state - all owned by PlayerController

### Stage 2A Success Criteria

Before proceeding to Stage 2B, verify:
- Build: 0 warnings, 0 errors
- Hotbar selection works (keys 1-9 change selected slot)
- Block breaking works (left click removes blocks)
- Block placement works (right click adds blocks)
- Block highlighting works (targeted block shows outline)
- Game.cs reduced from 431 lines to approximately 300 lines
- PlayerController owns all player logic (hotbar, interaction, raycast)
- Clean separation: Game.cs = window lifecycle, PlayerController = player behavior

---

## Stage 2B: Physics Implementation (BLOCKED - Requires Stage 2A Complete)

**Goal:** Implement Jitter Physics 2 in the properly structured PlayerController.

**Estimated Duration:** 3-4 hours
**Complexity:** Medium to High
**Why Second:** Physics code goes into architecturally correct location from day 1

### Task 2B.1: Integrate Jitter Physics 2 Library
**Status:** Blocked (requires Stage 2A complete)
**Priority:** HIGH
**Estimated Time:** 30-45 minutes
**Complexity:** Simple

**Description:** Install Jitter Physics 2 NuGet package and initialize physics world in PlayerController.

**Implementation Steps:**
1. Add Jitter2 NuGet package to TerraNova.Client project
2. Add physics World instance to PlayerController
3. Initialize physics world in PlayerController constructor
4. Add physics world step to PlayerController.Update() method
5. Verify physics world runs without errors

**Completion Criteria:**
- Jitter2 NuGet package installed and builds successfully
- Physics World initialized in PlayerController
- Physics simulation steps each frame (even if no bodies yet)
- No compilation errors or runtime exceptions
- Log output confirms physics world is updating

**Files to Modify:**
- `TerraNova.Client/TerraNova.Client.csproj` (NuGet reference)
- `TerraNova.Client/PlayerController.cs` (add physics World, initialization, update)

**Notes:**
- Jitter2 is a lightweight, pure C# physics engine (no native dependencies)
- Physics World owned by PlayerController (not Game.cs)
- Start with minimal configuration; optimization comes later

### Task 2B.2: Implement Player Physics Body
**Status:** Blocked (requires Task 2B.1)
**Priority:** HIGH
**Estimated Time:** 45-60 minutes
**Complexity:** Medium

**Description:** Add physics body (capsule) to PlayerController and implement force-based WASD movement.

**Implementation Steps:**
1. Create capsule rigid body in PlayerController
2. Add capsule to physics world
3. Position capsule at camera position
4. Implement force-based movement in PlayerController.Update()
5. Synchronize camera position with physics body position each frame
6. Disable/replace direct position manipulation

**Completion Criteria:**
- Player represented by capsule physics body
- WASD movement works via physics forces
- Camera position follows physics body
- Player can move around (floating is OK - no terrain collision yet)
- No crashes or physics exceptions
- Build: 0 warnings, 0 errors

**Files to Modify:**
- `TerraNova.Client/PlayerController.cs` (add physics body, force-based movement)

**Notes:**
- Use capsule shape (cylinder with rounded ends) - standard for FPS characters
- Initial movement may feel floaty - fine-tuning comes in Task 2B.4
- Player will still pass through terrain until Task 2B.3

### Task 2B.3: Add Terrain Collision Detection
**Status:** Blocked (requires Task 2B.2)
**Priority:** HIGH
**Estimated Time:** 60-90 minutes
**Complexity:** High

**Description:** Create physics collision shapes for terrain blocks so player collides with world.

**Implementation Steps:**
1. Implement system to add box collision shapes for solid blocks near player
2. Add collision shapes for all non-Air blocks within interaction range
3. Handle chunk loading/unloading - add/remove collision shapes dynamically
4. Synchronize physics bodies when blocks are broken/placed
5. Test collision detection with various terrain configurations

**Completion Criteria:**
- Player collides with terrain blocks and cannot walk through them
- Player can walk on top of blocks
- Collision shapes load/unload with chunks properly
- No performance issues with collision detection
- Player cannot fall through floor or clip through walls
- Build: 0 warnings, 0 errors

**Files to Modify:**
- `TerraNova.Client/PlayerController.cs` (manage terrain collision shapes in physics world)

**Files to Potentially Create:**
- `TerraNova.Client/TerrainPhysicsManager.cs` (helper class if needed)

**Notes:**
- Only add collision shapes for blocks near player (performance optimization)
- Each block = simple box shape (not full mesh collision)
- Hook into existing chunk loading system
- This is the most complex task in Phase 2

### Task 2B.4: Implement Gravity and Jumping
**Status:** Blocked (requires Task 2B.3)
**Priority:** HIGH
**Estimated Time:** 30-45 minutes
**Complexity:** Simple

**Description:** Add gravity to player physics body and implement spacebar jumping with ground detection.

**Implementation Steps:**
1. Enable gravity on player physics body
2. Implement ground detection (raycast downward from capsule)
3. Add jump mechanic (apply upward impulse when spacebar pressed AND grounded)
4. Tune gravity strength and jump force for Minecraft-like feel
5. Prevent bunny-hopping (jumping while airborne)

**Completion Criteria:**
- Player falls downward when not on solid ground
- Player lands on terrain and stops falling
- Spacebar makes player jump when on ground
- Cannot jump while already in air
- Jump height and gravity feel similar to Minecraft
- Smooth, responsive controls
- Build: 0 warnings, 0 errors

**Files to Modify:**
- `TerraNova.Client/PlayerController.cs` (add gravity, jumping logic, ground detection)

**Notes:**
- Minecraft gravity: ~32 blocks/sec² (use as reference)
- Minecraft jump height: ~1.25 blocks (use as reference)
- Ground detection: raycast slightly below capsule bottom
- Fine-tuning these values is critical for good game feel

### Stage 2B Success Criteria

Before marking Phase 2 complete, verify:
- Build: 0 warnings, 0 errors
- Physics-based movement feels natural and responsive
- No clipping through terrain or falling through floor
- Jump height and gravity feel similar to Minecraft
- Performance remains smooth (60 FPS) with physics enabled
- All Stage 2A functionality still works (hotbar, breaking, placing)
- PlayerController contains all player logic including physics

### Optional Enhancement: Fix Distance Metric Mismatch
**Status:** Optional (can be done anytime during Phase 2)
**Priority:** LOW
**Estimated Time:** 15 minutes
**Complexity:** Trivial

**Description:** Align ChunkLoader and OpenTKRenderer to use same distance calculation (Chebyshev).

**Implementation Steps:**
1. Change ChunkLoader.cs line 101 from Euclidean to Chebyshev distance
2. Change line 63 (load distance check) similarly
3. Update distance type from `float` to `int`
4. Test chunk loading/unloading behavior

**Completion Criteria:**
- Both systems use `Math.Max(Math.Abs(deltaX), Math.Abs(deltaZ))`
- No chunks have GPU meshes without World data
- Chunk behavior unchanged for player perception

**Files to Modify:**
- `TerraNova.GameLogic/ChunkLoader.cs` (lines 63, 101)

---

## Phase 3: Gameplay Features (PLANNED)

**Goal:** Add core Minecraft-like gameplay mechanics.

**Prerequisites:** Phase 2 complete (physics required for gameplay feel)

### Task 3.1: Block Breaking
**Status:** Planned
**Description:** Implement block destruction with mouse click and update chunk meshes.

### Task 3.2: Block Placement
**Status:** Planned
**Description:** Implement block placement with raycasting and adjacency rules.

### Task 3.3: Inventory System
**Status:** Planned
**Description:** Basic inventory UI and block selection.

---

## Phase 4: Polish and Optimization (PLANNED)

**Goal:** Performance improvements and user experience enhancements.

**Prerequisites:** Phase 3 complete (feature-complete baseline)

### Task 4.1: Chunk Mesh Optimization
**Status:** Planned
**Description:** Optimize mesh generation and implement frustum culling.

### Task 4.2: Texture System
**Status:** Planned
**Description:** Replace placeholder textures with proper block texture atlas.

### Task 4.3: Audio System
**Status:** Planned
**Description:** Add sound effects for footsteps, block breaking, and placement.

### Task 4.4: Graphics Enhancements
**Status:** Planned
**Description:** Lighting improvements, shadows, ambient occlusion.

---

## Technical Debt Log

**Active Issues:**
- Distance metric mismatch between ChunkLoader (Euclidean) and OpenTKRenderer (Chebyshev)
  - Impact: Minor GPU memory inefficiency for corner chunks
  - Priority: LOW - can be fixed during Phase 2 as optional enhancement
  - Estimated fix time: 15 minutes

**Resolved Issues:**
- ✅ World.RemoveChunk() architectural violation in OpenTKRenderer (Phase 1, Task 1.1)
- ✅ Duplicate raycast operations per frame (Phase 1, Task 1.3)
- ✅ Console.WriteLine usage instead of ILogger (Phase 1, Task 1.2)

---

## Development Guidelines

### Phase Execution Rules
1. **Sequential Task Completion:** Complete tasks in order within each phase (dependencies matter)
2. **Testing Required:** Each task must be tested before proceeding to next task
3. **Zero Warnings Policy:** Maintain 0 compilation warnings throughout development
4. **Commit Strategy:** Commit after each completed task with descriptive message

### Phase Transition Criteria
Before moving to next phase, verify:
- All tasks in current phase marked COMPLETED (or explicitly DEFERRED with justification)
- Build status: 0 Warnings, 0 Errors
- User testing confirms functionality works as expected
- ROADMAP.md updated to reflect actual completion status

### When Technical Debt Arises
Evaluate new issues discovered during implementation:
- **Critical/High Priority:** Add to current phase (blocks progress or causes bugs)
- **Medium Priority:** Add to next appropriate phase based on dependencies
- **Low Priority:** Add to Technical Debt Log for Phase 4 or future work

### Communication Protocol
- Update ROADMAP.md status after completing each task
- Document "What Was Done" and "Results" for completed tasks
- Keep Technical Debt Log current with newly discovered issues
- Mark phases with clear status: COMPLETED, IN PROGRESS, READY TO START, PLANNED, BLOCKED

---

## Phase 2 Execution Recommendation

**Architecture Decision:** Plan C (Hybrid Sequential Approach) - STRONGLY RECOMMENDED

**Start with:** Stage 2A (PlayerController Extraction) - DO NOT skip to physics!

**Suggested Approach:**

### Stage 2A (1-2 hours):
1. Complete Task 2A.1 (Move Hotbar Selection) - 20 min
2. Complete Task 2A.2 (Move Block Interaction) - 30 min
3. Complete Task 2A.3 (Move Raycast Caching) - 15 min
4. Complete Task 2A.4 (Update Game.cs Delegation) - 20 min
5. Complete Task 2A.5 (Update Hotbar UI) - 10 min
6. Verify Stage 2A Success Criteria before proceeding

### Stage 2B (3-4 hours):
7. Complete Task 2B.1 (Jitter Physics 2 Integration) - 30-45 min
8. Complete Task 2B.2 (Implement Player Physics Body) - 45-60 min
9. Complete Task 2B.3 (Add Terrain Collision) - 60-90 min (most complex)
10. Complete Task 2B.4 (Implement Gravity and Jumping) - 30-45 min
11. Verify Stage 2B Success Criteria
12. (Optional) Fix distance metric mismatch if time permits

**Why This Order Matters:**
- Stage 2A establishes clean architecture BEFORE adding physics complexity
- Physics code goes into PlayerController (correct location) from day 1
- Skipping to physics creates 600+ line god object in Game.cs
- Plan C saves 1+ hour compared to physics-first approach (no rework needed)
- Maintains Phase 1 principles throughout development

**Expected Challenges:**
- Task 2A.4 requires careful attention to ensure all logic is properly extracted
- Task 2B.3 (terrain collision) will likely need iteration to handle edge cases
- Physics tuning (Task 2B.4) is subjective - user testing is essential
- Jitter2 documentation may be limited - expect some trial and error

**Success Metrics for Phase 2:**
- Stage 2A: Game.cs reduced to ~300 lines, all player logic in PlayerController
- Stage 2B: Physics-based movement feels natural and responsive
- No clipping through terrain or falling through floor
- Jump height and gravity feel similar to Minecraft
- Performance remains smooth (60 FPS) with physics enabled
- Clean architecture maintained throughout (no god objects)
