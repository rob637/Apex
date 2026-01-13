# Apex Citadels: Development Roadmap

## 🚀 Phase 0: The Persistent Cube Test (Week 1-2)
*Goal: Prove core technology works*

### Milestone 0.1: Basic AR Scene
- [ ] Unity project setup with AR Foundation
- [ ] Camera permission handling
- [ ] Plane detection working
- [ ] Place a cube on detected surface

### Milestone 0.2: Cloud Anchors
- [ ] Integrate ARCore Geospatial API
- [ ] Host a cloud anchor at cube location
- [ ] Store anchor ID in Firebase
- [ ] Resolve anchor on different device
- [ ] **SUCCESS: Cube persists across devices!**

### Deliverable
```
Two phones can see the same cube in the same real-world location,
even after closing and reopening the app.
```

---

## 🏗️ Phase 1: Core Mechanics Prototype (Week 3-6)

### Milestone 1.1: Resource System
- [ ] Define resource types and properties
- [ ] Create resource node spawning logic
- [ ] Implement GPS-based node discovery
- [ ] Build harvesting interaction
- [ ] Add inventory management

### Milestone 1.2: Building System v1
- [ ] Create basic block prefabs (wall, floor, ramp)
- [ ] Implement block placement in AR
- [ ] Add block snapping/grid system
- [ ] Save structure to Firestore
- [ ] Load structure for other players

### Milestone 1.3: Account System
- [ ] Firebase Auth integration
- [ ] Anonymous → Social account linking
- [ ] Basic user profile
- [ ] Faction selection

### Deliverable
```
Player can walk around, collect resources, build a simple structure,
and another player can see that structure.
```

---

## ⚔️ Phase 2: Social & Combat (Week 7-12)

### Milestone 2.1: Territory System
- [ ] Define zone/district/city hierarchy
- [ ] Implement geohash-based zone detection
- [ ] Create Citadel Core (claim mechanic)
- [ ] Build zone ownership tracking
- [ ] Add territory map view

### Milestone 2.2: Real-Time Multiplayer
- [ ] Integrate Photon Fusion
- [ ] Player presence in zones
- [ ] Real-time structure sync
- [ ] Basic proximity chat/emotes

### Milestone 2.3: Battle System v1
- [ ] Raid initiation flow
- [ ] Basic attack/defend mechanics
- [ ] Health and damage system
- [ ] Battle resolution logic
- [ ] Victory/defeat rewards

### Milestone 2.4: Guild System
- [ ] Guild creation/management
- [ ] Guild chat
- [ ] Shared resource pool
- [ ] Guild leaderboard

### Deliverable
```
Players can form guilds, claim territories by building Citadels,
and raid other players' Citadels in real-time AR combat.
```

---

## 🎨 Phase 3: Content & Polish (Week 13-18)

### Milestone 3.1: Blueprint System
- [ ] Design blueprint rarity tiers
- [ ] Create 20+ unique blueprints
- [ ] Implement blueprint discovery (POIs)
- [ ] Blueprint inventory UI

### Milestone 3.2: Architect NPCs
- [ ] Design 4 architect personalities
- [ ] Implement architect recruitment
- [ ] Unique building styles per architect
- [ ] Architect leveling system

### Milestone 3.3: Visual Polish
- [ ] Occlusion shaders
- [ ] Particle effects (building, destruction)
- [ ] Sound design
- [ ] Haptic feedback
- [ ] Day/night cycle effects

### Milestone 3.4: Monetization v1
- [ ] Battle Pass framework
- [ ] Cosmetic shop
- [ ] Premium currency

### Deliverable
```
Feature-complete game with polished visuals, multiple blueprints,
architect system, and basic monetization.
```

---

## 🌍 Phase 4: Soft Launch (Week 19-24)

### Milestone 4.1: Soft Launch Prep
- [ ] Select 2-3 test cities
- [ ] Beta tester recruitment
- [ ] Analytics dashboard
- [ ] Customer support system
- [ ] Bug tracking workflow

### Milestone 4.2: Live Operations
- [ ] First Territory War event
- [ ] Weekly challenges
- [ ] Community management
- [ ] Balance adjustments
- [ ] Performance optimization

### Milestone 4.3: Feedback Integration
- [ ] User interview sessions
- [ ] Metrics analysis
- [ ] Feature prioritization
- [ ] Quick iteration cycle

### Deliverable
```
Game live in limited markets, gathering real user feedback,
iterating rapidly on core experience.
```

---

## 🚀 Phase 5: Global Launch (Week 25-36)

### Milestone 5.1: Scale Infrastructure
- [ ] Multi-region deployment
- [ ] Load testing at scale
- [ ] Anti-cheat hardening
- [ ] CDN optimization

### Milestone 5.2: Marketing Push
- [ ] App Store optimization
- [ ] Influencer partnerships
- [ ] PR campaign
- [ ] Community events

### Milestone 5.3: Post-Launch Content
- [ ] Season 1 Battle Pass
- [ ] New blueprints monthly
- [ ] Limited-time events
- [ ] B2B partnership pilots

---

## 📊 Success Metrics

### Phase 0-1 (Technical Validation)
- Anchor persistence: >95% success rate
- Cross-device sync: <2 second latency
- App stability: <1% crash rate

### Phase 2-3 (Product-Market Fit)
- D1 Retention: >40%
- D7 Retention: >20%
- Session length: >15 minutes average
- DAU/MAU: >25%

### Phase 4-5 (Growth)
- Organic installs: >30% of total
- Viral coefficient: >0.5
- ARPDAU: >$0.10
- LTV/CAC: >3x

---

## 🎯 Key Risk Mitigations

| Risk | Mitigation |
|------|------------|
| VPS coverage gaps | Fallback to GPS + IMU anchoring |
| Battery drain | Aggressive power management, background limits |
| GPS spoofing | Multi-factor location validation |
| Network latency | Optimistic updates with reconciliation |
| Content moderation | ML-based filtering + user reporting |
| Competition | Speed to market, unique building mechanic |

---

## 💡 Innovation Opportunities

### Near-Term
- Integration with fitness apps (steps = resources)
- AR photo mode for social sharing
- Local business partnerships (sponsored safe zones)

### Medium-Term
- UGC blueprint marketplace
- Cross-game NFT interoperability
- eSports territorial competitions

### Long-Term
- XR glasses as primary platform
- Persistent AR world beyond gaming
- Real-world commerce integration

---

*"Move fast, but build to last."*
