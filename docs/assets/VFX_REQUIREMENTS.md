# Visual Effects (VFX) Asset Requirements

## Overview
This document outlines all required visual effects for Apex Citadels, with implementation guidance for Unity VFX Graph and Particle Systems.

---

# SECTION 1: COMBAT VFX (40 effects)

## 1.1 Weapon Trail Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-CMB01 | Sword Trail Basic | White/silver sword swing arc | 0.5s | Trail Renderer |
| VFX-CMB02 | Sword Trail Fire | Orange flame sword trail | 0.5s | Trail + Particles |
| VFX-CMB03 | Sword Trail Ice | Blue frost sword trail | 0.5s | Trail + Particles |
| VFX-CMB04 | Sword Trail Lightning | Yellow electric sword trail | 0.5s | Trail + Particles |
| VFX-CMB05 | Sword Trail Holy | Golden divine sword trail | 0.5s | Trail + Particles |
| VFX-CMB06 | Sword Trail Dark | Purple shadow sword trail | 0.5s | Trail + Particles |
| VFX-CMB07 | Axe Trail Heavy | Red heavy impact trail | 0.6s | Trail Renderer |
| VFX-CMB08 | Spear Trail Thrust | Sharp thrust trail | 0.4s | Trail Renderer |

## 1.2 Impact Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-CMB09 | Hit Sparks Metal | Metallic sparks on armor hit | 0.3s | Particle Burst |
| VFX-CMB10 | Hit Sparks Weapon | Weapon clash sparks | 0.3s | Particle Burst |
| VFX-CMB11 | Hit Blood Light | Light hit blood splash | 0.3s | Particle Burst |
| VFX-CMB12 | Hit Blood Heavy | Heavy hit blood splash | 0.5s | Particle System |
| VFX-CMB13 | Hit Dust Light | Light dust impact | 0.4s | Particle System |
| VFX-CMB14 | Hit Dust Heavy | Heavy dust cloud | 0.6s | Particle System |
| VFX-CMB15 | Critical Hit Flash | Screen flash + particles | 0.3s | Full Screen + Particles |
| VFX-CMB16 | Block Impact | Shield block sparks | 0.3s | Particle Burst |

## 1.3 Projectile Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-CMB17 | Arrow Trail | Arrow flight trail | Continuous | Trail Renderer |
| VFX-CMB18 | Arrow Fire | Flaming arrow | Continuous | Trail + Particles |
| VFX-CMB19 | Arrow Ice | Frost arrow | Continuous | Trail + Particles |
| VFX-CMB20 | Crossbow Bolt | Heavy bolt trail | Continuous | Trail Renderer |
| VFX-CMB21 | Catapult Boulder | Boulder with debris | Continuous | Particle System |
| VFX-CMB22 | Catapult Fire | Flaming boulder | Continuous | Particle System |
| VFX-CMB23 | Ballista Bolt | Giant bolt trail | Continuous | Trail + Particles |

## 1.4 Magic Combat Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-CMB24 | Fireball Projectile | Fire magic ball | Continuous | VFX Graph |
| VFX-CMB25 | Fireball Explosion | Fire spell impact | 1.0s | VFX Graph |
| VFX-CMB26 | Ice Shard Projectile | Ice magic shard | Continuous | VFX Graph |
| VFX-CMB27 | Ice Shatter | Ice spell impact | 0.8s | VFX Graph |
| VFX-CMB28 | Lightning Bolt | Electric magic | 0.3s | Line Renderer + Particles |
| VFX-CMB29 | Lightning Chain | Chained lightning | 0.5s | Multiple Line Renderers |
| VFX-CMB30 | Heal Burst | Healing spell | 1.0s | VFX Graph |
| VFX-CMB31 | Buff Aura | Buff spell wrap | 2.0s | Particle System |
| VFX-CMB32 | Debuff Chains | Debuff visual | 2.0s | Particle System |
| VFX-CMB33 | Shield Spell | Magic barrier | Continuous | VFX Graph |

