using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using ApexCitadels.Core;
using ApexCitadels.PC.UI;
using ApexCitadels.Map;

#pragma warning disable 0414

namespace ApexCitadels.PC
{
    /// <summary>
    /// Bootstraps the PC scene by creating and configuring all required objects.
    /// Attach this to an empty GameObject in your PC scene to auto-setup everything.
    /// </summary>
    public class PCSceneBootstrapper : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetup = true;
        [SerializeField] private bool createUI = true;
        [SerializeField] private bool createEnvironment = true;

        [Header("Camera Settings")]
        [SerializeField] private Color skyColor = new Color(0.4f, 0.6f, 0.9f);
        [SerializeField] private float startHeight = 200f;

        [Header("Lighting")]
        [SerializeField] private bool createLighting = true;
        [SerializeField] private Color sunColor = new Color(1f, 0.95f, 0.8f);
        [SerializeField] private float sunIntensity = 1.2f;

        [Header("Environment")]
        [SerializeField] private bool createGround = true;
        [SerializeField] private float groundSize = 10000f;
        [SerializeField] private Color groundColor = new Color(0.25f, 0.4f, 0.25f);

        [Header("References (Auto-filled if empty)")]
        [SerializeField] private PCGameController gameController;
        [SerializeField] private PCCameraController cameraController;
        [SerializeField] private PCInputManager inputManager;
        [SerializeField] private WorldMapRenderer worldMapRenderer;
        [SerializeField] private BaseEditor baseEditor;
        [SerializeField] private PCUIManager uiManager;

        private void Awake()
        {
            // Check platform - PC, WebGL, or Editor
            if (!PlatformManager.HasKeyboardMouse)
            {
                ApexLogger.LogWarning("Not running on PC/WebGL platform, redirecting to AR scene", ApexLogger.LogCategory.General);
                SceneManager.LoadScene("ARGameplay");
                return;
            }

            if (autoSetup)
            {
                StartCoroutine(SetupScene());
            }
        }

        private IEnumerator SetupScene()
        {
            ApexLogger.Log("Starting PC scene setup...", ApexLogger.LogCategory.General);

            // Wait a frame for other Awake calls
            yield return null;

            // Step 1: Create Core Systems
            CreateCoreSystems();
            yield return null;

            // Step 2: Create Lighting
            if (createLighting)
            {
                CreateLighting();
            }
            yield return null;

            // Step 3: Create Environment
            if (createEnvironment)
            {
                CreateEnvironment();
            }
            yield return null;

            // Step 4: Create UI
            if (createUI)
            {
                CreateUISystem();
            }
            yield return null;

            // Step 5: Connect Systems
            ConnectSystems();

            ApexLogger.Log("PC scene setup complete!", ApexLogger.LogCategory.General);
        }

        #region Core Systems

        private void CreateCoreSystems()
        {
            ApexLogger.Log("Creating core systems...", ApexLogger.LogCategory.General);

            // Ensure GameManager exists (Critical dependency)
            if (FindFirstObjectByType<GameManager>() == null)
            {
                ApexLogger.Log("Creating GameManager...", ApexLogger.LogCategory.General);
                GameObject gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }

            // PC Game Controller
            if (gameController == null)
            {
                gameController = FindFirstObjectByType<PCGameController>();
                if (gameController == null)
                {
                    GameObject controllerObj = new GameObject("PCGameController");
                    gameController = controllerObj.AddComponent<PCGameController>();
                }
            }

            // Camera Controller
            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<PCCameraController>();
                if (cameraController == null)
                {
                    GameObject cameraObj = new GameObject("PCCamera");
                    Camera cam = cameraObj.AddComponent<Camera>();
                    cam.tag = "MainCamera";
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = skyColor;
                    cam.farClipPlane = 5000f;
                    
                    // Add audio listener
                    cameraObj.AddComponent<AudioListener>();
                    
                    cameraController = cameraObj.AddComponent<PCCameraController>();
                    
                    // Position camera
                    cameraObj.transform.position = new Vector3(0, startHeight, 0);
                    cameraObj.transform.rotation = Quaternion.Euler(60f, 0, 0);
                }
            }

