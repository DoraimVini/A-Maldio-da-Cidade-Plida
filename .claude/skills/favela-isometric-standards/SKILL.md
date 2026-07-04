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
   - Rotation MUST stay `Quaternion.identity` (no tilt on any axis). This project does NOT use a physically-tilted 3D dimetric camera — `LevelBlockoutGenerator` places walls/floors as flat `SpriteRenderer`/`BoxCollider2D` on the XY plane and fakes depth purely via Y-sorting (`sortingOrder = -worldCenter.y * 10`). The "isometric feel" comes from `PlayerMovement.ConvertToIsometric` remapping input direction, not from camera rotation. Tilting the camera (e.g. 26.57° on X, the classic 3D-dimetric-diorama trick) breaks the Y-sort depth illusion and visually desyncs colliders from sprites — do not reintroduce it. (`PrefabMigrationTool.cs`'s "cenário A" / `Quaternion.identity` is the correct reference implementation; `PlaytestSceneSetup.cs` previously diverged from it and was fixed to match.)

3. **Tilemap & Cell Size**:
   - No `UnityEngine.Grid`/`Tilemap` component is currently used in code — level geometry is generated procedurally as plain XY `Vector2` positions (`LevelBlockoutPlanner` + `LevelBlockoutGenerator`). If a Grid/Tilemap is introduced later, keep `cellSize` at `(1.0, 0.5, 1.0)`, but don't assume one exists today.

4. **Sprite Settings (Pixels Per Unit)**:
   - All Sprites must have a consistent Pixels Per Unit (PPU). The standard for this project is `16`.

5. **Sorting (Y-Sorting)**:
   - Sprites must be sorted correctly based on their Y position on the screen to maintain depth. Ensure Transparency Sort Mode is set to `Custom Axis` in Project Settings (usually X=0, Y=1, Z=0).

## Audit Enforcement
Any time you modify a Room prefab, the Camera setup, or Enemy/Player rigidbodies, YOU MUST verify these constraints. If a Rigidbody2D has a gravity scale other than `0.0`, you must immediately fix it.
