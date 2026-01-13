# Apex Citadels: Technical Architecture

## System Overview

```
┌────────────────────────────────────────────────────────────────────┐
│                        CLIENT LAYER                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐             │
│  │   iOS App    │  │ Android App  │  │  XR Glasses  │             │
│  │   (Unity)    │  │   (Unity)    │  │   (Unity)    │             │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘             │
│         │                 │                 │                      │
│         └────────────┬────┴────────────────┘                      │
│                      │                                             │
│              ┌───────▼───────┐                                     │
│              │  AR Foundation │  (ARCore / ARKit / OpenXR)        │
│              │  + VPS Layer   │                                    │
│              └───────┬───────┘                                     │
└──────────────────────┼─────────────────────────────────────────────┘
                       │
                       │ HTTPS / WebSocket
                       │
┌──────────────────────▼─────────────────────────────────────────────┐
│                      BACKEND LAYER                                  │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │                    API Gateway (AWS/GCP)                     │  │
│  └─────────────────────────────────────────────────────────────┘  │
│           │              │              │              │           │
│  ┌────────▼───┐  ┌───────▼────┐ ┌──────▼─────┐ ┌─────▼──────┐   │
│  │   Auth     │  │  Game      │ │  Spatial   │ │  Real-Time │   │
│  │  Service   │  │  State     │ │  Anchors   │ │  Battles   │   │
│  │ (Firebase) │  │  Service   │ │  Service   │ │  (Photon)  │   │
│  └────────────┘  └────────────┘ └────────────┘ └────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │                    DATA LAYER                                │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │  │
│  │  │ Firestore│  │ Redis    │  │ Cloud    │  │ BigQuery │   │  │
│  │  │ (State)  │  │ (Cache)  │  │ Storage  │  │(Analytics│   │  │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │  │
│  └─────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Core Technical Challenges & Solutions

### Challenge 1: Spatial Persistence (The Cube Test)

**Problem:** How do we ensure a 3D object placed by Player A appears in the exact same real-world location for Player B?

**Solution: Layered Anchor System**

```
┌─────────────────────────────────────────────────┐
│              ANCHOR HIERARCHY                    │
│                                                  │
│  Level 1: GPS Anchor (±5 meters)                │
│     └─ Level 2: VPS Anchor (±5 centimeters)     │
│           └─ Level 3: Local Anchor (±1 cm)      │
│                 └─ Level 4: Object Transform    │
└─────────────────────────────────────────────────┘
```

1. **GPS Anchor** - Coarse positioning to find the right area
2. **VPS/Cloud Anchor** - Google ARCore Geospatial API or Niantic Lightship VPS
3. **Local Anchor** - Device-relative refinement using plane detection
4. **Object Transform** - Final position relative to anchor

### Challenge 2: Real-Time Synchronization

**Problem:** Multiple players need to see the same Citadel state simultaneously.

**Solution: Hybrid Sync Architecture**

```csharp
// Sync Strategy by Data Type

Static Structures (Walls, Towers):
  → Firestore with local caching
  → Sync on area entry, poll every 60s

Dynamic State (Health, Damage):
  → Redis pub/sub
  → Real-time WebSocket updates

Battle State (Attacks, Positions):
  → Photon PUN2 / Fusion
  → 20 tick/second during combat
```

### Challenge 3: Occlusion (Visual Realism)

**Problem:** Virtual objects must appear BEHIND real-world objects.

**Solution: Depth-Based Occlusion Pipeline**

```
┌────────────────────────────────────────────────┐
│           OCCLUSION PIPELINE                    │
│                                                 │
│  1. Capture camera frame                        │
│  2. Run LiDAR/ToF depth scan (if available)    │
│  3. Run ML depth estimation (fallback)         │
│  4. Generate depth buffer                       │
│  5. Render virtual objects with depth test     │
│  6. Apply edge smoothing shader                │
└────────────────────────────────────────────────┘
```

---

## Technology Stack

### Client (Unity 2022 LTS+)

| Component | Technology | Purpose |
|-----------|------------|---------|
| AR Framework | AR Foundation 5.x | Cross-platform AR |
| Geospatial | ARCore Geospatial API | VPS anchoring |
| Rendering | URP + AR Shaders | Mobile-optimized graphics |
| Networking | Photon Fusion | Real-time multiplayer |
| UI | Unity UI Toolkit | Modern UI system |
| Analytics | Unity Analytics + Firebase | User behavior tracking |

### Backend (Serverless-First)

| Component | Technology | Purpose |
|-----------|------------|---------|
| Auth | Firebase Auth | Social login, anonymous |
| Database | Firestore | Game state, user data |
| Cache | Redis (Upstash) | Hot data, leaderboards |
| Storage | Cloud Storage | 3D assets, user content |
| Functions | Cloud Functions | Game logic, validation |
| Real-time | Photon Server | Battle synchronization |
| Analytics | BigQuery | Data warehouse |
| Maps | Google Maps Platform | POI data, geocoding |

### DevOps

| Component | Technology | Purpose |
|-----------|------------|---------|
| CI/CD | GitHub Actions | Automated builds |
| Unity Cloud | Unity Build Automation | Multi-platform builds |
| Monitoring | Datadog / Sentry | Error tracking |
| CDN | CloudFlare | Asset delivery |

---

## Data Models

### User Document
```typescript
interface User {
  uid: string;
  displayName: string;
  faction: 'builders' | 'raiders' | 'merchants';
  guildId?: string;
  
