using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using ApexCitadels.PC;
using ApexCitadels.PC.UI;
using ApexCitadels.PC.WebGL;

namespace ApexCitadels.PC.Editor
{
    /// <summary>
    /// ULTIMATE ONE-CLICK SETUP for PC Scene.
    /// Run from: Window → Apex Citadels → ONE-CLICK SETUP
    /// 
    /// This script does EVERYTHING needed to run the PC client:
    /// 
    /// SCENE SETUP:
    /// 1. Creates PCMain.unity scene
    /// 2. Sets up Camera with correct settings + PCCameraController
    /// 3. Creates Directional Light with DayNightCycle
    /// 4. Configures Render/Ambient settings
    /// 
    /// MANAGERS:
    /// 5. Creates PCGameController
    /// 6. Creates PCInputManager  
    /// 7. Creates WorldMapRenderer
    /// 8. Creates BaseEditor
    /// 9. Creates FirebaseWebClient
    /// 10. Creates WebGLBridge
    /// 
    /// UI SYSTEM:
    /// 11. Creates Canvas with PCUIManager
    /// 12. Creates all UI Panel prefabs
    /// 13. Creates EventSystem
    /// 
    /// WIRING:
    /// 14. Wires all references between components
    /// 15. Saves scene to Build Settings
    /// 
    /// BUILD CONFIG:
    /// 16. Configures WebGL Player Settings
    /// </summary>
    public class PCCompleteSetup : EditorWindow
    {
        private static string SCENE_PATH = "Assets/Scenes/PCMain.unity";
        private static string SCENES_FOLDER = "Assets/Scenes";
        private static string PREFABS_FOLDER = "Assets/Prefabs/PC";
        private static string UI_PREFABS_FOLDER = "Assets/Prefabs/PC/UI";
        
        private Vector2 scrollPos;
        private static List<string> setupLog = new List<string>();

        [MenuItem("Window/Apex Citadels/🚀 ONE-CLICK SETUP (Start Here!)", false, 0)]
        public static void OneClickSetup()
        {
            setupLog.Clear();
            
            if (EditorUtility.DisplayDialog("🏰 Apex Citadels - Complete PC Setup",
                "This will set up EVERYTHING needed to run the PC client:\n\n" +
                "✓ Create PCMain.unity scene\n" +
                "✓ Camera with sky color & controller\n" +
                "✓ Directional Light with day/night cycle\n" +
                "✓ All manager objects\n" +
                "✓ UI Canvas with all panels\n" +
                "✓ Firebase & WebGL integration\n" +
                "✓ Build settings configured\n\n" +
                "This takes about 10 seconds.", "Set Up Everything", "Cancel"))
            {
                RunCompleteSetup();
            }
        }

