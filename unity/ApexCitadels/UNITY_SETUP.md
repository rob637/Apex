# Apex Citadels - Unity Configuration Guide

## Quick Start Checklist

Before opening Unity, ensure you have:
- [ ] Unity 6 (6000.x) or Unity 2022.3 LTS installed
- [ ] Android device with ARCore OR iOS device with ARKit
- [ ] Firebase account and project created
- [ ] Google Cloud account for ARCore Geospatial API

---

## Step 1: Open Project in Unity

1. Open Unity Hub
2. Click **Add** → Navigate to `unity/ApexCitadels`
3. Select Unity 6 as the editor version
4. Open the project (first import may take 5-10 minutes)

---

## Step 2: Install Required Packages

After opening, Unity will auto-import packages from `manifest.json`:
- ✅ AR Foundation 6.0.4
- ✅ ARCore XR Plugin 6.0.4
- ✅ ARKit XR Plugin 6.0.4
- ✅ ARCore Extensions 1.43.0 (for Geospatial)
- ✅ TextMeshPro 3.0.6
- ✅ Input System 1.7.0

If any package fails to import:
1. Window → Package Manager
2. Click + → Add package by name
3. Enter the package name (e.g., `com.google.ar.core.arfoundation.extensions`)

---

## Step 3: Import Firebase Unity SDK

**Download:** https://firebase.google.com/docs/unity/setup

Required packages (import in order):
1. `FirebaseAuth.unitypackage`
2. `FirebaseFirestore.unitypackage`
3. `FirebaseFunctions.unitypackage`

After importing:
1. Copy `google-services.json` (Android) to `Assets/`
2. Copy `GoogleService-Info.plist` (iOS) to `Assets/`
3. Add `FIREBASE_ENABLED` to Player Settings → Scripting Define Symbols

---

## Step 4: ARCore Geospatial API Setup

### 4.1 API Key Configuration

After creating your API key in Google Cloud Console, you need to add it to Unity:

#### For Android:

1. In Unity, go to **Edit → Project Settings → XR Plug-in Management → ARCore Extensions**
2. Check **"Geospatial"** under Optional Features
3. Paste your API key in the **"Android API Key"** field

#### For iOS:

1. In Unity, go to **Edit → Project Settings → XR Plug-in Management → ARCore Extensions**
2. Check **"Geospatial"** under Optional Features  
3. Paste your API key in the **"iOS API Key"** field

### 4.2 Update AppConfig.json

Edit `Assets/Resources/AppConfig.json`:
```json
{
  "arcore": {
    "geospatialApiKey": "YOUR_API_KEY_HERE",
    "geospatialEnabled": true,
    "cloudAnchorsEnabled": true
  }
}
```

---

## Step 5: Configure Build Settings

### Android Build Settings:
1. File → Build Settings → Switch Platform to Android
2. Player Settings:
   - Minimum API Level: **24** (Android 7.0)
   - Target API Level: **34** or higher
   - Scripting Backend: **IL2CPP**
   - Target Architectures: **ARM64** only
   - Internet Access: **Required**

### iOS Build Settings:
1. File → Build Settings → Switch Platform to iOS
2. Player Settings:
   - Target minimum iOS Version: **12.0**
   - Architecture: **ARM64**
   - Camera Usage Description: "Required for AR features"
   - Location Usage Description: "Required for finding nearby citadels"

---

## Step 6: Configure XR Settings

1. Edit → Project Settings → XR Plug-in Management
2. **Android tab:** 
   - [x] ARCore
3. **iOS tab:**
   - [x] ARKit
4. **ARCore Extensions:**
   - [x] Geospatial
   - Paste API key

---

## Step 7: Scene Setup

The project includes pre-configured scenes in `Assets/Scenes/`:

| Scene | Purpose |
|-------|---------|
| Bootstrap | Initial loading, Firebase init |
| ARGameplay | Main AR gameplay scene |
| MapView | 2D map with territory overlay |

### ARGameplay Scene Hierarchy:
```
AR Session
└── AR Session (Component)

XR Origin
├── AR Camera
│   └── AR Camera Manager
│   └── AR Pose Driver
├── AR Raycast Manager
├── AR Plane Manager
└── AR Anchor Manager

Managers
├── ServiceLocator
├── SpatialAnchorManager
└── AnchorPersistenceService

Canvas (UI)
├── StatusText
├── PlaceCubeButton
└── LoadCubesButton

EventSystem
```

---

## Step 8: Build & Test

### Android:
1. Connect Android device (USB debugging enabled)
2. File → Build Settings → Build and Run
3. Grant camera and location permissions when prompted

### iOS:
1. Build → Open in Xcode
2. Configure signing
3. Run on device

---

## Testing Without VPS

If you're in an area without Google VPS coverage, the system will fall back to:
1. GPS-only positioning (less accurate, ~5m)
2. Local anchors (device-only, not persistent)

To check VPS coverage in your area, use:
- Google's Geospatial API Coverage Map
- Or enable debug logging in SpatialAnchorManager

---

## Troubleshooting

### "Tracking not ready"
- Move your phone slowly to scan the environment
- Ensure good lighting
- Point at textured surfaces (not blank walls)

### "Anchor resolution failed"
- Check internet connectivity
- Verify API key is correct
- Ensure ARCore/ARKit is up to date on device

### "GPS inaccurate"
- Go outside for better GPS signal
- Wait 30 seconds for GPS to stabilize
- Ensure location permissions are granted