## 1.5 Death & Defeat Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-CMB34 | Death Dissolve | Character dissolve | 2.0s | Shader + Particles |
| VFX-CMB35 | Soul Rise | Soul leaving body | 1.5s | Particle System |
| VFX-CMB36 | Body Fall Dust | Dust on body fall | 0.5s | Particle Burst |
| VFX-CMB37 | Respawn Glow | Respawn effect | 1.5s | VFX Graph |
| VFX-CMB38 | Knockout Stars | Stunned stars | Continuous | Particle System |
| VFX-CMB39 | Rage Aura | Low HP rage effect | Continuous | Particle System |
| VFX-CMB40 | Victory Glow | Victory celebration | 2.0s | VFX Graph |

---

# SECTION 2: BUILDING VFX (24 effects)

## 2.1 Construction Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-BLD01 | Placement Ghost | Building preview hologram | Continuous | Shader |
| VFX-BLD02 | Placement Valid | Green valid placement | Continuous | Shader + Particles |
| VFX-BLD03 | Placement Invalid | Red invalid placement | Continuous | Shader + Particles |
| VFX-BLD04 | Construction Sparkle | Building sparkles | Continuous | Particle System |
| VFX-BLD05 | Construction Dust | Construction dust | Continuous | Particle System |
| VFX-BLD06 | Complete Flash | Build complete flash | 0.5s | Full Screen Flash |
| VFX-BLD07 | Upgrade Spiral | Upgrade in progress | Continuous | VFX Graph |
| VFX-BLD08 | Upgrade Complete | Upgrade finish burst | 1.0s | VFX Graph |

## 2.2 Building State Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-BLD09 | Fire Damage | Building on fire | Continuous | VFX Graph |
| VFX-BLD10 | Smoke Damage | Damage smoke | Continuous | Particle System |
| VFX-BLD11 | Crack Sparks | Structural damage | Continuous | Particle Bursts |
| VFX-BLD12 | Collapse Dust | Building collapse | 2.0s | VFX Graph |
| VFX-BLD13 | Debris Fall | Falling debris | 1.5s | Particle System |
| VFX-BLD14 | Rubble Dust | After collapse | 1.0s | Particle System |
| VFX-BLD15 | Repair Sparkle | Repair in progress | Continuous | Particle System |
| VFX-BLD16 | Protected Shield | Protection active | Continuous | Shader + Particles |

## 2.3 Building Function Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-BLD17 | Resource Generate | Resource production | 0.5s | Particle Burst |
| VFX-BLD18 | Forge Fire | Active forge | Continuous | VFX Graph |
| VFX-BLD19 | Magic Tower Glow | Magic building glow | Continuous | Shader + Particles |
| VFX-BLD20 | Portal Swirl | Active portal | Continuous | VFX Graph |
| VFX-BLD21 | Beacon Light | Tower beacon | Continuous | Light + Particles |
| VFX-BLD22 | Mill Dust | Mill operation | Continuous | Particle System |
| VFX-BLD23 | Chimney Smoke | Building chimney | Continuous | Particle System |
| VFX-BLD24 | Water Wheel Splash | Water mill | Continuous | Particle System |

---

# SECTION 3: UI & FEEDBACK VFX (32 effects)

## 3.1 Resource Collection

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-UI01 | Gold Collect | Coins flying to UI | 1.0s | UI Particles |
| VFX-UI02 | Gem Collect | Gems flying to UI | 1.0s | UI Particles |
| VFX-UI03 | XP Collect | XP orbs to bar | 0.8s | UI Particles |
| VFX-UI04 | Resource Pulse | Resource gained pulse | 0.3s | UI Animation |
| VFX-UI05 | Loot Burst | Loot drop burst | 0.5s | World Particles |
| VFX-UI06 | Treasure Shine | Chest open shine | 1.0s | VFX Graph |
| VFX-UI07 | Rare Item Glow | Rare item reveal | 1.5s | VFX Graph |
| VFX-UI08 | Legendary Rays | Legendary reveal | 2.0s | VFX Graph |

