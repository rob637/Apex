using Camera = UnityEngine.Camera;
using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// Fantasy World Manager - Master controller that orchestrates the entire
    /// fantasy ground-level view system. Coordinates OSM data, procedural generation,
    /// terrain, citadels, and player navigation to create an immersive medieval
    /// transformation of the player's real-world neighborhood.
    /// </summary>
    public class FantasyWorldManager : MonoBehaviour
    {
        [Header("Geographic Settings")]
        [SerializeField] private double defaultLatitude = 40.7128; // NYC
        [SerializeField] private double defaultLongitude = -74.0060;
        [SerializeField] private float worldRadius = 500f; // meters
        [SerializeField] private float metersPerUnit = 1f;
        
        [Header("System References")]
        [SerializeField] private OSMDataPipeline osmPipeline;
        [SerializeField] private ProceduralBuildingGenerator buildingGenerator;
        [SerializeField] private FantasyRoadRenderer roadRenderer;
        [SerializeField] private FantasyTerrainGenerator terrainGenerator;
        [SerializeField] private GroundLevelLODManager lodManager;
        [SerializeField] private PlayerCitadelRenderer citadelRenderer;
        [SerializeField] private FantasyWalkingController walkingController;
        
        [Header("Player Settings")]
        [SerializeField] private string playerId = "test_player";
        [SerializeField] private string allianceId = "";
        
        [Header("Camera Modes")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CameraMode defaultCameraMode = CameraMode.StrategicView;
        
        [Header("World Settings")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool useRealLocation = false;
        [SerializeField] private float updateInterval = 30f;
        
        [Header("Visual Settings")]
        [SerializeField] private bool enableDayNightCycle = true;
        [SerializeField] private Light directionalLight;
        [SerializeField] private float dayLength = 600f; // seconds
        
        [Header("Audio")]
        [SerializeField] private AudioSource ambientAudioSource;
        [SerializeField] private AudioClip[] dayAmbience;
        [SerializeField] private AudioClip[] nightAmbience;
        [SerializeField] private AudioClip[] fantasyMusic;
        
        // Singleton
        private static FantasyWorldManager _instance;
        public static FantasyWorldManager Instance => _instance;
        
        // State
        private double _currentLatitude;
        private double _currentLongitude;
        private Vector3 _worldOrigin;
        private bool _isInitialized = false;
        private bool _isLoading = false;
        private CameraMode _currentCameraMode;
        private float _timeOfDay = 0.5f; // 0-1 (0 = midnight, 0.5 = noon)
        
        // Events
        public event Action OnWorldInitialized;
        public event Action OnWorldLoading;
        public event Action OnWorldLoaded;
        public event Action<CameraMode> OnCameraModeChanged;
        public event Action<float> OnTimeOfDayChanged;
        
        public bool IsInitialized => _isInitialized;
        public bool IsLoading => _isLoading;
        public double CurrentLatitude => _currentLatitude;
        public double CurrentLongitude => _currentLongitude;
        public Vector3 WorldOrigin => _worldOrigin;
        public CameraMode CurrentCameraMode => _currentCameraMode;
        public float TimeOfDay => _timeOfDay;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            InitializeComponents();
            
            if (generateOnStart)
            {
                if (useRealLocation)
                {
                    StartCoroutine(InitializeWithRealLocation());
                }
                else
                {
                    InitializeWorld(defaultLatitude, defaultLongitude);
                }
            }
            
            if (enableDayNightCycle)
            {
                StartCoroutine(DayNightCycleCoroutine());
            }
        }
        
        private void Update()
        {
            HandleCameraModeInput();
        }
        
        #region Initialization
        
        private void InitializeComponents()
        {
            // Find components if not assigned
            if (osmPipeline == null) osmPipeline = FindFirstObjectByType<OSMDataPipeline>();
            if (buildingGenerator == null) buildingGenerator = FindFirstObjectByType<ProceduralBuildingGenerator>();
            if (roadRenderer == null) roadRenderer = FindFirstObjectByType<FantasyRoadRenderer>();
            if (terrainGenerator == null) terrainGenerator = FindFirstObjectByType<FantasyTerrainGenerator>();
            if (lodManager == null) lodManager = FindFirstObjectByType<GroundLevelLODManager>();
            if (citadelRenderer == null) citadelRenderer = FindFirstObjectByType<PlayerCitadelRenderer>();
            if (walkingController == null) walkingController = FindFirstObjectByType<FantasyWalkingController>();
            if (mainCamera == null) mainCamera = Camera.main;
            
            // Create missing components
            if (osmPipeline == null)
            {
                GameObject pipelineObj = new GameObject("OSMDataPipeline");
                pipelineObj.transform.SetParent(transform);
                osmPipeline = pipelineObj.AddComponent<OSMDataPipeline>();
            }
            
            if (buildingGenerator == null)
            {
                GameObject genObj = new GameObject("BuildingGenerator");
                genObj.transform.SetParent(transform);
                buildingGenerator = genObj.AddComponent<ProceduralBuildingGenerator>();
            }
            
            if (roadRenderer == null)
            {
                GameObject roadObj = new GameObject("RoadRenderer");
                roadObj.transform.SetParent(transform);
                roadRenderer = roadObj.AddComponent<FantasyRoadRenderer>();
            }
            
            if (terrainGenerator == null)
            {
                GameObject terrainObj = new GameObject("TerrainGenerator");
                terrainObj.transform.SetParent(transform);
                terrainGenerator = terrainObj.AddComponent<FantasyTerrainGenerator>();
            }
            
            if (lodManager == null)
            {
                GameObject lodObj = new GameObject("LODManager");
                lodObj.transform.SetParent(transform);
                lodManager = lodObj.AddComponent<GroundLevelLODManager>();
            }
            
            if (citadelRenderer == null)
            {
                GameObject citadelObj = new GameObject("CitadelRenderer");
                citadelObj.transform.SetParent(transform);
                citadelRenderer = citadelObj.AddComponent<PlayerCitadelRenderer>();
            }
            
            // Initialize player info
            citadelRenderer?.Initialize(playerId, allianceId);
            
            SetCameraMode(defaultCameraMode);
        }
        
        private IEnumerator InitializeWithRealLocation()
        {
            // Request location permission and get GPS coordinates
            if (!Input.location.isEnabledByUser)
            {
                Debug.LogWarning("[FantasyWorld] Location services disabled, using default");
                InitializeWorld(defaultLatitude, defaultLongitude);
                yield break;
            }
            
            Input.location.Start(1f, 0.1f);
            
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }
            
            if (Input.location.status == LocationServiceStatus.Running)
            {
                double lat = Input.location.lastData.latitude;
                double lon = Input.location.lastData.longitude;
                InitializeWorld(lat, lon);
            }
            else
            {
                Debug.LogWarning("[FantasyWorld] Could not get location, using default");
                InitializeWorld(defaultLatitude, defaultLongitude);
            }
        }
        
        /// <summary>
        /// Initialize the fantasy world at given coordinates
        /// </summary>
        public async void InitializeWorld(double latitude, double longitude)
        {
            if (_isLoading) return;
            
            _isLoading = true;
            OnWorldLoading?.Invoke();
            
            Debug.Log($"[FantasyWorld] Initializing at ({latitude:F6}, {longitude:F6})");
            
            _currentLatitude = latitude;
            _currentLongitude = longitude;
            _worldOrigin = Vector3.zero;
            
            // Clear existing world
            ClearWorld();
            
            try
            {
                // Initialize LOD manager
                lodManager?.Initialize(latitude, longitude, _worldOrigin);
                
                // Generate terrain
                if (terrainGenerator != null)
                {
                    var terrain = terrainGenerator.GenerateProceduralTerrain(_worldOrigin, new Vector2d(latitude, longitude));
                    Debug.Log("[FantasyWorld] Terrain generated");
                }
                
                // Fetch OSM data
                if (osmPipeline != null)
                {
                    double latRange = worldRadius / 111320.0;
                    double lonRange = worldRadius / (111320.0 * Math.Cos(latitude * Math.PI / 180));
                    
                    var areaData = await osmPipeline.FetchBoundingBox(
                        latitude - latRange,
                        latitude + latRange,
                        longitude - lonRange,
                        longitude + lonRange
                    );
                    
                    if (areaData != null)
                    {
                        Debug.Log($"[FantasyWorld] OSM data: {areaData.buildings?.Count ?? 0} buildings, {areaData.roads?.Count ?? 0} roads");
                        
                        // Generate buildings
                        buildingGenerator?.GenerateBuildings(areaData, _worldOrigin, metersPerUnit);
                        
                        // Generate roads
                        roadRenderer?.RenderRoads(areaData, _worldOrigin, metersPerUnit);
                    }
                }
                
                // Load citadels
                if (citadelRenderer != null)
                {
                    double latRange = worldRadius / 111320.0;
                    double lonRange = worldRadius / (111320.0 * Math.Cos(latitude * Math.PI / 180));
                    
                    await citadelRenderer.LoadCitadelsInArea(
                        latitude - latRange,
                        latitude + latRange,
                        longitude - lonRange,
                        longitude + lonRange,
                        _worldOrigin,
                        latitude,
                        longitude
                    );
                }
                
                _isInitialized = true;
                _isLoading = false;
                
                OnWorldInitialized?.Invoke();
                OnWorldLoaded?.Invoke();
                
                Debug.Log("[FantasyWorld] World initialization complete");
                
                // Start periodic updates
                StartCoroutine(PeriodicUpdateCoroutine());
            }
            catch (Exception e)
            {
                Debug.LogError($"[FantasyWorld] Initialization failed: {e.Message}");
                _isLoading = false;
            }
        }
        
        /// <summary>
        /// Clear the entire world
        /// </summary>
        public void ClearWorld()
        {
            buildingGenerator?.ClearBuildings();
            roadRenderer?.ClearRoads();
            terrainGenerator?.DestroyTerrain();
            citadelRenderer?.ClearAllCitadels();
            lodManager?.UnloadAllChunks();
            
            _isInitialized = false;
        }
        
        #endregion
        
        #region Camera Modes
        
        private void HandleCameraModeInput()
        {
            // Number keys to switch camera modes
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetCameraMode(CameraMode.StrategicView);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetCameraMode(CameraMode.OverheadView);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetCameraMode(CameraMode.FirstPerson);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetCameraMode(CameraMode.Cinematic);
            
            // F to toggle first person
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (_currentCameraMode == CameraMode.FirstPerson)
                    SetCameraMode(CameraMode.StrategicView);
                else
                    SetCameraMode(CameraMode.FirstPerson);
            }
        }
        
        /// <summary>
        /// Set camera mode
        /// </summary>
        public void SetCameraMode(CameraMode mode)
        {
            if (_currentCameraMode == mode) return;
            
            _currentCameraMode = mode;
            
            // Configure camera and controls based on mode
            switch (mode)
            {
                case CameraMode.StrategicView:
                    ConfigureStrategicView();
                    break;
                case CameraMode.OverheadView:
                    ConfigureOverheadView();
                    break;
                case CameraMode.FirstPerson:
                    ConfigureFirstPersonView();
                    break;
                case CameraMode.Cinematic:
                    ConfigureCinematicView();
                    break;
            }
            
            OnCameraModeChanged?.Invoke(mode);
            Debug.Log($"[FantasyWorld] Camera mode: {mode}");
        }
        
        private void ConfigureStrategicView()
        {
            // Enable RTS-style camera
            if (mainCamera != null)
            {
                mainCamera.transform.position = _worldOrigin + new Vector3(0, 200f, -100f);
                mainCamera.transform.rotation = Quaternion.Euler(60f, 0, 0);
            }
            
            if (walkingController != null)
            {
                walkingController.gameObject.SetActive(false);
            }
            
            lodManager?.TransitionToView(ViewMode.Aerial);
        }
        
        private void ConfigureOverheadView()
        {
            // Top-down view
            if (mainCamera != null)
            {
                mainCamera.transform.position = _worldOrigin + Vector3.up * 500f;
                mainCamera.transform.rotation = Quaternion.Euler(90f, 0, 0);
            }
            
            if (walkingController != null)
            {
                walkingController.gameObject.SetActive(false);
            }
            
            lodManager?.TransitionToView(ViewMode.Aerial);
        }
        
        private void ConfigureFirstPersonView()
        {
            // Enable first-person controller
            if (walkingController != null)
            {
                walkingController.gameObject.SetActive(true);
                walkingController.TeleportTo(_worldOrigin + Vector3.up * 2f);
            }
            
            lodManager?.TransitionToView(ViewMode.Ground);
        }
        
        private void ConfigureCinematicView()
        {
            // Auto-flying camera
            if (walkingController != null)
            {
                walkingController.gameObject.SetActive(false);
            }
            
            // Would start cinematic camera path
            lodManager?.TransitionToView(ViewMode.Transitioning);
        }
        
        #endregion
        
        #region Day/Night Cycle
        
        private IEnumerator DayNightCycleCoroutine()
        {
            while (true)
            {
                _timeOfDay += Time.deltaTime / dayLength;
                if (_timeOfDay >= 1f) _timeOfDay = 0f;
                
                UpdateLighting();
                OnTimeOfDayChanged?.Invoke(_timeOfDay);
                
                yield return null;
            }
        }
        
        private void UpdateLighting()
        {
            if (directionalLight == null) return;
            
            // Sun rotation (rises at 0.25, sets at 0.75)
            float sunAngle = (_timeOfDay - 0.25f) * 360f;
            directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0);
            
            // Intensity (dim at night)
            float intensity = Mathf.Clamp01(Mathf.Sin(_timeOfDay * Mathf.PI * 2 - Mathf.PI / 2) * 0.5f + 0.5f);
            directionalLight.intensity = Mathf.Lerp(0.1f, 1.2f, intensity);
            
            // Color (warm at sunrise/sunset)
            float sunHeight = Mathf.Sin(_timeOfDay * Mathf.PI);
            Color sunColor = Color.Lerp(new Color(1f, 0.6f, 0.3f), Color.white, Mathf.Clamp01(sunHeight * 2));
            directionalLight.color = sunColor;
            
            // Update ambient
            if (ambientAudioSource != null)
            {
                bool isNight = _timeOfDay < 0.25f || _timeOfDay > 0.75f;
                AudioClip[] clips = isNight ? nightAmbience : dayAmbience;
                
                if (clips != null && clips.Length > 0 && !ambientAudioSource.isPlaying)
                {
                    ambientAudioSource.clip = clips[UnityEngine.Random.Range(0, clips.Length)];
                    ambientAudioSource.Play();
                }
            }
        }
        
        /// <summary>
        /// Set time of day (0-1)
        /// </summary>
        public void SetTimeOfDay(float time)
        {
            _timeOfDay = Mathf.Clamp01(time);
            UpdateLighting();
            OnTimeOfDayChanged?.Invoke(_timeOfDay);
        }
        
        #endregion
        
        #region Updates
        
        private IEnumerator PeriodicUpdateCoroutine()
        {
            while (_isInitialized)
            {
                yield return new WaitForSeconds(updateInterval);
                
                // Update citadels (check for changes)
                if (citadelRenderer != null)
                {
                    // Would fetch updated citadel data from Firebase
                }
                
                // Update LOD
                if (lodManager != null && mainCamera != null)
                {
                    lodManager.LoadChunksAroundPosition(mainCamera.transform.position);
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Move to new location
        /// </summary>
        public void MoveToLocation(double latitude, double longitude)
        {
            InitializeWorld(latitude, longitude);
        }
        
        /// <summary>
        /// Focus camera on citadel
        /// </summary>
        public void FocusOnCitadel(string citadelId)
        {
            var citadel = citadelRenderer?.GetCitadelAtPosition(Vector3.zero);
            // Would implement camera movement to citadel
        }
        
        /// <summary>
        /// Teleport player to world position
        /// </summary>
        public void TeleportPlayer(Vector3 worldPosition)
        {
            if (walkingController != null)
            {
                walkingController.TeleportTo(worldPosition);
            }
        }
        
        /// <summary>
        /// Convert GPS to world position
        /// </summary>
        public Vector3 GeoToWorld(double lat, double lon)
        {
            double latDiff = lat - _currentLatitude;
            double lonDiff = lon - _currentLongitude;
            
            float metersNorth = (float)(latDiff * 111320);
            float metersEast = (float)(lonDiff * 111320 * Math.Cos(_currentLatitude * Math.PI / 180));
            
            return _worldOrigin + new Vector3(metersEast / metersPerUnit, 0, metersNorth / metersPerUnit);
        }
        
        /// <summary>
        /// Convert world position to GPS
        /// </summary>
        public Vector2d WorldToGeo(Vector3 worldPos)
        {
            float metersNorth = (worldPos.z - _worldOrigin.z) * metersPerUnit;
            float metersEast = (worldPos.x - _worldOrigin.x) * metersPerUnit;
            
            double lat = _currentLatitude + metersNorth / 111320.0;
            double lon = _currentLongitude + metersEast / (111320.0 * Math.Cos(_currentLatitude * Math.PI / 180));
            
            return new Vector2d(lat, lon);
        }
        
        /// <summary>
        /// Get terrain height at world position
        /// </summary>
        public float GetTerrainHeight(Vector3 worldPos)
        {
            if (terrainGenerator != null)
            {
                return terrainGenerator.GetHeightAtPosition(worldPos);
            }
            return 0f;
        }
        
        #endregion
    }
    
    #region Enums
    
    public enum CameraMode
    {
        StrategicView,   // RTS-style angled view
        OverheadView,    // Top-down map view
        FirstPerson,     // Walking exploration
        Cinematic        // Auto-flying beauty shots
    }
    
    #endregion
}