        [MenuItem("Window/Apex Citadels/Complete PC Setup (Options)", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<PCCompleteSetup>("PC Complete Setup");
            window.minSize = new Vector2(450, 600);
        }

        [MenuItem("Window/Apex Citadels/Scene Diagnostic", false, 20)]
        public static void OpenDiagnostic()
        {
            PCSceneDiagnostic.ShowWindow();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 16;
            
            EditorGUILayout.LabelField("🏰 Apex Citadels - PC Setup", headerStyle);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "This tool creates and configures the entire PC scene.\n" +
                "Just click the green button - everything else is automatic!",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            // Big green button
            GUI.backgroundColor = new Color(0.3f, 0.9f, 0.3f);
            GUIStyle bigButtonStyle = new GUIStyle(GUI.skin.button);
            bigButtonStyle.fontSize = 18;
            bigButtonStyle.fontStyle = FontStyle.Bold;
            
            if (GUILayout.Button("🚀 ONE-CLICK SETUP", bigButtonStyle, GUILayout.Height(50)))
            {
                RunCompleteSetup();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(20);
            
            // Status section
            EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);
            DrawStatusChecklist();
            
            EditorGUILayout.Space(20);
            
            // Individual actions
            EditorGUILayout.LabelField("Individual Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Fix Current Scene (Keep Existing)"))
            {
                FixCurrentScene();
            }
            
            if (GUILayout.Button("Create UI Prefabs Only"))
            {
                CreateUIPrefabs();
            }
            
            if (GUILayout.Button("Configure WebGL Build Settings"))
            {
                ConfigureWebGLBuildSettings();
            }
            
            if (GUILayout.Button("Open Scene Diagnostic"))
            {
                PCSceneDiagnostic.ShowWindow();
            }
            
            // Log section
            if (setupLog.Count > 0)
            {
                EditorGUILayout.Space(20);
                EditorGUILayout.LabelField("Setup Log", EditorStyles.boldLabel);
                
                foreach (string log in setupLog)
                {
                    EditorGUILayout.LabelField(log, EditorStyles.miniLabel);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawStatusChecklist()
        {
            // Check each component
            bool hasScene = File.Exists(SCENE_PATH);
            bool hasCamera = Camera.main != null;
            bool hasLight = Object.FindFirstObjectByType<Light>() != null;
            bool hasGameController = Object.FindFirstObjectByType<PCGameController>() != null;
            bool hasWorldMap = Object.FindFirstObjectByType<WorldMapRenderer>() != null;
            bool hasUIManager = Object.FindFirstObjectByType<PCUIManager>() != null;
            bool hasFirebase = Object.FindFirstObjectByType<FirebaseWebClient>() != null;
            
            EditorGUILayout.LabelField($"  {(hasScene ? "✅" : "❌")} PCMain.unity scene");
            EditorGUILayout.LabelField($"  {(hasCamera ? "✅" : "❌")} Main Camera");
            EditorGUILayout.LabelField($"  {(hasLight ? "✅" : "❌")} Directional Light");
            EditorGUILayout.LabelField($"  {(hasGameController ? "✅" : "❌")} PCGameController");
            EditorGUILayout.LabelField($"  {(hasWorldMap ? "✅" : "❌")} WorldMapRenderer");
            EditorGUILayout.LabelField($"  {(hasUIManager ? "✅" : "❌")} PCUIManager");
            EditorGUILayout.LabelField($"  {(hasFirebase ? "✅" : "❌")} FirebaseWebClient");
        }

        public static void RunCompleteSetup()
        {
            setupLog.Clear();
            
            Log("═══════════════════════════════════════════════════════════");
            Log("  APEX CITADELS - COMPLETE PC SETUP STARTING");
            Log("═══════════════════════════════════════════════════════════");

            try
            {
                EditorUtility.DisplayProgressBar("PC Setup", "Creating scene...", 0.05f);
                CreateOrOpenScene();
                
                EditorUtility.DisplayProgressBar("PC Setup", "Setting up camera...", 0.15f);
                SetupCamera();
                
                EditorUtility.DisplayProgressBar("PC Setup", "Setting up lighting...", 0.25f);
                SetupLighting();
                
                EditorUtility.DisplayProgressBar("PC Setup", "Configuring render settings...", 0.35f);
                SetupRenderSettings();
                
                EditorUtility.DisplayProgressBar("PC Setup", "Creating managers...", 0.45f);
                SetupManagers();
                
                EditorUtility.DisplayProgressBar("PC Setup", "Creating UI...", 0.55f);
                SetupUI();
                
                EditorUtility.DisplayProgressBar("PC Setup", "Creating UI prefabs...", 0.65f);
                CreateUIPrefabs();
                
                EditorUtility.DisplayProgressBar("PC Setup", "Wiring references...", 0.75f);
                WireUpReferences();
                
                EditorUtility.DisplayProgressBar("PC Setup", "Configuring build settings...", 0.85f);
                ConfigureWebGLBuildSettings();
                
                EditorUtility.DisplayProgressBar("PC Setup", "Saving scene...", 0.95f);
                SaveScene();
                
                EditorUtility.ClearProgressBar();
                
                Log("═══════════════════════════════════════════════════════════");
                Log("  ✅ SETUP COMPLETE! Press Play to test.");
                Log("═══════════════════════════════════════════════════════════");
                
                EditorUtility.DisplayDialog("✅ Setup Complete!", 
                    "PC Scene has been fully configured!\n\n" +
                    "✅ Scene created & saved\n" +
                    "✅ Camera configured with blue sky\n" +
                    "✅ Directional Light created\n" +
                    "✅ All managers created\n" +
                    "✅ UI Canvas with panels\n" +
                    "✅ Firebase client ready\n" +
                    "✅ WebGL build settings configured\n" +
                    "✅ Added to Build Settings\n\n" +
                    "Press PLAY to test!\n\n" +
                    "You should see:\n" +
                    "• Blue sky\n" +
                    "• Green ground plane\n" +
                    "• Territory markers (after Firebase loads)\n" +
                    "• WASD camera movement", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"Setup failed: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Setup Failed", 
                    $"An error occurred:\n{ex.Message}\n\nCheck console for details.", "OK");
            }
        }

        private static void Log(string message)
        {
            Debug.Log(message);
            setupLog.Add(message);
        }

        private static void CreateOrOpenScene()
        {
            Log("[Setup] Step 1: Creating/Opening scene...");
            
            // Ensure Scenes folder exists
            if (!AssetDatabase.IsValidFolder(SCENES_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
                Log("[Setup] Created Assets/Scenes folder");
            }
            
            // Check if scene exists
            if (File.Exists(SCENE_PATH))
            {
                Log($"[Setup] Opening existing scene: {SCENE_PATH}");
                EditorSceneManager.OpenScene(SCENE_PATH);
            }
            else
            {
                Log($"[Setup] Creating new scene: {SCENE_PATH}");
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(newScene, SCENE_PATH);
            }
        }

        private static void SetupCamera()
        {
            Log("[Setup] Step 2: Setting up camera...");
            
            Camera mainCam = Camera.main;
            GameObject camObj;
            
            if (mainCam == null)
            {
                mainCam = Object.FindFirstObjectByType<Camera>();
            }
            
            if (mainCam != null)
            {
                camObj = mainCam.gameObject;
                Log($"[Setup] Using existing camera: {camObj.name}");
            }
            else
            {
                camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                Log("[Setup] Created new Main Camera");
            }
            
            // Configure camera
            camObj.tag = "MainCamera";
            camObj.name = "Main Camera";
            
            // Position for world map view - angled down at 70 degrees
            camObj.transform.position = new Vector3(0, 200, -100);
            camObj.transform.rotation = Quaternion.Euler(70, 0, 0);
            
            // Camera settings - SOLID COLOR for reliable sky
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.5f, 0.7f, 1f); // Nice blue sky
            mainCam.nearClipPlane = 0.3f;
            mainCam.farClipPlane = 5000f;
            mainCam.fieldOfView = 60f;
            
            // Audio listener
            if (camObj.GetComponent<AudioListener>() == null)
            {
                camObj.AddComponent<AudioListener>();
            }
            
            // PCCameraController
            if (camObj.GetComponent<PCCameraController>() == null)
            {
                camObj.AddComponent<PCCameraController>();
                Log("[Setup] Added PCCameraController");
            }
            
            Log("[Setup] ✅ Camera configured");
        }

        private static void SetupLighting()
        {
            Log("[Setup] Step 3: Setting up lighting...");
            
            // Find existing directional light
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
                Log($"[Setup] Using existing light: {sunObj.name}");
            }
            else
            {
                sunObj = new GameObject("Directional Light");
                sunLight = sunObj.AddComponent<Light>();
                Log("[Setup] Created new Directional Light");
            }
            
            // Configure light - CRITICAL for colors to show!
            sunObj.name = "Directional Light";
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1f, 0.96f, 0.88f); // Warm sunlight
            sunLight.intensity = 1.2f; // Bright enough to see colors
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.7f;
            sunLight.shadowBias = 0.05f;
            sunLight.shadowNormalBias = 0.4f;
            
            // Nice angle for lighting
            sunObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            
            // DayNightCycle
            if (sunObj.GetComponent<DayNightCycle>() == null)
            {
                sunObj.AddComponent<DayNightCycle>();
                Log("[Setup] Added DayNightCycle");
            }
            
            Log("[Setup] ✅ Lighting configured");
        }

        private static void SetupRenderSettings()
        {
            Log("[Setup] Step 4: Setting up render settings...");
            
            // Ambient lighting - CRITICAL for object visibility
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.7f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.55f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.35f, 0.3f);
            RenderSettings.ambientIntensity = 1f;
            
            // No fog for now
            RenderSettings.fog = false;
            
            // No skybox - using solid color
            RenderSettings.skybox = null;
            
            // Reflection
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.5f;
            
            Log("[Setup] ✅ Render settings configured");
        }

