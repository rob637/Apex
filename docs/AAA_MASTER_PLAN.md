# 🏰 APEX CITADELS - AAA DEVELOPMENT MASTER PLAN

**Document Version:** 2.0  
**Last Updated:** January 18, 2026  
**Status:** ACTIVE DEVELOPMENT  

---

## ⚠️ RELATED DOCUMENTS - REVIEW THESE TOGETHER

> **AI INSTRUCTION**: When reviewing this document, you MUST also review these companion documents for complete context:

| Document | Purpose | Review When |
|----------|---------|-------------|
| **[README.md](../README.md)** | Technical implementation bible - 3,000+ lines of script inventory, API references, Unity AI prompts, system specifications | Always - contains ALL technical details |
| **[PC_HYBRID_DESIGN.md](PC_HYBRID_DESIGN.md)** | PC-specific architecture, scene structure, camera systems | PC client work |
| **[GAME_DESIGN.md](GAME_DESIGN.md)** | Core game mechanics, balance, progression | Gameplay features |
| **[TECHNICAL_ARCHITECTURE.md](TECHNICAL_ARCHITECTURE.md)** | System architecture, data flow, Firebase structure | Backend integration |
| **[REQUIREMENTS.md](REQUIREMENTS.md)** | Original requirements, Unity AI prompts library | Feature implementation |

**This document = Strategic Roadmap & Status Tracking**  
**README.md = Technical Implementation Details**

---

## 📋 TABLE OF CONTENTS

