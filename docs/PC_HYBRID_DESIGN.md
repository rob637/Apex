# Apex Citadels: PC Hybrid Mode Design Document

**Status:** Phase 2A Implementation Complete ✅  
**Created:** January 17, 2026  
**Updated:** January 18, 2026  
**Priority:** Phase 2-3  

---

## Implementation Progress

### ✅ Core PC Systems (Complete)

| Script | Description | Status |
|--------|-------------|--------|
| `PlatformManager.cs` | Platform detection & feature gating | ✅ Complete |
| `PCCameraController.cs` | Multi-mode camera (WorldMap, Territory, FP, Cinematic) | ✅ Complete |
| `PCInputManager.cs` | Keyboard/mouse input handling with rebinding | ✅ Complete |
| `WorldMapRenderer.cs` | 3D world map with territory visualization | ✅ Complete |
| `BaseEditor.cs` | PC-exclusive building editor with undo/redo | ✅ Complete |
| `PCGameController.cs` | Main PC client state machine | ✅ Complete |
| `PCSceneBootstrapper.cs` | Auto scene setup on load | ✅ Complete |
| `PCTerritoryBridge.cs` | Territory data integration | ✅ Complete |

### ✅ PC UI Panels (Complete)

| Script | Description | Status |
|--------|-------------|--------|
| `PCUIManager.cs` | UI panel management | ✅ Complete |
| `TerritoryDetailPanel.cs` | Territory info display | ✅ Complete |
| `AlliancePanel.cs` | Alliance management with War Room | ✅ Complete |
| `BuildMenuPanel.cs` | Building catalog with categories | ✅ Complete |
| `StatisticsPanel.cs` | PC-exclusive analytics dashboard | ✅ Complete |
| `BattleReplayPanel.cs` | Battle replay viewer UI | ✅ Complete |
| `CraftingPanel.cs` | Crafting workshop UI | ✅ Complete |
| `MarketPanel.cs` | Trading and economy UI | ✅ Complete |

### ✅ PC-Exclusive Features (Complete)

| Script | Description | Status |
|--------|-------------|--------|
| `BattleReplaySystem.cs` | Record/playback battle replays | ✅ Complete |
| `CraftingSystem.cs` | PC crafting with quality system | ✅ Complete |

### ✅ Editor Tools (Complete)

| Script | Description | Status |
|--------|-------------|--------|
| `PCPrefabCreator.cs` | Create UI/world prefabs | ✅ Complete |
| `PCSceneSetup.cs` | Scene setup wizard | ✅ Complete |

### ✅ WebGL Bridge (Complete)

| Script | Description | Status |
|--------|-------------|--------|
| `WebGLBridge.cs` | C# bridge for JS-Unity communication | ✅ Complete |
| `WebGLBridge.jslib` | JavaScript plugin for Unity | ✅ Complete |
| `index.html` | PC hosting page with JS bridge | ✅ Complete |

### 🔄 Remaining Tasks

#### Unity Editor Tasks (Must do in Unity):
1. **Create PCMain Scene**
   - File → New Scene → Save as `Assets/Scenes/PCMain.unity`
   - Run menu: `Apex/PC/Setup PC Scene (Full)`
   - Run menu: `Apex/PC/Create All PC Prefabs`
   
2. **Wire Up Components**
   - Select PCGameController in hierarchy
   - Assign references: cameraController, inputManager, worldMapRenderer, baseEditor, uiManager
   - Select PCUIManager and assign panel prefabs
   
3. **Add WebGL Bridge**
   - Add empty GameObject named "WebGLBridge"
   - Add `WebGLBridge.cs` component to it

4. **Build WebGL**
   - File → Build Settings → WebGL
   - Player Settings → Enable gzip compression
   - Build to `backend/hosting-pc/build/`

#### Testing Tasks:
5. Test Firebase authentication flow
6. Test all keyboard shortcuts (WASD, Tab, B, M, etc.)
7. Test JS-Unity bridge communication
8. Test territory selection and camera modes

---

## Executive Summary

Apex Citadels will support both AR (mobile) and PC gameplay modes, sharing the same persistent world. Players build and explore in AR when outside, then manage and strategize on PC when at home. Both clients connect to the same Firebase backend - the world is one, the views are two.

---

## Core Philosophy

### "One World, Two Windows"

