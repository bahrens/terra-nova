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

**Estimated Duration:** 8.5-10.5 hours total (1-2 hours Stage 2A + 25-30 min Stage 2B.0 + 4-4.5 hours Stage 2B.0 Extended + 3-4 hours Stage 2B)
**Priority:** HIGH - Core gameplay feature
**Architecture Decision:** Application Coordinator Pattern - recommended by game-architecture-optimizer

### Architectural Rationale

**Why Four Stages?**
Phase 2 is split into four sequential stages to maintain clean architecture throughout:

1. **Stage 2A: PlayerController Extraction** (1-2 hours)
   - Moves existing player logic from Game.cs into proper PlayerController class
   - Reduces Game.cs from 431 lines to ~300 lines
   - Establishes separation of concerns BEFORE adding complexity
   - Sets foundation for physics to be added in architecturally correct location

2. **Stage 2B.0: Pre-Physics Refactoring** (25-30 minutes) - Input Routing
   - Fixes architectural gaps discovered by game-architecture-optimizer
   - Moves camera movement input from Game.cs to PlayerController
   - Implements proper Update() orchestration method
   - Prevents cross-component coupling and incorrect update order
   - Makes physics integration straightforward

3. **Stage 2B.0 Extended: Application Coordinator Pattern** (4-4.5 hours) - CRITICAL REFACTORING
   - Eliminates "God Object" anti-pattern in Game.cs (335 lines doing 6 jobs)
   - Extracts business logic into 4 specialized coordinator classes
   - Reduces Game.cs to thin OpenTK adapter (~110 lines)
   - Fixes network event registration architectural flaw
   - **MUST HAPPEN BEFORE STAGE 2B** - prevents Game.cs ballooning to 500+ lines with physics
   - Saves 4+ hours of rework vs. adding physics first

4. **Stage 2B: Physics Implementation** (3-4 hours)
   - Adds Jitter Physics 2 to the properly structured architecture
   - Physics integrates cleanly with ClientApplication coordinator
   - Clean separation between OpenTK lifecycle (Game.cs) and game logic (ClientApplication)
   - No rework or refactoring required after physics is added

**Why Refactor Game.cs Before Physics?**
- Game.cs is already a "God Object" anti-pattern (335 lines, 6 responsibilities)
- Adding physics directly would balloon Game.cs to 500+ lines
- Physics belongs in application layer, not OpenTK lifecycle management class
- Refactoring now: 4 hours. Refactoring after physics: 8+ hours of painful rework
- Testing is easier without physics complexity
- Creates professional game architecture that scales to future features
- Application Coordinator Pattern is industry standard for game engines

**Current State:**
- Player logic scattered in Game.cs (hotbar, block interaction, raycasting)
- Game.cs handles both window lifecycle AND player behavior (SRP violation)
- No physics or collision detection
- Movement uses direct position manipulation (noclip-style)

**Target State After Stage 2A:**
- PlayerController owns all player-related logic and state
- Game.cs focused on window lifecycle, initialization, and delegation
- Clean separation of concerns ready for input refactoring
- All existing functionality preserved (hotbar, block breaking/placing)

**Target State After Stage 2B.0:**
- All player input (keyboard and mouse) routed through PlayerController
- PlayerController.Update() properly orchestrates all subsystems
- Game.cs simplified to single Update() delegation
- Clean foundation ready for Game.cs refactoring

**Target State After Stage 2B.0 Extended:**
- Game.cs is thin OpenTK adapter (~110 lines, single responsibility)
- ClientApplication orchestrates all game systems (Camera, PlayerController, Renderer, GameEngine)
- NetworkCoordinator manages connection lifecycle and world events (fixes architectural flaw)
- UIManager centralizes UI overlay management (Crosshair, Hotbar)
- WindowStateManager handles FPS tracking and window state
- Professional game architecture following Application Coordinator Pattern
- Clean foundation ready for physics integration

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

Before proceeding to Stage 2B.0, verify:
- Build: 0 warnings, 0 errors
- Hotbar selection works (keys 1-9 change selected slot)
- Block breaking works (left click removes blocks)
- Block placement works (right click adds blocks)
- Block highlighting works (targeted block shows outline)
- Game.cs reduced from 431 lines to approximately 300 lines
- PlayerController owns all player logic (hotbar, interaction, raycast)
- Clean separation: Game.cs = window lifecycle, PlayerController = player behavior

