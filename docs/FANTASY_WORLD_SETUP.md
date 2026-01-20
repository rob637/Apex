# Fantasy World Setup Guide - Synty Asset Wiring

This guide walks you through setting up the Fantasy World system with your Synty POLYGON assets.

## Step 1: Create the Fantasy Prefab Library

1. **Right-click** in your Project window
2. Navigate to: `Create > Apex Citadels > Fantasy Prefab Library`
3. Name it `MainFantasyPrefabLibrary`
4. This creates a ScriptableObject that acts as a central registry for all your prefabs

## Step 2: Populate the Prefab Library

Open the `MainFantasyPrefabLibrary` in the Inspector and drag prefabs from your Synty packs:

### BUILDINGS

| Library Slot | Synty Pack | Suggested Prefabs |
|-------------|------------|-------------------|
| **Peasant Huts** | Fantasy Kingdom, Town | SM_Bld_Peasant_Hut_*, Small huts |
| **Cottages** | Fantasy Kingdom, Farm | SM_Bld_House_Small_*, Cottage_* |
| **Small Houses** | Town Pack, Fantasy Kingdom | SM_Bld_House_*, SM_Bld_Town_House_* |
| **Houses** | Fantasy Kingdom, Town Pack | SM_Bld_House_Medium_*, Various houses |
| **Town Houses** | Town Pack | SM_Bld_Townhouse_* |
| **Manors** | Fantasy Kingdom | SM_Bld_Manor_*, SM_Bld_Noble_House_* |
| **Noble Estates** | Fantasy Kingdom | SM_Bld_Castle_*, Large manor buildings |
| **Market Stalls** | Fantasy Kingdom, Town | SM_Prop_Market_Stall_* |
| **Taverns** | Fantasy Kingdom, Town | SM_Bld_Tavern_*, Inn buildings |
| **Inns** | Fantasy Kingdom | SM_Bld_Inn_* |
| **Blacksmiths** | Fantasy Kingdom | SM_Bld_Blacksmith_*, Forge buildings |
| **Bakeries** | Town Pack | SM_Bld_Shop_*, Bakery buildings |
| **General Stores** | Town Pack | SM_Bld_Shop_* |
| **Shops** | Town Pack, Fantasy Kingdom | All shop prefabs |
| **Guard Towers** | Fantasy Kingdom | SM_Bld_Tower_*, SM_Bld_Guard_Tower_* |
| **Barracks** | Fantasy Kingdom | SM_Bld_Barracks_* |
| **Fortresses** | Fantasy Kingdom | SM_Bld_Fortress_*, Castle components |
| **Castle Parts** | Fantasy Kingdom | SM_Bld_Castle_*, Walls, towers, gates |
| **Walls** | Fantasy Kingdom | SM_Env_Wall_*, SM_Bld_Wall_* |
| **Gates** | Fantasy Kingdom | SM_Bld_Gate_*, SM_Env_Gate_* |
| **Chapels** | Fantasy Kingdom | SM_Bld_Chapel_* |
| **Churches** | Fantasy Kingdom | SM_Bld_Church_* |
| **Cathedrals** | Fantasy Kingdom | SM_Bld_Cathedral_*, Large churches |
| **Town Halls** | Town Pack | SM_Bld_TownHall_*, Civic buildings |
| **Mills** | Farm, Fantasy Kingdom | SM_Bld_Mill_*, Windmill |
| **Warehouses** | Town Pack | SM_Bld_Warehouse_*, Large storage |
| **Workshops** | Fantasy Kingdom, Town | SM_Bld_Workshop_* |
| **Barns** | Farm | SM_Bld_Barn_*, Farm buildings |
| **Farmhouses** | Farm | SM_Bld_Farmhouse_* |
| **Silos** | Farm | SM_Bld_Silo_* |
| **Mage Towers** | Fantasy Kingdom | SM_Bld_MageTower_*, Wizard towers |
| **Ruins** | Dungeon Pack | SM_Env_Ruins_*, Destroyed buildings |
| **Monuments** | Fantasy Kingdom | SM_Prop_Statue_*, Monuments |
| **Fountains** | Fantasy Kingdom, Town | SM_Prop_Fountain_* |
| **Wells** | Fantasy Kingdom, Farm | SM_Prop_Well_* |

