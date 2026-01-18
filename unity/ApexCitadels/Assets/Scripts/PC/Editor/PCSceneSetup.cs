using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ApexCitadels.PC
{
    /// <summary>
    /// Editor wizard to set up the PC Main Scene with all required components
    /// </summary>
    public static class PCSceneSetup
    {
#if UNITY_EDITOR
        [MenuItem("Apex/PC/Setup PC Scene (Full)")]
        public static void SetupFullPCScene()
        {
            // Create a new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Clear default objects
            var mainCamera = GameObject.Find("Main Camera");
            if (mainCamera != null) Object.DestroyImmediate(mainCamera);
            
            var directionalLight = GameObject.Find("Directional Light");
            if (directionalLight != null) Object.DestroyImmediate(directionalLight);
            
            // Create scene structure
            SetupSceneRoot();
            SetupCamera();
            SetupLighting();
            SetupWorld();
            SetupUI();
            SetupManagers();
            
            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(scene);
            
            // Save scene
            string scenePath = "Assets/Scenes/PCMain.unity";
            EnsureDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log("[PCSceneSetup] PC Main Scene created successfully!");
            Debug.Log("Next steps:");
            Debug.Log("1. Run Apex/PC/Create All PC Prefabs to create UI prefabs");
            Debug.Log("2. Assign prefabs to manager components");
            Debug.Log("3. Configure Firebase settings");
        }
        
        [MenuItem("Apex/PC/Add PC Components to Current Scene")]
        public static void AddPCComponentsToCurrentScene()
        {
            SetupSceneRoot();
            SetupCamera();
            SetupLighting();
            SetupWorld();
            SetupUI();
            SetupManagers();
            
            Debug.Log("[PCSceneSetup] PC components added to current scene!");
        }
        
        private static void SetupSceneRoot()
        {
            // Create organization hierarchy
            CreateEmptyIfNotExists("--- MANAGERS ---");
            CreateEmptyIfNotExists("--- WORLD ---");
            CreateEmptyIfNotExists("--- UI ---");
            CreateEmptyIfNotExists("--- AUDIO ---");
        }
        
        private static void SetupCamera()
        {
            // Create PC Camera rig
            var cameraRig = new GameObject("PCCameraRig");
            
            // Main camera - add directly to rig since PCCameraController uses GetComponent<Camera>()
            cameraRig.tag = "MainCamera";
            
            var cam = cameraRig.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 2000f;
            
            cameraRig.AddComponent<AudioListener>();
            
            // Position for world map view
            cameraRig.transform.position = new Vector3(0, 100, -50);
            cameraRig.transform.rotation = Quaternion.Euler(60, 0, 0);
            
            // Add PC Camera Controller (it will find the Camera component automatically)
            cameraRig.AddComponent<PCCameraController>();
            
            Debug.Log("[PCSceneSetup] Camera rig created");
        }
        
        private static void SetupLighting()
        {
            // Create lighting root
            var lighting = new GameObject("Lighting");
            
            // Directional light (sun)
            var sunObj = new GameObject("Sun");
            sunObj.transform.SetParent(lighting.transform);
            sunObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            
            var sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.9f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            
            // Ambient light
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.6f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.45f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.25f);
            
            // Fog for atmosphere
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.6f, 0.7f, 0.8f);
            RenderSettings.fogStartDistance = 200f;
            RenderSettings.fogEndDistance = 1500f;
            
            Debug.Log("[PCSceneSetup] Lighting configured");
        }
        
        private static void SetupWorld()
        {
            var worldRoot = GameObject.Find("--- WORLD ---") ?? new GameObject("--- WORLD ---");
            
            // Ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(worldRoot.transform);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(100, 1, 100);
            
            var groundRenderer = ground.GetComponent<Renderer>();
            groundRenderer.material = new Material(Shader.Find("Standard"));
            groundRenderer.material.color = new Color(0.3f, 0.5f, 0.3f);
            
            // Territory container
            var territories = new GameObject("Territories");
            territories.transform.SetParent(worldRoot.transform);
            
            // Buildings container
            var buildings = new GameObject("Buildings");
            buildings.transform.SetParent(worldRoot.transform);
            
            // Effects container
            var effects = new GameObject("Effects");
            effects.transform.SetParent(worldRoot.transform);
            
            Debug.Log("[PCSceneSetup] World structure created");
        }
        
        private static void SetupUI()
        {
            var uiRoot = GameObject.Find("--- UI ---") ?? new GameObject("--- UI ---");
            
            // Create Canvas
            var canvasObj = new GameObject("PCCanvas");
            canvasObj.transform.SetParent(uiRoot.transform);
            
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Create UI structure
            CreateUIPanel(canvasObj.transform, "TopBar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(0, 0));
            CreateUIPanel(canvasObj.transform, "BottomBar", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 80));
            CreateUIPanel(canvasObj.transform, "LeftPanel", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(300, 0));
            CreateUIPanel(canvasObj.transform, "RightPanel", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-300, 0), new Vector2(0, 0));
            CreateUIPanel(canvasObj.transform, "CenterPanels", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            
            // Minimap in top-right
            var minimap = CreateUIPanel(canvasObj.transform, "Minimap", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-220, -10), new Vector2(-10, -220));
            minimap.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
            
            // Resource display in top bar
            var topBar = canvasObj.transform.Find("TopBar");
            if (topBar != null)
            {
                topBar.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
                
                // Resource text
                var resourceText = new GameObject("ResourceDisplay");
                resourceText.transform.SetParent(topBar);
                var rect = resourceText.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(0.5f, 1);
                rect.offsetMin = new Vector2(20, 5);
                rect.offsetMax = new Vector2(-20, -5);
                
                var text = resourceText.AddComponent<TextMeshProUGUI>();
                text.text = "Gold: 1000 | Stone: 500 | Wood: 750";
                text.fontSize = 18;
                text.alignment = TextAlignmentOptions.Left;
                text.color = Color.white;
            }
            
            // Add PCUIManager
            var uiManager = canvasObj.AddComponent<UI.PCUIManager>();
            
            // Event System
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.transform.SetParent(uiRoot.transform);
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            Debug.Log("[PCSceneSetup] UI structure created");
        }
        
        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent);
            
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            
            return panel;
        }
        
        private static void SetupManagers()
        {
            var managersRoot = GameObject.Find("--- MANAGERS ---") ?? new GameObject("--- MANAGERS ---");
            
            // Game Controller
            var gameController = new GameObject("PCGameController");
            gameController.transform.SetParent(managersRoot.transform);
            gameController.AddComponent<PCGameController>();

            // Game Manager (Required Core System)
            if (GameObject.FindFirstObjectByType<ApexCitadels.Core.GameManager>() == null)
            {
                var gameManager = new GameObject("GameManager");
                gameManager.transform.SetParent(managersRoot.transform);
                gameManager.AddComponent<ApexCitadels.Core.GameManager>();
            }
            
            // Input Manager
            var inputManager = new GameObject("PCInputManager");
            inputManager.transform.SetParent(managersRoot.transform);
            inputManager.AddComponent<PCInputManager>();
            
            // World Map Renderer
            var worldMap = new GameObject("WorldMapRenderer");
            worldMap.transform.SetParent(managersRoot.transform);
            worldMap.AddComponent<WorldMapRenderer>();
            
            // Territory Bridge
            var territoryBridge = new GameObject("PCTerritoryBridge");
            territoryBridge.transform.SetParent(managersRoot.transform);
            territoryBridge.AddComponent<PCTerritoryBridge>();
            
            // Base Editor
            var baseEditor = new GameObject("BaseEditor");
            baseEditor.transform.SetParent(managersRoot.transform);
            baseEditor.AddComponent<BaseEditor>();
            
            // Battle Replay System
            var replaySystem = new GameObject("BattleReplaySystem");
            replaySystem.transform.SetParent(managersRoot.transform);
            replaySystem.AddComponent<BattleReplaySystem>();
            
            // Crafting System
            var craftingSystem = new GameObject("CraftingSystem");
            craftingSystem.transform.SetParent(managersRoot.transform);
            craftingSystem.AddComponent<CraftingSystem>();
            
            // Platform Manager initialization happens automatically
            
            Debug.Log("[PCSceneSetup] Managers created");
        }
        
        private static void CreateEmptyIfNotExists(string name)
        {
            if (GameObject.Find(name) == null)
            {
                new GameObject(name);
            }
        }
        
        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path);
                string folder = System.IO.Path.GetFileName(path);
                
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    EnsureDirectory(parent);
                }
                
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
        
        [MenuItem("Apex/PC/Validate PC Scene")]
        public static void ValidatePCScene()
        {
            Debug.Log("=== PC Scene Validation ===");
            
            // Check required components
            CheckComponent<PCGameController>("PCGameController");
            CheckComponent<PCCameraController>("PCCameraController");
            CheckComponent<PCInputManager>("PCInputManager");
            CheckComponent<WorldMapRenderer>("WorldMapRenderer");
            CheckComponent<PCTerritoryBridge>("PCTerritoryBridge");
            CheckComponent<BaseEditor>("BaseEditor");
            CheckComponent<BattleReplaySystem>("BattleReplaySystem");
            CheckComponent<CraftingSystem>("CraftingSystem");
            CheckComponent<UI.PCUIManager>("PCUIManager");
            
            // Check Canvas
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Debug.Log("✓ Canvas found");
            }
            else
            {
                Debug.LogWarning("✗ Canvas not found - UI will not work");
            }
            
            // Check Camera
            var camera = Camera.main;
            if (camera != null)
            {
                Debug.Log("✓ Main Camera found");
            }
            else
            {
                Debug.LogWarning("✗ Main Camera not found");
            }
            
            // Check EventSystem
            var eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null)
            {
                Debug.Log("✓ EventSystem found");
            }
            else
            {
                Debug.LogWarning("✗ EventSystem not found - UI input will not work");
            }
            
            Debug.Log("=== Validation Complete ===");
        }
        
        private static void CheckComponent<T>(string name) where T : Component
        {
            var component = Object.FindFirstObjectByType<T>();
            if (component != null)
            {
                Debug.Log($"✓ {name} found");
            }
            else
            {
                Debug.LogWarning($"✗ {name} not found");
            }
        }
#endif
    }
}
