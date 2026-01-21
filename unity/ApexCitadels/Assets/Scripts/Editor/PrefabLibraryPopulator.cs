// ============================================================================
// APEX CITADELS - PREFAB LIBRARY POPULATOR (EDITOR)
// Auto-populates the FantasyPrefabLibrary with Synty prefabs
// ============================================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Editor utility to automatically populate the FantasyPrefabLibrary
    /// with appropriate Synty prefabs from the project.
    /// </summary>
    public class PrefabLibraryPopulator : EditorWindow
    {
        private FantasyPrefabLibrary targetLibrary;
        private bool populateBuildings = true;
        private bool populatePaths = true;
        private bool populateTrees = true;
        private bool populateBushes = true;
        private bool populateProps = true;
        private Vector2 scrollPos;
        
        [MenuItem("Apex Citadels/Populate Prefab Library")]
        public static void ShowWindow()
        {
            GetWindow<PrefabLibraryPopulator>("Prefab Populator");
        }
        
        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            GUILayout.Label("Fantasy Prefab Library Populator", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            targetLibrary = (FantasyPrefabLibrary)EditorGUILayout.ObjectField(
                "Target Library", targetLibrary, typeof(FantasyPrefabLibrary), false);
            
            if (targetLibrary == null)
            {
                // Try to find the main library
                var guids = AssetDatabase.FindAssets("t:FantasyPrefabLibrary");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    targetLibrary = AssetDatabase.LoadAssetAtPath<FantasyPrefabLibrary>(path);
                }
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Categories to Populate:", EditorStyles.boldLabel);
            
            populateBuildings = EditorGUILayout.Toggle("Buildings (Houses, Shops, etc.)", populateBuildings);
            populatePaths = EditorGUILayout.Toggle("Cobblestone Paths", populatePaths);
            populateTrees = EditorGUILayout.Toggle("Trees", populateTrees);
            populateBushes = EditorGUILayout.Toggle("Bushes/Vegetation", populateBushes);
            populateProps = EditorGUILayout.Toggle("Props (Barrels, etc.)", populateProps);
            
            GUILayout.Space(20);
            
            EditorGUILayout.HelpBox("This will search Synty folders and populate the prefab arrays.", MessageType.Info);
            
            if (GUILayout.Button("POPULATE ALL", GUILayout.Height(50)))
            {
                if (targetLibrary != null)
                {
                    PopulateAll();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please select a target FantasyPrefabLibrary asset.", "OK");
                }
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Populate Selected Categories", GUILayout.Height(30)))
            {
                if (targetLibrary != null)
                {
                    PopulateLibrary();
                }
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Populate Buildings Only", GUILayout.Height(25)))
            {
                if (targetLibrary != null)
                {
                    PopulateBuildingsOnly();
                }
            }
            
            if (GUILayout.Button("Populate Paths Only", GUILayout.Height(25)))
            {
                if (targetLibrary != null)
                {
                    PopulatePathsOnly();
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void PopulateAll()
        {
            Undo.RecordObject(targetLibrary, "Populate All Prefabs");
            
            int totalAdded = 0;
            totalAdded += PopulateAllBuildings();
            totalAdded += PopulateCobblestoneSegments();
            totalAdded += PopulateDirtPaths();
            totalAdded += PopulateTrees();
            totalAdded += PopulateBushes();
            totalAdded += PopulateProps();
            
            EditorUtility.SetDirty(targetLibrary);
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("Complete", $"Added {totalAdded} prefabs to the library.", "OK");
        }
        
        private void PopulateLibrary()
        {
            Undo.RecordObject(targetLibrary, "Populate Prefab Library");
            
            int totalAdded = 0;
            
            if (populateBuildings) totalAdded += PopulateAllBuildings();
            if (populatePaths)
            {
                totalAdded += PopulateCobblestoneSegments();
                totalAdded += PopulateDirtPaths();
            }
            if (populateTrees) totalAdded += PopulateTrees();
            if (populateBushes) totalAdded += PopulateBushes();
            if (populateProps) totalAdded += PopulateProps();
            
            EditorUtility.SetDirty(targetLibrary);
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("Complete", $"Added {totalAdded} prefabs to the library.", "OK");
        }
        
        private void PopulateBuildingsOnly()
        {
            Undo.RecordObject(targetLibrary, "Populate Buildings");
            
            int count = PopulateAllBuildings();
            
            EditorUtility.SetDirty(targetLibrary);
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("Complete", $"Added {count} building prefabs to the library.", "OK");
        }
        
        private void PopulatePathsOnly()
        {
            Undo.RecordObject(targetLibrary, "Populate Paths");
            
            int count = PopulateCobblestoneSegments();
            count += PopulateDirtPaths();
            
            EditorUtility.SetDirty(targetLibrary);
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("Complete", $"Added {count} path prefabs to the library.", "OK");
        }
        
        // =====================================================================
        // BUILDINGS - Search ALL Synty packs for building variety
        // =====================================================================
        private int PopulateAllBuildings()
        {
            int count = 0;
            
            Debug.Log("[PrefabPopulator] Searching all Synty packs for buildings...");
            
            // Houses - search all packs for variety
            var allHouses = FindBuildingsAcrossAllPacks("House", "Cottage", "Home", "Hut", "Cabin");
            if (allHouses.Length > 0)
            {
                // Split into small, medium, large based on naming
                var smallHouses = allHouses.Where(p => 
                    p.name.Contains("01") || p.name.Contains("02") || p.name.Contains("03") ||
                    p.name.Contains("Small") || p.name.Contains("Cottage") || p.name.Contains("Hut")).ToArray();
                var mediumHouses = allHouses.Where(p => 
                    p.name.Contains("04") || p.name.Contains("05") || p.name.Contains("06") ||
                    (!smallHouses.Contains(p) && !p.name.Contains("Large") && !p.name.Contains("Manor"))).ToArray();
                var largeHouses = allHouses.Where(p => 
                    p.name.Contains("07") || p.name.Contains("08") || p.name.Contains("09") || p.name.Contains("10") ||
                    p.name.Contains("Large") || p.name.Contains("Manor")).ToArray();
                
                // Ensure no overlap and all houses are assigned
                if (mediumHouses.Length == 0 && smallHouses.Length > 3) 
                {
                    mediumHouses = smallHouses.Skip(smallHouses.Length / 2).ToArray();
                    smallHouses = smallHouses.Take(smallHouses.Length / 2).ToArray();
                }
                
                if (smallHouses.Length > 0) { targetLibrary.cottages = smallHouses; count += smallHouses.Length; }
                if (mediumHouses.Length > 0) { targetLibrary.houses = mediumHouses; targetLibrary.smallHouses = mediumHouses; count += mediumHouses.Length; }
                if (largeHouses.Length > 0) { targetLibrary.townHouses = largeHouses; targetLibrary.manors = largeHouses; count += largeHouses.Length; }
                
                Debug.Log($"[PrefabPopulator] Houses: {smallHouses.Length} small, {mediumHouses.Length} medium, {largeHouses.Length} large (total {allHouses.Length})");
            }
            
            // Churches/Temples
            var churches = FindBuildingsAcrossAllPacks("Church", "Chapel", "Temple", "Shrine", "Cathedral");
            if (churches.Length > 0)
            {
                targetLibrary.churches = churches;
                targetLibrary.chapels = churches.Where(p => p.name.Contains("Chapel") || p.name.Contains("Small")).ToArray();
                if (targetLibrary.chapels.Length == 0) targetLibrary.chapels = churches.Take(2).ToArray();
                count += churches.Length;
                Debug.Log($"[PrefabPopulator] Added {churches.Length} churches/temples");
            }
            
            // Blacksmith/Forge
            var blacksmiths = FindBuildingsAcrossAllPacks("Blacksmith", "Forge", "Smithy", "Armorer");
            if (blacksmiths.Length > 0)
            {
                targetLibrary.blacksmiths = blacksmiths;
                count += blacksmiths.Length;
                Debug.Log($"[PrefabPopulator] Added {blacksmiths.Length} blacksmiths");
            }
            
            // Tavern/Inn/Pub
            var taverns = FindBuildingsAcrossAllPacks("Tavern", "Inn", "Pub", "Alehouse");
            if (taverns.Length > 0)
            {
                targetLibrary.taverns = taverns;
                targetLibrary.inns = taverns;
                count += taverns.Length;
                Debug.Log($"[PrefabPopulator] Added {taverns.Length} taverns/inns");
            }
            
            // Shops/Stores
            var shops = FindBuildingsAcrossAllPacks("Shop", "Store", "Market", "Merchant", "Trade");
            if (shops.Length > 0)
            {
                targetLibrary.shops = shops;
                targetLibrary.generalStores = shops.Take(Mathf.Min(3, shops.Length)).ToArray();
                count += shops.Length;
                Debug.Log($"[PrefabPopulator] Added {shops.Length} shops");
            }
            else if (allHouses.Length > 2)
            {
                // Use houses as shops fallback
                targetLibrary.shops = allHouses.Take(3).ToArray();
                targetLibrary.generalStores = allHouses.Take(2).ToArray();
                Debug.Log("[PrefabPopulator] Using houses as shops (fallback)");
            }
            
            // Guard towers/Watchtowers
            var towers = FindBuildingsAcrossAllPacks("Tower", "Watchtower", "Guard_Tower", "Lookout");
            if (towers.Length > 0)
            {
                // Filter out wall towers (those are in walls category)
                var guardTowers = towers.Where(p => !p.name.Contains("Wall")).ToArray();
                targetLibrary.guardTowers = guardTowers.Length > 0 ? guardTowers : towers.Take(5).ToArray();
                count += targetLibrary.guardTowers.Length;
                Debug.Log($"[PrefabPopulator] Added {targetLibrary.guardTowers.Length} guard towers");
            }
            
            // Barns/Warehouses/Storage
            var barns = FindBuildingsAcrossAllPacks("Barn", "Warehouse", "Storage", "Granary", "Silo");
            if (barns.Length > 0)
            {
                targetLibrary.barns = barns.Where(p => p.name.Contains("Barn")).ToArray();
                targetLibrary.warehouses = barns.Where(p => !p.name.Contains("Barn")).ToArray();
                if (targetLibrary.barns.Length == 0) targetLibrary.barns = barns.Take(barns.Length / 2).ToArray();
                if (targetLibrary.warehouses.Length == 0) targetLibrary.warehouses = barns.Skip(barns.Length / 2).ToArray();
                count += barns.Length;
                Debug.Log($"[PrefabPopulator] Added {barns.Length} barns/warehouses");
            }
            
            // Windmills/Mills
            var mills = FindBuildingsAcrossAllPacks("Windmill", "Mill", "Watermill");
            if (mills.Length > 0)
            {
                targetLibrary.windmills = mills;
                count += mills.Length;
                Debug.Log($"[PrefabPopulator] Added {mills.Length} mills");
            }
            
            // Castles/Keeps
            var castles = FindBuildingsAcrossAllPacks("Castle", "Keep", "Fortress", "Citadel", "Palace");
            if (castles.Length > 0)
            {
                targetLibrary.castles = castles;
                targetLibrary.keeps = castles.Where(p => p.name.Contains("Keep")).ToArray();
                if (targetLibrary.keeps.Length == 0) targetLibrary.keeps = castles.Take(2).ToArray();
                count += castles.Length;
                Debug.Log($"[PrefabPopulator] Added {castles.Length} castles/keeps");
            }
            
            // Walls/Gates
            var walls = FindBuildingsAcrossAllPacks("Wall_", "Gate", "Gatehouse", "Portcullis");
            if (walls.Length > 0)
            {
                targetLibrary.walls = walls.Where(p => p.name.Contains("Wall") && !p.name.Contains("Gate")).ToArray();
                targetLibrary.gates = walls.Where(p => p.name.Contains("Gate")).ToArray();
                count += walls.Length;
                Debug.Log($"[PrefabPopulator] Added {walls.Length} walls/gates");
            }
            
            // Stables
            var stables = FindBuildingsAcrossAllPacks("Stable", "Horse", "Pen");
            if (stables.Length > 0)
            {
                targetLibrary.stables = stables;
                count += stables.Length;
                Debug.Log($"[PrefabPopulator] Added {stables.Length} stables");
            }
            
            // Docks/Piers (for waterfront areas)
            var docks = FindBuildingsAcrossAllPacks("Dock", "Pier", "Wharf", "Harbor");
            if (docks.Length > 0)
            {
                targetLibrary.docks = docks;
                count += docks.Length;
                Debug.Log($"[PrefabPopulator] Added {docks.Length} docks");
            }
            
            Debug.Log($"[PrefabPopulator] Total buildings from all Synty packs: {count}");
            return count;
        }
        
        // =====================================================================
        // PATHS - Search multiple Synty packs
        // =====================================================================
        private int PopulateCobblestoneSegments()
        {
            // Search all packs for cobblestone/brick paths
            var cobblestones = FindPrefabsInAllSyntyPacks(
                "SM_Env_Path_Brick", "SM_Env_Path_Cobble", "SM_Env_Path_Stone",
                "SM_Env_Path_01", "SM_Env_Path_02", "SM_Env_Path_03",
                "SM_Gen_Path_Brick", "SM_Gen_Path_Cobble", "SM_Gen_Path_Stone",
                "Cobblestone", "Brick_Path"
            );
            
            targetLibrary.cobblestoneSegments = cobblestones;
            Debug.Log($"[PrefabPopulator] Populated {cobblestones.Length} cobblestone segments from all Synty packs");
            return cobblestones.Length;
        }
        
        private int PopulateDirtPaths()
        {
            // Search all packs for dirt/gravel paths
            var dirtPaths = FindPrefabsInAllSyntyPacks(
                "Road_Gravel", "Road_Dirt", "Path_Dirt", "Path_Gravel",
                "SM_Env_Path_Dirt", "SM_Gen_Path_Dirt", "SM_Gen_Road",
                "Trail", "Footpath"
            );
            
            targetLibrary.dirtPathSegments = dirtPaths;
            Debug.Log($"[PrefabPopulator] Populated {dirtPaths.Length} dirt path segments from all Synty packs");
            return dirtPaths.Length;
        }
        
        // =====================================================================
        // VEGETATION - Search ALL Synty packs for trees/bushes
        // =====================================================================
        private int PopulateTrees()
        {
            int count = 0;
            
            // Search all Synty packs for trees
            var allTrees = FindPrefabsInAllSyntyPacks(
                "SM_Env_Tree", "SM_Tree", "SM_Gen_Tree",
                "SM_Env_Pine", "SM_Env_Oak", "SM_Env_Willow", "SM_Env_Birch"
            );
            
            Debug.Log($"[PrefabPopulator] Found {allTrees.Length} total trees across all Synty packs");
            
            // Categorize trees
            var pines = allTrees.Where(p => 
                p.name.Contains("Pine") || p.name.Contains("Conifer")).ToArray();
            var oaks = allTrees.Where(p => 
                p.name.Contains("Oak") || p.name.Contains("Large") || 
                (p.name.Contains("Tree_0") && !pines.Contains(p))).ToArray();
            var willows = allTrees.Where(p => 
                p.name.Contains("Willow") || p.name.Contains("Weeping")).ToArray();
            var birch = allTrees.Where(p => 
                p.name.Contains("Birch")).ToArray();
            var dead = allTrees.Where(p => 
                p.name.Contains("Dead") || p.name.Contains("Burnt")).ToArray();
            var fantasy = allTrees.Where(p => 
                p.name.Contains("Magic") || p.name.Contains("Fantasy") || 
                p.name.Contains("Mystical") || p.name.Contains("Giant")).ToArray();
            
            // Assign to library
            if (pines.Length > 0) { targetLibrary.treesPine = pines; count += pines.Length; }
            if (oaks.Length > 0) { targetLibrary.treesOak = oaks; count += oaks.Length; }
            if (willows.Length > 0) { targetLibrary.treesWillow = willows; count += willows.Length; }
            if (dead.Length > 0) { targetLibrary.treesDead = dead; count += dead.Length; }
            if (fantasy.Length > 0) { targetLibrary.treesFantasy = fantasy; count += fantasy.Length; }
            
            // Also populate generic "trees" array with a mix
            var generalTrees = new List<GameObject>();
            if (oaks.Length > 0) generalTrees.AddRange(oaks.Take(5));
            if (pines.Length > 0) generalTrees.AddRange(pines.Take(5));
            if (willows.Length > 0) generalTrees.AddRange(willows.Take(3));
            if (birch.Length > 0) generalTrees.AddRange(birch.Take(3));
            targetLibrary.trees = generalTrees.ToArray();
            
            Debug.Log($"[PrefabPopulator] Trees: {pines.Length} pine, {oaks.Length} oak, {willows.Length} willow, {dead.Length} dead, {fantasy.Length} fantasy");
            return count;
        }
        
        private int PopulateBushes()
        {
            int count = 0;
            
            // Search all Synty packs for bushes and vegetation
            var allBushes = FindPrefabsInAllSyntyPacks(
                "SM_Env_Bush", "SM_Env_Shrub", "SM_Env_Hedge",
                "SM_Gen_Bush", "SM_Bush", "Shrub"
            );
            
            Debug.Log($"[PrefabPopulator] Found {allBushes.Length} total bushes across all Synty packs");
            
            if (allBushes.Length > 0)
            {
                var small = allBushes.Where(p => 
                    p.name.Contains("Small") || p.name.Contains("_01") || p.name.Contains("_02")).ToArray();
                var large = allBushes.Where(p => 
                    p.name.Contains("Large") || !small.Contains(p)).ToArray();
                
                targetLibrary.bushesSmall = small.Length > 0 ? small : allBushes.Take(allBushes.Length / 2).ToArray();
                targetLibrary.bushesLarge = large.Length > 0 ? large : allBushes.Skip(allBushes.Length / 2).ToArray();
                count = allBushes.Length;
            }
            
            // Flowers
            var flowers = FindPrefabsInAllSyntyPacks("SM_Env_Flower", "SM_Flower", "SM_Gen_Flower", "SM_Env_Plant");
            if (flowers.Length > 0)
            {
                targetLibrary.flowerPatches = flowers;
                count += flowers.Length;
                Debug.Log($"[PrefabPopulator] Added {flowers.Length} flower patches");
            }
            
            // Grass
            var grass = FindPrefabsInAllSyntyPacks(
                "SM_Env_Grass", "SM_Gen_Grass", "SM_Grass",
                "SM_Env_Fern", "SM_Env_Weed"
            );
            if (grass.Length > 0)
            {
                targetLibrary.grassClumps = grass;
                count += grass.Length;
                Debug.Log($"[PrefabPopulator] Added {grass.Length} grass clumps");
            }
            
            // Rocks
            var rocks = FindPrefabsInAllSyntyPacks(
                "SM_Env_Rock", "SM_Env_Stone", "SM_Env_Boulder",
                "SM_Gen_Rock", "SM_Rock"
            );
            if (rocks.Length > 0)
            {
                var small = rocks.Where(p => p.name.Contains("Small") || p.name.Contains("_01")).ToArray();
                var medium = rocks.Where(p => !p.name.Contains("Small") && !p.name.Contains("Large") && !p.name.Contains("Boulder")).ToArray();
                var large = rocks.Where(p => p.name.Contains("Large") || p.name.Contains("Boulder") || p.name.Contains("Giant")).ToArray();
                
                if (small.Length > 0) targetLibrary.rocksSmall = small;
                if (medium.Length > 0) targetLibrary.rocksMedium = medium;
                if (large.Length > 0) { targetLibrary.rocksLarge = large; targetLibrary.boulders = large; }
                
                count += rocks.Length;
                Debug.Log($"[PrefabPopulator] Added {rocks.Length} rocks ({small.Length} small, {medium.Length} medium, {large.Length} large)");
            }
            
            // Logs/stumps
            var logs = FindPrefabsInAllSyntyPacks("SM_Env_Log", "SM_Env_Stump", "SM_Log", "SM_Stump");
            if (logs.Length > 0)
            {
                targetLibrary.logs = logs;
                count += logs.Length;
                Debug.Log($"[PrefabPopulator] Added {logs.Length} logs/stumps");
            }
            
            // Mushrooms
            var mushrooms = FindPrefabsInAllSyntyPacks("SM_Env_Mushroom", "SM_Mushroom", "Mushroom");
            if (mushrooms.Length > 0)
            {
                targetLibrary.mushrooms = mushrooms;
                count += mushrooms.Length;
                Debug.Log($"[PrefabPopulator] Added {mushrooms.Length} mushrooms");
            }
            
            return count;
        }
        
        // =====================================================================
        // PROPS - Search ALL Synty packs for decorative props
        // =====================================================================
        private int PopulateProps()
        {
            int count = 0;
            
            Debug.Log("[PrefabPopulator] Searching all Synty packs for props...");
            
            // Barrels
            var barrels = FindPrefabsInAllSyntyPacks("SM_Prop_Barrel", "SM_Bld_Barrel", "Barrel");
            if (barrels.Length > 0)
            {
                targetLibrary.barrels = barrels;
                count += barrels.Length;
                Debug.Log($"[PrefabPopulator] Added {barrels.Length} barrels");
            }
            
            // Crates
            var crates = FindPrefabsInAllSyntyPacks("SM_Prop_Crate", "SM_Prop_Box", "SM_Bld_Crate", "Crate");
            if (crates.Length > 0)
            {
                targetLibrary.crates = crates;
                count += crates.Length;
                Debug.Log($"[PrefabPopulator] Added {crates.Length} crates");
            }
            
            // Sacks/Bags
            var sacks = FindPrefabsInAllSyntyPacks("SM_Prop_Sack", "SM_Prop_Bag", "SM_Bld_Sack");
            if (sacks.Length > 0)
            {
                targetLibrary.sacks = sacks;
                count += sacks.Length;
                Debug.Log($"[PrefabPopulator] Added {sacks.Length} sacks");
            }
            
            // Carts/Wagons from all vehicle folders
            var carts = FindPrefabsInAllSyntyPacks("SM_Veh_Cart", "SM_Veh_Wagon", "Cart", "Wagon");
            if (carts.Length > 0)
            {
                targetLibrary.carts = carts.Where(p => p.name.Contains("Cart")).ToArray();
                targetLibrary.wagons = carts.Where(p => p.name.Contains("Wagon")).ToArray();
                if (targetLibrary.carts.Length == 0) targetLibrary.carts = carts.Take(carts.Length / 2).ToArray();
                if (targetLibrary.wagons.Length == 0) targetLibrary.wagons = carts.Skip(carts.Length / 2).ToArray();
                count += carts.Length;
                Debug.Log($"[PrefabPopulator] Added {carts.Length} carts/wagons");
            }
            
            // Benches
            var benches = FindPrefabsInAllSyntyPacks("SM_Prop_Bench", "SM_Bld_Bench", "Bench");
            if (benches.Length > 0)
            {
                targetLibrary.benches = benches;
                count += benches.Length;
                Debug.Log($"[PrefabPopulator] Added {benches.Length} benches");
            }
            
            // Tables
            var tables = FindPrefabsInAllSyntyPacks("SM_Prop_Table", "SM_Bld_Table", "Table");
            if (tables.Length > 0)
            {
                targetLibrary.tables = tables;
                count += tables.Length;
                Debug.Log($"[PrefabPopulator] Added {tables.Length} tables");
            }
            
            // Wells
            var wells = FindPrefabsInAllSyntyPacks("SM_Prop_Well", "SM_Bld_Well", "Well");
            if (wells.Length > 0)
            {
                targetLibrary.wells = wells;
                count += wells.Length;
                Debug.Log($"[PrefabPopulator] Added {wells.Length} wells");
            }
            
            // Lanterns/Lights/Lamps
            var lanterns = FindPrefabsInAllSyntyPacks(
                "SM_Prop_Lantern", "SM_Prop_Light", "SM_Prop_Lamp", 
                "SM_Prop_Torch", "SM_Bld_Lantern", "Lantern", "Torch"
            );
            if (lanterns.Length > 0)
            {
                targetLibrary.lanterns = lanterns;
                count += lanterns.Length;
                Debug.Log($"[PrefabPopulator] Added {lanterns.Length} lanterns/lights");
            }
            
            // Signs
            var signs = FindPrefabsInAllSyntyPacks("SM_Prop_Sign", "SM_Bld_Sign", "Sign");
            if (signs.Length > 0)
            {
                targetLibrary.signs = signs;
                count += signs.Length;
                Debug.Log($"[PrefabPopulator] Added {signs.Length} signs");
            }
            
            // Flags/Banners
            var flags = FindPrefabsInAllSyntyPacks("SM_Prop_Flag", "SM_Prop_Banner", "SM_Bld_Flag", "Banner", "Flag");
            if (flags.Length > 0)
            {
                targetLibrary.flags = flags;
                count += flags.Length;
                Debug.Log($"[PrefabPopulator] Added {flags.Length} flags/banners");
            }
            
            // Market stalls
            var stalls = FindPrefabsInAllSyntyPacks("SM_Bld_Stall", "SM_Prop_Stall", "Market_Stall", "Stall");
            if (stalls.Length > 0)
            {
                targetLibrary.marketStalls = stalls;
                count += stalls.Length;
                Debug.Log($"[PrefabPopulator] Added {stalls.Length} market stalls");
            }
            
            // Fences
            var fences = FindPrefabsInAllSyntyPacks("SM_Bld_Fence", "SM_Prop_Fence", "SM_Env_Fence", "Fence");
            if (fences.Length > 0)
            {
                targetLibrary.fences = fences;
                count += fences.Length;
                Debug.Log($"[PrefabPopulator] Added {fences.Length} fences");
            }
            
            // Hay/Straw
            var hay = FindPrefabsInAllSyntyPacks("SM_Prop_Hay", "SM_Bld_Hay", "Hay", "Straw");
            if (hay.Length > 0)
            {
                targetLibrary.hayBales = hay;
                count += hay.Length;
                Debug.Log($"[PrefabPopulator] Added {hay.Length} hay bales");
            }
            
            Debug.Log($"[PrefabPopulator] Total props from all Synty packs: {count}");
            return count;
        }
        
        // =====================================================================
        // HELPER FUNCTIONS
        // =====================================================================
        
        /// <summary>
        /// All Synty pack paths to search for consistent art style
        /// </summary>
        private static readonly string[] AllSyntyPaths = new[]
        {
            "Assets/Synty/PolygonFantasyKingdom/Prefabs",
            "Assets/Synty/PolygonGeneric/Prefabs",
            "Assets/PolygonAdventure/Prefabs",
            "Assets/PolygonKnights/Prefabs",
            "Assets/PolygonVikings/Prefabs",
            "Assets/PolygonFarm/Prefabs",
            "Assets/PolygonTown/Prefabs",
            "Assets/PolygonNature/Prefabs",
            "Assets/PolygonDungeon/Prefabs",
            "Assets/PolygonPirates/Prefabs",
            "Assets/PolygonSamurai/Prefabs"
        };
        
        /// <summary>
        /// Priority order for building packs (fantasy-themed first)
        /// </summary>
        private static readonly string[] BuildingPackPriority = new[]
        {
            "Assets/Synty/PolygonFantasyKingdom/Prefabs",
            "Assets/PolygonKnights/Prefabs",
            "Assets/PolygonVikings/Prefabs",
            "Assets/PolygonAdventure/Prefabs",
            "Assets/PolygonFarm/Prefabs"
        };
        
        private GameObject[] FindPrefabsMatching(string searchPath, params string[] patterns)
        {
            var prefabs = new List<GameObject>();
            
            if (!AssetDatabase.IsValidFolder(searchPath))
            {
                Debug.LogWarning($"[PrefabPopulator] Folder not found: {searchPath}");
                return prefabs.ToArray();
            }
            
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                
                if (patterns.Any(p => name.Contains(p)))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null && !prefabs.Contains(prefab))
                    {
                        prefabs.Add(prefab);
                    }
                }
            }
            
            return prefabs.ToArray();
        }
        
        /// <summary>
        /// Search ALL Synty packs for matching prefabs
        /// </summary>
        private GameObject[] FindPrefabsInAllSyntyPacks(params string[] patterns)
        {
            var prefabs = new List<GameObject>();
            
            foreach (var basePath in AllSyntyPaths)
            {
                if (!AssetDatabase.IsValidFolder(basePath)) continue;
                
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { basePath });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var name = System.IO.Path.GetFileNameWithoutExtension(path);
                    
                    if (patterns.Any(p => name.Contains(p)))
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null && !prefabs.Any(p => p.name == prefab.name))
                        {
                            prefabs.Add(prefab);
                        }
                    }
                }
            }
            
            return prefabs.ToArray();
        }
        
        /// <summary>
        /// Search for buildings across all fantasy-appropriate Synty packs
        /// </summary>
        private GameObject[] FindBuildingsAcrossAllPacks(params string[] patterns)
        {
            var prefabs = new List<GameObject>();
            
            foreach (var basePath in BuildingPackPriority)
            {
                string buildingsPath = basePath + "/Buildings";
                string presetsPath = basePath + "/Buildings/Presets";
                
                // Check Presets first (complete buildings)
                if (AssetDatabase.IsValidFolder(presetsPath))
                {
                    prefabs.AddRange(FindPrefabsMatching(presetsPath, patterns));
                }
                
                // Then check main Buildings folder
                if (AssetDatabase.IsValidFolder(buildingsPath))
                {
                    var found = FindPrefabsMatching(buildingsPath, patterns);
                    foreach (var p in found)
                    {
                        if (!prefabs.Any(existing => existing.name == p.name))
                            prefabs.Add(p);
                    }
                }
            }
            
            return prefabs.ToArray();
        }
    }
}
#endif
