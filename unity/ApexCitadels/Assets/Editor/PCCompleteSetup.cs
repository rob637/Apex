using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.IO;

namespace ApexCitadels.PC.Editor
{
    /// <summary>
    /// ONE-CLICK COMPLETE SETUP for PC Scene.
    /// Run from: Window → Apex Citadels → Complete PC Setup
    /// 
    /// This script does EVERYTHING:
    /// 1. Creates PCMain scene
    /// 2. Sets up Camera with correct settings
    /// 3. Creates Directional Light
    /// 4. Configures Render/Ambient settings
    /// 5. Creates all required manager objects
    /// 6. Wires up all references
    /// 7. Saves the scene
    /// </summary>
    public class PCCompleteSetup : EditorWindow
    {
        private static string SCENE_PATH = "Assets/Scenes/PCMain.unity";
        private static string SCENES_FOLDER = "Assets/Scenes";
        
        private bool createNewScene = true;
        private bool setupCamera = true;
        private bool setupLighting = true;
        private bool setupManagers = true;
        private bool setupUI = true;
        private bool autoSave = true;
        
        private Vector2 scrollPos;

        [MenuItem("Window/Apex Citadels/Complete PC Setup", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<PCCompleteSetup>("PC Complete Setup");
            window.minSize = new Vector2(400, 500);
        }

        [MenuItem("Window/Apex Citadels/One-Click Setup (Recommended)", false, 0)]
        public static void OneClickSetup()
        {
            if (EditorUtility.DisplayDialog("Complete PC Setup",
                "This will create and configure the entire PC scene automatically.\n\n" +
                "• Create PCMain.unity scene\n" +
                "• Set up Camera (position, sky color, controller)\n" +
                "• Create Directional Light (sun)\n" +
                "• Configure ambient lighting\n" +
                "• Create all manager objects\n" +
                "• Set up UI Canvas\n" +
                "• Save everything\n\n" +
                "Continue?", "Yes, Set Up Everything", "Cancel"))
            {
                RunCompleteSetup();
            }
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            EditorGUILayout.LabelField("🏰 Apex Citadels - Complete PC Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "This tool creates and configures the entire PC scene in one click.\n" +
                "All managers, lighting, camera, and UI will be set up automatically.",
                MessageType.Info);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            
            createNewScene = EditorGUILayout.Toggle("Create/Reset Scene", createNewScene);
            setupCamera = EditorGUILayout.Toggle("Setup Camera", setupCamera);
            setupLighting = EditorGUILayout.Toggle("Setup Lighting", setupLighting);
            setupManagers = EditorGUILayout.Toggle("Setup Managers", setupManagers);
            setupUI = EditorGUILayout.Toggle("Setup UI", setupUI);
            autoSave = EditorGUILayout.Toggle("Auto-Save Scene", autoSave);
            
            EditorGUILayout.Space();
            
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("▶ RUN COMPLETE SETUP", GUILayout.Height(40)))
            {
                RunCompleteSetup();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Individual Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Just Fix Current Scene"))
            {
                FixCurrentScene();
            }
            
            if (GUILayout.Button("Open Scene Diagnostic"))
            {
                PCSceneDiagnostic.ShowWindow();
            }
            
            EditorGUILayout.EndScrollView();
        }

        public static void RunCompleteSetup()
        {
            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("  APEX CITADELS - COMPLETE PC SETUP STARTING");
            Debug.Log("═══════════════════════════════════════════════════════════");

            try
            {
                // Step 1: Create or open scene
                CreateOrOpenScene();
                
                // Step 2: Clear existing objects (optional fresh start)
                // We don't clear - we add what's missing
                
                // Step 3: Setup Camera
                SetupCamera();
                
                // Step 4: Setup Lighting
                SetupLighting();
                
                // Step 5: Setup Render Settings
                SetupRenderSettings();
                
                // Step 6: Setup All Managers
                SetupManagers();
                
                // Step 7: Setup UI
                SetupUI();
                
                // Step 8: Wire up references
                WireUpReferences();
                
                // Step 9: Save Scene
                SaveScene();
                
                Debug.Log("═══════════════════════════════════════════════════════════");
                Debug.Log("  ✅ SETUP COMPLETE! Press Play to test.");
                Debug.Log("═══════════════════════════════════════════════════════════");
                
                EditorUtility.DisplayDialog("Setup Complete!", 
                    "PC Scene has been fully configured!\n\n" +
                    "✅ Camera configured\n" +
                    "✅ Lighting created\n" +
                    "✅ Managers created\n" +
                    "✅ UI set up\n" +
                    "✅ Scene saved\n\n" +
                    "Press PLAY to test!", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Setup failed: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Setup Failed", 
                    $"An error occurred:\n{ex.Message}\n\nCheck console for details.", "OK");
            }
        }

        private static void CreateOrOpenScene()
        {
            Debug.Log("[Setup] Step 1: Creating/Opening scene...");
            
            // Ensure Scenes folder exists
            if (!AssetDatabase.IsValidFolder(SCENES_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            
            // Check if scene exists
            if (File.Exists(SCENE_PATH))
            {
                Debug.Log($"[Setup] Opening existing scene: {SCENE_PATH}");
                EditorSceneManager.OpenScene(SCENE_PATH);
            }
            else
            {
                Debug.Log($"[Setup] Creating new scene: {SCENE_PATH}");
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(newScene, SCENE_PATH);
            }
        }

        private static void SetupCamera()
        {
            Debug.Log("[Setup] Step 2: Setting up camera...");
            
            // Find or create main camera
            Camera mainCam = Camera.main;
            GameObject camObj;
            
            if (mainCam == null)
            {
                // Look for any camera
                mainCam = Object.FindFirstObjectByType<Camera>();
            }
            
            if (mainCam != null)
            {
                camObj = mainCam.gameObject;
                Debug.Log($"[Setup] Using existing camera: {camObj.name}");
            }
            else
            {
                camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                Debug.Log("[Setup] Created new Main Camera");
            }
            
            // Configure camera
            camObj.tag = "MainCamera";
            camObj.name = "Main Camera";
            
            // Position for world map view
            camObj.transform.position = new Vector3(0, 200, -100);
            camObj.transform.rotation = Quaternion.Euler(70, 0, 0);
            
            // Camera settings
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.5f, 0.7f, 1f); // Nice blue sky
            mainCam.nearClipPlane = 0.3f;
            mainCam.farClipPlane = 5000f;
            mainCam.fieldOfView = 60f;
            
            // Add audio listener if missing
            if (camObj.GetComponent<AudioListener>() == null)
            {
                camObj.AddComponent<AudioListener>();
            }
            
            // Add PCCameraController if missing
            if (camObj.GetComponent<PCCameraController>() == null)
            {
                camObj.AddComponent<PCCameraController>();
                Debug.Log("[Setup] Added PCCameraController");
            }
            
            Debug.Log("[Setup] ✅ Camera configured");
        }

        private static void SetupLighting()
        {
            Debug.Log("[Setup] Step 3: Setting up lighting...");
            
            // Find or create directional light
            Light sunLight = null;
            Light[] allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            
            foreach (var light in allLights)
            {
                if (light.type == LightType.Directional)
                {
                    sunLight = light;
                    break;
                }
            }
            
            GameObject sunObj;
            if (sunLight != null)
            {
                sunObj = sunLight.gameObject;
                Debug.Log($"[Setup] Using existing light: {sunObj.name}");
            }
            else
            {
                sunObj = new GameObject("Directional Light");
                sunLight = sunObj.AddComponent<Light>();
                Debug.Log("[Setup] Created new Directional Light");
            }
            
            // Configure light
            sunObj.name = "Directional Light";
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1f, 0.96f, 0.88f); // Warm sunlight
            sunLight.intensity = 1.2f;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.7f;
            sunLight.shadowBias = 0.05f;
            sunLight.shadowNormalBias = 0.4f;
            
            // Position for nice lighting angle
            sunObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            
            // Add DayNightCycle if missing
            if (sunObj.GetComponent<DayNightCycle>() == null)
            {
                sunObj.AddComponent<DayNightCycle>();
                Debug.Log("[Setup] Added DayNightCycle");
            }
            
            Debug.Log("[Setup] ✅ Lighting configured");
        }

        private static void SetupRenderSettings()
        {
            Debug.Log("[Setup] Step 4: Setting up render settings...");
            
            // Ambient lighting
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.7f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.55f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.35f, 0.3f);
            RenderSettings.ambientIntensity = 1f;
            
            // Fog (optional, disabled for now)
            RenderSettings.fog = false;
            
            // Skybox (none - using solid color)
            RenderSettings.skybox = null;
            
            // Reflection
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.5f;
            
            Debug.Log("[Setup] ✅ Render settings configured");
        }

