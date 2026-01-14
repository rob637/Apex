# Apex Citadels

> **Conquer the Real World** - Build persistent fortresses, claim territory, defend against rivals

![Status](https://img.shields.io/badge/status-prototype-yellow)
![Unity](https://img.shields.io/badge/Unity-6-black)
![Platform](https://img.shields.io/badge/platform-iOS%20%7C%20Android%20%7C%20Windows-blue)

---

## 🌟 What is Apex Citadels?

**The game where you conquer the real world.**

Build a fortress at your local park. Claim the territory. Watch rivals try to take it from you. Every structure persists in the real world — visible to all players, attackable by enemies, yours to defend.

*"I own a fortress at Central Park and I'm defending it from rivals right now."*

---

## 🎮 The Core Loop

```
EXPLORE → CLAIM → BUILD → DEFEND → EXPAND
   ↑                                    |
   └────────────────────────────────────┘
```

| Phase | Action | Emotion |
|-------|--------|---------|
| **🗺️ Explore** | Walk around, find unclaimed spots | Curiosity |
| **🚩 Claim** | Plant your flag, own real territory | Pride |
| **🏗️ Build** | Place blocks and structures | Creativity |
| **⚔️ Defend** | Others attack, you protect | Tension |
| **👑 Expand** | Take more territory | Ambition |

---

## ✨ What Makes It Unique

| Feature | Why It's Special |
|---------|------------------|
| **Real addresses matter** | "I own 123 Main Street" |
| **Physical presence required** | Must BE there to attack/defend |
| **Visible from street** | AR structures visible to passersby |
| **Local rivalries** | School vs school, neighborhood vs neighborhood |
| **Persistent world** | Build today, still there next year |

---

## 🚀 Development Phases

### Phase 1: "Land Rush" (MVP) ✅ *Current*
- [x] Persistent object placement
- [x] Cross-device visibility
- [x] Cloud sync (Firebase)
- [ ] Territory claiming (X meter radius)
- [ ] Basic building blocks
- [ ] Map view of claimed territories

**Success metric:** People build things and come back to see them

### Phase 2: "Fortify"
- [ ] Resource gathering (walk to collect)
- [ ] Building pieces (walls, towers, gates)
- [ ] Territory boundaries visible on map
- [ ] Alliance/team system
- [ ] Daily territory maintenance

**Success metric:** People spend 20+ mins building

### Phase 3: "Conquer"
- [ ] Attack mechanics
- [ ] Defense structures (turrets, traps)
- [ ] Territory wars (scheduled battles)
- [ ] Leaderboards by neighborhood/city
- [ ] Raid notifications

**Success metric:** Retention spikes around battle times

### Phase 4: "Dominate"
- [ ] City-wide events
- [ ] Seasonal competitions
- [ ] Premium cosmetics
- [ ] Spectator mode
- [ ] XR glasses support

---

## 📁 Project Structure

```
Apex/
├── docs/                          # Documentation
│   ├── VISION.md                  # Product vision and strategy
│   ├── TECHNICAL_ARCHITECTURE.md  # System design
│   └── ROADMAP.md                 # Development milestones
│
├── unity/ApexCitadels/           # Unity Project
│   └── Assets/
│       └── Scripts/
│           ├── AR/               # Spatial anchor system
│           ├── Backend/          # Firebase integration
│           └── Demo/             # Persistent Cube Test
│
└── backend/                      # Firebase Backend
    ├── functions/                # Cloud Functions (TypeScript)
    ├── firestore.rules          # Security rules
    └── firestore.indexes.json   # Database indexes
```

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
3. Install required packages (AR Foundation, Firebase)
4. Build to your device

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
| **Backend** | Firebase | Auth, Database, Functions |
| **Real-time** | Photon Fusion | Multiplayer battles (Phase 3) |
| **Storage** | Cloud Firestore | Game state persistence |

---

## 🎯 MVP Feature Priorities

### Must Have (Phase 1)
1. ✅ Persistent object placement
2. ✅ Cloud sync across devices
3. ⬜ Territory claiming with radius
4. ⬜ Basic block building (5-10 block types)
5. ⬜ Map view showing territories

### Should Have (Phase 2)
- Resource nodes on map
- Walking to gather resources
- Building upgrades
- Team/alliance system

### Could Have (Phase 3+)
- Real-time combat
- Defense structures
- Scheduled raid windows
- Leaderboards

---

## 🎮 Unity Setup Instructions

**IMPORTANT:** The scripts in `Assets/Scripts/` are ready but need to be wired up in Unity. Follow these prompts with Unity AI or do manually.

### Step 1: Create Game Manager Object

In your main scene, create an empty GameObject called `GameManager` and add these scripts:
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
- `GameUIController.cs`

**Unity AI Prompt:**
```
Create an empty GameObject called "GameManager" in the hierarchy. Add the following script components to it:
- TerritoryManager
- BuildingManager  
- PlayerManager
- CombatManager
- AllianceManager
- ResourceManager
- NotificationManager
- LeaderboardManager
- AchievementManager
- DailyRewardManager
- GameUIController

Make sure to add the ApexCitadels namespace using directives if needed.
```

### Step 2: Create Map View Canvas (for 2D territory map)

**Unity AI Prompt:**
```
Create a new Canvas called "MapCanvas" with:
1. Set Canvas Scaler to "Scale with Screen Size" at 1080x1920 reference
2. Add a child Panel called "MapContainer" that fills the screen
3. Add MapViewController script to MapCanvas
4. Create a simple marker prefab (small circle image) and assign it to markerPrefab
5. Create a player marker (yellow circle) and assign it to playerMarker
6. Add zoom in/out buttons in the corner
```

### Step 3: Create Main Game UI

**Unity AI Prompt:**
```
Create a game UI Canvas with these elements:
1. Top bar with:
   - Player level and XP progress bar
   - Resource displays (Stone, Wood, Metal, Crystal, Gems) as icons with numbers
   - Notification bell icon with badge count
2. Bottom bar with:
   - "Build" button to open building mode
   - "Map" button to toggle map view
   - "Attack" button (only active when enemy territory selected)
   - "Profile" button for player stats
3. Assign all UI elements to GameUIController in the inspector
```

### Step 4: Create Building System Prefabs

**Unity AI Prompt:**
```
Create prefab variants for each building block type. For each block:
1. Create a cube or appropriate primitive
2. Apply URP Lit material with distinct colors:
   - Stone: Gray (#808080)
   - Wood: Brown (#8B4513)
   - Metal: Silver (#C0C0C0)
   - Glass: Transparent Blue
   - Wall: Dark Gray
   - Tower: Tall cylinder
   - Flag: Thin pole with plane on top
3. Save each as a prefab in Assets/Prefabs/Blocks/
4. Assign the prefabs to BuildingManager's block prefab references
```

### Step 5: Create Territory Visualization

**Unity AI Prompt:**
```
Create a territory boundary visualization:
1. Create a LineRenderer-based circle prefab for territory borders
2. Owned territories: Green circle
3. Enemy territories: Red circle
4. Neutral: Gray circle
5. Add this as a child prefab option in TerritoryManager
```

### Step 6: Connect Firebase (if not already done)

**Unity AI Prompt:**
```
1. Import Firebase Unity SDK (FirebaseFirestore, FirebaseAuth)
2. Download google-services.json from Firebase Console
3. Place in Assets/StreamingAssets/
4. In AnchorPersistenceService, uncomment the Firebase initialization code
5. Add FirebaseApp.CheckAndFixDependenciesAsync() to your startup script
```

### Step 7: Configure for Android Build

**Unity AI Prompt:**
```
1. File → Build Settings → Switch Platform to Android
2. Player Settings:
   - Package Name: com.apexcitadels.game
   - Minimum API Level: 26 (Android 8.0)
   - Target API Level: 34
   - Scripting Backend: IL2CPP
   - Target Architectures: ARM64
3. XR Plug-in Management:
   - Enable ARCore for Android
   - Disable all Windows loaders
4. Add ARCore Extensions package for Geospatial API
```

### Required Packages (Package Manager)

Make sure these packages are installed:
- `com.unity.xr.arfoundation` (AR Foundation)
- `com.unity.xr.arcore` (ARCore XR Plugin)
- `com.unity.xr.arkit` (ARKit XR Plugin - for iOS)
- `com.google.ar.core.arfoundation.extensions` (ARCore Extensions)
- Firebase SDK packages (via .unitypackage from Firebase website)

### Script Dependencies Diagram

```
GameUIController
    ├── PlayerManager (auth, resources, XP)
    ├── TerritoryManager (claim, attack, defend)
    ├── BuildingManager (place blocks)
    ├── CombatManager (attack system)
    ├── AllianceManager (teams)
    ├── ResourceManager (harvesting)
    ├── NotificationManager (alerts)
    ├── LeaderboardManager (rankings)
    ├── AchievementManager (progress tracking)
    └── DailyRewardManager (login streaks)

All managers use Singleton pattern - access via ManagerName.Instance
All managers fire C# events for UI updates
```

### Testing Checklist

Before deploying to device:
- [ ] GameManager object exists with all manager scripts
- [ ] UI Canvas connected to GameUIController
- [ ] Block prefabs assigned to BuildingManager
- [ ] Firebase configured and initialized
- [ ] XR settings correct for target platform
- [ ] Scenes added to Build Settings
- [ ] AR Session and XR Origin in scene (for mobile)
- [ ] DesktopFallbackCamera in scene (for Windows testing)

---

## 📋 Complete Script Inventory

### Core Systems (`Assets/Scripts/`)

| Folder | Script | Purpose |
|--------|--------|---------|
| `AR/` | `SpatialAnchorManager.cs` | AR spatial anchors and positioning |
| `Backend/` | `AnchorPersistenceService.cs` | Firebase cloud sync |
| `Config/` | `AppConfig.cs` | Configuration management |
| `Demo/` | `PersistentCubeDemo.cs` | Test scene functionality |
| `Demo/` | `DesktopFallbackCamera.cs` | Windows desktop testing |
| `Territory/` | `Territory.cs` | Territory data model |
| `Territory/` | `TerritoryManager.cs` | Claim, attack, defend |
| `Building/` | `BuildingBlock.cs` | Block type definitions |
| `Building/` | `BuildingManager.cs` | Place and manage blocks |
| `Player/` | `PlayerProfile.cs` | Player data model |
| `Player/` | `PlayerManager.cs` | Auth, resources, XP |
| `Combat/` | `CombatManager.cs` | Attack mechanics |
| `Alliance/` | `Alliance.cs` | Alliance data models |
| `Alliance/` | `AllianceManager.cs` | Team management |
| `Resources/` | `ResourceManager.cs` | Harvest and gather |
| `Map/` | `MapViewController.cs` | 2D territory map |
| `Notifications/` | `NotificationManager.cs` | Push & in-game alerts |
| `Leaderboard/` | `LeaderboardManager.cs` | Rankings and scores |
| `Achievements/` | `AchievementManager.cs` | Progress tracking |
| `DailyRewards/` | `DailyRewardManager.cs` | Login streaks |
| `UI/` | `GameUIController.cs` | Connect systems to UI |

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