---

## Stage 2B.0: Pre-Physics Refactoring (BLOCKED - Requires Stage 2A Complete)

**Goal:** Refactor input handling architecture to prepare for physics integration.

**Estimated Duration:** 25-30 minutes
**Complexity:** Simple
**Why Critical:** Prevents cross-component coupling and incorrect update order when physics is added

**Architectural Rationale:**

Stage 2A successfully extracted player logic into PlayerController, but two architectural gaps remain:

1. **Input Location Mismatch:** Camera movement (WASD keys) is still in Game.cs, but physics body will live in PlayerController. This creates coupling risk when physics is added.

2. **Update Orchestration Missing:** PlayerController.Update() is a stub. Game.cs still orchestrates PlayerController internals, risking incorrect update order (common source of physics bugs).

**Without Stage 2B.0:** Stage 2B will be messier, require more rework, and create technical debt.

**With Stage 2B.0:** Physics integration becomes straightforward internal changes to PlayerController.

### Task 2B.0.1: Move Camera Movement Input to PlayerController
**Status:** Ready
**Priority:** CRITICAL
**Estimated Time:** 15 minutes
**Complexity:** SIMPLE

**Description:** Move WASD/Space/Shift camera movement input from Game.cs to PlayerController for proper encapsulation.

**Implementation Steps:**
1. Create `PlayerController.HandleMovementInput(KeyboardState, float deltaTime)` method
2. Move WASD/Space/Shift logic from Game.cs OnUpdateFrame (lines 173-184)
3. Update Game.cs to call `_playerController.HandleMovementInput(keyboardState, deltaTime)`
4. Preserve current behavior (direct camera.ProcessKeyboard calls)
5. Build and verify functionality unchanged

**Completion Criteria:**
- HandleMovementInput() method exists in PlayerController
- Handles W/A/S/D keys for movement, Space for up, LeftShift for down
- Uses deltaTime for frame-independent movement
- Game.cs lines 173-184 replaced with single HandleMovementInput() call
- All movement works identically to before refactoring
- Build: 0 warnings, 0 errors

**Files to Modify:**
- `TerraNova.Client/PlayerController.cs` (add HandleMovementInput method)
- `TerraNova.Client/Game.cs` (replace WASD logic with method call)

**Notes:**
- Currently calls camera.ProcessKeyboard() directly - this is correct for now
- When physics is added in Stage 2B, these will become force applications on physics body
- This refactoring isolates movement input in the correct architectural location

### Task 2B.0.2: Implement PlayerController.Update() Orchestration
**Status:** Blocked (requires Task 2B.0.1)
**Priority:** CRITICAL
**Estimated Time:** 10 minutes
**Complexity:** SIMPLE

**Description:** Implement PlayerController.Update() method body to orchestrate all player subsystems in correct order.

**Implementation Steps:**
1. Implement Update() method body (currently stub at lines 77-81 in PlayerController.cs)
2. Orchestrate method calls in correct order:
   - HandleMovementInput(keyboardState, deltaTime)
   - HandleHotbarSelection(keyboardState)
   - UpdateRaycast()
   - HandleBlockInteraction(mouseState)
3. Update Game.cs OnUpdateFrame to single `_playerController.Update(keyboardState, mouseState, deltaTime)` call
4. Remove scattered method calls from Game.cs
5. Build and verify functionality unchanged

**Completion Criteria:**
- PlayerController.Update() fully implemented (no longer stub)
- Calls subsystem methods in correct order
- Game.cs OnUpdateFrame simplified to single _playerController.Update() call
- All scattered PlayerController method calls removed from Game.cs
- All functionality works identically to before refactoring
- Build: 0 warnings, 0 errors

**Files to Modify:**
- `TerraNova.Client/PlayerController.cs` (implement Update() method body)
- `TerraNova.Client/Game.cs` (simplify to single Update() delegation)

**Notes:**
- This establishes proper update order: input → selection → raycast → interaction
- When physics is added, physics step will be inserted between input and interaction
- Prevents common physics bugs caused by incorrect update ordering

### Task 2B.0.3: Move Mouse Look to PlayerController (OPTIONAL)
**Status:** Blocked (requires Task 2B.0.2)
**Priority:** RECOMMENDED
**Estimated Time:** 5 minutes
**Complexity:** SIMPLE

**Description:** Complete input abstraction by moving mouse look from Game.cs to PlayerController.

