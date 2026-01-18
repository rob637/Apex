# Apex Citadels: PC Hybrid Mode Design Document

**Status:** Phase 2A Implementation In Progress 🔄  
**Created:** January 17, 2026  
**Updated:** January 17, 2026  
**Priority:** Phase 2-3  

---

## � WORLD-CLASS FEATURES CHECKLIST

These are the elements that make Apex Citadels unforgettable — sticky, challenging, beautiful, and impossible to put down.

### 🎨 AMAZING GRAPHICS & VISUALS

| Feature | Platform | Status | Impact |
|---------|----------|--------|--------|
| **Stylized "Mythic Modern" Art Direction** | Both | ⏳ | Unique visual identity |
| **Dynamic Weather System** | Both | ⏳ | Rain, snow, fog affects gameplay & mood |
| **Day/Night Cycle** | Both | ⏳ | Real-time sync with actual world |
| **Particle Effects (Magic, Fire, Combat)** | Both | ⏳ | Visceral, satisfying feedback |
| **Shader Effects (Glow, Dissolve, Portals)** | Both | ⏳ | "Wow factor" moments |
| **Cinematic Camera Modes** | PC | 🔧 | Dramatic replays & screenshots |
| **AR Occlusion (Real objects hide virtual)** | Mobile | ⏳ | Believable AR integration |
| **Skybox Environments** | PC | ⏳ | Blockade-generated fantasy skies |
| **3D Building Models (50+ unique)** | Both | ⏳ | Meshy-generated variety |
| **Character Animations (Idle, Combat, Emotes)** | Both | ⏳ | Mixamo rigged & expressive |
| **VFX for Building/Upgrade/Destruction** | Both | ⏳ | Satisfying construction |
| **UI Polish (Animations, Transitions, Sounds)** | Both | ⏳ | Buttery smooth feel |

### 🎯 STICKY ENGAGEMENT (Can't Put It Down)

| Feature | Platform | Status | Psychology |
|---------|----------|--------|------------|
| **Daily Login Rewards (30-day streak)** | Both | ✅ Script | Variable ratio schedule |
| **Season Pass (100 tiers)** | Both | ✅ Script | Progression treadmill |
| **Limited-Time World Events** | Both | ✅ Script | FOMO + community excitement |
| **Push Notifications (Attack alerts)** | Mobile | ✅ Script | Re-engagement hooks |
| **Alliance Chat & Social** | Both | ✅ Script | Social obligations |
| **Leaderboards (Personal, Alliance, Regional)** | Both | ✅ Script | Competition drive |
| **Achievement System (100+ badges)** | Both | ✅ Script | Completionist appeal |
| **Referral Rewards** | Both | ✅ Script | Viral growth |
| **"Your territory is under attack!"** | Both | ⏳ | Urgent call to action |
| **Activity Feed ("X just took Y's territory")** | Both | ⏳ | Social proof, rivalry |
| **Weekly Challenges** | Both | ⏳ | Fresh goals |
| **Streak Multipliers** | Both | ⏳ | Loss aversion on breaks |

### ⚔️ CHALLENGING & STRATEGIC (Skill Matters)

| Feature | Platform | Status | Depth |
|---------|----------|--------|-------|
| **6 Troop Types with Counters** | Both | ✅ Designed | Rock-paper-scissors+ |
| **Turn-Based Tactical Combat** | Both | ⏳ | Think, plan, execute |
| **Building Placement Strategy** | Both | 🔧 Script | Defense layouts matter |
| **Alliance War Coordination** | Both | ✅ Script | Team strategy |
| **3-Strike Siege System** | Both | ✅ Script | Stakes without devastation |
| **Resource Management** | Both | ✅ Script | Economy decisions |
| **Troop Composition Planning** | Both | ⏳ | Pre-battle strategy |
| **Terrain Bonuses (Hills, Water)** | Mobile | ⏳ | Real-world geography matters |
| **Time Zone Strategy** | Both | ⏳ | Global warfare meta |
| **Spy/Scout Mechanics** | Both | ⏳ | Information warfare |
| **Ambush & Trap Systems** | Both | ⏳ | Defensive creativity |

### 🎭 CREATIVE EXPRESSION (Make It Yours)

| Feature | Platform | Status | Personalization |
|---------|----------|--------|-----------------|
| **Base Editor with Undo/Redo** | PC | 🔧 Script | Full creative control |
| **50+ Building Types** | Both | ⏳ | Variety in design |
| **Color/Material Customization** | Both | ⏳ | Personal aesthetics |
| **Alliance Banners & Crests** | Both | ⏳ | Team identity |
| **Blueprint System (Design→Place)** | Both | 🔧 Designed | Plan on PC, deploy in AR |
| **Decoration Items (Non-combat)** | Both | ⏳ | Pure expression |
| **Emotes & Taunts** | Both | ⏳ | Social fun |
| **Profile Customization** | Both | ⏳ | Avatar, frames, titles |
| **Screenshot/Share Mode** | Both | ⏳ | Social bragging |
| **Citadel Naming** | Both | ⏳ | Personal attachment |

### 🌍 REAL-WORLD INTEGRATION (Magic in YOUR World)