        private static void SetupManagers()
        {
            Log("[Setup] Step 5: Setting up managers...");
            
            // PCGameController
            if (Object.FindFirstObjectByType<PCGameController>() == null)
            {
                var obj = new GameObject("PCGameController");
                obj.AddComponent<PCGameController>();
                Log("[Setup] Created PCGameController");
            }
            
            // PCInputManager
            if (Object.FindFirstObjectByType<PCInputManager>() == null)
            {
                var obj = new GameObject("PCInputManager");
                obj.AddComponent<PCInputManager>();
                Log("[Setup] Created PCInputManager");
            }
            
            // WorldMapRenderer
            if (Object.FindFirstObjectByType<WorldMapRenderer>() == null)
            {
                var obj = new GameObject("WorldMapRenderer");
                obj.AddComponent<WorldMapRenderer>();
                Log("[Setup] Created WorldMapRenderer");
            }
            
            // BaseEditor
            if (Object.FindFirstObjectByType<BaseEditor>() == null)
            {
                var obj = new GameObject("BaseEditor");
                obj.AddComponent<BaseEditor>();
                obj.SetActive(false); // Disabled until needed
                Log("[Setup] Created BaseEditor (disabled)");
            }
            
            // FirebaseWebClient
            if (Object.FindFirstObjectByType<FirebaseWebClient>() == null)
            {
                var obj = new GameObject("FirebaseWebClient");
                obj.AddComponent<FirebaseWebClient>();
                Log("[Setup] Created FirebaseWebClient");
            }
            
            // WebGLBridge
            if (Object.FindFirstObjectByType<WebGLBridgeComponent>() == null)
            {
                var obj = new GameObject("WebGLBridge");
                obj.AddComponent<WebGLBridgeComponent>();
                Log("[Setup] Created WebGLBridge");
            }
            
            // Territories Container
            if (GameObject.Find("Territories") == null)
            {
                var obj = new GameObject("Territories");
                Log("[Setup] Created Territories container");
            }
            
            // VisualEnhancements - for skybox, particles, glow effects
            if (Object.FindFirstObjectByType<VisualEnhancements>() == null)
            {
                var obj = new GameObject("VisualEnhancements");
                obj.AddComponent<VisualEnhancements>();
                Log("[Setup] Created VisualEnhancements");
            }
            
            // PCResourceSystem - for resource ticking and management
            if (Object.FindFirstObjectByType<PCResourceSystem>() == null)
            {
                var obj = new GameObject("PCResourceSystem");
                obj.AddComponent<PCResourceSystem>();
                Log("[Setup] Created PCResourceSystem");
            }
            
            Log("[Setup] ✅ Managers created");
        }