## 3.2 Progress & Achievement

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-UI09 | Level Up Burst | Level up explosion | 1.5s | VFX Graph |
| VFX-UI10 | Level Up Pillar | Light pillar | 2.0s | VFX Graph |
| VFX-UI11 | Achievement Pop | Achievement unlock | 1.0s | UI Particles |
| VFX-UI12 | Quest Complete | Quest done sparkle | 1.0s | UI Particles |
| VFX-UI13 | Daily Reward | Daily claim effect | 1.0s | UI Particles |
| VFX-UI14 | Streak Fire | Streak counter flames | Continuous | UI Particles |
| VFX-UI15 | Progress Fill | Bar fill sparkle | 0.5s | UI Particles |
| VFX-UI16 | Rank Up | Rank increase flash | 1.0s | Full Screen + UI |

## 3.3 Notification Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-UI17 | Alert Pulse | Alert icon pulse | 0.5s | UI Animation |
| VFX-UI18 | Warning Flash | Warning screen flash | 0.3s | Full Screen |
| VFX-UI19 | Success Sparkle | Success confirmation | 0.5s | UI Particles |
| VFX-UI20 | Error Shake | Error screen shake | 0.3s | Camera Shake |
| VFX-UI21 | Message Pop | New message pop | 0.3s | UI Animation |
| VFX-UI22 | Friend Online | Friend ping | 0.5s | UI Particles |
| VFX-UI23 | Timer Warning | Timer almost done | Continuous | UI Particles |
| VFX-UI24 | Countdown Flash | Countdown final | 0.5s | Full Screen |

## 3.4 Button & Touch Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-UI25 | Button Press | Press ripple | 0.3s | UI Particles |
| VFX-UI26 | Button Glow | Special button glow | Continuous | UI Shader |
| VFX-UI27 | Touch Ripple | Touch feedback | 0.3s | UI Particles |
| VFX-UI28 | Drag Trail | Drag indicator | Continuous | UI Trail |
| VFX-UI29 | Drop Target | Valid drop highlight | Continuous | UI Shader |
| VFX-UI30 | Tab Switch | Tab change wipe | 0.3s | UI Animation |
| VFX-UI31 | Panel Slide | Panel transition | 0.3s | UI Animation |
| VFX-UI32 | Modal Blur | Background blur | 0.2s | Post Processing |

---

# SECTION 4: ENVIRONMENTAL VFX (36 effects)

## 4.1 Weather Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-ENV01 | Rain Light | Light rain drops | Continuous | Particle System |
| VFX-ENV02 | Rain Heavy | Heavy rain | Continuous | Particle System |
| VFX-ENV03 | Rain Splash | Ground splashes | Continuous | Particle System |
| VFX-ENV04 | Snow Light | Light snowfall | Continuous | Particle System |
| VFX-ENV05 | Snow Heavy | Heavy snow | Continuous | Particle System |
| VFX-ENV06 | Snow Accumulate | Snow buildup | Continuous | Shader |
| VFX-ENV07 | Fog Light | Light fog | Continuous | Volume Fog |
| VFX-ENV08 | Fog Dense | Dense fog | Continuous | Volume Fog |
| VFX-ENV09 | Lightning Flash | Lightning bolt | 0.3s | Light + Post Process |
| VFX-ENV10 | Thunder Shake | Thunder camera shake | 0.5s | Camera Shake |
| VFX-ENV11 | Wind Particles | Visible wind | Continuous | Particle System |
| VFX-ENV12 | Dust Storm | Sandstorm | Continuous | VFX Graph |

## 4.2 Fire & Light Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-ENV13 | Torch Fire | Wall torch | Continuous | VFX Graph |
| VFX-ENV14 | Campfire | Campfire flames | Continuous | VFX Graph |
| VFX-ENV15 | Bonfire Large | Large fire | Continuous | VFX Graph |
| VFX-ENV16 | Fire Spread | Fire spreading | Continuous | VFX Graph |
| VFX-ENV17 | Ember Float | Floating embers | Continuous | Particle System |
| VFX-ENV18 | Candle Flicker | Candle flame | Continuous | Light + Particles |
| VFX-ENV19 | Lantern Glow | Lantern light | Continuous | Light |
| VFX-ENV20 | Magic Glow | Magical light | Continuous | Light + Particles |

