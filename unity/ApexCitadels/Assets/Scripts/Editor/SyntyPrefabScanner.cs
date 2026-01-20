// ============================================================================
// APEX CITADELS - SYNTY PREFAB SCANNER
// Editor tool to automatically discover and categorize Synty prefabs
// ============================================================================
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ApexCitadels.FantasyWorld;

namespace ApexCitadels.Editor
{
    public class SyntyPrefabScanner : EditorWindow
    {
        private FantasyPrefabLibrary _targetLibrary;
        private Vector2 _scrollPos;
        private Dictionary<string, List<GameObject>> _categorizedPrefabs = new Dictionary<string, List<GameObject>>();
        private bool _scanned = false;
        private string _status = "";
        
        [MenuItem("Apex Citadels/Synty Prefab Scanner")]
        public static void ShowWindow()
        {
            var window = GetWindow<SyntyPrefabScanner>("Synty Scanner");
            window.minSize = new Vector2(500, 600);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Synty POLYGON Prefab Scanner", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool scans your project for Synty POLYGON prefabs and helps populate your Fantasy Prefab Library.",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // Target library selection
            EditorGUILayout.LabelField("Target Library", EditorStyles.boldLabel);
            _targetLibrary = (FantasyPrefabLibrary)EditorGUILayout.ObjectField(
                "Prefab Library", _targetLibrary, typeof(FantasyPrefabLibrary), false);
            
            if (_targetLibrary == null)
            {
                EditorGUILayout.HelpBox(
                    "Create a Fantasy Prefab Library first:\nRight-click in Project > Create > Apex Citadels > Fantasy Prefab Library",
                    MessageType.Warning);
                
                if (GUILayout.Button("Create New Prefab Library"))
                {
                    CreateNewLibrary();
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Scan button
            EditorGUILayout.LabelField("Scan Project", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Scan for Synty Prefabs", GUILayout.Height(30)))
            {
                ScanForPrefabs();
            }
            
            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.HelpBox(_status, MessageType.Info);
            }
            
            EditorGUILayout.Space(10);
            
            // Results display
            if (_scanned && _categorizedPrefabs.Count > 0)
            {
                EditorGUILayout.LabelField("Scan Results", EditorStyles.boldLabel);
                
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                
                foreach (var category in _categorizedPrefabs.OrderBy(x => x.Key))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{category.Key}: {category.Value.Count} prefabs", EditorStyles.miniLabel);
                    
                    if (_targetLibrary != null && GUILayout.Button("Assign", GUILayout.Width(60)))
                    {
                        AssignToLibrary(category.Key, category.Value.ToArray());
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.Space(10);
                
                if (_targetLibrary != null && GUILayout.Button("Auto-Assign All Categories", GUILayout.Height(30)))
                {
                    AutoAssignAll();
                }
            }
        }
        
        private void CreateNewLibrary()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Fantasy Prefab Library",
                "MainFantasyPrefabLibrary",
                "asset",
                "Choose location for the new library");
            
            if (!string.IsNullOrEmpty(path))
            {
                var library = ScriptableObject.CreateInstance<FantasyPrefabLibrary>();
                AssetDatabase.CreateAsset(library, path);
                AssetDatabase.SaveAssets();
                _targetLibrary = library;
                EditorGUIUtility.PingObject(library);
            }
        }
        
        private void ScanForPrefabs()
        {
            _categorizedPrefabs.Clear();
            _status = "Scanning...";
            
            // Find all prefabs in the project
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int syntyCount = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // Check if it's likely a Synty prefab (in a POLYGON folder or has SM_ prefix)
                if (!path.Contains("POLYGON") && !path.Contains("Synty")) continue;
                
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                
                string name = prefab.name;
                string category = CategorizeByName(name);
                
                if (!string.IsNullOrEmpty(category))
                {
                    if (!_categorizedPrefabs.ContainsKey(category))
                        _categorizedPrefabs[category] = new List<GameObject>();
                    
                    _categorizedPrefabs[category].Add(prefab);
                    syntyCount++;
                }
            }
            
            _scanned = true;
            _status = $"Found {syntyCount} Synty prefabs in {_categorizedPrefabs.Count} categories";
        }
        
