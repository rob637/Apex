# Apex Citadels - Visual Asset Requirements

## Overview

This document provides detailed specifications for all visual assets needed for Apex Citadels. These requirements are formatted to work with AI generation tools.

---

## 🎨 Recommended AI Tools by Asset Type

| Asset Type | Recommended Tools | Quality Level | Cost |
|------------|------------------|---------------|------|
| **3D Models** | Meshy.ai, Tripo3D, Luma Genie | Good for MVP | $20-50/mo |
| **Textures** | Midjourney, DALL-E 3 | Excellent | $10-30/mo |
| **UI/Icons** | Midjourney, Recraft.ai | Excellent | $10-30/mo |
| **Animations** | Mixamo (free), DeepMotion | Good | Free-$20/mo |
| **Skyboxes** | Blockade Labs Skybox AI | Excellent | Free tier |
| **Audio/Music** | Suno.ai, ElevenLabs | Good | $10-30/mo |

### Tool-Specific Notes

**Meshy.ai** (Best for 3D)
- Text-to-3D and Image-to-3D
- Exports FBX, OBJ, GLB (Unity compatible)
- Good topology for game assets
- ~2-5 minutes per model

**Midjourney** (Best for 2D/Textures)
- Use `--tile` for seamless textures
- Use `--ar 1:1` for icons
- V6 has excellent consistency

**Tripo3D** (Alternative for 3D)
- Better for organic shapes
- Free tier available
- Good for quick iterations

---

## 📦 CATEGORY 1: Building Blocks (P0 - Critical)

### Style Guide
```
Visual Style: Low-poly stylized medieval/fantasy
Color Palette: Earth tones with magical accents
- Primary: Stone gray (#6B7280), Wood brown (#92400E)
- Accent: Magic blue (#3B82F6), Gold (#F59E0B)
- Rarity glow: Common(white), Rare(blue), Epic(purple), Legendary(gold)
Polycount Target: 500-2000 tris per piece
Texture Size: 512x512 or 1024x1024
Format: FBX or GLB with embedded textures
```

### Foundation Blocks (8 pieces)

| ID | Asset Name | AI Prompt | Dimensions |
|----|------------|-----------|------------|
| F01 | Stone Foundation 1x1 | "Low-poly medieval stone foundation block, square platform, weathered gray stone texture, moss details, stylized game asset, isometric view, white background" | 1m x 0.5m x 1m |
| F02 | Stone Foundation 2x1 | "Low-poly rectangular stone foundation, medieval castle base, cobblestone texture, stylized, game ready, white background" | 2m x 0.5m x 1m |
| F03 | Stone Foundation 2x2 | "Low-poly square stone platform, medieval fortress foundation, large cobblestone base, stylized game asset" | 2m x 0.5m x 2m |
| F04 | Wood Platform 1x1 | "Low-poly wooden platform, medieval dock style, wooden planks, rope details, stylized game asset" | 1m x 0.3m x 1m |
| F05 | Wood Platform 2x1 | "Low-poly rectangular wooden platform, medieval style planks, nail details, game asset" | 2m x 0.3m x 1m |
| F06 | Reinforced Foundation | "Low-poly reinforced stone foundation, iron corner brackets, medieval fortress style, game asset" | 1m x 0.5m x 1m |
| F07 | Crystal Foundation | "Low-poly magical crystal-infused stone platform, glowing blue veins, fantasy game asset" | 1m x 0.5m x 1m |
| F08 | Ancient Foundation | "Low-poly ancient runic stone platform, carved symbols, weathered, fantasy archaeology style" | 1m x 0.5m x 1m |

### Wall Segments (10 pieces)

