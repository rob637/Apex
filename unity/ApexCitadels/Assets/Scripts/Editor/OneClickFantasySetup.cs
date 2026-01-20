// ============================================================================
// APEX CITADELS - ONE-CLICK FANTASY SETUP
// Automatically sets up everything for Fantasy World generation
// Unity Menu: Apex Citadels > One-Click Fantasy Setup
// ============================================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ApexCitadels.Editor
{
    public static class OneClickFantasySetup
    {
        [MenuItem("Apex Citadels/One-Click Fantasy Setup")]
        public static void Setup()
        {
            Debug.Log("========================================");
            Debug.Log("ONE-CLICK FANTASY SETUP STARTING");
            Debug.Log("========================================");
            
            // Step 1: Find or create prefab library
            var library = FindOrCreateLibrary();
            if (library == null)
            {
                Debug.LogError("Failed to find or create prefab library!");
                return;
            }
            
            // Step 2: Scan and assign Synty prefabs
            ScanAndAssignPrefabs(library);
            
            // Step 3: Copy library to Resources folder
            CopyLibraryToResources(library);
            
            // Step 4: Assign library to scene generator
            AssignLibraryToScene(library);
            
            Debug.Log("========================================");
            Debug.Log("ONE-CLICK SETUP COMPLETE!");
            Debug.Log("Press Play to test the fantasy world.");
            Debug.Log("========================================");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        private static FantasyWorld.FantasyPrefabLibrary FindOrCreateLibrary()
        {
            // Find existing library
            string[] guids = AssetDatabase.FindAssets("t:FantasyPrefabLibrary");
            
            FantasyWorld.FantasyPrefabLibrary library = null;
            
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var lib = AssetDatabase.LoadAssetAtPath<FantasyWorld.FantasyPrefabLibrary>(path);
                if (lib != null)
                {
                    library = lib;
                    Debug.Log($"Found existing prefab library: {path}");
                    break;
                }
            }
            
            if (library != null)
            {
                return library;
            }
            
            // Create new library
            Debug.Log("Creating new FantasyPrefabLibrary...");
            library = ScriptableObject.CreateInstance<FantasyWorld.FantasyPrefabLibrary>();
            
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            AssetDatabase.CreateAsset(library, "Assets/Resources/MainFantasyPrefabLibrary.asset");
            return library;
        }
        
        private static void ScanAndAssignPrefabs(FantasyWorld.FantasyPrefabLibrary library)
        {
            Debug.Log("Scanning for Synty prefabs...");
            
            var categorizedPrefabs = new Dictionary<string, List<GameObject>>();
            
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int syntyCount = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                bool isSynty = path.Contains("Polygon") || 
                               path.Contains("POLYGON") || 
                               path.Contains("Synty");
                               
                if (!isSynty) continue;
                
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                
                string category = CategorizeByName(prefab.name, path);
                
                if (!string.IsNullOrEmpty(category))
                {
                    if (!categorizedPrefabs.ContainsKey(category))
                        categorizedPrefabs[category] = new List<GameObject>();
                    
                    categorizedPrefabs[category].Add(prefab);
                    syntyCount++;
                }
            }
            
            Debug.Log($"Found {syntyCount} Synty prefabs in {categorizedPrefabs.Count} categories");
            
            // Assign to library
            SerializedObject so = new SerializedObject(library);
            int assigned = 0;
            
            foreach (var category in categorizedPrefabs)
            {
                string fieldName = CategoryToFieldName(category.Key);
                SerializedProperty prop = so.FindProperty(fieldName);
                
                if (prop != null && prop.isArray)
                {
                    prop.ClearArray();
                    var prefabs = category.Value;
                    for (int i = 0; i < prefabs.Count; i++)
                    {
                        prop.InsertArrayElementAtIndex(i);
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
                    }
                    assigned++;
                    Debug.Log($"  {category.Key}: {prefabs.Count} prefabs");
                }
            }
            
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(library);
            
            Debug.Log($"Assigned {assigned} categories to library");
        }
        
        private static void CopyLibraryToResources(FantasyWorld.FantasyPrefabLibrary library)
        {
            string currentPath = AssetDatabase.GetAssetPath(library);
            string targetPath = "Assets/Resources/MainFantasyPrefabLibrary.asset";
            
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            if (currentPath != targetPath)
            {
                // Check if target already exists
                var existingAtTarget = AssetDatabase.LoadAssetAtPath<FantasyWorld.FantasyPrefabLibrary>(targetPath);
                if (existingAtTarget != null && existingAtTarget != library)
                {
                    // Copy data to existing
                    EditorUtility.CopySerialized(library, existingAtTarget);
                    EditorUtility.SetDirty(existingAtTarget);
                    Debug.Log($"Updated library at: {targetPath}");
                }
                else if (existingAtTarget == null)
                {
                    // Copy file
                    AssetDatabase.CopyAsset(currentPath, targetPath);
                    Debug.Log($"Copied library to: {targetPath}");
                }
            }
        }
        
        private static void AssignLibraryToScene(FantasyWorld.FantasyPrefabLibrary library)
        {
            // Find the library from Resources (the one we just placed there)
            var resourcesLibrary = UnityEngine.Resources.Load<FantasyWorld.FantasyPrefabLibrary>("MainFantasyPrefabLibrary");
            if (resourcesLibrary == null) resourcesLibrary = library;
            
            // Find all FantasyWorldGenerator components in scene
            var generators = Object.FindObjectsByType<FantasyWorld.FantasyWorldGenerator>(FindObjectsSortMode.None);
            
            foreach (var gen in generators)
            {
                if (gen.prefabLibrary == null || gen.prefabLibrary != resourcesLibrary)
                {
                    gen.prefabLibrary = resourcesLibrary;
                    EditorUtility.SetDirty(gen);
                    Debug.Log($"Assigned library to: {gen.gameObject.name}");
                }
            }
            
            if (generators.Length == 0)
            {
                Debug.Log("No FantasyWorldGenerator found in scene. Library will be auto-loaded at runtime.");
            }
        }
        
        private static string CategorizeByName(string name, string path)
        {
            string lower = name.ToLower();
            string pathLower = path.ToLower();
            
            // Context from path
            bool isTown = pathLower.Contains("polygontown");
            bool isViking = pathLower.Contains("polygonviking");
            bool isFarm = pathLower.Contains("polygonfarm");
            bool isBuilding = lower.Contains("bld_") || lower.Contains("building");
            
            // BUILDINGS
            if (lower.Contains("hut") && !lower.Contains("shutter")) return "PeasantHuts";
            if (lower.Contains("cottage")) return "Cottages";
            if (lower.Contains("house")) return "Houses";
            if (lower.Contains("tavern") || lower.Contains("pub")) return "Taverns";
            if (lower.Contains("blacksmith") || lower.Contains("forge") || lower.Contains("smithy")) return "Blacksmiths";
            if (lower.Contains("shop") && !lower.Contains("workshop")) return "Shops";
            if (lower.Contains("market") || lower.Contains("stall")) return "MarketStalls";
            if (lower.Contains("tower")) return "GuardTowers";
            if (lower.Contains("castle")) return "CastleParts";
            if (lower.Contains("wall") && isBuilding) return "Walls";
            if (lower.Contains("gate") && isBuilding) return "Gates";
            if (lower.Contains("church")) return "Churches";
            if (lower.Contains("mill")) return "Mills";
            if (lower.Contains("barn")) return "Barns";
            if (lower.Contains("fountain")) return "Fountains";
            if (lower.Contains("well") && !lower.Contains("dwell")) return "Wells";
            if (lower.Contains("ruin")) return "Ruins";
            
            // Generic buildings from context
            if (isBuilding && (isTown || isViking)) return "Houses";
            if (isBuilding && isFarm) return "Farmhouses";
            
            // NATURE
            if (lower.Contains("tree")) return "TreesOak";
            if (lower.Contains("bush")) return "BushesSmall";
            if (lower.Contains("rock")) return "RocksMedium";
            if (lower.Contains("boulder")) return "Boulders";
            if (lower.Contains("log") && !lower.Contains("dialog")) return "Logs";
            if (lower.Contains("mushroom")) return "Mushrooms";
            if (lower.Contains("stump")) return "Stumps";
            
            // PROPS
            if (lower.Contains("barrel")) return "Barrels";
            if (lower.Contains("crate")) return "Crates";
            if (lower.Contains("chest") && !lower.Contains("breastplate")) return "Chests";
            if (lower.Contains("cart")) return "Carts";
            if (lower.Contains("wagon")) return "Wagons";
            if (lower.Contains("bench")) return "Benches";
            if (lower.Contains("table") && !lower.Contains("stable")) return "Tables";
            if (lower.Contains("chair")) return "Chairs";
            if (lower.Contains("sign") && !lower.Contains("design")) return "Signs";
            if (lower.Contains("lantern")) return "Lanterns";
            if (lower.Contains("torch") && !lower.Contains("scorched")) return "Torches";
            if (lower.Contains("campfire")) return "Campfires";
            if (lower.Contains("fence")) return "FenceWood";
            if (lower.Contains("flag") && !lower.Contains("flagstone")) return "Flags";
            if (lower.Contains("banner")) return "Banners";
            
            return null;
        }
        
        private static string CategoryToFieldName(string category)
        {
            if (string.IsNullOrEmpty(category)) return "";
            return char.ToLower(category[0]) + category.Substring(1);
        }
    }
}
#endif