        private string CategorizeByName(string name)
        {
            // Normalize name for matching
            string lower = name.ToLower();
            
            // BUILDINGS
            if (lower.Contains("hut") && !lower.Contains("shutter")) return "PeasantHuts";
            if (lower.Contains("cottage")) return "Cottages";
            if (lower.Contains("house_small") || lower.Contains("smallhouse")) return "SmallHouses";
            if (lower.Contains("house") && !lower.Contains("town") && !lower.Contains("farm") && !lower.Contains("gate") && !lower.Contains("ware")) return "Houses";
            if (lower.Contains("townhouse") || lower.Contains("town_house")) return "TownHouses";
            if (lower.Contains("manor")) return "Manors";
            if (lower.Contains("noble") || lower.Contains("estate")) return "NobleEstates";
            if (lower.Contains("market") || lower.Contains("stall")) return "MarketStalls";
            if (lower.Contains("tavern")) return "Taverns";
            if (lower.Contains("inn")) return "Inns";
            if (lower.Contains("blacksmith") || lower.Contains("forge") || lower.Contains("anvil")) return "Blacksmiths";
            if (lower.Contains("bakery") || lower.Contains("baker")) return "Bakeries";
            if (lower.Contains("shop") && !lower.Contains("workshop")) return "Shops";
            if (lower.Contains("guard") && lower.Contains("tower")) return "GuardTowers";
            if (lower.Contains("barracks")) return "Barracks";
            if (lower.Contains("fortress")) return "Fortresses";
            if (lower.Contains("castle") && !lower.Contains("small")) return "CastleParts";
            if (lower.Contains("wall") && (lower.Contains("bld") || lower.Contains("env"))) return "Walls";
            if (lower.Contains("gate") && lower.Contains("bld")) return "Gates";
            if (lower.Contains("chapel")) return "Chapels";
            if (lower.Contains("church") && !lower.Contains("yard")) return "Churches";
            if (lower.Contains("cathedral")) return "Cathedrals";
            if (lower.Contains("townhall") || lower.Contains("town_hall")) return "TownHalls";
            if (lower.Contains("mill") && !lower.Contains("saw")) return "Mills";
            if (lower.Contains("warehouse")) return "Warehouses";
            if (lower.Contains("workshop")) return "Workshops";
            if (lower.Contains("barn")) return "Barns";
            if (lower.Contains("farmhouse") || lower.Contains("farm_house")) return "Farmhouses";
            if (lower.Contains("silo")) return "Silos";
            if (lower.Contains("mage") && lower.Contains("tower")) return "MageTowers";
            if (lower.Contains("wizard") && lower.Contains("tower")) return "MageTowers";
            if (lower.Contains("ruin")) return "Ruins";
            if (lower.Contains("monument") || lower.Contains("statue")) return "Monuments";
            if (lower.Contains("fountain")) return "Fountains";
            if (lower.Contains("well") && !lower.Contains("dwell")) return "Wells";
            if (lower.Contains("tower") && !lower.Contains("guard") && !lower.Contains("mage") && !lower.Contains("water")) return "GuardTowers";
            
            // NATURE
            if (lower.Contains("tree_oak") || (lower.Contains("tree") && lower.Contains("oak"))) return "TreesOak";
            if (lower.Contains("tree_pine") || (lower.Contains("tree") && lower.Contains("pine"))) return "TreesPine";
            if (lower.Contains("tree_willow") || (lower.Contains("tree") && lower.Contains("willow"))) return "TreesWillow";
            if (lower.Contains("tree_fantasy") || lower.Contains("tree_magic")) return "TreesFantasy";
            if (lower.Contains("tree_dead") || lower.Contains("deadtree")) return "TreesDead";
            if (lower.Contains("tree")) return "TreesOak"; // Default tree category
            if (lower.Contains("bush_small") || lower.Contains("smallbush")) return "BushesSmall";
            if (lower.Contains("bush_large") || lower.Contains("largebush")) return "BushesLarge";
            if (lower.Contains("bush") && lower.Contains("flower")) return "BushesFlower";
            if (lower.Contains("bush")) return "BushesSmall";
            if (lower.Contains("flower") && !lower.Contains("pot")) return "FlowerPatches";
            if (lower.Contains("grass") && lower.Contains("clump")) return "GrassClumps";
            if (lower.Contains("rock_small") || lower.Contains("smallrock")) return "RocksSmall";
            if (lower.Contains("rock_medium") || lower.Contains("mediumrock")) return "RocksMedium";
            if (lower.Contains("rock_large") || lower.Contains("largerock")) return "RocksLarge";
            if (lower.Contains("boulder")) return "Boulders";
            if (lower.Contains("rock")) return "RocksMedium";
            if (lower.Contains("log") && !lower.Contains("dialog")) return "Logs";
            if (lower.Contains("mushroom")) return "Mushrooms";
            if (lower.Contains("stump")) return "Stumps";
            
            // PROPS
            if (lower.Contains("barrel")) return "Barrels";
            if (lower.Contains("crate")) return "Crates";
            if (lower.Contains("sack") || lower.Contains("bag")) return "Sacks";
            if (lower.Contains("chest") && !lower.Contains("breastplate")) return "Chests";
            if (lower.Contains("cart") && !lower.Contains("cart")) return "Carts";
            if (lower.Contains("wagon")) return "Wagons";
            if (lower.Contains("boat") && !lower.Contains("rowboat")) return "Boats";
            if (lower.Contains("bench") && !lower.Contains("work")) return "Benches";
            if (lower.Contains("table") && !lower.Contains("stable")) return "Tables";
            if (lower.Contains("chair")) return "Chairs";
            if (lower.Contains("sign") && !lower.Contains("design")) return "Signs";
            if (lower.Contains("lantern")) return "Lanterns";
            if (lower.Contains("torch") && !lower.Contains("scorched")) return "Torches";
            if (lower.Contains("brazier")) return "Braziers";
            if (lower.Contains("campfire") || lower.Contains("camp_fire")) return "Campfires";
            if (lower.Contains("fence") && lower.Contains("wood")) return "FenceWood";
            if (lower.Contains("fence") && lower.Contains("stone")) return "FenceStone";
            if (lower.Contains("fence")) return "FenceWood";
            if (lower.Contains("hedge")) return "Hedges";
            if (lower.Contains("flag") && !lower.Contains("flagstone")) return "Flags";
            if (lower.Contains("banner")) return "Banners";
            if (lower.Contains("scarecrow")) return "Scarecrows";
            if (lower.Contains("haystack") || lower.Contains("hay_stack")) return "Haystacks";
            
            // CHARACTERS (if present)
            if (lower.Contains("character") || lower.Contains("chr_"))
            {
                if (lower.Contains("guard") || lower.Contains("soldier")) return "Guards";
                if (lower.Contains("knight")) return "Knights";
                if (lower.Contains("peasant") || lower.Contains("farmer")) return "Peasants";
                if (lower.Contains("merchant") || lower.Contains("trader")) return "Merchants";
                if (lower.Contains("noble") || lower.Contains("king") || lower.Contains("queen")) return "Nobles";
            }
            
            // ANIMALS
            if (lower.Contains("chicken") || lower.Contains("hen") || lower.Contains("rooster")) return "Chickens";
            if (lower.Contains("pig") && !lower.Contains("pigeon")) return "Pigs";
            if (lower.Contains("cow")) return "Cows";
            if (lower.Contains("horse") && !lower.Contains("horseshoe")) return "Horses";
            if (lower.Contains("dog")) return "Dogs";
            if (lower.Contains("cat") && !lower.Contains("catapult") && !lower.Contains("cathedral")) return "Cats";
            
            return null; // Uncategorized
        }
        
