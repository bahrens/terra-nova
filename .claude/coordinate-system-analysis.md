# Coordinate System Analysis - Visual Mismatch Investigation

## Executive Summary

**CONFIRMED ROOT CAUSE: Coordinate system mismatch between rendering and physics.**

- **Rendering**: Uses CENTER-ANCHORED coordinates (block N centered at position N)
- **Physics**: Uses CORNER-ANCHORED coordinates (block N occupies [N, N+1))
- **Raycasting**: Uses Math.Round, which works for CENTER-ANCHORED blocks

This creates a 0.5-unit offset where physics and rendering disagree on block boundaries.

---

## Confirmed Test Data

### Broken Block
- Block coordinates: (2, 39, 35)

### Player Position When Falling Through
- Feet position: (2.570, 39.001, 35.462)
- In X/Z plane: (2.570, 35.462)

### Expected Center (Physics Calculation)
- Physics assumes block (2, 39, 35) occupies:
  - X: [2.0, 3.0)
  - Y: [39.0, 40.0)
  - Z: [35.0, 36.0)
- Center: (2.5, 39.5, 35.5)

### Test Result
User position (2.570, 35.462) is very close to physics center (2.5, 35.5), confirming:
1. Physics works correctly - user DOES fall through
2. Visual alignment is wrong - user says they don't "look" centered

---

## System-by-System Analysis

### 1. Rendering System (CENTER-ANCHORED)

**File**: `TerraNova.Core\ChunkMeshBuilder.cs` lines 206-210

```csharp
// Front face (+Z)
vertexCount = AddFace(
    new float[] {
        posX - 0.5f, posY - 0.5f, posZ + 0.5f,  // Block extends ±0.5 from center
        posX + 0.5f, posY - 0.5f, posZ + 0.5f,
        posX + 0.5f, posY + 0.5f, posZ + 0.5f,
        posX - 0.5f, posY + 0.5f, posZ + 0.5f,
    },
    ...
);
```

**Interpretation**:
- Block at position (2, 39, 35) is rendered with vertices:
  - Min: (2 - 0.5, 39 - 0.5, 35 - 0.5) = (1.5, 38.5, 34.5)
  - Max: (2 + 0.5, 39 + 0.5, 35 + 0.5) = (2.5, 39.5, 35.5)
- **Rendering treats block coordinates as CENTER points**
- Block N visually occupies [N-0.5, N+0.5]

### 2. Physics System (CORNER-ANCHORED)

**File**: `TerraNova.Client\Physics\VoxelCollisionSystem.cs` lines 336-340

```csharp
// Create AABB for this voxel (1x1x1 block)
AABB voxelAABB = new AABB(
    new Vector3(x, y, z),        // Min corner at block coordinates
    new Vector3(x + 1, y + 1, z + 1)  // Max corner = min + 1
);
```

**Interpretation**:
- Block at position (2, 39, 35) has collision AABB:
  - Min: (2.0, 39.0, 35.0)
  - Max: (3.0, 40.0, 36.0)
- **Physics treats block coordinates as BOTTOM-LEFT-BACK CORNER**
- Block N occupies [N, N+1)

### 3. Raycasting System (CENTER-ANCHORED)

**File**: `TerraNova.Core\Raycaster.cs` lines 34-36

```csharp
// Convert to block coordinates
int x = (int)Math.Round(currentPosition.X, MidpointRounding.AwayFromZero);
int y = (int)Math.Round(currentPosition.Y, MidpointRounding.AwayFromZero);
int z = (int)Math.Round(currentPosition.Z, MidpointRounding.AwayFromZero);
```

**File**: `TerraNova.Core\Raycaster.cs` lines 67-71

```csharp
private static (float Distance, BlockFace Face)? IntersectAABB(Vector3 rayOrigin, Vector3 rayDirection, Vector3 blockCenter)
{
    // Block extends from -0.5 to +0.5 around its center
    Vector3 boxMin = blockCenter - new Vector3(0.5f, 0.5f, 0.5f);
    Vector3 boxMax = blockCenter + new Vector3(0.5f, 0.5f, 0.5f);
```

**Interpretation**:
- Math.Round(2.3) = 2, Math.Round(2.7) = 3
- This correctly maps rendered block positions to their CENTER coordinates
- Block N is selected when position is in range [N-0.5, N+0.5)
- **Raycasting assumes CENTER-ANCHORED blocks** (matches rendering)

---

## Why Raycasting Works Despite Mismatch

**Raycasting works because**:
1. Rendering uses center-anchored: block N at [N-0.5, N+0.5]
2. Raycasting uses Math.Round + center-anchored AABB
3. Both use the SAME coordinate system