| Feature | Platform | Status | Connection |
|---------|----------|--------|------------|
| **GPS Territory Claiming** | Mobile | ✅ Script | "I own my block" |
| **Real Address Display** | Both | ⏳ | "123 Main Street" pride |
| **Local Landmarks as Bonuses** | Both | ⏳ | Visit real POIs |
| **Weather Sync (Real → Game)** | Both | ⏳ | Immersion |
| **Neighborhood Rivalries** | Both | ⏳ | School vs school |
| **Regional Leaderboards** | Both | ⏳ | City pride |
| **AR Selfies with Citadel** | Mobile | ⏳ | Social sharing |
| **Walking Distance Rewards** | Mobile | ✅ Script | Health gamification |
| **Presence Detection (100% vs 50%)** | Mobile | ✅ Designed | Reward showing up |

### 🔊 AUDIO EXCELLENCE

| Feature | Platform | Status | Feel |
|---------|----------|--------|------|
| **Original Soundtrack (Epic/Ambient)** | Both | ⏳ | Suno-generated |
| **Adaptive Music (Battle intensity)** | Both | ⏳ | Dynamic tension |
| **3D Spatial Audio** | Both | ⏳ | Immersive positioning |
| **Satisfying SFX (Build, Attack, Collect)** | Both | ⏳ | Feedback loops |
| **Voice Lines (Troops, Commanders)** | Both | ⏳ | ElevenLabs generated |
| **UI Sounds (Clicks, Success, Error)** | Both | ⏳ | Polish |
| **Ambient Environmental Audio** | Both | ⏳ | World feels alive |

### 📱 MOBILE-SPECIFIC (Android & iOS)

| Feature | Platform | Status | Notes |
|---------|----------|--------|-------|
| **ARCore Support** | Android | ⏳ | Required for AR |
| **ARKit Support** | iOS | ⏳ | Required for AR |
| **Geospatial API (Cloud Anchors)** | Both | ⏳ | Persistent AR across devices |
| **Offline Mode (Limited)** | Both | ✅ Script | Play without signal |
| **Battery Optimization** | Both | ⏳ | 60min+ sessions |
| **Haptic Feedback** | Both | ⏳ | Tactile response |
| **Widget Support** | Both | ⏳ | Territory status at glance |
| **App Clips / Instant Apps** | Both | ⏳ | Try before install |
| **Face ID / Biometric Auth** | Both | ⏳ | Quick secure login |
| **Portrait & Landscape** | Both | ⏳ | Flexible play |

### 🖥️ PC-SPECIFIC (WebGL)

| Feature | Platform | Status | Notes |
|---------|----------|--------|-------|
| **4 Camera Modes** | PC | 🔧 Script | WorldMap, Territory, FP, Cinematic |
| **Keyboard Shortcuts (WASD, Tab, etc.)** | PC | 🔧 Script | Power user efficiency |
| **Key Rebinding** | PC | 🔧 Script | Accessibility |
| **Battle Replay System** | PC | 🔧 Script | Learn from losses |
| **Advanced Crafting Workshop** | PC | 🔧 Script | Quality system |
| **Statistics Dashboard** | PC | 🔧 Script | Analytics nerds |
| **Market with Charts** | PC | 🔧 Script | Trading depth |
| **Multi-Territory Management** | PC | 🔧 Script | Empire overview |
| **Discord Integration** | PC | ⏳ | Rich presence |
| **Streaming Mode** | PC | ⏳ | Hide sensitive info |

---

## 🎮 THE MAGIC FORMULA: MINECRAFT × FORTNITE × POKÉMON GO

Apex Citadels combines the **best elements** from three genre-defining games:

### From MINECRAFT: Creative Building & Exploration

| Feature | How Apex Does It | Platform |
|---------|------------------|----------|
| **Block-by-block building** | Place walls, towers, defenses piece by piece | Both |
| **First-person exploration** | Walk through YOUR citadel in FP view | PC |
| **Mining/gathering** | Walk to collect Stone, Wood, Iron, Crystal | Mobile |
| **Survival against threats** | Defend against real player attacks | Both |
| **Show off your creation** | Others SEE your citadel in AR | Both |
| **Procedural variety** | Different terrain bonuses per real location | Both |
| **Multiplayer building** | Alliance members can contribute to shared projects | Both |

### From FORTNITE: Social Events & Competitive Seasons

| Feature | How Apex Does It | Platform |
|---------|------------------|----------|
| **Season Pass (Battle Pass)** | 100-tier progression with cosmetics & rewards | Both |
| **Limited-time events** | World Events with giant AR structures | Both |
| **Social hangout spaces** | Visit friends' citadels, hang out in alliance hall | PC |
| **Emotes & expression** | Dance on enemy ruins, celebrate victories | Both |
| **Competitive seasons** | 10-week seasons with regional leaderboards | Both |
| **Cosmetic customization** | Skins, banners, effects, building themes | Both |
| **Spectator mode** | Watch epic battles unfold | PC |
| **Live events** | Synchronized world-changing moments | Both |

### From POKÉMON GO: Real-World Adventure

