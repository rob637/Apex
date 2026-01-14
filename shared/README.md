# Shared AR Components

This folder contains shared scripts that can be used across all AR game projects.

## 📁 Contents

### AR/
- **SharedSpatialAnchorManager.cs** - Handles AR raycasting and anchor creation

### Backend/
- **SharedAnchorPersistenceService.cs** - Saves/loads AR objects to Firebase cloud

## 🔧 How to Use

### For Each New Unity Project:

1. **Copy the scripts** from this folder to your Unity project:
   ```
   shared/AR/SpatialAnchorManager.cs → Assets/Scripts/AR/
   shared/Backend/AnchorPersistenceService.cs → Assets/Scripts/Backend/
   ```

2. **Update the namespace** at the top of each file to match your project:
   ```csharp
   // Change from:
   namespace Shared.AR
   
   // To your project namespace:
   namespace EasterEggHunt.AR
   // or
   namespace VirtualGeocaching.AR
   ```

3. **Configure the components** in Unity Editor with appropriate references

## 🎮 Projects Using These Components

| Project | Folder | Description |
|---------|--------|-------------|
| Apex Citadels | `unity/ApexCitadels/` | AR territory control game |
| Easter Egg Hunt | `unity/EasterEggHunt/` | Hide and find virtual eggs |
| Virtual Geocaching | `unity/VirtualGeocaching/` | Digital geocaching experience |

## 🔄 Updating Shared Code

When you improve a shared component:

1. Update the source file in this `shared/` folder
2. Copy the updated file to each Unity project that uses it
3. Test in each project

> **Note:** Unity doesn't support shared code across projects without Package Manager. 
> These files must be copied to each project manually.
