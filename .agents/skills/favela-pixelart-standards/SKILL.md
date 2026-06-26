---
name: favela-pixelart-standards
description: Enforces the asset import settings for all Pixel Art in Favela Amarela. Use when dealing with new graphical assets, textures, and sprites to ensure visual fidelity without blurring.
---

# Favela Amarela - Pixel Art Import Standards

## Core Asset Pipeline Rules

Every `Texture2D` or Sprite imported into the project MUST adhere to the following strict settings to prevent blurring and artifacts:

1. **Pixels Per Unit (PPU)**:
   - Must be exactly `16`. (Adjust only if explicitly requested for specific UI/large assets).
2. **Filter Mode**:
   - Must be set to `Point (no filter)`. Never use Bilinear or Trilinear for pixel art.
3. **Compression**:
   - Must be set to `None`. Compression ruins pixel art edges and color pallets.
4. **Max Size**:
   - Keep reasonable bounds, but Compression is the main culprit for artifacts.

## Enforcement
Whenever you add a new `.png`, `.jpg`, or generate an image using AI tools and import it into Unity, YOU MUST ensure the `.meta` file or the Unity Texture Importer assigns these precise settings.
