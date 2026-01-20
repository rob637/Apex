// ============================================================================
// APEX CITADELS - SYSTEM COORDINATOR
// Central controller that manages all major game systems to prevent conflicts.
// This is the SINGLE SOURCE OF TRUTH for which systems are active.
// ============================================================================
using UnityEngine;
using System;
using System.Collections.Generic;

namespace ApexCitadels.Core
{
    /// <summary>
    /// Game modes that determine which systems should be active.
    /// Each mode has a specific set of systems that work together.
    /// </summary>
    public enum GameMode
    {
        /// <summary>Strategic map view with Mapbox tiles and territories</summary>
        WorldMap,
        
        /// <summary>First-person exploration inside a citadel</summary>
        CitadelView,
        
        /// <summary>Building/editing mode for base construction</summary>
        BuildMode,
        
        /// <summary>Combat/battle mode</summary>
        Combat,
        
        /// <summary>Loading or transitioning between modes</summary>
        Loading
    }
    
    /// <summary>
    /// Terrain rendering system types - only ONE should be active at a time.
    /// </summary>
    public enum TerrainSystem
    {
        None,
        MapboxTiles,      // Real-world map tiles (primary for WorldMap mode)
        ProceduralTerrain, // Generated terrain (fallback/offline)
        FantasyTerrain     // Fantasy-styled terrain
    }
    
    /// <summary>
    /// Camera controller types - only ONE should be active at a time.
    /// </summary>
    public enum CameraControllerType
    {
        None,
        WorldMapCamera,    // Strategic overhead view
        FirstPerson,       // WASD walking
        EditorCamera,      // Orbit/pan for building
        Cinematic          // Automated camera paths
    }
    
    /// <summary>
    /// Atmosphere/lighting system types - only ONE should be active at a time.
    /// </summary>
    public enum AtmosphereSystem
    {
        None,
        DayNightCycle,     // Full day/night with sun movement
        StaticLighting,    // Fixed lighting (for indoor/citadel)
        WeatherSystem      // Dynamic weather effects
    }
    
    /// <summary>
    /// SystemCoordinator - The central brain of Apex Citadels.
    /// Manages which systems are active and coordinates transitions.
    /// 
    /// USAGE:
    ///   SystemCoordinator.Instance.SetGameMode(GameMode.WorldMap);
    ///   SystemCoordinator.Instance.SetTerrainSystem(TerrainSystem.MapboxTiles);
    /// </summary>
    public class SystemCoordinator : MonoBehaviour
    {
        #region Singleton
        
        private static SystemCoordinator _instance;
        public static SystemCoordinator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SystemCoordinator>();
                    if (_instance == null)
                    {
                        var go = new GameObject("SystemCoordinator");
                        _instance = go.AddComponent<SystemCoordinator>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Current State
        
        [Header("Current State (Read-Only in Inspector)")]
        [SerializeField] private GameMode _currentMode = GameMode.Loading;
        [SerializeField] private TerrainSystem _currentTerrain = TerrainSystem.None;
        [SerializeField] private CameraControllerType _currentCamera = CameraControllerType.None;
        [SerializeField] private AtmosphereSystem _currentAtmosphere = AtmosphereSystem.None;
        
        public GameMode CurrentMode => _currentMode;
        public TerrainSystem CurrentTerrain => _currentTerrain;
        public CameraControllerType CurrentCamera => _currentCamera;
        public AtmosphereSystem CurrentAtmosphere => _currentAtmosphere;
        
        #endregion
        
        #region Events
        
        /// <summary>Fired when game mode changes</summary>
        public event Action<GameMode, GameMode> OnModeChanged;  // (oldMode, newMode)
        
        /// <summary>Fired when terrain system changes</summary>
        public event Action<TerrainSystem> OnTerrainChanged;
        
        /// <summary>Fired when camera controller changes</summary>
        public event Action<CameraControllerType> OnCameraChanged;
        
        /// <summary>Fired when atmosphere system changes</summary>
        public event Action<AtmosphereSystem> OnAtmosphereChanged;
        
        #endregion
        
        #region System References
        
        [Header("System References (Auto-discovered or assigned)")]
        [SerializeField] private Camera _mainCamera;
        
        // Terrain systems
        private Map.MapboxTileRenderer _mapboxRenderer;
        private PC.Environment.ProceduralTerrain _proceduralTerrain;
        
        // Camera controllers
        private PC.PCCameraController _pcCameraController;
        
        // Atmosphere systems
        private PC.Environment.AtmosphericLighting _atmosphericLighting;
        private PC.Visual.SkyboxEnvironmentSystem _skyboxSystem;
        
        // Track what we've disabled so we can re-enable
        private HashSet<MonoBehaviour> _disabledSystems = new HashSet<MonoBehaviour>();
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[SystemCoordinator] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log("[SystemCoordinator] ========================================");
            Debug.Log("[SystemCoordinator] Initializing System Coordinator");
            Debug.Log("[SystemCoordinator] ========================================");
        }
        
