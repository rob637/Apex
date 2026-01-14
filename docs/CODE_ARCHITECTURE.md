# Apex Citadels - Code Architecture

## Script Structure

```
Assets/Scripts/
├── AR/                     # AR/Spatial systems
│   └── SpatialAnchorManager.cs
│
├── Achievements/           # Progress tracking
│   └── AchievementManager.cs
│
├── Alliance/               # Team system
│   ├── Alliance.cs             # Alliance data models
│   └── AllianceManager.cs      # Team management
│
├── Backend/                # Cloud services
│   └── AnchorPersistenceService.cs
│
├── Building/               # Construction system
│   ├── BuildingBlock.cs        # Block data & definitions
│   └── BuildingManager.cs      # Placement & management
│
├── Combat/                 # Attack system
│   └── CombatManager.cs        # Attack mechanics
│
├── Config/                 # Configuration
│   └── AppConfig.cs
│
├── DailyRewards/           # Login streaks
│   └── DailyRewardManager.cs
│
├── Demo/                   # Test scenes
│   ├── PersistentCubeDemo.cs
│   └── DesktopFallbackCamera.cs
│
├── Leaderboard/            # Rankings
│   └── LeaderboardManager.cs
│
├── Map/                    # 2D Map view
│   └── MapViewController.cs
│
├── Notifications/          # Alerts
│   └── NotificationManager.cs
│
├── Player/                 # Player systems
│   ├── PlayerProfile.cs        # Player data model
│   └── PlayerManager.cs        # Auth & state
│
├── Resources/              # Harvesting
│   └── ResourceManager.cs
│
├── Territory/              # Territory control
│   ├── Territory.cs            # Territory data model
│   └── TerritoryManager.cs     # Claiming & combat
│
└── UI/                     # User interface
    └── GameUIController.cs     # Main game UI
```

---

## System Dependencies

```
┌──────────────────────────────────────────────────────────────────┐
│                         GameUIController                          │
│                    (connects everything to UI)                    │
└───────────────────────────────┬──────────────────────────────────┘
                                │
        ┌───────────────────────┼───────────────────────┐
        │                       │                       │
        ▼                       ▼                       ▼
┌───────────────┐     ┌─────────────────┐     ┌─────────────────┐
│ PlayerManager │     │TerritoryManager │     │ BuildingManager │
│ (auth, xp)    │     │ (claim/defend)  │     │ (construction)  │
└───────┬───────┘     └────────┬────────┘     └────────┬────────┘
        │                      │                       │
        │                      ▼                       │
        │             ┌─────────────────┐              │
        │             │  CombatManager  │              │
        │             │   (attacks)     │              │
        │             └─────────────────┘              │
        │                                              │
        └───────────────────┬──────────────────────────┘
                            │
                            ▼
              ┌─────────────────────────────┐
              │    SpatialAnchorManager     │
              │   (AR positioning)          │
              └──────────────┬──────────────┘
                             │
                             ▼
              ┌─────────────────────────────┐
              │  AnchorPersistenceService   │
              │     (Firebase sync)         │
              └─────────────────────────────┘

Supporting Systems (all singleton):
┌───────────────┐ ┌───────────────┐ ┌────────────────┐ ┌───────────────┐
│AllianceManager│ │ResourceManager│ │LeaderboardMgr  │ │NotificationMgr│
│  (teams)      │ │ (harvesting)  │ │  (rankings)    │ │  (alerts)     │
└───────────────┘ └───────────────┘ └────────────────┘ └───────────────┘

┌────────────────┐ ┌───────────────┐ ┌────────────────┐
│AchievementMgr  │ │DailyRewardMgr │ │MapViewController│
│ (progress)     │ │ (streaks)     │ │  (2D map)      │
└────────────────┘ └───────────────┘ └────────────────┘
```

---

## Data Models