  stats: {
    level: number;
    xp: number;
    citadelsBuilt: number;
    raidsCompleted: number;
    raidsDefended: number;
  };
  
  inventory: {
    resources: ResourceInventory;
    blueprints: string[];
    architects: string[];
  };
  
  premiumStatus: {
    battlePass: boolean;
    landLicenses: string[];
  };
  
  createdAt: Timestamp;
  lastActive: Timestamp;
}
```

### Citadel Document
```typescript
interface Citadel {
  id: string;
  ownerId: string;
  guildId?: string;
  
  location: {
    geoHash: string;
    latitude: number;
    longitude: number;
    altitude: number;
    cloudAnchorId: string;  // VPS anchor reference
  };
  
  structure: {
    coreLevel: number;
    health: number;
    maxHealth: number;
    blocks: CitadelBlock[];
  };
  
  defenses: {
    guardians: Guardian[];
    turrets: Turret[];
    walls: Wall[];
  };
  
  zone: {
    cityId: string;
    districtId: string;
    zoneId: string;
  };
  
  createdAt: Timestamp;
  lastRaided?: Timestamp;
}

interface CitadelBlock {
  id: string;
  blueprintId: string;
  localPosition: Vector3;
  localRotation: Quaternion;
  material: string;
  health: number;
}
```

### Spatial Anchor Document
```typescript
interface SpatialAnchor {
  id: string;
  
  // Coarse location
  geoHash: string;
  latitude: number;
  longitude: number;
  
  // VPS anchor (from ARCore Geospatial or Niantic)
  cloudAnchorId: string;
  provider: 'arcore' | 'niantic' | 'custom';
  
  // Anchor quality metrics
  confidence: number;
  lastValidated: Timestamp;
  validationCount: number;
  
  // What's attached to this anchor
  attachedObjects: string[];  // Citadel IDs, Resource Node IDs, etc.
  
  createdAt: Timestamp;
  createdBy: string;
}
```

---

## API Endpoints

### REST API (Cloud Functions)

```
Authentication
  POST   /auth/register
  POST   /auth/link-social

User Management
  GET    /users/{uid}
  PATCH  /users/{uid}
  GET    /users/{uid}/inventory

Citadels
  POST   /citadels                    # Create new citadel
  GET    /citadels/{id}               # Get citadel details
  GET    /citadels/nearby?lat&lng&r   # Get citadels in radius
  POST   /citadels/{id}/blocks        # Add block to citadel
  DELETE /citadels/{id}/blocks/{bid}  # Remove block

Spatial Anchors
  POST   /anchors                     # Register new anchor
  GET    /anchors/nearby?lat&lng&r    # Get anchors in radius
  POST   /anchors/{id}/validate       # Confirm anchor still valid

Battles
  POST   /battles/initiate            # Start raid
  GET    /battles/{id}                # Get battle state
  POST   /battles/{id}/action         # Submit battle action

Resources
  GET    /resources/nodes?lat&lng&r   # Get resource nodes nearby
  POST   /resources/harvest           # Collect from node

Leaderboards
  GET    /leaderboards/global
  GET    /leaderboards/district/{id}
  GET    /leaderboards/guild/{id}
```

### Real-Time Events (WebSocket / Photon)

```
Channels:
  zone:{zoneId}           # All events in a zone
  citadel:{citadelId}     # Citadel-specific events
  battle:{battleId}       # Active battle events
  user:{userId}           # Private user events

Events:
  citadel.damaged         # Citadel took damage
  citadel.destroyed       # Citadel was destroyed
  battle.started          # Raid initiated
  battle.action           # Attack/defend action
  battle.ended            # Battle concluded
  player.entered_zone     # Player arrived in zone
  resource.spawned        # New resource node appeared
```

---

## Security Considerations

### Anti-Cheat Measures

1. **GPS Spoofing Detection**
   - Velocity checks (can't teleport)
   - Accelerometer correlation
   - Cell tower triangulation (where available)

2. **Server-Side Validation**
   - All game state changes validated on server
   - Client is "dumb terminal" for critical operations

3. **Rate Limiting**
   - Resource harvesting cooldowns
   - Building speed limits
   - API request throttling

4. **Replay Protection**
   - Signed timestamps on all requests
   - Nonce-based duplicate detection

---

## Scalability Strategy

### Phase 1: Soft Launch (10K users)
- Single region (us-central1)
- Firestore handles all persistence
- Basic caching

### Phase 2: Regional Launch (100K users)
- Multi-region deployment
- Redis caching layer
- CDN for static assets

### Phase 3: Global Launch (1M+ users)
- Global load balancing
- Regional Firestore instances
- Sharded game servers
- ML-based fraud detection

---

## Performance Targets

| Metric | Target | Critical |
|--------|--------|----------|
| App Launch → AR Ready | < 3 seconds | < 5 seconds |
| Anchor Resolution | < 2 seconds | < 4 seconds |
| Citadel Load (nearby) | < 1 second | < 2 seconds |
| Battle Latency | < 100ms | < 200ms |
| Frame Rate | 60 FPS | 30 FPS |
| Battery (1hr play) | < 20% | < 30% |

---

## Next Steps

1. **Prototype: Persistent Cube Test** ← START HERE
2. Build resource harvesting mechanics
3. Implement basic building system
4. Add multiplayer sync
5. Create battle system
6. Polish and soft launch