        private void Start()
        {
            // Discover all systems
            DiscoverSystems();
            
            // Set initial mode based on scene
            InitializeDefaultMode();
        }
        
        #endregion
        
        #region System Discovery
        
        /// <summary>
        /// Find and cache references to all managed systems.
        /// </summary>
        private void DiscoverSystems()
        {
            Debug.Log("[SystemCoordinator] Discovering systems...");
            
            // Camera
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                _mainCamera = FindFirstObjectByType<Camera>();
            }
            
            // Terrain
            _mapboxRenderer = FindFirstObjectByType<Map.MapboxTileRenderer>();
            _proceduralTerrain = FindFirstObjectByType<PC.Environment.ProceduralTerrain>();
            
            // Camera controllers
            _pcCameraController = FindFirstObjectByType<PC.PCCameraController>();
            
            // Atmosphere
            _atmosphericLighting = FindFirstObjectByType<PC.Environment.AtmosphericLighting>();
            _skyboxSystem = FindFirstObjectByType<PC.Visual.SkyboxEnvironmentSystem>();
            
            // Log what we found
            LogDiscoveredSystems();
        }
        
        private void LogDiscoveredSystems()
        {
            Debug.Log("[SystemCoordinator] --- Discovered Systems ---");
            Debug.Log($"  Main Camera: {(_mainCamera != null ? _mainCamera.name : "NOT FOUND")}");
            Debug.Log($"  MapboxTileRenderer: {(_mapboxRenderer != null ? "Found" : "Not Found")}");
            Debug.Log($"  ProceduralTerrain: {(_proceduralTerrain != null ? "Found" : "Not Found")}");
            Debug.Log($"  PCCameraController: {(_pcCameraController != null ? "Found" : "Not Found")}");
            Debug.Log($"  AtmosphericLighting: {(_atmosphericLighting != null ? "Found" : "Not Found")}");
            Debug.Log($"  SkyboxEnvironmentSystem: {(_skyboxSystem != null ? "Found" : "Not Found")}");
            Debug.Log("[SystemCoordinator] ---------------------------");
        }
        
        #endregion
        
        #region Mode Management
        
        /// <summary>
        /// Initialize the default mode based on what's in the scene.
        /// </summary>
        private void InitializeDefaultMode()
        {
            // If Mapbox is present and has valid config, use WorldMap mode
            if (_mapboxRenderer != null)
            {
                SetGameMode(GameMode.WorldMap);
            }
            else
            {
                // Fallback to loading mode
                SetGameMode(GameMode.Loading);
            }
        }
        
        /// <summary>
        /// Set the current game mode. This will configure all systems appropriately.
        /// </summary>
        public void SetGameMode(GameMode newMode)
        {
            if (newMode == _currentMode) return;
            
            var oldMode = _currentMode;
            Debug.Log($"[SystemCoordinator] Mode change: {oldMode} -> {newMode}");
            
            _currentMode = newMode;
            
            // Configure systems for this mode
            ConfigureForMode(newMode);
            
            // Fire event
            OnModeChanged?.Invoke(oldMode, newMode);
        }
        
