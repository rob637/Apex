# Apex Citadels: Development Roadmap

## ✅ Phase 0: The Persistent Cube Test (Complete)
*Goal: Prove core technology works*

### Milestone 0.1: Basic AR Scene ✅
- [x] Unity project setup with AR Foundation
- [x] Camera permission handling
- [x] Plane detection working
- [x] Place a cube on detected surface

### Milestone 0.2: Cloud Anchors ✅
- [x] Integrate ARCore Geospatial API
- [x] Host a cloud anchor at cube location
- [x] Store anchor ID in Firebase
- [x] Resolve anchor on different device
- [x] **SUCCESS: Cube persists across devices!**

---

## ✅ Phase 1-4: Core Game Systems (Complete)
*All 70+ C# scripts created for AR client - see REQUIREMENTS.md for full details*

- [x] Resource System (5 types, GPS-based collection)
- [x] Building System (30+ block types, templates)
- [x] Territory System (claim, contest, siege)
- [x] Combat System (turn-based, 6 troop types)
- [x] Alliance System (10 members, war declarations)
- [x] Season Pass (100 tiers, battle pass)
- [x] World Events (FOMO mechanics)
- [x] Social Features (friends, chat, referrals)
- [x] Admin Dashboard (React + MUI)
- [x] Firebase Backend (20 Cloud Functions)

---

## ✅ Phase 5: PC Hybrid Client (Complete)
*Goal: Desktop "Command Center" experience sharing same Firebase backend*

### Milestone 5.1: PC Core Systems ✅
- [x] Platform detection & feature gating
- [x] Multi-mode camera (WorldMap, Territory, FirstPerson, Cinematic)
- [x] Keyboard/mouse input with rebinding
- [x] 3D strategic world map renderer
- [x] Main game state machine

### Milestone 5.2: PC Building & Editor ✅
- [x] Detailed base editor with grid snapping
- [x] Undo/redo system (100 action history)
- [x] Blueprint save/load (design on PC, place in AR)
- [x] Building preview with valid/invalid materials

### Milestone 5.3: PC-Exclusive Features ✅
- [x] Battle replay viewer with playback controls
- [x] Crafting workshop (queue, quality tiers)
- [x] Market/trading panel
- [x] Alliance War Room
- [x] Statistics dashboard with analytics

### Milestone 5.4: PC UI System ✅
- [x] Panel management system
- [x] Territory detail panel
- [x] Alliance panel with chat
- [x] Build menu with categories
- [x] Crafting panel
- [x] Market panel
- [x] Replay panel

### Milestone 5.5: Unity Editor Tools ✅
- [x] PCPrefabCreator (menu: Apex/PC/Create All PC Prefabs)
- [x] PCSceneSetup (menu: Apex/PC/Setup PC Scene)
- [x] Scene validation tool

### Deliverable
```
PC client with 18 scripts ready for Unity integration.
Run "Apex > PC > Setup PC Scene (Full)" in Unity to create PCMain.unity.
Same Firebase backend as AR client - one world, two windows.
```

---

## 🔄 Phase 6: Unity Integration (Current)

### Milestone 3.2: Architect NPCs
- [ ] Design 4 architect personalities
- [ ] Implement architect recruitment
- [ ] Unique building styles per architect
- [ ] Architect leveling system

*Goal: Import scripts, configure scenes, build and test*

### Milestone 6.1: Scene Setup 🔄
- [ ] Open Unity Editor
- [ ] Run Apex > PC > Setup PC Scene (Full)
- [ ] Run Apex > PC > Create All PC Prefabs
- [ ] Run Apex > PC > Validate PC Scene
- [ ] Configure Firebase settings

### Milestone 6.2: Integration Testing
- [ ] Verify platform detection works
- [ ] Test camera modes
- [ ] Test input handling
- [ ] Test UI panels
- [ ] Verify Firebase connectivity

### Milestone 6.3: Build & Deploy
- [ ] Build PC executable
- [ ] Test on Windows
- [ ] Test shared data with AR client

### Deliverable
```
Working PC client that shares Firebase backend with AR client.
Players can manage bases on PC, place them via AR.
```

---

## 📅 Future Phases

### Phase 7: Soft Launch Prep
- [ ] Select 2-3 test cities
- [ ] Beta tester recruitment
- [ ] Analytics dashboard
- [ ] Customer support system

### Phase 8: Global Launch
- [ ] Multi-region deployment
- [ ] Marketing campaign
- [ ] Season 1 Battle Pass
- [ ] Post-launch content

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
