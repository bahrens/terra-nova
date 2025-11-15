# Terra Nova Physics System

## Overview

Custom AABB (Axis-Aligned Bounding Box) voxel collision system optimized for Minecraft-style games.

## Key Features

- **Custom collision detection**: Direct world queries instead of physics engine rigid bodies
- **Performance**: ~50-200x faster than rigid body approach (queries 10-30 voxels vs 20K+ rigid bodies per frame)
- **Fixed timestep**: 60 Hz deterministic physics
- **Auto-jump**: Smooth automatic climbing of ledges up to 0.5m (uses same physics as manual jump)
- **Player dimensions**: 0.6m × 1.8m (width × height), eye height 1.62m

## Movement

- **Speed**: 5.0 m/s
- **Jump**: 5.0 m/s initial velocity, reaches ~1.25m height
- **Gravity**: -20.0 m/s² (arcade-style, faster than Earth's -9.81)
- **Jump animation**: 0.3s smooth ease-in cubic acceleration

## Implementation

- `VoxelCollisionSystem`: Swept AABB collision detection (Y→X→Z axis order)
- `VoxelPhysicsWorld`: Fixed timestep physics loop with accumulator
- `VoxelPhysicsBody`: Physics body with smooth jump state
