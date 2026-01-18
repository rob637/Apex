# PC Scene Setup Checklist

This document provides a systematic checklist for setting up the PCMain scene correctly.

## ⚡ ONE-CLICK SETUP (Recommended)

**This is all you need to do:**

1. Open Unity
2. Go to menu: **Window → Apex Citadels → One-Click Setup (Recommended)**
3. Click **"Yes, Set Up Everything"**
4. Done! Press Play to test.

That's it. The script automatically:
- Creates the PCMain.unity scene
- Sets up the camera with correct position and settings
- Creates a Directional Light for proper lighting
- Configures ambient/render settings
- Creates all required manager objects
- Sets up the UI Canvas
- Wires up all references
- Saves the scene to Build Settings

---

## Quick Start (Alternative)

1. Open Unity
2. Open scene: `Assets/Scenes/PCMain.unity` (or create it)
3. Go to menu: **Window → Apex Citadels → Scene Diagnostic**
4. Click **"Fix All Issues"** button
5. **Save the scene** (Ctrl+S)

---

## Manual Setup (if needed)

### 1. ✅ CAMERA SETUP

**What you need:** A camera tagged "MainCamera" with proper settings.

**In Scene Hierarchy:**
- `Main Camera` (GameObject with Camera component)
  - Tag: `MainCamera`
  - Clear Flags: `Solid Color`
  - Background: Bright blue `#80B3FF` (RGB: 128, 179, 255)
  - Far Clip Plane: `5000`
  - Has `PCCameraController` script attached
  - Position: `(0, 200, -100)`
  - Rotation: `(70, 0, 0)` - angled down at 70°

**How to fix manually:**
1. Select your camera in Hierarchy
2. In Inspector:
   - Change Tag to `MainCamera`
   - Set Clear Flags to `Solid Color`
   - Click Background color, set to sky blue
   - Set Far Clip Plane to `5000`
3. Add Component → `PCCameraController`
4. Set Transform Position to `(0, 200, -100)`
5. Set Transform Rotation to `(70, 0, 0)`

### 2. ✅ LIGHTING SETUP (CRITICAL!)

**What you need:** A Directional Light to illuminate objects.

> ⚠️ **This is usually the #1 reason colors don't show!** Without a light, everything appears dark/black.

**In Scene Hierarchy:**
- `Directional Light` (or `Sun`) (GameObject with Light component)
  - Type: `Directional`
  - Color: Warm white `#FFF2D9` (RGB: 255, 242, 217)
  - Intensity: `1.0` or higher
  - Shadow Type: `Soft Shadows`
  - Rotation: `(50, -30, 0)`
  - Has `DayNightCycle` script attached (optional)

**How to create manually:**
1. GameObject → Light → Directional Light
2. Rename to "Sun"
3. In Inspector:
   - Color: Click the color, set to warm white
   - Intensity: `1.0`
   - Shadow Type: `Soft Shadows`
4. Set Rotation to `(50, -30, 0)`
5. (Optional) Add Component → `DayNightCycle`

### 3. ✅ AMBIENT LIGHTING

**In menu:** Edit → Render Settings (or Window → Rendering → Lighting)

Set these values:
- **Environment Lighting**
  - Source: `Gradient` or `Color`
  - Ambient Mode: `Trilight`
  - Sky Color: `#99B3E6` (light blue)
  - Equator Color: `#808080` (gray)
  - Ground Color: `#4D594D` (dark green-gray)
  - Intensity Multiplier: `1.0`

> If using URP, these settings are in **Lighting Settings** asset.

### 4. ✅ MANAGERS / SCRIPTS

**Required GameObjects in scene:**

```
Hierarchy:
├── Main Camera (Camera + PCCameraController)
├── Directional Light (Light + DayNightCycle)
├── PCSceneBootstrapper (PCSceneBootstrapper) ← Optional, auto-creates others
├── PCGameController (PCGameController)
├── PCInputManager (PCInputManager)
├── WorldMapRenderer (WorldMapRenderer)
├── EventSystem (EventSystem)
└── Canvas (Canvas + PCUIManager)
```

**Easiest approach:** Just have `PCSceneBootstrapper`:
1. Create empty GameObject, name it "SceneBootstrapper"
2. Add Component → `PCSceneBootstrapper`
3. In Inspector, check that `Auto Setup` is enabled
4. It will create missing objects at runtime

### 5. ✅ URP (Universal Render Pipeline)

**Check:** Edit → Project Settings → Graphics
- Should show a `UniversalRenderPipelineAsset`

**For materials to show colors in URP:**
- Use shader: `Universal Render Pipeline/Lit` or `Universal Render Pipeline/Simple Lit`
- The color property is `_BaseColor` (not `_Color`)

---

## Troubleshooting

### Problem: Everything is gray/black/unlit
**Cause:** No Directional Light in scene
**Fix:** Add a Directional Light with Intensity ≥ 1.0

### Problem: Sky is black or wrong color
**Cause:** Camera Clear Flags not set to Solid Color
**Fix:** Select camera, set Clear Flags to `Solid Color`, set Background to blue

### Problem: Ground plane doesn't show color
**Cause:** URP shader not being used, or no lighting
**Fix:** 
1. Ensure you have a Directional Light
2. Check that material uses URP shader

### Problem: Can't see anything
**Cause:** Camera position/rotation wrong
**Fix:** Position at `(0, 200, -100)`, Rotation at `(70, 0, 0)`

### Problem: Territories are all gray
**Cause:** Materials using wrong shader for URP
**Fix:** Will be addressed in WorldMapRenderer update

---

## Verification

After setup, when you press Play:
1. ✅ You should see a **blue sky** (solid color background)
2. ✅ You should see a **green ground plane** (if lighting works)
3. ✅ You should see **colorful territory markers** (if Firebase loads)
4. ✅ You should be able to **pan with WASD** and **zoom with scroll**
5. ✅ Console should show "Loaded X territories from Firebase"

---

## Scene Hierarchy Reference

```
PCMain Scene
├── SceneBootstrapper
│   └── PCSceneBootstrapper (script)
│
├── Main Camera
│   ├── Camera (component)
│   │   ├── Clear Flags: Solid Color
│   │   ├── Background: #80B3FF
│   │   └── Far Clip Plane: 5000
│   ├── AudioListener (component)
│   └── PCCameraController (script)
│
├── Sun (Directional Light)
│   ├── Light (component)
│   │   ├── Type: Directional
│   │   ├── Intensity: 1.0
│   │   └── Shadows: Soft
│   └── DayNightCycle (script)
│
├── PCGameController
│   └── PCGameController (script)
│
├── PCInputManager
│   └── PCInputManager (script)
│
├── WorldMapRenderer
│   └── WorldMapRenderer (script)
│
├── EventSystem
│   └── EventSystem (component)
│   └── StandaloneInputModule (component)
│
└── Canvas
    └── PCUIManager (script)
```