| Feature | How Apex Does It | Platform |
|---------|------------------|----------|
| **GPS exploration** | Walk to discover territories & resources | Mobile |
| **Claim real locations** | "I own the park near my house" | Mobile |
| **Community events** | Alliance raids, territory wars | Both |
| **Collection drive** | Blueprints, achievements, cosmetics | Both |
| **Social trading** | Market system for resources & items | Both |
| **Local rivalries** | Neighborhood vs neighborhood | Both |
| **Exercise gamification** | Walking = resources = stronger | Mobile |
| **AR magic in real world** | See YOUR fortress through phone camera | Mobile |

---

## 🕹️ PC ACTIVE GAMEPLAY (Not Just Management!)

The PC isn't a dashboard — it's a **GAME**. Here's what makes PC play ACTIVELY FUN:

### 🏰 CITADEL EXPLORATION MODE (Minecraft-style)

| Feature | Description | Status |
|---------|-------------|--------|
| **First-Person Walkthrough** | WASD to walk through your citadel interior | 🔧 Camera exists |
| **Interior Decoration** | Place furniture, trophies, displays inside | ⏳ |
| **NPC Citizens** | Your citadel has animated inhabitants | ⏳ |
| **Interactive Objects** | Click anvils, forges, training dummies | ⏳ |
| **Day/Night Ambiance** | Watch sunset from your tower balcony | ⏳ |
| **Weather Inside** | Rain on your courtyard, snow on battlements | ⏳ |
| **Pet Companions** | Creatures that follow you around | ⏳ |
| **Hidden Easter Eggs** | Discoverable secrets in your own citadel | ⏳ |

### ⚔️ ACTIVE COMBAT (Not Turn-Based Waiting)

| Feature | Description | Status |
|---------|-------------|--------|
| **Real-Time Battle Mode** | Control troops directly during defense | ⏳ |
| **Hero Commander** | You ARE a unit on the battlefield | ⏳ |
| **Quick Match Arena** | Instant PvP battles (separate from territory) | ⏳ |
| **Tower Defense Waves** | Survive waves of AI raiders | ⏳ |
| **Boss Raids** | Alliance fights giant world bosses | ⏳ |
| **Training Grounds** | Practice combat without risk | ⏳ |
| **Duel System** | 1v1 ranked matches for honor | ⏳ |
| **Spectate Live Battles** | Watch friends fight in real-time | ⏳ |

### 🎨 CREATIVE MODE (Full Minecraft Building)

| Feature | Description | Status |
|---------|-------------|--------|
| **Unlimited Sandbox** | Build anything with no resource limits | ⏳ |
| **Blueprint Export** | Save designs to use in real territories | 🔧 Designed |
| **Community Gallery** | Share blueprints, download others' designs | ⏳ |
| **Building Contests** | Weekly themes, community voting | ⏳ |
| **Time-Lapse Builder** | Watch your citadel construct itself | ⏳ |
| **Destruction Sandbox** | Test defenses by simulating attacks | ⏳ |
| **Terrain Editor** | Modify ground, add water, hills | ⏳ |
| **Lighting Designer** | Place torches, magic lights, effects | ⏳ |

### 🎪 SOCIAL HUB (Fortnite Lobby-style)

| Feature | Description | Status |
|---------|-------------|--------|
| **Alliance Hall** | 3D space where alliance hangs out | ⏳ |
| **Global Plaza** | Public space to meet other players | ⏳ |
| **Emote Interactions** | Dance, wave, taunt with others | ⏳ |
| **Mini-Games** | Arcade games in social spaces | ⏳ |
| **Trophy Room** | Display achievements, conquered flags | ⏳ |
| **War Room Table** | 3D holographic battle planning | ⏳ |
| **Merchant NPCs** | Shop from characters, not menus | ⏳ |
| **Event Stages** | Watch live events together | ⏳ |

### 🗺️ WORLD EXPLORATION (Not Just Clicking Map)

| Feature | Description | Status |
|---------|-------------|--------|
| **Fly-Through Mode** | Smoothly fly between territories like Google Earth | ⏳ |
| **Zoom to Street Level** | See detailed 3D terrain of any location | ⏳ |
| **Scout Enemy Bases** | Inspect rival citadels before attacking | ⏳ |
| **Discover Hidden Nodes** | Find secret resource spots on the map | ⏳ |
| **Time-Machine View** | Replay history of territory changes | ⏳ |
| **Weather Radar** | See real weather affecting different regions | ⏳ |
| **Alliance Borders** | Visualize territory control dramatically | ⏳ |
| **Landmark Hunting** | Discover real-world POIs for bonuses | ⏳ |

### 🎰 PROGRESSION LOOPS (Always Something To Do)

| Feature | Description | Status |
|---------|-------------|--------|
| **Daily Quests** | 3 unique objectives each day | ⏳ |
| **Weekly Challenges** | Bigger goals for bigger rewards | ⏳ |
| **Season Missions** | Epic multi-week storylines | ⏳ |
| **Crafting Queue** | Always crafting something in background | 🔧 Script |
| **Research Tree** | Unlock new buildings, troops, abilities | ⏳ |
| **Collection Log** | Track all blueprints, skins, achievements | ⏳ |
| **Prestige System** | Reset for permanent bonuses | ⏳ |
| **Mastery Challenges** | Per-building/troop mastery tracks | ⏳ |