```
┌─────────────────────────────────────────────────────────────┐
│                    SHARED WORLD (Firebase)                   │
│  Territories • Buildings • Resources • Players • Alliances  │
└─────────────────────────────────────────────────────────────┘
        ▲                                       ▲
        │                                       │
┌───────┴───────┐                     ┌────────┴────────┐
│   AR CLIENT   │                     │   PC CLIENT     │
│    (Mobile)   │                     │   (Desktop)     │
│               │                     │                 │
│ • Camera view │                     │ • 3D world map  │
│ • GPS-locked  │                     │ • Free camera   │
│ • Touch input │                     │ • KB/Mouse      │
│ • On location │                     │ • From anywhere │
└───────────────┘                     └─────────────────┘
```

### Design Principles

1. **AR is "Boots on Ground"** - Physical presence matters
2. **PC is "Command Center"** - Strategy and management
3. **Neither is "Better"** - Different strengths, both essential
4. **Same Account** - Seamless cross-platform progression
5. **Real-time Sync** - Changes reflect instantly on both

---

## Feature Matrix

### AR-Exclusive Features (Must Be There Physically)

| Feature | Rationale |
|---------|-----------|
| **Claim New Territory** | Physical presence = ownership proof |
| **Discover Resource Nodes** | Rewards exploration |
| **Place AR Anchors** | Requires spatial scanning |
| **First-time Building Placement** | "Plant your flag" moment |
| **Harvest Wild Resources** | Walk to collect |
| **Scout Enemy Territory** | Reconnaissance requires presence |
| **Capture Territory** | Must physically contest |
| **Drop Geospatial Beacons** | AR-specific feature |

### PC-Exclusive Features (Command Center)

| Feature | Rationale |
|---------|-----------|
| **Detailed Base Editor** | Precise building placement needs mouse |
| **Alliance War Room** | Strategy planning needs screen real estate |
| **Crafting Workshop** | Complex recipes need good UI |
| **Market/Trading Post** | Economy management |
| **Replay Battles** | Watch attack/defense replays |
| **Statistics Dashboard** | Deep analytics |
| **Territory Network View** | See all owned territories at once |
| **Blueprint Designer** | Design structures to place in AR later |

### Shared Features (Both Platforms)

| Feature | Notes |
|---------|-------|
| **View World Map** | AR: local area / PC: full world |
| **Manage Buildings** | Upgrade, repair, demolish |
| **Alliance Chat** | Real-time messaging |
| **Defend Territories** | Respond to attacks |
| **View Leaderboards** | Rankings and stats |
| **Collect Passive Income** | Timed resource generation |
| **Daily Rewards** | Login bonuses |
| **Achievements** | Progress tracking |
| **Profile/Settings** | Account management |

---

## PC Client Architecture

### Camera System

```csharp
// PC uses traditional 3D camera instead of AR camera
public class PCCameraController : MonoBehaviour
{
    // World Map View - Strategic overhead
    public void EnterWorldMapMode();
    
    // Territory View - Zoom into specific territory
    public void EnterTerritoryMode(string territoryId);
    
    // First Person - Walk through your citadel
    public void EnterFirstPersonMode();
    
    // Cinematic - Auto-tour of your empire
    public void EnterCinematicMode();
}
```

### World Rendering

PC client renders the same data differently:

| Data | AR Rendering | PC Rendering |
|------|--------------|--------------|
| Territory | AR boundary overlay | 3D hex/region on map |
| Buildings | AR-placed 3D models | Same models, world-space |
| Resources | AR floating icons | Map markers + 3D nodes |
| Other Players | AR avatars nearby | Icons on map |
| Combat | AR spell effects | Tactical battle view |

### Input Mapping

```
┌─────────────────────────────────────────┐
│            PC CONTROLS                   │
├─────────────────────────────────────────┤
│ WASD / Arrows    │ Camera movement      │
│ Mouse            │ Look / Select        │
│ Scroll           │ Zoom in/out          │
│ Left Click       │ Select / Interact    │
│ Right Click      │ Context menu         │
│ Space            │ Toggle map/territory │
│ Tab              │ Alliance panel       │
│ B                │ Building menu        │
│ I                │ Inventory            │
│ M                │ Full world map       │
│ Esc              │ Menu / Cancel        │
└─────────────────────────────────────────┘
```

---

## Gameplay Loop Integration