| ID | Asset Name | AI Prompt | Dimensions |
|----|------------|-----------|------------|
| W01 | Stone Wall Straight | "Low-poly medieval stone wall segment, gray brick pattern, crenellations on top, stylized game asset, side view" | 2m x 3m x 0.5m |
| W02 | Stone Wall Corner | "Low-poly medieval stone wall corner piece, L-shaped, defensive battlement style, game asset" | 1m x 3m x 1m |
| W03 | Stone Wall Gate | "Low-poly medieval stone wall with arched gateway, wooden gate frame, portcullis slot, game asset" | 3m x 4m x 0.5m |
| W04 | Stone Wall Window | "Low-poly medieval stone wall with arrow slit window, defensive architecture, game asset" | 2m x 3m x 0.5m |
| W05 | Wood Palisade | "Low-poly wooden palisade wall, sharpened log fence, medieval defensive, game asset" | 2m x 2.5m x 0.3m |
| W06 | Wood Palisade Gate | "Low-poly wooden palisade with simple gate, rope hinges, medieval style, game asset" | 2m x 2.5m x 0.3m |
| W07 | Reinforced Wall | "Low-poly reinforced stone wall, iron plates, rivets, heavy medieval fortress style" | 2m x 3m x 0.6m |
| W08 | Magic Barrier Wall | "Low-poly magical barrier wall, translucent blue energy between stone pillars, fantasy" | 2m x 3m x 0.3m |
| W09 | Half Wall | "Low-poly short medieval stone wall, waist height, defensive barrier, game asset" | 2m x 1.5m x 0.5m |
| W10 | Damaged Wall | "Low-poly damaged medieval stone wall, crumbling section, rubble, battle-worn" | 2m x 3m x 0.5m |

### Towers (6 pieces)

| ID | Asset Name | AI Prompt | Dimensions |
|----|------------|-----------|------------|
| T01 | Guard Tower | "Low-poly medieval guard tower, square stone tower, wooden roof, lookout platform, stylized game asset" | 2m x 6m x 2m |
| T02 | Archer Tower | "Low-poly medieval archer tower, tall narrow stone tower, multiple arrow slits, pointed roof" | 1.5m x 8m x 1.5m |
| T03 | Mage Tower | "Low-poly fantasy wizard tower, spiral stone tower, glowing crystal top, magical runes, purple accents" | 2m x 10m x 2m |
| T04 | Siege Tower | "Low-poly medieval siege tower, wooden construction, wheels, ladder, military style" | 3m x 8m x 3m |
| T05 | Watch Tower | "Low-poly small wooden watchtower, elevated platform, simple roof, frontier style" | 1.5m x 5m x 1.5m |
| T06 | Bell Tower | "Low-poly medieval bell tower, stone base, open top with bell, church/castle style" | 2m x 7m x 2m |

### Roofs & Decorations (8 pieces)

| ID | Asset Name | AI Prompt | Dimensions |
|----|------------|-----------|------------|
| R01 | Peaked Roof Small | "Low-poly medieval peaked roof, wooden shingles, small building cap, game asset" | 2m x 1.5m x 2m |
| R02 | Peaked Roof Large | "Low-poly medieval large peaked roof, slate tiles, dormer window, game asset" | 4m x 2m x 4m |
| R03 | Flat Roof Battlement | "Low-poly flat roof with battlements, medieval castle top, crenellations, game asset" | 2m x 0.5m x 2m |
| R04 | Dome Roof | "Low-poly ornate dome roof, middle eastern style, gold trim, fantasy palace" | 2m x 2m x 2m |
| D01 | Banner Pole | "Low-poly medieval banner pole with flag, castle decoration, heraldry style" | 0.2m x 3m x 0.2m |
| D02 | Torch Sconce | "Low-poly wall torch sconce, medieval iron bracket, flame, castle lighting" | 0.3m x 0.5m x 0.3m |
| D03 | Wooden Crate | "Low-poly wooden storage crate, medieval supplies, rope bound, game prop" | 0.5m x 0.5m x 0.5m |
| D04 | Barrel | "Low-poly wooden barrel, iron bands, medieval storage, game prop" | 0.4m x 0.6m x 0.4m |

### Special Buildings (6 pieces)

| ID | Asset Name | AI Prompt | Dimensions |
|----|------------|-----------|------------|
| S01 | Resource Mine | "Low-poly fantasy mine entrance, wooden frame, cart tracks, glowing crystals inside, game asset" | 3m x 3m x 3m |
| S02 | Barracks | "Low-poly medieval barracks building, long hall, multiple windows, military style" | 6m x 4m x 4m |
| S03 | Blacksmith | "Low-poly medieval blacksmith shop, forge chimney, anvil visible, smoke, game building" | 4m x 4m x 4m |
| S04 | Magic Well | "Low-poly magical well, ancient stone, glowing blue water, runic carvings, fantasy" | 1.5m x 2m x 1.5m |
| S05 | Throne Room | "Low-poly medieval throne room interior, ornate chair, banners, pillars, game asset" | 6m x 5m x 6m |
| S06 | Portal Gate | "Low-poly magical portal archway, stone frame, swirling purple energy, fantasy game" | 3m x 5m x 1m |

