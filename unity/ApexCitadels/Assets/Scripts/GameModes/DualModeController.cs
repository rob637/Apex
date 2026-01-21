// ============================================================================
// APEX CITADELS - DUAL MODE CONTROLLER
// Manages seamless transition between Map View and Ground View
// ============================================================================
using System;
using System.Collections;
using UnityEngine;
using ApexCitadels.Core;
using ApexCitadels.FantasyWorld;
using ApexCitadels.Map;

namespace ApexCitadels.GameModes
{
    public enum ViewMode
    {
        MapView,    // Flying over the map, tiles only
        GroundView, // On the ground, full fantasy world
        Transitioning
    }
    
    /// <summary>
    /// Master controller for the two-mode game system.
    /// Map View: Fly around looking at Mapbox tiles from above
    /// Ground View: Walk around in the fantasy world at street level
    /// </summary>
    public class DualModeController : MonoBehaviour
    {
        #region Singleton
        
        private static DualModeController _instance;
        public static DualModeController Instance => _instance;
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Mode Settings")]
        [SerializeField] private ViewMode startMode = ViewMode.MapView;
        
        [Header("Map View Settings")]
        [Tooltip("Height above ground for map view (meters)")]
        [SerializeField] private float mapViewHeight = 150f;
        
        [Tooltip("Camera angle in map view (degrees from horizontal)")]
        [SerializeField] private float mapViewAngle = 55f;
        
        [Tooltip("Movement speed in map view (meters/second)")]
        [SerializeField] private float mapMoveSpeed = 50f;
        
        [Header("Ground View Settings")]
        [Tooltip("Radius around player to generate fantasy world (meters)")]
        [SerializeField] private float groundViewRadius = 100f;
        
        [Tooltip("Camera distance behind player")]
        [SerializeField] private float thirdPersonDistance = 5f;
        
        [Tooltip("Camera height above player")]
        [SerializeField] private float thirdPersonHeight = 2f;
        
        [Header("Transition Settings")]
        [Tooltip("Duration of zoom transition (seconds)")]
        [SerializeField] private float transitionDuration = 1.5f;
        
        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private GameObject playerCharacter;
        [SerializeField] private Transform mapViewContainer;
        [SerializeField] private Transform groundViewContainer;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        
        #endregion
        
        #region Private Fields
        
        private ViewMode _currentMode = ViewMode.MapView;
        private Vector3 _mapPosition; // Current position in world coordinates (lat/lon mapped)
        private double _currentLatitude;
        private double _currentLongitude;
        
        // Components
        private MapViewCamera _mapCamera;
        private GroundViewController _groundController;
        private FantasyWorldGenerator _fantasyGenerator;
        
        #endregion
        
        #region Events
        
        public event Action<ViewMode> OnModeChanged;
        public event Action<double, double> OnLocationChanged;
        
        #endregion
        
        #region Properties
        
        public ViewMode CurrentMode => _currentMode;
        public double CurrentLatitude => _currentLatitude;
        public double CurrentLongitude => _currentLongitude;
        public float GroundViewRadius => groundViewRadius;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            SetupComponents();
        }
        
        private void Start()
        {
            // Get initial location from config
            var config = UnityEngine.Resources.Load<MapboxConfiguration>("MapboxConfig");
            if (config != null)
            {
                _currentLatitude = config.DefaultLatitude;
                _currentLongitude = config.DefaultLongitude;
            }
            else
            {
                // Default to Vienna, VA
                _currentLatitude = 38.9012;
                _currentLongitude = -77.2653;
            }
            
            Debug.Log($"[DualMode] Starting at {_currentLatitude}, {_currentLongitude}");
            
            // Start in configured mode
            StartCoroutine(InitializeMode(startMode));
        }
        