### Daily Player Journey (Hybrid)

```
MORNING (Commute - AR)
├── Open app while walking
├── Harvest resources at nodes near transit
├── Quick-check territories
└── Queue defense upgrades

DAYTIME (Work - Neither)
├── Passive resource generation
├── Alliance members defend if attacked
└── Notifications for important events

EVENING (Home - PC)
├── Review day's activity
├── Detailed base building/upgrades  
├── Alliance war planning
├── Crafting session
├── Strategic territory expansion planning
└── Design blueprints for tomorrow's AR session

WEEKEND (Exploration - AR)
├── Travel to new areas
├── Claim distant territories
├── Place blueprints designed on PC
├── Alliance group activities
└── Major resource harvesting runs
```

### Cross-Platform Synergy Examples

**Example 1: Building Flow**
1. **PC:** Design a fortress blueprint in the editor
2. **AR:** Walk to your territory, place the blueprint
3. **PC:** Fine-tune placement, add decorations
4. **AR:** See finished result in real-world context

**Example 2: Combat Flow**
1. **AR:** Scout enemy territory (reveals layout)
2. **PC:** Plan attack strategy with alliance
3. **AR:** Execute attack (must be present)
4. **PC:** Watch replay, analyze what worked

**Example 3: Resource Flow**
1. **AR:** Discover rare crystal cave
2. **PC:** Build harvesting outpost (design phase)
3. **AR:** Place outpost at location
4. **PC:** Monitor production, manage logistics

---

## Technical Requirements

### Shared Codebase Strategy

```
unity/ApexCitadels/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/              # Shared - managers, data
│   │   ├── AR/                # AR-specific
│   │   ├── PC/                # PC-specific (NEW)
│   │   │   ├── PCCameraController.cs
│   │   │   ├── PCInputManager.cs
│   │   │   ├── WorldMapRenderer.cs
│   │   │   ├── BaseEditorUI.cs
│   │   │   └── TacticalBattleView.cs
│   │   ├── UI/                # Shared UI components
│   │   └── ...
│   ├── Scenes/
│   │   ├── ARMain.unity       # Mobile AR scene
│   │   └── PCMain.unity       # PC scene (NEW)
```

### Platform Detection

```csharp
public static class PlatformManager
{
    public static bool IsAR => 
        Application.platform == RuntimePlatform.Android ||
        Application.platform == RuntimePlatform.IPhonePlayer;
    
    public static bool IsPC =>
        Application.platform == RuntimePlatform.WindowsPlayer ||
        Application.platform == RuntimePlatform.OSXPlayer ||
        Application.platform == RuntimePlatform.LinuxPlayer;
    
    public static bool IsEditor =>
        Application.isEditor;
}
```

### Build Targets

| Platform | Build Type | Features |
|----------|------------|----------|
| Android | AR Client | Full AR, GPS, camera |
| iOS | AR Client | Full AR, GPS, camera |
| Windows | PC Client | Full PC, no AR |
| macOS | PC Client | Full PC, no AR |
| WebGL | Lite Client | Map view only (future) |

---

## UI/UX Specifications

### PC Main Interface

```
┌────────────────────────────────────────────────────────────────┐
│ [Logo]  Resources: 🪨 1,234  🪵 892  ⚙️ 456  💎 23  💠 150    │
├────────────────────────────────────────────────────────────────┤
│                                                    ┌──────────┐│
│                                                    │ Alliance ││
│                                                    │ Chat     ││
│              [ 3D WORLD VIEW ]                     │          ││
│                                                    │ ──────── ││
│                                                    │ Player1: ││
│         (Territories rendered on world map)        │ "Attack  ││
│                                                    │  at 9pm" ││
│                                                    │          ││
│                                                    │ Player2: ││
│                                                    │ "Ready!" ││
├────────────────────────────────────────────────────┴──────────┤
│ [Map] [Build] [Alliance] [Crafting] [Market] [Stats] [Profile]│
└────────────────────────────────────────────────────────────────┘
```

### Territory Detail View