        private void AssignToLibrary(string category, GameObject[] prefabs)
        {
            if (_targetLibrary == null) return;
            
            Undo.RecordObject(_targetLibrary, "Assign Prefabs");
            
            var field = typeof(FantasyPrefabLibrary).GetField(CategoryToFieldName(category));
            if (field != null)
            {
                field.SetValue(_targetLibrary, prefabs);
                Debug.Log($"Assigned {prefabs.Length} prefabs to {category}");
            }
            else
            {
                Debug.LogWarning($"No field found for category: {category}");
            }
            
            EditorUtility.SetDirty(_targetLibrary);
            AssetDatabase.SaveAssets();
        }
        
        private void AutoAssignAll()
        {
            if (_targetLibrary == null) return;
            
            Undo.RecordObject(_targetLibrary, "Auto-Assign All Prefabs");
            
            foreach (var category in _categorizedPrefabs)
            {
                var field = typeof(FantasyPrefabLibrary).GetField(CategoryToFieldName(category.Key));
                if (field != null)
                {
                    field.SetValue(_targetLibrary, category.Value.ToArray());
                }
            }
            
            EditorUtility.SetDirty(_targetLibrary);
            AssetDatabase.SaveAssets();
            
            _status = "All categories assigned!";
            Debug.Log("Auto-assigned all prefab categories to library");
        }
        
        private string CategoryToFieldName(string category)
        {
            // Convert category name to field name (camelCase)
            if (string.IsNullOrEmpty(category)) return "";
            return char.ToLower(category[0]) + category.Substring(1);
        }
    }
}
#endif
