# Apex Citadels

> **Conquer the Real World** - Build persistent fortresses, claim territory, defend against rivals

![Status](https://img.shields.io/badge/status-prototype-yellow)
![Unity](https://img.shields.io/badge/Unity-6-black)
![Platform](https://img.shields.io/badge/platform-iOS%20%7C%20Android%20%7C%20Windows-blue)
![Age](https://img.shields.io/badge/age-Teen%2B-orange)

---

## 🌟 What is Apex Citadels?

**The game where you conquer the real world.**

Build a fortress at your local park. Claim the territory. Watch rivals try to take it from you. Every structure persists in the real world — visible to all players, attackable by enemies, yours to defend.

*"I own a fortress at Central Park and I'm defending it from rivals right now."*

### The Theme: Mythic Modern

*"Magic has awakened. Ancient ley lines under our cities now power the construction of impossible structures. You're a Citadel Lord, claiming territory where magic is strongest."*

Fantasy aesthetics meet real-world locations — castles, towers, and magic in YOUR neighborhood.

---

## 🎮 The Core Loop

### Dual-Platform Experience

Play on **AR Mobile** for boots-on-ground action, or **PC** for strategic command:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│   📱 AR MOBILE (Field Ops)              🖥️ PC (Command Center)              │
│   ─────────────────────────             ──────────────────────              │
│   • Walk to discover resources          • View entire territory network     │
│   • Claim territory in-person           • Design blueprints (place via AR)  │
│   • Build in AR (see it in real world)  • Queue crafting jobs               │
│   • Fight with 100% power               • Manage alliance wars              │
│   • Quick 5-min sessions                • Watch battle replays              │
│   • Place structures from blueprints    • Analyze statistics                │
│   • Defend your citadels                • Trade in the market               │
│                                                                             │
│              ↓                                    ↓                         │
│              └──────── SHARED FIREBASE BACKEND ────────┘                    │
│                        (One world, two windows)                             │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### AR Mobile Loop

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│   EXPLORE (walk around, find opportunities)                     │
│        ↓                                                        │
│   CLAIM (plant flag, own 25-50m radius)                        │
│        ↓                                                        │
│   GATHER (walking generates resources)                          │
│        ↓                                                        │
│   BUILD (walls, towers, defenses)                              │
│        ↓                                                        │
│   ┌─────────────────┐     ┌─────────────────┐                  │
│   │ DEFEND          │     │ ATTACK          │                  │
│   │ (protect yours) │ OR  │ (take theirs)   │                  │
│   └─────────────────┘     └─────────────────┘                  │
│        ↓                         ↓                              │
│   EARN REWARDS ←─────────────────┘                              │
│        ↓                                                        │
│   UPGRADE & EXPAND → (repeat)                                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### PC Command Loop

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│   OVERVIEW (view all territories on world map)                  │
│        ↓                                                        │
│   PLAN (design blueprints, queue crafting)                      │
│        ↓                                                        │
│   ANALYZE (review battle replays, statistics)                   │
│        ↓                                                        │
│   COORDINATE (alliance chat, war strategy)                      │
│        ↓                                                        │
│   TRADE (buy/sell in market)                                    │
│        ↓                                                        │
│   PREPARE (load blueprints for next AR session)                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

| Phase | AR Mobile | PC Command |
|-------|-----------|------------|
| **🗺️ Explore** | Walk around, find unclaimed spots | View territory network, plan expansion |
| **🚩 Claim** | Plant your flag, own real territory | N/A (must be in-person) |
| **⛏️ Gather** | Collect Stone, Wood, Iron, Crystal, Arcane | Monitor resource income |
| **🏗️ Build** | Place structures in AR | Design blueprints for later |
| **⚔️ Battle** | Fight with 100% power | Watch replays, analyze tactics |
| **👑 Expand** | Take more territory | Coordinate alliance strategy |
| **🔧 Craft** | Quick crafts | Advanced crafting with quality system |
| **💹 Trade** | Quick market access | Full market with graphs & history |

---

## ✨ What Makes It Unique

| Feature | Why It's Special |
|---------|------------------|
| **Real addresses matter** | "I own 123 Main Street" |
| **Dual-platform play** | AR mobile for action, PC for strategy — same world |
| **Physical presence rewarded** | 100% power in-person, 50% remote |
| **Visible to everyone** | AR structures visible to all players |
| **Local rivalries** | School vs school, neighborhood vs neighborhood |
| **Persistent world** | Build today, still there next year |
| **Fair loss system** | 3 battles to lose territory, reclaim mechanic |
| **Strategic combat** | Turn-based battles with 6 troop types |
| **Seasonal competition** | 10-week seasons with regional control |
| **Blueprint system** | Design on PC, place in AR — plan before you explore |

---

## ⚔️ Core Game Systems

### Territory Control

| Environment | Claim Radius | Max Territories |
|-------------|--------------|-----------------|
| Urban (high density) | 25 meters | 1-5 based on level |
| Suburban/Rural | 50 meters | 1-5 based on level |

### The Siege System (3-Strike Loss)

Territories require **3 battle losses** to fall — creating stakes without devastation:

```
SECURE ──────→ CONTESTED ──────→ VULNERABLE ──────→ FALLEN
  ↑               (1 loss)         (2 losses)       (3 losses)
  │                                                     │
  └─────────────── RECLAIM (24hr cooldown) ─────────────┘
```

**When territory falls, you KEEP:** Your level, cosmetics, and blueprint of your citadel  
**When territory falls, you LOSE:** Ownership (temporarily), 50% unprotected resources

### Combat: Turn-Based Strategy

| Troop Type | Role | Strong Against |
|------------|------|----------------|
| **Infantry** | Front line | Archers |
| **Archer** | Ranged damage | Cavalry |
| **Cavalry** | Fast flanking | Archers, Siege |
| **Siege** | Structure damage | Buildings |
| **Mage** | Area damage | Groups |
| **Guardian** | Defense | Cavalry |

**Participation Power:**
- Physical presence: 100%
- Remote (anywhere): 50%

### Resources (5 Types)

| Resource | Source | Primary Use |
|----------|--------|-------------|
| **Stone** | Urban, mountains | Walls, foundations |
| **Wood** | Parks, forests | Buildings, siege |
| **Iron** | Industrial areas | Weapons, defenses |
| **Crystal** | Special nodes | Upgrades, enchantments |
| **Arcane Essence** | World events | Magic structures |

### Alliance System

- **Size:** 10 members (expandable)
- **Roles:** Leader → Officers → Veterans → Members
- **Features:** Shared visibility, treasury, war declarations
- **War system:** 24hr warning → 48hr war → 72hr peace treaty

### Victory Conditions

| Type | Metric | Reset |
|------|--------|-------|
| **Seasonal Leaderboard** | Territory points, war score | Every 10 weeks |
| **Regional Control** | Control neighborhood/city | Persistent |

📖 **Full details:** [docs/GAME_DESIGN.md](docs/GAME_DESIGN.md)

---

## 🚀 Development Phases

### Phase 1: "Land Rush" (MVP) ✅ *Complete*
- [x] Persistent object placement
- [x] Cross-device visibility
- [x] Cloud sync (Firebase)
- [x] Territory claiming with radius
- [x] Basic building blocks
- [x] Map view of claimed territories

**Success metric:** People build things and come back to see them

### Phase 2: "Fortify" ✅ *Complete*
- [x] Resource gathering (walk to collect)
- [x] Building pieces (walls, towers, gates)
- [x] Territory boundaries visible on map
- [x] Alliance/team system
- [x] Daily territory maintenance
- [x] Daily rewards & login streaks

**Success metric:** People spend 20+ mins building

### Phase 3: "Conquer" ✅ *Complete*
- [x] Attack mechanics & combat system
- [x] Defense structures (turrets, traps)
- [x] Territory wars (scheduled battles)
- [x] Leaderboards by neighborhood/city
- [x] Push notifications
- [x] Anti-cheat & location validation

**Success metric:** Retention spikes around battle times

### Phase 4: "Dominate" ✅ *Complete*
- [x] World events with FOMO mechanics
- [x] Season Pass (100-tier battle pass)
- [x] Friends & social system
- [x] Real-time chat (global/alliance/DM)
- [x] Referral & viral growth system
- [x] Analytics & A/B testing
- [x] Admin Dashboard (web)

### Phase 5: "Command" ✅ *Complete* - PC Hybrid Client
- [x] PC client architecture (same Firebase backend)
- [x] Multi-mode camera system (WorldMap, Territory, FirstPerson, Cinematic)
- [x] Keyboard/mouse input with rebinding
- [x] 3D Strategic world map renderer
- [x] Detailed base editor with undo/redo
- [x] Blueprint designer (design on PC, place in AR)
- [x] Battle replay viewer (PC-exclusive)
- [x] Crafting workshop system (PC-exclusive)
- [x] Market/trading panel
- [x] Alliance War Room (PC-exclusive)
- [x] Statistics dashboard (PC-exclusive)
- [x] Unity Editor tools for PC scene setup

**Success metric:** 40%+ of players use both AR and PC weekly

---

## 🖥️ PC Hybrid Client

> **One World, Two Windows** - Same Firebase backend, different interfaces

The PC client provides a "Command Center" experience while the AR mobile client is "Boots on Ground":

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
│ • GPS-locked  │                     │ • Free camera   │
│ • Touch input │                     │ • KB/Mouse      │
│ • On location │                     │ • From anywhere │
└───────────────┘                     └─────────────────┘
```

### PC-Exclusive Features

| Feature | Description |
|---------|-------------|
| **Base Editor** | Precise building placement with grid snapping, undo/redo, blueprints |
| **Battle Replays** | Watch attack/defense replays with playback controls and analysis |
| **Crafting Workshop** | Queue-based crafting with quality tiers (Normal→Legendary) |
| **Market/Trading** | Buy/sell resources and items with other players |
| **Alliance War Room** | Strategy planning, member coordination, war declarations |
| **Statistics Dashboard** | Deep analytics, charts, performance tracking |
| **Blueprint Designer** | Design structures on PC, place them in AR later |

### PC Controls

| Key | Action |
|-----|--------|
| WASD / Arrows | Camera movement |
| Mouse | Look / Select |
| Scroll | Zoom in/out |
| Left Click | Select / Interact |
| Right Click | Context menu |
| Space | Toggle map/territory |
| Tab | Alliance panel |
| B | Building menu |
| M | Full world map |
| Esc | Menu / Cancel |

### PC Scripts Created

| Script | Description |
|--------|-------------|
| `PlatformManager.cs` | Platform detection & feature gating |
| `PCCameraController.cs` | 4 camera modes with smooth transitions |
| `PCInputManager.cs` | Keyboard/mouse with key rebinding |
| `WorldMapRenderer.cs` | 3D strategic world map |
| `BaseEditor.cs` | Building editor with undo/redo |
| `PCGameController.cs` | Main PC state machine |
| `BattleReplaySystem.cs` | Battle replay with playback controls |
| `CraftingSystem.cs` | PC crafting with quality system |
| `PCUIManager.cs` | Panel management |
| `TerritoryDetailPanel.cs` | Territory stats display |
| `AlliancePanel.cs` | War Room & member management |
| `BuildMenuPanel.cs` | Building catalog |
| `StatisticsPanel.cs` | Analytics dashboard |
| `BattleReplayPanel.cs` | Replay viewer UI |
| `CraftingPanel.cs` | Crafting workshop UI |
| `MarketPanel.cs` | Trading interface |

📖 **Full PC design:** [docs/PC_HYBRID_DESIGN.md](docs/PC_HYBRID_DESIGN.md)

---

## 📁 Project Structure

```
Apex/
├── docs/                          # Documentation
│   ├── GAME_DESIGN.md             # 🎮 Complete game design spec
│   ├── VISION.md                  # Product vision and strategy
│   ├── TECHNICAL_ARCHITECTURE.md  # System design
│   ├── CODE_ARCHITECTURE.md       # Code structure guide
│   └── ROADMAP.md                 # Development milestones
│
├── admin-dashboard/              # Admin Dashboard (React + Vite + MUI)
│   ├── src/
│   │   ├── pages/                # 10 admin pages
│   │   ├── layouts/              # Dashboard layout
│   │   └── config/               # Firebase & theme config
│   └── package.json
│
├── unity/ApexCitadels/           # Unity Project
│   └── Assets/
│       └── Scripts/
│           ├── Core/             # GameManager, initialization
│           ├── AR/               # Spatial anchor system
│           ├── PC/               # 🖥️ PC Client (NEW)
│           │   ├── PlatformManager.cs
│           │   ├── PCCameraController.cs
│           │   ├── PCInputManager.cs
│           │   ├── WorldMapRenderer.cs
│           │   ├── BaseEditor.cs
│           │   ├── PCGameController.cs
│           │   ├── BattleReplaySystem.cs
│           │   ├── CraftingSystem.cs
│           │   ├── UI/           # PC UI panels
│           │   └── Editor/       # Unity Editor tools
│           ├── Backend/          # Firebase integration
│           ├── WorldEvents/      # FOMO event system
│           ├── SeasonPass/       # Battle pass system
│           ├── Social/           # Friends & social features
│           ├── Chat/             # Real-time chat
│           ├── Referrals/        # Viral growth system
│           ├── Analytics/        # Event tracking & A/B tests
│           ├── AntiCheat/        # Location validation
│           ├── Territory/        # Territory control
│           ├── Building/         # Block placement + templates
│           ├── Combat/           # Attack mechanics
│           ├── Alliance/         # Team system
│           ├── Player/           # Player profiles
│           ├── Resources/        # Resource gathering
│           ├── Map/              # Map SDK + territory overlay
│           ├── WorldSeed/        # Pre-seeded ruins & landmarks
│           ├── Notifications/    # Push & in-game alerts
│           ├── Tutorial/         # Onboarding system
│           ├── Data/             # Persistence & offline
│           ├── IAP/              # In-app purchases
│           ├── Audio/            # Sound, music, ambient
│           ├── Localization/     # Multi-language i18n
│           ├── Leaderboard/      # Rankings
│           ├── Achievements/     # Progress tracking
│           ├── DailyRewards/     # Login streaks
│           ├── Privacy/          # GDPR, age gate, consent
│           ├── Moderation/       # Content filtering & reports
│           ├── Cosmetics/        # Shop & customization
│           ├── Monitoring/       # Performance & crash reporting
│           ├── UI/               # HUD & UI controllers
│           ├── Config/           # App configuration
│           └── Demo/             # Test scenes
│
└── backend/                      # Firebase Backend
    ├── functions/                # Cloud Functions (TypeScript)
    │   └── src/
    │       ├── index.ts          # Function exports
    │       ├── types/            # TypeScript interfaces
    │       ├── combat.ts         # Combat system
    │       ├── progression.ts    # XP & leveling
    │       ├── alliance.ts       # Alliance management
    │       ├── notifications.ts  # Push notifications
    │       ├── push.ts           # FCM push system
    │       ├── iap.ts            # In-app purchases
    │       ├── world-events.ts   # FOMO world events
    │       ├── season-pass.ts    # Battle pass system
    │       ├── friends.ts        # Social features
    │       ├── anticheat.ts      # Location validation
    │       ├── analytics.ts      # Telemetry & A/B tests
    │       ├── map-api.ts        # Real-time map tiles
    │       ├── referrals.ts      # Viral growth
    │       ├── chat.ts           # Chat infrastructure
    │       ├── world-seed.ts     # Pre-seeded world content
    │       ├── gdpr.ts           # GDPR compliance & privacy
    │       ├── moderation.ts     # Content moderation & bans
    │       └── cosmetics.ts      # Shop & cosmetic items
    ├── firestore.rules           # Security rules (30+ collections)
    └── firestore.indexes.json    # 50+ composite indexes
```

---

## 📚 Comprehensive Documentation

For detailed requirements, workplan, and Unity AI prompts, see:

| Document | Description |
|----------|-------------|
| **[docs/GAME_DESIGN.md](docs/GAME_DESIGN.md)** | 🎮 **Complete game design spec** - Territory, combat, siege, alliances, monetization, safety |
| **[docs/REQUIREMENTS.md](docs/REQUIREMENTS.md)** | 🎯 **Master planning document** - Full requirements, workplan, 13 Unity AI prompts, testing strategy |
| **[docs/PC_HYBRID_DESIGN.md](docs/PC_HYBRID_DESIGN.md)** | 🖥️ **PC client design** - Hybrid mode, PC features, cross-platform sync |
| [docs/VISION.md](docs/VISION.md) | Product vision and strategy |
| [docs/TECHNICAL_ARCHITECTURE.md](docs/TECHNICAL_ARCHITECTURE.md) | System design and tech stack |
| [docs/CODE_ARCHITECTURE.md](docs/CODE_ARCHITECTURE.md) | Complete code structure guide |
| [docs/ROADMAP.md](docs/ROADMAP.md) | Development milestones |
| [BUILD_SPRINT_SUMMARY.md](BUILD_SPRINT_SUMMARY.md) | Latest build sprint details |
| [admin-dashboard/README.md](admin-dashboard/README.md) | Admin dashboard setup guide |

### Current Status (January 2026)
- ✅ **85+ C# Scripts** - All game systems + PC client coded and ready
- ✅ **PC Hybrid Client** - 18 scripts for desktop gameplay
- ✅ **Firebase Backend** - 100% complete with 20 Cloud Function modules
- ✅ **Admin Dashboard** - Full React web dashboard with 10 pages
- ✅ **50+ Firestore Indexes** - Optimized for all query patterns
- ✅ **Security Rules** - 30+ collections protected
- ✅ **80+ Unit Tests** - Territory, Alliance, World Events, Season Pass, IAP
- ✅ **In-App Purchases** - Complete IAP system with receipt validation
- ✅ **Push Notifications** - FCM with preferences and scheduled notifications
- ✅ **Audio System** - SFX, music, ambient with pooling and crossfade
- ✅ **Onboarding Tutorial** - Step-based tutorial with rewards
- ✅ **Data Persistence** - Local caching with encryption
- ✅ **Offline Mode** - Action queuing with sync
- ✅ **Localization** - Multi-language support (15 languages)
- ✅ **AR Session Control** - Advanced anchor management with persistence
- ✅ **Map SDK** - Multi-provider tiles (OSM, Mapbox, Google Maps, MapTiler)
- ✅ **World Seed System** - Pre-populated ruins, landmarks, resource hotspots
- ✅ **Building Templates** - 15+ pre-designed structures (watchtower to tower complex)
- ✅ **GDPR Compliance** - Data export/deletion, consent management, age gate
- ✅ **Content Moderation** - Profanity filter, reports, mute/ban system
- ✅ **Cosmetics Shop** - 10 categories, rotating shop, gems/coins currency
- ✅ **Performance Monitoring** - FPS/memory tracking, Crashlytics, analytics
- ✅ **Backend Sprint 1-5** - Turn-based combat, siege system, blueprints complete
- ⏳ **Unity Integration** - Pending (see Sprint 6 tasks below)

---

## 🎮 Unity AI Integration Tasks (Sprint 6)

The backend Cloud Functions are complete. The following Unity implementation tasks are ready for the Unity AI to build.

### Data Models Created (Ready to Use)

These C# files are already created in `unity/ApexCitadels/Assets/Scripts/Data/`:

| File | Contents |
|------|----------|
| `BattleTypes.cs` | Troop, TroopType, TerritoryState, BattleFormation, ScheduledBattle, BattleResult, etc. |
| `ResourceTypes.cs` | ResourceType, ResourceCost, ResourceConfig |
| `GameTypes.cs` | Blueprint, AllianceWar, BattleConfig, TerritoryConfig constants |
| `TroopConfig.cs` | TroopDefinition for all 6 troop types with stats and counters |

### Service Interfaces Created (Ready to Implement)

`unity/ApexCitadels/Assets/Scripts/Backend/ICloudFunctions.cs` defines interfaces:

| Interface | Backend File | Functions |
|-----------|--------------|-----------|
| `IBattleService` | battle.ts | scheduleBattle, setBattleFormation, executeBattle, reclaimTerritory, trainTroops |
| `IProtectionService` | protection.ts | getMyProtectionStatus, checkTerritoryAttackable, waiveNewcomerShield |
| `IBlueprintService` | blueprint.ts | saveBlueprint, getMyBlueprints, applyBlueprint, previewBlueprintCost |
| `IAllianceWarService` | alliance.ts | declareWar, cancelWar, getWarStatus |
| `ILocationService` | location-utils.ts | getLocationInfo, setAreaDensity |

---

### 📋 Unity AI Task List

#### Task 1: Implement Cloud Function Services

Create Firebase implementations for the service interfaces:

```
Create implementations of the service interfaces in ICloudFunctions.cs.

Files to create in Assets/Scripts/Backend/:
1. BattleService.cs - Implements IBattleService
2. ProtectionService.cs - Implements IProtectionService  
3. BlueprintService.cs - Implements IBlueprintService
4. AllianceWarService.cs - Implements IAllianceWarService
5. LocationService.cs - Implements ILocationService

Each service should:
- Use Firebase.Functions.FirebaseFunctions.DefaultInstance
- Call HttpsCallable with the function name
- Deserialize JSON responses into the Data types
- Handle errors appropriately

Example pattern:
var functions = FirebaseFunctions.DefaultInstance;
var callable = functions.GetHttpsCallable("scheduleBattle");
var result = await callable.CallAsync(new Dictionary<string, object> {
    { "territoryId", territoryId },
    { "participation", participation.ToString().ToLower() }
});
var json = result.Data.ToString();
return JsonUtility.FromJson<ScheduledBattle>(json);
```

#### Task 2: Update Territory.cs for Siege System

Update the existing Territory.cs to support the 4-state siege system:

```
Update Assets/Scripts/Territory/Territory.cs to support the siege system.

Add these properties:
- TerritoryState State (enum from Data/BattleTypes.cs)
- int BattleLosses (0-3)
- DateTime? LastStateChangeAt
- DateTime? FallenAt
- string PreviousOwnerId
- string BlueprintId
- LocationDensity Density

Add these methods:
- float GetProductionMultiplier() - Returns multiplier based on state
- bool CanBeReclaimed(string userId) - Check if user can reclaim (24hr cooldown)
- void UpdateStateFromBackend(TerritoryState state, int losses)

Remove/deprecate:
- IsContested (replaced by State == TerritoryState.Contested)
- ContestingPlayerId (use PreviousOwnerId for reclaim eligibility)
```

#### Task 3: Update CombatManager for Turn-Based Battles

The existing CombatManager.cs uses instant damage. Update for turn-based:

```
Update Assets/Scripts/Combat/CombatManager.cs for turn-based battles.

Changes needed:
1. Remove instant attack methods (QuickStrike, HeavyAttack, Siege)
2. Add battle scheduling flow:
   - ScheduleAttack(territoryId) -> calls IBattleService.ScheduleBattleAsync
   - SetFormation(battleId, troops) -> calls IBattleService.SetBattleFormationAsync
   - GetMyBattles() -> returns scheduled battles

3. Add troop management:
   - UserTroops CurrentTroops
   - TrainTroops(TroopType, count)
   - CollectTrainedTroops()

4. Add events:
   - OnBattleScheduled(ScheduledBattle)
   - OnBattleStarting(ScheduledBattle) - 5 min warning
   - OnBattleCompleted(BattleResult)
   - OnTroopsReady(UserTroops)

5. Add real-time battle listener for when battle executes
```

#### Task 4: Create Battle UI

```
Create battle-related UI in Assets/Scripts/UI/Battle/:

1. BattleSchedulePanel.cs
   - Shows upcoming battles (attacker and defender)
   - Countdown timer to battle start
   - Button to set formation

2. FormationSetupPanel.cs
   - Shows available troops from UserTroops
   - Drag-drop or tap to add troops to formation
   - Power calculation display
   - Strategy selector (aggressive/defensive/balanced)
   - Confirm button

3. BattleResultPanel.cs
   - Animated battle replay (show rounds)
   - Winner announcement
   - Casualties for both sides
   - XP and loot gained
   - Territory state change notification

4. TroopTrainingPanel.cs
   - List of troop types with costs
   - Training queue with progress bars
   - Collect button when ready
   - Current troop counts display
```

#### Task 5: Create Blueprint UI

```
Create blueprint management UI in Assets/Scripts/UI/Blueprint/:

1. BlueprintListPanel.cs
   - List of saved blueprints (manual + auto-saves)
   - Preview thumbnail for each
   - Building count and cost display
   - Delete/Rename buttons
   - "Save Current" button

2. BlueprintDetailPanel.cs
   - Full building list with block types
   - Total cost vs rebuild cost (with discount)
   - "Apply to Territory" button
   - Can afford indicator

3. SaveBlueprintDialog.cs
   - Name input (50 char max)
   - Description input (200 char max)
   - Save/Cancel buttons

4. ApplyBlueprintDialog.cs
   - Cost preview from IBlueprintService.PreviewBlueprintCostAsync
   - Resource comparison (have vs need)
   - Missing resources highlighted
   - Confirm/Cancel buttons
```

#### Task 6: Create Protection Status UI

```
Create protection status display in Assets/Scripts/UI/Protection/:

1. ProtectionStatusWidget.cs (persistent HUD element)
   - Shield icon when newcomer shield active
   - Days remaining countdown
   - "Waive Shield" button (with confirmation)
   - Activity bonus indicator (+25% or +50%)

2. AttackBlockedDialog.cs
   - Shows when player tries to attack protected territory
   - Reason display (shield, cooldown, etc.)
   - "OK" dismissal

3. NewcomerShieldWaiveDialog.cs
   - Warning about losing protection
   - Benefits of keeping vs waiving
   - Confirm/Cancel
```

#### Task 7: Create Alliance War UI

```
Create alliance war UI in Assets/Scripts/UI/Alliance/:

1. WarDashboardPanel.cs
   - Current war status (if any)
   - Phase indicator (Warning/Battle/Peace)
   - Time remaining in current phase
   - Score display (Challenger vs Defender)
   - Participant list with contributions

2. DeclareWarPanel.cs
   - Alliance selector/search
   - War cost display
   - Warning about 24hr wait
   - Declare button

3. WarTimelineWidget.cs
   - Visual timeline: Warning → Battle → Peace
   - Current position indicator
   - Phase durations (24hr → 48hr → 72hr)
```

#### Task 8: Update TerritoryManager for New Features

```
Update Assets/Scripts/Territory/TerritoryManager.cs:

1. Add blueprint integration:
   - SaveCurrentTerritoryBlueprint()
   - ApplyBlueprintToTerritory(blueprintId)

2. Add reclaim support:
   - GetReclaimableTerritories() - fallen territories user can reclaim
   - ReclaimTerritory(territoryId, blueprintId)
   - CanReclaimTerritory(territoryId) - check 24hr cooldown

3. Add dynamic radius:
   - GetTerritoryRadius(latitude, longitude) - uses ILocationService
   - Territory creation should use density-based radius

4. Update territory visuals for state:
   - Different colors/effects for Contested/Vulnerable/Fallen
   - Damage overlay when state degrades
```

---

### 🔧 Backend Cloud Functions Reference

The Unity implementation should call these Firebase Cloud Functions:

#### Battle Functions (battle.ts)
| Function | Parameters | Returns |
|----------|------------|---------|
| `scheduleBattle` | `{ territoryId, participation }` | `ScheduledBattle` |
| `setBattleFormation` | `{ battleId, troops[], strategy }` | `BattleFormation` |
| `executeBattle` | `{ battleId }` | `BattleResult` |
| `reclaimTerritory` | `{ territoryId, blueprintId? }` | `{ success, territory }` |
| `trainTroops` | `{ troopType, count }` | `TrainingQueueItem` |
| `collectTrainedTroops` | `{}` | `UserTroops` |

#### Protection Functions (protection.ts)
| Function | Parameters | Returns |
|----------|------------|---------|
| `getMyProtectionStatus` | `{}` | `ProtectionStatus` |
| `checkTerritoryAttackable` | `{ territoryId }` | `{ canAttack, reason }` |
| `waiveNewcomerShield` | `{}` | `{ success }` |
| `grantTemporaryShield` | `{ hours }` | `{ expiresAt }` |

#### Blueprint Functions (blueprint.ts)
| Function | Parameters | Returns |
|----------|------------|---------|
| `saveBlueprint` | `{ territoryId, name, description? }` | `Blueprint` |
| `getMyBlueprints` | `{}` | `BlueprintListResponse` |
| `getBlueprintDetails` | `{ blueprintId }` | `Blueprint` |
| `deleteBlueprint` | `{ blueprintId }` | `{ success }` |
| `renameBlueprint` | `{ blueprintId, name, description? }` | `{ success }` |
| `applyBlueprint` | `{ territoryId, blueprintId }` | `{ success, buildingsPlaced, spent }` |
| `previewBlueprintCost` | `{ blueprintId }` | `BlueprintCostPreview` |

#### Alliance War Functions (alliance.ts)
| Function | Parameters | Returns |
|----------|------------|---------|
| `declareAllianceWar` | `{ targetAllianceId }` | `AllianceWar` |
| `cancelAllianceWar` | `{ warId }` | `{ success }` |
| `getAllianceWarStatus` | `{}` | `AllianceWar` |

#### Location Functions (location-utils.ts)
| Function | Parameters | Returns |
|----------|------------|---------|
| `getLocationInfo` | `{ latitude, longitude }` | `{ density, territoryRadius }` |

---

## 🚀 Quick Start

### Prerequisites

- **Unity 6** (6000.x)
- **Node.js 18+** (for Firebase functions)
- **Firebase CLI** (`npm install -g firebase-tools`)
- **Google Cloud account** (for ARCore Geospatial API)
- **Android device** with ARCore support OR **iOS device** with ARKit

### 1️⃣ Clone the Repository

```bash
git clone https://github.com/rob637/Apex.git
cd Apex
```

### 2️⃣ Set Up Firebase

```bash
cd backend
npm install -g firebase-tools
firebase login
firebase projects:create apex-citadels  # Or use existing project
firebase deploy
```

### 3️⃣ Configure ARCore Geospatial API

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Enable **ARCore API**
3. Create an API key
4. Add the key to your Unity project

### 4️⃣ Open Unity Project

1. Open Unity Hub
2. Add project: `unity/ApexCitadels`
3. Install required packages (AR Foundation)
4. **Firebase is optional:** Project compiles without Firebase SDK
   - To enable Firebase: Import Firebase packages and add `FIREBASE_ENABLED` to Scripting Define Symbols
   - Without Firebase: Uses mock data for testing UI/gameplay
5. Build to your device

### 5️⃣ Desktop Testing (Windows)

For testing without a mobile device:
1. Open `unity/ApexCitadels` in Unity
2. Disable AR Session and XR Origin in Hierarchy
3. Edit → Project Settings → XR Plug-in Management → Uncheck all loaders for Windows
4. Ensure your Camera is tagged "MainCamera"
5. Build and Run for Windows

---

## 🔑 The Persistent Cube Test

The entire game rests on one core technology: **spatial persistence**.

```
┌─────────────────────────────────────────────────────────┐
│                                                         │
│   Device A                        Device B              │
│   ┌─────────┐                    ┌─────────┐           │
│   │ 📱      │                    │ 📱      │           │
│   │  ┌───┐  │     Cloud          │  ┌───┐  │           │
│   │  │ 🟦 │  │ ──────────────▶   │  │ 🟦 │  │           │
│   │  └───┘  │   Anchor Data      │  └───┘  │           │
│   │   📍    │                    │   📍    │           │
│   └─────────┘                    └─────────┘           │
│                                                         │
│   User A places cube             User B sees SAME cube │
│   at 40.7128°N, 74.0060°W        at EXACT location     │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

✅ **STATUS: WORKING** - Desktop and mobile placement confirmed!

---

## 🛠️ Tech Stack

| Layer | Technology | Purpose |
|-------|------------|---------|
| **AR Engine** | Unity AR Foundation | Cross-platform AR |
| **Geospatial** | ARCore Geospatial API | Centimeter-accurate positioning |
| **Backend** | Firebase Cloud Functions v2 | Serverless TypeScript functions |
| **Database** | Cloud Firestore | Real-time game state persistence |
| **Auth** | Firebase Authentication | User accounts & sessions |
| **Admin** | React + Vite + Material-UI | Web dashboard for operations |
| **Analytics** | Custom + Firebase Analytics | Event tracking & A/B testing |
| **Real-time** | Firestore Listeners | Live updates & chat |
| **Maps** | Geohash encoding | Efficient location queries |

---

## 🎯 Feature Status

### Core Systems ✅
| Feature | Backend | Unity | Status |
|---------|---------|-------|--------|
| Territory Control | ✅ | ✅ | Ready |
| Building System | ✅ | ✅ | Ready |
| Combat Mechanics | ✅ | ✅ | Ready |
| Alliance/Teams | ✅ | ✅ | Ready |
| Resource Gathering | ✅ | ✅ | Ready |
| Player Progression | ✅ | ✅ | Ready |

### Engagement Systems ✅
| Feature | Backend | Unity | Status |
|---------|---------|-------|--------|
| World Events (FOMO) | ✅ | ✅ | Ready |
| Season Pass (100 tiers) | ✅ | ✅ | Ready |
| Friends & Social | ✅ | ✅ | Ready |
| Real-time Chat | ✅ | ✅ | Ready |
| Referral System | ✅ | ✅ | Ready |
| Daily Rewards | ✅ | ✅ | Ready |
| Achievements | ✅ | ✅ | Ready |
| Leaderboards | ✅ | ✅ | Ready |

### Infrastructure ✅
| Feature | Backend | Admin | Status |
|---------|---------|-------|--------|
| Analytics & A/B Tests | ✅ | ✅ | Ready |
| Anti-cheat & Validation | ✅ | ✅ | Ready |
| Push Notifications | ✅ | - | Ready |
| Map Tile API | ✅ | - | Ready |
| Admin Dashboard | - | ✅ | Ready |
| Moderation Tools | ✅ | ✅ | Ready |

### Monetization & UX ✅
| Feature | Backend | Unity | Status |
|---------|---------|-------|--------|
| In-App Purchases | ✅ | ✅ | Ready |
| Push Notifications | ✅ | ✅ | Ready |
| Onboarding Tutorial | ✅ | ✅ | Ready |
| Data Persistence | - | ✅ | Ready |
| Offline Mode | - | ✅ | Ready |
| Audio System | - | ✅ | Ready |
| Localization (i18n) | - | ✅ | Ready |
| AR Spatial Anchors | - | ✅ | Ready |
| Map SDK Integration | ✅ | ✅ | Ready |
| World Seed System | ✅ | ✅ | Ready |
| Building Templates | - | ✅ | Ready |
| Cosmetics Shop | ✅ | ✅ | Ready |

### Compliance & Safety ✅
| Feature | Backend | Unity | Status |
|---------|---------|-------|--------|
| GDPR Compliance | ✅ | ✅ | Ready |
| Age Gate (COPPA) | ✅ | ✅ | Ready |
| Consent Management | ✅ | ✅ | Ready |
| Content Moderation | ✅ | ✅ | Ready |
| Performance Monitoring | ✅ | ✅ | Ready |

### Remaining Work ⏳
| Feature | Priority | Notes |
|---------|----------|-------|
| Real-time Battles (Photon) | Medium | Complex multiplayer |
| AR Occlusion | Low | ARCore depth APIs |
| XR Glasses Support | Low | Future platform |

---

## 🎮 Unity Setup Instructions

**IMPORTANT:** The scripts in `Assets/Scripts/` are ready but need to be wired up in Unity. Follow these prompts with Unity AI or do manually.

### Step 1: Create Game Manager Object

In your main scene, create an empty GameObject called `GameManager` and add these core scripts:
- `GameManager.cs` (Core/)
- `TerritoryManager.cs`
- `BuildingManager.cs`
- `PlayerManager.cs`
- `CombatManager.cs`
- `AllianceManager.cs`
- `ResourceManager.cs`
- `NotificationManager.cs`
- `LeaderboardManager.cs`
- `AchievementManager.cs`
- `DailyRewardManager.cs`
- `GDPRManager.cs` (Privacy/)
- `ContentModerationManager.cs` (Moderation/)
- `CosmeticsShopManager.cs` (Cosmetics/)
- `PerformanceMonitor.cs` (Monitoring/)

### Step 2: Create Engagement Systems Object

Create another GameObject called `EngagementSystems` and add:
- `WorldEventManager.cs` (WorldEvents/)
- `SeasonPassManager.cs` (SeasonPass/)
- `FriendsManager.cs` (Social/)
- `ChatManager.cs` (Chat/)
- `ReferralManager.cs` (Referrals/)
- `AnalyticsManager.cs` (Analytics/)
- `AntiCheatManager.cs` (AntiCheat/)

### Step 3: Create UI Controllers

Add to your main UI Canvas:
- `GameUIController.cs` (UI/)
- `GameHUDController.cs` (UI/)

**Unity AI Prompt:**
```
Create an empty GameObject called "GameManager" in the hierarchy. Add ALL the following script components:

Core Scripts:
- ApexCitadels.Core.GameManager
- ApexCitadels.Territory.TerritoryManager
- ApexCitadels.Building.BuildingManager
- ApexCitadels.Player.PlayerManager
- ApexCitadels.Combat.CombatManager
- ApexCitadels.Alliance.AllianceManager
- ApexCitadels.Resources.ResourceManager
- ApexCitadels.Notifications.NotificationManager
- ApexCitadels.Leaderboard.LeaderboardManager
- ApexCitadels.Achievements.AchievementManager
- ApexCitadels.DailyRewards.DailyRewardManager

Engagement Scripts:
- ApexCitadels.WorldEvents.WorldEventManager
- ApexCitadels.SeasonPass.SeasonPassManager
- ApexCitadels.Social.FriendsManager
- ApexCitadels.Chat.ChatManager
- ApexCitadels.Referrals.ReferralManager
- ApexCitadels.Analytics.AnalyticsManager
- ApexCitadels.AntiCheat.AntiCheatManager

All scripts use singleton pattern and will self-initialize.
```

### Step 4: Create Main Game HUD

**Unity AI Prompt:**
```
Create a game HUD Canvas with these elements for GameHUDController:

TOP BAR:
- Player name text, level badge, XP progress bar
- Currency displays (coins, gems icons with amounts)
- Settings gear button

EVENT BANNER (collapsible):
- Event icon, event name text, countdown timer
- "Join" button, reward preview

SEASON PASS WIDGET:
- Current tier badge, XP bar to next tier
- "Claim Reward" button (shows when available)
- New reward notification dot

SOCIAL WIDGET (bottom left):
- Friends button with pending request badge
- Chat button with unread message badge
- Gift button with pending gifts badge

QUICK ACTIONS (bottom right):
- Share/Referral button
- Daily reward button with availability indicator

NOTIFICATION TOAST AREA (top, below top bar):
- Spawn point for toast notifications

Assign all elements to GameHUDController in inspector.
```

### Step 5: Create World Event Panel

**Unity AI Prompt:**
```
Create a full-screen panel "WorldEventsPanel" with:

HEADER:
- "World Events" title
- Close button (X)

EVENT LIST (ScrollView):
- For each event show:
  - Event type icon (resource rush, territory war, etc.)
  - Event name and description
  - Time remaining countdown
  - Current multipliers (2x XP, 1.5x Resources, etc.)
  - Progress bar (if community goal)
  - "Join Event" / "View Details" button
  - Reward preview icons

ACTIVE EVENTS TAB / UPCOMING EVENTS TAB / COMPLETED TAB

Wire to WorldEventManager events:
- OnEventsUpdated
- OnEventStarted
- OnEventEnded
- OnParticipationUpdated
```

### Step 6: Create Season Pass Panel

**Unity AI Prompt:**
```
Create a Season Pass panel with:

HEADER:
- Season name and number
- Days remaining countdown
- "Buy Premium Pass" button (if not owned)

REWARD TRACK (horizontal ScrollView):
- 100 tier slots
- Each slot shows:
  - Tier number
  - Free reward (always visible)
  - Premium reward (locked/unlocked based on pass)
  - Claimed checkmark if collected
  - Current tier highlighted

PROGRESS BAR:
- Current XP / XP to next tier
- "Claim" button if rewards available

CHALLENGES TAB:
- Daily challenges (3)
- Weekly challenges (5)
- Each shows: description, progress, XP reward

Wire to SeasonPassManager events.
```

### Step 7: Create Chat Panel

**Unity AI Prompt:**
```
Create a Chat panel with:

CHANNEL TABS:
- Global, Alliance, Direct Messages

CHANNEL LIST (for DMs):
- Contact name, last message preview, unread badge

MESSAGE AREA:
- Scrollable message list
- Each message: avatar, name, timestamp, content
- System messages styled differently
- Typing indicator at bottom

INPUT AREA:
- Text input field
- Send button
- Emoji button

Wire to ChatManager events:
- OnChannelsUpdated
- OnNewMessage
- OnTypingUsersUpdated
```

### Step 8: Create Friends Panel

**Unity AI Prompt:**
```
Create a Friends panel with tabs:

FRIENDS TAB:
- Search bar
- Friend list with:
  - Avatar, name, level
  - Online status indicator
  - "Send Gift" button
  - "Remove" button

REQUESTS TAB:
- Incoming requests with Accept/Decline
- Outgoing requests (pending)

GIFTS TAB:
- Pending gifts to claim
- "Claim All" button

ACTIVITY FEED TAB:
- Recent friend activities
- Like button on each

Wire to FriendsManager events.
```

### Step 9: Create Referral Panel

**Unity AI Prompt:**
```
Create a Referral panel with:

MY CODE SECTION:
- Large referral code display
- "Copy Code" button
- "Share" button (native share)
- QR code (optional)

STATS:
- Total referrals count
- Rewards earned

MILESTONES:
- Progress bar to next milestone
- Milestone rewards preview
- "Claim" button when reached

REFERRED FRIENDS LIST:
- Avatar, name, level, rewards generated

Wire to ReferralManager events.
```

### Script Dependencies Diagram

```
GameManager (Core initialization)
    ├── PlayerManager (auth, resources, XP)
    ├── TerritoryManager (claim, attack, defend)
    ├── BuildingManager (place blocks)
    ├── CombatManager (attack system)
    ├── AllianceManager (teams)
    ├── ResourceManager (harvesting)
    ├── NotificationManager (alerts)
    ├── PushNotificationManager (FCM push)
    ├── LeaderboardManager (rankings)
    ├── AchievementManager (progress)
    ├── DailyRewardManager (streaks)
    │
    ├── WorldEventManager (FOMO events)
    ├── SeasonPassManager (battle pass)
    ├── FriendsManager (social)
    ├── ChatManager (messaging)
    ├── ReferralManager (viral growth)
    ├── AnalyticsManager (tracking)
    ├── AntiCheatManager (validation)
    │
    ├── TutorialManager (onboarding)
    ├── IAPManager (in-app purchases)
    ├── DataPersistenceManager (local cache)
    └── OfflineModeManager (offline sync)

All managers use Singleton pattern - access via ManagerName.Instance
All managers fire C# events for UI updates
GameManager.Instance initializes Firebase and starts all systems
```

### Step 10: Create Push Notification Settings Panel

**Unity AI Prompt:**
```
Create a Notification Settings panel with:

HEADER:
- "Notifications" title
- Close button (X)

MASTER TOGGLE:
- "Enable Notifications" main toggle
- Status indicator showing push token state

CATEGORY TOGGLES (each with description):
- Combat Alerts: "Get notified when attacked"
- Alliance Updates: "Alliance invites, wars, chat"
- Social: "Friend requests, messages, gifts"
- Rewards: "Daily rewards, achievements, milestones"
- Events: "World events, time-limited offers"
- Marketing: "News, updates, promotions"

QUIET HOURS:
- Enable quiet hours toggle
- Start hour dropdown (0-23)
- End hour dropdown (0-23)
- "Don't disturb during sleep" hint

FOOTER:
- "Test Notification" button
- Token status text (for debugging)

Wire to PushNotificationManager:
- Use PushNotificationManager.Instance.GetSettings()
- Call PushNotificationManager.Instance.UpdatePreferences()
- Call PushNotificationManager.Instance.SendTestNotification()
```

### Step 11: Create Tutorial UI

**Unity AI Prompt:**
```
Create a Tutorial overlay system with:

DIALOG BOX (bottom of screen):
- Character portrait image (left side)
- Character name text
- Dialog text (with typewriter effect)
- "Next" / "Got it!" button
- "Skip" button (smaller, corner)

HIGHLIGHT OVERLAY:
- Semi-transparent dark mask covering screen
- Cutout hole for highlighted UI element
- Pulse animation on highlighted element
- Arrow pointing to target

PROGRESS BAR (top):
- Current step / total steps
- Step name text
- Small dot indicators for each step

REWARD POPUP:
- Shows when tutorial step grants reward
- Reward icon, name, amount
- "Claim" button with sparkle effect

COMPLETION SCREEN:
- "Tutorial Complete!" header
- Summary of rewards earned
- "Start Playing!" button
- Confetti particles

Wire to TutorialManager and TutorialUI:
- TutorialManager.Instance.OnStepStarted
- TutorialManager.Instance.OnStepCompleted
- TutorialManager.Instance.OnTutorialCompleted
- TutorialUI elements for ShowDialog, ShowHighlight, etc.
```

### Step 12: Create IAP Store Panel

**Unity AI Prompt:**
```
Create an In-App Purchase store panel with:

HEADER:
- "Store" title
- Close button
- Player's current gem/coin balance display

TAB BAR:
- Currency (gems, coins)
- Bundles (starter, value packs)
- Season Pass
- Subscriptions

PRODUCT GRID (ScrollView):
Each product card shows:
- Product icon/image
- Product name
- Description text
- Price button (local currency)
- "Best Value" badge (if applicable)
- "Limited Time" badge (if applicable)
- Bonus percentage indicator ("+20% bonus")

SUBSCRIPTION SECTION:
- VIP tier cards with benefits list
- Monthly/annual toggle
- Current subscription status

PURCHASE CONFIRMATION POPUP:
- Product preview
- "Confirm Purchase" button
- "Cancel" button
- Legal text

PROCESSING OVERLAY:
- Spinner/loading animation
- "Processing purchase..." text

Wire to IAPManager:
- IAPManager.Instance.GetAllProducts()
- IAPManager.Instance.PurchaseProduct(productId)
- IAPManager.Instance.RestorePurchases()
- IAPManager.Instance.OnPurchaseCompleted event
```

### Testing Checklist

Before deploying to device:
- [ ] GameManager object exists with all manager scripts
- [ ] EngagementSystems object exists with engagement scripts
- [ ] UI Canvas connected to GameUIController and GameHUDController
- [ ] Block prefabs assigned to BuildingManager
- [ ] Firebase configured and initialized
- [ ] XR settings correct for target platform
- [ ] Scenes added to Build Settings
- [ ] AR Session and XR Origin in scene (for mobile)
- [ ] DesktopFallbackCamera in scene (for Windows testing)
- [ ] PushNotificationManager configured with FCM
- [ ] TutorialManager has tutorial steps defined
- [ ] IAPManager has product catalog configured
- [ ] DataPersistenceManager initialized early in scene
- [ ] AudioManager configured with Audio Mixer
- [ ] SFXLibrary and MusicLibrary ScriptableObjects created

### Required Packages (Package Manager)

Make sure these packages are installed:
- `com.unity.xr.arfoundation` (AR Foundation)
- `com.unity.xr.arcore` (ARCore XR Plugin)
- `com.unity.xr.arkit` (ARKit XR Plugin - for iOS)
- `com.google.ar.core.arfoundation.extensions` (ARCore Extensions)
- `com.unity.textmeshpro` (TextMeshPro for UI)
- `com.unity.purchasing` (Unity IAP for in-app purchases)
- Firebase SDK packages (via .unitypackage from Firebase website)
  - Firebase Auth
  - Firebase Firestore
  - Firebase Functions
  - Firebase Messaging (for push notifications)
- Newtonsoft.Json (for JSON serialization)

---

## 🔔 Push Notifications System

### Overview

The push notification system uses Firebase Cloud Messaging (FCM) to deliver real-time alerts to users across iOS and Android devices.

### Features

- **27 Notification Types** - Combat, alliance, social, rewards, events, economy, system
- **User Preferences** - Per-category toggles with quiet hours
- **Topic Messaging** - Alliance-wide broadcasts
- **Scheduled Notifications** - Daily reward reminders, welcome back, event reminders
- **Automatic Triggers** - Territory attacks, friend requests, alliance invites, level ups
- **Token Management** - Multi-device support with automatic cleanup

### Backend Functions (`push.ts`)

| Function | Type | Purpose |
|----------|------|---------|
| `registerPushToken` | Callable | Register device FCM token |
| `unregisterPushToken` | Callable | Remove device token |
| `updateNotificationPreferences` | Callable | Save user preferences |
| `getNotificationSettings` | Callable | Retrieve current settings |
| `sendPushNotification` | Callable | Send to specific user |
| `broadcastNotification` | Callable | Send to all users |
| `subscribeToAllianceTopic` | Callable | Join alliance topic |
| `unsubscribeFromAllianceTopic` | Callable | Leave alliance topic |
| `onTerritoryAttacked` | Trigger | Auto-notify on attack |
| `onFriendRequestCreated` | Trigger | Auto-notify on friend request |
| `onAllianceInviteCreated` | Trigger | Auto-notify on invite |
| `onAllianceWarStarted` | Trigger | Notify all alliance members |
| `onUserLevelUp` | Trigger | Notify on level up |
| `sendDailyRewardReminder` | Scheduled | 6PM UTC daily |
| `sendWelcomeBackNotification` | Scheduled | 10AM UTC for inactive users |
| `sendWorldEventReminders` | Scheduled | Hourly event checks |
| `sendSeasonEndingReminder` | Scheduled | 12PM UTC season alerts |

### Unity Setup

1. **Import Firebase Messaging SDK** (via .unitypackage)
2. **Add PushNotificationManager.cs** to a persistent GameObject
3. **Configure Android notification channels** (handled automatically)
4. **Request iOS notification permissions** (handled by manager)
5. **Wire NotificationSettingsUI** to settings panel

### Notification Categories

| Category | Description | Default |
|----------|-------------|---------|
| `combat` | Territory attacks, raid results | ✅ On |
| `alliance` | Invites, wars, leadership | ✅ On |
| `social` | Friend requests, messages, gifts | ✅ On |
| `rewards` | Daily rewards, achievements | ✅ On |
| `events` | World events, limited offers | ✅ On |
| `marketing` | News, promotions | ❌ Off |

---

## 📖 Onboarding Tutorial System

### Overview

A step-based tutorial system that guides new players through game mechanics with dialog, highlights, and rewards.

### Features

- **11 Step Types** - Welcome, dialog, highlight, action, AR setup, building, resources, combat, alliance, rewards, complete
- **10 Requirement Types** - For step completion validation
- **Character System** - Named characters with portraits for dialog
- **Step Rewards** - Grant resources/items on step completion
- **Progress Persistence** - Local + server sync
- **Skip Functionality** - Skip step or entire tutorial
- **Analytics Integration** - Track tutorial funnel

### Tutorial Step Types

| Type | Description |
|------|-------------|
| `Welcome` | Introduction screen |
| `Dialog` | Character speech with text |
| `Highlight` | Point to UI element |
| `Action` | Wait for user action |
| `ARSetup` | AR initialization steps |
| `BuildCitadel` | First building placement |
| `CollectResources` | Resource gathering |
| `Attack` | Combat introduction |
| `JoinAlliance` | Alliance tutorial |
| `Reward` | Grant reward |
| `Complete` | Celebration screen |

### Default Tutorial Flow

```
1. Welcome → "Welcome to Apex Citadels!"
2. ARSetup → Initialize AR camera
3. Dialog → Explain territory claiming
4. BuildCitadel → Place first structure
5. Dialog → Explain resources
6. CollectResources → Gather resources
7. Dialog → Explain combat
8. Reward → Grant starter pack
9. Complete → "You're ready!"
```

### Unity Setup

1. **Add TutorialManager.cs** to GameManager object
2. **Add TutorialUI.cs** to UI Canvas
3. **Configure tutorial steps** in inspector or code
4. **Assign character portraits** for dialog
5. **Wire UI elements** (dialog panel, highlight overlay, progress bar)

### API Usage

```csharp
// Start tutorial for new users
TutorialManager.Instance.StartTutorial();

// Check if tutorial completed
bool done = TutorialManager.Instance.IsTutorialCompleted();

// Skip current step
TutorialManager.Instance.SkipCurrentStep();

// Skip entire tutorial
TutorialManager.Instance.SkipTutorial();

// Listen to events
TutorialManager.Instance.OnStepCompleted += (stepId) => { };
TutorialManager.Instance.OnTutorialCompleted += () => { };
```

---

## 💳 In-App Purchases (IAP) System

### Overview

Complete monetization system using Unity IAP with server-side receipt validation via Firebase Cloud Functions.

### Features

- **5 Product Types** - Currency, bundles, season pass, subscriptions, special offers
- **Receipt Validation** - Server-side validation for iOS App Store and Google Play
- **Purchase History** - Track all transactions with analytics
- **Subscription Management** - VIP tiers with recurring billing
- **Fraud Prevention** - Duplicate detection, server-only fulfillment
- **Restore Purchases** - Cross-device purchase restoration

### Product Categories

| Category | Examples |
|----------|----------|
| Currency | 100 gems, 1000 coins, mega gem pack |
| Bundles | Starter pack, value bundle, resource boost |
| Season Pass | Premium battle pass upgrade |
| Subscriptions | VIP Bronze, Silver, Gold, Diamond |
| Special Offers | Limited time deals, first purchase bonus |

### Backend Functions (`iap.ts`)

| Function | Purpose |
|----------|---------|
| `validatePurchase` | Validate receipt with store |
| `getPurchaseHistory` | Get user's purchase records |
| `getProducts` | Get product catalog |
| `restorePurchases` | Restore previous purchases |
| `checkSubscriptionStatus` | Verify active subscriptions |
| `processSubscriptionRenewal` | Handle renewals |
| `cancelSubscription` | Process cancellations |
| `grantPurchaseRewards` | Fulfill purchase items |

### Unity Setup

1. **Install Unity IAP** via Package Manager
2. **Add IAPManager.cs** to persistent GameObject
3. **Configure product catalog** in Unity IAP settings
4. **Add IAPStoreUI.cs** to store panel
5. **Configure Firebase project** for receipt validation

### API Usage

```csharp
// Initialize (automatic on Awake)
IAPManager.Instance.Initialize();

// Get all products
var products = IAPManager.Instance.GetAllProducts();

// Purchase a product
IAPManager.Instance.PurchaseProduct("com.apex.gems100");

// Restore purchases
IAPManager.Instance.RestorePurchases();

// Listen to events
IAPManager.Instance.OnPurchaseCompleted += (productId, success) => { };
IAPManager.Instance.OnPurchaseFailed += (productId, error) => { };
```

---

## �️ Map SDK Integration

### Overview

Multi-provider map tile system supporting OpenStreetMap (free), Mapbox, Google Maps, and MapTiler with pan/zoom controls, territory overlay, and offline caching.

### Providers & Setup

| Provider | API Key Required | Notes |
|----------|-----------------|-------|
| OpenStreetMap | ❌ No | Free, good for development |
| Mapbox | ✅ Yes | High quality, satellite views |
| Google Maps | ✅ Yes | Best POI data |
| MapTiler | ✅ Yes | Vector tiles available |

Configure in `AppConfig.json`:
```json
{
  "mapProvider": "Mapbox",
  "mapboxApiKey": "pk.eyJ1IjoieW91ci11c2VybmFtZSIsImEiOiJjbGFiY2RlZmdoaWprbG1ub3BxcnN0dXZ3eHl6In0.your-signature"
}
```

### Map Styles

| Style | Description |
|-------|-------------|
| `Streets` | Standard street map |
| `Satellite` | Satellite imagery |
| `Hybrid` | Satellite + labels |
| `Dark` | Dark mode for night play |
| `Light` | Minimal, light background |
| `Terrain` | Elevation shading |
| `GameOverlay` | Custom game-themed style |

### Unity Scripts

| Script | Purpose |
|--------|---------|
| `MapTileProvider.cs` | Fetches tiles from provider APIs |
| `MapRenderer.cs` | Renders tiles with pan/zoom controls |
| `TerritoryOverlay.cs` | Draws territory zones on map |

### Usage

```csharp
// Initialize map
MapRenderer.Instance.Initialize(latitude: 37.7749, longitude: -122.4194);

// Change provider
MapTileProvider.Instance.SetProvider(MapProvider.Mapbox);

// Change style
MapTileProvider.Instance.SetStyle(MapStyle.Dark);

// Pan to location
MapRenderer.Instance.PanTo(38.9072, -77.0369, animated: true);

// Set zoom
MapRenderer.Instance.SetZoom(15);

// Convert coordinates
Vector2 screenPos = MapRenderer.Instance.GeoToScreen(lat, lon);
(double lat, double lon) = MapRenderer.Instance.ScreenToGeo(screenPos);
```

### Territory Overlay

```csharp
// Initialize with territories
TerritoryOverlay.Instance.Initialize(MapRenderer.Instance);

// Zone colors by ownership
// - Owned by player: Green
// - Alliance territory: Blue  
// - Enemy territory: Red
// - Neutral/unclaimed: Gray
// - Contested: Orange pulsing
```

### Offline Support

```csharp
// Download area for offline use
await MapTileProvider.Instance.DownloadAreaForOffline(
    centerLat: 37.7749,
    centerLon: -122.4194,
    radiusKm: 5,
    minZoom: 12,
    maxZoom: 16
);
```

---

## 🌍 World Seed System

### Overview

Pre-populate the world with discoverable points of interest: ancient ruins, abandoned forts, resource hotspots, and landmarks that players can explore and claim.

### Seed Point Types

| Type | Description | Resources |
|------|-------------|-----------|
| `AncientRuins` | Mysterious ruins with artifacts | Artifacts, Crystal |
| `AbandonedFort` | Old fortifications | Stone, Metal |
| `SacredSite` | Mystical locations | Crystal, XP Boost |
| `ResourceHotspot` | Rich resource deposits | Stone, Wood, Metal |
| `Landmark` | Famous/notable locations | XP, Coins |
| `NpcTerritory` | AI-controlled zones | Various |
| `MysteryLocation` | Random discoveries | Random rewards |
| `HistoricalSite` | Real-world history points | Artifacts, XP |

### Backend Functions (`world-seed.ts`)

| Function | Type | Purpose |
|----------|------|---------|
| `seedRegion` | Admin | Populate area with seed points |
| `getNearbySeeds` | Callable | Get seeds near player |
| `discoverSeed` | Callable | First-time discovery |
| `claimSeedResources` | Callable | Gather resources from hotspot |
| `getSeededRegions` | Admin | List all seeded regions |
| `clearRegionSeeds` | Admin | Remove seeds from region |
| `importPOIData` | Admin | Import external POI data |

### Unity Integration

```csharp
// Get nearby seeds (auto-refreshes every 30s)
WorldSeedManager.Instance.SetPlayerPosition(lat, lon);

// Listen for discoveries
WorldSeedManager.Instance.OnSeedDiscovered += (seed) => {
    Debug.Log($"Discovered: {seed.name}!");
};

// Listen for resources claimed
WorldSeedManager.Instance.OnResourcesClaimed += (resources) => {
    // Grant resources to player
};

// Listen for proximity alerts
WorldSeedManager.Instance.OnSeedApproached += (seed, distance) => {
    // Show "Nearby Discovery" notification
};
```

### Admin Seeding Workflow

```bash
# Via Firebase Console or Admin API
curl -X POST 'https://[PROJECT].cloudfunctions.net/seedRegion' \
  -H 'Authorization: Bearer [ADMIN_TOKEN]' \
  -d '{
    "centerLat": 37.7749,
    "centerLon": -122.4194,
    "radiusKm": 10,
    "seedDensity": 5,
    "seedTypes": ["AncientRuins", "ResourceHotspot", "Landmark"]
  }'
```

### Seed Discovery Rewards

| First Discovery | Reward |
|-----------------|--------|
| Any seed type | 50-200 XP |
| Ancient Ruins | +100 XP bonus |
| Sacred Site | +150 XP bonus |
| Mystery Location | Random item |

---

## 🏗️ Building Templates

### Overview

15+ pre-designed building templates ranging from simple watchtowers to complex tower complexes. Templates provide instant placement of tested, balanced structures.

### Template Categories

| Category | Templates | Unlock Level |
|----------|-----------|--------------|
| **Starter** | Watchtower, Walled Compound | 1 |
| **Defensive** | Small Fortress, Bunker, Wall, Gatehouse, Turret Nest | 3-8 |
| **Economy** | Resource Outpost, Trading Post | 5-10 |
| **Military** | Barracks, Armory | 8-12 |
| **Advanced** | Command Center, Tower Complex | 15-20 |
| **Decorative** | Victory Monument, Garden Plaza, Torch Circle | 5-10 |

### Template Details

| Template | Size | Blocks | Cost |
|----------|------|--------|------|
| Watchtower | 3x3x5 | 9 | 50 Stone |
| Walled Compound | 5x5x3 | 36 | 100 Stone, 50 Wood |
| Small Fortress | 7x7x4 | 72 | 300 Stone, 100 Metal |
| Command Center | 9x9x6 | ~200 | 500 Stone, 300 Metal, 100 Crystal |
| Tower Complex | 11x11x8 | ~350 | 800 Stone, 500 Metal, 200 Crystal |

### Unity Usage

```csharp
// Get all templates
List<BuildingTemplate> templates = BuildingTemplateManager.Instance.GetAllTemplates();

// Get by category
List<BuildingTemplate> defensive = BuildingTemplateManager.Instance.GetTemplatesByCategory("Defensive");

// Check if player can afford
bool canAfford = BuildingTemplateManager.Instance.CanAffordTemplate(template, playerInventory);

// Preview template placement
BuildingTemplateManager.Instance.ShowTemplatePreview(template, position, rotation);

// Place template (returns list of BuildingBlocks)
List<BuildingBlock> blocks = await BuildingTemplateManager.Instance.PlaceTemplate(
    template: template,
    position: hitPoint,
    rotation: Quaternion.identity
);
```

### Adding Custom Templates

```csharp
var customTemplate = new BuildingTemplate {
    Id = "custom_tower",
    Name = "Custom Tower",
    Category = "Custom",
    UnlockLevel = 5,
    Description = "My custom design",
    BlockPositions = new List<Vector3Int> { /* ... */ },
    BlockTypes = new List<BlockType> { /* ... */ },
    ResourceCost = new Dictionary<string, int> {
        { "Stone", 100 },
        { "Wood", 50 }
    }
};
BuildingTemplateManager.Instance.RegisterTemplate(customTemplate);
```

### Unity AI Prompt for Template UI

```
Create a "Building Templates" panel with:

HEADER:
- "Templates" title with close button
- Category filter buttons (All, Starter, Defensive, Economy, Military, Advanced, Decorative)

TEMPLATE GRID (ScrollView):
- Each template shows:
  - 3D preview thumbnail
  - Template name
  - Block count badge
  - Lock icon if level too low
  - Resource cost icons

SELECTED TEMPLATE PANEL:
- Larger 3D preview (rotatable)
- Full description
- Resource requirements list with afford status
- "Place Template" button (disabled if can't afford)
- "Save Design" button to create new template

Wire to BuildingTemplateManager:
- OnTemplateSelected
- OnTemplatePlaced
```

---

## �💾 Data Persistence & Offline Mode

### Overview

Local data caching with encryption for offline play and action queuing when network is unavailable.

### Data Persistence Features

- **Generic Caching** - Store any serializable data with keys
- **Expiry Management** - Auto-expire stale data
- **Disk Storage** - Persist across app restarts
- **XOR Encryption** - Protect sensitive cached data
- **Size Limits** - Automatic cleanup when cache grows
- **Batch Operations** - Clear all, clear expired

### Offline Mode Features

- **Connectivity Detection** - Check network state
- **Action Queuing** - Queue actions when offline
- **Automatic Sync** - Sync when back online
- **Conflict Resolution** - Server wins, local wins, merge, ask user
- **Feature Availability** - Check what's available offline

### Cached Data Types

| Key | Data Type | Expiry |
|-----|-----------|--------|
| `player_profile` | Player data | 1 hour |
| `resources` | Resource counts | 30 minutes |
| `inventory` | Item inventory | 1 hour |
| `territories` | Nearby territories | 10 minutes |
| `alliance` | Alliance data | 1 hour |
| `settings` | User preferences | Never |
| `tutorial` | Tutorial progress | Never |

### Unity Setup

1. **Add DataPersistenceManager.cs** early in scene hierarchy
2. **Add OfflineModeManager.cs** to GameManager object
3. **Configure expiry times** in inspector
4. **Wrap Firebase calls** with `ExecuteWithOfflineFallback`

### API Usage

```csharp
// Cache data
DataPersistenceManager.Instance.Set("player_profile", playerData);

// Get cached data
var player = DataPersistenceManager.Instance.Get<PlayerProfile>("player_profile");

// Check offline status
bool isOffline = OfflineModeManager.Instance.IsOffline;

// Queue action for later
OfflineModeManager.Instance.QueueOfflineAction("claim_reward", data);

// Wrap Firebase calls
await OfflineModeManager.Instance.ExecuteWithOfflineFallback(
    () => FirebaseFunction(),
    () => CachedFallback()
);
```

---

## 🔊 Audio System

### Overview

Complete audio management system with sound effects, music, ambient sounds, and spatial 3D audio for immersive AR gameplay.

### Features

- **Audio Mixer Integration** - Master, Music, SFX, Ambient, UI, Voice channels
- **Object Pooling** - Efficient SFX playback with reusable audio sources
- **Music Crossfading** - Smooth transitions between tracks
- **Dynamic Music States** - Combat, peace, victory, defeat, boss, event modes
- **Ambient Presets** - Day/night, indoor/outdoor, battlefield environments
- **Spatial Audio** - 3D positioned sounds for AR immersion
- **Haptic Feedback** - iOS/Android vibration support
- **ScriptableObject Libraries** - Easy audio asset management

### Audio Managers

| Manager | Purpose |
|---------|---------|
| `AudioManager` | Central control, mixer, volume settings, haptics |
| `SFXManager` | Sound effects with pooling, categories, convenience methods |
| `MusicManager` | Background music, crossfading, playlists, stingers |
| `AmbientSoundManager` | Environmental audio, spatial sources, time of day |

### Sound Effect Categories

| Category | Examples |
|----------|----------|
| Combat | Attack, hit, block, damage, death, explosion |
| Building | Place, remove, rotate, snap, complete, destroy |
| Resource | Collect, drop, gold, gem, XP gain |
| UI | Click, hover, panel open/close, notification, error |
| Social | Message sent/received, friend online, gift |
| Achievement | Unlock, level up, daily reward, milestone |
| Territory | Claim, lost, under attack, war start/end |

### Music States

| State | Description |
|-------|-------------|
| `Menu` | Main menu theme |
| `Peace` | Exploration music |
| `Combat` | Battle music (auto-triggers) |
| `Victory` | Victory stinger |
| `Defeat` | Defeat stinger |
| `Boss` | Boss/alliance war music |
| `Event` | World event music |
| `Building` | Creative mode music |

### Unity Setup

1. **Create Audio Mixer** (Window → Audio → Audio Mixer)
   - Create groups: Master, Music, SFX, Ambient, UI, Voice
   - Expose volume parameters

2. **Add AudioManager** to persistent GameObject
   - Assign Audio Mixer and groups
   - Configure default volumes

3. **Add SFXManager, MusicManager, AmbientSoundManager**

4. **Create ScriptableObject libraries:**
   - Right-click → Create → Apex Citadels → Audio → SFX Library
   - Right-click → Create → Apex Citadels → Audio → Music Library
   - Right-click → Create → Apex Citadels → Audio → Ambient Library

5. **Use context menu "Generate Default Entries"** on libraries to create placeholders

### API Usage

```csharp
// Sound Effects
SFXManager.Instance.Play("ui_click");
SFXManager.Instance.PlayAt("explosion", position);
SFXManager.Instance.PlayAttack();
SFXManager.Instance.PlayButtonClick();

// Music
MusicManager.Instance.Play("menu_theme");
MusicManager.Instance.EnterCombat();
MusicManager.Instance.ExitCombat();
MusicManager.Instance.PlayVictoryStinger();

// Ambient
AmbientSoundManager.Instance.SetPreset("outdoor_day");
AmbientSoundManager.Instance.SetCityAmbient();

// Volume Control
AudioManager.Instance.SetMasterVolume(0.8f);
AudioManager.Instance.SetMusicVolume(0.5f);
AudioManager.Instance.ToggleMute();

// Haptics
AudioManager.Instance.PlayHapticLight();
AudioManager.Instance.PlayHapticSuccess();
```

### Unity AI Prompt - Audio Settings Panel

```
Create an Audio Settings panel with:

HEADER:
- "Audio Settings" title
- Close button (X)
- "Reset to Default" button

VOLUME SLIDERS:
Each slider shows: Label, Slider (0-100%), Current % text
- Master Volume (with mute button)
- Music Volume (with test button)
- SFX Volume (with test button)
- Ambient Volume
- UI Volume
- Voice Volume

TOGGLES:
- Vibration/Haptics toggle with ON/OFF indicator
- 3D Spatial Audio toggle with ON/OFF indicator
- Mute When Unfocused toggle with ON/OFF indicator

NOW PLAYING SECTION:
- Current track name text
- Skip button

Wire to AudioSettingsUI.cs component.
```

---

## 🌍 Localization System

### Overview

Complete multi-language support system enabling internationalization (i18n) across 15 languages with automatic text updates, date/number formatting, and RTL support.

### Supported Languages

| Language | Code | Native Name |
|----------|------|-------------|
| English | `en` | English |
| Spanish | `es` | Español |
| French | `fr` | Français |
| German | `de` | Deutsch |
| Italian | `it` | Italiano |
| Portuguese | `pt` | Português |
| Russian | `ru` | Русский |
| Japanese | `ja` | 日本語 |
| Korean | `ko` | 한국어 |
| Chinese (Simplified) | `zh-CN` | 简体中文 |
| Chinese (Traditional) | `zh-TW` | 繁體中文 |
| Arabic | `ar` | العربية |
| Turkish | `tr` | Türkçe |
| Polish | `pl` | Polski |
| Dutch | `nl` | Nederlands |

### Features

- **Automatic System Language Detection** - Uses device language as default
- **Runtime Language Switching** - Change language without restart
- **JSON-based Language Files** - Easy to edit and translate
- **Fallback System** - Falls back to English if key is missing
- **Format String Support** - `{0}`, `{1}` placeholders
- **Pluralization** - `_one` / `_other` suffixes
- **Named Placeholders** - `{player}`, `{gold}` style
- **RTL Support** - Right-to-left language handling
- **Date/Number Formatting** - Culture-aware formatting
- **LocalizedText Component** - Automatic UI updates
- **LocalizedImage/Audio** - Localized sprites and audio clips

### Language File Format

```json
{
    "language": "English",
    "languageCode": "en",
    "entries": [
        { "key": "common.hello", "value": "Hello" },
        { "key": "common.welcome", "value": "Welcome" },
        { "key": "building.level_format", "value": "Level {0}" },
        { "key": "resource.gold_one", "value": "1 Gold" },
        { "key": "resource.gold_other", "value": "{0} Gold" }
    ]
}
```

### Key Categories

| Prefix | Description |
|--------|-------------|
| `common.` | Generic UI (yes, no, ok, cancel, etc.) |
| `menu.` | Main menu items |
| `settings.` | Settings labels |
| `game.` | In-game terms (level, score, health) |
| `building.` | Building-related strings |
| `combat.` | Combat terms (attack, defend, victory) |
| `territory.` | Territory control strings |
| `alliance.` | Alliance/team strings |
| `resource.` | Resource names |
| `player.` | Player profile strings |
| `ar.` | AR-related instructions |
| `notification.` | Push notification text |
| `tutorial.` | Onboarding text |
| `error.` | Error messages |
| `time.` | Time formatting |

### API Usage

```csharp
// Basic lookup
string hello = LocalizationManager.Instance.Get("common.hello");

// With format arguments
string level = LocalizationManager.Instance.GetFormat("building.level_format", 5);
// Result: "Level 5"

// With named placeholders
string msg = LocalizationManager.Instance.GetWithPlaceholders("notification.gift", 
    new Dictionary<string, object> { { "player", "Alex" }, { "item", "Gold" } });

// Pluralization
string gold = LocalizationManager.Instance.GetPlural("resource.gold", 5);
// Result: "5 Gold"

// Static shorthand
string text = LocalizationManager.T("common.ok");
string formatted = LocalizationManager.TF("building.cost_format", 100);

// Change language
LocalizationManager.Instance.SetLanguage(SystemLanguage.Spanish);
LocalizationManager.Instance.SetLanguageByCode("es");

// Date/number formatting
string num = LocalizationManager.Instance.FormatNumber(1234567);
string date = LocalizationManager.Instance.FormatDate(DateTime.Now);
```

### LocalizedText Component

1. Add `LocalizedText` component to any UI text (TextMeshPro or Legacy Text)
2. Set the `Localization Key` field
3. Text automatically updates when language changes

### Unity Setup

1. **Create Language Files** in `Resources/Localization/`:
   - `en.json`, `es.json`, `fr.json`, etc.

2. **Add LocalizationManager** to persistent GameObject
   - Configure default language
   - Enable system language detection

3. **Add LocalizedText** to UI text elements
   - Set localization keys

4. **Create Language Settings UI** with LanguageSettingsUI component

### Unity AI Prompt - Language Settings Panel

```
Create a Language Settings panel with:

HEADER:
- "Language / Idioma" title
- Close button (X)

CURRENT LANGUAGE DISPLAY:
- Current language name in native format
- Language code (e.g., "EN")
- RTL indicator (for Arabic/Hebrew)

LANGUAGE LIST (vertical scroll):
For each language show:
- Flag icon (if available)
- Native language name (e.g., "Español" not "Spanish")
- Selection indicator (checkmark or highlight)

PREVIEW SECTION:
- Preview text showing sample translations
- Updates live when hovering over languages

BUTTONS:
- "Apply" button
- "Cancel" button
- "Reset to System Language" button

Wire to LanguageSettingsUI.cs component.
```

---

## 📍 AR Spatial Anchors System

### Overview

Enhanced AR anchor management system with cloud persistence, tracking state management, desktop fallback, and comprehensive session control.

### Features

- **Multi-platform Support** - ARCore (Android) and ARKit (iOS)
- **Anchor Persistence** - Save/restore anchors between sessions
- **Cloud-ready Architecture** - Prepared for cloud anchor integration
- **Desktop Fallback** - Full functionality in editor and standalone builds
- **Tracking State Management** - Monitoring and recovery
- **Managed Anchors** - Rich anchor metadata and state
- **Plane Detection** - Horizontal/vertical surface detection
- **Request Queuing** - Queue anchors before tracking is ready
- **Session Lifecycle** - Full control over AR session

### AR Components

| Component | Purpose |
|-----------|---------|
| `SpatialAnchorManager` | Anchor creation, persistence, raycasting |
| `ARSessionController` | Session lifecycle, camera management |

### SpatialAnchorManager Features

- **Multiple Raycast Modes** - Surface, feature point, desktop fallback
- **Anchor Tracking States** - Tracking, Limited, Lost
- **Auto-persistence** - Automatic save/restore of anchors
- **Geospatial Stub** - Ready for ARCore Geospatial extension
- **Event System** - OnTrackingReady, OnAnchorCreated, etc.

### ARSessionController Features

- **Auto-initialization** - Start session automatically
- **Fallback Camera** - Desktop testing with WASD controls
- **Quality Settings** - Low/Medium/High presets
- **Light Estimation** - Ambient lighting data
- **Permission Handling** - Camera permission requests
- **Session Recovery** - Auto-reset on tracking loss

### API Usage

```csharp
// Check tracking state
if (SpatialAnchorManager.Instance.IsTracking)
{
    // Ready to create anchors
}

// Create anchor from screen tap
string anchorId = SpatialAnchorManager.Instance.CreateAnchorFromScreenPoint(
    Input.mousePosition);

// Create anchor at specific position
string anchorId = SpatialAnchorManager.Instance.CreateAnchor(
    position, rotation, "my_anchor_id");

// Get hit position from screen point
if (SpatialAnchorManager.Instance.TryGetHitPosition(screenPos, out Vector3 hitPos, out Quaternion hitRot))
{
    // Place object at hitPos
}

// Save anchor to persistent storage
SpatialAnchorManager.Instance.SaveAnchor(anchorId);

// Get managed anchor info
ManagedAnchor anchor = SpatialAnchorManager.Instance.GetAnchor(anchorId);
Debug.Log($"Anchor {anchor.Id} at {anchor.Position}, tracking: {anchor.TrackingState}");

// Queue anchor for when tracking is ready
SpatialAnchorManager.Instance.QueueAnchorRequest(position, rotation, "queued_anchor");

// Session control
ARSessionController.Instance.StartARSession();
ARSessionController.Instance.PauseSession();
ARSessionController.Instance.ResumeSession();
ARSessionController.Instance.ResetSession();

// Toggle AR/Fallback mode
ARSessionController.Instance.ToggleARMode();

// Get active camera (AR or fallback)
Camera cam = ARSessionController.Instance.ActiveCamera;

// Session state
bool isAR = ARSessionController.Instance.IsARSupported;
bool isTracking = ARSessionController.Instance.IsTracking;
```

### Events

```csharp
// SpatialAnchorManager events
SpatialAnchorManager.Instance.OnTrackingReady += () => { /* Ready to place */ };
SpatialAnchorManager.Instance.OnAnchorCreated += (anchor, id) => { /* Anchor created */ };
SpatialAnchorManager.Instance.OnAnchorRestored += (id) => { /* Saved anchor restored */ };
SpatialAnchorManager.Instance.OnPlaneDetected += (plane) => { /* Surface found */ };

// ARSessionController events  
ARSessionController.Instance.OnSessionStarted += () => { /* Session ready */ };
ARSessionController.Instance.OnTrackingStateChanged += (state) => { /* Tracking changed */ };
ARSessionController.Instance.OnLightEstimationUpdated += (light) => { /* Update lighting */ };
ARSessionController.Instance.OnARNotSupported += () => { /* Show fallback UI */ };
```

### Desktop Development

When running in the Unity Editor or standalone Windows/Mac builds:

1. **Auto-detection** - System automatically enables desktop mode
2. **Fallback Camera** - WASD movement, right-click look, scroll zoom
3. **Ground Plane Raycasting** - Anchors place on y=0 plane
4. **Physics Raycasting** - Hit colliders for more precise placement
5. **Full API Compatibility** - All anchor operations work identically

### Unity Setup

1. **Create AR Session Origin** GameObject with:
   - ARSession component
   - XR Origin with AR Camera
   - ARRaycastManager
   - ARPlaneManager
   - ARAnchorManager

2. **Add SpatialAnchorManager** - Assign AR components
3. **Add ARSessionController** - Configure session settings
4. **Configure Fallback Camera** for desktop testing

### Unity AI Prompt - AR Setup Scene

```
Create an AR game scene with:

AR SESSION ORIGIN:
- ARSession component (auto-start enabled)
- XR Origin with AR Camera
- ARRaycastManager
- ARPlaneManager (horizontal detection)
- ARAnchorManager

SPATIAL ANCHOR MANAGER:
- Add SpatialAnchorManager.cs to persistent object
- Wire up all AR component references
- Enable desktop fallback
- Configure tracking timeout (15s)

AR SESSION CONTROLLER:
- Add ARSessionController.cs
- Enable auto-start session
- Configure quality to High
- Enable light estimation

FALLBACK CAMERA:
- Create FallbackCamera GameObject
- Position at (0, 5, -10), looking at origin
- Disable by default (enabled when AR unavailable)

UI ELEMENTS:
- Tracking status indicator (top)
- "Scan your environment" message (center, show during init)
- Crosshair/reticle for tap target (center)
- "Tap to place" instruction (bottom)
```

---

## � GDPR Compliance & Privacy System

### Overview

Complete GDPR (General Data Protection Regulation) and COPPA (Children's Online Privacy Protection Act) compliance system with data portability, right to deletion, consent management, and age verification.

### Features

- **Data Export (Article 15 & 20)** - Export all user data in JSON/CSV format
- **Data Deletion (Article 17)** - 30-day grace period with cancellation option
- **Consent Management** - Granular consent with version tracking
- **Privacy Settings** - Profile visibility, leaderboard opt-out
- **Age Gate** - Date of birth verification (13+ required)
- **Consent Dialog** - GDPR-compliant consent UI with Accept All/Reject All

### Backend Functions (`gdpr.ts`)

| Function | Type | Purpose |
|----------|------|---------|
| `requestDataExport` | Callable | Request full data export |
| `processDataExport` | Scheduled | Process pending exports |
| `getDataExportStatus` | Callable | Check export progress |
| `requestDataDeletion` | Callable | Request account deletion |
| `cancelDataDeletion` | Callable | Cancel deletion request |
| `processScheduledDeletions` | Scheduled | Execute pending deletions (3 AM UTC) |
| `updateConsent` | Callable | Update consent preferences |
| `getConsent` | Callable | Get current consent status |
| `updatePrivacySettings` | Callable | Update privacy settings |
| `getPrivacySettings` | Callable | Get privacy settings |
| `adminGetDeletionRequests` | Callable | Admin view of pending deletions |
| `adminForceDelete` | Callable | Admin immediate deletion |

### Unity Scripts

| Script | Purpose |
|--------|---------|
| `GDPRManager.cs` | Consent management, data export/delete requests |
| `AgeGateUI.cs` | COPPA age verification with DOB input |
| `ConsentDialogUI.cs` | Consent toggles with Accept All/Reject All |

### API Usage

```csharp
// Check if consent is required (first launch)
GDPRManager.Instance.OnConsentRequired += () => {
    // Show consent dialog
    consentDialog.Show();
};

// Update consent
await GDPRManager.Instance.UpdateConsent(
    analytics: true,
    marketing: false,
    personalization: true,
    thirdParty: false
);

// Request data export
await GDPRManager.Instance.RequestDataExport();

// Request account deletion
await GDPRManager.Instance.RequestDataDeletion();

// Age gate check
AgeGateUI.Instance.OnAgeVerified += (age) => {
    // User is 13+, proceed
};
AgeGateUI.Instance.OnAgeRejected += () => {
    // User under 13, block access
};
```

### Unity AI Prompt - Privacy Settings Panel

```
Create a Privacy Settings panel with:

HEADER:
- "Privacy Settings" title
- Close button (X)

CONSENT SECTION:
- "Data Collection" header
- Analytics toggle: "Help improve the game with usage data"
- Marketing toggle: "Receive promotional offers"
- Personalization toggle: "Personalized game experience"
- Third-party toggle: "Share data with partners"
- "Update Preferences" button

PRIVACY SETTINGS:
- Profile visibility dropdown (Public, Friends Only, Private)
- Leaderboard opt-out toggle
- Activity feed privacy toggle

DATA RIGHTS:
- "Export My Data" button with status indicator
- "Delete My Account" button (red, with confirmation)

LEGAL LINKS:
- Privacy Policy link
- Terms of Service link

Wire to GDPRManager events and methods.
```

---

## 🛡️ Content Moderation System

### Overview

Multi-layer content moderation system with client-side quick filtering, server-side comprehensive analysis, user reporting, and moderation actions (warn, mute, ban).

### Features

- **Profanity Filter** - Pattern-based profanity detection
- **Slur Detection** - Critical severity automatic blocking
- **Spam Detection** - Excessive caps, repeated chars, link spam
- **PII Detection** - Phone numbers, emails, addresses
- **Leetspeak Normalization** - Evasion detection (e.g., "f4ck")
- **Toxicity Scoring** - 0-100 score for content severity
- **User Reports** - Report system with duplicate prevention
- **Auto-escalation** - 3 reports triggers priority review
- **Mute System** - Time-based chat restrictions
- **Ban System** - Temporary or permanent bans
- **Appeal System** - Users can appeal bans

### Backend Functions (`moderation.ts`)

| Function | Type | Purpose |
|----------|------|---------|
| `moderateContent` | Callable | Analyze content before posting |
| `reportContent` | Callable | Submit user report |
| `resolveReport` | Callable | Admin resolve report |
| `getPendingReports` | Callable | Admin get report queue |
| `muteUser` | Callable | Mute user for duration |
| `banUserFunction` | Callable | Ban user (temp/permanent) |
| `appealBan` | Callable | Submit ban appeal |
| `checkBanStatus` | Callable | Check if user is banned |
| `cleanupExpiredMutes` | Scheduled | Remove expired mutes hourly |

### Unity Script - ContentModerationManager.cs

```csharp
// Moderate content before sending
var result = await ContentModerationManager.Instance.ModerateContent(
    message, 
    "chat"
);

if (result.Approved) {
    // Send message
    SendMessage(result.FilteredContent ?? message);
} else {
    // Show warning to user
    ShowWarning(result.Message);
}

// Report another user's content
var report = await ContentModerationManager.Instance.ReportContent(
    reportedUserId: "user123",
    contentType: "chat",
    contentId: "msg456",
    content: "offensive message here",
    reason: "harassment",
    description: "User is harassing me repeatedly"
);

// Check if content is safe (sync)
bool safe = ContentModerationManager.Instance.IsContentSafe(text);

// Get filtered version
string filtered = ContentModerationManager.Instance.GetFilteredContent(text);

// Listen for moderation actions
ContentModerationManager.Instance.OnUserWarning += (message) => {
    ShowWarningDialog(message);
};

ContentModerationManager.Instance.OnUserMuted += (message, expiresAt) => {
    ShowMutedDialog(message, expiresAt);
};

ContentModerationManager.Instance.OnUserBanned += (banStatus) => {
    ShowBannedScreen(banStatus);
};
```

### Moderation Flow

```
User Submits Content
         │
         ▼
┌─────────────────────┐
│ Client-Side Filter  │ ← Quick local check
│ (Profanity patterns)│
└─────────────────────┘
         │
         ▼
┌─────────────────────┐
│ Server Validation   │ ← Comprehensive analysis
│ (All patterns +     │
│  toxicity scoring)  │
└─────────────────────┘
         │
         ▼
    ┌────┴────┐
    │ Result  │
    └────┬────┘
         │
   ┌─────┼─────┐
   ▼     ▼     ▼
Approved Auto-  Blocked
         action
         │
    ┌────┴────┐
    ▼    ▼    ▼
   Warn Mute  Ban
```

---

## 🛍️ Cosmetics Shop System

### Overview

Complete cosmetics monetization system with 10 item categories, rotating shop, gem/coin dual currency, rarity tiers, and time-limited offers.

### Cosmetic Categories

| Category | Description |
|----------|-------------|
| `block_skin` | Custom textures for building blocks |
| `territory_effect` | Visual effects around territory |
| `avatar_frame` | Decorative frames for profile |
| `avatar_background` | Profile background images |
| `victory_animation` | Celebration effects on victory |
| `building_effect` | Particle effects on buildings |
| `chat_bubble` | Custom chat message backgrounds |
| `title` | Display titles under name |
| `emote` | Animated emotes/expressions |
| `trail_effect` | Effects following player |

### Rarity Tiers

| Rarity | Color | Price Multiplier |
|--------|-------|------------------|
| Common | Gray | 1x |
| Uncommon | Green | 1.5x |
| Rare | Blue | 2.5x |
| Epic | Purple | 4x |
| Legendary | Orange | 8x |

### Backend Functions (`cosmetics.ts`)

| Function | Type | Purpose |
|----------|------|---------|
| `getShopCatalog` | Callable | Get all shop items |
| `getFeaturedItems` | Callable | Get featured/sale items |
| `purchaseCosmetic` | Callable | Buy with gems or coins |
| `getUserCosmetics` | Callable | Get owned items |
| `equipCosmetic` | Callable | Equip an item |
| `unequipCosmetic` | Callable | Unequip item from slot |
| `toggleFavorite` | Callable | Add/remove from favorites |
| `getCurrencyBalance` | Callable | Get gem/coin balance |
| `awardCurrency` | Admin | Grant currency to user |
| `adminCreateCosmetic` | Admin | Create new cosmetic item |
| `adminCreateRotation` | Admin | Create shop rotation |
| `adminGrantCosmetic` | Admin | Gift item to user |

### Shop Rotation System

Shop rotations feature specific items with discounts for limited time:

```json
{
  "name": "Winter Sale",
  "startDate": "2026-01-15",
  "endDate": "2026-01-22",
  "featuredItems": ["legendary_ice_skin", "epic_snowflake_trail"],
  "discountPercentage": 30
}
```

### API Usage

```csharp
// Open shop
CosmeticsShopManager.Instance.OpenShop();

// Purchase item
CosmeticsShopManager.Instance.PurchaseItem("gems"); // or "coins"

// Listen for purchases
CosmeticsShopManager.Instance.OnItemPurchased += (item) => {
    ShowPurchaseConfirmation(item);
};

// Equip item
CosmeticsShopManager.Instance.EquipSelectedItem();

// Get currently equipped
string equippedSkin = CosmeticsShopManager.Instance.GetEquippedItem("block_skin");

// Check balance
var balance = CosmeticsShopManager.Instance.GetBalance();
Debug.Log($"Gems: {balance.Gems}, Coins: {balance.Coins}");
```

### Unity AI Prompt - Cosmetics Shop Panel

```
Create a Cosmetics Shop panel with:

HEADER:
- "Shop" title with close button
- Currency display (gems icon + amount, coins icon + amount)
- "Get More Gems" button

TAB BAR:
- Shop / Inventory / Favorites tabs

CATEGORY BAR (horizontal scroll):
- All, Block Skins, Territory Effects, Avatar Frames, etc.

ITEM GRID (scrollable):
Each item card shows:
- Preview image/thumbnail
- Rarity border color
- Item name
- Price (gems or coins)
- Owned badge (checkmark)
- Equipped badge (star)
- Sale badge (discount %)

ITEM PREVIEW PANEL (when item selected):
- Large preview (3D rotatable for skins)
- Item name, description
- Rarity with colored background
- Price with original/sale comparison
- Limited time countdown (if applicable)
- "Purchase with Gems" button
- "Purchase with Coins" button
- "Equip" / "Unequip" button (if owned)
- "Favorite" heart button

Wire to CosmeticsShopManager events.
```

---

## 📊 Performance Monitoring System

### Overview

Comprehensive performance monitoring with FPS tracking, memory profiling, Firebase Analytics integration, Crashlytics error reporting, and custom performance traces.

### Features

- **FPS Monitoring** - Average, min, max FPS with history
- **Memory Tracking** - Used memory, peak memory
- **Threshold Alerts** - Automatic alerts for low FPS or high memory
- **Firebase Analytics** - Event logging with parameters
- **Crashlytics Integration** - Error and exception reporting
- **Custom Traces** - Track performance of specific operations
- **Session Management** - Track session duration and metrics
- **Debug Overlay** - In-editor performance display

### Thresholds

| Metric | Warning | Critical |
|--------|---------|----------|
| FPS | < 30 | < 15 |
| Memory | > 512 MB | > 768 MB |

### API Usage

```csharp
// Start a performance trace
using (var trace = PerformanceMonitor.Instance.StartTrace("load_territory"))
{
    trace.SetAttribute("territory_id", territoryId);
    await LoadTerritory();
    trace.SetMetric("blocks_loaded", blockCount);
}
// Trace automatically ends and logs when disposed

// Log screen view
PerformanceMonitor.Instance.LogScreenView("Shop", "CosmeticsShopPanel");

// Log custom event
PerformanceMonitor.Instance.LogEvent("item_purchased", new Dictionary<string, object> {
    ["item_id"] = "legendary_skin_01",
    ["currency"] = "gems",
    ["price"] = 800
});

// Log error
PerformanceMonitor.Instance.LogError("Failed to load territory", stackTrace);

// Log exception
try {
    // risky operation
} catch (Exception ex) {
    PerformanceMonitor.Instance.LogException(ex, "TerritoryManager.Load");
}

// Set user properties for analytics
PerformanceMonitor.Instance.SetUserId(playerId);
PerformanceMonitor.Instance.SetUserProperty("player_level", "25");

// Force garbage collection with logging
PerformanceMonitor.Instance.ForceGarbageCollection();

// Get current metrics
var metrics = PerformanceMonitor.Instance.GetCurrentMetrics();
Debug.Log($"FPS: {metrics.AverageFps}, Memory: {metrics.MemoryUsedMb}MB");

// Listen for performance issues
PerformanceMonitor.Instance.OnLowFpsDetected += (severity, fps) => {
    if (severity == "critical") {
        // Reduce quality settings
        QualitySettings.DecreaseLevel();
    }
};

PerformanceMonitor.Instance.OnHighMemoryDetected += (severity, memoryMb) => {
    if (severity == "critical") {
        // Force cleanup
        Resources.UnloadUnusedAssets();
    }
};
```

### Unity Setup

1. **Add PerformanceMonitor** to persistent GameObject
2. **Configure thresholds** in inspector
3. **Enable/disable debug overlay** for development
4. **Configure sampling interval** (default 1 second)

### Debug Overlay (Editor/Development)

Enable `showDebugOverlay` in inspector to display:
- Current FPS with min/max
- Frame time in milliseconds
- Memory usage with peak
- Session ID
- Error/warning counts
- "Force GC" button

---

## �📋 Complete Script Inventory

### Unity Scripts (`Assets/Scripts/`)

#### Core Systems
| Folder | Script | Purpose |
|--------|--------|---------|
| `Core/` | `GameManager.cs` | App initialization, auth, lifecycle |
| `AR/` | `SpatialAnchorManager.cs` | Enhanced AR spatial anchors with persistence |
| `AR/` | `ARSessionController.cs` | AR session lifecycle management |
| `Backend/` | `AnchorPersistenceService.cs` | Firebase cloud sync |
| `Config/` | `AppConfig.cs` | Configuration management |

#### Game Mechanics
| Folder | Script | Purpose |
|--------|--------|---------|
| `Territory/` | `Territory.cs` | Territory data model |
| `Territory/` | `TerritoryManager.cs` | Claim, attack, defend territories |
| `Building/` | `BuildingBlock.cs` | Block type definitions |
| `Building/` | `BuildingManager.cs` | Place and manage blocks |
| `Player/` | `PlayerProfile.cs` | Player data model |
| `Player/` | `PlayerManager.cs` | Auth, resources, XP |
| `Combat/` | `CombatManager.cs` | Attack mechanics |
| `Alliance/` | `Alliance.cs` | Alliance data models |
| `Alliance/` | `AllianceManager.cs` | Team management |
| `Resources/` | `ResourceManager.cs` | Harvest and gather |
| `Map/` | `MapViewController.cs` | 2D territory map |

#### Engagement Systems
| Folder | Script | Purpose |
|--------|--------|---------|
| `WorldEvents/` | `WorldEventManager.cs` | FOMO events, multipliers, participation |
| `SeasonPass/` | `SeasonPassManager.cs` | 100-tier battle pass, challenges |
| `Social/` | `FriendsManager.cs` | Friends, gifts, activity feed |
| `Chat/` | `ChatManager.cs` | Real-time chat, channels, reactions |
| `Referrals/` | `ReferralManager.cs` | Referral codes, sharing, milestones |
| `DailyRewards/` | `DailyRewardManager.cs` | Login streaks |
| `Achievements/` | `AchievementManager.cs` | Progress tracking |
| `Leaderboard/` | `LeaderboardManager.cs` | Rankings and scores |
| `Notifications/` | `NotificationManager.cs` | Legacy notification alerts |
| `Notifications/` | `PushNotificationManager.cs` | FCM push notifications, topics |
| `Notifications/` | `NotificationSettingsUI.cs` | User notification preferences UI |
| `Tutorial/` | `TutorialManager.cs` | Onboarding tutorial system |
| `Tutorial/` | `TutorialUI.cs` | Tutorial visual presentation |

#### Monetization
| Folder | Script | Purpose |
|--------|--------|---------|
| `IAP/` | `IAPManager.cs` | Unity IAP integration, purchases |
| `IAP/` | `IAPStoreUI.cs` | In-app purchase store interface |

#### Data & Offline
| Folder | Script | Purpose |
|--------|--------|---------|
| `Data/` | `DataPersistenceManager.cs` | Local caching, encryption, expiry |
| `Data/` | `OfflineModeManager.cs` | Offline detection, action queue, sync |

#### Audio System
| Folder | Script | Purpose |
|--------|--------|---------|
| `Audio/` | `AudioManager.cs` | Central audio control, mixer, volumes |
| `Audio/` | `SFXManager.cs` | Sound effects with object pooling |
| `Audio/` | `MusicManager.cs` | Background music, crossfading |
| `Audio/` | `AmbientSoundManager.cs` | Environmental/spatial audio |
| `Audio/` | `AudioSettingsUI.cs` | Audio settings panel |

#### Localization System
| Folder | Script | Purpose |
|--------|--------|---------|
| `Localization/` | `LocalizationManager.cs` | Multi-language support, string lookup |
| `Localization/` | `LocalizedText.cs` | Auto-updating localized text component |
| `Localization/` | `LanguageSettingsUI.cs` | Language selection UI panel |

#### Infrastructure
| Folder | Script | Purpose |
|--------|--------|---------|
| `Analytics/` | `AnalyticsManager.cs` | Event tracking, sessions, A/B tests |
| `AntiCheat/` | `AntiCheatManager.cs` | GPS validation, trust scores |
| `UI/` | `GameUIController.cs` | Connect systems to UI |
| `UI/` | `GameHUDController.cs` | Main HUD, notifications, all systems |
| `Demo/` | `PersistentCubeDemo.cs` | Test scene functionality |
| `Demo/` | `DesktopFallbackCamera.cs` | Windows desktop testing |

#### Privacy & Compliance
| Folder | Script | Purpose |
|--------|--------|---------|
| `Privacy/` | `GDPRManager.cs` | GDPR consent, data export/delete requests |
| `Privacy/` | `AgeGateUI.cs` | COPPA age verification (13+) |
| `Privacy/` | `ConsentDialogUI.cs` | Consent toggles UI |

#### Content Moderation
| Folder | Script | Purpose |
|--------|--------|---------|
| `Moderation/` | `ContentModerationManager.cs` | Client-side filter, server validation |

#### Cosmetics Shop
| Folder | Script | Purpose |
|--------|--------|---------|
| `Cosmetics/` | `CosmeticsShopManager.cs` | Shop UI, purchases, equip management |

#### Performance Monitoring
| Folder | Script | Purpose |
|--------|--------|---------|
| `Monitoring/` | `PerformanceMonitor.cs` | FPS, memory, Crashlytics, analytics |

### Backend Cloud Functions (`backend/functions/src/`)

| File | Lines | Purpose |
|------|-------|---------|
| `index.ts` | ~400 | Function exports and initialization |
| `types/index.ts` | ~500 | TypeScript interfaces |
| `combat.ts` | ~400 | Combat system, attack definitions |
| `progression.ts` | ~300 | XP calculation, leveling |
| `alliance.ts` | ~500 | Alliance management, wars |
| `notifications.ts` | ~400 | Push notification triggers |
| `push.ts` | ~1100 | FCM push system, preferences, topics |
| `iap.ts` | ~1580 | In-app purchases, receipt validation |
| `world-events.ts` | ~650 | 10 event types, FOMO mechanics |
| `season-pass.ts` | ~750 | Battle pass, challenges, rewards |
| `friends.ts` | ~880 | Social features, gifting |
| `anticheat.ts` | ~550 | Location validation, trust scores |
| `analytics.ts` | ~700 | Event tracking, A/B testing |
| `map-api.ts` | ~730 | Real-time map tiles, geohash |
| `referrals.ts` | ~550 | Referral codes, viral challenges |
| `chat.ts` | ~800 | Chat channels, moderation |
| `gdpr.ts` | ~1090 | GDPR compliance, data export/delete |
| `moderation.ts` | ~930 | Content moderation, bans, appeals |
| `cosmetics.ts` | ~800 | Shop catalog, purchases, rotations |

### Admin Dashboard (`admin-dashboard/src/pages/`)

| Page | Purpose |
|------|---------|
| `LoginPage.tsx` | Firebase authentication |
| `DashboardPage.tsx` | Overview stats, DAU/MAU charts |
| `PlayersPage.tsx` | Player management, ban/unban |
| `TerritoriesPage.tsx` | Territory statistics |
| `AlliancesPage.tsx` | Alliance moderation |
| `WorldEventsPage.tsx` | Create/manage events |
| `SeasonPassPage.tsx` | Season management |
| `AnalyticsPage.tsx` | Retention, revenue charts |
| `ModerationPage.tsx` | Reports, suspicious activity |
| `SettingsPage.tsx` | Feature toggles, config |

---

## 🤖 Unity AI Prompts (Ready to Execute)

When Unity access is available, use these prompts with AI tools (Claude, ChatGPT, etc.):

### Quick Reference

| # | Task | Dependencies |
|---|------|--------------|
| 1 | Create PersistentCubeTest scene | None |
| 2 | Generate block prefabs (10 types) | None |
| 3 | Create Territory prefab | Blocks |
| 4 | Create UI Canvas with panels | None |
| 5 | Generate Player visual (avatar) | None |
| 6 | Generate Resource nodes (4 types) | None |
| 7 | Create VFX (attack/defense effects) | None |
| 8 | Create GameManager prefab | All scripts |
| 9 | Create Main Menu scene | UI Canvas |
| 10 | Create Map View scene | Territory prefab |
| 11 | Create Combat scene | Combat scripts |
| 12 | Create Alliance UI | Alliance scripts |
| 13 | Create sound effects | None |

### Full Prompts

**See [docs/REQUIREMENTS.md](docs/REQUIREMENTS.md#13-unity-ai-agent-prompts) for complete, detailed prompts.**

<details>
<summary><strong>Prompt #1: PersistentCubeTest Scene</strong></summary>

```
Create a Unity 6 test scene "PersistentCubeTest" for Apex Citadels.

Scene setup:
- AR Session with ARCoreExtensions for Geospatial
- XR Origin with Main Camera
- Default Cube (1m, blue material) at world origin
- UI Canvas with: Place button, Delete button, Sync status text

Attach scripts:
- PersistentCubeDemo.cs to cube
- SpatialAnchorManager.cs to XR Origin
- AnchorPersistenceService.cs to GameManager object

Configure AR Session:
- Enable Geospatial mode
- Set location permission request

Create a simple flow: tap Place → cube placed at camera forward → anchor created → synced to cloud
```
</details>

<details>
<summary><strong>Prompt #2: Block Prefabs (All 10 Types)</strong></summary>

```
Create 10 building block prefabs for Apex Citadels (AR construction game):

1. Foundation Block: 2x0.5x2m, gray concrete texture, heavy/stable feel
2. Wall Block: 2x2x0.3m, stone texture, castle-like
3. Tower Block: 1x3x1m, vertical stone pillar
4. Roof Block: 2x0.3x2m, slanted (30°), terracotta color
5. Window Block: 0.8x1x0.2m, transparent center, stone frame
6. Door Block: 1x2x0.3m, wood texture with metal hinges
7. Defensive Wall: 2x2.5x0.4m, stone with crenellations on top
8. Turret Base: 1.5x0.5x1.5m, reinforced stone, attachment point on top
9. Resource Storage: 1.5x1x1.5m, wooden crate with metal bands
10. Flag Pole: 0.1x4x0.1m, metal pole with cloth flag (customizable color)

Each prefab needs:
- BoxCollider (accurate bounds)
- BuildingBlock.cs component attached
- Layer set to "Building"
- Appropriate material (create basic materials)
```
</details>

<details>
<summary><strong>Prompt #3: Territory Prefab</strong></summary>

```
Create a Territory prefab for Apex Citadels.

Visual components:
- Boundary ring (20m radius, semi-transparent shader)
- Center monument (obelisk-style, 2m tall)
- Ownership banner (flag with team color)
- Health bar above monument (3D sprite)

Structure:
Territory (empty GameObject)
├── BoundaryRing (cylinder, scaled to 40x0.1x40)
├── Monument (custom mesh or primitive stack)
├── OwnerBanner (plane with flag material)
├── HealthDisplay (world-space Canvas)
├── BuildZone (invisible trigger collider, sphere radius 20)
└── SpawnPoints (4 empty GameObjects at cardinal directions)

Attach Territory.cs and TerritoryManager.cs

Create 3 team color variants: Red, Blue, Yellow
```
</details>

**[View all 13 prompts →](docs/REQUIREMENTS.md#13-unity-ai-agent-prompts)**

---

## 💡 Why Now?

The technology has finally caught up with the vision:

- ✅ **VPS Coverage** - Google/Niantic mapped major cities
- ✅ **5G Networks** - Low latency for real-time multiplayer
- ✅ **XR Glasses** - Consumer devices shipping (Android XR, Vision Pro)
- ✅ **Post-Pandemic** - People want outdoor social gaming

---

## 🤝 Contributing

This is a prototype in active development. If you're interested in contributing:

1. Fork the repository
2. Create a feature branch
3. Submit a pull request

---

## 📄 License

Copyright © 2026 Apex Citadels. All rights reserved.

---

## 📬 Contact

- **Repository**: [github.com/rob637/Apex](https://github.com/rob637/Apex)

---

*"We're not building an app. We're building the layer where the digital and physical worlds merge — and you can own a piece of it."*