### NATURE (from POLYGON Nature pack)

| Library Slot | Prefabs to Use |
|-------------|----------------|
| **Trees Oak** | SM_Env_Tree_Oak_* |
| **Trees Pine** | SM_Env_Tree_Pine_* |
| **Trees Willow** | SM_Env_Tree_Willow_* |
| **Trees Fantasy** | SM_Env_Tree_Fantasy_*, any magical looking trees |
| **Trees Dead** | SM_Env_Tree_Dead_* |
| **Bushes Small** | SM_Env_Bush_Small_* |
| **Bushes Large** | SM_Env_Bush_Large_* |
| **Bushes Flower** | SM_Env_Bush_Flower_* |
| **Flower Patches** | SM_Env_Flowers_* |
| **Grass Clumps** | SM_Env_Grass_* |
| **Rocks Small** | SM_Env_Rock_Small_* |
| **Rocks Medium** | SM_Env_Rock_Medium_* |
| **Rocks Large** | SM_Env_Rock_Large_* |
| **Boulders** | SM_Env_Boulder_* |
| **Logs** | SM_Env_Log_* |
| **Mushrooms** | SM_Env_Mushroom_* |
| **Stumps** | SM_Env_Stump_* |

### PROPS

| Library Slot | Pack | Prefabs |
|-------------|------|---------|
| **Barrels** | Multiple | SM_Prop_Barrel_* |
| **Crates** | Multiple | SM_Prop_Crate_* |
| **Sacks** | Farm, Fantasy Kingdom | SM_Prop_Sack_* |
| **Chests** | Dungeon Pack | SM_Prop_Chest_* |
| **Carts** | Farm, Fantasy Kingdom | SM_Prop_Cart_* |
| **Wagons** | Fantasy Kingdom | SM_Prop_Wagon_* |
| **Benches** | Town Pack | SM_Prop_Bench_* |
| **Tables** | Multiple | SM_Prop_Table_* |
| **Chairs** | Multiple | SM_Prop_Chair_* |
| **Signs** | Multiple | SM_Prop_Sign_* |
| **Lanterns** | Fantasy Kingdom | SM_Prop_Lantern_* |
| **Torches** | Dungeon Pack | SM_Prop_Torch_* |
| **Braziers** | Fantasy Kingdom | SM_Prop_Brazier_* |
| **Campfires** | Adventure | SM_Prop_Campfire_* |
| **Fence Wood** | Farm, Fantasy Kingdom | SM_Env_Fence_* |
| **Fence Stone** | Fantasy Kingdom | SM_Env_Wall_Stone_* |
| **Hedges** | Nature | SM_Env_Hedge_* |
| **Flags** | Fantasy Kingdom | SM_Prop_Flag_* |
| **Banners** | Fantasy Kingdom | SM_Prop_Banner_* |
| **Statues** | Fantasy Kingdom | SM_Prop_Statue_* |
| **Scarecrows** | Farm | SM_Prop_Scarecrow_* |
| **Haystacks** | Farm | SM_Prop_Haystack_* |

### CHARACTERS (Optional - for ambient life)

| Library Slot | Pack | Prefabs |
|-------------|------|---------|
| **Peasants** | Fantasy Characters, Modular Fantasy Hero | Common clothing characters |
| **Merchants** | Town Pack, Fantasy Characters | Dressed merchants |
| **Guards** | Knights | Armored soldiers |
| **Knights** | Knights | Full armor characters |
| **Nobles** | Fantasy Characters | Fancy dressed characters |