**Implementation Steps:**
1. Create `PlayerController.HandleMouseLook(float deltaX, float deltaY)` method
2. Move mouse look logic from Game.cs OnMouseMove event handler
3. Update Game.cs OnMouseMove to call `_playerController.HandleMouseLook(e.DeltaX, e.DeltaY)`
4. Preserve current camera rotation behavior
5. Build and verify functionality unchanged

**Completion Criteria:**
- HandleMouseLook() method exists in PlayerController
- Takes deltaX and deltaY parameters
- Updates camera rotation (preserves current behavior)
- Game.cs OnMouseMove simplified to single HandleMouseLook() call
- Mouse look works identically to before refactoring
- Build: 0 warnings, 0 errors

**Files to Modify:**
- `TerraNova.Client/PlayerController.cs` (add HandleMouseLook method)
- `TerraNova.Client/Game.cs` (simplify OnMouseMove event handler)

**Notes:**
- Optional but recommended for consistency
- Completes input abstraction - all input routed through PlayerController
- Makes future VR or gamepad support easier to add

### Stage 2B.0 Success Criteria

Before proceeding to Stage 2B.0 Extended, verify:
- Build: 0 warnings, 0 errors
- All player input (keyboard and mouse) routed through PlayerController
- Game.cs OnUpdateFrame simplified to single _playerController.Update() call
- All movement (WASD/Space/Shift) works identically to before
- Mouse look works identically to before
- Hotbar, block breaking/placing still work
- PlayerController.Update() orchestrates all subsystems in correct order
- Clean foundation established for Game.cs refactoring

---

## Stage 2B.0 Extended: Application Coordinator Pattern (BLOCKED - Requires Stage 2B.0 Complete)

**Goal:** Refactor Game.cs from "God Object" anti-pattern to thin OpenTK adapter following Application Coordinator Pattern.

**Estimated Duration:** 4-4.5 hours
**Complexity:** Medium to High
**Why Critical:** Prevents Game.cs from becoming 500+ lines when physics is added; establishes professional game architecture

### Architectural Analysis: The "God Object" Problem

**Current Game.cs Responsibilities (6 distinct jobs):**
1. OpenTK lifecycle management (OnLoad, OnUpdateFrame, OnRenderFrame) - **BELONGS HERE**
2. Application initialization orchestration (Camera, Renderer, GameEngine, etc.) - **EXTRACT**
3. Network event coordination (WorldReceived event handler in update loop) - **EXTRACT - CRITICAL FLAW**
4. UI management (Crosshair, Hotbar ownership and rendering) - **EXTRACT**
5. Window state management (FPS tracking, window title updates) - **EXTRACT**
6. System coordination (orchestrating Camera, PlayerController, Renderer updates) - **EXTRACT**

**Architectural Flaw - Network Events in Update Loop:**
Game.cs currently registers network event handlers inside `OnUpdateFrame` (lines 195-204), causing:
- Event handlers registered thousands of times per second
- Potential memory leaks from repeated event subscriptions
- Incorrect lifecycle management of network state
- Violation of event-driven architecture principles

**Target Architecture - Application Coordinator Pattern:**

Game.cs should be a **thin adapter** between OpenTK and game logic:
- **ONLY** OpenGL setup and lifecycle callbacks
- **DELEGATES** to ClientApplication for all game logic
- Target: ~110 lines (down from 335 lines)

Four new coordinator classes handle business logic:

1. **ClientApplication** - Main orchestrator
   - Owns Camera, PlayerController, Renderer, GameEngine
   - Orchestrates all game systems
   - Public API: Initialize(), Update(), Render(), OnResize(), OnMouseMove()

2. **NetworkCoordinator** - Network management
   - Manages connection lifecycle
   - Handles WorldReceived events (FIXES architectural flaw)
   - Attaches callbacks to GameEngine once, not every frame

3. **UIManager** - UI overlay management
   - Owns Crosshair and Hotbar
   - Centralizes UI initialization, updates, rendering

4. **WindowStateManager** - Window state
   - FPS tracking and reporting
   - Window title updates

**Benefits:**
- Single Responsibility Principle - each class has one job
- Testability - coordinators can be unit tested independently
- Maintainability - easy to find and modify specific functionality
- Scalability - adding physics becomes simple coordinator integration
- Professional architecture - industry standard game engine pattern