        private static void SetupUI()
        {
            Log("[Setup] Step 6: Setting up UI...");
            
            // EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventObj = new GameObject("EventSystem");
                eventObj.AddComponent<EventSystem>();
                eventObj.AddComponent<StandaloneInputModule>();
                Log("[Setup] Created EventSystem");
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
                Log("[Setup] Created Canvas");
            }
            
            // PCUIManager
            if (canvas.GetComponent<PCUIManager>() == null)
            {
                canvas.gameObject.AddComponent<PCUIManager>();
                Log("[Setup] Added PCUIManager to Canvas");
            }
            
            // Create UI panel containers
            CreateUIPanelContainer(canvas.transform, "TopBar", 
                new Vector2(0, 1), new Vector2(1, 1), 
                new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(0, 60));
            
            CreateUIPanelContainer(canvas.transform, "BottomBar", 
                new Vector2(0, 0), new Vector2(1, 0), 
                new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(0, 60));
            
            CreateUIPanelContainer(canvas.transform, "LeftPanel", 
                new Vector2(0, 0), new Vector2(0, 1), 
                new Vector2(0, 0.5f), new Vector2(160, 0), new Vector2(320, 0));
            
            CreateUIPanelContainer(canvas.transform, "RightPanel", 
                new Vector2(1, 0), new Vector2(1, 1), 
                new Vector2(1, 0.5f), new Vector2(-160, 0), new Vector2(320, 0));
            
