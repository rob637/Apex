// ============================================================================
// APEX CITADELS - MAIN MENU
// Consolidated, clean menu system for all Apex Citadels features
// ============================================================================
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace ApexCitadels.Editor
{
    /// <summary>
    /// Main menu for Apex Citadels - provides quick access to all features
    /// 
    /// ═══════════════════════════════════════════════════════════════════════
    /// MENU STRUCTURE
    /// ═══════════════════════════════════════════════════════════════════════
    /// 
    /// Apex Citadels/
    /// ├── ▶ Play Fantasy Kingdom        (Standalone procedural world)
    /// ├── ▶ Play GPS Fantasy            (Your real neighborhood!)
    /// │
    /// ├── Scenes/
    /// │   ├── Create GPS Fantasy Scene
    /// │   ├── Open Fantasy Kingdom
    /// │   └── Open Main Menu
    /// │
    /// ├── Materials/
    /// │   ├── Fix Synty Materials
    /// │   ├── Assign Missing Textures
    /// │   └── Repair Broken Materials
    /// │
    /// ├── Setup/
    /// │   ├── One-Click Setup
    /// │   ├── Configure Mapbox API
    /// │   └── Setup Status Dashboard
    /// │
    /// ├── Build/
    /// │   ├── Build Android
    /// │   ├── Build iOS
    /// │   └── Build WebGL
    /// │
    /// └── Help/
    ///     ├── Quick Start Guide
    ///     └── Documentation
    /// 
    /// ═══════════════════════════════════════════════════════════════════════
    /// </summary>
    public static class ApexCitadelsMainMenu
    {
        // ====================================================================
        // PLAY MODES (Top level - most important!)
        // ====================================================================
        
        [MenuItem("Apex Citadels/▶ Play Fantasy Kingdom", false, 0)]
        public static void PlayFantasyKingdom()
        {
            if (!Application.isPlaying)
            {
                string scenePath = "Assets/Scenes/FantasyKingdom.unity";
                if (File.Exists(scenePath))
                {
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    EditorSceneManager.OpenScene(scenePath);
                }
                EditorApplication.isPlaying = true;
            }
        }
        
        [MenuItem("Apex Citadels/▶ Play GPS Fantasy (Your Neighborhood)", false, 1)]
        public static void PlayGPSFantasy()
        {
            string scenePath = "Assets/Scenes/GPSFantasyKingdom.unity";
            
            // Create scene if it doesn't exist
            if (!File.Exists(scenePath))
            {
                CreateGPSFantasyScene();
            }
            else
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(scenePath);
                EditorApplication.isPlaying = true;
            }
        }
        
        // ====================================================================
        // SCENES
        // ====================================================================
        
        [MenuItem("Apex Citadels/Scenes/Create GPS Fantasy Scene", false, 10)]
        public static void CreateGPSFantasyScene()
        {
            string scenePath = "Assets/Scenes/GPSFantasyKingdom.unity";
            
            // Create Scenes folder if needed
            if (!Directory.Exists("Assets/Scenes"))
            {
                Directory.CreateDirectory("Assets/Scenes");
                AssetDatabase.Refresh();
            }
            
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // Add Sun (Directional Light)
            var sunObj = new GameObject("Sun");
            var sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.2f;
            sunObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            
            // Add GPS Fantasy Generator
            var generatorObj = new GameObject("GPSFantasyGenerator");
            var generator = generatorObj.AddComponent<ApexCitadels.FantasyWorld.GPSFantasyGenerator>();
            
            // Set coordinates using SerializedObject
            var serializedGen = new SerializedObject(generator);
            
            // Set to 6709 Reynard Drive, Springfield, VA 22152
            var latProp = serializedGen.FindProperty("latitude");
            var lonProp = serializedGen.FindProperty("longitude");
            if (latProp != null) latProp.doubleValue = 38.7700021;
            if (lonProp != null) lonProp.doubleValue = -77.2481544;
            serializedGen.ApplyModifiedProperties();
            
            // Try to assign prefab library
            var prefabLibrary = AssetDatabase.LoadAssetAtPath<ApexCitadels.FantasyWorld.FantasyPrefabLibrary>(
                "Assets/Resources/MainFantasyPrefabLibrary.asset");
            if (prefabLibrary != null)
            {
                var prefabProp = serializedGen.FindProperty("prefabLibrary");
                if (prefabProp != null)
                {
                    prefabProp.objectReferenceValue = prefabLibrary;
                    serializedGen.ApplyModifiedProperties();
                }
            }
            
            // Add Player (try to find existing prefab)
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/FantasyPlayer.prefab");
            if (playerPrefab == null)
            {
                playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            }
            
            if (playerPrefab != null)
            {
                var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                player.transform.position = new Vector3(0, 1, 0);
                player.tag = "Player";
            }
            else
            {
                // Create basic player
                var playerObj = new GameObject("Player");
                playerObj.tag = "Player";
                playerObj.transform.position = new Vector3(0, 1, 0);
                
                // Add camera
                var camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                var cam = camObj.AddComponent<Camera>();
                camObj.transform.SetParent(playerObj.transform);
                camObj.transform.localPosition = new Vector3(0, 1.6f, 0);
                camObj.AddComponent<AudioListener>();
            }
            
            // Add basic UI
            var canvasObj = new GameObject("UI");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Save scene
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();
            
            Debug.Log($"[ApexMenu] Created GPS Fantasy scene at {scenePath}");
            Debug.Log("[ApexMenu] Location: 6709 Reynard Drive, Springfield, VA (38.7700, -77.2482)");
            
            EditorUtility.DisplayDialog("GPS Fantasy Scene Created", 
                "Scene created with your home location!\n\n" +
                "6709 Reynard Drive, Springfield, VA 22152\n" +
                "Lat: 38.7700, Lon: -77.2482\n\n" +
                "Press Play to see your neighborhood as a fantasy kingdom!", 
                "OK");
        }
        
        [MenuItem("Apex Citadels/Scenes/Open Fantasy Kingdom", false, 11)]
        public static void OpenFantasyKingdom()
        {
            string scenePath = "Assets/Scenes/FantasyKingdom.unity";
            if (File.Exists(scenePath))
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(scenePath);
            }
            else
            {
                EditorUtility.DisplayDialog("Scene Not Found", 
                    "FantasyKingdom scene not found.\nCreate it first or check path.", "OK");
            }
        }
        
        [MenuItem("Apex Citadels/Scenes/Open Main Menu", false, 12)]
        public static void OpenMainMenu()
        {
            string scenePath = "Assets/Scenes/MainMenu.unity";
            if (File.Exists(scenePath))
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(scenePath);
            }
        }
        
        // ====================================================================
        // MATERIAL TOOLS
        // ====================================================================
        
        [MenuItem("Apex Citadels/Materials/Fix Synty Materials (Batch)", false, 20)]
        public static void FixSyntyMaterials()
        {
            EditorApplication.ExecuteMenuItem("Apex Citadels/Advanced/Tools/Synty Material Batch Upgrader");
        }
        
        [MenuItem("Apex Citadels/Materials/Assign Missing Textures", false, 21)]
        public static void AssignMissingTextures()
        {
            EditorApplication.ExecuteMenuItem("Apex Citadels/Advanced/Tools/Synty Texture Assignment Tool");
        }
        
        [MenuItem("Apex Citadels/Materials/Repair Broken Materials", false, 22)]
        public static void RepairMaterials()
        {
            EditorApplication.ExecuteMenuItem("Apex Citadels/Advanced/Tools/Material Repair Tool");
        }
        
        // ====================================================================
        // SETUP (Only show if needed)
        // ====================================================================
        
        [MenuItem("Apex Citadels/Setup/One-Click Setup", false, 30)]
        public static void OneClickSetup()
        {
            EditorApplication.ExecuteMenuItem("Apex Citadels/Advanced/Quick Start/ONE-CLICK SETUP (Start Here!)");
        }
        
        [MenuItem("Apex Citadels/Setup/Configure Mapbox API", false, 31)]
        public static void ConfigureMapbox()
        {
            EditorApplication.ExecuteMenuItem("Apex Citadels/Advanced/PC/Configure Mapbox API");
        }
        
        [MenuItem("Apex Citadels/Setup/Setup Status Dashboard", false, 32)]
        public static void ShowDashboard()
        {
            EditorApplication.ExecuteMenuItem("Apex Citadels/Advanced/Quick Start/Setup Status Dashboard");
        }
        
        // ====================================================================
        // BUILD
        // ====================================================================
        
        [MenuItem("Apex Citadels/Build/Build Android", false, 40)]
        public static void BuildAndroid()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            BuildPipeline.BuildPlayer(GetScenePaths(), "Builds/Android/ApexCitadels.apk", BuildTarget.Android, BuildOptions.None);
        }
        
        [MenuItem("Apex Citadels/Build/Build iOS", false, 41)]
        public static void BuildiOS()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            BuildPipeline.BuildPlayer(GetScenePaths(), "Builds/iOS", BuildTarget.iOS, BuildOptions.None);
        }
        
        [MenuItem("Apex Citadels/Build/Build WebGL", false, 42)]
        public static void BuildWebGL()
        {
            EditorApplication.ExecuteMenuItem("Apex Citadels/Build/Build WebGL");
        }
        
        private static string[] GetScenePaths()
        {
            return new[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/FantasyKingdom.unity",
                "Assets/Scenes/GPSFantasyKingdom.unity"
            };
        }
        
        // ====================================================================
        // HELP
        // ====================================================================
        
        [MenuItem("Apex Citadels/Help/Quick Start Guide", false, 100)]
        public static void ShowQuickStart()
        {
            Application.OpenURL("https://github.com/rob637/Apex/blob/main/QUICKSTART.md");
        }
        
        [MenuItem("Apex Citadels/Help/Documentation", false, 101)]
        public static void ShowDocs()
        {
            string readmePath = Application.dataPath + "/../../docs/README.md";
            if (File.Exists(readmePath))
            {
                EditorUtility.RevealInFinder(readmePath);
            }
        }
    }
}