        private static void SetupManagers()
        {
            Debug.Log("[Setup] Step 5: Setting up managers...");
            
            // PCGameController
            if (Object.FindFirstObjectByType<PCGameController>() == null)
            {
                var obj = new GameObject("PCGameController");
                obj.AddComponent<PCGameController>();
                Debug.Log("[Setup] Created PCGameController");
            }
            
            // PCInputManager
            if (Object.FindFirstObjectByType<PCInputManager>() == null)
            {
                var obj = new GameObject("PCInputManager");
                obj.AddComponent<PCInputManager>();
                Debug.Log("[Setup] Created PCInputManager");
            }
            
            // WorldMapRenderer
            if (Object.FindFirstObjectByType<WorldMapRenderer>() == null)
            {
                var obj = new GameObject("WorldMapRenderer");
                obj.AddComponent<WorldMapRenderer>();
                Debug.Log("[Setup] Created WorldMapRenderer");
            }
            
            // BaseEditor
            if (Object.FindFirstObjectByType<BaseEditor>() == null)
            {
                var obj = new GameObject("BaseEditor");
                obj.AddComponent<BaseEditor>();
                obj.SetActive(false); // Disabled until needed
                Debug.Log("[Setup] Created BaseEditor (disabled)");
            }
            
            // FirebaseWebClient (created by WorldMapRenderer, but ensure it exists)
            if (Object.FindFirstObjectByType<ApexCitadels.PC.WebGL.FirebaseWebClient>() == null)
            {
                var obj = new GameObject("FirebaseWebClient");
                obj.AddComponent<ApexCitadels.PC.WebGL.FirebaseWebClient>();
                Debug.Log("[Setup] Created FirebaseWebClient");
            }
            
            // Territories Container
            if (GameObject.Find("Territories") == null)
            {
                var obj = new GameObject("Territories");
                Debug.Log("[Setup] Created Territories container");
            }
            
            Debug.Log("[Setup] ✅ Managers created");
        }

