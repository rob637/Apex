# Apex Citadels - Comprehensive Requirements & World-Class Workplan

> **Last Updated:** January 14, 2026  
> **Version:** 2.0  
> **Status:** Pre-Alpha Development (Scripts Complete, Awaiting Unity Integration)  
> **Target:** Android MVP → Cross-Platform Global Launch

---

## 📋 Table of Contents

1. [Executive Summary](#executive-summary)
2. [Product Overview](#product-overview)
3. [Technology Stack (2026 Best-in-Class)](#technology-stack)
4. [Feature Requirements by Phase](#feature-requirements-by-phase)
5. [Technical Requirements](#technical-requirements)
6. [Backend Architecture](#backend-architecture)
7. [Security & Compliance](#security--compliance)
8. [Asset Requirements](#asset-requirements)
9. [Development Workplan](#development-workplan)
10. [Unity AI Prompts (Pending Unity Access)](#unity-ai-prompts)
11. [Testing & QA Strategy](#testing--qa-strategy)
12. [DevOps & CI/CD Pipeline](#devops--cicd-pipeline)
13. [Performance Benchmarks](#performance-benchmarks)
14. [Monetization Strategy](#monetization-strategy)
15. [Launch Strategy](#launch-strategy)

---

## 🚀 Executive Summary

**Apex Citadels** is a next-generation **Spatial Social Sandbox Game** positioned to capture the emerging XR gaming market in 2026. By combining the addictive discovery mechanics of Pokémon GO with the creative depth of Fortnite and the territorial warfare of Clash of Clans, we're creating an entirely new genre: **Persistent AR Territory Games**.

### Why 2026 is the Perfect Moment
| Technology Enabler | Maturity | Impact |
|--------------------|----------|--------|
| VPS (Visual Positioning System) | ✅ Production Ready | Centimeter-accurate persistence |
| ARCore Geospatial API | ✅ Global Coverage | 100+ countries mapped |
| Android XR / Apple Vision Pro | 🔄 Launching 2026 | XR glasses go mainstream |
| 5G/Edge Computing | ✅ Widespread | Sub-20ms latency for real-time battles |
| AI Asset Generation | ✅ Production Quality | Rapid content creation |

### Current Project Status
| Component | Status | Completeness |
|-----------|--------|--------------|
| Vision & Design Docs | ✅ Complete | 100% |
| C# Game Scripts | ✅ Complete | 20/20 Scripts |
| Firebase Backend | ✅ Functional | 70% (needs Cloud Functions expansion) |
| Unity Scene Setup | ⏳ Awaiting Unity Access | 0% |
| 3D Assets | ⏳ Not Started | 0% |
| Testing | ⏳ Blocked | 0% |

---

## 🎯 Product Overview

### Vision Statement
> *"Every street corner is a canvas. Every city block is a battlefield. Build your legacy in the real world."*

### Core Value Proposition
| Differentiator | Traditional AR Games | Apex Citadels |
|----------------|---------------------|---------------|
| Content Model | Developer-created POIs | Player-generated worlds |
| Persistence | Session-based | Permanent (years) |
| Social | Async collection | Real-time territorial warfare |
| Ownership | Virtual badges | Real addresses ("I own 123 Main St") |
| Platform | Phone only | Phone + Tablet + XR Glasses |

### Target Platforms (Priority Order)
| Platform | Priority | Timeline | Status |
|----------|----------|----------|--------|
| Android (ARCore) | P0 | Q1 2026 | 🔄 In Development |
| iOS (ARKit) | P1 | Q2 2026 | ⏳ Planned |
| Android XR Glasses | P2 | Q3 2026 | ⏳ Research |
| Meta Quest 3S | P3 | Q4 2026 | ⏳ Research |
| Apple Vision Pro | P4 | 2027 | ⏳ Future |

### Success Metrics

#### MVP (Phase 1) - "Land Rush"
| Metric | Target | Measurement |
|--------|--------|-------------|
| Persistent Objects | 100+ cubes/structures | Firebase analytics |
| Unique Locations | 10+ distinct GPS coords | Geohash diversity |
| Cross-Device Sync | <3 second latency | Performance logs |
| Session Length | 10+ minutes average | Analytics |
| Day-1 Retention | 40%+ | Cohort analysis |

#### Growth (Phase 2-3) - "Fortify & Conquer"
| Metric | Target | Measurement |
|--------|--------|-------------|
| DAU | 10,000+ | Analytics |
| Territories Claimed | 1,000+ | Database count |
| Alliance Formation | 100+ active alliances | Database count |
| Session Length | 25+ minutes | Analytics |
| Week-1 Retention | 25%+ | Cohort analysis |

#### Scale (Phase 4+) - "Dominate"
| Metric | Target | Measurement |
|--------|--------|-------------|
| MAU | 100,000+ | Analytics |
| Revenue/User | $0.50+ ARPDAU | Revenue analytics |
| Global Coverage | 50+ countries | Geo analytics |
| XR Adoption | 10%+ on glasses | Device analytics |

---

## �️ Technology Stack

### 2026 Best-in-Class Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            CLIENT LAYER (Unity 6)                           │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │  📱 Android  │  │   🍎 iOS     │  │ 🥽 Android   │  │ 🖥️ Desktop   │    │
│  │   ARCore     │  │   ARKit      │  │   XR/Quest   │  │   Testing    │    │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘    │
│         └────────────┬────┴─────────────────┴────────────────┘             │
│                      │                                                      │
│              ┌───────▼───────────────────────────────┐                     │
│              │     AR Foundation 6.x + XR Core      │                      │
│              │   + ARCore Geospatial Extensions     │                      │
│              └───────┬───────────────────────────────┘                     │
└──────────────────────┼──────────────────────────────────────────────────────┘
                       │
                       │  HTTPS/WSS + Protocol Buffers
                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           API GATEWAY LAYER                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │              Firebase App Check + API Gateway (GCP)                 │   │
│  │                     Rate Limiting • Auth • Routing                  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
                       │
          ┌────────────┼────────────┬────────────────┬─────────────┐
          ▼            ▼            ▼                ▼             ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          MICROSERVICES LAYER                                │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐     │
│  │   Auth   │  │ Spatial  │  │  Combat  │  │  Social  │  │ Economy  │     │
│  │ Service  │  │ Anchors  │  │ Service  │  │ Service  │  │ Service  │     │
│  │(Firebase)│  │ (Custom) │  │ (Photon) │  │(Firebase)│  │(Firebase)│     │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘  └──────────┘     │
│                                                                             │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐     │
│  │Analytics │  │ Push     │  │ CDN      │  │ ML/AI    │  │ Admin    │     │
│  │(BigQuery)│  │ (FCM)    │  │(CloudFlr)│  │(Vertex)  │  │ (Custom) │     │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘  └──────────┘     │
└─────────────────────────────────────────────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            DATA LAYER                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │  Firestore   │  │   Redis      │  │ Cloud        │  │  BigQuery    │   │
│  │(Primary DB)  │  │  (Cache)     │  │ Storage      │  │ (Analytics)  │   │
│  │- Users       │  │- Leaderboards│  │- 3D Assets   │  │- Events      │   │
│  │- Territories │  │- Session     │  │- User UGC    │  │- Funnels     │   │
│  │- Citadels    │  │- Hot Data    │  │- Backups     │  │- ML Training │   │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Client Stack (Unity 6000.x LTS)

| Component | Technology | Version | Purpose |
|-----------|------------|---------|---------|
| Game Engine | Unity 6 | 6000.0.x LTS | Cross-platform development |
| Rendering | URP | 17.x | Mobile-optimized graphics |
| AR Framework | AR Foundation | 6.0+ | Cross-platform AR abstraction |
| Android AR | ARCore + Extensions | 1.42+ | Geospatial API, Cloud Anchors |
| iOS AR | ARKit | 6.0 | LiDAR, World Tracking |
| XR Support | XR Interaction Toolkit | 3.0+ | Future glasses support |
| Networking | Photon Fusion 2 | Latest | Real-time multiplayer |
| UI System | UI Toolkit | Native | Modern declarative UI |
| Addressables | Addressables | 2.x | Asset streaming |
| Dependency Injection | VContainer | Latest | Clean architecture |
| Reactive | UniRx / R3 | Latest | Event-driven architecture |

### Backend Stack (Serverless-First)

| Component | Technology | Purpose | Cost Model |
|-----------|------------|---------|------------|
| Auth | Firebase Auth | Social login, anonymous upgrade | Free tier generous |
| Database | Cloud Firestore | Real-time sync, offline support | Pay per operation |
| Functions | Cloud Functions v2 | Serverless game logic | Pay per invocation |
| Cache | Upstash Redis | Leaderboards, hot data | Pay per request |
| Storage | Cloud Storage | 3D assets, UGC | Pay per GB |
| CDN | CloudFlare R2 | Global asset delivery | Free egress |
| Real-time | Photon Cloud | Battle synchronization | CCU-based |
| Push | Firebase Cloud Messaging | Territory alerts | Free |
| Analytics | BigQuery + Looker | Deep analytics | Pay per query |
| ML | Vertex AI | Content moderation, cheating detection | Pay per prediction |
| Monitoring | Datadog | APM, logging, alerts | Usage-based |

### DevOps Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| Version Control | GitHub | Source control, collaboration |
| CI/CD | GitHub Actions | Automated testing, deployment |
| Unity Cloud | Unity Build Automation | Multi-platform builds |
| IaC | Terraform | Infrastructure as code |
| Secrets | Google Secret Manager | API keys, credentials |
| Feature Flags | LaunchDarkly / Firebase RC | Gradual rollouts |

---

## 📦 Feature Requirements by Phase

### Phase 1: "Land Rush" (MVP) - Current Focus 🎯

**Goal:** Prove the core technology works and creates engagement

#### F1.1 Territory System ✅ Code Complete
| ID | Requirement | Description | Status | Priority |
|----|-------------|-------------|--------|----------|
| F1.1.1 | GPS-based claiming | Claim territory at current GPS location | ✅ Script | P0 |
| F1.1.2 | Territory radius | 20m base radius per territory | ✅ Script | P0 |
| F1.1.3 | Level scaling | Radius expands +5m per level | ✅ Script | P1 |
| F1.1.4 | Claim limits | Maximum 5 territories per player (MVP) | ✅ Script | P0 |
| F1.1.5 | Overlap prevention | Territories cannot overlap | ✅ Script | P0 |
| F1.1.6 | AR visualization | Glowing dome/circle boundary in AR | ⬜ Unity | P1 |
| F1.1.7 | Map display | Territory circles on 2D overhead map | ⬜ Unity | P1 |
| F1.1.8 | Claim animation | Flag-planting or beacon activation effect | ⬜ Unity | P2 |

#### F1.2 Building System ✅ Code Complete
| ID | Requirement | Description | Status | Priority |
|----|-------------|-------------|--------|----------|
| F1.2.1 | Block variety | 12 block types available | ✅ Script | P0 |
| F1.2.2 | Ghost preview | Translucent placement preview | ✅ Script | P0 |
| F1.2.3 | Grid snapping | 0.5m snap grid for alignment | ✅ Script | P1 |
| F1.2.4 | Resource costs | Each block costs specific resources | ✅ Script | P0 |
| F1.2.5 | Persistence | Blocks persist across sessions | ✅ Script | P0 |
| F1.2.6 | Block prefabs | 3D models with materials/physics | ⬜ Unity | P0 |
| F1.2.7 | Building UI | Selection panel with costs | ⬜ Unity | P0 |
| F1.2.8 | Undo system | Remove last placed block | ⬜ Script | P2 |

#### F1.3 Player System ✅ Code Complete
| ID | Requirement | Description | Status | Priority |
|----|-------------|-------------|--------|----------|
| F1.3.1 | Anonymous auth | Play immediately, upgrade later | ✅ Script | P0 |
| F1.3.2 | Resource tracking | 5 resource types with persistence | ✅ Script | P0 |
| F1.3.3 | Level/XP | Progression system with milestones | ✅ Script | P0 |
| F1.3.4 | Cloud sync | Profile persists across devices | ✅ Script | P0 |
| F1.3.5 | Profile UI | Display stats, level, resources | ⬜ Unity | P1 |
| F1.3.6 | Social login | Google, Apple, Facebook upgrade | ⬜ Script | P1 |

#### F1.4 Map View ✅ Code Complete
| ID | Requirement | Description | Status | Priority |
|----|-------------|-------------|--------|----------|
| F1.4.1 | 2D overhead | Bird's eye view of area | ✅ Script | P0 |
| F1.4.2 | Owned territories | Green circles/markers | ✅ Script | P0 |
| F1.4.3 | Enemy territories | Red circles/markers | ✅ Script | P0 |
| F1.4.4 | Neutral areas | Gray/unclaimed visualization | ✅ Script | P1 |
| F1.4.5 | Player position | Real-time GPS marker | ✅ Script | P0 |
| F1.4.6 | Zoom controls | Pinch or button zoom | ✅ Script | P1 |
| F1.4.7 | Selection | Tap territory for details | ✅ Script | P1 |
| F1.4.8 | Map Canvas | Full implementation in Unity | ⬜ Unity | P0 |

#### F1.5 Basic Combat ✅ Code Complete
| ID | Requirement | Description | Status | Priority |
|----|-------------|-------------|--------|----------|
| F1.5.1 | Attack action | Initiate attack on enemy territory | ✅ Script | P0 |
| F1.5.2 | Attack types | 3 attack types with cooldowns | ✅ Script | P1 |
| F1.5.3 | Health system | Territory HP degrades with attacks | ✅ Script | P0 |
| F1.5.4 | Conquest | Territory transfers at 0 HP | ✅ Script | P0 |
| F1.5.5 | Raid windows | 6-10 PM local time attacks only | ✅ Script | P2 |
| F1.5.6 | VFX/SFX | Attack animations and sound | ⬜ Unity | P1 |

---

### Phase 2: "Fortify" - Engagement Features

**Goal:** Add depth, progression, and social systems to drive retention

#### F2.1 Resource Gathering ✅ Code Complete
| ID | Requirement | Description | Status | Priority |
|----|-------------|-------------|--------|----------|
| F2.1.1 | World nodes | Resource nodes spawn in real world | ✅ Script | P0 |
| F2.1.2 | Node types | Stone, Forest, Ore, Crystal, Gem | ✅ Script | P0 |
| F2.1.3 | Proximity harvest | Walk within 5m to collect | ✅ Script | P0 |
| F2.1.4 | Regeneration | Nodes respawn after cooldown | ✅ Script | P1 |
| F2.1.5 | Passive income | Owned nodes generate resources | ✅ Script | P1 |
| F2.1.6 | Node visuals | AR 3D models for each type | ⬜ Unity | P1 |

#### F2.2 Alliance System ✅ Code Complete
| ID | Requirement | Description | Status | Priority |
|----|-------------|-------------|--------|----------|
| F2.2.1 | Alliance creation | Create for 1000 gems | ✅ Script | P0 |
| F2.2.2 | Open joining | Join public alliances | ✅ Script | P0 |
| F2.2.3 | Invitations | Invite players by ID | ✅ Script | P1 |
| F2.2.4 | Role hierarchy | Leader, Officer, Member | ✅ Script | P1 |
| F2.2.5 | Alliance wars | Coordinated territory battles | ✅ Script | P2 |
| F2.2.6 | Alliance UI | Full management panel | ⬜ Unity | P1 |

#### F2.3 Progression Systems ✅ Code Complete
| ID | Requirement | Description | Status | Priority |
|----|-------------|-------------|--------|----------|
| F2.3.1 | Achievements | 30+ achievements across categories | ✅ Script | P1 |
| F2.3.2 | Daily rewards | 7-day login cycle | ✅ Script | P0 |
| F2.3.3 | Streak bonuses | Multipliers for consecutive days | ✅ Script | P1 |
| F2.3.4 | Leaderboards | Global, regional, weekly | ✅ Script | P1 |
| F2.3.5 | Achievement UI | Panel with progress tracking | ⬜ Unity | P1 |
| F2.3.6 | Reward popup | Daily reward claim flow | ⬜ Unity | P0 |
| F2.3.7 | Leaderboard UI | Rankings display | ⬜ Unity | P1 |

#### F2.4 Notifications ✅ Code Complete
| ID | Requirement | Description | Status | Priority |
|----|-------------|-------------|--------|----------|
| F2.4.1 | In-game toasts | Contextual popup messages | ✅ Script | P0 |
| F2.4.2 | Push notifications | Territory attack alerts | ✅ Script | P0 |
| F2.4.3 | Notification history | Scrollable message log | ✅ Script | P2 |
| F2.4.4 | Toast prefab | Animated UI component | ⬜ Unity | P1 |

---

### Phase 3: "Conquer" - Competitive Features

**Goal:** Create compelling PvP that drives daily engagement

| ID | Feature | Description | Status | Priority |
|----|---------|-------------|--------|----------|
| F3.1 | Defense structures | Turrets, walls, traps | ⬜ Not Started | P1 |
| F3.2 | Real-time battles | Synchronous multiplayer combat | ⬜ Not Started | P1 |
| F3.3 | Spectator mode | Watch ongoing battles | ⬜ Not Started | P2 |
| F3.4 | Battle replay | Review past battles | ⬜ Not Started | P3 |
| F3.5 | Siege mechanics | Coordinated alliance attacks | ⬜ Not Started | P2 |

---

### Phase 4: "Dominate" - Scale & Monetization

**Goal:** Global launch, sustainable revenue, XR expansion

| ID | Feature | Description | Status | Priority |
|----|---------|-------------|--------|----------|
| F4.1 | City-wide events | Weekly/monthly competitions | ⬜ Not Started | P1 |
| F4.2 | Seasons | 3-month themed content cycles | ⬜ Not Started | P1 |
| F4.3 | Premium cosmetics | Skins, effects, decorations | ⬜ Not Started | P0 |
| F4.4 | Battle pass | Tiered progression rewards | ⬜ Not Started | P1 |
| F4.5 | XR glasses mode | Optimized for Android XR/Quest | ⬜ Not Started | P2 |
| F4.6 | Content creator tools | Custom events, tournaments | ⬜ Not Started | P3 |
| F3.3 | Spectator mode | ⬜ Not Started |
| F3.4 | City-wide events | ⬜ Not Started |
| F3.5 | Premium cosmetics store | ⬜ Not Started |

---

## 🔧 Technical Requirements

### T1. Backend Services (Firebase + GCP)

| ID | Requirement | Description | Status | Notes |
|----|-------------|-------------|--------|-------|
| T1.1 | Firestore setup | Database configured | ✅ Done | Multi-region enabled |
| T1.2 | Security rules | Production-ready rules | ✅ Done | Needs audit |
| T1.3 | Anonymous auth | Instant play capability | ✅ Done | Working |
| T1.4 | Cloud Functions | Server-side validation | 🔄 Partial | Need combat validation |
| T1.5 | Geohash indexing | Spatial queries | 🔄 Partial | Basic implementation |
| T1.6 | Push notifications | FCM integration | ⬜ Not Started | Critical for engagement |
| T1.7 | Rate limiting | Abuse prevention | ✅ Done | 10 anchors/hour |
| T1.8 | Data backups | Automated backups | ⬜ Not Started | Required for launch |

### T2. AR/Spatial Platform

| ID | Requirement | Description | Status | Notes |
|----|-------------|-------------|--------|-------|
| T2.1 | AR Foundation | Base AR setup | ✅ Done | v6.x ready |
| T2.2 | ARCore Android | Android AR support | ✅ Done | Working |
| T2.3 | Plane detection | Surface recognition | ✅ Done | Horizontal planes |
| T2.4 | Local anchors | Session persistence | ✅ Done | Working |
| T2.5 | Geospatial API | Global VPS positioning | ⬜ Not Started | Priority for MVP |
| T2.6 | Cloud Anchors | Multi-device sharing | ⬜ Not Started | Priority for MVP |
| T2.7 | Depth occlusion | Real-world hiding | ⬜ Not Started | LiDAR devices |
| T2.8 | Semantic segmentation | Surface understanding | ⬜ Not Started | Advanced feature |

### T3. Real-time Multiplayer

| ID | Requirement | Description | Status | Notes |
|----|-------------|-------------|--------|-------|
| T3.1 | Photon setup | Network infrastructure | ⬜ Not Started | Phase 3 |
| T3.2 | State sync | Real-time position sync | ⬜ Not Started | 20 tick/sec |
| T3.3 | Lobby system | Battle matchmaking | ⬜ Not Started | Phase 3 |
| T3.4 | Anti-cheat | Client validation | ⬜ Not Started | Server authority |
| T3.5 | Latency compensation | Smooth experience | ⬜ Not Started | Interpolation |

### T4. Performance Targets

| ID | Requirement | Target | Status | Priority |
|----|-------------|--------|--------|----------|
| T4.1 | Frame rate | 60 FPS on Pixel 5 | ⬜ Untested | P0 |
| T4.2 | App size | < 100MB initial | ⬜ Untested | P1 |
| T4.3 | RAM usage | < 300MB peak | ⬜ Untested | P1 |
| T4.4 | Load time | < 5s to gameplay | ⬜ Untested | P0 |
| T4.5 | Battery | < 15%/hour active | ⬜ Untested | P1 |
| T4.6 | Network | < 1MB/minute data | ⬜ Untested | P2 |
| T4.7 | Offline | Core features offline | ⬜ Untested | P2 |

---

## 🔐 Security & Compliance

### Security Requirements

| Requirement | Implementation | Status |
|-------------|---------------|--------|
| Authentication | Firebase Auth with App Check | ✅ |
| Data Encryption | TLS 1.3 in transit, AES-256 at rest | ✅ |
| API Security | Rate limiting, request validation | ✅ |
| Input Validation | Server-side sanitization | 🔄 |
| Anti-Cheat | GPS spoofing detection | ⬜ |
| Device Attestation | SafetyNet/Play Integrity | ⬜ |

### Compliance Requirements

| Regulation | Requirement | Status |
|------------|-------------|--------|
| GDPR | Data export, deletion rights | ⬜ |
| CCPA | Privacy policy, opt-out | ⬜ |
| COPPA | Age gate (13+) | ⬜ |
| App Store | Review guidelines compliance | ⬜ |
| Play Store | Policy compliance | ⬜ |

---

## 🎨 Asset Requirements

### A1. Building Block Assets (12 Total)

| ID | Asset | Geometry | Material | Status | AI Prompt |
|----|-------|----------|----------|--------|-----------|
| A1.1 | Stone Block | 1m³ Cube | Gray rocky PBR | ⬜ | "Stylized stone brick texture, gray with blue tint, weathered, game asset, seamless tileable" |
| A1.2 | Wood Block | 1m³ Cube | Brown plank PBR | ⬜ | "Stylized wooden plank texture, warm brown oak, visible grain, game asset" |
| A1.3 | Metal Block | 1m³ Cube | Silver metallic | ⬜ | "Brushed steel plate texture, silver with rust spots, sci-fi industrial" |
| A1.4 | Glass Block | 1m³ Cube | Transparent blue | ⬜ | "Holographic glass material, transparent blue, energy field effect" |
| A1.5 | Wall Block | 2x1x0.3m | Brick texture | ⬜ | "Medieval castle wall texture, large stone bricks, mortar lines" |
| A1.6 | Gate Block | 2x2.5m Arch | Iron texture | ⬜ | "Fortress iron gate, ornate metalwork, medieval fantasy" |
| A1.7 | Tower Block | 0.5r x 2h Cylinder | Stone texture | ⬜ | "Castle tower texture, cylindrical stone, arrow slits" |
| A1.8 | Turret Block | Mechanical | Steel/tech | ⬜ | "Futuristic defense turret, rotating mechanism, energy weapon" |
| A1.9 | Flag Block | Pole + cloth | Fabric physics | ⬜ | "Victory flag on pole, fabric physics, customizable colors" |
| A1.10 | Banner Block | Hanging fabric | Cloth texture | ⬜ | "Medieval hanging banner, heraldic design, cloth material" |
| A1.11 | Torch Block | Pole + flame | Emissive fire | ⬜ | "Wall-mounted torch, flickering flame VFX, warm light" |
| A1.12 | Beacon Block | Crystal shape | Emissive glow | ⬜ | "Magical beacon crystal, pulsing blue energy, territory marker" |

### A2. Resource Node Assets (5 Total)

| ID | Asset | Style | AR Scale | Status |
|----|-------|-------|----------|--------|
| A2.1 | Stone Quarry | Rock pile | 2m diameter | ⬜ |
| A2.2 | Forest Grove | Tree cluster | 3m diameter | ⬜ |
| A2.3 | Ore Deposit | Metal veins | 1.5m diameter | ⬜ |
| A2.4 | Crystal Cave | Glowing crystals | 2m diameter | ⬜ |
| A2.5 | Gem Mine | Sparkling gems | 1.5m diameter | ⬜ |

### A3. UI Assets

| Category | Count | Style | Resolution | Status |
|----------|-------|-------|------------|--------|
| Resource Icons | 5 | Flat + gradient | 128x128 | ⬜ |
| Action Buttons | 8 | Rounded, shadowed | 256x256 | ⬜ |
| Map Markers | 10 | Top-down, clean | 64x64 | ⬜ |
| Achievement Badges | 30+ | Circular, metallic | 256x256 | ⬜ |
| Alliance Emblems | Template | Customizable | 512x512 | ⬜ |
| Progress Bars | 3 variants | Animated | Scalable | ⬜ |

### A4. VFX Assets

| Effect | Type | Priority | Status |
|--------|------|----------|--------|
| Placement preview | Hologram shader | P0 | ⬜ |
| Territory boundary | Line renderer + shader | P0 | ⬜ |
| Attack impact | Particle explosion | P1 | ⬜ |
| Resource harvest | Floating particles | P1 | ⬜ |
| Level up | Golden burst | P2 | ⬜ |
| Conquest | Flag animation | P2 | ⬜ |

### A5. Audio Assets

| Category | Count | Source | Status |
|----------|-------|--------|--------|
| Block placement | 4 variants | Freesound/AI | ⬜ |
| Attack sounds | 6 variants | Freesound/AI | ⬜ |
| Harvest sounds | 5 variants | Freesound/AI | ⬜ |
| UI feedback | 10+ variants | Freesound/AI | ⬜ |
| Victory fanfare | 1 | Composed/AI | ⬜ |
| Ambient loops | 3 | Composed/AI | ⬜ |

---

## 📅 Development Workplan

### Phase 1: MVP Foundation (Weeks 1-4)

#### Sprint 1 (Week 1-2): Unity Scene Setup ⏳ BLOCKED - Awaiting Unity Access

| Day | Task | Owner | Deliverable | Status |
|-----|------|-------|-------------|--------|
| 1-2 | Scene architecture | Dev | MainGame scene with managers | ⬜ |
| 3-4 | Core UI canvas | Dev | GameCanvas with all panels | ⬜ |
| 5-6 | Block prefabs | Dev | 12 primitive block prefabs | ⬜ |
| 7 | Integration test | QA | All managers initialize | ⬜ |

**Sprint 1 Definition of Done:**
- [ ] GameManager with all 11 manager scripts attached
- [ ] GameCanvas with TopBar, BottomBar, StatusPanel
- [ ] BuildingPanel with 12 block buttons
- [ ] MapCanvas with basic layout
- [ ] All scripts compile without errors

#### Sprint 2 (Week 3-4): AR & Firebase Integration

| Day | Task | Owner | Deliverable | Status |
|-----|------|-------|-------------|--------|
| 8-10 | AR session setup | Dev | Plane detection working | ⬜ |
| 11-12 | Firebase connection | Dev | Auth + Firestore sync | ⬜ |
| 13-14 | Block placement in AR | Dev | Tap to place blocks | ⬜ |

**Sprint 2 Definition of Done:**
- [ ] AR camera activates on Android
- [ ] Planes detected and visualized
- [ ] Tap on plane places selected block
- [ ] Block position persists to Firebase
- [ ] Block loads on app restart

---

### Phase 2: Core Gameplay (Weeks 5-8)

#### Sprint 3 (Week 5-6): Territory & Map

| Task | Description | Priority | Status |
|------|-------------|----------|--------|
| Territory claiming | GPS-based claim with radius | P0 | ⬜ |
| Territory visualization | AR boundary (dome/circle) | P1 | ⬜ |
| Map view | 2D overhead with markers | P0 | ⬜ |
| Cross-device sync | Territories visible to all | P0 | ⬜ |

#### Sprint 4 (Week 7-8): Combat & Resources

| Task | Description | Priority | Status |
|------|-------------|----------|--------|
| Attack mechanics | Tap enemy territory to attack | P0 | ⬜ |
| Damage feedback | Health bars, VFX, sounds | P1 | ⬜ |
| Resource nodes | Spawn and harvest | P0 | ⬜ |
| Conquest flow | Territory transfer | P0 | ⬜ |

**Phase 2 Definition of Done:**
- [ ] Can claim 5 territories at different GPS locations
- [ ] Territories visible on map and in AR
- [ ] Can attack enemy territories
- [ ] Resources collect from nodes
- [ ] Territory changes owner at 0 HP

---

### Phase 3: Engagement Features (Weeks 9-12)

#### Sprint 5 (Week 9-10): Progression Systems

| Task | Description | Priority | Status |
|------|-------------|----------|--------|
| Daily rewards | 7-day cycle with popup | P0 | ⬜ |
| Achievements | 30 achievements with UI | P1 | ⬜ |
| Leaderboards | Global/regional rankings | P1 | ⬜ |
| Push notifications | Territory attack alerts | P0 | ⬜ |

#### Sprint 6 (Week 11-12): Social & Polish

| Task | Description | Priority | Status |
|------|-------------|----------|--------|
| Alliance creation | Create/join teams | P1 | ⬜ |
| Alliance UI | Management panel | P1 | ⬜ |
| Performance optimization | 60 FPS target | P0 | ⬜ |
| Bug fixing | Stabilization | P0 | ⬜ |

---

### Phase 4: Beta & Launch (Weeks 13-18)

#### Sprint 7-8 (Week 13-16): Beta Testing

| Task | Description | Priority | Status |
|------|-------------|----------|--------|
| Internal alpha | Team testing | P0 | ⬜ |
| Closed beta | 50-100 testers | P0 | ⬜ |
| Analytics integration | Track all events | P0 | ⬜ |
| Crash reporting | Sentry/Crashlytics | P0 | ⬜ |
| Feedback collection | In-app surveys | P1 | ⬜ |

#### Sprint 9-10 (Week 17-18): Launch Prep

| Task | Description | Priority | Status |
|------|-------------|----------|--------|
| Store listing | Screenshots, description | P0 | ⬜ |
| Privacy policy | GDPR/CCPA compliant | P0 | ⬜ |
| Marketing assets | Trailer, press kit | P1 | ⬜ |
| Soft launch | Limited geography | P0 | ⬜ |
| Global launch | Full release | P0 | ⬜ |

---

### 🗓️ Master Timeline

```
2026 Timeline:
═══════════════════════════════════════════════════════════════════════════════

Jan 14    Feb        Mar        Apr        May        Jun        Jul
  │         │          │          │          │          │          │
  ▼         ▼          ▼          ▼          ▼          ▼          ▼
┌──────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌──────────────┐
│ PHASE 1: MVP     │ │ PHASE 2: CORE   │ │ PHASE 3: ENGAGE │ │ PHASE 4: BETA│
│ Foundation       │ │ Gameplay        │ │ Features        │ │ & Launch     │
├──────────────────┤ ├─────────────────┤ ├─────────────────┤ ├──────────────┤
│ • Unity Setup    │ │ • Territory     │ │ • Daily Rewards │ │ • Alpha Test │
│ • Scene Creation │ │ • Map View      │ │ • Achievements  │ │ • Beta Test  │
│ • AR Integration │ │ • Combat        │ │ • Alliances     │ │ • Soft Launch│
│ • Firebase Sync  │ │ • Resources     │ │ • Leaderboards  │ │ • GA Launch  │
└──────────────────┘ └─────────────────┘ └─────────────────┘ └──────────────┘
     Week 1-4             Week 5-8            Week 9-12         Week 13-18

KEY MILESTONES:
★ Jan 28: First Block Placed in AR
★ Feb 14: Territory Claiming Working
★ Mar 1:  Combat System Complete
★ Apr 1:  Feature Complete (Beta Ready)
★ May 1:  Soft Launch (Limited Geo)
★ Jun 1:  Global Launch
```

---

## 🤖 Unity AI Prompts (Pending Unity Access)

> **Note:** These prompts are ready to execute once Unity access is available. Copy and paste directly into Unity AI Assistant (Copilot).

### 🎬 PROMPT 1: Initial Scene Setup
```
Create a new scene called "MainGame" with:
1. An empty GameObject called "GameManager"
2. Add these script components to GameManager:
   - TerritoryManager (from ApexCitadels.Territory namespace)
   - BuildingManager (from ApexCitadels.Building namespace)
   - PlayerManager (from ApexCitadels.Player namespace)
   - CombatManager (from ApexCitadels.Combat namespace)
   - AllianceManager (from ApexCitadels.Alliance namespace)
   - ResourceManager (from ApexCitadels.Resources namespace)
   - NotificationManager (from ApexCitadels.Notifications namespace)
   - LeaderboardManager (from ApexCitadels.Leaderboard namespace)
   - AchievementManager (from ApexCitadels.Achievements namespace)
   - DailyRewardManager (from ApexCitadels.DailyRewards namespace)
   - GameUIController (from ApexCitadels.UI namespace)
3. Add AR Session and XR Origin for AR functionality
4. Add a Directional Light for scene lighting
5. Set the scene as the startup scene in Build Settings
```

### 🎨 PROMPT 2: Main Game Canvas (HUD)
```
Create a Canvas called "GameCanvas" with:
1. Canvas Scaler set to "Scale with Screen Size" at 1080x1920 reference
2. A Panel called "TopBar" anchored to top, 150px height containing:
   - Text "Lv.1" on the left for level display (use TMP)
   - A Slider for XP progress bar (green fill)
   - 5 horizontal resource displays (icon placeholder + TMP number) for Stone, Wood, Metal, Crystal, Gems
   - A button on the right with notification bell icon
3. A Panel called "StatusPanel" in the center with Text for status messages (semi-transparent)
4. A Panel called "BottomBar" anchored to bottom, 200px height with:
   - 4 equally spaced buttons: "Build", "Map", "Attack", "Profile"
   - Each button should be 150x150 with icon placeholder and label below
5. Set the Canvas to "Screen Space - Overlay"
```

### 🏗️ PROMPT 3: Building Selection Panel
```
Create a Panel called "BuildingPanel" that:
1. Is anchored to bottom, initially positioned off-screen (for slide-up animation)
2. Has a dark semi-transparent background
3. Contains a ScrollView with horizontal layout group
4. Has 12 building block buttons arranged in 2 rows of 6:
   Row 1 (Basic): Stone, Wood, Metal, Glass, Wall, Gate
   Row 2 (Advanced): Tower, Turret, Flag, Banner, Torch, Beacon
5. Each button (120x140) shows:
   - Icon placeholder (80x80)
   - Name text (TMP, 16pt)
   - Cost text (TMP, 12pt, yellow)
6. Has a "Cancel" button at top-right to close
7. Add slide animation using DOTween or Animation component
8. Starts disabled (hidden)
```

### 🗺️ PROMPT 4: Map View Canvas
```
Create a Canvas called "MapCanvas" (separate from GameCanvas) with:
1. A full-screen Panel called "MapContainer" with dark blue background (#1a1a2e)
2. A RawImage or custom renderer area for the map (centered, 90% of screen)
3. A small circle image as "PlayerMarker" (yellow, 20x20, center of map area)
4. Buttons in top-right corner: "+" for zoom in, "-" for zoom out (40x40 each)
5. A button in bottom-right: "Center" to recenter on player
6. A button in top-left: "Close" (X icon) to return to AR view
7. A text label showing current coordinates (bottom-left)
8. Add MapViewController script to MapCanvas
9. Set Canvas to "Screen Space - Overlay" with sort order 10
10. Start the Canvas as disabled (toggled by Map button in main UI)
```

### 🧱 PROMPT 5: Block Prefabs (Create All 12)
```
Create prefabs for each building block type in Assets/Prefabs/Blocks/:

1. StoneBlock: 1x1x1 Cube, gray color (#808080), URP Lit material, BoxCollider
2. WoodBlock: 1x1x1 Cube, brown color (#8B4513), URP Lit material, BoxCollider
3. MetalBlock: 1x1x1 Cube, silver color (#C0C0C0), metallic=0.8, BoxCollider
4. GlassBlock: 1x1x1 Cube, transparent blue (#4488FF), alpha=0.5, BoxCollider
5. WallBlock: 2x1x0.3 Cube (long flat), brick color (#A0522D), BoxCollider
6. GateBlock: Create using 3 cubes (2 pillars + 1 top), iron gray (#555566), BoxCollider on parent
7. TowerBlock: Cylinder, 0.5 radius, 2 height, stone color (#707070), CapsuleCollider
8. TurretBlock: Cylinder base (0.3r, 0.3h) + Cube top (0.4), gunmetal (#383838), BoxCollider
9. FlagBlock: Thin cylinder pole (0.05r, 2h) + Quad for cloth (0.5x0.3), white
10. BannerBlock: Quad (0.3x0.6) with cloth-like rotation, medieval red (#8B0000)
11. TorchBlock: Small cylinder (0.03r, 0.3h) with Point Light child (orange, range 3, intensity 2)
12. BeaconBlock: Octahedron or stretched cube (crystal shape), emission material (blue #00AAFF, intensity 2)

For each prefab:
- Add Rigidbody (isKinematic=true)
- Add BoxCollider or appropriate collider
- Place in Assets/Prefabs/Blocks/ folder
- Tag as "BuildingBlock"
```

### 🌐 PROMPT 6: Territory Boundary Prefab
```
Create a prefab called "TerritoryBoundary" in Assets/Prefabs/Territory/:
1. Empty parent GameObject named "TerritoryBoundary"
2. Add LineRenderer component with:
   - Width: 0.2 units (start and end)
   - Use Positions: 33 points (to form circle + close point)
   - Material: Create new Unlit material named "TerritoryLineMaterial"
   - Color gradient: Solid green (#00FF00) by default
   - Loop: true (connects last to first point)
3. Add a script or comment explaining:
   - Green (#00FF00, alpha 0.8) for owned territories
   - Red (#FF0000, alpha 0.8) for enemy territories
   - Gray (#888888, alpha 0.5) for neutral/unclaimed
4. Create 3 material variants: TerritoryOwned, TerritoryEnemy, TerritoryNeutral
5. Scale should be adjustable via script (default 20m radius = 40m diameter)
```

### 🔔 PROMPT 7: Toast Notification Prefab
```
Create a prefab called "ToastNotification" in Assets/Prefabs/UI/:
1. Root: Panel (800x120) with rounded corners if available, otherwise standard
2. Background: Semi-transparent black (#000000, alpha 0.85)
3. Layout: Horizontal layout group with padding 15
4. Children:
   - Image (80x80) for notification icon on left
   - Vertical layout group containing:
     - TMP Text for title (bold, white, 22pt, left-aligned)
     - TMP Text for message (regular, #CCCCCC, 16pt, left-aligned)
5. Add CanvasGroup component for fade animations (alpha starts at 0)
6. Add Animator with simple FadeIn and FadeOut clips
7. Anchor: Top center of screen (with 100px offset from top)
8. Include animation to slide down, stay 3s, slide up
```

### 📊 PROMPT 8: Resource Display Prefab
```
Create a prefab called "ResourceDisplay" in Assets/Prefabs/UI/:
1. Root: Empty RectTransform (100x40)
2. Horizontal layout group (spacing 5, child alignment middle-left)
3. Children:
   - Image (32x32) for resource icon
   - TMP Text for amount (white, 18pt, right-aligned, min-width 50)
4. Create 5 variants for the icon colors:
   - Stone: Gray (#808080)
   - Wood: Brown (#8B4513)
   - Metal: Silver (#C0C0C0)
   - Crystal: Cyan (#00FFFF)
   - Gems: Magenta (#FF00FF)
5. Save base prefab and create color variants
```

### 🎁 PROMPT 9: Daily Reward Popup
```
Create a Panel called "DailyRewardPopup" in Assets/Prefabs/UI/:
1. Fullscreen overlay: Panel with semi-transparent black (#000000, alpha 0.7)
2. Centered dialog: Panel (850x650) with white/light background, rounded if available
3. Header section:
   - TMP Text "Daily Reward!" (centered, 36pt, bold, gold color #FFD700)
   - Streak counter TMP "Day 3 Streak! 🔥" (centered, 20pt)
4. Reward grid (7 items in horizontal row):
   - Each item (90x120):
     - Day number label (TMP, "Day 1", 14pt)
     - Reward icon Image (60x60)
     - Amount TMP (18pt, bold)
     - Checkmark overlay Image (green, initially hidden)
     - Highlight/glow Image for current day (yellow border, animated pulse)
5. Claim button (200x60) at bottom:
   - Green background when claimable
   - Gray and non-interactable when already claimed
   - TMP "Claim!" text (24pt, white, bold)
6. X close button in top-right corner (40x40)
7. Add CanvasGroup for fade animation
8. Start disabled
```

### 🏆 PROMPT 10: Achievement Panel
```
Create a Panel called "AchievementPanel" in Assets/Prefabs/UI/:
1. Full-height panel (anchored right, 450px wide) for slide-in from right
2. Header:
   - "Achievements" TMP title (28pt, bold)
   - "15/30 Unlocked" counter TMP (18pt, right side)
   - X close button (40x40)
3. Tab bar (horizontal):
   - Buttons: All, Territory, Building, Combat, Social, Resources, Exploration
   - Selected tab highlighted (underline or background)
4. ScrollView (vertical):
   - Content container with Vertical Layout Group
5. Achievement item prefab (full width x 100px):
   - Icon Image (left, 70x70, gray=locked, colored=unlocked)
   - Name TMP (20pt, bold)
   - Description TMP (14pt, #888888)
   - Progress bar Slider (if in-progress)
   - Reward icons (XP icon + amount, Gem icon + amount)
6. Animation: Slide in from right edge
7. Start disabled
```

### 👥 PROMPT 11: Alliance Panel
```
Create a Panel called "AlliancePanel" in Assets/Prefabs/UI/:
1. Full-screen panel with header "Alliance" and X close button
2. State 1 - No Alliance (default):
   - "Create Alliance" button (prominent, center)
   - Input fields: Alliance Name (max 20 chars), Alliance Tag (3-4 chars)
   - Create cost display: "Cost: 1000 💎"
   - Divider with "OR"
   - Search bar to find existing alliances
   - ScrollView list of open alliances (name, tag, member count, join button)
3. State 2 - In Alliance:
   - Alliance banner/emblem area (top, 150px)
   - Alliance name and tag TMP (28pt)
   - Stats row: Members (X/50), Territories, Wars Won
   - Tab bar: Members, Wars, Settings
   - Members tab: ScrollView list (avatar, name, role badge, last active)
   - "Invite Player" button (for officers/leader)
   - "Leave Alliance" button (red, bottom, with confirmation)
4. Both states share same panel, toggle visibility via script
5. Start disabled
```

### 📱 PROMPT 12: AR Session Configuration
```
Configure AR for Android in the MainGame scene:
1. Create GameObject "AR Session" with:
   - AR Session component (Match Frame Rate = true)
   - AR Input Manager component
2. Create GameObject "XR Origin (AR)" with:
   - XR Origin component
   - AR Plane Manager (Detection Mode = Horizontal, Plane Prefab = see below)
   - AR Raycast Manager
   - AR Anchor Manager (for persistent objects)
3. Under XR Origin, create "Camera Offset" with:
   - "Main Camera" child with:
     - Camera component (clear flags = solid color, background = black)
     - AR Camera Manager
     - AR Camera Background
     - Audio Listener
     - Tag = "MainCamera"
4. Create a simple AR Plane Prefab:
   - Quad with AR Default Plane material (transparent white)
   - AR Plane component
   - Save to Assets/Prefabs/AR/ARPlanePrefab
5. Player Settings recommendations (for build):
   - Minimum API Level: 26 (Android 8.0)
   - Target API Level: 34
   - Graphics API: OpenGLES3
   - ARCore Required: true
```

### 🔗 PROMPT 13: Connect Everything (Final Integration)
```
In the GameManager object, find GameUIController and assign these references in the Inspector:

TopBar References:
- levelText → TopBar/LevelText (TMP)
- xpSlider → TopBar/XPSlider (Slider)
- Resource texts → TopBar/Resources/[Stone,Wood,Metal,Crystal,Gems]Text (TMP each)
- notificationButton → TopBar/NotificationButton (Button)

BottomBar References:
- buildButton → BottomBar/BuildButton (Button)
- mapButton → BottomBar/MapButton (Button)
- attackButton → BottomBar/AttackButton (Button)
- profileButton → BottomBar/ProfileButton (Button)

Panel References:
- statusText → StatusPanel/StatusText (TMP)
- buildingPanel → BuildingPanel (GameObject)
- mapCanvas → MapCanvas (GameObject)
- dailyRewardPopup → DailyRewardPopup (GameObject)
- achievementPanel → AchievementPanel (GameObject)
- alliancePanel → AlliancePanel (GameObject)
- toastContainer → GameCanvas/ToastContainer (Transform for spawning toasts)

BuildingManager References:
- blockPrefabs → Array of all 12 block prefabs from Assets/Prefabs/Blocks/
- placementPreviewPrefab → PlacementPreview prefab (transparent version)

TerritoryManager References:
- territoryBoundaryPrefab → TerritoryBoundary prefab
- ownedTerritoryMaterial → TerritoryOwned material
- enemyTerritoryMaterial → TerritoryEnemy material

Verify all references are connected and press Play to test initialization.
```
   - A Slider for XP progress bar
   - 5 horizontal resource displays (icon + number) for Stone, Wood, Metal, Crystal, Gems
   - A button on the right with notification bell icon
3. A Panel called "StatusPanel" in the center with Text for status messages
4. A Panel called "BottomBar" anchored to bottom, 200px height with:
   - 4 equally spaced buttons: "Build", "Map", "Attack", "Profile"
   - Each button should be 150x150 with icon and label below
```

### Prompt 3: Building Selection Panel
```
Create a Panel called "BuildingPanel" that:
1. Slides up from the bottom when Build is tapped
2. Contains a ScrollView with horizontal layout
3. Has 12 building block buttons in a grid:
   - Stone, Wood, Metal, Glass (basic)
   - Wall, Gate, Tower, Turret (defensive)
   - Flag, Banner, Torch, Beacon (decorative)
4. Each button shows: icon, name, resource cost
5. Has a "Cancel" button to close the panel
6. Assign to GameUIController's buildingPanel reference
```

### Prompt 4: Map View Canvas
```
Create a Canvas called "MapCanvas" (separate from GameCanvas) with:
1. A full-screen Panel called "MapContainer" with dark background
2. A small circle image as "PlayerMarker" (yellow, center of screen)
3. Buttons in top-right corner: "+" for zoom in, "-" for zoom out
4. A button in bottom-right: "Center on Me" 
5. A button in top-left: "Close" to return to AR view
6. Add MapViewController script to MapCanvas
7. Assign MapContainer and PlayerMarker to the script
8. Set Canvas to initially disabled (toggled by Map button)
```

### Prompt 5: Block Prefabs
```
Create prefabs for each building block type in Assets/Prefabs/Blocks/:

1. StoneBlock: 1x1x1 Cube, gray color (#808080), URP Lit material
2. WoodBlock: 1x1x1 Cube, brown color (#8B4513), URP Lit material
3. MetalBlock: 1x1x1 Cube, silver color (#C0C0C0), metallic 0.8
4. GlassBlock: 1x1x1 Cube, transparent blue, alpha 0.5
5. WallBlock: 2x1x0.3 Cube (long flat), brick color (#A0522D)
6. GateBlock: Archway shape (can use multiple cubes), iron gray
7. TowerBlock: Cylinder, 0.5 radius, 2 height, stone texture
8. TurretBlock: Cylinder base + cube top, mechanical gray
9. FlagBlock: Thin cylinder pole (0.05 radius) + plane for cloth
10. BannerBlock: Plane with cloth texture, hanging orientation
11. TorchBlock: Small cylinder with Point Light (orange, range 3)
12. BeaconBlock: Crystal shape with emission material (blue glow)

Add a BoxCollider to each prefab.
```

### Prompt 6: Territory Boundary Prefab
```
Create a prefab called "TerritoryBoundary" that:
1. Uses a LineRenderer component
2. Draws a circle with 32 segments
3. Has width of 0.3 units
4. Uses an unlit material with:
   - Green color (#00FF00) for owned
   - Red color (#FF0000) for enemy  
   - Gray color (#888888) for neutral
5. Loops back to start point
6. Add a script to generate circle points based on radius parameter
```

### Prompt 7: Notification Toast Prefab
```
Create a prefab called "ToastNotification" for popup messages:
1. A Panel with rounded corners (if available) or standard
2. Background color: semi-transparent black
3. Contains:
   - Image on left for notification icon
   - Text for title (bold, white, 24pt)
   - Text for message (regular, white, 18pt)
4. Total size: 800x120
5. Add CanvasGroup component for fade animations
6. Save to Assets/Prefabs/UI/
```

### Prompt 8: Resource Icon Prefab
```
Create a prefab called "ResourceDisplay" for the top bar:
1. Horizontal layout group
2. Image (40x40) for resource icon
3. Text for amount (white, 20pt, right-aligned)
4. Total width: 100px
5. Create 5 instances in scene, one for each resource type
6. Assign different colors to each icon placeholder:
   - Stone: Gray
   - Wood: Brown
   - Metal: Silver
   - Crystal: Cyan
   - Gems: Magenta
```

### Prompt 9: Daily Reward Popup
```
Create a Panel called "DailyRewardPopup" as a modal dialog:
1. Semi-transparent black overlay covering full screen
2. Centered white panel (800x600)
3. Title text "Daily Reward!" at top
4. 7 reward slots in a horizontal row:
   - Each slot: 80x100
   - Day number label
   - Icon for reward type
   - Amount text
   - Checkmark overlay for claimed days
   - Highlight/glow for current day
5. "Claim" button at bottom (only active if unclaimed)
6. Streak counter text showing current streak
7. Close button in corner
```

### Prompt 10: Achievement Panel
```
Create a Panel called "AchievementPanel" as a slide-in panel:
1. Slides in from right side of screen
2. Header with "Achievements" title and X close button
3. Tab buttons for categories: All, Territory, Building, Combat, Social, Resources, Exploration, Milestones
4. ScrollView containing achievement items:
   - Each item: 100px height
   - Icon on left (locked=gray, unlocked=colored)
   - Name and description text
   - Progress bar showing current/target
   - Rewards listed (XP, Gems)
5. Counter at top: "15/30 Unlocked"
```

### Prompt 11: Alliance Panel
```
Create a Panel called "AlliancePanel":
1. Full-screen panel that opens from Profile
2. If not in alliance:
   - "Create Alliance" button (name input, tag input)
   - Search bar to find alliances
   - List of open alliances to join
3. If in alliance:
   - Alliance banner/name/tag at top
   - Member list with roles
   - Stats (territories, XP, wars won)
   - "Invite Player" button (for officers)
   - "Leave Alliance" button
   - "War" tab showing active/past wars
```

### Prompt 12: AR Configuration
```
Configure the AR setup for Android:
1. Ensure AR Session is in scene with:
   - Match Frame Rate enabled
   - Attempt Update checked
2. XR Origin with:
   - Camera Floor Offset Object set to AR Camera
   - AR Camera has AR Camera Manager and AR Camera Background
3. Add AR Plane Manager to XR Origin:
   - Detection Mode: Horizontal
   - Plane Prefab: Create simple quad with transparent material
4. Add AR Raycast Manager to XR Origin
5. Player Settings:
   - Minimum API Level: 26
   - Target API Level: 34
   - ARCore required
```

### Prompt 13: Connect UI to GameUIController
```
In the GameUIController component on GameManager, assign these references:

From TopBar:
- levelText: The "Lv.1" Text component
- xpSlider: The XP progress Slider
- stoneText, woodText, metalText, crystalText, gemsText: Resource amount texts
- notificationButton: The bell button

From BottomBar:
- buildButton, mapButton, attackButton, profileButton: Action buttons

From BuildingPanel:
- buildingPanel: The slide-up panel
- Block buttons array: All 12 block selection buttons

From other panels:
- statusText: Center status text
- toastContainer: Transform for spawning toasts
- mapCanvas: The MapCanvas GameObject
- dailyRewardPopup: The DailyRewardPopup panel
- achievementPanel: The AchievementPanel
- alliancePanel: The AlliancePanel
```

---

## 🧪 Testing & QA Strategy

### Test Pyramid

```
                    ┌─────────────────────┐
                    │   E2E / Device      │  ← 10% (Critical paths only)
                    │   Tests             │
                    ├─────────────────────┤
                    │   Integration       │  ← 30% (System interactions)
                    │   Tests             │
                    ├─────────────────────┤
                    │   Unit Tests        │  ← 60% (Business logic)
                    │                     │
                    └─────────────────────┘
```

### Unit Tests (C# / Unity Test Framework)

| ID | Test Suite | Description | Priority | Status |
|----|------------|-------------|----------|--------|
| UT1 | TerritoryTests | Overlap detection, radius calc, claim validation | P0 | ⬜ |
| UT2 | ResourceTests | Cost calculation, inventory management | P0 | ⬜ |
| UT3 | PlayerTests | XP/Level progression, stat updates | P1 | ⬜ |
| UT4 | CombatTests | Damage calculation, cooldowns, conquest | P0 | ⬜ |
| UT5 | GeoTests | Distance calculations, geohash encoding | P0 | ⬜ |
| UT6 | AllianceTests | Join/leave logic, role permissions | P1 | ⬜ |
| UT7 | AchievementTests | Progress tracking, unlock conditions | P2 | ⬜ |

### Integration Tests (Unity Play Mode)

| ID | Test Scenario | Description | Priority | Status |
|----|---------------|-------------|----------|--------|
| IT1 | Place → Sync | Block placed → Firebase write → reload → visible | P0 | ⬜ |
| IT2 | Claim → Map | Territory claimed → map marker appears | P0 | ⬜ |
| IT3 | Attack → Notify | Attack territory → defender gets notification | P0 | ⬜ |
| IT4 | Login → Reward | First login of day → daily reward popup | P1 | ⬜ |
| IT5 | Progress → Achievement | Action triggers → achievement progress → unlock | P1 | ⬜ |
| IT6 | Alliance → Territory | Join alliance → see alliance territories on map | P1 | ⬜ |

### Device Tests (Android)

| ID | Test | Description | Pass Criteria | Status |
|----|------|-------------|---------------|--------|
| DT1 | AR Activation | AR camera starts | Plane detection works | ⬜ |
| DT2 | GPS Accuracy | Location tracking | <10m accuracy | ⬜ |
| DT3 | Block Placement | Tap to place block | Block renders at tap point | ⬜ |
| DT4 | Cross-Device Sync | 2 phones same location | Both see same blocks | ⬜ |
| DT5 | Push Delivery | Send test push | Notification received in <5s | ⬜ |
| DT6 | Performance | 10 min gameplay | Stable 60 FPS, no crashes | ⬜ |
| DT7 | Battery Test | 1 hour active play | <20% battery drain | ⬜ |
| DT8 | Network Test | 3G connection | Playable with <2s sync delay | ⬜ |
| DT9 | Offline Test | Airplane mode | Can view owned territories | ⬜ |

### User Acceptance Tests

| ID | Scenario | Expected Outcome | Status |
|----|----------|------------------|--------|
| UAT1 | New User FTUE | Can place first block within 2 minutes | ⬜ |
| UAT2 | Territory Claim | Understands ownership concept | ⬜ |
| UAT3 | Attack Flow | Can find and attack enemy territory | ⬜ |
| UAT4 | Resource System | Understands costs and harvesting | ⬜ |
| UAT5 | Alliance Join | Can join alliance in <1 minute | ⬜ |
| UAT6 | Daily Return | Knows to come back tomorrow | ⬜ |

---

## 🚀 DevOps & CI/CD Pipeline

### GitHub Actions Workflow

```yaml
# .github/workflows/main.yml

name: Apex Citadels CI/CD

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  # Stage 1: Code Quality
  lint-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
      - name: Run C# Lint (dotnet format)
        run: dotnet format --verify-no-changes
      - name: Run Unit Tests
        run: dotnet test

  # Stage 2: Firebase Functions
  deploy-functions:
    needs: lint-and-test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
      - uses: actions/checkout@v4
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
      - name: Install dependencies
        run: cd backend/functions && npm ci
      - name: Build TypeScript
        run: cd backend/functions && npm run build
      - name: Deploy to Firebase
        run: firebase deploy --only functions
        env:
          FIREBASE_TOKEN: ${{ secrets.FIREBASE_TOKEN }}

  # Stage 3: Unity Build (via Unity Cloud Build)
  trigger-unity-build:
    needs: lint-and-test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
      - name: Trigger Unity Cloud Build
        run: |
          curl -X POST \
            -H "Authorization: Basic ${{ secrets.UNITY_CLOUD_API_KEY }}" \
            "https://build-api.cloud.unity3d.com/api/v1/orgs/$ORG/projects/$PROJECT/buildtargets/$TARGET/builds"
```

### Environment Strategy

| Environment | Purpose | Firebase Project | Auto-Deploy |
|-------------|---------|------------------|-------------|
| Development | Local testing | apex-dev | No |
| Staging | QA testing | apex-staging | On PR merge |
| Production | Live users | apex-prod | Manual approval |

### Monitoring & Alerting

| Tool | Purpose | Alerts |
|------|---------|--------|
| Datadog | APM, logs, metrics | Error rate >1%, latency >500ms |
| Sentry | Crash reporting | Any crash, ANR >5s |
| Firebase Performance | Client metrics | Cold start >3s, frame drops |
| PagerDuty | On-call | Critical alerts → SMS |

---

## 📈 Performance Benchmarks

### Client Performance Targets

| Metric | Minimum | Target | Stretch | Measurement |
|--------|---------|--------|---------|-------------|
| FPS (ARCore active) | 30 | 60 | 120 | Unity Profiler |
| Initial Load Time | 8s | 5s | 3s | Stopwatch |
| Scene Transition | 2s | 1s | 0.5s | Analytics |
| Memory (Peak) | 500MB | 300MB | 200MB | Profiler |
| APK Size | 150MB | 100MB | 75MB | Build output |
| Battery/Hour | 20% | 15% | 10% | Device measurement |

### Backend Performance Targets

| Metric | Target | Alert Threshold | Measurement |
|--------|--------|-----------------|-------------|
| API Latency (p50) | 100ms | 200ms | Cloud Monitoring |
| API Latency (p99) | 500ms | 1000ms | Cloud Monitoring |
| Firestore Reads/User/Day | 1000 | 5000 | Firestore metrics |
| Cloud Function Cold Start | 500ms | 2000ms | Function logs |
| Concurrent Users | 10,000 | N/A | Load testing |

### Reference Devices

| Device | Tier | Use Case |
|--------|------|----------|
| Pixel 5 | Mid | Primary test device |
| Samsung A52 | Budget | Min-spec testing |
| Pixel 8 Pro | High | Performance ceiling |
| Samsung Galaxy Tab S9 | Tablet | Tablet experience |

---

## 💰 Monetization Strategy

### Revenue Model: Free-to-Play with Cosmetics + Battle Pass

```
Revenue Streams:
═══════════════════════════════════════════════════════════════

1. COSMETIC SHOP (60% of revenue)
   ├── Block Skins (Stone→Obsidian, Wood→Enchanted, etc.)
   ├── Territory Effects (Custom boundary colors/animations)
   ├── Profile Customization (Avatars, banners, titles)
   └── Emotes & Celebrations

2. BATTLE PASS (30% of revenue)
   ├── Free Track (Basic rewards, keep engagement)
   ├── Premium Track ($9.99/season, exclusive cosmetics)
   └── Season Duration: 90 days

3. CONVENIENCE (10% of revenue)
   ├── Resource Boosters (2x harvest for 1 hour)
   ├── Shield Time (Protection from attacks)
   └── Queue Skips (Instant building completion)

Pricing Strategy:
═══════════════════════════════════════════════════════════════

Gem Packs (Premium Currency):
├── 100 Gems   = $0.99  (Impulse buy)
├── 500 Gems   = $4.99  (Best $/gem ratio)
├── 1200 Gems  = $9.99  (Popular)
├── 2500 Gems  = $19.99 (Enthusiast)
└── 6500 Gems  = $49.99 (Whale)

Battle Pass:
├── Free Track = $0
├── Premium    = $9.99 (or 1000 Gems)
└── Premium+   = $19.99 (Instant 25 tier unlock)
```

### Ethical Monetization Principles

1. **No Pay-to-Win** - Purchases are cosmetic only
2. **Earnable Premium Currency** - Gems from achievements/daily play
3. **Transparent Odds** - If any randomness, display probabilities
4. **Spending Limits** - Optional monthly caps
5. **Age-Appropriate** - No loot boxes, clear purchase flows

---

## 🚀 Launch Strategy

### Soft Launch Plan

| Phase | Geography | Users | Duration | Goal |
|-------|-----------|-------|----------|------|
| Alpha | Internal only | 10-20 | 2 weeks | Core loop validation |
| Closed Beta | Invite-only | 100-500 | 4 weeks | Feature completeness |
| Soft Launch 1 | Canada | 5,000 | 4 weeks | Retention metrics |
| Soft Launch 2 | + Australia | 20,000 | 4 weeks | Monetization testing |
| Global Launch | Worldwide | ∞ | Ongoing | Scale |

### Launch Checklist

**Technical Readiness:**
- [ ] Load tested to 10,000 CCU
- [ ] Crash-free rate >99.5%
- [ ] ANR rate <0.5%
- [ ] All P0 bugs resolved

**Store Readiness:**
- [ ] App Store listing complete
- [ ] Play Store listing complete
- [ ] Screenshots (5+ per platform)
- [ ] Video trailer (30-60 seconds)
- [ ] Privacy policy URL
- [ ] Support email configured

**Marketing Readiness:**
- [ ] Press kit prepared
- [ ] Influencer outreach list
- [ ] Social media accounts active
- [ ] Community Discord server
- [ ] Launch announcement drafted

**Operational Readiness:**
- [ ] On-call rotation scheduled
- [ ] Runbooks documented
- [ ] Rollback procedure tested
- [ ] Customer support trained

---

## 🎨 AI Asset Generation Prompts

### 3D Block Textures (Use with Leonardo.ai, Midjourney, or Stable Diffusion)

```
TEXTURE GENERATION PROMPTS
══════════════════════════════════════════════════════════════

Style Guide: Low-poly stylized game texture, seamless tileable, 512x512, 
PBR-ready (albedo, normal, roughness maps)

STONE BLOCK:
"Stylized medieval stone brick texture, gray with subtle blue undertones, 
weathered mossy edges, hand-painted game art style, seamless tileable, 
ambient occlusion baked, Clash Royale art direction"

WOOD BLOCK:
"Stylized wooden plank texture, warm honey oak color, visible wood grain 
with knots, cartoon game asset, hand-painted style, seamless tileable"

METAL BLOCK:
"Stylized brushed steel plate texture, silver with orange rust accents, 
riveted industrial panels, sci-fi game asset, seamless tileable"

GLASS/ENERGY BLOCK:
"Magical energy field texture, glowing cyan hexagonal grid, holographic 
shimmer effect, fantasy game UI style, transparent with glow"

CRYSTAL BLOCK:
"Stylized crystal cluster texture, deep purple amethyst, faceted surface 
with inner glow, fantasy RPG style, magical gem material"
```

### UI Icon Prompts (Use with DALL-E 3, Midjourney)

```
ICON GENERATION PROMPTS
══════════════════════════════════════════════════════════════

Style Guide: 2D mobile game icon, flat design with subtle gradients, 
128x128 or 256x256, transparent background, bold outlines

RESOURCE ICONS:
"Mobile game icon, pile of gray stone rocks, stylized minimal, 
bold black outline, gradient shading, transparent background, 
Clash of Clans style" [Repeat for: Wood logs, Metal ingots, 
Cyan crystal, Purple gem]

ACTION BUTTONS:
"Mobile game button icon, hammer and building blocks, construction 
symbol, blue and orange colors, rounded square shape, glossy, 
Fortnite UI style"

"Mobile game button icon, treasure map with red pin marker, 
navigation symbol, brown parchment colors, rounded square, 
adventure game style"

"Mobile game button icon, crossed swords with shield, combat 
attack symbol, red and silver colors, fierce, mobile game UI"

ACHIEVEMENT BADGES:
"Game achievement badge, golden circular frame with laurel wreath, 
castle tower icon in center, 'Landowner' title below, metallic 
shading, mobile game reward style"
```

### Map Marker Prompts

```
MAP MARKER GENERATION PROMPTS
══════════════════════════════════════════════════════════════

Style: Top-down game map icons, simple shapes, 64x64, transparent BG

"Top-down map marker, owned territory, green glowing circle with 
castle flag icon, strategy game minimap style, clean edges"

"Top-down map marker, enemy territory, red ominous circle with 
skull warning icon, danger zone indicator, game UI"

"Top-down map marker, resource node, golden diamond shape with 
sparkle, treasure location indicator, RPG game style"

"Top-down map marker, player position, blue arrow pointing up, 
location indicator with pulse ring, navigation UI"
```

---

## 📊 Progress Summary Dashboard

### Development Status Overview

```
APEX CITADELS - PROJECT STATUS (January 14, 2026)
═══════════════════════════════════════════════════════════════════════════════

BACKEND (Firebase)          ████████████████████░░░░  80% Complete
├─ Firestore Setup         ✅ Complete
├─ Security Rules          ✅ Complete  
├─ Auth System             ✅ Complete
├─ Cloud Functions         🔄 Partial (need combat validation)
├─ Push Notifications      ⬜ Not Started
└─ Analytics               ⬜ Not Started

C# SCRIPTS                  ████████████████████████  100% Complete (20/20)
├─ Territory System        ✅ TerritoryManager.cs, Territory.cs
├─ Building System         ✅ BuildingManager.cs, BuildingBlock.cs
├─ Player System           ✅ PlayerManager.cs, PlayerProfile.cs
├─ Combat System           ✅ CombatManager.cs
├─ Alliance System         ✅ AllianceManager.cs, Alliance.cs
├─ Resource System         ✅ ResourceManager.cs
├─ Map System              ✅ MapViewController.cs
├─ Notifications           ✅ NotificationManager.cs
├─ Leaderboards            ✅ LeaderboardManager.cs
├─ Achievements            ✅ AchievementManager.cs
├─ Daily Rewards           ✅ DailyRewardManager.cs
├─ UI Controller           ✅ GameUIController.cs
├─ AR/Spatial              ✅ SpatialAnchorManager.cs
└─ Backend Services        ✅ AnchorPersistenceService.cs

UNITY SETUP                 ░░░░░░░░░░░░░░░░░░░░░░░░  0% (BLOCKED)
├─ Scene Architecture      ⏳ Awaiting Unity Access
├─ UI Canvas               ⏳ Awaiting Unity Access
├─ Block Prefabs           ⏳ Awaiting Unity Access
├─ AR Configuration        ⏳ Awaiting Unity Access
└─ Integration             ⏳ Awaiting Unity Access

3D/UI ASSETS               ░░░░░░░░░░░░░░░░░░░░░░░░  0% Not Started
├─ Block Textures (12)     ⬜ Ready for AI generation
├─ Resource Icons (5)      ⬜ Ready for AI generation
├─ UI Elements (20+)       ⬜ Ready for AI generation
└─ VFX/Particles (10)      ⬜ Needs Unity for creation

TESTING                    ░░░░░░░░░░░░░░░░░░░░░░░░  0% Blocked
├─ Unit Tests              ⬜ Scripts ready, awaiting Unity
├─ Integration Tests       ⬜ Blocked on Unity
├─ Device Tests            ⬜ Blocked on Unity
└─ UAT                     ⬜ Blocked on Unity

═══════════════════════════════════════════════════════════════════════════════
CURRENT BLOCKER: Unity License/Access
NEXT ACTION: Execute Unity AI Prompts (Section 10) when access available
═══════════════════════════════════════════════════════════════════════════════
```

### Script Inventory (Ready for Unity)

| Category | Scripts | Status | Location |
|----------|---------|--------|----------|
| AR/Spatial | 1 | ✅ | `Assets/Scripts/AR/` |
| Backend | 1 | ✅ | `Assets/Scripts/Backend/` |
| Config | 1 | ✅ | `Assets/Scripts/Config/` |
| Demo | 2 | ✅ | `Assets/Scripts/Demo/` |
| Territory | 2 | ✅ | `Assets/Scripts/Territory/` |
| Building | 2 | ✅ | `Assets/Scripts/Building/` |
| Player | 2 | ✅ | `Assets/Scripts/Player/` |
| Combat | 1 | ✅ | `Assets/Scripts/Combat/` |
| Alliance | 2 | ✅ | `Assets/Scripts/Alliance/` |
| Resources | 1 | ✅ | `Assets/Scripts/Resources/` |
| Map | 1 | ✅ | `Assets/Scripts/Map/` |
| Notifications | 1 | ✅ | `Assets/Scripts/Notifications/` |
| Leaderboard | 1 | ✅ | `Assets/Scripts/Leaderboard/` |
| Achievements | 1 | ✅ | `Assets/Scripts/Achievements/` |
| DailyRewards | 1 | ✅ | `Assets/Scripts/DailyRewards/` |
| UI | 1 | ✅ | `Assets/Scripts/UI/` |
| **TOTAL** | **21** | ✅ | All namespaced under `ApexCitadels.*` |

---

## 🔄 Daily Standup Template

```markdown
## Date: [YYYY-MM-DD]

### ✅ Yesterday:
- [Completed task 1]
- [Completed task 2]

### 🎯 Today:
- [ ] [Planned task 1]
- [ ] [Planned task 2]

### 🚧 Blockers:
- [Blocker description] → [Proposed resolution]

### 📝 Notes:
- [Any additional context, decisions, or observations]

### 📊 Key Metrics:
- Scripts Complete: 20/20
- Unity Setup: 0/13 prompts
- Assets Created: 0/50+
```

---

## 📚 Reference Documentation

### External Resources

| Resource | URL | Purpose |
|----------|-----|---------|
| AR Foundation Docs | [Unity AR Foundation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/manual/index.html) | AR setup reference |
| ARCore Geospatial | [Google ARCore](https://developers.google.com/ar/develop/geospatial) | VPS integration |
| Firebase Unity SDK | [Firebase Docs](https://firebase.google.com/docs/unity/setup) | Backend integration |
| Photon Fusion | [Photon Docs](https://doc.photonengine.com/fusion/current/getting-started/fusion-intro) | Multiplayer (Phase 3) |
| URP Documentation | [Unity URP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/index.html) | Rendering pipeline |

### Internal Documentation

| Document | Location | Purpose |
|----------|----------|---------|
| Vision | [docs/VISION.md](docs/VISION.md) | Product strategy |
| Tech Architecture | [docs/TECHNICAL_ARCHITECTURE.md](docs/TECHNICAL_ARCHITECTURE.md) | System design |
| Roadmap | [docs/ROADMAP.md](docs/ROADMAP.md) | Milestone tracking |
| Unity Setup Guide | [README.md](README.md) | Setup instructions |
| Backend Functions | [backend/functions/src/index.ts](backend/functions/src/index.ts) | API reference |

---

## 🏆 Success Vision

### What "World Class" Means for Apex Citadels

1. **Technical Excellence**
   - Sub-5cm AR positioning accuracy
   - <100ms global sync latency
   - 99.9% uptime
   - Seamless XR glasses transition

2. **User Experience Excellence**
   - 60 second time-to-first-joy
   - Intuitive without tutorials
   - Delightful microinteractions
   - Accessible to casual players, deep for hardcore

3. **Business Excellence**
   - $1+ ARPDAU within 6 months
   - 25%+ D7 retention
   - <$2 CPI in target markets
   - Positive ROI within 12 months

4. **Cultural Impact**
   - "I own a fortress on 5th Avenue"
   - Local community meetups
   - Esports-ready alliance wars
   - Featured in app stores

---

*Document Version: 2.0*  
*Last Updated: January 14, 2026*  
*Maintained by: Apex Citadels Development Team*  
*Next Review: Upon Unity Access*
