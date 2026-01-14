# Virtual Geocaching - Unity Project Setup

## 📍 Game Concept
A digital version of geocaching where players hide and find virtual caches in real-world locations using AR. Each cache has a logbook that finders can sign.

## 📋 Setup Steps

### 1. Create Unity Project
1. Open Unity Hub
2. Click "New Project"
3. Select "3D (URP)" template
4. Name: "VirtualGeocaching"
5. Location: This folder (`unity/VirtualGeocaching`)

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
1. Create scene: `Assets/Scenes/GeocachingMain`
2. Add AR Session, XR Origin
3. Add GeocacheManager to scene
4. Create cache container prefabs
5. Wire up UI

### 6. Create Cache Prefabs
1. Small cache (film canister style)
2. Medium cache (small box)
3. Large cache (ammo can style)
4. Add materials and colliders
5. Save as prefabs

## 🎮 Game Features
- [ ] Hide caches with name, hint, difficulty
- [ ] Find caches using AR
- [ ] Sign virtual logbook
- [ ] View cache history and finders
- [ ] Rate cache difficulty/terrain
- [ ] Trackable items (travel bugs)
- [ ] Achievement badges
- [ ] Cache maintenance notifications

## 📊 Cache Properties
- Name & Description
- Hint (shown when nearby)
- Size: Small, Medium, Large
- Difficulty: 1-5 stars
- Terrain: 1-5 stars
- Hidden date
- Logbook entries