**Code Size Impact:**
- Game.cs: 335 lines → ~110 lines (67% reduction)
- OnUpdateFrame: 130 lines → 14 lines (89% reduction)
- OnRenderFrame: 36 lines → 9 lines (75% reduction)
- New coordinator classes: ~460 lines (well-organized, single-purpose)

### Task 2B.0.5: Extract WindowStateManager
**Status:** Ready
**Priority:** HIGH
**Estimated Time:** 30 minutes
**Complexity:** SIMPLE

**Description:** Create WindowStateManager to handle FPS tracking and window state updates.

**Implementation Steps:**
1. Create `TerraNova.Client/WindowStateManager.cs`
2. Move FPS tracking logic from Game.cs (lines 131-143)
3. Add CurrentFPS property (public read-only)
4. Add UpdateFPS(double deltaTime) method
5. Add GetWindowTitle() method that returns formatted title
6. Update Game.cs to instantiate WindowStateManager in OnLoad
7. Update Game.cs to call windowStateManager.UpdateFPS(deltaTime)
8. Update Game.cs to use windowStateManager.GetWindowTitle()

**Completion Criteria:**
- WindowStateManager.cs exists with FPS tracking logic
- CurrentFPS property provides read-only access to FPS value
- UpdateFPS() method handles FPS calculation
- GetWindowTitle() returns "Terra Nova - FPS: XX.X"
- Game.cs uses WindowStateManager instead of local FPS tracking
- Window title updates correctly with FPS display
- Build: 0 warnings, 0 errors

**Files to Create:**
- `TerraNova.Client/WindowStateManager.cs` (~60 lines)

**Files to Modify:**
- `TerraNova.Client/Game.cs` (remove FPS tracking, add WindowStateManager usage)

**Notes:**
- This is the simplest extraction task - good starting point
- WindowStateManager is stateful (tracks FPS history)
- Future enhancements: window size tracking, fullscreen state, etc.

### Task 2B.0.6: Extract UIManager
**Status:** Blocked (requires Task 2B.0.5)
**Priority:** HIGH
**Estimated Time:** 45 minutes
**Complexity:** SIMPLE to MODERATE

**Description:** Create UIManager to centralize UI overlay management (Crosshair, Hotbar).

**Implementation Steps:**
1. Create `TerraNova.Client/UIManager.cs`
2. Add Crosshair and Hotbar fields (ownership transferred from Game.cs)
3. Implement Initialize() method (creates crosshair and hotbar shaders/VAOs)
4. Implement Update(int selectedHotbarSlot) method
5. Implement Render(int width, int height, int selectedHotbarSlot, Inventory inventory) method
6. Move crosshair rendering logic from Game.cs OnRenderFrame
7. Move hotbar rendering logic from Game.cs OnRenderFrame
8. Update Game.cs to instantiate UIManager in OnLoad
9. Update Game.cs to call uiManager.Render() in OnRenderFrame
10. Remove Crosshair and Hotbar fields from Game.cs

**Completion Criteria:**
- UIManager.cs exists with UI rendering logic
- UIManager owns Crosshair and Hotbar instances
- Initialize() method creates all UI resources
- Render() method handles all UI overlay rendering
- Game.cs no longer owns UI components
- Crosshair displays correctly (centered reticle)
- Hotbar displays correctly (9 slots, selection highlight)
- Hotbar selection visual feedback works
- Build: 0 warnings, 0 errors

**Files to Create:**
- `TerraNova.Client/UIManager.cs` (~80 lines)

**Files to Modify:**
- `TerraNova.Client/Game.cs` (remove UI fields, add UIManager usage)

**Notes:**
- UIManager depends on ILogger (add to constructor)
- Future enhancements: health bar, inventory UI, debug overlay, etc.
- Centralizing UI makes future UI features easier to add

### Task 2B.0.7: Extract NetworkCoordinator (CRITICAL - Fixes Architectural Flaw)
**Status:** Blocked (requires Task 2B.0.6)
**Priority:** CRITICAL
**Estimated Time:** 60 minutes
**Complexity:** MODERATE to HIGH

**Description:** Create NetworkCoordinator to manage network lifecycle and FIX event handler registration bug.

**CRITICAL BUG TO FIX:**
Game.cs currently registers WorldReceived event handlers **inside OnUpdateFrame** (lines 195-204), causing:
- Event handlers subscribed 60+ times per second (thousands per session)
- Potential memory leaks from duplicate subscriptions
- Poor performance from redundant event handler chains
- Incorrect network lifecycle management

