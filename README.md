# Apex Citadels

> **The Spatial Social Sandbox Game** - Build, Battle, and Conquer the Real World

![Status](https://img.shields.io/badge/status-prototype-yellow)
![Unity](https://img.shields.io/badge/Unity-2022.3%20LTS-black)
![Platform](https://img.shields.io/badge/platform-iOS%20%7C%20Android%20%7C%20XR-blue)

---

## 🌟 What is Apex Citadels?

Apex Citadels combines the best of **Pokémon GO** (location-based discovery) and **Fortnite** (creative building & competitive battles) into a persistent augmented reality world.

**Your creations persist.** Build a fortress in your local park today, and players around the world can discover, admire, raid, or conquer it tomorrow.

---

## 🎮 Core Gameplay

### 🗺️ **SCAVENGE** - Explore the Real World
Walk to real-world locations to harvest digital resources. Different environments yield different materials:
- 🪨 **Stone** from brick buildings
- 🌳 **Timber** from parks
- ⚡ **Neon** from commercial areas
- 💎 **Crystal** from glass towers

### 🏗️ **BUILD** - Create Persistent Structures
Use AR to construct **Citadels** - fortresses that exist in the digital layer over reality. Your buildings stay exactly where you place them, visible to all players.

### ⚔️ **BATTLE** - Raid and Defend
Travel to enemy Citadels and launch raids. Use your phone as a tactical window to breach defenses, while defenders scramble to protect their territory.

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

- **Unity 2022.3 LTS** or newer
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

### 5️⃣ Run the Persistent Cube Test

1. Deploy to two physical devices
2. On Device A: Place a cube on a table
3. Close the app on Device A
4. On Device B: Open the app near Device A's location
5. **SUCCESS**: Device B sees the same cube! 🎉

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

If this works, everything else is just content.

---

## 🛠️ Tech Stack

| Layer | Technology | Purpose |
|-------|------------|---------|
| **AR Engine** | Unity AR Foundation | Cross-platform AR |
| **Geospatial** | ARCore Geospatial API | Centimeter-accurate VPS |
| **Backend** | Firebase | Auth, Database, Functions |
| **Real-time** | Photon Fusion | Multiplayer battles |
| **Storage** | Cloud Firestore | Game state persistence |

---

## 📊 Development Phases

| Phase | Duration | Goal |
|-------|----------|------|
| **0. Cube Test** | 2 weeks | Prove persistence technology |
| **1. Core Loop** | 4 weeks | Resources + Building |
| **2. Social** | 6 weeks | Territories + Battles |
| **3. Polish** | 6 weeks | Content + Monetization |
| **4. Soft Launch** | 6 weeks | Test markets |
| **5. Global** | Ongoing | Scale worldwide |

---

## 💡 Why 2026?

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

*"We're not building an app. We're building the metaverse layer on top of reality."*