---

## 🏠 100% HOME PC PLAYER - COMPLETE STANDALONE EXPERIENCE

**Design Philosophy:** A player who NEVER touches mobile should have 500+ hours of fun.

### What You CAN Do 100% From PC (No Mobile Required)

| Activity | How It Works | Fun Factor |
|----------|--------------|------------|
| **Claim Territories** | "Remote Claim" costs 2x resources but works | Full ownership |
| **Build Citadels** | Full base editor, same as mobile | Creative expression |
| **Fight Battles** | 50% power, but full participation | Strategic depth |
| **Join Alliances** | Chat, coordinate, war planning | Social connection |
| **Craft Items** | PC-exclusive advanced crafting | Unique advantage |
| **Trade on Market** | Full market access with charts | Economy gameplay |
| **Complete Season Pass** | All 100 tiers achievable | Progression |
| **Earn Achievements** | 80%+ available on PC | Completionist |
| **Watch Replays** | Every battle, frame by frame | Learn & improve |
| **Design Blueprints** | Creative mode unlimited | Pure creativity |
| **Explore World** | Fly anywhere, scout enemies | Discovery |
| **Compete on Leaderboards** | Separate PC rankings exist | Fair competition |

### PC-Only Advantages (Reward for Being Here)

| Feature | Benefit | Why It's Special |
|---------|---------|------------------|
| **Crafting Quality System** | Create Superior/Epic/Legendary items | Mobile = Normal only |
| **Battle Replays** | Frame-by-frame analysis | Mobile = no replays |
| **Statistics Dashboard** | Deep analytics on everything | Data nerds rejoice |
| **Market Charts** | Price history, trend analysis | Trader advantage |
| **Keyboard Shortcuts** | Instant actions, no tap-tap-tap | Efficiency |
| **Multi-Monitor Support** | Map + Citadel + Chat views | Power users |
| **Creative Mode** | Unlimited sandbox building | Test ideas free |
| **Streaming Mode** | Hide player info for content creators | Twitch-friendly |
| **Bulk Operations** | Manage all territories at once | Scale management |

### PC Daily Gameplay Loop (100% Fun At Home)