        private void Update()
        {
            // Check for mode switch input
            if (_currentMode != ViewMode.Transitioning)
            {
                // Space or click to land/take off
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (_currentMode == ViewMode.MapView)
                    {
                        LandAtCurrentPosition();
                    }
                    else
                    {
                        TakeOff();
                    }
                }
                
                // Right-click also lands
                if (Input.GetMouseButtonDown(1) && _currentMode == ViewMode.MapView)
                {
                    LandAtCurrentPosition();
                }
            }
        }
        
        #endregion
        
        #region Setup
        
        private void SetupComponents()
        {
            // Find or create camera
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    var camObj = new GameObject("MainCamera");
                    mainCamera = camObj.AddComponent<Camera>();
                    camObj.AddComponent<AudioListener>();
                    camObj.tag = "MainCamera";
                }
            }
            
            // Create containers
            if (mapViewContainer == null)
            {
                var mapObj = new GameObject("MapViewContainer");
                mapObj.transform.SetParent(transform);
                mapViewContainer = mapObj.transform;
            }
            
            if (groundViewContainer == null)
            {
                var groundObj = new GameObject("GroundViewContainer");
                groundObj.transform.SetParent(transform);
                groundViewContainer = groundObj.transform;
                groundViewContainer.gameObject.SetActive(false);
            }
            
            // Setup map camera controller
            _mapCamera = mainCamera.GetComponent<MapViewCamera>();
            if (_mapCamera == null)
            {
                _mapCamera = mainCamera.gameObject.AddComponent<MapViewCamera>();
            }
            _mapCamera.Initialize(mapViewHeight, mapViewAngle, mapMoveSpeed);
            
            // Setup ground controller
            _groundController = GetComponent<GroundViewController>();
            if (_groundController == null)
            {
                _groundController = gameObject.AddComponent<GroundViewController>();
            }
            
            // Find fantasy world generator
            _fantasyGenerator = FindAnyObjectByType<FantasyWorldGenerator>();
        }
        
        #endregion
        
        #region Mode Initialization
        
        private IEnumerator InitializeMode(ViewMode mode)
        {
            Debug.Log($"[DualMode] Initializing {mode}");
            
            if (mode == ViewMode.MapView)
            {
                yield return InitializeMapView();
            }
            else
            {
                yield return InitializeGroundView();
            }
            
            _currentMode = mode;
            OnModeChanged?.Invoke(mode);
        }
        
        private IEnumerator InitializeMapView()
        {
            // Disable ground view stuff
            groundViewContainer.gameObject.SetActive(false);
            if (playerCharacter != null)
                playerCharacter.SetActive(false);
            
            // IMPORTANT: Hide/clear fantasy world objects - they should NOT appear in Map View
            if (_fantasyGenerator != null)
            {
                _fantasyGenerator.ClearWorld();
                _fantasyGenerator.gameObject.SetActive(false);
            }
            
            // Also hide any debug visualization
            var debugView = FindAnyObjectByType<FantasyWorld.FantasyWorldDebugView>();
            if (debugView != null)
            {
                debugView.ClearDebugVisuals();
                debugView.showDebugView = false;
            }
            
            // Enable map view
            mapViewContainer.gameObject.SetActive(true);
            _mapCamera.enabled = true;
            _mapCamera.SetPosition(_currentLatitude, _currentLongitude);
            
            // Setup sky for map view (blue sky)
            SetupMapViewSky();
            
            // Disable ground view fog - need clear visibility for map
            DisableGroundViewFog();
            
            // Make sure Mapbox tiles are rendering
            var mapbox = FindAnyObjectByType<Map.MapboxTileRenderer>();
            if (mapbox != null)
            {
                mapbox.gameObject.SetActive(true);
            }
            
            yield return null;
        }
        
        private IEnumerator InitializeGroundView()
        {
            // Enable ground view container
            groundViewContainer.gameObject.SetActive(true);
            
            // Create or enable player character
            if (playerCharacter == null)
            {
                playerCharacter = CreatePlayerCharacter();
            }
            playerCharacter.SetActive(true);
            playerCharacter.transform.position = Vector3.zero;
            
            // Setup third-person camera
            _mapCamera.enabled = false;
            
            // Position camera behind player for third person view
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0, thirdPersonHeight, -thirdPersonDistance);
                mainCamera.transform.rotation = Quaternion.Euler(15f, 0, 0);
            }
            
            // Setup ground controller
            _groundController.enabled = true;
            _groundController.SetPlayer(playerCharacter);
            _groundController.SetCamera(mainCamera);
            
            // Enable and generate fantasy world around player
            if (_fantasyGenerator != null)
            {
                _fantasyGenerator.gameObject.SetActive(true);
                _fantasyGenerator.SetGenerationRadius(groundViewRadius);
                _fantasyGenerator.Initialize(_currentLatitude, _currentLongitude);
                yield return _fantasyGenerator.GenerateWorldCoroutine();
            }
            
            // Setup sky and ensure ground is visible
            SetupGroundViewSky();
            EnsureGroundIsVisible();
            
            Debug.Log("[DualMode] Ground View initialized");
            yield return null;
        }
        
        #endregion
        
        #region Mode Transitions
        
        /// <summary>
        /// Land at the current map view position
        /// </summary>
        public void LandAtCurrentPosition()
        {
            if (_currentMode != ViewMode.MapView) return;
            
            // Get current map camera position and convert to lat/lon
            var pos = _mapCamera.GetCurrentWorldPosition();
            _currentLatitude = _mapCamera.CurrentLatitude;
            _currentLongitude = _mapCamera.CurrentLongitude;
            
            Debug.Log($"[DualMode] Landing at {_currentLatitude}, {_currentLongitude}");
            
            StartCoroutine(TransitionToGround());
        }
        
        /// <summary>
        /// Take off from ground view back to map view
        /// </summary>
        public void TakeOff()
        {
            if (_currentMode != ViewMode.GroundView) return;
            
            Debug.Log($"[DualMode] Taking off from {_currentLatitude}, {_currentLongitude}");
            
            StartCoroutine(TransitionToMap());
        }
        
        private IEnumerator TransitionToGround()
        {
            _currentMode = ViewMode.Transitioning;
            OnModeChanged?.Invoke(ViewMode.Transitioning);
            
            // Store start position
            Vector3 startPos = mainCamera.transform.position;
            Quaternion startRot = mainCamera.transform.rotation;
            
            // Calculate end position (ground level, behind where player will be)
            Vector3 groundPos = new Vector3(0, thirdPersonHeight, -thirdPersonDistance);
            Quaternion groundRot = Quaternion.Euler(15f, 0, 0); // Slight downward angle
            
            // Create player character at destination
            if (playerCharacter == null)
            {
                playerCharacter = CreatePlayerCharacter();
            }
            playerCharacter.transform.position = Vector3.zero;
            playerCharacter.SetActive(true);
            
            // Start generating fantasy world during transition
            Coroutine worldGen = null;
            if (_fantasyGenerator != null)
            {
                _fantasyGenerator.gameObject.SetActive(true);
                groundViewContainer.gameObject.SetActive(true);
                _fantasyGenerator.SetGenerationRadius(groundViewRadius);
                _fantasyGenerator.Initialize(_currentLatitude, _currentLongitude);
                worldGen = StartCoroutine(_fantasyGenerator.GenerateWorldCoroutine());
            }
            
            // Hide Mapbox satellite tiles - we use fantasy grass ground instead
            var mapbox = FindAnyObjectByType<Map.MapboxTileRenderer>();
            if (mapbox != null)
            {
                mapbox.gameObject.SetActive(false);
            }
            
            // Animate camera down
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                
                // Use smooth step for nice easing
                float smooth = t * t * (3f - 2f * t);
                
                mainCamera.transform.position = Vector3.Lerp(startPos, groundPos, smooth);
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, groundRot, smooth);
                
                yield return null;
            }
            
            // Ensure final position
            mainCamera.transform.position = groundPos;
            mainCamera.transform.rotation = groundRot;
            
            // Wait for world generation if still running
            if (worldGen != null)
            {
                yield return worldGen;
            }
            
            // Disable map camera controls, enable ground controls
            _mapCamera.enabled = false;
            _groundController.enabled = true;
            _groundController.SetPlayer(playerCharacter);
            _groundController.SetCamera(mainCamera);
            
            // Setup sky and ground for ground view
            SetupGroundViewSky();
            EnsureGroundIsVisible();
            
            _currentMode = ViewMode.GroundView;
            OnModeChanged?.Invoke(ViewMode.GroundView);
            
            Debug.Log("[DualMode] Now in Ground View");
        }
        
        private IEnumerator TransitionToMap()
        {
            _currentMode = ViewMode.Transitioning;
            OnModeChanged?.Invoke(ViewMode.Transitioning);
            
            // Store start
            Vector3 startPos = mainCamera.transform.position;
            Quaternion startRot = mainCamera.transform.rotation;
            
            // Calculate map view position
            Vector3 mapPos = new Vector3(0, mapViewHeight, -mapViewHeight * 0.5f);
            Quaternion mapRot = Quaternion.Euler(mapViewAngle, 0, 0);
            
            // Disable ground controls
            _groundController.enabled = false;
            
            // Animate camera up
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                float smooth = t * t * (3f - 2f * t);
                
                mainCamera.transform.position = Vector3.Lerp(startPos, mapPos, smooth);
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, mapRot, smooth);
                
                yield return null;
            }
            
            // Final position
            mainCamera.transform.position = mapPos;
            mainCamera.transform.rotation = mapRot;
            
            // Hide player and ground view elements
            if (playerCharacter != null)
                playerCharacter.SetActive(false);
            
            // Clear fantasy world objects to free memory and hide generator
            if (_fantasyGenerator != null)
            {
                _fantasyGenerator.ClearWorld();
                _fantasyGenerator.gameObject.SetActive(false);
            }
            
            // Hide debug view
            var debugView = FindAnyObjectByType<FantasyWorld.FantasyWorldDebugView>();
            if (debugView != null)
            {
                debugView.ClearDebugVisuals();
                debugView.showDebugView = false;
            }
            
            groundViewContainer.gameObject.SetActive(false);
            
            // Enable map controls
            _mapCamera.enabled = true;
            _mapCamera.SetPosition(_currentLatitude, _currentLongitude);
            
            // Setup map sky
            SetupMapViewSky();
            
            _currentMode = ViewMode.MapView;
            OnModeChanged?.Invoke(ViewMode.MapView);
            
            Debug.Log("[DualMode] Now in Map View");
        }
        
        #endregion
        
        #region Player Character
        
        private GameObject CreatePlayerCharacter()
        {
            GameObject player;
            
            if (playerPrefab != null)
            {
                player = Instantiate(playerPrefab, groundViewContainer);
            }
            else
            {
                // Create placeholder character - will be replaced with Synty character
                player = CreatePlaceholderCharacter();
            }
            
            player.name = "PlayerCharacter";
            player.transform.SetParent(groundViewContainer);
            
            // Add character controller if needed
            if (player.GetComponent<CharacterController>() == null)
            {
                var cc = player.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.radius = 0.3f;
                cc.center = new Vector3(0, 0.9f, 0);
            }
            
            return player;
        }
        
        private GameObject CreatePlaceholderCharacter()
        {
            // Simple capsule as placeholder
            var player = new GameObject("PlaceholderCharacter");
            
            // Body - capsule at chest level
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(player.transform);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);  // Center of 1.8m tall character
            body.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);  // Taller capsule
            
            // Remove collider from visual (CharacterController provides collision)
            var col = body.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            
            // Set material - bright color for visibility
            var renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.color = new Color(0.2f, 0.6f, 0.9f); // Bright blue
                    mat.SetFloat("_Smoothness", 0.3f);
                    renderer.material = mat;
                }
            }
            
            // Add a head sphere for more character-like appearance
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(player.transform);
            head.transform.localPosition = new Vector3(0, 1.7f, 0);
            head.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            
            var headCol = head.GetComponent<Collider>();
            if (headCol != null) DestroyImmediate(headCol);
            
            var headRenderer = head.GetComponent<Renderer>();
            if (headRenderer != null && renderer != null)
            {
                headRenderer.material = renderer.sharedMaterial;
            }
            
            Debug.Log("[DualMode] Created placeholder character");
            
            return player;
        }
        
        #endregion
        
        #region Sky Setup
        
        private void SetupMapViewSky()
        {
            // Set a nice blue sky for map view
            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color(0.4f, 0.6f, 0.9f); // Nice sky blue
                mainCamera.farClipPlane = 1000f; // Need to see far in map view
            }
            
            // Or use skybox if available
            var skyMat = UnityEngine.Resources.Load<Material>("Skyboxes/DaySkybox");
            if (skyMat != null)
            {
                RenderSettings.skybox = skyMat;
                mainCamera.clearFlags = CameraClearFlags.Skybox;
            }
        }
        
        private void SetupGroundViewSky()
        {
            // Try to use a proper skybox for ground view
            var skyMat = UnityEngine.Resources.Load<Material>("Skyboxes/DaySkybox");
            if (skyMat == null)
            {
                skyMat = UnityEngine.Resources.Load<Material>("Skyboxes/FantasySkybox");
            }
            
            if (skyMat != null)
            {
                RenderSettings.skybox = skyMat;
                if (mainCamera != null)
                {
                    mainCamera.clearFlags = CameraClearFlags.Skybox;
                }
            }
            else
            {
                // Fallback: Use a gradient sky color instead of solid blue
                if (mainCamera != null)
                {
                    mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    // Light blue sky color
                    mainCamera.backgroundColor = new Color(0.53f, 0.81f, 0.92f); // Sky blue
                }
            }
            
            // Set up ambient lighting for ground view
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.7f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.6f, 0.7f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.35f, 0.25f);
            
            // Set camera clip planes appropriate for ground level view
            // Limit view distance for immersion - like real life, you can't see very far at street level
            if (mainCamera != null)
            {
                mainCamera.nearClipPlane = 0.1f;
                mainCamera.farClipPlane = 150f; // Limit to 150m for immersive ground-level feel
                mainCamera.fieldOfView = 60f;
            }
            
            // Enable atmospheric fog for immersive view distance limiting
            SetupGroundViewFog();
            
            Debug.Log("[DualMode] Ground View sky setup complete");
        }
        
        private void EnsureGroundIsVisible()
        {
            // We use fantasy grass ground, not Mapbox satellite tiles
            // The FantasyWorldGenerator creates its own procedural ground
            // But we also create a fallback ground here for safety
            CreateFallbackGround();
            
            Debug.Log("[DualMode] Ground setup complete");
        }
        
        private void CreateFallbackGround()
        {
            // Check if we already have one
            var existing = groundViewContainer.Find("FallbackGround");
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }
            
            Debug.Log("[DualMode] Creating fallback ground plane");
            
            // Create a large ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "FallbackGround";
            ground.transform.SetParent(groundViewContainer);
            ground.transform.localPosition = new Vector3(0, -0.05f, 0); // Slightly below origin to avoid z-fighting
            
            // Make it big enough - 30x the radius
            float size = groundViewRadius * 3f / 10f; // Plane is 10 units by default
            ground.transform.localScale = new Vector3(size, 1, size);
            
            // Give it a grassy material
            var renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.color = new Color(0.2f, 0.45f, 0.15f); // Green grass color
                    mat.SetFloat("_Smoothness", 0.1f);
                    renderer.material = mat;
                    
                    // Make sure it renders on both sides just in case
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = true;
                }
            }
            
            Debug.Log($"[DualMode] Ground plane created at scale {size}");
        }
        
        /// <summary>
        /// Setup atmospheric fog for immersive ground-level view distance
        /// Player should only see ~50-100m like real life - not distant objects
        /// </summary>
        private void SetupGroundViewFog()
        {
            // Enable fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            
            // Fog density tuned for 50-100m visibility
            // ExponentialSquared fog: visibility = 3/sqrt(density) for 95% opacity
            // For 80m visibility: density = (3/80)^2 = 0.0014
            RenderSettings.fogDensity = 0.012f; // Soft fade starting ~60m, fully obscured ~100m
            
            // Use a soft blue-gray atmospheric fog color that blends with sky
            RenderSettings.fogColor = new Color(0.65f, 0.75f, 0.85f); // Atmospheric blue-gray
            
            Debug.Log("[DualMode] Ground view fog enabled - visibility ~80m");
        }
        
        /// <summary>
        /// Disable fog for map view (need to see far)
        /// </summary>
        private void DisableGroundViewFog()
        {
            RenderSettings.fog = false;
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Move to a specific location (in map view)
        /// </summary>
        public void GoToLocation(double latitude, double longitude)
        {
            _currentLatitude = latitude;
            _currentLongitude = longitude;
            
            if (_currentMode == ViewMode.MapView)
            {
                _mapCamera.SetPosition(latitude, longitude);
            }
            
            OnLocationChanged?.Invoke(latitude, longitude);
        }
        
        /// <summary>
        /// Get current mode
        /// </summary>
        public bool IsInMapView() => _currentMode == ViewMode.MapView;
        public bool IsInGroundView() => _currentMode == ViewMode.GroundView;
        public bool IsTransitioning() => _currentMode == ViewMode.Transitioning;
        
        #endregion
    }
}