            // Input Manager
            if (inputManager == null)
            {
                inputManager = FindFirstObjectByType<PCInputManager>();
                if (inputManager == null)
                {
                    GameObject inputObj = new GameObject("PCInputManager");
                    inputManager = inputObj.AddComponent<PCInputManager>();
                }
            }

            // World Map Renderer
            if (worldMapRenderer == null)
            {
                worldMapRenderer = FindFirstObjectByType<WorldMapRenderer>();
                if (worldMapRenderer == null)
                {
                    GameObject mapObj = new GameObject("WorldMapRenderer");
                    worldMapRenderer = mapObj.AddComponent<WorldMapRenderer>();
                }
            }

            // Base Editor
            if (baseEditor == null)
            {
                baseEditor = FindFirstObjectByType<BaseEditor>();
                if (baseEditor == null)
                {
                    GameObject editorObj = new GameObject("BaseEditor");
                    baseEditor = editorObj.AddComponent<BaseEditor>();
                    editorObj.SetActive(false); // Disabled until needed
                }
            }

            ApexLogger.Log("Core systems created", ApexLogger.LogCategory.General);
        }

        #endregion

        #region Lighting

        private void CreateLighting()
        {
            ApexLogger.Log("Creating lighting...", ApexLogger.LogCategory.General);

            // Check for existing directional light
            Light sunLight = null;
            Light existingLight = FindFirstObjectByType<Light>();
            
            if (existingLight != null && existingLight.type == LightType.Directional)
            {
                // Use existing light
                sunLight = existingLight;
                sunLight.color = sunColor;
                sunLight.intensity = sunIntensity;
            }
            else
            {
                // Create sun light
                GameObject sunObj = new GameObject("Sun");
                sunLight = sunObj.AddComponent<Light>();
                sunLight.type = LightType.Directional;
                sunLight.color = sunColor;
                sunLight.intensity = sunIntensity;
                sunLight.shadows = LightShadows.Soft;
                sunLight.shadowStrength = 0.8f;
                sunObj.transform.rotation = Quaternion.Euler(50f, -30f, 0);
            }

            // Ensure Day/Night cycle script is attached
            if (sunLight.GetComponent<DayNightCycle>() == null)
            {
                sunLight.gameObject.AddComponent<DayNightCycle>();
            }

            // Add ambient light adjustment
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.7f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.5f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.35f, 0.3f);

            ApexLogger.Log("Lighting created", ApexLogger.LogCategory.General);
        }

        #endregion

        #region Environment

        private void CreateEnvironment()
        {
            ApexLogger.Log("Creating environment...", ApexLogger.LogCategory.General);

            if (createGround)
            {
                CreateGroundPlane();
            }

            // Add skybox or sky color
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = skyColor;
        }

        private void CreateGroundPlane()
        {
            // Skip if Mapbox is providing the ground
            var mapbox = FindFirstObjectByType<MapboxTileRenderer>();
            if (mapbox != null && mapbox.gameObject.activeInHierarchy)
            {
                Debug.Log("[PCBootstrapper] Mapbox active - skipping ground plane");
                return;
            }
            
            // Check if ground already exists
            if (GameObject.Find("GroundPlane") != null)
                return;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundPlane";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(groundSize / 10f, 1, groundSize / 10f);

            // Configure material with URP shader
            Renderer renderer = ground.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") 
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Sprites/Default");
            
            Material groundMat;
            if (shader != null)
            {
                groundMat = new Material(shader);
            }
            else
            {
                // Fallback prevents null shader exception
                groundMat = new Material(renderer.sharedMaterial); 
            }
            
            // URP uses _BaseColor, Standard uses _Color
            if (groundMat.HasProperty("_BaseColor"))
                groundMat.SetColor("_BaseColor", groundColor);
            else
                groundMat.color = groundColor;
                
            renderer.material = groundMat;

            // Make ground static
            ground.isStatic = true;

            ApexLogger.Log("Ground plane created", ApexLogger.LogCategory.General);
        }

        #endregion

        #region UI

        private void CreateUISystem()
        {
            ApexLogger.Log("Creating UI system...", ApexLogger.LogCategory.General);

            // Check for existing Canvas
            Canvas existingCanvas = FindFirstObjectByType<Canvas>();
            if (existingCanvas != null && existingCanvas.GetComponent<PCUIManager>() != null)
            {
                uiManager = existingCanvas.GetComponent<PCUIManager>();
                return;
            }

            // Create UI Canvas
            GameObject canvasObj = new GameObject("PCUICanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            // Add required components
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Add UI Manager
            uiManager = canvasObj.AddComponent<PCUIManager>();
            
            // Add Controls Help Panel (shows keyboard shortcuts)
            canvasObj.AddComponent<ApexCitadels.PC.UI.ControlsHelpPanel>();
            
            // Add Tutorial System
            canvasObj.AddComponent<ApexCitadels.PC.UI.TutorialSystem>();

            // Create Event System if needed
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            ApexLogger.Log("UI system created", ApexLogger.LogCategory.General);
        }

        #endregion

        #region System Connection

        private void ConnectSystems()
        {
            ApexLogger.Log("Connecting systems...", ApexLogger.LogCategory.General);

            // Connect camera to input events
            if (inputManager != null && cameraController != null)
            {
                inputManager.OnToggleMapTerritoryView += cameraController.ToggleMapTerritoryView;
                inputManager.OnCycleCameraMode += CycleCameraMode;
                ApexLogger.Log("[Bootstrapper] ✓ Connected: InputManager -> CameraController (Space/T = ToggleView, C = CycleCamera)", ApexLogger.LogCategory.General);
            }
            else
            {
                ApexLogger.LogWarning($"[Bootstrapper] ✗ Cannot connect input: inputManager={inputManager != null}, cameraController={cameraController != null}", ApexLogger.LogCategory.General);
            }

            // Connect world map to territory selection
            if (worldMapRenderer != null && gameController != null)
            {
                worldMapRenderer.OnTerritoryClicked += (id) => gameController.ViewTerritory(id);
                ApexLogger.Log("[Bootstrapper] ✓ Connected: WorldMapRenderer -> GameController (territory clicks)", ApexLogger.LogCategory.General);
            }

            // Connect territory manager (if exists)
            var territoryManager = Territory.TerritoryManager.Instance;
            if (territoryManager != null && worldMapRenderer != null)
            {
                // Will be refreshed when territory data changes
                territoryManager.OnTerritoryClaimed += (t) => worldMapRenderer.RefreshVisibleTerritories();
                territoryManager.OnTerritoryLost += (t) => worldMapRenderer.RefreshVisibleTerritories();
                ApexLogger.Log("[Bootstrapper] ✓ Connected: TerritoryManager -> WorldMapRenderer", ApexLogger.LogCategory.General);
            }

            ApexLogger.Log("Systems connected", ApexLogger.LogCategory.General);
        }

        private void CycleCameraMode()
        {
            if (cameraController == null) return;

            // Cycle through modes
            var currentMode = cameraController.CurrentMode;
            var nextMode = currentMode switch
            {
                PCCameraMode.WorldMap => PCCameraMode.FirstPerson,
                PCCameraMode.FirstPerson => PCCameraMode.Cinematic,
                PCCameraMode.Cinematic => PCCameraMode.WorldMap,
                _ => PCCameraMode.WorldMap
            };

            cameraController.SetMode(nextMode);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually trigger setup (if autoSetup is false)
        /// </summary>
        public void ManualSetup()
        {
            StartCoroutine(SetupScene());
        }

        /// <summary>
        /// Get reference to the game controller
        /// </summary>
        public PCGameController GetGameController() => gameController;

        /// <summary>
        /// Get reference to the camera controller
        /// </summary>
        public PCCameraController GetCameraController() => cameraController;

        #endregion
    }
}