\`\`\`
MORNING (10 min)
├── Collect daily rewards
├── Check overnight attack notifications  
├── Queue crafting jobs
└── Review market prices

AFTERNOON SESSION (30-60 min)
├── Design/upgrade citadel in Base Editor
├── Participate in scheduled battles (50% power is still FUN)
├── Work on daily quests
├── Chat with alliance, plan strategy
└── Browse Creative Mode gallery for inspiration

EVENING SESSION (60-120 min)
├── Alliance War participation
├── Extended building session
├── Watch battle replays, learn tactics
├── Trade on market
├── Complete weekly challenges
└── Explore world map, scout enemies

ALWAYS RUNNING
├── Crafting queue (background)
├── Alliance chat
└── Attack notifications
\`\`\`

### How PC Catches Up Without Mobile

| Mobile Advantage | PC Compensation | Balance |
|------------------|-----------------|---------|
| 100% battle power | PC gets replay analysis + better prep | Strategy vs brute force |
| Walking = resources | PC crafting creates rare items | Different economy |
| In-person claiming | Remote claim (2x cost) | Pay more, play different |
| AR immersion | 3D first-person immersion | Different vibe, same fun |
| Local discovery | World map exploration | See MORE of the world |

---

## 📱 100% MOBILE FIELD PLAYER - COMPLETE STANDALONE EXPERIENCE

**Design Philosophy:** A player who NEVER opens PC should have 500+ hours of fun.

### What You CAN Do 100% From Mobile (No PC Required)

| Activity | How It Works | Fun Factor |
|----------|--------------|------------|
| **Claim Territories** | Walk there, plant flag | Real-world ownership |
| **Build Citadels** | Full AR building, place each piece | Minecraft in reality |
| **Fight Battles** | 100% power when present | Maximum impact |
| **Join Alliances** | Mobile chat, coordinate on the go | Social anywhere |
| **Craft Items** | Quick crafts (no quality system) | Fast & functional |
| **Trade on Market** | Full access (simpler UI) | Economy gameplay |
| **Complete Season Pass** | All 100 tiers achievable | Progression |
| **Earn Achievements** | 100% available on mobile | Full completionist |
| **Gather Resources** | Walk, collect, explore | Exercise & game |
| **Discover Nodes** | Find secret spots in real world | Treasure hunting |
| **AR Photography** | Selfies with YOUR citadel | Social sharing |
| **Compete on Leaderboards** | Same global rankings | Fair competition |

### Mobile-Only Advantages (Reward for Being Outside)

| Feature | Benefit | Why It's Special |
|---------|---------|------------------|
| **100% Battle Power** | Full combat effectiveness | PC = 50% |
| **Free Territory Claims** | Standard resource cost | PC = 2x cost |
| **Walking Resources** | Passive income while moving | PC = no walking |
| **AR Immersion** | SEE it in the real world | Magic made real |
| **GPS Discovery** | Find hidden nodes, landmarks | Explore your city |
| **Physical Presence Bonus** | Bonus rewards for showing up | Rewarded activity |
| **Community Meetups** | Alliance raids in person | Real friendships |
| **Territory Selfies** | "I own this" social proof | Bragging rights |
| **Local Rivalries** | Face-to-face competition | Real stakes |

### Mobile Daily Gameplay Loop (100% Fun Outside)

\`\`\`
COMMUTE (15-20 min)
├── Collect walking resources automatically
├── Check territory status
├── Quick chat with alliance
└── Claim daily rewards

LUNCH BREAK (20-30 min)
├── Walk to nearby territory, reinforce it
├── Scout enemy positions in area
├── Collect resource nodes
├── Participate in battle (if scheduled)
└── Quick build session in AR

EVENING WALK (30-60 min)
├── Extended resource gathering route
├── Claim new territory if ready
├── Major building session at home base
├── Alliance coordination for raids
├── World Event participation
└── AR photography session

WEEKEND SESSION (2-3 hours)
├── Multi-territory maintenance tour
├── Alliance war participation
├── New territory expansion
├── Community meetup events
├── Landmark discovery expedition
└── Competitive play session
\`\`\`

### How Mobile Succeeds Without PC

| PC Advantage | Mobile Compensation | Balance |
|--------------|---------------------|---------|
| Crafting quality | Combat power (100% vs 50%) | Fight vs forge |
| Battle replays | Was THERE, felt it live | Analysis vs experience |
| Statistics dashboard | Instinct & experience | Data vs intuition |
| Keyboard efficiency | Touch feels natural in AR | Different interfaces |
| Multi-window views | Focused mobile experience | Depth vs simplicity |

---

## 🤝 SYNERGY (Why Playing Both Is Best - But Not Required)

| Combined Benefit | How It Works |
|------------------|--------------|
| **Design on PC → Deploy in AR** | Blueprint system, plan at home, build outside |
| **Fight in AR → Analyze on PC** | Battle live, then study the replay |
| **Scout on PC → Strike on Mobile** | Find targets from orbit, attack in person |
| **Craft on PC → Use on Mobile** | Make Legendary items, wield them at 100% power |
| **Earn on Mobile → Trade on PC** | Gather resources walking, trade with full charts |

**The rule:** Both platforms are COMPLETE games. Together they're UNSTOPPABLE.
---

## �🎯 MASTER IMPLEMENTATION CHECKLIST

This is the definitive checklist for getting both **PC** and **AR Mobile** clients fully operational.

### Legend
- ✅ Complete and tested
- 🔧 Code exists, needs integration/testing  
- ⏳ Not started
- 🔴 Blocked by dependency

---

## PART A: PC CLIENT CHECKLIST

### A1. Firebase Backend ✅ COMPLETE

| Task | Status | Notes |
|------|--------|-------|
| Firebase project created | ✅ | apex-citadels-dev |
| Firestore database configured | ✅ | Collections: territories, players, alliances, etc. |
| Firestore security rules deployed | ✅ | Read public, write via functions |
| Firebase Hosting (admin) | ✅ | https://apex-citadels-dev.web.app |
| Firebase Hosting (pc) | ✅ | https://apex-citadels-pc.web.app |
| Cloud Functions deployed | ✅ | 20+ function modules |
| Service account for admin | ✅ | For seeding/admin operations |
| Test data seeded | ✅ | Vienna VA + SF territories |

### A2. PC Unity Scripts 🔧 CODE COMPLETE - NEEDS SCENE

| Script | Location | Status | Notes |
|--------|----------|--------|-------|
| `PlatformManager.cs` | PC/ | ✅ Ready | Static, no setup needed |
| `PCCameraController.cs` | PC/ | 🔧 | 4 camera modes |
| `PCInputManager.cs` | PC/ | 🔧 | WASD, mouse, key rebinding |
| `WorldMapRenderer.cs` | PC/ | 🔧 | 3D territory visualization |
| `BaseEditor.cs` | PC/ | 🔧 | Building placement, undo/redo |
| `PCGameController.cs` | PC/ | 🔧 | Main state machine |
| `PCSceneBootstrapper.cs` | PC/ | 🔧 | Auto scene setup |
| `PCTerritoryBridge.cs` | PC/ | 🔧 | Firebase integration |
| `BattleReplaySystem.cs` | PC/ | 🔧 | PC-exclusive replays |
| `CraftingSystem.cs` | PC/ | 🔧 | PC-exclusive crafting |

### A3. PC UI Panel Scripts 🔧 CODE COMPLETE - NEEDS PREFABS

| Script | Location | Status | Notes |
|--------|----------|--------|-------|
| `PCUIManager.cs` | PC/UI/ | 🔧 | Panel management |
| `TerritoryDetailPanel.cs` | PC/UI/ | 🔧 | Territory stats display |
| `AlliancePanel.cs` | PC/UI/ | 🔧 | War Room & members |
| `BuildMenuPanel.cs` | PC/UI/ | 🔧 | Building catalog |
| `StatisticsPanel.cs` | PC/UI/ | 🔧 | Analytics dashboard |
| `BattleReplayPanel.cs` | PC/UI/ | 🔧 | Replay viewer |
| `CraftingPanel.cs` | PC/UI/ | 🔧 | Crafting workshop |
| `MarketPanel.cs` | PC/UI/ | 🔧 | Trading interface |

### A4. WebGL Bridge ✅ CODE COMPLETE - NEEDS REBUILD

| Component | Status | Notes |
|-----------|--------|-------|
| `WebGLBridge.cs` | ✅ | C# DllImport bindings + Firebase callbacks |
| `WebGLBridge.jslib` | ✅ | Full JS functions + Firebase SDK calls |
| `WebGLBridgeComponent.cs` | ✅ | MonoBehaviour wrapper |
| `FirebaseWebClient.cs` | ✅ | REST API fallback + WebGL bridge integration |
| Firebase JS SDK in index.html | ✅ | Auth + Firestore initialized |
| Shader fixes | ✅ | WebGL-safe material creation |

### A5. Unity Editor Tools ✅ READY

| Tool | Status | Notes |
|------|--------|-------|
| `PCPrefabCreator.cs` | ✅ | Menu: Apex/PC/Create All PC Prefabs |
| `PCSceneSetup.cs` | ✅ | Menu: Apex/PC/Setup PC Scene |

### A6. PC Scene ⏳ NOT CREATED (Unity Editor Required)

| Task | Status | Instructions |
|------|--------|--------------|
| Create PCMain.unity | ⏳ | File → New Scene → Save as Assets/Scenes/PCMain.unity |
| Run scene setup wizard | ⏳ | Menu: Apex → PC → Setup PC Scene (Full) |
| Create UI prefabs | ⏳ | Menu: Apex → PC → Create All PC Prefabs |
| Wire up references | ⏳ | Assign camera, input, UI manager refs |
| Add WebGL bridge | ⏳ | Add WebGLBridge component to scene |

### A7. WebGL Build 🔧 DEPLOYED - NEEDS REBUILD FOR SHADER FIX

| Task | Status | Instructions |
|------|--------|--------------|
| Switch to WebGL platform | ✅ | File → Build Settings → WebGL |
| Configure Player Settings | ✅ | Compression: Disabled (Firebase Hosting issue), Memory: 512MB |
| Build | 🔧 | Output to backend/hosting-pc/build/ - **REBUILD NEEDED for shader fix** |
| Deploy | ✅ | firebase deploy --only hosting:pc |

**Note:** Current build has shader errors. After pulling latest code, rebuild WebGL in Unity.

---

## PART B: AR MOBILE CLIENT CHECKLIST

### B1. Core AR Systems ✅ CODE COMPLETE

| Script | Location | Status | Notes |
|--------|----------|--------|-------|
| `GameManager.cs` | Core/ | ✅ | Main initialization |
| `TerritoryManager.cs` | Territory/ | ✅ | Territory control |
| `BuildingManager.cs` | Building/ | ✅ | Block placement |
| `PlayerManager.cs` | Player/ | ✅ | Player state |
| `CombatManager.cs` | Combat/ | ✅ | Attack mechanics |
| `AllianceManager.cs` | Alliance/ | ✅ | Team system |
| `ResourceManager.cs` | Resources/ | ✅ | Resource gathering |
| `SpatialAnchorManager.cs` | AR/ | ✅ | AR anchor persistence |

### B2. Engagement Systems ✅ CODE COMPLETE

| Script | Location | Status | Notes |
|--------|----------|--------|-------|
| `WorldEventManager.cs` | WorldEvents/ | ✅ | FOMO events |
| `SeasonPassManager.cs` | SeasonPass/ | ✅ | 100-tier battle pass |
| `FriendsManager.cs` | Social/ | ✅ | Social features |
| `ChatManager.cs` | Chat/ | ✅ | Real-time chat |
| `ReferralManager.cs` | Referrals/ | ✅ | Viral growth |
| `AnalyticsManager.cs` | Analytics/ | ✅ | Event tracking |
| `AntiCheatManager.cs` | AntiCheat/ | ✅ | Location validation |
| `DailyRewardManager.cs` | DailyRewards/ | ✅ | Login streaks |
| `AchievementManager.cs` | Achievements/ | ✅ | Progress tracking |
| `LeaderboardManager.cs` | Leaderboard/ | ✅ | Rankings |

### B3. Monetization & UX ✅ CODE COMPLETE

| Script | Location | Status | Notes |
|--------|----------|--------|-------|
| `IAPManager.cs` | IAP/ | ✅ | In-app purchases |
| `NotificationManager.cs` | Notifications/ | ✅ | Push notifications |
| `TutorialManager.cs` | Tutorial/ | ✅ | Onboarding |
| `LocalDataManager.cs` | Data/ | ✅ | Offline persistence |
| `AudioManager.cs` | Audio/ | ✅ | SFX, music, ambient |
| `LocalizationManager.cs` | Localization/ | ✅ | 15 languages |
| `CosmeticsShopManager.cs` | Cosmetics/ | ✅ | Shop system |

### B4. Compliance & Safety ✅ CODE COMPLETE

| Script | Location | Status | Notes |
|--------|----------|--------|-------|
| `GDPRManager.cs` | Privacy/ | ✅ | Data export/deletion |
| `ContentModerationManager.cs` | Moderation/ | ✅ | Profanity filter, reports |
| `PerformanceMonitor.cs` | Monitoring/ | ✅ | FPS, memory, crashes |

### B5. AR Scene Setup ⏳ (Unity Editor Required)

| Task | Status | Instructions |
|------|--------|--------------|
| Create ARMain.unity (if not exists) | ⏳ | File → New Scene |
| Add AR Session | ⏳ | GameObject → XR → AR Session |
| Add XR Origin | ⏳ | GameObject → XR → XR Origin |
| Create GameManager object | ⏳ | Add all manager scripts |
| Create EngagementSystems object | ⏳ | Add engagement scripts |
| Create UI Canvas | ⏳ | Add HUD controllers |
| Configure AR camera | ⏳ | Set up Geospatial API |

### B6. Mobile Build ⏳ (Unity Editor Required)

| Platform | Status | Instructions |
|----------|--------|--------------|
| Android | ⏳ | Build Settings → Android, ARCore XR Plugin |
| iOS | ⏳ | Build Settings → iOS, ARKit XR Plugin |

---

## PART C: SHARED BACKEND INTEGRATION

### C1. Cloud Functions ✅ COMPLETE (20+ Modules)

| Function Module | File | Status |
|-----------------|------|--------|
| Combat/Battles | combat.ts | ✅ |
| Territory Control | territory.ts | ✅ |
| Alliance Wars | alliance.ts | ✅ |
| Blueprints | blueprint.ts | ✅ |
| Protection System | protection.ts | ✅ |
| Progression | progression.ts | ✅ |
| World Events | world-events.ts | ✅ |
| Season Pass | season-pass.ts | ✅ |
| Friends/Social | friends.ts | ✅ |
| Chat | chat.ts | ✅ |
| Referrals | referrals.ts | ✅ |
| Analytics | analytics.ts | ✅ |
| Anti-cheat | anticheat.ts | ✅ |
| IAP Validation | iap.ts | ✅ |
| Notifications | notifications.ts | ✅ |
| Moderation | moderation.ts | ✅ |
| GDPR | gdpr.ts | ✅ |
| Cosmetics | cosmetics.ts | ✅ |
| Map Tiles | map-api.ts | ✅ |
| World Seed | world-seed.ts | ✅ |

### C2. Unity Service Implementations 🔧 INTERFACES DEFINED

| Interface | File | Implementation Status |
|-----------|------|----------------------|
| `IBattleService` | ICloudFunctions.cs | ⏳ Need BattleService.cs |
| `IProtectionService` | ICloudFunctions.cs | ⏳ Need ProtectionService.cs |
| `IBlueprintService` | ICloudFunctions.cs | ⏳ Need BlueprintService.cs |
| `IAllianceWarService` | ICloudFunctions.cs | ⏳ Need AllianceWarService.cs |
| `ILocationService` | ICloudFunctions.cs | ⏳ Need LocationService.cs |

---

## PART D: STEP-BY-STEP INSTRUCTIONS

### D1. PC Client - Complete Setup (Unity Editor)

```
STEP 1: Create PC Scene
─────────────────────────────────────────────────────
1. Open Unity Editor with ApexCitadels project
2. File → New Scene
3. Save As: Assets/Scenes/PCMain.unity
4. Menu: Apex → PC → Setup PC Scene (Full)
5. Menu: Apex → PC → Create All PC Prefabs