## 4.3 Water Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-ENV21 | Water Ripple | Surface ripples | Continuous | Shader |
| VFX-ENV22 | Water Splash Small | Small splash | 0.5s | Particle System |
| VFX-ENV23 | Water Splash Large | Large splash | 0.8s | VFX Graph |
| VFX-ENV24 | Waterfall Spray | Waterfall mist | Continuous | Particle System |
| VFX-ENV25 | River Flow | River current | Continuous | Shader |
| VFX-ENV26 | Puddle Ripple | Rain puddle | Continuous | Shader |

## 4.4 Nature Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-ENV27 | Leaves Falling | Autumn leaves | Continuous | Particle System |
| VFX-ENV28 | Leaves Wind | Wind-blown leaves | Continuous | Particle System |
| VFX-ENV29 | Flower Petals | Cherry blossom | Continuous | Particle System |
| VFX-ENV30 | Pollen Float | Floating pollen | Continuous | Particle System |
| VFX-ENV31 | Fireflies | Night fireflies | Continuous | Particle System |
| VFX-ENV32 | Butterflies | Butterfly flight | Continuous | Particle System |
| VFX-ENV33 | Dust Motes | Dust in light | Continuous | Particle System |
| VFX-ENV34 | Grass Sway | Grass movement | Continuous | Shader |
| VFX-ENV35 | Tree Rustle | Tree movement | Continuous | Shader |
| VFX-ENV36 | Cloud Shadow | Moving cloud shadows | Continuous | Shader |

---

# SECTION 5: SPECIAL EFFECTS (24 effects)

## 5.1 Magical Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-SPL01 | Magic Circle | Summoning circle | 2.0s | VFX Graph |
| VFX-SPL02 | Teleport In | Arrival effect | 1.0s | VFX Graph |
| VFX-SPL03 | Teleport Out | Departure effect | 1.0s | VFX Graph |
| VFX-SPL04 | Portal Open | Portal opening | 1.5s | VFX Graph |
| VFX-SPL05 | Portal Active | Active portal | Continuous | VFX Graph |
| VFX-SPL06 | Enchant Item | Item enchanting | 2.0s | VFX Graph |
| VFX-SPL07 | Curse Effect | Dark curse | 1.5s | VFX Graph |
| VFX-SPL08 | Blessing Light | Divine blessing | 1.5s | VFX Graph |

## 5.2 Power-Up Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-SPL09 | Speed Boost | Speed power-up | 0.5s | Particle System |
| VFX-SPL10 | Strength Boost | Attack power-up | 0.5s | Particle System |
| VFX-SPL11 | Defense Boost | Defense power-up | 0.5s | Particle System |
| VFX-SPL12 | Invincibility | Invincible state | Continuous | Shader + Particles |
| VFX-SPL13 | Rage Mode | Berserk state | Continuous | VFX Graph |
| VFX-SPL14 | Stealth Mode | Invisible state | Continuous | Shader |
| VFX-SPL15 | Charge Up | Power charging | 1.0s | VFX Graph |
| VFX-SPL16 | Release Power | Power release | 0.5s | VFX Graph |

## 5.3 Event Effects

| ID | Effect Name | Description | Duration | Implementation |
|----|-------------|-------------|----------|----------------|
| VFX-SPL17 | Boss Spawn | Boss appearance | 2.0s | VFX Graph |
| VFX-SPL18 | Event Start | Event beginning | 1.5s | VFX Graph |
| VFX-SPL19 | Event Complete | Event finished | 2.0s | VFX Graph |
| VFX-SPL20 | World Change | World transition | 3.0s | Full Screen + VFX |
| VFX-SPL21 | Day Transition | Day to night | 2.0s | Post Processing |
| VFX-SPL22 | Season Change | Season transition | 3.0s | Post Processing + VFX |
| VFX-SPL23 | Fireworks | Celebration fireworks | 5.0s | VFX Graph |
| VFX-SPL24 | Confetti | Victory confetti | 3.0s | Particle System |

---

# SUMMARY: TOTAL VFX ASSETS