---

## 🖼️ CATEGORY 2: UI Assets (P0 - Critical)

### UI Style Guide
```
Style: Clean fantasy UI with subtle gradients
Shape Language: Rounded rectangles with ornate corners
Colors:
- Background: Dark slate (#1E293B) with 80% opacity
- Panels: Gradient from #334155 to #1E293B
- Buttons: Gold accent (#F59E0B) with hover glow
- Text: White (#FFFFFF) and cream (#FEF3C7)
- Borders: Thin gold (#D97706) or silver (#9CA3AF)
Resolution: Design at 2x (retina), export 1x and 2x
Format: PNG with transparency
```

### Resource Icons (8 icons) - 128x128px

| ID | Icon Name | AI Prompt |
|----|-----------|-----------|
| I01 | Gold Coin | "Game UI icon, single gold coin, shiny metallic, fantasy medieval style, simple clean design, transparent background, 2D flat" |
| I02 | Gem Crystal | "Game UI icon, blue crystal gem, faceted diamond shape, glowing, fantasy style, transparent background, 2D" |
| I03 | Wood Bundle | "Game UI icon, bundle of wooden logs tied with rope, medieval resource, clean design, transparent background" |
| I04 | Stone Pile | "Game UI icon, pile of gray stone blocks, quarried rock, medieval resource, transparent background" |
| I05 | Metal Ingot | "Game UI icon, silver/steel metal ingot bar, refined ore, medieval crafting, transparent background" |
| I06 | Crystal Shard | "Game UI icon, purple magic crystal shard, glowing energy, fantasy resource, transparent background" |
| I07 | Food Basket | "Game UI icon, woven basket with bread and apples, medieval food resource, transparent background" |
| I08 | XP Star | "Game UI icon, golden star with sparkle effects, experience points, RPG style, transparent background" |

### Action Icons (12 icons) - 96x96px

| ID | Icon Name | AI Prompt |
|----|-----------|-----------|
| A01 | Build/Hammer | "Game UI icon, medieval hammer tool, construction symbol, clean vector style, golden handle, transparent" |
| A02 | Attack/Sword | "Game UI icon, crossed swords, combat attack symbol, silver blades, fantasy RPG, transparent" |
| A03 | Defend/Shield | "Game UI icon, medieval shield with emblem, defense symbol, silver and blue, transparent" |
| A04 | Upgrade/Arrow Up | "Game UI icon, upward arrow with sparkles, upgrade symbol, green and gold, transparent" |
| A05 | Collect/Hand | "Game UI icon, hand grabbing coins, collect resources, golden glow, transparent" |
| A06 | Move/Arrows | "Game UI icon, four directional arrows, movement control, clean design, transparent" |
| A07 | Rotate/Circular | "Game UI icon, circular rotation arrows, 3D rotate symbol, clean design, transparent" |
| A08 | Delete/X | "Game UI icon, red X mark, delete/cancel symbol, clean bold design, transparent" |
| A09 | Confirm/Check | "Game UI icon, green checkmark, confirm/accept symbol, bold clean design, transparent" |
| A10 | Info/Question | "Game UI icon, question mark in circle, help/info symbol, blue and white, transparent" |
| A11 | Settings/Gear | "Game UI icon, mechanical gear cog, settings symbol, metallic gray, transparent" |
| A12 | Menu/Bars | "Game UI icon, three horizontal lines, hamburger menu symbol, clean design, transparent" |

### Navigation Icons (8 icons) - 80x80px

| ID | Icon Name | AI Prompt |
|----|-----------|-----------|
| N01 | Map | "Game UI icon, treasure map scroll, navigation symbol, parchment style, transparent" |
| N02 | Inventory | "Game UI icon, open backpack/chest, inventory symbol, medieval style, transparent" |
| N03 | Alliance | "Game UI icon, two hands shaking, alliance/guild symbol, golden, transparent" |
| N04 | Leaderboard | "Game UI icon, trophy cup with number 1, ranking symbol, gold and silver, transparent" |
| N05 | Shop | "Game UI icon, merchant stall or coin purse, shop symbol, golden coins, transparent" |
| N06 | Profile | "Game UI icon, knight helmet silhouette, player profile symbol, silver, transparent" |
| N07 | Notifications | "Game UI icon, bell with alert dot, notifications symbol, golden bell, red dot, transparent" |
| N08 | Chat | "Game UI icon, speech bubbles, chat/message symbol, clean design, transparent" |