            CreateUIPanelContainer(canvas.transform, "CenterPanel", 
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 
                new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(600, 400));
            
            Log("[Setup] ✅ UI configured");
        }

        private static void CreateUIPanelContainer(Transform parent, string name, 
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return;
            
            var panelObj = new GameObject(name);
            panelObj.transform.SetParent(parent, false);
            
            var rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            
            // Semi-transparent background
            var image = panelObj.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
            
            // Start hidden
            panelObj.SetActive(false);
        }

        private static void CreateUIPrefabs()
        {
            Log("[Setup] Step 7: Creating UI prefabs...");
            
            // Ensure prefab folders exist
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PREFABS_FOLDER);
            EnsureFolder(UI_PREFABS_FOLDER);
            
            // Create each UI panel prefab
            CreateTerritoryDetailPanelPrefab();
            CreateAlliancePanelPrefab();
            CreateBuildMenuPanelPrefab();
            CreateStatisticsPanelPrefab();
            CreateBattleReplayPanelPrefab();
            CreateCraftingPanelPrefab();
            CreateMarketPanelPrefab();
            
            AssetDatabase.Refresh();
            Log("[Setup] ✅ UI prefabs created");
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static void CreateTerritoryDetailPanelPrefab()
        {
            string prefabPath = $"{UI_PREFABS_FOLDER}/TerritoryDetailPanel.prefab";
            if (File.Exists(prefabPath)) return;
            
            var panel = CreateBasicPanel("TerritoryDetailPanel", 400, 500);
            AddPanelTitle(panel.transform, "Territory Details");
            AddPanelText(panel.transform, "TerritoryName", "Territory Name", 24, new Vector2(0, -50));
            AddPanelText(panel.transform, "OwnerName", "Owner: Unknown", 16, new Vector2(0, -80));
            AddPanelText(panel.transform, "ResourcesText", "Resources: 0", 14, new Vector2(0, -110));
            AddPanelText(panel.transform, "DefenseText", "Defense: 0", 14, new Vector2(0, -135));
            AddPanelButton(panel.transform, "AttackButton", "Attack", new Vector2(0, -200));
            AddPanelButton(panel.transform, "CloseButton", "Close", new Vector2(0, -250));
            
            // Add script
            panel.AddComponent<TerritoryDetailPanel>();
            
            PrefabUtility.SaveAsPrefabAsset(panel, prefabPath);
            Object.DestroyImmediate(panel);
            Log("[Setup] Created TerritoryDetailPanel prefab");
        }