**Implementation Steps:**
1. Create `TerraNova.Client/NetworkCoordinator.cs`
2. Add GameEngine and Camera fields
3. Add _worldInitialized flag to track initialization state
4. Implement Initialize(GameEngine engine, Camera camera) method
5. Move network event registration ONCE in Initialize() (NOT in update loop)
6. Implement HandleWorldReceived() method (from Game.cs lines 197-202)
7. Move world initialization logic from Game.cs OnUpdateFrame
8. Update Game.cs to instantiate NetworkCoordinator in OnLoad
9. Update Game.cs to call networkCoordinator.Initialize() ONCE after GameEngine connection
10. Remove WorldReceived event registration from Game.cs OnUpdateFrame (lines 195-204)
11. Remove _worldInitialized flag from Game.cs (now owned by NetworkCoordinator)

**Completion Criteria:**
- NetworkCoordinator.cs exists with network management logic
- WorldReceived event handler registered ONCE (not in update loop)
- HandleWorldReceived() called when world data received from server
- World initialization (spawn position, camera position) happens once
- _worldInitialized flag properly prevents duplicate initialization
- Game.cs no longer has network event registration in OnUpdateFrame
- World loading and chunk streaming work correctly
- No duplicate event subscriptions (verify with logging)
- Build: 0 warnings, 0 errors

**Files to Create:**
- `TerraNova.Client/NetworkCoordinator.cs` (~120 lines)

**Files to Modify:**
- `TerraNova.Client/Game.cs` (remove network event logic, add NetworkCoordinator usage)

**Notes:**
- This task fixes a CRITICAL architectural flaw discovered by architect
- NetworkCoordinator depends on ILogger (add to constructor)
- Event handlers should be registered in Initialize(), not Update()
- This is the most important task in Stage 2B.0 Extended
- Future enhancements: connection retry logic, disconnect handling, etc.

### Task 2B.0.8: Create ClientApplication Orchestrator
**Status:** Blocked (requires Task 2B.0.7)
**Priority:** HIGH
**Estimated Time:** 90 minutes
**Complexity:** HIGH

**Description:** Create ClientApplication as main orchestrator and refactor Game.cs to thin adapter.

**Implementation Steps:**
1. Create `TerraNova.Client/ClientApplication.cs`
2. Add fields: Camera, PlayerController, Renderer, GameEngine, NetworkCoordinator, UIManager, WindowStateManager
3. Implement Initialize() method:
   - Create Camera
   - Create Renderer (with ILogger)
   - Create GameEngine and connect to server
   - Create NetworkCoordinator and initialize
   - Create PlayerController
   - Create UIManager and initialize
   - Create WindowStateManager
4. Implement Update(KeyboardState keyboard, MouseState mouse, float deltaTime) method:
   - Call windowStateManager.UpdateFPS(deltaTime)
   - Call playerController.Update(keyboard, mouse, deltaTime)
   - Call gameEngine.Update()
5. Implement Render(int width, int height) method:
   - Call renderer.Render(...)
   - Call uiManager.Render(...)
6. Implement OnResize(int width, int height) method:
   - Update camera aspect ratio
   - Update renderer viewport
7. Implement OnMouseMove(float deltaX, float deltaY) method:
   - Delegate to playerController.HandleMouseLook(deltaX, deltaY)
8. Refactor Game.cs to use ClientApplication:
   - Replace all game system fields with single _clientApplication field
   - OnLoad: Create and initialize ClientApplication
   - OnUpdateFrame: Call _clientApplication.Update() (14 lines down from 130)
   - OnRenderFrame: Call _clientApplication.Render() (9 lines down from 36)
   - OnResize: Call _clientApplication.OnResize()
   - OnMouseMove: Call _clientApplication.OnMouseMove()
9. Verify Game.cs is now ~110 lines (thin adapter)
10. Test all functionality end-to-end

**Completion Criteria:**
- ClientApplication.cs exists as main orchestrator (~200 lines)
- Game.cs reduced from 335 lines to approximately 110 lines
- Game.cs OnUpdateFrame reduced from 130 lines to ~14 lines
- Game.cs OnRenderFrame reduced from 36 lines to ~9 lines
- Game.cs is now a thin OpenTK adapter (single responsibility)
- All functionality works identically to before refactoring:
  - Movement (WASD/Space/Shift)
  - Mouse look
  - Hotbar selection (keys 1-9)
  - Block breaking (left click)
  - Block placement (right click)
  - Block highlighting (targeted block outline)
  - FPS display in window title
  - Chunk loading/streaming