```
┌────────────────────────────────────────────────────────────────┐
│ ← Back to Map          CENTRAL CITADEL (Level 5)      [Edit]  │
├────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────┐  ┌─────────────────────┐ │
│  │                                 │  │ TERRITORY STATS     │ │
│  │    [3D Isometric View of       │  │                     │ │
│  │     Territory with all         │  │ Defense: ████░ 85   │ │
│  │     buildings rendered]        │  │ Income:  ███░░ 62/h │ │
│  │                                 │  │ Blocks:  47/100     │ │
│  │                                 │  │                     │ │
│  │    🏰 ← Your Citadel           │  │ RECENT ACTIVITY     │ │
│  │    🧱🧱🧱 ← Walls              │  │ • Defended attack   │ │
│  │    🗼 ← Tower                  │  │ • +50 stone/hour    │ │
│  │                                 │  │ • Upgraded wall     │ │
│  └─────────────────────────────────┘  └─────────────────────┘ │
├────────────────────────────────────────────────────────────────┤
│ [Upgrade] [Add Building] [Set Rally Point] [Transfer] [Defend]│
└────────────────────────────────────────────────────────────────┘
```

---

## Balance Considerations

### Preventing PC Advantage

| Concern | Solution |
|---------|----------|
| PC players grind more hours | Cap daily PC-only rewards |
| PC building is faster | AR placement gives XP bonus |
| PC combat is easier | Require AR presence for capture |
| PC players never go outside | Weekly AR activity requirements for full rewards |

### Encouraging Both Platforms

| Incentive | Details |
|-----------|---------|
| AR Discovery Bonus | First to find a location gets permanent bonus |
| PC Strategy Bonus | Well-planned attacks do more damage |
| Hybrid Daily Quests | "Build on PC, place in AR" type quests |
| Cross-Platform Achievements | Unlock titles for using both |

### The "AR Check-in" System

To prevent pure PC play, require periodic AR verification:
- Territory ownership decays without AR visit (monthly)
- Some resources only harvestable in AR
- Alliance wars require AR presence for victory points
- Seasonal events are AR-focused

---

## Implementation Phases

### Phase 2A: PC Strategic View (Read-Mostly)

**Scope:** View-only PC client with limited interaction

- [ ] PC scene with world map camera
- [ ] Render owned territories on map
- [ ] View building layouts (read-only)
- [ ] Alliance chat integration
- [ ] Basic statistics dashboard
- [ ] Profile management

**Effort:** ~2-3 weeks  
**Dependency:** AR client stable

### Phase 2B: PC Command Center (Full Management)

**Scope:** Full territory management from PC

- [ ] Detailed base building editor
- [ ] Upgrade and repair buildings
- [ ] Resource management UI
- [ ] Alliance war planning tools
- [ ] Crafting system
- [ ] Market/trading

**Effort:** ~4-6 weeks  
**Dependency:** Phase 2A complete

### Phase 3: PC Combat & Advanced

**Scope:** Tactical gameplay on PC

- [ ] Battle replay viewer
- [ ] Tactical combat interface
- [ ] Advanced analytics
- [ ] Tournament spectator mode
- [ ] Blueprint designer
- [ ] Cinematic territory tours

**Effort:** ~4-6 weeks  
**Dependency:** Phase 2B complete, combat system mature

---

## Open Questions (To Resolve Later)

1. **Steam Release?** - Would Steam distribution make sense?
2. **Cross-Play Alliances?** - Can AR and PC players be in same alliance? (Yes, but confirm)
3. **PC-Only Players?** - Allow accounts that never use AR? (Probably limited mode)
4. **Web Client?** - Browser-based lite version for quick checks?
5. **Controller Support?** - Gamepad on PC?
6. **VR Mode?** - Future possibility for immersive base viewing?

---

## Success Metrics

| Metric | Target |
|--------|--------|
| Cross-platform DAU | 40%+ use both platforms weekly |
| Session length (PC) | 20+ minutes average |
| Session length (AR) | 10+ minutes average |
| Feature adoption | 60%+ PC users use base editor |
| Retention | PC availability increases D30 by 20% |

---

## Appendix: Reference Games

| Game | Hybrid Approach | Lessons |
|------|-----------------|---------|
| Pokémon GO + Home | Mobile catch, console manage | Clean separation works |
| Ingress Intel Map | Web strategic view | Players want overview |
| The Division | Mobile companion app | Deep integration possible |
| EVE Online | Mobile for market/skills | Async activities valuable |
| Clash of Clans | Mobile-only but... | Shows appetite for base building |

---

*This document will be revisited after AR client ships and real player feedback is gathered.*