### Territory
```csharp
Territory {
    Id: string
    OwnerId: string
    OwnerName: string
    CenterLatitude: double
    CenterLongitude: double
    RadiusMeters: float      // 10m base, +5m per level
    Level: int               // 1-10
    Health: int              // Damage from attacks
    MaxHealth: int           // 100 + 50 per level
    IsContested: bool
}
```

### BuildingBlock
```csharp
BuildingBlock {
    Id: string
    Type: BlockType          // Stone, Wood, Wall, Tower, etc.
    OwnerId: string
    TerritoryId: string
    Latitude/Longitude: double
    LocalPosition: Vector3
    LocalRotation: Quaternion
    Health: int
}
```

### PlayerProfile
```csharp
PlayerProfile {
    Id: string
    DisplayName: string
    Level: int
    Experience: int
    
    // Resources
    Stone: int
    Wood: int
    Metal: int
    Crystal: int
    
    // Stats
    TerritoriesOwned: int
    BlocksPlaced: int
}
```

---

## Firebase Schema

### Collections

```
/users/{userId}
    - displayName: string
    - level: int
    - experience: int
    - stone: int
    - wood: int
    - metal: int
    - crystal: int
    - gems: int
    - createdAt: timestamp
    - lastLoginAt: timestamp
    - allianceId: string
    - totalTerritoriesCaptured: int
    - attacksWon: int
    - defensesWon: int

/territories/{territoryId}
    - ownerId: string
    - ownerName: string
    - allianceId: string
    - name: string
    - centerLat: number
    - centerLng: number
    - radiusMeters: number
    - level: int
    - health: int
    - maxHealth: int
    - isContested: boolean
    - claimedAt: timestamp
    - geohash: string          // For geo queries

/blocks/{blockId}
    - ownerId: string
    - territoryId: string
    - type: string
    - latitude: number
    - longitude: number
    - altitude: number
    - posX, posY, posZ: number
    - rotX, rotY, rotZ, rotW: number
    - health: int
    - placedAt: timestamp
    - geohash: string

/alliances/{allianceId}
    - name: string
    - tag: string              // 2-4 char tag [APEX]
    - leaderId: string
    - leaderName: string
    - description: string
    - members: array
    - totalTerritories: int
    - weeklyXP: int
    - allTimeXP: int
    - isOpen: boolean
    - minLevelToJoin: int
    - createdAt: timestamp

/invitations/{invitationId}
    - allianceId: string
    - allianceName: string
    - inviterId: string
    - inviterName: string
    - inviteeId: string
    - createdAt: timestamp
    - expiresAt: timestamp
    - accepted: boolean
    - declined: boolean

/wars/{warId}
    - attackingAllianceId: string
    - attackingAllianceName: string
    - defendingAllianceId: string
    - defendingAllianceName: string
    - startTime: timestamp
    - endTime: timestamp
    - attackerScore: int
    - defenderScore: int
    - status: string
    - winnerId: string
    - battles: array

/leaderboards/{category}/entries/{userId}
    - playerId: string
    - playerName: string
    - allianceTag: string
    - level: int
    - score: number
    - region: string
    - lastActive: timestamp

/resourceNodes/{nodeId}
    - type: string
    - latitude: number
    - longitude: number
    - currentAmount: int
    - maxAmount: int
    - territoryId: string
    - ownerId: string
    - regenerationTime: timestamp
    - geohash: string

/anchors/{anchorId}           // Existing spatial anchors
    - (existing fields)
```

---

## Key Flows

### Territory Claiming
```
1. Player taps "Claim Territory"
2. TerritoryManager.TryClaimTerritory(lat, lng)
3. Check: Player hasn't reached max territories
4. Check: No overlapping territories
5. Create Territory object
6. Save to Firebase
7. Create visual boundary
8. Award XP
```

### Block Placement
```
1. Player selects block type
2. BuildingManager.EnterPlacementMode()
3. Show ghost preview following AR raycast
4. Player confirms placement
5. Check: Valid position, has resources
6. Create BuildingBlock object
7. Spawn visual prefab
8. Save to Firebase
9. Deduct resources, award XP
```