        private static void CreateAlliancePanelPrefab()
        {
            string prefabPath = $"{UI_PREFABS_FOLDER}/AlliancePanel.prefab";
            if (File.Exists(prefabPath)) return;
            
            var panel = CreateBasicPanel("AlliancePanel", 500, 600);
            AddPanelTitle(panel.transform, "Alliance");
            AddPanelText(panel.transform, "AllianceName", "Alliance Name", 22, new Vector2(0, -50));
            AddPanelText(panel.transform, "MemberCount", "Members: 0/50", 14, new Vector2(0, -80));
            AddPanelButton(panel.transform, "MembersButton", "View Members", new Vector2(0, -150));
            AddPanelButton(panel.transform, "WarRoomButton", "War Room", new Vector2(0, -200));
            AddPanelButton(panel.transform, "CloseButton", "Close", new Vector2(0, -280));
            
            panel.AddComponent<AlliancePanel>();
            
            PrefabUtility.SaveAsPrefabAsset(panel, prefabPath);
            Object.DestroyImmediate(panel);
            Log("[Setup] Created AlliancePanel prefab");
        }

        private static void CreateBuildMenuPanelPrefab()
        {
            string prefabPath = $"{UI_PREFABS_FOLDER}/BuildMenuPanel.prefab";
            if (File.Exists(prefabPath)) return;
            
            var panel = CreateBasicPanel("BuildMenuPanel", 350, 500);
            AddPanelTitle(panel.transform, "Build Menu");
            AddPanelText(panel.transform, "CategoryLabel", "Category: All", 14, new Vector2(0, -50));
            AddPanelButton(panel.transform, "WallsButton", "Walls", new Vector2(0, -100));
            AddPanelButton(panel.transform, "TowersButton", "Towers", new Vector2(0, -145));
            AddPanelButton(panel.transform, "ProductionButton", "Production", new Vector2(0, -190));
            AddPanelButton(panel.transform, "DecorativeButton", "Decorative", new Vector2(0, -235));
            AddPanelButton(panel.transform, "CloseButton", "Close", new Vector2(0, -310));
            
            panel.AddComponent<BuildMenuPanel>();
            
            PrefabUtility.SaveAsPrefabAsset(panel, prefabPath);
            Object.DestroyImmediate(panel);
            Log("[Setup] Created BuildMenuPanel prefab");
        }

        private static void CreateStatisticsPanelPrefab()
        {
            string prefabPath = $"{UI_PREFABS_FOLDER}/StatisticsPanel.prefab";
            if (File.Exists(prefabPath)) return;
            
            var panel = CreateBasicPanel("StatisticsPanel", 600, 500);
            AddPanelTitle(panel.transform, "Statistics Dashboard");
            AddPanelText(panel.transform, "TerritoriesOwned", "Territories: 0", 16, new Vector2(-150, -60));
            AddPanelText(panel.transform, "TotalResources", "Resources: 0", 16, new Vector2(150, -60));
            AddPanelText(panel.transform, "AttacksWon", "Attacks Won: 0", 16, new Vector2(-150, -90));
            AddPanelText(panel.transform, "DefensesWon", "Defenses Won: 0", 16, new Vector2(150, -90));
            AddPanelButton(panel.transform, "CloseButton", "Close", new Vector2(0, -240));
            
            panel.AddComponent<StatisticsPanel>();
            
            PrefabUtility.SaveAsPrefabAsset(panel, prefabPath);
            Object.DestroyImmediate(panel);
            Log("[Setup] Created StatisticsPanel prefab");
        }

        private static void CreateBattleReplayPanelPrefab()
        {
            string prefabPath = $"{UI_PREFABS_FOLDER}/BattleReplayPanel.prefab";
            if (File.Exists(prefabPath)) return;
            
            var panel = CreateBasicPanel("BattleReplayPanel", 700, 500);
            AddPanelTitle(panel.transform, "Battle Replay");
            AddPanelText(panel.transform, "BattleInfo", "Select a battle to replay", 14, new Vector2(0, -60));
            AddPanelButton(panel.transform, "PlayButton", "▶ Play", new Vector2(-100, -200));
            AddPanelButton(panel.transform, "PauseButton", "⏸ Pause", new Vector2(0, -200));
            AddPanelButton(panel.transform, "SpeedButton", "2x Speed", new Vector2(100, -200));
            AddPanelButton(panel.transform, "CloseButton", "Close", new Vector2(0, -280));
            
            panel.AddComponent<BattleReplayPanel>();
            
            PrefabUtility.SaveAsPrefabAsset(panel, prefabPath);
            Object.DestroyImmediate(panel);
            Log("[Setup] Created BattleReplayPanel prefab");
        }

