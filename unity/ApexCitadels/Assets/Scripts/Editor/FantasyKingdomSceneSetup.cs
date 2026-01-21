// ============================================================================
// APEX CITADELS - FANTASY KINGDOM ONE-CLICK SCENE SETUP
// Editor script to create and configure the Fantasy Kingdom scene
// ============================================================================
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

namespace ApexCitadels.FantasyWorld.Editor
{
    /// <summary>
    /// One-click setup for Fantasy Kingdom scene
    /// Creates all required GameObjects, components, and configuration
    /// </summary>
    public class FantasyKingdomSceneSetup : EditorWindow
    {
        private bool createNewScene = true;
        private bool setupLighting = true;
        private bool setupPostProcessing = true;
        private bool setupPlayer = true;
        private string sceneName = "FantasyKingdom";
        
        [MenuItem("Apex Citadels/Create Fantasy Kingdom Scene", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<FantasyKingdomSceneSetup>("Fantasy Kingdom Setup");
            window.minSize = new Vector2(400, 350);
            window.maxSize = new Vector2(400, 350);
        }
        
        [MenuItem("Apex Citadels/Quick Setup Fantasy Kingdom (One Click)", false, 101)]
        public static void QuickSetup()
        {
            if (EditorUtility.DisplayDialog("Create Fantasy Kingdom Scene",
                "This will create a new Fantasy Kingdom scene with all components configured.\n\n" +
                "• New scene will be created\n" +
                "• Lighting and post-processing configured\n" +
                "• Player controller added\n" +
                "• World generator ready to go\n\n" +
                "Continue?", "Create Scene", "Cancel"))
            {
                CreateFantasyKingdomScene(true, true, true, true, "FantasyKingdom");
            }
        }
        
        private void OnGUI()
        {
            GUILayout.Space(10);
            
            // Header
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("⚔ Fantasy Kingdom Setup ⚔", headerStyle);
            
            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Create a complete Fantasy Kingdom scene with one click!\n" +
                "All components will be configured and ready to play.",
                MessageType.Info);
            
            GUILayout.Space(15);
            
            // Options
            EditorGUILayout.LabelField("Scene Options", EditorStyles.boldLabel);
            sceneName = EditorGUILayout.TextField("Scene Name", sceneName);
            createNewScene = EditorGUILayout.Toggle("Create New Scene", createNewScene);
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Components", EditorStyles.boldLabel);
            setupLighting = EditorGUILayout.Toggle("Setup Lighting", setupLighting);
            setupPostProcessing = EditorGUILayout.Toggle("Setup Post Processing", setupPostProcessing);
            setupPlayer = EditorGUILayout.Toggle("Setup Player Controller", setupPlayer);
            
            GUILayout.Space(20);
            
            // Create Button
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("🏰 Create Fantasy Kingdom Scene 🏰", GUILayout.Height(40)))
            {
                CreateFantasyKingdomScene(createNewScene, setupLighting, setupPostProcessing, setupPlayer, sceneName);
                Close();
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(10);
            
            // Info
            EditorGUILayout.HelpBox(
                "After creation:\n" +
                "1. Press Play to generate the kingdom\n" +
                "2. Use WASD to move, Mouse to look\n" +
                "3. Press V to toggle 1st/3rd person view",
                MessageType.None);
        }
        
        private static void CreateFantasyKingdomScene(bool newScene, bool lighting, bool postProcess, bool player, string name)
        {
            Debug.Log("[FantasyKingdom] Starting scene setup...");
            
            // Create new scene or use current
            if (newScene)
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene.name = name;
            }
            else
            {
                // Clear existing scene objects (optional)
            }
            
            // Ensure required folders exist
            EnsureFolderExists("Assets/Scenes/FantasyKingdom");
            EnsureFolderExists("Assets/Resources");
            
            // Create config if needed
            var config = CreateOrGetConfig();
            var prefabLibrary = FindPrefabLibrary();
            
            // Create scene hierarchy
            CreateKingdomManager(config, prefabLibrary);
            
            if (lighting)
            {
                CreateLighting();
            }
            
            if (postProcess)
            {
                CreatePostProcessing();
            }
            
            if (player)
            {
                CreatePlayer();
            }
            
            // Save scene
            string scenePath = $"Assets/Scenes/FantasyKingdom/{name}.unity";
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
            
