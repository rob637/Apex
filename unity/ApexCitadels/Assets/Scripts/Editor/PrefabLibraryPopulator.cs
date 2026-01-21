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
        // BUILDINGS
        // =====================================================================
        private int PopulateAllBuildings()
        {
            int count = 0;
            string presetsPath = "Assets/Synty/PolygonFantasyKingdom/Prefabs/Buildings/Presets";
            
            // Houses (various sizes)
            var houses = FindPrefabsMatching(presetsPath, "SM_Bld_Preset_House");
            if (houses.Length > 0)
            {
                // Split into small, medium, large based on name
                var smallHouses = houses.Where(p => p.name.Contains("01") || p.name.Contains("02") || p.name.Contains("03")).ToArray();
                var mediumHouses = houses.Where(p => p.name.Contains("04") || p.name.Contains("05") || p.name.Contains("06")).ToArray();
                var largeHouses = houses.Where(p => p.name.Contains("07") || p.name.Contains("08") || p.name.Contains("09") || p.name.Contains("10")).ToArray();
                
                if (smallHouses.Length > 0) { targetLibrary.cottages = smallHouses; count += smallHouses.Length; }
                if (mediumHouses.Length > 0) { targetLibrary.houses = mediumHouses; targetLibrary.smallHouses = mediumHouses; count += mediumHouses.Length; }
                if (largeHouses.Length > 0) { targetLibrary.townHouses = largeHouses; targetLibrary.manors = largeHouses; count += largeHouses.Length; }
                
                Debug.Log($"[PrefabPopulator] Houses: {smallHouses.Length} small, {mediumHouses.Length} medium, {largeHouses.Length} large");
            }
            
            // Churches
            var churches = FindPrefabsMatching(presetsPath, "SM_Bld_Preset_Church");
            if (churches.Length > 0)
            {
                targetLibrary.churches = churches;
                targetLibrary.chapels = churches;
                count += churches.Length;
                Debug.Log($"[PrefabPopulator] Added {churches.Length} churches");
            }
            
            // Blacksmith
            var blacksmiths = FindPrefabsMatching(presetsPath, "SM_Bld_Preset_Blacksmith");
            if (blacksmiths.Length > 0)
            {
                targetLibrary.blacksmiths = blacksmiths;
                count += blacksmiths.Length;
                Debug.Log($"[PrefabPopulator] Added {blacksmiths.Length} blacksmiths");
            }
            
            // Tavern/Inn from Props or Presets
            var taverns = FindPrefabsMatching(presetsPath, "Tavern", "Inn");
            if (taverns.Length > 0)
            {
                targetLibrary.taverns = taverns;
                targetLibrary.inns = taverns;
                count += taverns.Length;
            }
            
            // Use some houses as shops if no specific shop prefabs
            if (targetLibrary.shops == null || targetLibrary.shops.Length == 0)
            {
                if (houses.Length > 2)
                {
                    targetLibrary.shops = houses.Take(3).ToArray();
                    targetLibrary.generalStores = houses.Take(2).ToArray();
                    Debug.Log($"[PrefabPopulator] Using houses as shops (fallback)");
                }
            }
            
            // Guard towers from Castle folder
            string castlePath = "Assets/Synty/PolygonFantasyKingdom/Prefabs/Castle";
            var towers = FindPrefabsMatching(castlePath, "SM_Bld_Castle_Tower", "SM_Bld_Tower");
            if (towers.Length > 0)
            {
                targetLibrary.guardTowers = towers.Take(5).ToArray();
                count += targetLibrary.guardTowers.Length;
                Debug.Log($"[PrefabPopulator] Added {targetLibrary.guardTowers.Length} guard towers");
            }
            
            // Barns/Warehouses - use larger buildings
            var barns = FindPrefabsMatching(presetsPath, "Barn", "Warehouse", "Storage");
            if (barns.Length == 0 && houses.Length > 0)
            {
                // Use large houses as warehouses fallback
                targetLibrary.warehouses = houses.Where(p => p.name.Contains("09") || p.name.Contains("10")).ToArray();
                targetLibrary.barns = targetLibrary.warehouses;
            }
            else if (barns.Length > 0)
            {
                targetLibrary.barns = barns;
                targetLibrary.warehouses = barns;
                count += barns.Length;
            }
            
            Debug.Log($"[PrefabPopulator] Total buildings populated: {count}");
            return count;
        }
        
        // =====================================================================
        // PATHS
        // =====================================================================
        private int PopulateCobblestoneSegments()
        {
            var prefabs = new List<GameObject>();
            
            // Find cobblestone/brick path prefabs from PolygonFantasyKingdom
            string[] searchPaths = new[]
            {
                "Assets/Synty/PolygonFantasyKingdom/Prefabs/Environments"
            };
            
            string[] searchPatterns = new[]
            {
                "SM_Env_Path_Brick",
                "SM_Env_Path_01",
                "SM_Env_Path_02",
                "SM_Env_Path_03",
                "SM_Env_Path_04",
                "SM_Env_Path_05"
            };
            
            foreach (var searchPath in searchPaths)
            {
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var name = System.IO.Path.GetFileNameWithoutExtension(path);
                    
                    // Check if it matches our patterns
                    if (searchPatterns.Any(p => name.StartsWith(p)))
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null && !prefabs.Contains(prefab))
                        {
                            prefabs.Add(prefab);
                            Debug.Log($"[PrefabPopulator] Added cobblestone: {name}");
                        }
                    }
                }
            }
            
            targetLibrary.cobblestoneSegments = prefabs.ToArray();
            Debug.Log($"[PrefabPopulator] Populated {prefabs.Count} cobblestone segments");
            return prefabs.Count;
        }
        
        private int PopulateDirtPaths()
        {
            var prefabs = new List<GameObject>();
            
            // Find dirt/gravel paths from PolygonGeneric
            string searchPath = "Assets/Synty/PolygonGeneric/Prefabs/Environment";
            
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                
                if (name.Contains("Road_Gravel") || name.Contains("Path"))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null && !prefabs.Contains(prefab))
                    {
                        prefabs.Add(prefab);
                        Debug.Log($"[PrefabPopulator] Added dirt path: {name}");
                    }
                }
            }
            
            targetLibrary.dirtPathSegments = prefabs.ToArray();
            Debug.Log($"[PrefabPopulator] Populated {prefabs.Count} dirt path segments");
            return prefabs.Count;
        }
        
        // =====================================================================
        // VEGETATION
        // =====================================================================
        private int PopulateTrees()
        {
            int count = 0;
            string envPath = "Assets/Synty/PolygonFantasyKingdom/Prefabs/Environments";
            
            // Pine trees
            var pines = FindPrefabsMatching(envPath, "SM_Env_Tree_Pine", "SM_Env_Pine");
            if (pines.Length > 0)
            {
                targetLibrary.treesPine = pines;
                count += pines.Length;
                Debug.Log($"[PrefabPopulator] Added {pines.Length} pine trees");
            }
            
            // Oak/regular trees
            var oaks = FindPrefabsMatching(envPath, "SM_Env_Tree_0", "SM_Env_Tree_Large");
            if (oaks.Length > 0)
            {
                targetLibrary.treesOak = oaks;
                count += oaks.Length;
                Debug.Log($"[PrefabPopulator] Added {oaks.Length} oak trees");
            }
            
            // Dead trees
            var dead = FindPrefabsMatching(envPath, "SM_Env_Tree_Dead", "SM_Env_Dead_Tree");
            if (dead.Length > 0)
            {
                targetLibrary.treesDead = dead;
                count += dead.Length;
            }
            
            // Fantasy trees
            var fantasy = FindPrefabsMatching(envPath, "SM_Env_Tree_Magic", "SM_Env_Tree_Fantasy");
            if (fantasy.Length > 0)
            {
                targetLibrary.treesFantasy = fantasy;
                count += fantasy.Length;
            }
            
            Debug.Log($"[PrefabPopulator] Total trees: {count}");
            return count;
        }
        
        private int PopulateBushes()
        {
            int count = 0;
            string envPath = "Assets/Synty/PolygonFantasyKingdom/Prefabs/Environments";
            
            var bushes = FindPrefabsMatching(envPath, "SM_Env_Bush", "SM_Env_Shrub", "SM_Env_Hedge");
            if (bushes.Length > 0)
            {
                var small = bushes.Where(p => p.name.Contains("Small") || p.name.Contains("01") || p.name.Contains("02")).ToArray();
                var large = bushes.Where(p => !small.Contains(p)).ToArray();
                
                if (small.Length > 0) targetLibrary.bushesSmall = small;
                else targetLibrary.bushesSmall = bushes.Take(bushes.Length / 2).ToArray();
                
                if (large.Length > 0) targetLibrary.bushesLarge = large;
                else targetLibrary.bushesLarge = bushes.Skip(bushes.Length / 2).ToArray();
                
                count = bushes.Length;
                Debug.Log($"[PrefabPopulator] Added {count} bushes");
            }
            
            // Flowers
            var flowers = FindPrefabsMatching(envPath, "SM_Env_Flower", "SM_Env_Plant");
            if (flowers.Length > 0)
            {
                targetLibrary.flowerPatches = flowers;
                count += flowers.Length;
            }
            
            // Grass
            var grass = FindPrefabsMatching(envPath, "SM_Env_Grass");
            if (grass.Length > 0)
            {
                targetLibrary.grassClumps = grass;
                count += grass.Length;
            }
            
            // Rocks
            var rocks = FindPrefabsMatching(envPath, "SM_Env_Rock", "SM_Env_Stone");
            if (rocks.Length > 0)
            {
                var small = rocks.Where(p => p.name.Contains("Small") || p.name.Contains("01")).ToArray();
                var large = rocks.Where(p => p.name.Contains("Large") || p.name.Contains("Boulder")).ToArray();
                
                if (small.Length > 0) targetLibrary.rocksSmall = small;
                if (large.Length > 0) { targetLibrary.rocksLarge = large; targetLibrary.boulders = large; }
                
                count += rocks.Length;
                Debug.Log($"[PrefabPopulator] Added {rocks.Length} rocks");
            }
            
            return count;
        }
        
        // =====================================================================
        // PROPS
        // =====================================================================
        private int PopulateProps()
        {
            int count = 0;
            string propsPath = "Assets/Synty/PolygonFantasyKingdom/Prefabs/Props";
            
            // Barrels
            var barrels = FindPrefabsMatching(propsPath, "SM_Prop_Barrel");
            if (barrels.Length > 0)
            {
                targetLibrary.barrels = barrels;
                count += barrels.Length;
            }
            
            // Crates
            var crates = FindPrefabsMatching(propsPath, "SM_Prop_Crate", "SM_Prop_Box");
            if (crates.Length > 0)
            {
                targetLibrary.crates = crates;
                count += crates.Length;
            }
            
            // Sacks
            var sacks = FindPrefabsMatching(propsPath, "SM_Prop_Sack", "SM_Prop_Bag");
            if (sacks.Length > 0)
            {
                targetLibrary.sacks = sacks;
                count += sacks.Length;
            }
            
            // Carts/Wagons
            string vehiclesPath = "Assets/Synty/PolygonFantasyKingdom/Prefabs/Vehicles";
            var carts = FindPrefabsMatching(vehiclesPath, "SM_Veh_Cart", "SM_Veh_Wagon");
            if (carts.Length > 0)
            {
                targetLibrary.carts = carts.Where(p => p.name.Contains("Cart")).ToArray();
                targetLibrary.wagons = carts.Where(p => p.name.Contains("Wagon")).ToArray();
                if (targetLibrary.carts.Length == 0) targetLibrary.carts = carts;
                if (targetLibrary.wagons.Length == 0) targetLibrary.wagons = carts;
                count += carts.Length;
            }
            
            // Benches
            var benches = FindPrefabsMatching(propsPath, "SM_Prop_Bench");
            if (benches.Length > 0)
            {
                targetLibrary.benches = benches;
                count += benches.Length;
            }
            
            // Tables
            var tables = FindPrefabsMatching(propsPath, "SM_Prop_Table");
            if (tables.Length > 0)
            {
                targetLibrary.tables = tables;
                count += tables.Length;
            }
            
            // Wells
            var wells = FindPrefabsMatching(propsPath, "SM_Prop_Well");
            if (wells.Length > 0)
            {
                targetLibrary.wells = wells;
                count += wells.Length;
            }
            
            // Lanterns/Lights
            var lanterns = FindPrefabsMatching(propsPath, "SM_Prop_Lantern", "SM_Prop_Light", "SM_Prop_Lamp");
            if (lanterns.Length > 0)
            {
                targetLibrary.lanterns = lanterns;
                count += lanterns.Length;
            }
            
            // Signs
            var signs = FindPrefabsMatching(propsPath, "SM_Prop_Sign");
            if (signs.Length > 0)
            {
                targetLibrary.signs = signs;
                count += signs.Length;
            }
            
            Debug.Log($"[PrefabPopulator] Total props: {count}");
            return count;
        }
        
        // =====================================================================
        // HELPER FUNCTIONS
        // =====================================================================
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
    }
}
#endif
