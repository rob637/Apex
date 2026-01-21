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
        private bool populatePaths = true;
        private bool populateTrees = false;
        private bool populateBushes = false;
        private bool populateProps = false;
        
        [MenuItem("Apex Citadels/Populate Prefab Library")]
        public static void ShowWindow()
        {
            GetWindow<PrefabLibraryPopulator>("Prefab Populator");
        }
        
        private void OnGUI()
        {
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
            
            populatePaths = EditorGUILayout.Toggle("Cobblestone Paths", populatePaths);
            populateTrees = EditorGUILayout.Toggle("Trees", populateTrees);
            populateBushes = EditorGUILayout.Toggle("Bushes", populateBushes);
            populateProps = EditorGUILayout.Toggle("Props (Barrels, etc.)", populateProps);
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("Populate Selected Categories", GUILayout.Height(40)))
            {
                if (targetLibrary != null)
                {
                    PopulateLibrary();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please select a target FantasyPrefabLibrary asset.", "OK");
                }
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Populate Paths Only (Quick)", GUILayout.Height(30)))
            {
                if (targetLibrary != null)
                {
                    PopulatePathsOnly();
                }
            }
        }
        
        private void PopulateLibrary()
        {
            Undo.RecordObject(targetLibrary, "Populate Prefab Library");
            
            int totalAdded = 0;
            
            if (populatePaths)
            {
                totalAdded += PopulateCobblestoneSegments();
                totalAdded += PopulateDirtPaths();
            }
            
            if (populateTrees)
            {
                totalAdded += PopulateTrees();
            }
            
            if (populateBushes)
            {
                totalAdded += PopulateBushes();
            }
            
            if (populateProps)
            {
                totalAdded += PopulateProps();
            }
            
            EditorUtility.SetDirty(targetLibrary);
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("Complete", $"Added {totalAdded} prefabs to the library.", "OK");
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
        
        private int PopulateCobblestoneSegments()
        {
            var prefabs = new List<GameObject>();
            
            // Find cobblestone/brick path prefabs from PolygonFantasyKingdom
            string[] searchPaths = new[]
            {
                "Assets/Synty/PolygonFantasyKingdom/Prefabs/Environments",
                "Assets/Synty/PolygonFantasyKingdom/Prefabs/Props/Paths"
            };
            
            string[] searchPatterns = new[]
            {
                "SM_Env_Path_Brick",
                "SM_Env_Path_01",
                "SM_Env_Path_02",
                "SM_Env_Path_03",
                "SM_Env_Path_04",
                "SM_Env_Path_05",
                "SM_Prop_Path_Brick"
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
        
        private int PopulateTrees()
        {
            // Placeholder - implement as needed
            return 0;
        }
        
        private int PopulateBushes()
        {
            // Placeholder - implement as needed
            return 0;
        }
        
        private int PopulateProps()
        {
            // Placeholder - implement as needed
            return 0;
        }
    }
}
#endif
