# Apex Citadels - Unity Configuration Guide

## ARCore Geospatial API Setup

### 1. API Key Configuration

After creating your API key in Google Cloud Console, you need to add it to Unity:

#### For Android:

1. In Unity, go to **Edit → Project Settings → XR Plug-in Management → ARCore Extensions**
2. Check **"Geospatial"** under Optional Features
3. Paste your API key in the **"Android API Key"** field

#### For iOS:

1. In Unity, go to **Edit → Project Settings → XR Plug-in Management → ARCore Extensions**
2. Check **"Geospatial"** under Optional Features  
3. Paste your API key in the **"iOS API Key"** field

### 2. Required Unity Packages

Install these packages via Package Manager (Window → Package Manager):

```
AR Foundation: 5.1.0 or later
ARCore XR Plugin: 5.1.0 or later
ARKit XR Plugin: 5.1.0 or later (for iOS)
Google ARCore Extensions for Unity: 1.38.0 or later
```

### 3. Firebase Unity SDK

Download from: https://firebase.google.com/docs/unity/setup

Required packages:
- FirebaseAuth.unitypackage
- FirebaseFirestore.unitypackage

After importing, add the `google-services.json` (Android) or `GoogleService-Info.plist` (iOS) to your Assets folder.

### 4. Project Settings

#### Android Build Settings:
- Minimum API Level: 24 (Android 7.0)
- Target API Level: 33 or higher
- Scripting Backend: IL2CPP
- Target Architectures: ARM64

#### iOS Build Settings:
- Target minimum iOS Version: 12.0
- Architecture: ARM64
- Camera Usage Description: "Required for AR features"
- Location Usage Description: "Required for finding nearby citadels"

### 5. Scene Setup

Your AR scene needs these components:

```
Main Camera
└── AR Camera Manager
└── AR Pose Driver

AR Session
└── AR Session (Component)

AR Session Origin
├── AR Camera
├── AR Raycast Manager
├── AR Anchor Manager
├── AR Plane Manager
└── AREarthManager (for Geospatial)

Managers (Empty GameObject)
├── SpatialAnchorManager (our script)
├── AnchorPersistenceService (our script)
└── PersistentCubeDemo (our script)

UI Canvas
├── Status Text
├── Tracking Status Text
├── Place Cube Button
├── Load Cubes Button
└── Clear Cubes Button
```

## Testing Without VPS

If you're in an area without Google VPS coverage, the system will fall back to:
1. GPS-only positioning (less accurate, ~5m)
2. Local anchors (device-only, not persistent)

To check VPS coverage in your area, use:
- Google's Geospatial API Coverage Map
- Or enable debug logging in SpatialAnchorManager

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