- Build: 0 warnings, 0 errors

**Files to Create:**
- `TerraNova.Client/ClientApplication.cs` (~200 lines)

**Files to Modify:**
- `TerraNova.Client/Game.cs` (major refactoring to thin adapter)

**Notes:**
- This is the largest and most complex task in Stage 2B.0 Extended
- ClientApplication is the "heart" of the game - orchestrates everything
- Game.cs becomes simple OpenTK window management
- Take time to test thoroughly - this touches every system
- Consider committing after this task (major architectural milestone)

### Task 2B.0.9: Update Dependency Injection Container
**Status:** Blocked (requires Task 2B.0.8)
**Priority:** HIGH
**Estimated Time:** 15 minutes
**Complexity:** SIMPLE

**Description:** Register new coordinator services in Program.cs DI container.

**Implementation Steps:**
1. Open `TerraNova.Client/Program.cs`
2. Register ClientApplication as singleton (if using DI for Game.cs)
3. Alternatively: Update Game.cs constructor to manually instantiate ClientApplication
4. Verify application launches correctly
5. Test all functionality end-to-end
6. Commit changes with descriptive message

**Completion Criteria:**
- Program.cs updated with new service registrations (if using DI)
- Game.cs constructor receives ClientApplication (or instantiates it)
- Application launches without errors
- All functionality verified working:
  - Window opens and displays game world
  - Movement, mouse look, hotbar, block interaction all work
  - FPS display updates correctly
  - Network connection and chunk streaming work
  - No console errors or exceptions
- Build: 0 warnings, 0 errors

**Files to Modify:**
- `TerraNova.Client/Program.cs` (potentially, depends on DI usage)
- `TerraNova.Client/Game.cs` (constructor, if needed)

**Notes:**
- This is a small cleanup/integration task
- Verifies the entire refactoring works end-to-end
- Good opportunity for final testing before Stage 2B
- Consider this a "phase transition" commit

### Stage 2B.0 Extended Success Criteria

Before proceeding to Stage 2B (Physics), verify:
- Build: 0 warnings, 0 errors
- **Code metrics:**
  - Game.cs reduced from 335 lines to approximately 110 lines
  - Game.cs OnUpdateFrame: 130 lines → ~14 lines
  - Game.cs OnRenderFrame: 36 lines → ~9 lines
  - 4 new coordinator classes created (~460 lines total, well-organized)
- **Architecture:**
  - Game.cs is thin OpenTK adapter (single responsibility)
  - ClientApplication orchestrates all game systems
  - NetworkCoordinator fixes event registration flaw (no longer in update loop)
  - UIManager centralizes UI overlay management
  - WindowStateManager handles FPS and window state
- **Functionality (all working identically to before):**
  - Movement (WASD/Space/Shift)
  - Mouse look
  - Hotbar selection (keys 1-9, visual feedback)
  - Block breaking (left click)
  - Block placement (right click)
  - Block highlighting (targeted block outline)
  - FPS display in window title
  - Network connection and world loading
  - Chunk loading/streaming
- **Testing:**
  - No console errors or exceptions
  - No memory leaks from duplicate event subscriptions
  - All existing functionality preserved
  - Clean foundation ready for physics integration

### Why Refactor Game.cs Now? (Decision Rationale)

**Option 1: Refactor Before Physics** (CHOSEN)
- Time investment: 4 hours now
- Result: Clean architecture, physics integrates smoothly
- Physics implementation: 3-4 hours (as estimated)
- Total: 7-8 hours

**Option 2: Physics First, Refactor Later**
- Physics implementation: 3-4 hours (messy, bloats Game.cs to 500+ lines)
- Result: Technical debt, "God Object" anti-pattern worsens
- Refactoring: 6-8 hours (painful, risk breaking physics)
- Total: 9-12 hours + high risk

**Decision Factors:**
1. **Time Savings:** Refactoring now saves 2-4 hours vs. refactoring later
2. **Risk Reduction:** Testing without physics complexity is much easier
3. **Clean Integration:** Physics becomes clean coordinator integration, not messy addition to god object
4. **Professional Architecture:** Application Coordinator Pattern is industry standard
5. **Future Features:** Every new feature benefits from clean architecture (saves time forever)
6. **No Downside:** All functionality preserved, just better organized