### Panel Frames (6 frames) - Various sizes

| ID | Frame Name | AI Prompt | Size |
|----|------------|-----------|------|
| P01 | Main Panel | "Game UI panel frame, medieval fantasy style, ornate golden corners, dark gradient center, elegant border, PNG" | 800x600 |
| P02 | Dialog Box | "Game UI dialog box frame, parchment style background, decorative corners, speech bubble shape" | 600x200 |
| P03 | Tooltip | "Game UI tooltip frame, small info box, pointed bottom, dark background, thin gold border" | 300x150 |
| P04 | Button Normal | "Game UI button, medieval style, stone texture, golden border, rounded rectangle" | 200x60 |
| P05 | Button Pressed | "Game UI button pressed state, medieval style, darker stone, inset effect, golden border" | 200x60 |
| P06 | Progress Bar | "Game UI progress bar frame, medieval style, empty bar with golden end caps, ornate design" | 400x40 |

### Rarity Frames (5 frames) - 120x120px item frames

| ID | Frame Name | AI Prompt |
|----|------------|-----------|
| RF01 | Common Frame | "Game UI item frame, simple gray stone border, common rarity, clean design, square" |
| RF02 | Uncommon Frame | "Game UI item frame, green glowing border, uncommon rarity, subtle magic effect" |
| RF03 | Rare Frame | "Game UI item frame, blue glowing border, rare rarity, magical sparkles" |
| RF04 | Epic Frame | "Game UI item frame, purple glowing border, epic rarity, energy wisps" |
| RF05 | Legendary Frame | "Game UI item frame, golden glowing border, legendary rarity, divine light rays" |

---

## ✨ CATEGORY 3: Visual Effects (P1 - Important)

### VFX Style Guide
```
Style: Stylized particle effects, not photorealistic
Colors: Match building/UI palette with magical accents
Format: Unity Particle System or sprite sheets
Duration: 0.5-3 seconds typically
Performance: Max 100 particles per effect for mobile AR
```

### Building Effects (6 effects)

| ID | Effect Name | Description | AI Reference Prompt |
|----|-------------|-------------|---------------------|
| VFX01 | Place Building | Sparkles rising, golden glow, dust settling | "Magical building placement effect, golden sparkles rising upward, dust particles, fantasy construction, game VFX reference" |
| VFX02 | Destroy Building | Debris flying, smoke, crumbling particles | "Building destruction effect, stone debris flying, dust cloud, crumbling particles, medieval demolition VFX" |
| VFX03 | Upgrade Complete | Bright flash, ascending stars, level-up feel | "Upgrade complete effect, bright golden flash, stars ascending, magical transformation, RPG level up VFX" |
| VFX04 | Resource Collect | Floating icons moving to UI, absorption | "Resource collection effect, floating gold coins moving toward screen, magical absorption trail" |
| VFX05 | Building Select | Outline glow, subtle pulse, selection indicator | "Selection highlight effect, glowing outline pulse, subtle blue aura, game object selection" |
| VFX06 | Invalid Placement | Red pulse, X pattern, rejection feedback | "Invalid placement effect, red pulse wave, X pattern flash, error feedback VFX" |

### Combat Effects (5 effects)

| ID | Effect Name | Description | AI Reference Prompt |
|----|-------------|-------------|---------------------|
| VFX07 | Quick Strike | Fast slash, small impact | "Quick attack slash effect, fast white arc, small impact sparks, action game combat VFX" |
| VFX08 | Heavy Assault | Large impact, shockwave | "Heavy attack impact effect, large explosion burst, ground shockwave ring, powerful hit VFX" |
| VFX09 | Siege Attack | Catapult trail, big explosion | "Siege attack effect, projectile arc trail, large fiery explosion, medieval warfare VFX" |
| VFX10 | Shield Block | Barrier flash, deflection | "Shield block effect, energy barrier flash, deflection sparks, defensive magic VFX" |
| VFX11 | Critical Hit | Extra flash, screen shake reference | "Critical hit effect, bright flash burst, impact stars, screen shake intensity, dramatic VFX" |

### Ambient Effects (4 effects)

