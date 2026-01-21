# Fantasy Kingdom Scene Setup Guide

## Overview

This guide explains how to set up the new **Fantasy Kingdom** scene - a beautiful standalone fantasy world that doesn't rely on GPS/Mapbox satellite tiles. Instead, it generates a cohesive fantasy kingdom at proper 1:1 scale.

## Quick Setup (5 minutes)

### Step 1: Create the Scene

1. In Unity: **File → New Scene → Basic (URP)**
2. Save as: `Assets/Scenes/FantasyKingdom.unity`

### Step 2: Delete Default Objects

1. Delete the default `Main Camera` (we'll create our own)
2. Keep the `Directional Light` (rename to "Sun")

### Step 3: Create the Kingdom Manager

1. Create empty GameObject: **GameObject → Create Empty**
2. Rename to `FantasyKingdomManager`
3. Add these components:
   - `StandaloneFantasyGenerator`
   - `FantasyKingdomController`
   - `FantasyKingdomUISetup` (auto-creates UI)

### Step 4: Configure the Generator

On `StandaloneFantasyGenerator`:

1. **Config**: Create new or use existing
   - Right-click in Project: **Create → Apex Citadels → Fantasy Kingdom Config**
   - Drag to the Config slot
   
2. **Prefab Library**: 
   - Drag `MainFantasyPrefabLibrary` from `Resources/` folder

### Step 5: Create the Player

1. Create empty GameObject: `Player`
2. Position at `(0, 1, 50)` - outside castle, facing center
3. Add components:
   - `Character Controller` (height: 1.8, radius: 0.3)
   - `FantasyPlayerController`
4. Create child: `Main Camera`
   - Position at `(0, 1.6, 0)` (eye level)
   - Tag as `MainCamera`

### Step 6: Configure Lighting

Select the `Directional Light`:
- Rotation: `(50, -30, 0)` for nice afternoon lighting
- Color: Warm white `(255, 244, 214)`
- Intensity: 1.2

Add ambient:
- **Window → Rendering → Lighting**
- Environment Lighting: Gradient
- Sky: `(135, 180, 255)` (light blue)
- Equator: `(200, 200, 180)` (warm)
- Ground: `(70, 50, 40)` (brown)

### Step 7: Add Post Processing (Optional but Recommended)

1. Create empty: `PostProcessing`
2. Add `Volume` component
3. Add these overrides:
   - **Bloom**: Intensity 0.3
   - **Color Adjustments**: Saturation +10
   - **Vignette**: Intensity 0.2

### Step 8: Play!

Press Play - the kingdom will generate automatically!

---

## Scene Hierarchy

```
FantasyKingdom (Scene)
├── FantasyKingdomManager
│   ├── StandaloneFantasyGenerator
│   ├── FantasyKingdomController
│   └── FantasyKingdomUISetup
│       ├── LoadingCanvas
│       └── GameplayCanvas
├── Player
│   ├── FantasyPlayerController
│   └── Main Camera
├── Sun (Directional Light)
├── PostProcessing (Volume)
└── [Generated at Runtime]
    ├── Terrain/
    ├── Buildings/
    ├── Roads/
    ├── Vegetation/
    ├── Props/
    └── Walls/
```

---

## Configuration Options

### FantasyKingdomConfig (ScriptableObject)

| Setting | Default | Description |
|---------|---------|-------------|
| **Kingdom Size** | 500m | Total world size (square) |
| **Town Radius** | 150m | Size of the walled town |
| **Generate Hills** | true | Add gentle terrain variation |
| **Hill Height** | 15m | Maximum hill elevation |
| **Generate Castle** | true | Central castle/keep |
| **Generate Walls** | true | Town fortification walls |
| **Wall Radius** | 120m | Distance from center to walls |
| **Residential Count** | 40 | Number of houses |
| **Commercial Count** | 15 | Shops, taverns, etc. |
| **Tree Count** | 200 | Forest density |
| **Torch Count** | 30 | Street lighting |

### Recommended Presets

**Small Village** (fast generation):
```
Kingdom Size: 300
Town Radius: 100
Residential: 20
Commercial: 8
Tree Count: 100
```

**Large Kingdom** (impressive but slower):
```
Kingdom Size: 800
Town Radius: 250
Residential: 80
Commercial: 30
Tree Count: 400
```

---

## Player Controls

| Key | Action |
|-----|--------|
| WASD | Move |
| Mouse | Look around |
| Shift | Run |
| Space | Jump |
| V | Toggle 1st/3rd person |
| Escape | Toggle cursor |

---

## Troubleshooting

### "No prefabs found" warnings
- Ensure `MainFantasyPrefabLibrary` is in `Resources/` folder
- Check that Synty prefabs are properly assigned in the library

### Black/pink materials
- Make sure you're using URP
- Upgrade materials: **Edit → Rendering → Materials → Convert...**

### Performance issues
- Reduce `Objects Per Frame` in config (default: 10)
- Lower tree/prop counts
- Enable `Use LOD` option

### Player falls through ground
- Ensure "Ground" layer exists
- Set ground collision mesh to "Ground" layer
- Check CharacterController height

---

## Comparison: Old vs New

| Aspect | Old (Mapbox) | New (Fantasy Kingdom) |
|--------|--------------|----------------------|
| Scale | 0.042 (tiny!) | 1.0 (proper) |
| Ground | Satellite tiles | Stylized terrain |
| Generation | ~80 seconds | ~5-10 seconds |
| Dependencies | GPS, Internet, API key | None |
| Art Style | Mismatched | Cohesive fantasy |
| File Size | Large tile cache | Minimal |

---

## Next Steps

1. **Add more prefab variety** - populate the FantasyPrefabLibrary with more Synty assets
2. **Custom terrain textures** - create or import stylized grass/dirt materials
3. **Day/night cycle** - add atmospheric lighting changes
4. **NPCs** - populate the kingdom with villagers
5. **Quests** - add interaction points and objectives

---

## Files Created

| File | Purpose |
|------|---------|
| `StandaloneFantasyGenerator.cs` | Main world generation |
| `FantasyKingdomController.cs` | Scene management |
| `FantasyPlayerController.cs` | First/third person movement |
| `FantasyKingdomUISetup.cs` | Auto-creates loading UI |
| `FantasyKingdomConfig.cs` | Configuration ScriptableObject |

All files are in: `Assets/Scripts/FantasyWorld/`
