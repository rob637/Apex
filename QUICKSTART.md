# Apex Citadels - Quick Start Guide

## 🚀 Getting Started (5 Steps)

This guide walks you through setting up and running the Apex Citadels prototype.

---

## ✅ Step 1: Prerequisites

Ensure you have:
- [ ] **Unity 2022.3 LTS** or newer installed
- [ ] **Android device** with ARCore support, OR **iOS device** with ARKit
- [ ] **USB cable** to connect device to computer
- [ ] **Node.js 18+** installed

---

## ✅ Step 2: Firebase Setup (COMPLETED IN CODESPACES)

The following has been configured:
- ✅ Firebase project created: `apex-citadels-dev`
- ✅ Cloud Functions written and ready
- ✅ Firestore rules configured
- ✅ Database indexes defined

### Remaining Manual Steps:

1. **Enable Firestore Database:**
   - Go to: https://console.firebase.google.com/project/apex-citadels-dev/firestore
   - Click "Create database"
   - Select "Start in test mode"
   - Location: us-central1
   - Click "Enable"

2. **Register Mobile Apps:**
   - Go to: https://console.firebase.google.com/project/apex-citadels-dev/settings/general
   - Click Android icon → Package: `com.apexcitadels.game` → Download `google-services.json`
   - Click iOS icon → Bundle ID: `com.apexcitadels.game` → Download `GoogleService-Info.plist`

3. **Deploy Backend:**
   ```bash
   cd backend
   firebase login  # if not already logged in
   firebase deploy --only firestore:rules,firestore:indexes
   firebase deploy --only functions
   ```

---

## ✅ Step 3: ARCore Geospatial API Setup

1. **Enable ARCore API:**
   - Go to: https://console.cloud.google.com/apis/library/arcore.googleapis.com?project=apex-citadels-dev
   - Click "Enable"

2. **Create API Key:**
   - Go to: https://console.cloud.google.com/apis/credentials?project=apex-citadels-dev
   - Click "+ CREATE CREDENTIALS" → "API key"
   - Copy the key
   - Click "Edit API key":
     - Name: "Apex Citadels AR Key"
     - Restrict to: ARCore API
     - Save

3. **Add API Key to Unity:**
   - Open `unity/ApexCitadels/Assets/Resources/AppConfig.json`
   - Replace `PASTE_YOUR_API_KEY_HERE` with your API key

---

## ✅ Step 4: Unity Project Setup

### 4.1 Open Project
1. Open Unity Hub
2. Click "Add" → Navigate to `unity/ApexCitadels`
3. Open project (Unity will import packages automatically)

### 4.2 Install Required Packages
In Unity: Window → Package Manager
- AR Foundation 5.1.x ✓
- ARCore XR Plugin 5.1.x ✓
- ARKit XR Plugin 5.1.x ✓

For ARCore Extensions (Geospatial):
- Window → Package Manager → + → Add package by name
- Enter: `com.google.ar.core.arfoundation.extensions`

### 4.3 Import Firebase SDK
1. Download Firebase Unity SDK: https://firebase.google.com/docs/unity/setup
2. Import: `FirebaseAuth.unitypackage`
3. Import: `FirebaseFirestore.unitypackage`
4. Copy `google-services.json` to `Assets/`
5. Copy `GoogleService-Info.plist` to `Assets/`

### 4.4 Configure XR Settings
1. Edit → Project Settings → XR Plug-in Management
2. Android tab: Enable "ARCore"
3. iOS tab: Enable "ARKit"
4. ARCore Extensions: Enable "Geospatial" and paste API key

### 4.5 Create Demo Scene
1. Create new scene: Assets → Create → Scene → "PersistentCubeDemo"
2. Add AR Session Origin with:
   - AR Camera
   - AR Raycast Manager
   - AR Anchor Manager
   - AR Plane Manager
   - AREarthManager
3. Add empty GameObject "Managers" with:
   - SpatialAnchorManager component
   - AnchorPersistenceService component
   - PersistentCubeDemo component
4. Create UI Canvas with buttons and text

---

## ✅ Step 5: Build & Test

### Android Build:
1. File → Build Settings
2. Switch Platform to Android
3. Player Settings:
   - Minimum API Level: 24
   - Target API Level: 34
   - Scripting Backend: IL2CPP
   - Target Architectures: ARM64
4. Connect Android device (USB debugging enabled)
5. Build and Run

### iOS Build:
1. File → Build Settings
2. Switch Platform to iOS
3. Player Settings:
   - Target minimum iOS Version: 12.0
   - Architecture: ARM64
4. Build → Open in Xcode → Run on device

---

## 🧪 Running the Persistent Cube Test

### Test Procedure:
1. **Device A**: Open app, wait for "Ready" status
2. **Device A**: Tap "Place Cube" → Tap on a surface
3. **Device A**: Verify "Cube placed and saved to cloud!" message
4. **Device A**: Close app
5. **Device B**: Open app in same location
6. **Device B**: Tap "Load Cubes"
7. **Device B**: Verify the cube appears in the same location!

### Success Criteria:
- ✅ Cube appears within 10cm of original placement
- ✅ Cube orientation matches
- ✅ Cube persists after app restart
- ✅ Cube visible on different device

---

## 🐛 Troubleshooting

### "Tracking not ready"
- Move phone slowly to scan environment
- Ensure good lighting
- Point at textured surfaces

### "No cubes found nearby"
- Verify GPS is enabled
- Check you're within 100m of placed cube
- Ensure internet connectivity

### Build Errors
- Verify all packages imported
- Check Firebase SDK compatible with Unity version
- Ensure google-services.json in Assets folder

### ARCore Not Working
- Verify device supports ARCore
- Update Google Play Services for AR
- Check API key is correct

---

## 📁 Project Structure

```
Apex/
├── backend/
│   ├── functions/src/index.ts    # Cloud Functions
│   ├── firestore.rules           # Security rules
│   └── firebase.json             # Firebase config
│
├── unity/ApexCitadels/
│   ├── Assets/
│   │   ├── Scripts/
│   │   │   ├── AR/SpatialAnchorManager.cs
│   │   │   ├── Backend/AnchorPersistenceService.cs
│   │   │   ├── Config/AppConfig.cs
│   │   │   └── Demo/PersistentCubeDemo.cs
│   │   └── Resources/AppConfig.json
│   └── Packages/manifest.json
│
└── docs/
    ├── VISION.md
    ├── TECHNICAL_ARCHITECTURE.md
    └── ROADMAP.md
```

---

## 🎯 Next Steps After Cube Test

Once the cube test passes:
1. Implement resource harvesting system
2. Add basic building mechanics
3. Create multiplayer sync
4. Build battle system
5. Polish and iterate!

---

*Need help? Check the docs folder for detailed architecture and roadmap.*