            Debug.Log($"[FantasyKingdom] Scene created and saved to: {scenePath}");
            Debug.Log("[FantasyKingdom] Press Play to generate your kingdom!");
            
            // Show completion dialog
            EditorUtility.DisplayDialog("Fantasy Kingdom Created!",
                $"Scene saved to:\n{scenePath}\n\n" +
                "Press Play to generate your kingdom!\n\n" +
                "Controls:\n" +
                "• WASD - Move\n" +
                "• Mouse - Look\n" +
                "• Shift - Run\n" +
                "• Space - Jump\n" +
                "• V - Toggle View Mode",
                "Let's Go!");
        }
        
        private static void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] parts = path.Split('/');
                string currentPath = parts[0];
                
                for (int i = 1; i < parts.Length; i++)
                {
                    string newPath = currentPath + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, parts[i]);
                    }
                    currentPath = newPath;
                }
            }
        }
        
        private static FantasyKingdomConfig CreateOrGetConfig()
        {
            // Try to find existing config
            string[] guids = AssetDatabase.FindAssets("t:FantasyKingdomConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var existing = AssetDatabase.LoadAssetAtPath<FantasyKingdomConfig>(path);
                if (existing != null)
                {
                    Debug.Log($"[FantasyKingdom] Using existing config: {path}");
                    return existing;
                }
            }
            
            // Create new config
            var config = ScriptableObject.CreateInstance<FantasyKingdomConfig>();
            
            // Set nice defaults
            config.kingdomSize = 500f;
            config.townRadius = 150f;
            config.generateHills = true;
            config.hillHeight = 15f;
            config.generateCastle = true;
            config.generateWalls = true;
            config.wallRadius = 120f;
            config.wallTowerCount = 8;
            config.residentialCount = 40;
            config.commercialCount = 15;
            config.militaryCount = 5;
            config.religiousCount = 2;
            config.industrialCount = 8;
            config.generateRoads = true;
            config.mainRoadWidth = 8f;
            config.sideRoadWidth = 4f;
            config.treeCount = 200;
            config.bushCount = 80;
            config.forestStartRadius = 160f;
            config.propCount = 100;
            config.generateFountain = true;
            config.generateMarketSquare = true;
            config.generateTorches = true;
            config.torchCount = 30;
            config.objectsPerFrame = 10;
            
            string configPath = "Assets/Resources/FantasyKingdomConfig.asset";
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[FantasyKingdom] Created config: {configPath}");
            return config;
        }
        
        private static FantasyPrefabLibrary FindPrefabLibrary()
        {
            // Try to find MainFantasyPrefabLibrary
            string[] guids = AssetDatabase.FindAssets("MainFantasyPrefabLibrary t:FantasyPrefabLibrary");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var library = AssetDatabase.LoadAssetAtPath<FantasyPrefabLibrary>(path);
                if (library != null)
                {
                    Debug.Log($"[FantasyKingdom] Found prefab library: {path}");
                    return library;
                }
            }
            
            // Try any prefab library
            guids = AssetDatabase.FindAssets("t:FantasyPrefabLibrary");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var library = AssetDatabase.LoadAssetAtPath<FantasyPrefabLibrary>(path);
                if (library != null)
                {
                    Debug.Log($"[FantasyKingdom] Found prefab library: {path}");
                    return library;
                }
            }
            
            Debug.LogWarning("[FantasyKingdom] No prefab library found! Please assign manually.");
            return null;
        }
        
        private static void CreateKingdomManager(FantasyKingdomConfig config, FantasyPrefabLibrary prefabLibrary)
        {
            // Create main manager object
            var manager = new GameObject("FantasyKingdomManager");
            
            // Add StandaloneFantasyGenerator
            var generator = manager.AddComponent<StandaloneFantasyGenerator>();
            
            // Use SerializedObject to set private serialized fields
            var serializedGenerator = new SerializedObject(generator);
            var configField = serializedGenerator.FindProperty("config");
            var libraryField = serializedGenerator.FindProperty("prefabLibrary");
            
            if (configField != null)
                configField.objectReferenceValue = config;
            if (libraryField != null)
                libraryField.objectReferenceValue = prefabLibrary;
            
            serializedGenerator.ApplyModifiedPropertiesWithoutUndo();
            
            // Add FantasyKingdomController
            var controller = manager.AddComponent<FantasyKingdomController>();
            
            // Set controller references
            var serializedController = new SerializedObject(controller);
            var genField = serializedController.FindProperty("worldGenerator");
            if (genField != null)
                genField.objectReferenceValue = generator;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            
            // Add UI Setup
            manager.AddComponent<FantasyKingdomUISetup>();
            
            Debug.Log("[FantasyKingdom] Created FantasyKingdomManager with all components");
        }
        
        private static void CreateLighting()
        {
            // Create Directional Light (Sun)
            var sunObj = new GameObject("Sun");
            var light = sunObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.84f); // Warm sunlight
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.8f;
            sunObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            
            // Try to add URP additional light data
            var additionalData = sunObj.AddComponent<UniversalAdditionalLightData>();
            if (additionalData != null)
            {
                additionalData.shadowBias = 0.05f;
            }
            
            // Set ambient lighting via RenderSettings
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.53f, 0.7f, 1f);      // Light blue sky
            RenderSettings.ambientEquatorColor = new Color(0.78f, 0.78f, 0.7f); // Warm horizon
            RenderSettings.ambientGroundColor = new Color(0.27f, 0.2f, 0.16f);  // Brown ground
            
            // Fog for atmosphere
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.7f, 0.75f, 0.85f);
            RenderSettings.fogStartDistance = 100f;
            RenderSettings.fogEndDistance = 400f;
            
            Debug.Log("[FantasyKingdom] Created lighting setup");
        }
        
        private static void CreatePostProcessing()
        {
            // Create Volume object
            var volumeObj = new GameObject("PostProcessing");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1;
            
            // Create profile
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            
            // Bloom
            var bloom = profile.Add<Bloom>();
            bloom.active = true;
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.3f);
            bloom.scatter.Override(0.7f);
            
            // Color Adjustments
            var colorAdjust = profile.Add<ColorAdjustments>();
            colorAdjust.active = true;
            colorAdjust.saturation.Override(10f);
            colorAdjust.contrast.Override(5f);
            
            // Vignette
            var vignette = profile.Add<Vignette>();
            vignette.active = true;
            vignette.intensity.Override(0.25f);
            vignette.smoothness.Override(0.4f);
            
            // Save profile
            string profilePath = "Assets/Scenes/FantasyKingdom/FantasyKingdomPostProcess.asset";
            EnsureFolderExists("Assets/Scenes/FantasyKingdom");
            AssetDatabase.CreateAsset(profile, profilePath);
            
            volume.profile = profile;
            
            Debug.Log("[FantasyKingdom] Created post-processing setup");
        }
        
        private static void CreatePlayer()
        {
            // Create Player object
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0, 1, 50);
            player.transform.rotation = Quaternion.Euler(0, 180, 0);
            player.tag = "Player";
            
            // Add CharacterController
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.4f;
            
            // Add FantasyPlayerController
            var playerController = player.AddComponent<FantasyPlayerController>();
            
            // Create Camera
            var cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            cameraObj.transform.SetParent(player.transform);
            cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0);
            cameraObj.transform.localRotation = Quaternion.identity;
            
            var camera = cameraObj.AddComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            camera.fieldOfView = 70f;
            
            // Add URP camera data
            var cameraData = cameraObj.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            
            // Add AudioListener
            cameraObj.AddComponent<AudioListener>();
            
            // Set camera reference in player controller
            var serializedPlayer = new SerializedObject(playerController);
            var cameraField = serializedPlayer.FindProperty("playerCamera");
            if (cameraField != null)
                cameraField.objectReferenceValue = camera;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
            
            // Link player to kingdom controller
            var controller = Object.FindAnyObjectByType<FantasyKingdomController>();
            if (controller != null)
            {
                var serializedController = new SerializedObject(controller);
                var playerField = serializedController.FindProperty("playerController");
                var mainCamField = serializedController.FindProperty("mainCamera");
                
                if (playerField != null)
                    playerField.objectReferenceValue = playerController;
                if (mainCamField != null)
                    mainCamField.objectReferenceValue = camera;
                
                serializedController.ApplyModifiedPropertiesWithoutUndo();
            }
            
            Debug.Log("[FantasyKingdom] Created player with camera");
        }
    }
}