**Example**: Looking at rendered block (2, 39, 35)
- Visual bounds: [1.5, 2.5] × [38.5, 39.5] × [34.5, 35.5]
- Raycast hits at position ~(2.1, 39.2, 35.3)
- Math.Round(2.1, 39.2, 35.3) = (2, 39, 35) ✓ Correct!
- AABB test uses block center (2, 39, 35) ± 0.5 ✓ Matches visual!

---

## Why Physics Doesn't Match Visuals

**Physics uses different coordinate system**:
- Block (2, 39, 35) visually occupies: [1.5, 2.5] × [38.5, 39.5] × [34.5, 35.5]
- Block (2, 39, 35) physics occupies: [2.0, 3.0] × [39.0, 40.0] × [35.0, 36.0]
- **0.5 unit offset in all directions!**

**What user sees**:
- Block broken at visual position [1.5, 2.5] × [38.5, 39.5] × [34.5, 35.5]
- User expects hole at [1.5, 2.5] × [38.5, 39.5] × [34.5, 35.5]
- Physics creates hole at [2.0, 3.0] × [39.0, 40.0] × [35.0, 36.0]
- To fall through, user must stand at (2.5, 35.5) which is VISUALLY offset

---

## Visual Demonstration

```
RENDERED BLOCKS (center-anchored):
┌─────────┬─────────┬─────────┐
│         │         │         │
│  (1,35) │  (2,35) │  (3,35) │  ← Block coordinates are centers
│         │    X    │         │     X = broken block
└─────────┴─────────┴─────────┘
 1.5     2.5     2.5 3.5     4.5  ← Actual world coordinates

PHYSICS BLOCKS (corner-anchored):
┌─────────┬─────────┬─────────┐
│         │         │         │
│  [1,2)  │  [2,3)  │  [3,4)  │  ← Block N occupies [N, N+1)
│         │    X    │         │     X = broken block
└─────────┴─────────┴─────────┘
 1.0     2.0     3.0     4.0  ← Block boundaries

MISMATCH:
- Visual hole: X ∈ [1.5, 2.5]
- Physics hole: X ∈ [2.0, 3.0]
- Offset: 0.5 units to the RIGHT

Result: To fall through visual hole, must stand 0.5 units to the right of visual center!
```

---

## Root Cause Confirmation

**Why this happened**:
1. Minecraft-style voxel games traditionally use INTEGER coordinates for block corners
2. Rendering often uses block centers for convenience (symmetric ±0.5 offsets)
3. Physics AABBs naturally use min/max corners
4. These two conventions were never reconciled

**The test confirms**:
- User at (2.570, 35.462) fell through ✓
- This is near physics center (2.5, 35.5) ✓
- But user says it doesn't LOOK centered ✗
- Because visual center is (2.0, 35.0) ✗

---

## Solution Options

### Option A: Change Physics to Match Rendering (CENTER-ANCHORED)

**Modification**: `VoxelCollisionSystem.cs` line 337-340

```csharp
// OLD (corner-anchored):
AABB voxelAABB = new AABB(
    new Vector3(x, y, z),
    new Vector3(x + 1, y + 1, z + 1)
);

// NEW (center-anchored):
AABB voxelAABB = new AABB(
    new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
    new Vector3(x + 0.5f, y + 0.5f, z + 0.5f)
);
```

**Pros**:
- Aligns physics with rendering (majority system)
- Aligns physics with raycasting
- Visual consistency - blocks look where they are
- Minimal code changes (one file, one function)

**Cons**:
- Non-standard for voxel physics (most engines use corner-anchored)
- May cause edge cases with block coordinate calculations
- Requires careful testing of existing physics

**Affected Systems**: Physics only

---

### Option B: Change Rendering to Match Physics (CORNER-ANCHORED)

**Modification**: `ChunkMeshBuilder.cs` all vertex generation

```csharp
// OLD (center-anchored):
posX - 0.5f, posY - 0.5f, posZ + 0.5f,

// NEW (corner-anchored):
posX, posY, posZ + 1.0f,
```

**Pros**:
- Standard voxel engine convention
- Physics remains traditional and predictable
- Easier to reason about block boundaries

**Cons**:
- MASSIVE code changes (every vertex in ChunkMeshBuilder)
- Must also update raycasting (Math.Round → Math.Floor)
- Must update camera position calculations
- Risk of breaking ambient occlusion, lighting, face culling
- Higher risk of introducing bugs

**Affected Systems**: Rendering, raycasting, camera, possibly lighting

---