**Architect's Recommendation:** "This refactoring will save significant time and pain in Stage 2B and beyond. Do it now."

---

## Stage 2B: Physics Implementation (BLOCKED - Requires Stage 2B.0 Extended Complete)

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

**Architecture Decision:** Application Coordinator Pattern - STRONGLY RECOMMENDED

**Start with:** Stage 2A (PlayerController Extraction) - DO NOT skip to physics!

**Suggested Approach:**

### Stage 2A (1-2 hours):
1. Complete Task 2A.1 (Move Hotbar Selection) - 20 min
2. Complete Task 2A.2 (Move Block Interaction) - 30 min
3. Complete Task 2A.3 (Move Raycast Caching) - 15 min
4. Complete Task 2A.4 (Update Game.cs Delegation) - 20 min
5. Complete Task 2A.5 (Update Hotbar UI) - 10 min
6. Verify Stage 2A Success Criteria before proceeding

### Stage 2B.0 (25-30 minutes): Input Routing - CRITICAL
7. Complete Task 2B.0.1 (Move Camera Movement Input) - 15 min
8. Complete Task 2B.0.2 (Implement Update() Orchestration) - 10 min
9. Complete Task 2B.0.3 (Move Mouse Look) - 5 min (OPTIONAL)
10. Verify Stage 2B.0 Success Criteria before proceeding

### Stage 2B.0 Extended (4-4.5 hours): Application Coordinator Pattern - CRITICAL REFACTORING
11. Complete Task 2B.0.5 (Extract WindowStateManager) - 30 min
12. Complete Task 2B.0.6 (Extract UIManager) - 45 min
13. Complete Task 2B.0.7 (Extract NetworkCoordinator) - 60 min (FIXES CRITICAL BUG)
14. Complete Task 2B.0.8 (Create ClientApplication) - 90 min (MAJOR REFACTORING)
15. Complete Task 2B.0.9 (Update DI Container) - 15 min
16. Verify Stage 2B.0 Extended Success Criteria before proceeding

### Stage 2B (3-4 hours): Physics Implementation
17. Complete Task 2B.1 (Jitter Physics 2 Integration) - 30-45 min
18. Complete Task 2B.2 (Implement Player Physics Body) - 45-60 min
19. Complete Task 2B.3 (Add Terrain Collision) - 60-90 min (most complex)
20. Complete Task 2B.4 (Implement Gravity and Jumping) - 30-45 min
21. Verify Stage 2B Success Criteria
22. (Optional) Fix distance metric mismatch if time permits

**Why This Order Matters:**
- Stage 2A establishes clean architecture BEFORE adding complexity
- Stage 2B.0 fixes input routing architectural gaps
- **Stage 2B.0 Extended eliminates "God Object" anti-pattern BEFORE physics**
- Game.cs refactoring prevents 500+ line monster class
- NetworkCoordinator fixes CRITICAL event registration bug (handlers in update loop)
- Testing is easier without physics complexity
- Physics integrates cleanly into ClientApplication coordinator
- Refactoring now: 4 hours. Refactoring after physics: 8+ hours + high risk
- Application Coordinator Pattern is industry standard game architecture
- Maintains Phase 1 principles throughout development

**Expected Challenges:**
- Task 2A.4 requires careful attention to ensure all logic is properly extracted
- Task 2B.0.8 (ClientApplication) is the most complex refactoring - test thoroughly
- Task 2B.3 (terrain collision) will likely need iteration to handle edge cases
- Physics tuning (Task 2B.4) is subjective - user testing is essential
- Jitter2 documentation may be limited - expect some trial and error

**Success Metrics for Phase 2:**
- Stage 2A: Game.cs reduced to ~300 lines, all player logic in PlayerController
- Stage 2B.0: All input routed through PlayerController, proper Update() orchestration
- **Stage 2B.0 Extended: Game.cs reduced to ~110 lines, 4 coordinator classes created**
- **Stage 2B.0 Extended: Network event bug fixed (no more handlers in update loop)**
- Stage 2B: Physics-based movement feels natural and responsive
- No clipping through terrain or falling through floor
- Jump height and gravity feel similar to Minecraft
- Performance remains smooth (60 FPS) with physics enabled
- Clean architecture maintained throughout (no god objects)
- Professional game engine architecture following industry standards