STEP 2: Verify Scene Hierarchy
─────────────────────────────────────────────────────
After setup, you should have:
├── PCGameController (with PCGameController.cs)
├── Main Camera (with PCCameraController.cs)
├── InputManager (with PCInputManager.cs)
├── WorldMapRenderer (with WorldMapRenderer.cs)
├── BaseEditor (with BaseEditor.cs)
├── UIManager (with PCUIManager.cs)
├── WebGLBridge (with WebGLBridge.cs)
└── Canvas (with all UI panels)

STEP 3: Wire Up References
─────────────────────────────────────────────────────
Select PCGameController and assign:
- Camera Controller: Main Camera
- Input Manager: InputManager
- World Map Renderer: WorldMapRenderer
- Base Editor: BaseEditor
- UI Manager: UIManager

Select PCUIManager and assign panel prefabs:
- Territory Detail Panel
- Alliance Panel
- Build Menu Panel
- Statistics Panel
- Battle Replay Panel
- Crafting Panel
- Market Panel

STEP 4: Configure Build Settings
─────────────────────────────────────────────────────
1. File → Build Settings
2. Add Scene: Assets/Scenes/PCMain.unity
3. Switch Platform → WebGL
4. Player Settings:
   - Company Name: ApexCitadels
   - Product Name: Apex Citadels
   - Compression Format: Gzip
   - WebGL Memory Size: 512
   - Enable WebGL 2.0: ✓