| ID | Effect Name | Description | AI Reference Prompt |
|----|-------------|-------------|---------------------|
| VFX12 | Magic Aura | Gentle glow around magic buildings | "Magical aura effect, soft glowing particles orbiting, blue purple energy wisps, ambient magic" |
| VFX13 | Fire/Torch | Flickering flame for torch props | "Torch flame effect, flickering orange fire, rising embers, warm glow, medieval torch" |
| VFX14 | Water Sparkle | For wells and water features | "Water sparkle effect, light reflections on water surface, gentle ripples, magical well" |
| VFX15 | Territory Border | Edge glow for owned territory | "Territory border effect, glowing line on ground, faction color pulse, ownership indicator" |

---

## 🌍 CATEGORY 4: Environment Assets (P1 - Important)

### AR-Specific Assets

| ID | Asset Name | AI Prompt | Notes |
|----|------------|-----------|-------|
| E01 | AR Grid Overlay | "Game AR placement grid, hexagonal pattern, subtle blue glow, holographic style, transparent" | Semi-transparent, guides building placement |
| E02 | AR Ground Plane | "Game AR ground indicator, circular target area, scanning animation reference, tech style" | Shows detected surfaces |
| E03 | AR Boundary | "Game AR play area boundary, glowing edge line, safe zone indicator, blue wireframe style" | Territory boundaries in AR |
| E04 | Distance Marker | "Game distance indicator, holographic arrow pointing, meters display, AR navigation" | Shows direction to territories |

### Skybox Environments (Use Blockade Labs)

| ID | Environment | Prompt for Blockade Labs Skybox AI |
|----|-------------|-----------------------------------|
| SKY01 | Day Clear | "Medieval fantasy kingdom, clear blue sky, distant mountains, green rolling hills, peaceful, stylized" |
| SKY02 | Day Cloudy | "Medieval fantasy landscape, dramatic clouds, castle silhouettes in distance, epic atmosphere" |
| SKY03 | Sunset | "Fantasy kingdom at sunset, orange golden sky, purple clouds, magical atmosphere, castle spires" |
| SKY04 | Night | "Fantasy kingdom at night, starry sky, two moons, magical aurora, mysterious atmosphere" |
| SKY05 | Battle | "Dark stormy sky, lightning, ominous clouds, fantasy war atmosphere, dramatic" |

---

## 👤 CATEGORY 5: Characters & Avatars (P2 - Future)

### Avatar Components (Mix & Match System)

| Category | Items Needed | AI Prompt Base |
|----------|--------------|----------------|
| Heads | 10 variations | "Low-poly character head, [style] hairstyle, fantasy warrior, game avatar, front view" |
| Bodies | 5 armor types | "Low-poly character torso, [type] armor, medieval fantasy, game avatar component" |
| Accessories | 8 items | "Low-poly character accessory, [item], fantasy medieval, avatar customization" |

### NPC Characters (4 characters)

| ID | Character | AI Prompt |
|----|-----------|-----------|
| NPC01 | Builder NPC | "Low-poly medieval builder character, hammer and apron, friendly craftsman, fantasy game NPC, T-pose" |
| NPC02 | Merchant NPC | "Low-poly medieval merchant character, coin purse, fancy clothes, shop keeper, game NPC, T-pose" |
| NPC03 | Guard NPC | "Low-poly medieval guard character, spear and shield, armor, castle defender, game NPC, T-pose" |
| NPC04 | Mage NPC | "Low-poly fantasy wizard character, staff, robes, magical aura, advisor NPC, T-pose" |

---

## 🎵 CATEGORY 6: Audio Assets (P2 - Future)

### Music Tracks (Use Suno.ai)

| ID | Track Name | Suno Prompt | Duration |
|----|------------|-------------|----------|
| M01 | Main Theme | "Epic medieval fantasy orchestra, heroic theme, castle building game, adventurous, memorable melody" | 2-3 min loop |
| M02 | Building Mode | "Calm medieval ambient, construction sounds, peaceful fantasy, gentle strings, building game" | 2-3 min loop |
| M03 | Combat | "Intense medieval battle music, drums, brass, urgent combat, fantasy war, action game" | 2 min loop |
| M04 | Victory | "Triumphant medieval fanfare, victory celebration, heroic achievement, short jingle" | 15-30 sec |
| M05 | Defeat | "Somber medieval music, defeat theme, minor key, brief sadness, respectful" | 15-30 sec |