        private static void CreateCraftingPanelPrefab()
        {
            string prefabPath = $"{UI_PREFABS_FOLDER}/CraftingPanel.prefab";
            if (File.Exists(prefabPath)) return;
            
            var panel = CreateBasicPanel("CraftingPanel", 500, 550);
            AddPanelTitle(panel.transform, "Crafting Workshop");
            AddPanelText(panel.transform, "RecipeLabel", "Select a recipe", 14, new Vector2(0, -60));
            AddPanelText(panel.transform, "MaterialsLabel", "Materials needed:", 14, new Vector2(0, -140));
            AddPanelButton(panel.transform, "CraftButton", "Craft", new Vector2(0, -250));
            AddPanelButton(panel.transform, "CloseButton", "Close", new Vector2(0, -310));
            
            panel.AddComponent<CraftingPanel>();
            
            PrefabUtility.SaveAsPrefabAsset(panel, prefabPath);
            Object.DestroyImmediate(panel);
            Log("[Setup] Created CraftingPanel prefab");
        }

        private static void CreateMarketPanelPrefab()
        {
            string prefabPath = $"{UI_PREFABS_FOLDER}/MarketPanel.prefab";
            if (File.Exists(prefabPath)) return;
            
            var panel = CreateBasicPanel("MarketPanel", 650, 550);
            AddPanelTitle(panel.transform, "Market");
            AddPanelText(panel.transform, "BalanceLabel", "Balance: 0 Gold", 16, new Vector2(0, -55));
            AddPanelButton(panel.transform, "BuyTab", "Buy", new Vector2(-100, -100));
            AddPanelButton(panel.transform, "SellTab", "Sell", new Vector2(0, -100));
            AddPanelButton(panel.transform, "HistoryTab", "History", new Vector2(100, -100));
            AddPanelButton(panel.transform, "CloseButton", "Close", new Vector2(0, -300));
            
            panel.AddComponent<MarketPanel>();
            
            PrefabUtility.SaveAsPrefabAsset(panel, prefabPath);
            Object.DestroyImmediate(panel);
            Log("[Setup] Created MarketPanel prefab");
        }

        private static GameObject CreateBasicPanel(string name, float width, float height)
        {
            var panel = new GameObject(name);
            var rect = panel.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            
            // Background
            var image = panel.AddComponent<Image>();
            image.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
            
            // Border (child)
            var border = new GameObject("Border");
            border.transform.SetParent(panel.transform, false);
            var borderRect = border.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = Vector2.zero;
            var borderImage = border.AddComponent<Image>();
            borderImage.color = new Color(0.4f, 0.35f, 0.6f, 1f);
            borderImage.type = Image.Type.Sliced;
            borderImage.raycastTarget = false;
            
            return panel;
        }