| Category | Count |
|----------|-------|
| Combat VFX | 40 |
| Building VFX | 24 |
| UI & Feedback VFX | 32 |
| Environmental VFX | 36 |
| Special Effects | 24 |
| **TOTAL** | **156** |

---

# UNITY IMPLEMENTATION GUIDE

## VFX Graph Setup

### Recommended VFX Graph Templates
```
1. Burst Effect Template (explosions, impacts)
2. Continuous Effect Template (fires, auras)
3. Trail Effect Template (weapon trails, projectiles)
4. Environmental Loop Template (weather, ambient)
```

### VFX Graph Contexts
```
Initialize Particle
├── Set Capacity: 1000 (adjust per effect)
├── Set Bounds: Custom
└── Set Lifetime: 1-5s

Update Particle
├── Turbulence (for organic effects)
├── Conform to Sphere (for auras)
└── Add Velocity (for movement)

Output Particle
├── Lit/Unlit Particle
├── Distortion Output
└── Motion Blur
```

## Particle System Settings

### Burst Effects
```
Start Lifetime: 0.3-0.5
Start Speed: 5-15
Max Particles: 50-200
Emission: Bursts (Count: 20-50)
Shape: Cone/Sphere
```

### Continuous Effects
```
Start Lifetime: 1-3
Start Speed: 1-5
Max Particles: 100-500
Emission: Rate over Time: 20-100
Shape: Cone/Edge
```

### Trail Effects
```
Trail Module: Enabled
Ratio: 0.5-1.0
Lifetime: 0.5-2.0
Width over Trail: Curve (1.0 → 0.0)
Color over Trail: Gradient
```

## Effect Pooling System

```csharp
public class VFXPool : MonoBehaviour
{
    [System.Serializable]
    public class PooledEffect
    {
        public string id;
        public GameObject prefab;
        public int initialSize = 10;
    }
    
    [SerializeField] PooledEffect[] effects;
    Dictionary<string, Queue<GameObject>> pools;
    
    public GameObject Spawn(string id, Vector3 position, Quaternion rotation)
    {
        if (pools[id].Count == 0) ExpandPool(id);
        var effect = pools[id].Dequeue();
        effect.transform.SetPositionAndRotation(position, rotation);
        effect.SetActive(true);
        return effect;
    }
    
    public void Return(string id, GameObject effect)
    {
        effect.SetActive(false);
        pools[id].Enqueue(effect);
    }
}
```

## Mobile Optimization

### Performance Guidelines
```
Max Active Particles: 500-1000 total
VFX Graph: Use Particle Strips instead of Quads
Textures: 128x128 max for particles
Draw Calls: Batch similar effects
Overdraw: Minimize overlapping particles
```

### LOD System for VFX
```csharp
public class VFXLODManager : MonoBehaviour
{
    public enum VFXQuality { Low, Medium, High, Ultra }
    
    [SerializeField] VFXQuality currentQuality;
    
    public void SetQuality(VFXQuality quality)
    {
        currentQuality = quality;
        switch (quality)
        {
            case VFXQuality.Low:
                SetMaxParticles(100);
                DisableVFXGraph();
                break;
            case VFXQuality.Medium:
                SetMaxParticles(300);
                EnableSimpleVFX();
                break;
            case VFXQuality.High:
                SetMaxParticles(600);
                EnableAllVFX();
                break;
            case VFXQuality.Ultra:
                SetMaxParticles(1000);
                EnableAllVFX();
                break;
        }
    }
}
```

## Shader Effects

### Required Shaders
```
1. Hologram Shader (building placement preview)
2. Dissolve Shader (death effects)
3. Distortion Shader (heat/magic)
4. Outline Shader (selection highlights)
5. Fresnel Glow Shader (power-ups, auras)
6. Panning Texture Shader (portals, energy)
```

### Example Dissolve Shader Properties
```
_DissolveAmount ("Dissolve", Range(0, 1)) = 0
_EdgeColor ("Edge Color", Color) = (1, 0.5, 0, 1)
_EdgeWidth ("Edge Width", Range(0, 0.2)) = 0.05
_NoiseTexture ("Noise", 2D) = "white" {}
```