        private static void SetupUI()
        {
            Debug.Log("[Setup] Step 6: Setting up UI...");
            
            // EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventObj = new GameObject("EventSystem");
                eventObj.AddComponent<EventSystem>();
                eventObj.AddComponent<StandaloneInputModule>();
                Debug.Log("[Setup] Created EventSystem");
            }
            
            // Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                
                canvasObj.AddComponent<GraphicRaycaster>();
                Debug.Log("[Setup] Created Canvas");
            }
            
            // PCUIManager
            if (Object.FindFirstObjectByType<ApexCitadels.PC.UI.PCUIManager>() == null)
            {
                canvas.gameObject.AddComponent<ApexCitadels.PC.UI.PCUIManager>();
                Debug.Log("[Setup] Added PCUIManager to Canvas");
            }
            
            // Create basic UI panels structure
            CreateUIPanel(canvas.transform, "TopBar", new Vector2(0, 1), new Vector2(1, 1), 
                new Vector2(0, -30), new Vector2(0, 60));
            CreateUIPanel(canvas.transform, "BottomBar", new Vector2(0, 0), new Vector2(1, 0), 
                new Vector2(0, 30), new Vector2(0, 60));
            CreateUIPanel(canvas.transform, "LeftPanel", new Vector2(0, 0.5f), new Vector2(0, 0.5f), 
                new Vector2(150, 0), new Vector2(300, 400));
            CreateUIPanel(canvas.transform, "RightPanel", new Vector2(1, 0.5f), new Vector2(1, 0.5f), 
                new Vector2(-150, 0), new Vector2(300, 400));
            