        private static void AddPanelTitle(Transform parent, string title)
        {
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);
            var rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -10);
            rect.sizeDelta = new Vector2(300, 40);
            
            var text = titleObj.AddComponent<Text>();
            text.text = title;
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.9f, 0.85f, 1f);
        }

        private static void AddPanelText(Transform parent, string name, string content, int fontSize, Vector2 position)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            var rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(350, 30);
            
            var text = textObj.AddComponent<Text>();
            text.text = content;
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }

        private static void AddPanelButton(Transform parent, string name, string label, Vector2 position)
        {
            var buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);
            var rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(150, 35);
            
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.3f, 0.25f, 0.5f, 1f);
            
            var button = buttonObj.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.25f, 0.5f);
            colors.highlightedColor = new Color(0.4f, 0.35f, 0.6f);
            colors.pressedColor = new Color(0.25f, 0.2f, 0.4f);
            button.colors = colors;
            
            // Button text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            var text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }

        private static void WireUpReferences()
        {
            Log("[Setup] Step 8: Wiring up references...");
            
            var gameController = Object.FindFirstObjectByType<PCGameController>();
            var cameraController = Object.FindFirstObjectByType<PCCameraController>();
            var inputManager = Object.FindFirstObjectByType<PCInputManager>();
            var worldMapRenderer = Object.FindFirstObjectByType<WorldMapRenderer>();
            var baseEditor = Object.FindFirstObjectByType<BaseEditor>();
            var uiManager = Object.FindFirstObjectByType<PCUIManager>();
            
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
            
            // Wire PCUIManager with prefabs
            if (uiManager != null)
            {
                var so = new SerializedObject(uiManager);
                
                // Load and assign prefabs
                AssignPrefab(so, "territoryDetailPanelPrefab", $"{UI_PREFABS_FOLDER}/TerritoryDetailPanel.prefab");
                AssignPrefab(so, "alliancePanelPrefab", $"{UI_PREFABS_FOLDER}/AlliancePanel.prefab");
                AssignPrefab(so, "buildMenuPanelPrefab", $"{UI_PREFABS_FOLDER}/BuildMenuPanel.prefab");
                AssignPrefab(so, "statisticsPanelPrefab", $"{UI_PREFABS_FOLDER}/StatisticsPanel.prefab");
                AssignPrefab(so, "battleReplayPanelPrefab", $"{UI_PREFABS_FOLDER}/BattleReplayPanel.prefab");
                AssignPrefab(so, "craftingPanelPrefab", $"{UI_PREFABS_FOLDER}/CraftingPanel.prefab");
                AssignPrefab(so, "marketPanelPrefab", $"{UI_PREFABS_FOLDER}/MarketPanel.prefab");
                
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(uiManager);
            }
            
            Log("[Setup] ✅ References wired");
        }

        private static void SetSerializedReference(SerializedObject so, string propertyName, Object value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null && value != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static void AssignPrefab(SerializedObject so, string propertyName, string prefabPath)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    prop.objectReferenceValue = prefab;
                }
            }
        }

        private static void ConfigureWebGLBuildSettings()
        {
            Log("[Setup] Step 9: Configuring WebGL build settings...");
            
            // Switch to WebGL platform if not already
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Log("[Setup] Note: Not switching platforms automatically. Do this manually if needed:");
                Log("[Setup]   File → Build Settings → WebGL → Switch Platform");
            }
            
            // Configure Player Settings for WebGL
            PlayerSettings.companyName = "ApexCitadels";
            PlayerSettings.productName = "Apex Citadels";
            
            // WebGL specific settings
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // Firebase hosting issue
            PlayerSettings.WebGL.memorySize = 512;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.decompressionFallback = true;
            
            // Color space
            PlayerSettings.colorSpace = ColorSpace.Linear;
            
            Log("[Setup] ✅ WebGL settings configured");
        }

        private static void SaveScene()
        {
            Log("[Setup] Step 10: Saving scene...");
            
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SCENE_PATH);
            
            // Add to build settings
            AddSceneToBuildSettings();
            
            Log($"[Setup] ✅ Scene saved to {SCENE_PATH}");
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
                Log("[Setup] Added scene to Build Settings");
            }
        }

        private static void FixCurrentScene()
        {
            Log("[Setup] Fixing current scene...");
            
            SetupCamera();
            SetupLighting();
            SetupRenderSettings();
            SetupManagers();
            SetupUI();
            WireUpReferences();
            
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            
            Log("[Setup] ✅ Current scene fixed - remember to save!");
            EditorUtility.DisplayDialog("Scene Fixed", 
                "Current scene has been fixed.\nRemember to save! (Ctrl+S)", "OK");
        }
    }
}
