// ============================================================================
// APEX CITADELS - FANTASY WORLD DIAGNOSTIC
// Run this to check if everything is set up correctly
// Unity Menu: Apex Citadels > Diagnose Fantasy World Setup
// ============================================================================
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace ApexCitadels.Editor
{
    public class FantasyWorldDiagnostic : EditorWindow
    {
        private Vector2 _scrollPos;
        private List<DiagnosticResult> _results = new List<DiagnosticResult>();
        
        private class DiagnosticResult
        {
            public string Category;
            public string Check;
            public bool Passed;
            public string Details;
            public string FixAction;
        }
        
        [MenuItem("Apex Citadels/Diagnose Fantasy World Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<FantasyWorldDiagnostic>("Fantasy World Diagnostic");
            window.minSize = new Vector2(600, 500);
            window.RunDiagnostics();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🔍 Fantasy World Setup Diagnostic", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Run Diagnostic", GUILayout.Height(30)))
            {
                RunDiagnostics();
            }
            
            EditorGUILayout.Space(10);
            
            // Summary
            int passed = _results.Count(r => r.Passed);
            int failed = _results.Count(r => !r.Passed);
            
            EditorGUILayout.BeginHorizontal();
            GUI.color = Color.green;
            EditorGUILayout.LabelField($"✅ Passed: {passed}", EditorStyles.boldLabel);
            GUI.color = failed > 0 ? Color.red : Color.green;
            EditorGUILayout.LabelField($"❌ Failed: {failed}", EditorStyles.boldLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            string currentCategory = "";
            foreach (var result in _results)
            {
                if (result.Category != currentCategory)
                {
                    currentCategory = result.Category;
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField($"=== {currentCategory} ===", EditorStyles.boldLabel);
                }
                
                EditorGUILayout.BeginHorizontal("box");
                
                GUI.color = result.Passed ? Color.green : Color.red;
                EditorGUILayout.LabelField(result.Passed ? "✅" : "❌", GUILayout.Width(25));
                GUI.color = Color.white;
                
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(result.Check, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(result.Details, EditorStyles.wordWrappedMiniLabel);
                
                if (!result.Passed && !string.IsNullOrEmpty(result.FixAction))
                {
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField($"FIX: {result.FixAction}", EditorStyles.wordWrappedMiniLabel);
                    GUI.color = Color.white;
                }
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            
            // Critical actions
            if (failed > 0)
            {
                EditorGUILayout.LabelField("🚨 CRITICAL ACTIONS NEEDED:", EditorStyles.boldLabel);
                
                foreach (var result in _results.Where(r => !r.Passed))
                {
                    if (!string.IsNullOrEmpty(result.FixAction))
                    {
                        EditorGUILayout.LabelField($"• {result.FixAction}", EditorStyles.wordWrappedLabel);
                    }
                }
            }
        }
        
        private void RunDiagnostics()
        {
            _results.Clear();
            
            // === SYNTY ASSETS ===
            CheckSyntyAssets();
            
            // === MAPBOX ===
            CheckMapboxConfiguration();
            
            // === PREFAB LIBRARY ===
            CheckPrefabLibrary();
            
            // === SCENE SETUP ===
            CheckSceneSetup();
            
            // === URP ===
            CheckURPSetup();
            
            Repaint();
        }
        
        private void CheckSyntyAssets()
        {
            // Find all prefabs in project
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int syntyCount = 0;
            List<string> syntyFolders = new List<string>();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("POLYGON") || path.Contains("Synty"))
                {
                    syntyCount++;
                    string folder = Path.GetDirectoryName(path);
                    if (!syntyFolders.Contains(folder))
                        syntyFolders.Add(folder);
                }
            }
            
            _results.Add(new DiagnosticResult
            {
                Category = "SYNTY ASSETS",
                Check = "Synty POLYGON Prefabs Found",
                Passed = syntyCount > 100, // Expecting hundreds
                Details = syntyCount > 0 
                    ? $"Found {syntyCount} Synty prefabs in {syntyFolders.Count} folders:\n{string.Join("\n", syntyFolders.Take(5))}"
                    : "NO SYNTY PREFABS FOUND IN PROJECT!",
                FixAction = syntyCount == 0 
                    ? "Import Synty .unitypackage files: Assets > Import Package > Custom Package"
                    : syntyCount < 100 
                        ? "Some Synty packs may be missing. Check Unity Asset Store downloads."
                        : ""
            });
            
            // Check for common Synty folders
            string[] expectedFolders = new[]
            {
                "PolygonFantasyKingdom",
                "PolygonTown", 
                "PolygonVikings",
                "PolygonKnights",
                "PolygonAdventure",
                "PolygonNature"
            };
            
            List<string> foundPacks = new List<string>();
            List<string> missingPacks = new List<string>();
            
            foreach (var folder in expectedFolders)
            {
                if (AssetDatabase.IsValidFolder($"Assets/{folder}") || 
                    AssetDatabase.IsValidFolder($"Assets/Synty/{folder}") ||
                    AssetDatabase.IsValidFolder($"Assets/Art/{folder}"))
                {
                    foundPacks.Add(folder);
                }
                else
                {
                    missingPacks.Add(folder);
                }
            }
            
            _results.Add(new DiagnosticResult
            {
                Category = "SYNTY ASSETS",
                Check = "Synty Asset Packs Installed",
                Passed = foundPacks.Count >= 3,
                Details = foundPacks.Count > 0 
                    ? $"Found: {string.Join(", ", foundPacks)}\nMissing: {string.Join(", ", missingPacks)}"
                    : "No Synty pack folders found. Expected folders like 'PolygonFantasyKingdom'",
                FixAction = foundPacks.Count < 3 
                    ? "Download Synty packs from Unity Asset Store: Window > Package Manager > My Assets"
                    : ""
            });
        }
        
        private void CheckMapboxConfiguration()
        {
            var config = UnityEngine.Resources.Load<ApexCitadels.Map.MapboxConfiguration>("MapboxConfig");
            
            _results.Add(new DiagnosticResult
            {
                Category = "MAPBOX",
                Check = "MapboxConfig.asset exists",
                Passed = config != null,
                Details = config != null 
                    ? $"Found at: Resources/MapboxConfig.asset"
                    : "MapboxConfig.asset not found in Resources folder!",
                FixAction = config == null 
                    ? "Menu: Apex Citadels > PC > Configure Mapbox API"
                    : ""
            });
            
            if (config != null)
            {
                bool hasToken = !string.IsNullOrEmpty(config.AccessToken) && 
                               config.AccessToken.StartsWith("pk.");
                
                _results.Add(new DiagnosticResult
                {
                    Category = "MAPBOX",
                    Check = "Mapbox Access Token Valid",
                    Passed = hasToken,
                    Details = hasToken 
                        ? $"Token starts with: {config.AccessToken.Substring(0, 20)}..."
                        : "No valid token found! Token should start with 'pk.'",
                    FixAction = hasToken ? "" : "Get token from mapbox.com > Account > Access Tokens"
                });
                
                _results.Add(new DiagnosticResult
                {
                    Category = "MAPBOX",
                    Check = "Mapbox Location Set",
                    Passed = config.DefaultLatitude != 0 && config.DefaultLongitude != 0,
                    Details = $"Lat: {config.DefaultLatitude}, Lon: {config.DefaultLongitude}",
                    FixAction = ""
                });
            }
        }
        
        private void CheckPrefabLibrary()
        {
            // Find all FantasyPrefabLibrary assets
            string[] guids = AssetDatabase.FindAssets("t:FantasyPrefabLibrary");
            
            _results.Add(new DiagnosticResult
            {
                Category = "PREFAB LIBRARY",
                Check = "FantasyPrefabLibrary Asset Exists",
                Passed = guids.Length > 0,
                Details = guids.Length > 0 
                    ? $"Found {guids.Length} library asset(s)"
                    : "No FantasyPrefabLibrary asset found!",
                FixAction = guids.Length == 0 
                    ? "Create: Right-click Project > Create > Apex Citadels > Fantasy Prefab Library"
                    : ""
            });
            
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var library = AssetDatabase.LoadAssetAtPath<FantasyWorld.FantasyPrefabLibrary>(path);
                
                if (library != null)
                {
                    int totalPrefabs = 0;
                    int filledArrays = 0;
                    
                    // Check building arrays
                    if (library.peasantHuts?.Length > 0) { totalPrefabs += library.peasantHuts.Length; filledArrays++; }
                    if (library.cottages?.Length > 0) { totalPrefabs += library.cottages.Length; filledArrays++; }
                    if (library.houses?.Length > 0) { totalPrefabs += library.houses.Length; filledArrays++; }
                    if (library.taverns?.Length > 0) { totalPrefabs += library.taverns.Length; filledArrays++; }
                    if (library.castleParts?.Length > 0) { totalPrefabs += library.castleParts.Length; filledArrays++; }
                    
                    // Check nature arrays
                    if (library.treesOak?.Length > 0) { totalPrefabs += library.treesOak.Length; filledArrays++; }
                    if (library.treesPine?.Length > 0) { totalPrefabs += library.treesPine.Length; filledArrays++; }
                    if (library.bushesSmall?.Length > 0) { totalPrefabs += library.bushesSmall.Length; filledArrays++; }
                    
                    _results.Add(new DiagnosticResult
                    {
                        Category = "PREFAB LIBRARY",
                        Check = "Prefab Library Has Prefabs Assigned",
                        Passed = totalPrefabs > 20,
                        Details = $"Found {totalPrefabs} prefabs in {filledArrays} categories",
                        FixAction = totalPrefabs == 0 
                            ? "Menu: Apex Citadels > Synty Prefab Scanner > Click 'Auto-Assign All Categories'"
                            : totalPrefabs < 20 
                                ? "Some categories are empty. Run Synty Scanner again."
                                : ""
                    });
                }
            }
        }
        
        private void CheckSceneSetup()
        {
            // Check if FantasyWorldDemo scene is open
            var demo = FindFirstObjectByType<FantasyWorld.FantasyWorldDemo>();
            
            _results.Add(new DiagnosticResult
            {
                Category = "SCENE SETUP",
                Check = "FantasyWorldDemo Component Present",
                Passed = demo != null,
                Details = demo != null 
                    ? $"Found on: {demo.gameObject.name}"
                    : "No FantasyWorldDemo in scene!",
                FixAction = demo == null 
                    ? "Open scene: Assets/Scenes/FantasyWorldTest.unity"
                    : ""
            });
            
            if (demo != null)
            {
                var generator = demo.GetComponent<FantasyWorld.FantasyWorldGenerator>();
                
                _results.Add(new DiagnosticResult
                {
                    Category = "SCENE SETUP",
                    Check = "Prefab Library Assigned to Generator",
                    Passed = generator != null && generator.prefabLibrary != null,
                    Details = generator?.prefabLibrary != null 
                        ? $"Library: {generator.prefabLibrary.name}"
                        : "No prefab library assigned to generator!",
                    FixAction = generator?.prefabLibrary == null 
                        ? "Select FantasyWorldDemo object, drag FantasyPrefabLibrary asset to 'Prefab Library' field"
                        : ""
                });
            }
            
            // Check for MapboxTileRenderer
            var mapbox = FindFirstObjectByType<ApexCitadels.Map.MapboxTileRenderer>();
            
            _results.Add(new DiagnosticResult
            {
                Category = "SCENE SETUP",
                Check = "MapboxTileRenderer In Scene",
                Passed = mapbox != null,
                Details = mapbox != null 
                    ? $"Found: {mapbox.gameObject.name}"
                    : "No MapboxTileRenderer - map tiles won't load!",
                FixAction = ""
            });
        }
        
        private void CheckURPSetup()
        {
            // Check if URP is the active pipeline
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            
            _results.Add(new DiagnosticResult
            {
                Category = "RENDERING",
                Check = "Universal Render Pipeline Active",
                Passed = pipeline != null && pipeline.name.Contains("Universal"),
                Details = pipeline != null 
                    ? $"Active pipeline: {pipeline.name}"
                    : "No render pipeline configured!",
                FixAction = pipeline == null 
                    ? "Project Settings > Graphics > Set URP Asset"
                    : ""
            });
            
            // Check for URP Lit shader
            var litShader = Shader.Find("Universal Render Pipeline/Lit");
            
            _results.Add(new DiagnosticResult
            {
                Category = "RENDERING",
                Check = "URP Lit Shader Available",
                Passed = litShader != null,
                Details = litShader != null ? "URP Lit shader found" : "URP Lit shader not available!",
                FixAction = ""
            });
        }
    }
}
#endif