1. [Executive Summary](#executive-summary)
2. [The Vision](#the-vision)
3. [Current State Assessment](#current-state-assessment)
4. [AAA Quality Standards](#aaa-quality-standards)
5. [Architecture Overview](#architecture-overview)
6. [Completed Systems](#completed-systems)
7. [In-Progress Work](#in-progress-work)
8. [AAA Gap Analysis](#aaa-gap-analysis)
9. [Prioritized Roadmap](#prioritized-roadmap)
10. [Technical Debt](#technical-debt)
11. [Quality Metrics](#quality-metrics)

---

## EXECUTIVE SUMMARY

### What Is Apex Citadels?

**"Conquer the Real World"** - A location-based AR game where players:
- 📱 **AR Mobile**: Walk to real locations, stake GPS territory, build persistent structures
- 🖥️ **PC Desktop**: Strategic command center viewing the SAME world on real maps

**Core Philosophy: "One World - Two Ways to Access"**

### Where We Are Today

| Component | Completion | Quality Level |
|-----------|------------|---------------|
| Backend (Firebase/Functions) | 95% | Production-Ready |
| Admin Dashboard | 90% | Production-Ready |
| AR Mobile Scripts | 85% | Feature-Complete |
| PC Client Scripts | 70% | Functional |
| **Visual Quality** | **30%** | **NEEDS WORK** |
| **Real-World Map** | **50%** | **Foundation Built** |
| **Gameplay Loop** | **40%** | **NEEDS WORK** |

### The Gap to AAA

To match games like **Chronicles Medieval, Ascent, Blackwell**, we need:
- 🎨 Stunning visuals (particle effects, shaders, lighting)
- 🗺️ Real-world map that feels magical
- ⚔️ Engaging combat with visual feedback
- 🏗️ Satisfying building experience
- 🎵 Professional audio (music, SFX, voice)
- ✨ Polish (animations, transitions, juice)

---

## THE VISION

### Target Experience

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│   📱 AR MOBILE (Field Ops)              🖥️ PC (Command Center)              │
│   ─────────────────────────             ──────────────────────              │
│   • Walk to discover territories        • View entire world on REAL MAP     │
│   • Claim territory at GPS coords       • See same territories from above   │
│   • Build in AR (see in real world)     • Design blueprints for AR deploy   │
│   • Fight with 100% power               • Watch battle replays              │
│   • Quick 5-min sessions                • Manage alliance wars              │
│                                                                             │
│              ↓                                    ↓                         │
│              └──────── SHARED FIREBASE BACKEND ────────┘                    │
│                        (One world, two windows)                             │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Competitive Analysis

| Game | What They Do Well | What We'll Do Better |
|------|-------------------|---------------------|
| **Pokémon GO** | GPS exploration, community | Persistent building, territorial control |
| **Clash of Clans** | Base building, clan wars | Real-world locations, AR placement |
| **Fortnite** | Season pass, social events | Real-world integration, PC+Mobile sync |
| **Ingress** | Territorial control | Better visuals, building creativity |

### The "Magic Moment"

> "I built a fortress at the park near my house. I can see it on my phone when I walk by, AND see it on the real-world map from my PC. My friend attacked it yesterday - I watched the battle replay and improved my defenses."

---

## CURRENT STATE ASSESSMENT

### ✅ COMPLETED: Backend Infrastructure (95%)

| System | Status | Files | Notes |
|--------|--------|-------|-------|
| Firebase Project | ✅ | - | apex-citadels-dev |
| Cloud Functions (20+) | ✅ | backend/functions/src/ | All core game logic |
| Firestore Security Rules | ✅ | firestore.rules | Read/write permissions |
| Firestore Indexes | ✅ | firestore.indexes.json | 40+ composite indexes |
| Firebase Hosting | ✅ | 2 sites | Admin + PC |
| Test Data | ✅ | seed-local.mjs | Vienna VA + SF |

### ✅ COMPLETED: Admin Dashboard (90%)

| Page | Status | Notes |
|------|--------|-------|
| Dashboard | ✅ | Real-time metrics |
| Players | ✅ | Search, ban, details |
| Territories | ✅ | Management |
| Alliances | ✅ | Moderation |
| World Events | ✅ | Create/schedule |
| Season Pass | ✅ | Tier management |
| Analytics | ✅ | Charts, retention |
| Moderation | ✅ | Reports, bans |
| Settings | ✅ | Feature toggles |

### 🔄 IN-PROGRESS: PC Client (75%)

| System | Scripts | Status | Quality |
|--------|---------|--------|---------|
| Core Platform | PlatformManager, PCGameController | ✅ | Good |
| Camera | PCCameraController | ✅ | Good |
| Input | PCInputManager | ✅ | Good |
| Firebase Bridge | FirebaseWebClient, WebGLBridge | ✅ | Good |
| **Real-World Map** | GeoCoordinates, MapTileProvider, RealWorldMapRenderer | ✅ | **Foundation** |
| Fantasy Terrain | ProceduralTerrain, AtmosphericLighting | ✅ | Good |
| Visual Effects | AAAVisualEffects, TerritoryEffects | ✅ | Foundation |
| **Combat VFX** | CombatVFX, DamageNumbers, CombatCameraEffects | ✅ | **AAA Quality** |
| **Combat Audio** | CombatAudioSFX (procedural placeholder sounds) | ✅ | Foundation |
| UI Panels | 8 panels created | ✅ | Functional |

### 🔄 IN-PROGRESS: AR Mobile (85%)

| System | Status | Notes |
|--------|--------|-------|
| AR Foundation | ✅ | ARCore/ARKit ready |
| Geospatial API | ✅ | Cloud anchors |
| Territory System | ✅ | GPS claiming |
| Building System | ✅ | Block placement |
| Combat System | ✅ | Turn-based |
| Social Systems | ✅ | All managers created |
| UI/HUD | 🔄 | Needs polish |

---

## AAA QUALITY STANDARDS

### What Makes a Game "AAA"?

1. **Visual Fidelity** - Looks stunning in screenshots and videos
2. **Responsive Feel** - Every action has satisfying feedback
3. **Audio Excellence** - Professional music, sound effects, voice
4. **Polish** - No rough edges, smooth animations everywhere
5. **Depth** - Hundreds of hours of engaging content
6. **Performance** - Solid 60fps, no stutters

### Reference Games for Quality Targets

| Aspect | Reference | Target |
|--------|-----------|--------|
| Map Visual | Google Earth + Mapbox | Real streets with fantasy overlay |
| Building | Clash of Clans | Detailed 3D citadels |
| Combat VFX | Genshin Impact | Impactful spell effects |
| UI Polish | Fortnite | Smooth animations, haptics |
| Audio | Any Supercell game | Satisfying SFX on every action |

---

## ARCHITECTURE OVERVIEW

### System Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLIENT LAYER                                    │
│                                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐            │
│  │   AR Mobile     │  │   PC WebGL      │  │   Admin Web     │            │
│  │   (Unity)       │  │   (Unity)       │  │   (React)       │            │
│  │                 │  │                 │  │                 │            │
│  │ • ARCore/ARKit  │  │ • Real Map      │  │ • Moderation    │            │
│  │ • GPS/Geospatial│  │ • MapTileProvider│ │ • Analytics     │            │
│  │ • Touch/Gyro    │  │ • KB/Mouse      │  │ • Events        │            │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘            │
│           │                    │                    │                      │
│           └────────────────────┼────────────────────┘                      │
│                                │                                           │
└────────────────────────────────┼───────────────────────────────────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │      FIREBASE           │
                    │                         │
                    │  • Auth (Social Login)  │
                    │  • Firestore (State)    │
                    │  • Functions (Logic)    │
                    │  • Hosting (Web Apps)   │
                    │  • Storage (Assets)     │
                    └─────────────────────────┘
```

### Key File Locations

| Purpose | Path |
|---------|------|
| PC Core Scripts | `unity/ApexCitadels/Assets/Scripts/PC/` |
| PC GeoMap System | `unity/ApexCitadels/Assets/Scripts/PC/GeoMapping/` |
| PC Environment | `unity/ApexCitadels/Assets/Scripts/PC/Environment/` |
| PC UI | `unity/ApexCitadels/Assets/Scripts/PC/UI/` |
| AR Scripts | `unity/ApexCitadels/Assets/Scripts/AR/` |
| Shared/Core | `unity/ApexCitadels/Assets/Scripts/Core/` |
| Backend Functions | `backend/functions/src/` |
| Admin Dashboard | `admin-dashboard/src/` |

---

## COMPLETED SYSTEMS

### Backend Services (✅ Production-Ready)

| Service | File | Features |
|---------|------|----------|
| Combat | combat.ts | Attack, defend, damage calculation |
| Territory | territory.ts | Claim, contest, siege (3-strike) |
| Alliance | alliance.ts | Create, join, war declarations |
| Season Pass | season-pass.ts | 100 tiers, challenges, premium |
| World Events | world-events.ts | 10 event types, FOMO mechanics |
| Friends | friends.ts | Add, gift, activity feed |
| Chat | chat.ts | Global/alliance/DM, moderation |
| Referrals | referrals.ts | Codes, milestones, viral |
| Analytics | analytics.ts | Events, sessions, A/B tests |
| Anti-Cheat | anticheat.ts | GPS validation, trust scores |
| IAP | iap.ts | Purchase validation |
| Notifications | notifications.ts | FCM push |
| Map API | map-api.ts | Tiles, geohash, heatmaps |

### PC Client - Foundation (✅ Functional)

| Component | Files | Status |
|-----------|-------|--------|
| Platform Detection | PlatformManager.cs | ✅ |
| Game Controller | PCGameController.cs | ✅ |
| Camera System | PCCameraController.cs | ✅ |
| Input Handling | PCInputManager.cs | ✅ |
| Firebase Integration | FirebaseWebClient.cs, WebGLBridge | ✅ |
| UI Manager | PCUIManager.cs | ✅ |
| Day/Night Cycle | DayNightCycle.cs | ✅ |

### PC Client - Real-World Map (✅ Foundation Built)

| Component | Files | Status |
|-----------|-------|--------|
| GPS Coordinates | GeoCoordinates.cs | ✅ |
| Map Tile Provider | MapTileProvider.cs | ✅ |
| Real World Renderer | RealWorldMapRenderer.cs | ✅ |
| Geo Camera Controller | GeoMapCameraController.cs | ✅ |
| Editor Tools | GeoMapEditorTools.cs | ✅ |

### PC Client - Visual Effects (✅ Foundation Built)

| Component | Files | Status |
|-----------|-------|--------|
| Post-Processing | AAAVisualEffects.cs | ✅ |
| Procedural Terrain | ProceduralTerrain.cs | ✅ |
| Atmospheric Lighting | AtmosphericLighting.cs | ✅ |
| Territory Visuals | EnhancedTerritoryVisual.cs | ✅ |
| Territory Effects | TerritoryEffects.cs | ✅ |
| Environmental Props | EnvironmentalProps.cs | ✅ |

### PC Client - Combat VFX System (✅ NEWLY COMPLETED)

| Component | Files | Features |
|-----------|-------|----------|
| Particle Effects | CombatVFX.cs | Explosions, impacts, projectiles, shields, auras |
| Damage Numbers | DamageNumbers.cs | Floating text, crits, heals, combos, kills |
| Camera Effects | CombatCameraEffects.cs | Shake, flash, slow-mo, chromatic aberration |
| Audio SFX | CombatAudioSFX.cs | 25+ procedural sounds (sword, arrow, explosion, victory) |
| Integration | CombatEffectsIntegration.cs | Central coordinator for all combat effects |

---

## IN-PROGRESS WORK

### Real-World Map Enhancement

**Current State:** Foundation built with tile loading and demo territories  
**Target State:** Stunning, immersive real-world view with fantasy overlay

| Task | Priority | Status |
|------|----------|--------|
| Basic tile rendering | High | ✅ Done |
| Territory markers at GPS coords | High | ✅ Done |
| Demo territories (DC landmarks) | High | ✅ Done |
| Load territories from Firebase | High | ✅ Connected |
| Multiple map providers | Medium | ✅ Done |
| 3D terrain elevation | Medium | ⏳ TODO |
| Fantasy overlay effects | High | ⏳ TODO |
| Smooth tile transitions | Medium | ⏳ TODO |
| Building models on map | High | ⏳ TODO |

### Visual Polish

**Current State:** Basic post-processing and effects  
**Target State:** AAA-quality visuals matching commercial games

| Task | Priority | Status |
|------|----------|--------|
| Post-processing stack | High | ✅ Done |
| Ambient particles | Medium | ✅ Done |
| Territory auras | Medium | ✅ Done |
| God rays | Low | ✅ Done |
| **Combat VFX** | High | ✅ **DONE** |
| **Damage Numbers** | High | ✅ **DONE** |
| **Combat Camera Effects** | High | ✅ **DONE** |
| **Shader-based effects** | High | ⏳ TODO |
| **Building detail LOD** | High | ⏳ TODO |
| **Water reflections** | Medium | ⏳ TODO |
| **Weather effects** | Medium | ⏳ TODO |

---

## AAA GAP ANALYSIS

### 🔴 CRITICAL GAPS (Must Fix for AAA)

| Gap | Current | Target | Effort |
|-----|---------|--------|--------|
| ~~Combat System~~ | ~~Scripts exist, no UI~~ | ~~Full battle simulation with VFX~~ | ✅ **DONE** |
| **Base Editor** | Script exists, not integrated | Drag-drop building with preview | 2-3 days |
| **Audio System** | Combat SFX done | Music, ambient, full SFX library | 2-3 days |
| **Real Map Quality** | Basic tiles | Fantasy-styled real geography | 2-3 days |
| **Loading/Transitions** | Instant/jarring | Smooth animated transitions | 1-2 days |

### 🟡 IMPORTANT GAPS (Needed for Polish)

| Gap | Current | Target | Effort |
|-----|---------|--------|--------|
| **First-Person Mode** | None | Walk through citadel interior | 2-3 days |
| **Troop Management** | Backend only | Train, upgrade, deploy UI | 1-2 days |
| **UI Animations** | Static | Eased transitions, bounces | 1-2 days |
| **Notification Toasts** | Basic | Animated, categorized | 1 day |
| **Settings Panel** | None | Graphics, audio, controls | 1 day |

### 🟢 NICE-TO-HAVE (For "Wow" Factor)

| Gap | Current | Target | Effort |
|-----|---------|--------|--------|
| **Cinematic Mode** | None | Auto-tour of territories | 1-2 days |
| **Photo Mode** | None | Screenshot with filters | 1 day |
| **Emotes** | None | Player expression system | 2 days |
| **Alliance Hall** | None | 3D social space | 3-5 days |
| **Dynamic Weather** | None | Rain/snow/fog synced to real weather | 2 days |

---

## PRIORITIZED ROADMAP

### Phase 6: Core Gameplay Loop (Current Sprint)
**Goal:** Make the PC client feel like a GAME, not a tech demo

| # | Task | Days | Impact |
|---|------|------|--------|
| 1 | Combat System UI & VFX | 2-3 | ⭐⭐⭐⭐⭐ |
| 2 | Base Editor Integration | 2-3 | ⭐⭐⭐⭐⭐ |
| 3 | Troop Training UI | 1-2 | ⭐⭐⭐⭐ |
| 4 | Resource Spending | 1 | ⭐⭐⭐⭐ |
| 5 | Battle Replay Viewer | 1-2 | ⭐⭐⭐ |

### Phase 7: Visual Excellence
**Goal:** Screenshots that make people say "I need to play this"

| # | Task | Days | Impact |
|---|------|------|--------|
| 1 | Real Map Fantasy Overlay | 2-3 | ⭐⭐⭐⭐⭐ |
| 2 | Building Model Variety | 2-3 | ⭐⭐⭐⭐⭐ |
| 3 | Shader-Based Effects | 2 | ⭐⭐⭐⭐ |
| 4 | UI Animation Polish | 2 | ⭐⭐⭐⭐ |
| 5 | Loading Screens | 1 | ⭐⭐⭐ |

### Phase 8: Audio & Feedback
**Goal:** Every action feels impactful

| # | Task | Days | Impact |
|---|------|------|--------|
| 1 | Background Music (Suno) | 1-2 | ⭐⭐⭐⭐ |
| 2 | UI Sound Effects | 1 | ⭐⭐⭐⭐ |
| 3 | Combat SFX | 1-2 | ⭐⭐⭐⭐ |
| 4 | Ambient Audio | 1 | ⭐⭐⭐ |
| 5 | Voice Lines (ElevenLabs) | 2 | ⭐⭐⭐ |

### Phase 9: Social & Events
**Goal:** Keep players coming back daily

| # | Task | Days | Impact |
|---|------|------|--------|
| 1 | Live Event Banners | 1 | ⭐⭐⭐⭐ |
| 2 | Alliance War UI | 2 | ⭐⭐⭐⭐ |
| 3 | Notification System | 1-2 | ⭐⭐⭐⭐ |
| 4 | First-Person Mode | 2-3 | ⭐⭐⭐ |
| 5 | Cinematic Mode | 1-2 | ⭐⭐⭐ |

### Phase 10: AR Integration Testing
**Goal:** Verify AR ↔ PC sync works flawlessly

| # | Task | Days | Impact |
|---|------|------|--------|
| 1 | AR Scene Setup | 2 | ⭐⭐⭐⭐⭐ |
| 2 | Cross-Platform Sync Test | 2 | ⭐⭐⭐⭐⭐ |
| 3 | Geospatial Anchor Test | 2 | ⭐⭐⭐⭐⭐ |
| 4 | Mobile Build & Test | 2-3 | ⭐⭐⭐⭐⭐ |

---

## TECHNICAL DEBT

### Code Quality Issues

| Issue | Location | Priority |
|-------|----------|----------|
| Mock data fallbacks | Multiple scripts | Medium |
| Hardcoded colors | UI scripts | Low |
| Missing null checks | Some managers | Medium |
| No error boundaries | UI panels | Medium |

### Architecture Improvements Needed

| Improvement | Description | Priority |
|-------------|-------------|----------|
| Service locator | Replace FindObjectOfType patterns | Medium |
| Event bus | Centralize event handling | Medium |
| Object pooling | For particle effects | High |
| Asset bundles | For on-demand loading | Medium |

---

## QUALITY METRICS

### Performance Targets

| Metric | Target | Current |
|--------|--------|---------|
| PC Frame Rate | 60 fps | ~45 fps |
| Mobile Frame Rate | 30 fps | Unknown |
| Load Time | <5 sec | ~8 sec |
| Memory (PC) | <2 GB | ~1.5 GB |
| Memory (Mobile) | <1 GB | Unknown |
| Network Latency | <200ms | <500ms |

### User Experience Targets

| Metric | Target | Current |
|--------|--------|---------|
| Time to First Action | <30 sec | ~60 sec |
| Session Length | 15+ min | Unknown |
| D1 Retention | 40% | Unknown |
| D7 Retention | 20% | Unknown |

---

## APPENDIX: File Index

### Backend (/backend/functions/src/)
- combat.ts, territory.ts, alliance.ts, blueprint.ts
- season-pass.ts, world-events.ts, friends.ts, chat.ts
- referrals.ts, analytics.ts, anticheat.ts, iap.ts
- notifications.ts, moderation.ts, gdpr.ts, cosmetics.ts
- map-api.ts, world-seed.ts, protection.ts, progression.ts

### PC Client (/unity/ApexCitadels/Assets/Scripts/PC/)
- PlatformManager.cs, PCGameController.cs, PCCameraController.cs
- PCInputManager.cs, WorldMapRenderer.cs, BaseEditor.cs
- DayNightCycle.cs, MaterialHelper.cs, PCSceneBootstrapper.cs
- **GeoMapping/**: GeoCoordinates.cs, MapTileProvider.cs, RealWorldMapRenderer.cs, GeoMapCameraController.cs
- **Environment/**: AAAVisualEffects.cs, ProceduralTerrain.cs, AtmosphericLighting.cs, EnhancedTerritoryVisual.cs, TerritoryEffects.cs, EnvironmentalProps.cs, WorldEnvironmentManager.cs
- **UI/**: PCUIManager.cs, TerritoryDetailPanel.cs, AlliancePanel.cs, BuildMenuPanel.cs, StatisticsPanel.cs, BattleReplayPanel.cs, CraftingPanel.cs, MarketPanel.cs, TopBarHUD.cs

### Documentation (/docs/)
- **This Document**: AAA_MASTER_PLAN.md (primary reference)
- GAME_DESIGN.md - Full game design spec
- TECHNICAL_ARCHITECTURE.md - System design
- PC_HYBRID_DESIGN.md - PC-specific design (detailed)
- VISION.md - Product vision
- ROADMAP.md - Development milestones

---

## NEXT ACTIONS

**Immediate (This Session):**
1. ☐ Review this document for accuracy
2. ☐ Choose next priority: Combat System or Real Map Enhancement
3. ☐ Begin implementation

**This Week:**
1. ☐ Complete Phase 6 (Core Gameplay Loop)
2. ☐ Start Phase 7 (Visual Excellence)

**This Month:**
1. ☐ Complete Phases 6-8
2. ☐ Begin AR Integration Testing
3. ☐ First internal playtest

---

*"We're building the first game where your neighborhood becomes your kingdom. Let's make it unforgettable."*
