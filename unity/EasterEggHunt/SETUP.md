# Easter Egg Hunt - Unity Project Setup

## 🥚 Game Concept
Players hide virtual Easter eggs in real-world locations using AR. Other players can then search for and collect these eggs to earn points.

## 📋 Setup Steps

### 1. Create Unity Project
1. Open Unity Hub
2. Click "New Project"
3. Select "3D (URP)" template
4. Name: "EasterEggHunt"
5. Location: This folder (`unity/EasterEggHunt`)

### 2. Copy Shared AR Scripts
Copy from `ApexCitadels/Assets/Scripts/`:
- `AR/SpatialAnchorManager.cs` → `Assets/Scripts/AR/`
- `Backend/AnchorPersistenceService.cs` → `Assets/Scripts/Backend/`
- `Config/AppConfig.cs` → `Assets/Scripts/Config/`

### 3. Install Packages
Window → Package Manager:
- AR Foundation 5.1.x
- ARCore XR Plugin 5.1.x
- ARKit XR Plugin 5.1.x
- Add: `com.google.ar.core.arfoundation.extensions`

### 4. Import Firebase SDK
- FirebaseAuth.unitypackage
- FirebaseFirestore.unitypackage
- Copy google-services.json to Assets/
- Copy GoogleService-Info.plist to Assets/

### 5. Create Main Scene
1. Create scene: `Assets/Scenes/EggHuntMain`
2. Add AR Session, XR Origin
3. Add EggHuntManager to scene
4. Create egg prefabs (colorful 3D eggs)
5. Wire up UI

### 6. Create Egg Prefabs
1. Create 3D egg shapes (or import models)
2. Add different materials (colors/patterns)
3. Add collider for tap detection
4. Save as prefabs in Assets/Prefabs/

## 🎮 Game Features
- [ ] Hide eggs in AR locations
- [ ] Find eggs hidden by others
- [ ] Collect eggs for points
- [ ] Leaderboard
- [ ] Seasonal events (Easter, Halloween, etc.)
- [ ] Special rare eggs with bonus points