### ANIMALS (Optional)

| Library Slot | Pack | Prefabs |
|-------------|------|---------|
| **Chickens** | Farm | SM_Prop_Chicken_* or animated |
| **Pigs** | Farm | SM_Prop_Pig_* |
| **Cows** | Farm | SM_Prop_Cow_* |
| **Horses** | Farm, Knights | SM_Prop_Horse_* |

## Step 3: Set Up the Scene

1. Create an empty GameObject called `FantasyWorld`
2. Add the `FantasyWorldDemo` component to it
3. In the Inspector:
   - Drag your `MainFantasyPrefabLibrary` to the **Prefab Library** slot
   - Set your **Latitude/Longitude** (Vienna, VA Mashie Drive area is preset)
   - Set **Radius** (start with 200-300 meters for testing)

## Step 4: Quick Prefab Population Script

For faster setup, you can use this Editor script. Save it in `Assets/Editor/`:

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class PrefabLibraryPopulator : EditorWindow
{
    [MenuItem("Apex Citadels/Populate Prefab Library")]
    public static void ShowWindow()
    {
        GetWindow<PrefabLibraryPopulator>("Prefab Populator");
    }

    private void OnGUI()
    {
        GUILayout.Label("This scans your Synty folders and populates the library", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Auto-Populate from Synty Assets"))
        {
            AutoPopulate();
        }
    }

    private void AutoPopulate()
    {
        // Find all Synty prefabs and categorize them
        // This is a starting point - customize as needed
        
        string[] searchFolders = new string[] { "Assets" };
        string[] guids = AssetDatabase.FindAssets("SM_Bld t:Prefab", searchFolders);
        
        Debug.Log($"Found {guids.Length} building prefabs");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            
            // Categorize based on name
            if (name.Contains("House")) Debug.Log($"House: {name}");
            if (name.Contains("Castle")) Debug.Log($"Castle: {name}");
            if (name.Contains("Tower")) Debug.Log($"Tower: {name}");
            // ... etc
        }
    }
}
```

## Step 5: Test Generation

1. Enter **Play Mode**
2. You should see a UI in the top-left corner
3. Click **Generate** to create the fantasy world
4. The system will:
   - Fetch real OSM building data from OpenStreetMap
   - Convert each building to a fantasy equivalent
   - Spawn trees, bushes, and props

## Troubleshooting

### "No prefab found for X"
- The library slot for that building type is empty
- Add more prefab variations to cover edge cases

### Buildings look wrong scale
- Adjust `Building Scale Multiplier` in the config
- Individual prefabs may need scale adjustment

### Too few/many trees
- Adjust `Tree Density` and `Bush Density` (0-1 range)

### Performance issues
- Reduce radius
- Lower `Max Buildings Per Frame`
- Enable LOD system (coming soon)

## Your Downloaded Packs

Based on what you downloaded, here's where each fits:

| Pack | Primary Use |
|------|-------------|
| Fantasy Kingdom | Buildings (castles, houses, towers, churches) |
| Nature | Trees, bushes, rocks, flowers |
| Town Pack | Town buildings, shops, houses |
| Fantasy Rivals | Character variations, props |
| Farm | Barns, farmhouses, animals, rural props |
| Fantasy Characters | Peasants, merchants, nobles |
| Knights | Guards, knights, military |
| Icons | UI elements (not for 3D) |
| Particle FX | Magic effects, fire, smoke |
| Vikings | Viking buildings, longhouses |
| Samurai | Asian-style buildings |
| Pirates | Docks, ships, coastal buildings |
| Adventure | Camps, exploration props |
| Dungeon Pack | Ruins, underground props |
| Modular Fantasy Hero | Character customization |

## Next Steps

After basic setup works:
1. Add street-level camera for walking through
2. Enable LOD system for better performance
3. Add ambient NPCs walking around
4. Add day/night cycle
5. Add particle effects (smoke from chimneys, etc.)