        /// <summary>
        /// Configure all systems for a specific game mode.
        /// </summary>
        private void ConfigureForMode(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.WorldMap:
                    ConfigureWorldMapMode();
                    break;
                    
                case GameMode.CitadelView:
                    ConfigureCitadelMode();
                    break;
                    
                case GameMode.BuildMode:
                    ConfigureBuildMode();
                    break;
                    
                case GameMode.Combat:
                    ConfigureCombatMode();
                    break;
                    
                case GameMode.Loading:
                    ConfigureLoadingMode();
                    break;
            }
        }
        
        private void ConfigureWorldMapMode()
        {
            Debug.Log("[SystemCoordinator] Configuring WorldMap mode");
            
            // Terrain: Use Mapbox if available, else procedural
            if (_mapboxRenderer != null)
            {
                SetTerrainSystem(TerrainSystem.MapboxTiles);
            }
            else
            {
                SetTerrainSystem(TerrainSystem.ProceduralTerrain);
            }
            
            // Camera: World map overhead view
            SetCameraController(CameraControllerType.WorldMapCamera);
            
            // Atmosphere: Day/night cycle
            SetAtmosphereSystem(AtmosphereSystem.DayNightCycle);
            
            // Destroy conflicting objects
            DestroyConflictingObjects();
        }
        
        private void ConfigureCitadelMode()
        {
            Debug.Log("[SystemCoordinator] Configuring Citadel mode");
            
            // Terrain: None (interior)
            SetTerrainSystem(TerrainSystem.None);
            
            // Camera: First person
            SetCameraController(CameraControllerType.FirstPerson);
            
            // Atmosphere: Static indoor lighting
            SetAtmosphereSystem(AtmosphereSystem.StaticLighting);
        }
        
        private void ConfigureBuildMode()
        {
            Debug.Log("[SystemCoordinator] Configuring Build mode");
            
            // Terrain: Procedural for building
            SetTerrainSystem(TerrainSystem.ProceduralTerrain);
            
            // Camera: Editor orbit camera
            SetCameraController(CameraControllerType.EditorCamera);
            
            // Atmosphere: Static lighting for clarity
            SetAtmosphereSystem(AtmosphereSystem.StaticLighting);
        }
        
        private void ConfigureCombatMode()
        {
            Debug.Log("[SystemCoordinator] Configuring Combat mode");
            
            // Keep current terrain
            // Camera: Could be overhead or follow
            SetCameraController(CameraControllerType.WorldMapCamera);
            
            // Atmosphere: Keep current
        }
        
        private void ConfigureLoadingMode()
        {
            Debug.Log("[SystemCoordinator] Configuring Loading mode");
            
            // Disable expensive systems during loading
            SetTerrainSystem(TerrainSystem.None);
            SetCameraController(CameraControllerType.None);
        }
        
        #endregion
        
        #region Terrain Management
        
        /// <summary>
        /// Set the active terrain system. Only one terrain system can be active.
        /// </summary>
        public void SetTerrainSystem(TerrainSystem terrain)
        {
            if (terrain == _currentTerrain) return;
            
            Debug.Log($"[SystemCoordinator] Terrain: {_currentTerrain} -> {terrain}");
            
            // Disable all terrain systems first
            DisableAllTerrainSystems();
            
            // Enable the requested one
            _currentTerrain = terrain;
            
            switch (terrain)
            {
                case TerrainSystem.MapboxTiles:
                    EnableSystem(_mapboxRenderer);
                    break;
                    
                case TerrainSystem.ProceduralTerrain:
                    EnableSystem(_proceduralTerrain);
                    break;
                    
                case TerrainSystem.FantasyTerrain:
                    // Enable fantasy terrain if we have it
                    break;
            }
            
            OnTerrainChanged?.Invoke(terrain);
        }
        
        private void DisableAllTerrainSystems()
        {
            DisableSystem(_mapboxRenderer);
            DisableSystem(_proceduralTerrain);
            
            // Also destroy any stray terrain objects
            DestroyByName("GroundPlane", "GridOverlay", "WorldTerrain", 
                          "ProceduralTerrainSystem", "FantasyTerrain");
        }
        
        #endregion
        
        #region Camera Management
        
        /// <summary>
        /// Set the active camera controller. Only one can be active.
        /// </summary>
        public void SetCameraController(CameraControllerType controller)
        {
            if (controller == _currentCamera) return;
            
            Debug.Log($"[SystemCoordinator] Camera: {_currentCamera} -> {controller}");
            _currentCamera = controller;
            
            // Configure PCCameraController if present
            if (_pcCameraController != null)
            {
                switch (controller)
                {
                    case CameraControllerType.WorldMapCamera:
                        _pcCameraController.enabled = true;
                        // Set to WorldMap mode via the controller's API
                        break;
                        
                    case CameraControllerType.FirstPerson:
                        _pcCameraController.enabled = true;
                        break;
                        
                    case CameraControllerType.None:
                        _pcCameraController.enabled = false;
                        break;
                }
            }
            
            OnCameraChanged?.Invoke(controller);
        }
        
        #endregion
        
        #region Atmosphere Management
        
        /// <summary>
        /// Set the active atmosphere system.
        /// </summary>
        public void SetAtmosphereSystem(AtmosphereSystem atmosphere)
        {
            if (atmosphere == _currentAtmosphere) return;
            
            Debug.Log($"[SystemCoordinator] Atmosphere: {_currentAtmosphere} -> {atmosphere}");
            _currentAtmosphere = atmosphere;
            
            // Configure based on selection
            switch (atmosphere)
            {
                case AtmosphereSystem.DayNightCycle:
                    EnableSystem(_atmosphericLighting);
                    EnableSystem(_skyboxSystem);
                    break;
                    
                case AtmosphereSystem.StaticLighting:
                    DisableSystem(_atmosphericLighting);
                    // Keep skybox but stop time progression
                    break;
                    
                case AtmosphereSystem.None:
                    DisableSystem(_atmosphericLighting);
                    break;
            }
            
            OnAtmosphereChanged?.Invoke(atmosphere);
        }
        
        #endregion
        
        #region Utility Methods
        
        private void EnableSystem(MonoBehaviour system)
        {
            if (system == null) return;
            
            system.enabled = true;
            system.gameObject.SetActive(true);
            _disabledSystems.Remove(system);
        }
        
        private void DisableSystem(MonoBehaviour system)
        {
            if (system == null) return;
            
            system.enabled = false;
            _disabledSystems.Add(system);
        }
        
        private void DestroyByName(params string[] names)
        {
            foreach (var name in names)
            {
                var obj = GameObject.Find(name);
                if (obj != null)
                {
                    Debug.Log($"[SystemCoordinator] Destroying: {name}");
                    Destroy(obj);
                }
            }
        }
        
        /// <summary>
        /// Destroy objects that commonly cause conflicts.
        /// </summary>
        private void DestroyConflictingObjects()
        {
            // Objects that should never coexist with Mapbox
            if (_currentTerrain == TerrainSystem.MapboxTiles)
            {
                DestroyByName(
                    "GroundPlane", "GridOverlay", "WorldTerrain",
                    "ProceduralTerrainSystem", "FantasyTerrain",
                    "GeoMapSystem", "RealWorldMap", "WaterPlane"
                );
            }
        }
        
        /// <summary>
        /// Check if a specific system type is currently active.
        /// </summary>
        public bool IsSystemActive(TerrainSystem terrain) => _currentTerrain == terrain;
        public bool IsSystemActive(CameraControllerType camera) => _currentCamera == camera;
        public bool IsSystemActive(AtmosphereSystem atmosphere) => _currentAtmosphere == atmosphere;
        
        /// <summary>
        /// Re-discover systems (useful after scene load).
        /// </summary>
        public void RefreshSystems()
        {
            Debug.Log("[SystemCoordinator] Refreshing system references...");
            DiscoverSystems();
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Log current state to console.
        /// </summary>
        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            Debug.Log("[SystemCoordinator] === CURRENT STATE ===");
            Debug.Log($"  Mode: {_currentMode}");
            Debug.Log($"  Terrain: {_currentTerrain}");
            Debug.Log($"  Camera: {_currentCamera}");
            Debug.Log($"  Atmosphere: {_currentAtmosphere}");
            Debug.Log($"  Disabled Systems: {_disabledSystems.Count}");
            Debug.Log("=====================================");
        }
        
        #endregion
    }
}