            Debug.Log("[Setup] ✅ UI configured");
        }

        private static void CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, 
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            // Check if already exists
            Transform existing = parent.Find(name);
            if (existing != null) return;
            
            var panelObj = new GameObject(name);
            panelObj.transform.SetParent(parent, false);
            
            var rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            
            // Add semi-transparent background
            var image = panelObj.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
            
            // Start hidden (UI manager will show them)
            panelObj.SetActive(false);
        }

        private static void WireUpReferences()
        {
            Debug.Log("[Setup] Step 7: Wiring up references...");
            
            // Get all the components
            var gameController = Object.FindFirstObjectByType<PCGameController>();
            var cameraController = Object.FindFirstObjectByType<PCCameraController>();
            var inputManager = Object.FindFirstObjectByType<PCInputManager>();
            var worldMapRenderer = Object.FindFirstObjectByType<WorldMapRenderer>();
            var baseEditor = Object.FindFirstObjectByType<BaseEditor>();
            var uiManager = Object.FindFirstObjectByType<ApexCitadels.PC.UI.PCUIManager>();
            
            // Wire PCGameController
            if (gameController != null)
            {
                var so = new SerializedObject(gameController);
                
                SetSerializedReference(so, "cameraController", cameraController);
                SetSerializedReference(so, "inputManager", inputManager);
                SetSerializedReference(so, "worldMapRenderer", worldMapRenderer);
                SetSerializedReference(so, "baseEditor", baseEditor);
                SetSerializedReference(so, "uiManager", uiManager);
                
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(gameController);
            }
            
            // Wire WorldMapRenderer territories container
            if (worldMapRenderer != null)
            {
                var so = new SerializedObject(worldMapRenderer);
                var territoriesObj = GameObject.Find("Territories");
                if (territoriesObj != null)
                {
                    var prop = so.FindProperty("territoriesContainer");
                    if (prop != null)
                    {
                        prop.objectReferenceValue = territoriesObj.transform;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                EditorUtility.SetDirty(worldMapRenderer);
            }
            
            Debug.Log("[Setup] ✅ References wired");
        }

        private static void SetSerializedReference(SerializedObject so, string propertyName, Object value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null && value != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static void SaveScene()
        {
            Debug.Log("[Setup] Step 8: Saving scene...");
            
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SCENE_PATH);
            
            // Also add to build settings if not already there
            AddSceneToBuildSettings();
            
            Debug.Log($"[Setup] ✅ Scene saved to {SCENE_PATH}");
        }

        private static void AddSceneToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            bool found = false;
            
            foreach (var scene in scenes)
            {
                if (scene.path == SCENE_PATH)
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
                scenes.CopyTo(newScenes, 0);
                newScenes[scenes.Length] = new EditorBuildSettingsScene(SCENE_PATH, true);
                EditorBuildSettings.scenes = newScenes;
                Debug.Log("[Setup] Added scene to Build Settings");
            }
        }

        private static void FixCurrentScene()
        {
            Debug.Log("[Setup] Fixing current scene...");
            
            SetupCamera();
            SetupLighting();
            SetupRenderSettings();
            SetupManagers();
            SetupUI();
            WireUpReferences();
            
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            
            Debug.Log("[Setup] ✅ Current scene fixed - remember to save!");
            EditorUtility.DisplayDialog("Scene Fixed", 
                "Current scene has been fixed.\nRemember to save! (Ctrl+S)", "OK");
        }
    }
}