### Option C: Add 0.5 Offset Layer Between Systems

**Modification**: Add translation layer in physics-rendering interface

```csharp
// When converting physics → rendering:
Vector3 renderPos = physicsPos + new Vector3(0.5f, 0.5f, 0.5f);

// When converting rendering → physics:
Vector3 physicsPos = renderPos - new Vector3(0.5f, 0.5f, 0.5f);
```

**Pros**:
- No changes to core systems
- Can be isolated to interface boundaries
- Safer incremental migration

**Cons**:
- Cognitive overhead (must remember offset everywhere)
- Easy to forget offset in new code
- Doesn't fix the root issue
- Two different coordinate systems to maintain

**Affected Systems**: All boundary conversions

---

## Recommended Solution

**Option A: Change Physics to CENTER-ANCHORED**

### Rationale:
1. **Minimal risk**: One file, one function change
2. **Aligns with majority**: Rendering + raycasting already center-anchored
3. **Visual consistency**: Critical for user experience
4. **Easier to test**: Physics behavior is easiest to validate

### Implementation Plan:

1. **Modify VoxelCollisionSystem.cs**:
   - Update voxel AABB creation (line 337-340)
   - Update voxel coordinate iteration bounds (lines 277-282)

2. **Test thoroughly**:
   - Collision detection accuracy
   - Ground detection
   - Wall sliding
   - Step-up/auto-jump
   - Falling through holes
   - Block placement

3. **Verify alignment**:
   - Break block, confirm visual hole matches physics hole
   - Check all 6 faces of blocks
   - Test at various world coordinates (positive/negative)

### Code Changes Required:

**File**: `VoxelCollisionSystem.cs`

```csharp
// Line 336-340: Change voxel AABB creation
// OLD:
AABB voxelAABB = new AABB(
    new Vector3(x, y, z),
    new Vector3(x + 1, y + 1, z + 1)
);

// NEW:
AABB voxelAABB = new AABB(
    new Vector3(x - 0.5f, y - 0.5f, z - 0.5f),
    new Vector3(x + 0.5f, y + 0.5f, z + 0.5f)
);
```

```csharp
// Lines 277-282: Voxel iteration bounds
// This should remain the same (Math.Floor still correct for finding which blocks to test)
// The change is only in the AABB bounds, not which blocks to check
```

**Note**: The voxel iteration bounds using Math.Floor are actually correct for both systems, because they determine "which integer block coordinates to test", not "what the block bounds are". The AABB bounds change is the only modification needed.

---

## Questions for Architecture Review

1. **Are there other systems that depend on corner-anchored block coordinates?**
   - World generation?
   - Chunk boundaries?
   - Block placement logic?

2. **Does anything rely on block (N, N, N) having its corner at world position (N, N, N)?**

3. **Are there any serialization concerns?**
   - Saved player positions?
   - Entity positions?

4. **Performance implications?**
   - Does center-anchored add floating-point operations in hot paths?

5. **Should we rename coordinate system terminology in code?**
   - Currently: "blockPos", "worldPos" are ambiguous
   - Suggest: "blockCoords" (integer), "worldPos" (float, center-anchored)

---

## Additional Notes

### Why Math.Floor in Iteration is Still Correct

```csharp
// Goal: Find which blocks a sweep AABB touches
sweepAABB.Min = (1.7, 39.2, 35.8)
sweepAABB.Max = (2.3, 39.8, 36.1)

// Math.Floor gives block coordinates to test:
minX = Floor(1.7) = 1  // Block 1 extends to [0.5, 1.5], touches sweep
maxX = Floor(2.3) = 2  // Block 2 extends to [1.5, 2.5], touches sweep
minZ = Floor(35.8) = 35
maxZ = Floor(36.1) = 36

// This is correct for CENTER-ANCHORED blocks!
// Block 1: [0.5, 1.5] overlaps sweep [1.7, 2.3] ✓
// Block 2: [1.5, 2.5] overlaps sweep [1.7, 2.3] ✓
```

The iteration bounds calculation is coordinate-system-agnostic because it's finding "which integer block IDs might be relevant", not "what are the block bounds".

---

## Conclusion

**RECOMMENDATION**: Implement Option A (change physics to center-anchored)

This is the safest, most maintainable solution that provides visual consistency with minimal code changes. The physics system is the minority system (1 file vs. rendering + raycasting), making it the logical choice to conform to the majority convention.

After this change:
- Block N will be centered at position (N, N, N) in all systems
- Block N will occupy [N-0.5, N+0.5] in all systems
- Visual holes will match physics holes exactly
- User experience will be consistent and predictable