STEP 5: Build WebGL
─────────────────────────────────────────────────────
1. File → Build Settings → Build
2. Select folder: [project]/backend/hosting-pc/build/
3. Wait for build (5-15 minutes)
4. Verify output:
   - build.data.gz
   - build.framework.js.gz
   - build.loader.js
   - build.wasm.gz

STEP 6: Deploy
─────────────────────────────────────────────────────
cd /workspaces/Apex/backend
firebase deploy --only hosting:pc

STEP 7: Test
─────────────────────────────────────────────────────
Open: https://apex-citadels-pc.web.app/build/
```

### D2. AR Mobile Client - Complete Setup (Unity Editor)

```
STEP 1: Create AR Scene (if not exists)
─────────────────────────────────────────────────────
1. Open Unity Editor with ApexCitadels project
2. File → New Scene
3. Save As: Assets/Scenes/ARMain.unity

STEP 2: Add AR Foundation Components
─────────────────────────────────────────────────────
1. GameObject → XR → AR Session
2. GameObject → XR → XR Origin (Mobile AR)
3. On XR Origin, add components:
   - AR Plane Manager
   - AR Raycast Manager
   - AR Anchor Manager

STEP 3: Create GameManager Object
─────────────────────────────────────────────────────
1. Create empty GameObject named "GameManager"
2. Add ALL these scripts:
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
   - ApexCitadels.Privacy.GDPRManager
   - ApexCitadels.Moderation.ContentModerationManager
   - ApexCitadels.Cosmetics.CosmeticsShopManager
   - ApexCitadels.Monitoring.PerformanceMonitor