### Sound Effects (Use freesound.org or generate)

| Category | Sounds Needed | Count |
|----------|---------------|-------|
| Building | Place, destroy, upgrade, select | 4 |
| Combat | Attack types, hit, block, victory | 6 |
| UI | Button click, panel open/close, notification, error | 5 |
| Ambient | Wind, birds, torch crackle | 3 |
| Resources | Coin collect, gem collect, resource gather | 4 |

---

## 📋 Production Checklist

### MVP Launch (Minimum Viable)
- [ ] 20 Building blocks (foundations, walls, towers, roofs)
- [ ] 25 UI icons (resources, actions, navigation)
- [ ] 6 Panel frames
- [ ] 5 Building VFX
- [ ] 3 Combat VFX
- [ ] AR grid overlay

### Full Launch (Complete Experience)
- [ ] 38 Building blocks (all listed above)
- [ ] 50+ UI icons
- [ ] All panel frames and rarity borders
- [ ] 15 VFX effects
- [ ] 5 Skybox environments
- [ ] 4 NPC characters
- [ ] 5 Music tracks
- [ ] 22 Sound effects

### World-Class (Premium Experience)
- [ ] 60+ Building blocks with variants
- [ ] 100+ UI icons including cosmetic items
- [ ] Animated UI elements
- [ ] 30+ VFX with quality variations
- [ ] Seasonal skybox variants
- [ ] Full avatar customization (50+ pieces)
- [ ] Voice acting for NPCs
- [ ] Dynamic music system
- [ ] Haptic feedback patterns

---

## 🔧 Technical Specifications

### 3D Models
```
Format: FBX or GLB (GLB preferred for web preview)
Polycount: 500-2000 triangles per building piece
UV Mapping: Single UV set, no overlapping
Textures: Albedo required, Normal optional
Texture Size: 512x512 (mobile) or 1024x1024 (quality)
Origin Point: Center bottom of model
Forward: -Z axis
Scale: 1 Unity unit = 1 meter
```

### UI Assets
```
Format: PNG with transparency
Resolution: 2x for retina, provide 1x variant
Color Space: sRGB
Compression: PNG-8 for simple icons, PNG-24 for gradients
9-slice: Mark slice guides for scalable panels
```

### VFX
```
Format: Unity Particle System prefabs OR sprite sheets
Sprite Sheets: 4x4 or 8x8 grid, PNG sequence
Frame Rate: 24-30 FPS for sprite animations
Looping: Ambient effects should loop seamlessly
```

### Audio
```
Format: OGG (compressed) or WAV (high quality)
Sample Rate: 44.1kHz
Bit Depth: 16-bit
Music: Stereo
SFX: Mono (for 3D positioning)
```

---

## 💡 Tips for AI Generation

### For Best 3D Results (Meshy/Tripo)
1. Generate from multiple angles and pick best
2. Always include "low-poly", "game asset", "stylized"
3. Specify "white background" or "transparent background"
4. Generate 3-5 variants and choose best
5. May need manual cleanup in Blender for topology

### For Best 2D Results (Midjourney)
1. Use `--v 6` for latest model
2. Add `--style raw` for less artistic interpretation
3. Use `--tile` for seamless textures
4. Use `--ar 1:1` for square icons
5. Add "game UI icon" and "transparent background"
6. Use `--no text, words, letters` to avoid unwanted text

### For Consistency
1. Create a few "hero" assets first as reference
2. Use Midjourney's `--sref` with reference images
3. Maintain same prompting structure across assets
4. Generate in batches by category

---

## 📊 Estimated Effort

| Approach | Time | Cost | Quality |
|----------|------|------|---------|
| Pure AI Generation | 2-4 weeks | $100-300 | Good (80%) |
| AI + Light Touch-up | 4-6 weeks | $200-500 | Great (90%) |
| AI + Professional Polish | 6-8 weeks | $500-2000 | Excellent (95%) |
| Professional Artists | 8-16 weeks | $5000-20000 | World-class (100%) |

### Recommended Approach for MVP
1. **Week 1-2**: Generate all 3D building blocks with Meshy
2. **Week 2-3**: Generate all UI with Midjourney
3. **Week 3-4**: Create VFX sprite sheets, basic audio
4. **Week 4+**: Iterate and polish based on testing

---

*Document generated for Apex Citadels asset production*
*Last updated: January 2026*
