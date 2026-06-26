---
name: favela-isometric-standards
description: Enforces the strict 2D isometric grid rules for Favela Amarela rendering and physics. Use when creating or modifying level layout, prefabs, physics colliders or camera settings.
---

# Favela Amarela - Isometric Standards

## Core Mandates

1. **Gravity is Void**:
   - `Rigidbody2D.gravityScale` MUST ALWAYS be `0.0`. The isometric perspective handles depth artificially. If gravity is applied, the game's line of sight and movement systems will completely break.

2. **Camera Projection**:
   - Camera must be Orthographic.
   - The rotation on the X-axis must be precisely `26.57°` to achieve a true isometric projection (arctangent of 0.5).

3. **Tilemap & Cell Size**:
   - The global grid `cellSize` must be exactly `(1.0, 0.5, 1.0)`.

4. **Sprite Settings (Pixels Per Unit)**:
   - All Sprites must have a consistent Pixels Per Unit (PPU). The standard for this project is `16`.

5. **Sorting (Y-Sorting)**:
   - Sprites must be sorted correctly based on their Y position on the screen to maintain depth. Ensure Transparency Sort Mode is set to `Custom Axis` in Project Settings (usually X=0, Y=1, Z=0).

## Audit Enforcement
Any time you modify a Room prefab, the Camera setup, or Enemy/Player rigidbodies, YOU MUST verify these constraints. If a Rigidbody2D has a gravity scale other than `0.0`, you must immediately fix it.