### Territory Attack
```
1. Player approaches enemy territory
2. Player taps "Attack"
3. TerritoryManager.AttackTerritory(id, damage)
4. Reduce territory health
5. If health <= 0: Transfer ownership
6. Update Firebase
7. Notify defender (push notification)
```

---

## Unity Scene Setup (for Unity AI)

### Required GameObjects
```
Scene: PersistentCubeDemo
├── AR Session (disabled for desktop)
├── XR Origin (disabled for desktop)
├── GameManager (empty GameObject)
│   ├── PlayerManager
│   ├── TerritoryManager
│   ├── BuildingManager
│   ├── CombatManager
│   ├── AllianceManager
│   ├── ResourceManager
│   ├── NotificationManager
│   ├── LeaderboardManager
│   ├── AchievementManager
│   ├── DailyRewardManager
│   └── GameUIController
├── SpatialAnchorManager
├── AnchorPersistenceService
├── Camera (MainCamera tag)
├── MapCanvas (for 2D map view)
│   ├── MapContainer
│   ├── PlayerMarker
│   ├── ZoomButtons
│   └── MapViewController
├── GameCanvas
│   ├── TopBar
│   │   ├── PlayerLevelDisplay
│   │   ├── XPProgressBar
│   │   ├── ResourcesPanel (Stone, Wood, Metal, Crystal, Gems)
│   │   └── NotificationBell
│   ├── CenterPanel
│   │   ├── StatusText
│   │   └── ToastContainer
│   ├── BottomBar
│   │   ├── BuildButton
│   │   ├── MapButton
│   │   ├── AttackButton
│   │   └── ProfileButton
│   └── Popups
│       ├── BuildingPanel
│       ├── TerritoryInfoPanel
│       ├── AlliancePanel
│       ├── LeaderboardPanel
│       ├── AchievementsPanel
│       └── DailyRewardPanel
├── Directional Light
└── Environment (Plane, test objects)
```

### Prefabs Needed
```
Assets/Prefabs/
├── Blocks/
│   ├── StoneBlock.prefab
│   ├── WoodBlock.prefab
│   ├── MetalBlock.prefab
│   ├── GlassBlock.prefab
│   ├── WallBlock.prefab
│   ├── GateBlock.prefab
│   ├── TowerBlock.prefab
│   ├── TurretBlock.prefab
│   ├── FlagBlock.prefab
│   ├── BannerBlock.prefab
│   ├── TorchBlock.prefab
│   └── BeaconBlock.prefab
├── Territory/
│   ├── TerritoryBoundary.prefab (LineRenderer circle)
│   └── TerritoryFlag.prefab
├── Map/
│   ├── MapMarker.prefab
│   └── PlayerMarker.prefab
├── UI/
│   ├── ToastNotification.prefab
│   ├── ResourceNodeIndicator.prefab
│   └── DamageNumber.prefab
└── Resources/
    ├── StoneMine.prefab
    ├── Forest.prefab
    ├── OreDeposit.prefab
    ├── CrystalCave.prefab
    └── GemMine.prefab
```

---

## Testing Checklist

### Desktop Testing
- [ ] GameManager exists with all managers
- [ ] Place blocks on plane
- [ ] Claim territory (mock GPS)
- [ ] See territory boundary
- [ ] Switch block types
- [ ] Resource display updates
- [ ] Daily reward popup works
- [ ] Achievements track progress
- [ ] Leaderboard loads mock data

### Mobile Testing (Android)
- [ ] AR plane detection
- [ ] Place blocks in AR
- [ ] GPS territory claiming
- [ ] Cross-device persistence
- [ ] Territory boundaries in AR
- [ ] Push notifications work
- [ ] Map view shows real location
- [ ] Resource nodes appear nearby