STEP 4: Create EngagementSystems Object
─────────────────────────────────────────────────────
1. Create empty GameObject named "EngagementSystems"
2. Add these scripts:
   - ApexCitadels.WorldEvents.WorldEventManager
   - ApexCitadels.SeasonPass.SeasonPassManager
   - ApexCitadels.Social.FriendsManager
   - ApexCitadels.Chat.ChatManager
   - ApexCitadels.Referrals.ReferralManager
   - ApexCitadels.Analytics.AnalyticsManager
   - ApexCitadels.AntiCheat.AntiCheatManager

STEP 5: Create UI Canvas
─────────────────────────────────────────────────────
1. GameObject → UI → Canvas
2. Set Canvas Scaler to "Scale With Screen Size"
3. Add GameUIController.cs
4. Add GameHUDController.cs
5. Create HUD elements (see README.md for detailed layout)

STEP 6: Configure ARCore Geospatial API
─────────────────────────────────────────────────────
1. Window → XR → ARCore Extensions
2. Enable Geospatial
3. Add API key from Google Cloud Console
4. Add AREarthManager to XR Origin
5. Add ARGeospatialCreator for anchor placement

STEP 7: Configure Build Settings
─────────────────────────────────────────────────────
For Android:
1. File → Build Settings → Android
2. Player Settings:
   - Minimum API Level: 26 (Android 8)
   - Target API Level: 34
   - Scripting Backend: IL2CPP
   - ARM64 only
3. XR Plug-in Management:
   - Enable ARCore

For iOS:
1. File → Build Settings → iOS
2. Player Settings:
   - Target minimum iOS version: 14.0
   - Camera Usage Description: "AR features"
   - Location Usage Description: "Territory claiming"
3. XR Plug-in Management:
   - Enable ARKit

STEP 8: Build
─────────────────────────────────────────────────────
Android: Build → APK or AAB
iOS: Build → Xcode Project → Archive in Xcode
```

---

## QUICK REFERENCE

### Live URLs

| Service | URL | Status |
|---------|-----|--------|
| Admin Dashboard | https://apex-citadels-dev.web.app | ✅ Live |
| PC Client (Web) | https://apex-citadels-pc.web.app | ✅ Player view with map |
| PC Unity WebGL | https://apex-citadels-pc.web.app/build/ | 🔧 Deployed, needs rebuild for shader fix |
| Firebase Console | https://console.firebase.google.com/project/apex-citadels-dev | ✅ Live |

### Key File Locations

| Purpose | Path |
|---------|------|
| PC Scripts | unity/ApexCitadels/Assets/Scripts/PC/ |
| AR Scripts | unity/ApexCitadels/Assets/Scripts/AR/ |
| Core Scripts | unity/ApexCitadels/Assets/Scripts/Core/ |
| WebGL Bridge | unity/ApexCitadels/Assets/Scripts/PC/WebGL/ |
| JS Plugin | unity/ApexCitadels/Assets/Plugins/WebGL/WebGLBridge.jslib |
| Cloud Functions | backend/functions/src/ |
| Admin Dashboard | admin-dashboard/src/ |
| PC Hosting | backend/hosting-pc/ |

### Firebase Configuration

```javascript
{
  apiKey: "AIzaSyA7ljLJjxoq8VCqV1EGFpO5nhk56H0B6oo",
  projectId: "apex-citadels-dev",
  authDomain: "apex-citadels-dev.firebaseapp.com"
}
```

---

> **📋 IMPLEMENTATION CHECKLISTS:** See the **MASTER IMPLEMENTATION CHECKLIST** at the top of this document for detailed step-by-step instructions for both PC and AR Mobile platforms.

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
