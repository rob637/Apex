// ============================================================================
// APEX CITADELS - FANTASY WORLD GENERATOR
// Main orchestrator for converting real-world geography to fantasy
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Simple logger adapter for fantasy world module
    /// </summary>
    internal static class Logger
    {
        public static void Log(string message, string tag = "FantasyWorld")
        {
            ApexLogger.Log($"[{tag}] {message}", ApexLogger.LogCategory.Map);
        }
        
        public static void LogWarning(string message, string tag = "FantasyWorld")
        {
            ApexLogger.LogWarning($"[{tag}] {message}", ApexLogger.LogCategory.Map);
        }
        
        public static void LogError(string message, string tag = "FantasyWorld")
        {
            ApexLogger.LogError($"[{tag}] {message}", ApexLogger.LogCategory.Map);
        }
    }
    
    /// <summary>
    /// Unified OSM data container for the generator
    /// </summary>
    public class OSMData
    {
        public List<OSMBuilding> Buildings = new List<OSMBuilding>();
        public List<OSMRoad> Roads = new List<OSMRoad>();
        public List<OSMArea> Areas = new List<OSMArea>();
        
        /// <summary>
        /// Convert from OSMAreaData
        /// </summary>
        public static OSMData FromAreaData(OSMAreaData areaData)
        {
            if (areaData == null) return null;
            
            var data = new OSMData
            {
                Buildings = areaData.Buildings ?? new List<OSMBuilding>(),
                Roads = areaData.Roads ?? new List<OSMRoad>()
            };
            
            // Combine all areas
            data.Areas = new List<OSMArea>();
            if (areaData.Parks != null) data.Areas.AddRange(areaData.Parks);
            if (areaData.Water != null) data.Areas.AddRange(areaData.Water);
            if (areaData.Forests != null) data.Areas.AddRange(areaData.Forests);
            
            return data;
        }
    }
    

    /// <summary>
    /// Configuration for fantasy world generation
    /// </summary>
    [Serializable]
    public class FantasyWorldConfig
    {
        [Header("Generation Area")]
        [Tooltip("Radius in meters. 300-500m recommended for suburbs")]
        public float radiusMeters = 300f;
        public float cellSize = 100f; // For chunked loading
        
        [Header("Building Settings")]
        public bool generateBuildings = true; // ENABLED: Spawn fantasy buildings!
        public float buildingScaleMultiplier = 1f;
        public bool randomizeRotation = true;
        
        [Header("Vegetation Settings")]
        public bool generateVegetation = true; // ENABLED: Trees, bushes, ground cover!
        [Range(0f, 1f)]
        public float treeDensity = 0.7f;
        [Range(0f, 1f)]
        public float bushDensity = 0.8f;
        public float minTreeSpacing = 4f;
        
        [Header("Roads/Paths")]
        public bool generatePaths = true;
        public float pathWidth = 4f;
        
        [Header("Props & Details")]
        public bool generateProps = true; // ENABLED: Barrels, crates, market stalls!
        [Range(0f, 1f)]
        public float propDensity = 0.5f;
        
        [Header("Performance")]
        public int maxBuildingsPerFrame = 5;
        public int maxVegetationPerFrame = 20;
        public float lodDistance1 = 100f;
        public float lodDistance2 = 300f;
    }
    
    /// <summary>
    /// A generated cell/chunk of the fantasy world
    /// </summary>
    public class FantasyWorldCell
    {
        public Vector2Int CellCoord;
        public Bounds WorldBounds;
        public GameObject CellRoot;
        public List<GameObject> Buildings = new List<GameObject>();
        public List<GameObject> Vegetation = new List<GameObject>();
        public List<GameObject> Props = new List<GameObject>();
        public bool IsLoaded;
        public bool IsGenerating;
    }
    
    /// <summary>
    /// Main fantasy world generator.
    /// Converts real-world OSM data into a procedural fantasy landscape.
    /// </summary>
    public class FantasyWorldGenerator : MonoBehaviour
    {
        [Header("Configuration")]
        public FantasyWorldConfig config = new FantasyWorldConfig();
        
        [Header("Prefab Library")]
        public FantasyPrefabLibrary prefabLibrary;
        
        [Header("References")]
        public Transform playerTransform;
        public Material groundMaterial;
        public Material waterMaterial;
        
        [Header("Parents")]
        public Transform buildingsParent;
        public Transform vegetationParent;
        public Transform propsParent;
        public Transform pathsParent;
        
        [Header("Debug")]
        public FantasyWorldDebugView debugView;
        
        // OSM Data Fetcher
        private OSMDataFetcher _osmFetcher;
        
        // Cell management
        private Dictionary<Vector2Int, FantasyWorldCell> _cells = new Dictionary<Vector2Int, FantasyWorldCell>();
        private Vector2Int _lastPlayerCell;
        
        // Current geo location
        private double _originLat;
        private double _originLon;
        private bool _isInitialized;
        private int _proceduralBuildingCount; // Debug counter
        private float _worldScale = 1f; // Meters per world unit (from Mapbox)
        private float _prefabScale = 1f; // Scale to apply to prefabs (1/worldScale)
        private float _roadWidthMultiplier = 3f; // Make roads wider to match real road widths
        
        // Fallback material for pink/missing shader prefabs
        private Material _fallbackRoadMaterial;
        
        // Player tracking for dynamic road generation
        private Vector3 _lastGenerationPosition = Vector3.zero;
        private float _regenerationDistance = 50f; // Regenerate when player moves this far
        private bool _isRegenerating = false;
        
        // Generation queue
        private Queue<Vector2Int> _cellGenerationQueue = new Queue<Vector2Int>();
        private bool _isGenerating;
        
        #pragma warning disable 0067 // Event may be used in future for chunked loading
        public event Action<FantasyWorldCell> OnCellGenerated;
        #pragma warning restore 0067
        public event Action<string> OnGenerationProgress;
        
        private void Awake()
        {
             // DESIGN SPIKE: Inject Glass UI for immediate testing
            if (gameObject.GetComponent<GlassUI>() == null)
            {
                gameObject.AddComponent<GlassUI>();
            }

            _osmFetcher = gameObject.AddComponent<OSMDataFetcher>();
            
            // Create fallback material for URP (fixes pink Synty prefabs)
            CreateFallbackMaterial();
            
            // Create parent objects if not assigned
            if (buildingsParent == null)
            {
                buildingsParent = new GameObject("Buildings").transform;
                buildingsParent.SetParent(transform);
            }
            if (vegetationParent == null)
            {
                vegetationParent = new GameObject("Vegetation").transform;
                vegetationParent.SetParent(transform);
            }
            if (propsParent == null)
            {
                propsParent = new GameObject("Props").transform;
                propsParent.SetParent(transform);
            }
            if (pathsParent == null)
            {
                pathsParent = new GameObject("Paths").transform;
                // CRITICAL: Place paths at world origin, NOT parented to FantasyWorldGenerator
                // This ensures road coordinates from MapboxTileRenderer.LatLonToWorld align correctly
                pathsParent.position = Vector3.zero;
                pathsParent.rotation = Quaternion.identity;
                // Parent to scene root (not this transform) to avoid coordinate offsets
                pathsParent.SetParent(null);
            }
        }
        
        /// <summary>
        /// Track player movement and regenerate roads when they've moved far enough
        /// </summary>
        private void Update()
        {
            if (!_isInitialized || _isRegenerating) return;
            if (playerTransform == null) return;
            
            // Check if player has moved far enough to regenerate
            float distanceMoved = Vector3.Distance(playerTransform.position, _lastGenerationPosition);
            if (distanceMoved > _regenerationDistance)
            {
                // Convert player world position to lat/lon and regenerate
                StartCoroutine(RegenerateAtPlayerPosition());
            }
        }
        
        /// <summary>
        /// Regenerate roads centered on player's current position
        /// </summary>
        private IEnumerator RegenerateAtPlayerPosition()
        {
            if (_isRegenerating) yield break;
            _isRegenerating = true;
            
            Logger.Log($"Player moved {_regenerationDistance}m - regenerating roads at new position", "FantasyWorld");
            
            // Get player's world position and convert to lat/lon
            Vector3 playerPos = playerTransform.position;
            _lastGenerationPosition = playerPos;
            
            // Convert world position to lat/lon using MapboxTileRenderer
            var mapbox = FindAnyObjectByType<ApexCitadels.Map.MapboxTileRenderer>();
            if (mapbox != null)
            {
                var (newLat, newLon) = mapbox.WorldToLatLon(playerPos);
                _originLat = newLat;
                _originLon = newLon;
                
                Logger.Log($"New origin: {_originLat:F6}, {_originLon:F6}", "FantasyWorld");
                
                // Clear existing roads
                if (pathsParent != null)
                {
                    foreach (Transform child in pathsParent)
                    {
                        Destroy(child.gameObject);
                    }
                }
                
                // Fetch and generate new roads
                yield return StartCoroutine(GenerateWorldCoroutine());
            }
            
            _isRegenerating = false;
        }
        
        /// <summary>
        /// Create a fallback URP-compatible material for pink/missing shader prefabs
        /// </summary>
        private void CreateFallbackMaterial()
        {
            // Try to find URP Lit shader
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            }
            if (urpShader == null)
            {
                urpShader = Shader.Find("Sprites/Default"); // Last resort
            }
            
            if (urpShader != null)
            {
                _fallbackRoadMaterial = new Material(urpShader);
                _fallbackRoadMaterial.name = "FallbackRoadMaterial";
                // Cobblestone-ish gray color
                _fallbackRoadMaterial.color = new Color(0.45f, 0.42f, 0.38f, 1f);
                Logger.Log($"Created fallback road material with shader: {urpShader.name}", "FantasyWorld");
            }
            else
            {
                Logger.LogWarning("Could not find URP shader for fallback material", "FantasyWorld");
            }
        }
        
        /// <summary>
        /// Apply fallback material to fix pink/magenta missing shader issues
        /// Smart Version: Adapts Standard materials to URP Lit to preserve textures
        /// </summary>
        private void ApplyFallbackMaterial(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var newMaterials = new Material[renderer.sharedMaterials.Length];
                bool modificationsMade = false;

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    Material oldMat = renderer.sharedMaterials[i];
                    
                    // Check if material is broken (Standard shader in URP or Error shader)
                    bool isBroken = false;
                    if (oldMat == null || oldMat.shader == null || 
                        oldMat.shader.name.Contains("Standard") || 
                        oldMat.shader.name.Contains("Hidden/InternalErrorShader") ||
                        oldMat.shader.name == "Hidden/InternalErrorShader")
                    {
                        isBroken = true;
                    }

                    if (isBroken)
                    {
                        // Attempt to fix by swapping shader to URP Lit
                        if (oldMat != null)
                        {
                            // Create new URP material instance
                            Material newMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                            newMat.name = oldMat.name + "_URP_Fix";
                            
                            // Try to carry over the main texture (Albedo)
                            if (oldMat.HasProperty("_MainTex") && oldMat.GetTexture("_MainTex") != null)
                            {
                                newMat.SetTexture("_BaseMap", oldMat.GetTexture("_MainTex"));
                                // If original had a color tint, try to carry it over, otherwise white
                                if (oldMat.HasProperty("_Color"))
                                     newMat.SetColor("_BaseColor", oldMat.GetColor("_Color"));
                                else
                                     newMat.SetColor("_BaseColor", Color.white);
                            }
                            else
                            {
                                // No texture found? Use the grey fallback material
                                if (_fallbackRoadMaterial != null)
                                {
                                    newMat = _fallbackRoadMaterial;
                                }
                            }
                            
                            newMaterials[i] = newMat;
                            modificationsMade = true;
                        }
                    }
                    else
                    {
                        // Keep good material
                        newMaterials[i] = oldMat;
                    }
                }
                
                if (modificationsMade)
                {
                    renderer.sharedMaterials = newMaterials;
                }
            }
        }
        
        /// <summary>
        /// Initialize the generator at a specific geographic location
        /// </summary>
        public void Initialize(double latitude, double longitude)
        {
            _originLat = latitude;
            _originLon = longitude;
            _isInitialized = true;
            
            // Ensure pathsParent exists (may have been lost if scene-root object was destroyed)
            EnsurePathsParentExists();
            
            Logger.Log($"FantasyWorldGenerator initialized at {latitude}, {longitude}", "FantasyWorld");
        }
        
        /// <summary>
        /// Ensure pathsParent exists - find existing or create new
        /// </summary>
        private void EnsurePathsParentExists()
        {
            if (pathsParent == null)
            {
                // Try to find existing Paths object in scene
                var existingPaths = GameObject.Find("Paths");
                if (existingPaths != null)
                {
                    pathsParent = existingPaths.transform;
                    Logger.Log("Found existing Paths container in scene", "FantasyWorld");
                }
                else
                {
                    // Create new
                    pathsParent = new GameObject("Paths").transform;
                    pathsParent.position = Vector3.zero;
                    pathsParent.rotation = Quaternion.identity;
                    Logger.Log("Created new Paths container", "FantasyWorld");
                }
            }
        }
        
        /// <summary>
        /// Set the generation radius (for Ground View mode)
        /// </summary>
        public void SetGenerationRadius(float radiusMeters)
        {
            config.radiusMeters = radiusMeters;
            Logger.Log($"Generation radius set to {radiusMeters}m", "FantasyWorld");
        }
        
        /// <summary>
        /// Clear all generated world objects (for mode transitions)
        /// </summary>
        public void ClearWorld()
        {
            Logger.Log("Clearing fantasy world...", "FantasyWorld");
            
            // Clear buildings
            if (buildingsParent != null)
            {
                foreach (Transform child in buildingsParent)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Clear vegetation
            if (vegetationParent != null)
            {
                foreach (Transform child in vegetationParent)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Clear props
            if (propsParent != null)
            {
                foreach (Transform child in propsParent)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Clear paths
            if (pathsParent != null)
            {
                foreach (Transform child in pathsParent)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Clear cell tracking
            _cells.Clear();
            _cellGenerationQueue.Clear();
            _isGenerating = false;
            _proceduralBuildingCount = 0; // Reset counter
            
            Logger.Log("Fantasy world cleared", "FantasyWorld");
        }
        
        /// <summary>
        /// Public coroutine for generating world (for DualModeController)
        /// </summary>
        public IEnumerator GenerateWorldCoroutine()
        {
            GenerateWorld();
            
            // Wait for generation to complete
            while (_isGenerating)
            {
                yield return null;
            }
            
            // Give a little extra time for final setup
            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// Generate fantasy world around the current location
        /// </summary>
        public void GenerateWorld()
        {
            if (!_isInitialized)
            {
                Logger.LogError("Generator not initialized. Call Initialize() first.", "FantasyWorld");
                return;
            }
            
            // Try to load prefab library from Resources if not assigned
            if (prefabLibrary == null)
            {
                prefabLibrary = UnityEngine.Resources.Load<FantasyPrefabLibrary>("MainFantasyPrefabLibrary");
                if (prefabLibrary == null)
                    prefabLibrary = UnityEngine.Resources.Load<FantasyPrefabLibrary>("FantasyPrefabLibrary");
                
                // Also try finding any library in the project
                if (prefabLibrary == null)
                {
                    var allLibraries = UnityEngine.Resources.FindObjectsOfTypeAll<FantasyPrefabLibrary>();
                    if (allLibraries != null && allLibraries.Length > 0)
                    {
                        prefabLibrary = allLibraries[0];
                        Logger.Log($"Found prefab library: {prefabLibrary.name}", "FantasyWorld");
                    }
                }
            }
            
            if (prefabLibrary == null)
            {
                Logger.LogWarning("No prefab library assigned! Using procedural fallback buildings.", "FantasyWorld");
                // Continue anyway - we'll use procedural buildings
            }
            else
            {
                Logger.Log($"Using prefab library: {prefabLibrary.name}", "FantasyWorld");
            }
            
            OnGenerationProgress?.Invoke("Fetching map data...");
            
            // Fetch OSM data for user's specified location
            // Coordinates will be converted using tile center as origin in ConvertCoordinatesToWorldSpace
            // Use larger radius (200m instead of 100m) to ensure roads are visible
            float fetchRadius = Mathf.Max(config.radiusMeters, 200f);
            Logger.Log($"Fetching OSM data for {_originLat:F6}, {_originLon:F6} with radius {fetchRadius}m", "FantasyWorld");
            
            _osmFetcher.FetchArea(_originLat, _originLon, fetchRadius, OnOSMDataReceived);
        }
        
        /// <summary>
        /// Called when OSM data is received
        /// </summary>
        private void OnOSMDataReceived(OSMAreaData areaData)
        {
            if (areaData == null)
            {
                Logger.LogError("Failed to fetch OSM data", "FantasyWorld");
                OnGenerationProgress?.Invoke("ERROR: Failed to fetch map data");
                return;
            }
            
            // Convert to unified format
            var osmData = OSMData.FromAreaData(areaData);
            
            // Convert all lat/lon coordinates to world space
            ConvertCoordinatesToWorldSpace(osmData);
            
            // Pass to debug view if available
            if (debugView == null)
            {
                debugView = GetComponent<FantasyWorldDebugView>();
                if (debugView == null)
                {
                    debugView = gameObject.AddComponent<FantasyWorldDebugView>();
                    debugView.generator = this;
                }
            }
            debugView?.SetOSMData(osmData);
            
            OnGenerationProgress?.Invoke($"Found {osmData.Buildings.Count} buildings, {osmData.Roads.Count} roads");
            Logger.Log($"OSM Data: {osmData.Buildings.Count} buildings, {osmData.Roads.Count} roads, {osmData.Areas.Count} areas", "FantasyWorld");
            
            // Check for empty data and use fallback if necessary
            // If less than 5 buildings found, assume data is too sparse and fill it in
            if (osmData.Buildings.Count < 5)
            {
                Logger.LogWarning($"Only found {osmData.Buildings.Count} buildings - triggering procedural fallback...", "FantasyWorld");
                OnGenerationProgress?.Invoke("Map data sparse. Filling with fantasy city...");
                GenerateFallbackCity(osmData);
            }

            // Generate the world
            StartCoroutine(GenerateWorldCoroutine(osmData));
        }

        private void GenerateFallbackCity(OSMData osmData)
        {
            // Create a procedural grid typical of American suburbs
            float blockSize = 80f; // meters
            int gridSize = 4;
            
            // Generate roads
            for (int x = -gridSize; x <= gridSize; x++)
            {
                // Vertical road
                var vRoad = new OSMRoad { Width = 6f, RoadType = "residential" };
                vRoad.Points.Add(new Vector3(x * blockSize, 0, -gridSize * blockSize));
                vRoad.Points.Add(new Vector3(x * blockSize, 0, gridSize * blockSize));
                osmData.Roads.Add(vRoad);
                
                // Horizontal road
                var hRoad = new OSMRoad { Width = 6f, RoadType = "residential" };
                hRoad.Points.Add(new Vector3(-gridSize * blockSize, 0, x * blockSize));
                hRoad.Points.Add(new Vector3(gridSize * blockSize, 0, x * blockSize));
                osmData.Roads.Add(hRoad);
            }
            
            // Generate houses in blocks
            for (int x = -gridSize; x < gridSize; x++)
            {
                for (int z = -gridSize; z < gridSize; z++)
                {
                    // 4 houses per block
                    CreateFallbackHouse(osmData, (x + 0.25f) * blockSize, (z + 0.25f) * blockSize);
                    CreateFallbackHouse(osmData, (x + 0.75f) * blockSize, (z + 0.25f) * blockSize);
                    CreateFallbackHouse(osmData, (x + 0.25f) * blockSize, (z + 0.75f) * blockSize);
                    CreateFallbackHouse(osmData, (x + 0.75f) * blockSize, (z + 0.75f) * blockSize);
                }
            }
        }
        
        private void CreateFallbackHouse(OSMData data, float x, float z)
        {
            if (UnityEngine.Random.value < 0.2f) return; // Leave some empty lots
            
            var building = new OSMBuilding { BuildingType = "residential", Levels = 1 };
            float size = 12f;
            
            // Create world points directly
            building.WorldPoints.Add(new Vector3(x - size/2, 0, z - size/2));
            building.WorldPoints.Add(new Vector3(x + size/2, 0, z - size/2));
            building.WorldPoints.Add(new Vector3(x + size/2, 0, z + size/2));
            building.WorldPoints.Add(new Vector3(x - size/2, 0, z + size/2));
            
            data.Buildings.Add(building);
        }
        
        /// <summary>
        /// Convert all lat/lon coordinates to world space relative to origin
        /// Uses MapboxTileRenderer.LatLonToWorld for EXACT alignment with satellite tiles
        /// </summary>
        private void ConvertCoordinatesToWorldSpace(OSMData osmData)
        {
            // Get MapboxTileRenderer to use its coordinate conversion
            var mapbox = FindAnyObjectByType<ApexCitadels.Map.MapboxTileRenderer>();
            
            // Get scale from Mapbox if available, otherwise use default
            _worldScale = GetMapboxScale();
            _prefabScale = 1f / _worldScale;
            
            Logger.Log($"World scale: {_worldScale:F2} m/unit, Prefab scale: {_prefabScale:F4}", "FantasyWorld");
            
            if (mapbox != null)
            {
                // Debug: Log positions to verify alignment
                Logger.Log($"MapboxTileRenderer position: {mapbox.transform.position}", "FantasyWorld");
                Logger.Log($"FantasyWorldGenerator position: {transform.position}", "FantasyWorld");
                Logger.Log($"PathsParent position: {(pathsParent != null ? pathsParent.position.ToString() : "null")}", "FantasyWorld");
                
                Logger.Log("Using MapboxTileRenderer.LatLonToWorld for coordinate conversion", "FantasyWorld");
                
                // Convert buildings using Mapbox's coordinate system
                foreach (var building in osmData.Buildings)
                {
                    building.WorldPoints = new List<Vector3>();
                    foreach (var p in building.FootprintPoints)
                    {
                        // p.x is lon, p.y is lat (from OSM parser)
                        Vector3 worldPos = mapbox.LatLonToWorld(p.y, p.x);
                        building.WorldPoints.Add(worldPos);
                    }
                }
                
                // Convert roads using Mapbox's coordinate system
                int roadDebugCount = 0;
                foreach (var road in osmData.Roads)
                {
                    road.Points.Clear();
                    foreach (var p in road.LatLonPoints)
                    {
                        // p.x is lon, p.y is lat
                        Vector3 worldPos = mapbox.LatLonToWorld(p.y, p.x);
                        road.Points.Add(worldPos);
                    }
                    
                    // Debug: Log first 2 roads to verify coordinate conversion
                    if (roadDebugCount < 2 && road.Points.Count > 0)
                    {
                        Logger.Log($"Road '{road.Name}' first point: lat={road.LatLonPoints[0].y:F6}, lon={road.LatLonPoints[0].x:F6} -> world {road.Points[0]}", "FantasyWorld");
                        roadDebugCount++;
                    }
                }
                
                // Convert areas using Mapbox's coordinate system
                foreach (var area in osmData.Areas)
                {
                    area.WorldPoints = new List<Vector3>();
                    foreach (var p in area.Polygon)
                    {
                        // p.x is lon, p.y is lat
                        Vector3 worldPos = mapbox.LatLonToWorld(p.y, p.x);
                        area.WorldPoints.Add(worldPos);
                    }
                }
            }
            else
            {
                // Fallback: use manual calculation (may not align with Mapbox)
                Logger.LogWarning("No MapboxTileRenderer - using fallback coordinate conversion", "FantasyWorld");
                
                double metersPerDegreeLat = 110540;
                double metersPerDegreeLon = 111320 * Math.Cos(_originLat * Math.PI / 180);
                
                // Convert buildings
                foreach (var building in osmData.Buildings)
                {
                    building.WorldPoints = new List<Vector3>();
                    foreach (var p in building.FootprintPoints)
                    {
                        float x = (float)((p.x - _originLon) * metersPerDegreeLon / _worldScale);
                        float z = (float)((p.y - _originLat) * metersPerDegreeLat / _worldScale);
                        building.WorldPoints.Add(new Vector3(x, 0, z));
                    }
                }
                
                // Convert roads
                foreach (var road in osmData.Roads)
                {
                    road.ConvertToWorldSpaceScaled(_originLat, _originLon, _worldScale);
                }
                
                // Convert areas
                foreach (var area in osmData.Areas)
                {
                    area.WorldPoints = new List<Vector3>();
                    foreach (var p in area.Polygon)
                    {
                        float x = (float)((p.x - _originLon) * metersPerDegreeLon / _worldScale);
                        float z = (float)((p.y - _originLat) * metersPerDegreeLat / _worldScale);
                        area.WorldPoints.Add(new Vector3(x, 0, z));
                    }
                }
            }
        }
        
        /// <summary>
        /// Get the meters-per-unit scale from MapboxTileRenderer for alignment
        /// </summary>
        private float GetMapboxScale()
        {
            var mapbox = FindAnyObjectByType<ApexCitadels.Map.MapboxTileRenderer>();
            if (mapbox != null)
            {
                // MapboxTileRenderer uses tileWorldSize = 80 units for each tile
                // At zoom 14, each tile is about 1222m at equator, less at higher latitudes
                // Calculate meters per world unit to match Mapbox coordinate system
                double tileMeters = 40075016.686 * Math.Cos(_originLat * Math.PI / 180) / Math.Pow(2, 14);
                float tileWorldSize = 80f; // Default from MapboxTileRenderer
                float scale = (float)(tileMeters / tileWorldSize);
                Logger.Log($"Mapbox scale: {scale:F2} meters per world unit (tile size: {tileMeters:F0}m)", "FantasyWorld");
                return scale;
            }
            Logger.LogWarning("No MapboxTileRenderer found - using 1:1 meter scale", "FantasyWorld");
            return 1f; // No scaling if no Mapbox
        }

        
        /// <summary>
        /// Generate world in coroutine for smooth performance
        /// </summary>
        private IEnumerator GenerateWorldCoroutine(OSMData osmData)
        {
            _isGenerating = true;
            
            // ============================================
            // ROADS ONLY MODE - Disable everything except roads for debugging
            // Set to false when ready to add more elements
            // ============================================
            bool ROADS_ONLY_MODE = false; // ENABLED: Full fantasy world generation!
            
            // Show stats in console
            string stats = $"Generating World: {osmData.Buildings.Count} Buildings, {osmData.Roads.Count} Roads, {osmData.Areas.Count} Areas";
            if (ROADS_ONLY_MODE) stats += " [ROADS ONLY MODE]";
            Logger.Log(stats, "FantasyWorld");
            OnGenerationProgress?.Invoke(stats);
            
            // Analyze neighborhood context
            var context = NeighborhoodContext.DefaultSuburban();
            if (osmData.Buildings.Count > 0)
            {
                var firstBuilding = osmData.Buildings[0];
                var centroid = firstBuilding.CalculateCentroid();
                context = NeighborhoodContext.Analyze(
                    new Vector2(centroid.x, centroid.z),
                    osmData.Buildings,
                    osmData.Roads,
                    osmData.Areas
                );
            }
            
            // Generate ground (only if Mapbox isn't providing it)
            CreateGround();
            
            // Generate roads
            if (config.generatePaths)
            {
                OnGenerationProgress?.Invoke("Paving roads...");
                yield return StartCoroutine(GenerateRoadsCoroutine(osmData.Roads));
            }

            // ============================================
            // SKIP EVERYTHING BELOW IN ROADS ONLY MODE
            // ============================================
            if (ROADS_ONLY_MODE)
            {
                Logger.Log("ROADS ONLY MODE: Skipping buildings, vegetation, props", "FantasyWorld");
                _isGenerating = false;
                OnGenerationProgress?.Invoke("Roads complete! (ROADS ONLY MODE)");
                Logger.Log("Fantasy world generation complete", "FantasyWorld");
                yield break;
            }

            // Generate buildings
            if (config.generateBuildings)
            {
                OnGenerationProgress?.Invoke("Generating buildings...");
                yield return StartCoroutine(GenerateBuildingsCoroutine(osmData.Buildings, context));
            }
            
            // Generate vegetation
            if (config.generateVegetation)
            {
                OnGenerationProgress?.Invoke("Growing forests...");
                yield return StartCoroutine(GenerateVegetationCoroutine(osmData));
                
                // Generate ground cover (grass clumps, rocks) in open areas
                OnGenerationProgress?.Invoke("Spreading grass and ground cover...");
                yield return StartCoroutine(GenerateGroundCoverCoroutine(osmData));
            }
            
            // Generate props
            if (config.generateProps)
            {
                OnGenerationProgress?.Invoke("Adding details...");
                yield return StartCoroutine(GeneratePropsCoroutine(osmData.Buildings));
            }
            
            // Generate street signs
            OnGenerationProgress?.Invoke("Placing street signs...");
            GenerateStreetSigns(osmData.Roads);
            
            // Register with mini-map
            RegisterWithMiniMap(osmData);
            
            _isGenerating = false;
            OnGenerationProgress?.Invoke("World generation complete!");
            Logger.Log("Fantasy world generation complete", "FantasyWorld");
        }
        
        private void GenerateStreetSigns(List<OSMRoad> roads)
        {
            var signGenerator = GetComponent<FantasyStreetSigns>();
            if (signGenerator == null)
            {
                signGenerator = gameObject.AddComponent<FantasyStreetSigns>();
            }
            
            // Create signs parent
            Transform signsParent = transform.Find("StreetSigns");
            if (signsParent == null)
            {
                signsParent = new GameObject("StreetSigns").transform;
                signsParent.SetParent(transform);
            }
            
            signGenerator.GenerateSigns(roads, signsParent);
        }
        
        private void RegisterWithMiniMap(OSMData osmData)
        {
            var miniMap = FindFirstObjectByType<MiniMapUI>();
            if (miniMap == null) return;
            
            // Collect building positions
            var buildingPositions = new System.Collections.Generic.List<Vector3>();
            foreach (Transform child in buildingsParent)
            {
                buildingPositions.Add(child.position);
            }
            miniMap.RegisterBuildings(buildingPositions);
            
            // Collect road paths
            var roadPaths = new System.Collections.Generic.List<System.Collections.Generic.List<Vector3>>();
            foreach (var road in osmData.Roads)
            {
                if (road.Points != null && road.Points.Count > 1)
                {
                    roadPaths.Add(road.Points);
                }
            }
            miniMap.RegisterRoads(roadPaths);
        }
        
        /// <summary>
        /// DEBUG: Create a highly visible marker at the origin to help visualize coordinate system
        /// </summary>
        private void CreateOriginMarker()
        {
            // Red cube at origin
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "DEBUG_ORIGIN_MARKER";
            marker.transform.SetParent(buildingsParent);
            marker.transform.position = new Vector3(0, 5f, 0); // 5m up
            marker.transform.localScale = new Vector3(3f, 10f, 3f); // Tall red pillar
            
            var renderer = marker.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
            mat.color = Color.red;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.red);
            renderer.material = mat;
            
            Logger.Log("DEBUG: Created red origin marker at (0, 5, 0)", "FantasyWorld");
        }
        
        /// <summary>
        /// Generate fantasy buildings from OSM building data
        /// </summary>
        private IEnumerator GenerateBuildingsCoroutine(List<OSMBuilding> buildings, NeighborhoodContext context)
        {
            int count = 0;
            int prefabCount = 0;
            int proceduralCount = 0;
            
            foreach (var osmBuilding in buildings)
            {
                // Classify the building
                var fantasyType = BuildingClassifier.ClassifyBuilding(osmBuilding, context);
                var size = BuildingClassifier.ClassifySize(osmBuilding.CalculateArea());
                
                // Calculate position (centroid of building footprint)
                var position = osmBuilding.CalculateCentroid();
                
                // Log first few buildings for debugging
                if (count < 3)
                {
                    Logger.Log($"Building {count}: Type={fantasyType}, Size={size}, Position={position}, OSMType={osmBuilding.BuildingType}", "FantasyWorld");
                }
                
                // Calculate rotation
                float rotation = 0f;
                if (config.randomizeRotation)
                {
                    // Align to footprint orientation if available, otherwise random
                    rotation = osmBuilding.CalculateOrientation();
                    if (rotation == 0f)
                    {
                        rotation = UnityEngine.Random.Range(0f, 360f);
                    }
                }
                
                // Calculate dimensions from OSM data (already in scaled world units)
                var dimensions = osmBuilding.CalculateDimensions();
                
                // Get appropriate prefab - or use procedural fallback
                GameObject building;
                var prefab = prefabLibrary?.GetBuilding(size, fantasyType);
                if (prefab != null)
                {
                    // Use Synty prefab - scale to match world scale
                    building = Instantiate(prefab, position, Quaternion.Euler(0, rotation, 0), buildingsParent);
                    
                    // Apply base scale to match world coordinate scale
                    building.transform.localScale = Vector3.one * _prefabScale;
                    prefabCount++;
                }
                else
                {
                    // Create procedural fantasy building as fallback (also scaled)
                    building = CreateProceduralBuilding(position, rotation, dimensions * _prefabScale, fantasyType, size);
                    building.transform.SetParent(buildingsParent);
                    proceduralCount++;
                }
                
                building.name = $"Building_{fantasyType}_{count}";
                
                // Add metadata component
                var meta = building.AddComponent<FantasyBuildingMeta>();
                meta.BuildingType = fantasyType;
                meta.Size = size;
                meta.OriginalOSMType = osmBuilding.BuildingType;
                meta.FootprintArea = osmBuilding.CalculateArea();
                
                count++;
                
                // Yield periodically for smooth performance
                if (count % config.maxBuildingsPerFrame == 0)
                {
                    OnGenerationProgress?.Invoke($"Generating buildings... {count}/{buildings.Count}");
                    yield return null;
                }
            }
            
            Logger.Log($"Generated {count} buildings ({prefabCount} prefabs, {proceduralCount} procedural) at scale {_prefabScale:F3}", "FantasyWorld");
        }
        
        /// <summary>
        /// Generate vegetation (trees, bushes) in appropriate areas
        /// </summary>
        private IEnumerator GenerateVegetationCoroutine(OSMData osmData)
        {
            int treeCount = 0;
            int bushCount = 0;
            
            // Get all building footprints for collision avoidance
            var buildingAreas = new List<Bounds>();
            foreach (var building in osmData.Buildings)
            {
                var centroid = building.CalculateCentroid();
                var dims = building.CalculateDimensions();
                buildingAreas.Add(new Bounds(centroid, dims * 1.2f)); // Slight padding
            }
            
            // Generate trees in parks and green areas
            foreach (var area in osmData.Areas)
            {
                if (area.AreaType != "park" && area.AreaType != "forest" && area.AreaType != "grass")
                    continue;
                
                var centroid = area.CalculateCentroid();
                float radius = area.CalculateApproximateRadius();
                
                // Calculate tree count based on area and density
                float areaSize = Mathf.PI * radius * radius;
                int targetTrees = Mathf.RoundToInt(areaSize * config.treeDensity / 100f);
                targetTrees = Mathf.Min(targetTrees, 50); // Cap per area
                
                for (int i = 0; i < targetTrees; i++)
                {
                    // Random position within area
                    var offset = UnityEngine.Random.insideUnitCircle * radius;
                    var pos = new Vector3(centroid.x + offset.x, 0, centroid.z + offset.y);
                    
                    // Check for building collision
                    bool blocked = false;
                    foreach (var bb in buildingAreas)
                    {
                        if (bb.Contains(pos))
                        {
                            blocked = true;
                            break;
                        }
                    }
                    if (blocked) continue;
                    
                    // Get tree prefab or create procedural tree
                    bool useFantasyTree = area.AreaType == "forest" || UnityEngine.Random.value < 0.2f;
                    var treePrefab = prefabLibrary?.GetTree(useFantasyTree);
                    
                    GameObject tree;
                    float rotation = UnityEngine.Random.Range(0f, 360f);
                    float scale = UnityEngine.Random.Range(0.8f, 1.2f);
                    
                    if (treePrefab != null)
                    {
                        tree = Instantiate(treePrefab, pos, Quaternion.Euler(0, rotation, 0), vegetationParent);
                        tree.transform.localScale = Vector3.one * scale * _prefabScale;
                    }
                    else
                    {
                        tree = CreateProceduralTree(pos, rotation);
                        tree.transform.SetParent(vegetationParent);
                        tree.transform.localScale = Vector3.one * scale * _prefabScale;
                    }
                    tree.name = $"Tree_{treeCount}";
                    
                    treeCount++;
                }
                
                // Generate bushes
                int targetBushes = Mathf.RoundToInt(areaSize * config.bushDensity / 50f);
                targetBushes = Mathf.Min(targetBushes, 30);
                
                for (int i = 0; i < targetBushes; i++)
                {
                    var offset = UnityEngine.Random.insideUnitCircle * radius;
                    var pos = new Vector3(centroid.x + offset.x, 0, centroid.z + offset.y);
                    
                    var bushPrefab = prefabLibrary?.GetBush();
                    
                    float rotation = UnityEngine.Random.Range(0f, 360f);
                    float scale = UnityEngine.Random.Range(0.6f, 1.0f);
                    
                    GameObject bush;
                    if (bushPrefab != null)
                    {
                        bush = Instantiate(bushPrefab, pos, Quaternion.Euler(0, rotation, 0), vegetationParent);
                        bush.transform.localScale = Vector3.one * scale * _prefabScale;
                    }
                    else
                    {
                        bush = CreateProceduralBush(pos, rotation);
                        bush.transform.SetParent(vegetationParent);
                        bush.transform.localScale = Vector3.one * scale * _prefabScale;
                    }
                    bush.name = $"Bush_{bushCount}";
                    
                    bushCount++;
                }
                
                if ((treeCount + bushCount) % config.maxVegetationPerFrame == 0)
                {
                    yield return null;
                }
            }
            
            // Also scatter some vegetation along roads (suburban feel)
            foreach (var road in osmData.Roads)
            {
                if (road.RoadType != "residential" && road.RoadType != "footway") continue;
                
                // Scatter trees along road
                for (int i = 0; i < road.Points.Count - 1; i++)
                {
                    var p1 = road.Points[i];
                    var p2 = road.Points[i + 1];
                    float segmentLength = Vector3.Distance(p1, p2);
                    
                    // One tree every ~20 meters
                    int treesOnSegment = Mathf.RoundToInt(segmentLength / 20f * config.treeDensity);
                    
                    for (int j = 0; j < treesOnSegment; j++)
                    {
                        float t = (j + 0.5f) / treesOnSegment;
                        var midPoint = Vector3.Lerp(p1, p2, t);
                        
                        // Offset to side of road
                        var direction = (p2 - p1).normalized;
                        var perpendicular = new Vector3(-direction.z, 0, direction.x);
                        float sideOffset = (UnityEngine.Random.value < 0.5f ? 1f : -1f) * (3f + UnityEngine.Random.Range(0f, 2f));
                        var pos = midPoint + perpendicular * sideOffset;
                        
                        // Check collision
                        bool blocked = false;
                        foreach (var bb in buildingAreas)
                        {
                            if (bb.Contains(pos))
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (blocked) continue;
                        
                        var treePrefab = prefabLibrary?.GetTree(false);
                        float treeRotation = UnityEngine.Random.Range(0f, 360f);
                        
                        GameObject tree;
                        if (treePrefab != null)
                        {
                            tree = Instantiate(treePrefab, pos, Quaternion.Euler(0, treeRotation, 0), vegetationParent);
                            tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.8f, 1.1f) * _prefabScale;
                        }
                        else
                        {
                            tree = CreateProceduralTree(pos, treeRotation);
                            tree.transform.SetParent(vegetationParent);
                            tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.8f, 1.1f) * _prefabScale;
                        }
                        
                        tree.name = $"StreetTree_{treeCount}";
                        ApplyFallbackMaterial(tree);
                        
                        treeCount++;
                    }
                }
            }
            
            Logger.Log($"Generated {treeCount} trees, {bushCount} bushes", "FantasyWorld");
        }
        
        /// <summary>
        /// Generate ground cover (grass clumps, rocks, small details) scattered across the area
        /// This fills in the "empty" spaces to make the world feel lush and alive
        /// </summary>
        private IEnumerator GenerateGroundCoverCoroutine(OSMData osmData)
        {
            int grassCount = 0;
            int rockCount = 0;
            
            // Build exclusion zones (buildings, roads)
            var exclusionZones = new List<Bounds>();
            
            // Add building footprints with padding
            foreach (var building in osmData.Buildings)
            {
                var centroid = building.CalculateCentroid();
                var dims = building.CalculateDimensions();
                exclusionZones.Add(new Bounds(centroid, dims * 1.5f)); // Extra padding around buildings
            }
            
            // Add road corridors
            foreach (var road in osmData.Roads)
            {
                if (road.Points == null || road.Points.Count < 2) continue;
                for (int i = 0; i < road.Points.Count - 1; i++)
                {
                    var p1 = road.Points[i];
                    var p2 = road.Points[i + 1];
                    var mid = (p1 + p2) / 2f;
                    var length = Vector3.Distance(p1, p2);
                    exclusionZones.Add(new Bounds(mid, new Vector3(road.Width + 2f, 5f, length)));
                }
            }
            
            // Scatter grass clumps in a grid pattern with randomization
            float radius = config.radiusMeters;
            float spacing = 4f; // Base spacing for grass clumps
            
            for (float x = -radius; x < radius; x += spacing)
            {
                for (float z = -radius; z < radius; z += spacing)
                {
                    // Random offset within cell
                    float offsetX = UnityEngine.Random.Range(-spacing * 0.4f, spacing * 0.4f);
                    float offsetZ = UnityEngine.Random.Range(-spacing * 0.4f, spacing * 0.4f);
                    var pos = new Vector3(x + offsetX, 0, z + offsetZ);
                    
                    // Skip if outside generation radius
                    if (pos.magnitude > radius) continue;
                    
                    // Skip if in exclusion zone
                    bool excluded = false;
                    foreach (var zone in exclusionZones)
                    {
                        if (zone.Contains(pos))
                        {
                            excluded = true;
                            break;
                        }
                    }
                    if (excluded) continue;
                    
                    // Random chance to skip (variety)
                    if (UnityEngine.Random.value > 0.7f) continue;
                    
                    // Choose grass or rock
                    bool isRock = UnityEngine.Random.value < 0.1f; // 10% rocks
                    
                    GameObject groundItem;
                    float rotation = UnityEngine.Random.Range(0f, 360f);
                    float scale = UnityEngine.Random.Range(0.6f, 1.2f);
                    
                    if (isRock)
                    {
                        var rockPrefab = prefabLibrary?.GetRock();
                        if (rockPrefab != null)
                        {
                            groundItem = Instantiate(rockPrefab, pos, Quaternion.Euler(0, rotation, 0), vegetationParent);
                        }
                        else
                        {
                            groundItem = CreateProceduralRock(pos);
                            groundItem.transform.SetParent(vegetationParent);
                        }
                        groundItem.name = $"Rock_{rockCount}";
                        rockCount++;
                    }
                    else
                    {
                        var grassPrefab = prefabLibrary?.GetGrassClump();
                        if (grassPrefab != null)
                        {
                            groundItem = Instantiate(grassPrefab, pos, Quaternion.Euler(0, rotation, 0), vegetationParent);
                        }
                        else
                        {
                            groundItem = CreateProceduralGrass(pos);
                            groundItem.transform.SetParent(vegetationParent);
                        }
                        groundItem.name = $"Grass_{grassCount}";
                        grassCount++;
                    }
                    
                    groundItem.transform.localScale = Vector3.one * scale * _prefabScale;
                    ApplyFallbackMaterial(groundItem);
                    
                    // Yield occasionally for smooth performance
                    if ((grassCount + rockCount) % 50 == 0)
                    {
                        yield return null;
                    }
                }
            }
            
            Logger.Log($"Generated ground cover: {grassCount} grass clumps, {rockCount} rocks", "FantasyWorld");
        }
        
        /// <summary>
        /// Create a simple procedural grass clump
        /// </summary>
        private GameObject CreateProceduralGrass(Vector3 position)
        {
            var grass = new GameObject("ProceduralGrass");
            grass.transform.position = position;
            
            // Create several thin quads for grass blades
            for (int i = 0; i < 5; i++)
            {
                var blade = GameObject.CreatePrimitive(PrimitiveType.Quad);
                blade.transform.SetParent(grass.transform);
                blade.transform.localPosition = new Vector3(
                    UnityEngine.Random.Range(-0.3f, 0.3f),
                    0.25f,
                    UnityEngine.Random.Range(-0.3f, 0.3f)
                );
                blade.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);
                blade.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-15f, 15f));
                
                var renderer = blade.GetComponent<Renderer>();
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(
                    UnityEngine.Random.Range(0.2f, 0.35f),
                    UnityEngine.Random.Range(0.45f, 0.6f),
                    UnityEngine.Random.Range(0.1f, 0.2f)
                );
                renderer.material = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                
                // Remove collider from grass
                var collider = blade.GetComponent<Collider>();
                if (collider != null) DestroyImmediate(collider);
            }
            
            return grass;
        }
        
        /// <summary>
        /// Create a simple procedural rock
        /// </summary>
        private GameObject CreateProceduralRock(Vector3 position)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.transform.position = position;
            rock.transform.localScale = new Vector3(
                UnityEngine.Random.Range(0.3f, 0.8f),
                UnityEngine.Random.Range(0.2f, 0.5f),
                UnityEngine.Random.Range(0.3f, 0.8f)
            );
            rock.transform.rotation = Quaternion.Euler(
                UnityEngine.Random.Range(-10f, 10f),
                UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(-10f, 10f)
            );
            
            var renderer = rock.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            float gray = UnityEngine.Random.Range(0.35f, 0.55f);
            mat.color = new Color(gray, gray * 0.95f, gray * 0.9f);
            mat.SetFloat("_Smoothness", 0.1f);
            renderer.material = mat;
            
            return rock;
        }
        
        /// <summary>
        /// Generate props around buildings
        /// </summary>
        private IEnumerator GeneratePropsCoroutine(List<OSMBuilding> buildings)
        {
            int propCount = 0;
            
            foreach (var building in buildings)
            {
                // Random chance to have props
                if (UnityEngine.Random.value > config.propDensity) continue;
                
                var centroid = building.CalculateCentroid();
                var dims = building.CalculateDimensions();
                
                // Props around the building
                int numProps = UnityEngine.Random.Range(1, 4);
                
                for (int i = 0; i < numProps; i++)
                {
                    // Position offset from building
                    float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float distance = Mathf.Max(dims.x, dims.z) * 0.7f + UnityEngine.Random.Range(1f, 3f);
                    var offset = new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
                    var pos = centroid + offset;
                    
                    // Choose prop type
                    PropType propType = (PropType)UnityEngine.Random.Range(0, 8);
                    var propPrefab = prefabLibrary?.GetProp(propType);
                    
                    GameObject prop;
                    if (propPrefab != null)
                    {
                        prop = Instantiate(propPrefab, pos, Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0), propsParent);
                    }
                    else
                    {
                        prop = CreateProceduralProp(pos, propType);
                        prop.transform.SetParent(propsParent);
                    }
                    
                    prop.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.8f, 1.2f) * _prefabScale;
                    prop.name = $"Prop_{propType}_{propCount}";
                    
                    propCount++;
                }
                
                if (propCount % config.maxVegetationPerFrame == 0)
                {
                    yield return null;
                }
            }
            
            Logger.Log($"Generated {propCount} props", "FantasyWorld");
        }
        
        /// <summary>
        /// Get bounds of a prefab
        /// </summary>
        private Bounds GetPrefabBounds(GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one * 5f);
            }
            
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            
            return bounds;
        }
        
        /// <summary>
        /// Create a procedural fantasy building when no Synty prefab is available
        /// </summary>
        private GameObject CreateProceduralBuilding(Vector3 position, float rotation, Vector3 dimensions, FantasyBuildingType type, BuildingSize size)
        {
            // Only log first few to avoid spam
            if (_proceduralBuildingCount < 3)
            {
                Logger.Log($"Creating PROCEDURAL building #{_proceduralBuildingCount} at {position} - Type: {type}, Size: {size}, Dims: {dimensions}", "FantasyWorld");
            }
            _proceduralBuildingCount++;
            
            GameObject building = new GameObject("ProceduralBuilding");
            building.transform.position = position;
            building.transform.rotation = Quaternion.Euler(0, rotation, 0);
            
            // Determine building height based on size
            float height = size switch
            {
                BuildingSize.Tiny => UnityEngine.Random.Range(2f, 3f),
                BuildingSize.Small => UnityEngine.Random.Range(3f, 5f),
                BuildingSize.Medium => UnityEngine.Random.Range(5f, 8f),
                BuildingSize.Large => UnityEngine.Random.Range(8f, 12f),
                BuildingSize.VeryLarge => UnityEngine.Random.Range(10f, 15f),
                BuildingSize.Huge => UnityEngine.Random.Range(12f, 20f),
                _ => 6f
            };
            
            // Ensure dimensions are reasonable - buildings should be visible!
            float width = Mathf.Max(dimensions.x, 5f);  // Minimum 5m wide
            float depth = Mathf.Max(dimensions.z, 5f);  // Minimum 5m deep
            height = Mathf.Max(height, 4f);              // Minimum 4m tall
            
            // Create main building body (cube)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(building.transform);
            body.transform.localPosition = new Vector3(0, height / 2f, 0);
            body.transform.localScale = new Vector3(width, height, depth);
            body.transform.localRotation = Quaternion.identity;
            
            // Make sure the building is on the Default layer and visible
            body.layer = 0; // Default layer
            
            // Choose colors based on building type - use BRIGHT colors for debugging
            Color wallColor = GetBuildingWallColor(type);
            Color roofColor = GetBuildingRoofColor(type);
            
            // Apply material to body - TRY MULTIPLE APPROACHES
            var bodyRenderer = body.GetComponent<Renderer>();
            Material wallMat = null;
            
            // Try URP Lit first
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Unlit/Color"); // Fallback to unlit
            if (shader == null) shader = Shader.Find("Standard");
            
            if (shader != null)
            {
                wallMat = new Material(shader);
                wallMat.color = wallColor;
                if (wallMat.HasProperty("_BaseColor"))
                    wallMat.SetColor("_BaseColor", wallColor);
                if (wallMat.HasProperty("_Smoothness"))
                    wallMat.SetFloat("_Smoothness", 0.1f);
                bodyRenderer.material = wallMat;
            }
            else
            {
                // Ultimate fallback: just set the color on whatever material is there
                bodyRenderer.material.color = wallColor;
                Logger.LogWarning("No URP shader found - using default material!", "FantasyWorld");
            }
            
            // Create peaked roof
            GameObject roof = CreateRoof(width, depth, height, roofColor);
            roof.transform.SetParent(building.transform);
            roof.transform.localPosition = new Vector3(0, height, 0);
            roof.transform.localRotation = Quaternion.identity;
            
            // Add some variety - tower for special buildings
            if (type == FantasyBuildingType.MageTower || type == FantasyBuildingType.GuardTower || type == FantasyBuildingType.Church)
            {
                AddTower(building.transform, Mathf.Min(width, depth) * 0.4f, height * 1.5f, roofColor);
            }
            
            // Add chimney for residential
            if (type == FantasyBuildingType.House || type == FantasyBuildingType.Cottage || type == FantasyBuildingType.Manor)
            {
                AddChimney(building.transform, width, depth, height);
            }
            
            return building;
        }
        
        private Color GetBuildingWallColor(FantasyBuildingType type)
        {
            return type switch
            {
                FantasyBuildingType.Tavern => new Color(0.45f, 0.30f, 0.18f),    // Warm brown
                FantasyBuildingType.Blacksmith => new Color(0.25f, 0.22f, 0.20f), // Dark gray
                FantasyBuildingType.Church => new Color(0.85f, 0.82f, 0.75f),    // Light stone
                FantasyBuildingType.Castle => new Color(0.55f, 0.52f, 0.48f),    // Gray stone
                FantasyBuildingType.MageTower => new Color(0.30f, 0.28f, 0.45f), // Purple-ish
                FantasyBuildingType.Barn => new Color(0.55f, 0.25f, 0.15f),      // Red barn
                FantasyBuildingType.Mill => new Color(0.50f, 0.45f, 0.35f),      // Tan
                _ => new Color(
                    UnityEngine.Random.Range(0.7f, 0.9f),
                    UnityEngine.Random.Range(0.65f, 0.85f),
                    UnityEngine.Random.Range(0.55f, 0.75f)
                ) // Varied warm colors (cottage whites, creams, tans)
            };
        }
        
        private Color GetBuildingRoofColor(FantasyBuildingType type)
        {
            return type switch
            {
                FantasyBuildingType.Church => new Color(0.35f, 0.32f, 0.28f),    // Dark slate
                FantasyBuildingType.Castle => new Color(0.25f, 0.25f, 0.30f),    // Blue-gray
                FantasyBuildingType.MageTower => new Color(0.20f, 0.18f, 0.35f), // Deep purple
                FantasyBuildingType.Barn => new Color(0.30f, 0.15f, 0.10f),      // Dark red
                _ => new Color(
                    UnityEngine.Random.Range(0.35f, 0.50f),
                    UnityEngine.Random.Range(0.20f, 0.35f),
                    UnityEngine.Random.Range(0.10f, 0.20f)
                ) // Varied browns/terracotta
            };
        }
        
        private GameObject CreateRoof(float width, float depth, float baseHeight, Color roofColor)
        {
            GameObject roof = new GameObject("Roof");
            
            // Create a simple peaked roof using stretched cube (wedge approximation)
            float roofHeight = Mathf.Min(width, depth) * 0.4f;
            float roofOverhang = 0.5f;
            
            // Main roof - use two rotated planes to form a peak
            for (int i = 0; i < 2; i++)
            {
                GameObject roofSide = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roofSide.name = $"RoofSide_{i}";
                roofSide.transform.SetParent(roof.transform);
                
                float angle = (i == 0) ? 35f : -35f;
                float xOffset = (i == 0) ? -width * 0.25f : width * 0.25f;
                
                roofSide.transform.localPosition = new Vector3(xOffset, roofHeight * 0.5f, 0);
                roofSide.transform.localRotation = Quaternion.Euler(0, 0, angle);
                roofSide.transform.localScale = new Vector3(width * 0.65f, 0.15f, depth + roofOverhang);
                
                var renderer = roofSide.GetComponent<Renderer>();
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.color = roofColor;
                    mat.SetFloat("_Smoothness", 0.2f);
                    renderer.material = mat;
                }
            }
            
            return roof;
        }
        
        private void AddTower(Transform parent, float radius, float height, Color roofColor)
        {
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "Tower";
            tower.transform.SetParent(parent);
            
            // Position at corner
            float offset = radius * 1.5f;
            tower.transform.localPosition = new Vector3(offset, height / 2f, offset);
            tower.transform.localScale = new Vector3(radius * 2f, height / 2f, radius * 2f);
            
            var renderer = tower.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.6f, 0.58f, 0.55f); // Stone color
                mat.SetFloat("_Smoothness", 0.1f);
                renderer.material = mat;
            }
            
            // Conical roof on tower
            GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cone.name = "TowerRoof";
            cone.transform.SetParent(tower.transform);
            cone.transform.localPosition = new Vector3(0, 0.6f, 0);
            cone.transform.localScale = new Vector3(1.3f, 0.4f, 1.3f);
            
            var coneRenderer = cone.GetComponent<Renderer>();
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = roofColor;
                mat.SetFloat("_Smoothness", 0.2f);
                coneRenderer.material = mat;
            }
        }
        
        private void AddChimney(Transform parent, float buildingWidth, float buildingDepth, float buildingHeight)
        {
            GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chimney.name = "Chimney";
            chimney.transform.SetParent(parent);
            
            float chimneyHeight = 2f;
            float chimneySize = 0.6f;
            
            // Position on roof
            chimney.transform.localPosition = new Vector3(
                buildingWidth * 0.3f,
                buildingHeight + chimneyHeight / 2f + 0.5f,
                buildingDepth * 0.2f
            );
            chimney.transform.localScale = new Vector3(chimneySize, chimneyHeight, chimneySize);
            
            var renderer = chimney.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.45f, 0.25f, 0.15f); // Brick color
                mat.SetFloat("_Smoothness", 0.1f);
                renderer.material = mat;
            }
        }
        
        /// <summary>
        /// Create a procedural tree when no prefab is available
        /// </summary>
        private GameObject CreateProceduralTree(Vector3 position, float rotation)
        {
            GameObject tree = new GameObject("ProceduralTree");
            tree.transform.position = position;
            tree.transform.rotation = Quaternion.Euler(0, rotation, 0);
            
            float trunkHeight = UnityEngine.Random.Range(3f, 5f);
            float trunkRadius = UnityEngine.Random.Range(0.15f, 0.25f);
            float canopyRadius = UnityEngine.Random.Range(2f, 3.5f);
            
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            
            // Trunk (cylinder)
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0, trunkHeight / 2f, 0);
            trunk.transform.localScale = new Vector3(trunkRadius * 2f, trunkHeight / 2f, trunkRadius * 2f);
            
            var trunkRenderer = trunk.GetComponent<Renderer>();
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.35f, 0.22f, 0.12f); // Brown bark
                mat.SetFloat("_Smoothness", 0.1f);
                trunkRenderer.material = mat;
            }
            
            // Canopy (multiple spheres for fullness)
            Color leafColor = new Color(
                UnityEngine.Random.Range(0.15f, 0.35f),
                UnityEngine.Random.Range(0.4f, 0.6f),
                UnityEngine.Random.Range(0.1f, 0.25f)
            );
            
            // Main canopy sphere
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(tree.transform);
            canopy.transform.localPosition = new Vector3(0, trunkHeight + canopyRadius * 0.5f, 0);
            canopy.transform.localScale = Vector3.one * canopyRadius * 2f;
            
            var canopyRenderer = canopy.GetComponent<Renderer>();
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = leafColor;
                mat.SetFloat("_Smoothness", 0.05f);
                canopyRenderer.material = mat;
            }
            
            // Add smaller side spheres for natural look
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f * Mathf.Deg2Rad;
                float offsetDist = canopyRadius * 0.5f;
                float smallerRadius = canopyRadius * UnityEngine.Random.Range(0.5f, 0.7f);
                
                GameObject subCanopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                subCanopy.name = $"Canopy_{i}";
                subCanopy.transform.SetParent(tree.transform);
                subCanopy.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * offsetDist,
                    trunkHeight + canopyRadius * 0.3f + UnityEngine.Random.Range(-0.3f, 0.3f),
                    Mathf.Sin(angle) * offsetDist
                );
                subCanopy.transform.localScale = Vector3.one * smallerRadius * 2f;
                
                var subRenderer = subCanopy.GetComponent<Renderer>();
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.color = leafColor * UnityEngine.Random.Range(0.9f, 1.1f);
                    mat.SetFloat("_Smoothness", 0.05f);
                    subRenderer.material = mat;
                }
            }
            
            return tree;
        }
        
        /// <summary>
        /// Create a procedural bush when no prefab is available
        /// </summary>
        private GameObject CreateProceduralBush(Vector3 position, float rotation)
        {
            GameObject bush = new GameObject("ProceduralBush");
            bush.transform.position = position;
            bush.transform.rotation = Quaternion.Euler(0, rotation, 0);
            
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            
            Color leafColor = new Color(
                UnityEngine.Random.Range(0.15f, 0.30f),
                UnityEngine.Random.Range(0.35f, 0.55f),
                UnityEngine.Random.Range(0.08f, 0.20f)
            );
            
            // Create 2-4 overlapping spheres for bushy look
            int numSpheres = UnityEngine.Random.Range(2, 5);
            for (int i = 0; i < numSpheres; i++)
            {
                float radius = UnityEngine.Random.Range(0.4f, 0.8f);
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-0.3f, 0.3f),
                    radius * 0.8f,
                    UnityEngine.Random.Range(-0.3f, 0.3f)
                );
                
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = $"BushPart_{i}";
                sphere.transform.SetParent(bush.transform);
                sphere.transform.localPosition = offset;
                sphere.transform.localScale = Vector3.one * radius * 2f;
                
                var renderer = sphere.GetComponent<Renderer>();
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.color = leafColor * UnityEngine.Random.Range(0.9f, 1.1f);
                    mat.SetFloat("_Smoothness", 0.05f);
                    renderer.material = mat;
                }
            }
            
            return bush;
        }
        
        /// <summary>
        /// Create a procedural prop when no prefab is available
        /// </summary>
        private GameObject CreateProceduralProp(Vector3 position, PropType propType)
        {
            GameObject prop = new GameObject("ProceduralProp");
            prop.transform.position = position;
            prop.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
            
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            
            switch (propType)
            {
                case PropType.Barrel:
                    CreateBarrel(prop, shader);
                    break;
                case PropType.Crate:
                    CreateCrate(prop, shader);
                    break;
                case PropType.Well:
                    CreateWell(prop, shader);
                    break;
                case PropType.Lantern:
                    CreateLantern(prop, shader);
                    break;
                default:
                    // Default to a simple barrel
                    CreateBarrel(prop, shader);
                    break;
            }
            
            return prop;
        }
        
        private void CreateBarrel(GameObject parent, Shader shader)
        {
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(parent.transform);
            barrel.transform.localPosition = new Vector3(0, 0.5f, 0);
            barrel.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            var renderer = barrel.GetComponent<Renderer>();
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.45f, 0.30f, 0.15f);
                mat.SetFloat("_Smoothness", 0.2f);
                renderer.material = mat;
            }
        }
        
        private void CreateCrate(GameObject parent, Shader shader)
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Crate";
            crate.transform.SetParent(parent.transform);
            crate.transform.localPosition = new Vector3(0, 0.4f, 0);
            crate.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            
            var renderer = crate.GetComponent<Renderer>();
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.55f, 0.40f, 0.20f);
                mat.SetFloat("_Smoothness", 0.1f);
                renderer.material = mat;
            }
        }
        
        private void CreateWell(GameObject parent, Shader shader)
        {
            // Base cylinder
            GameObject wellBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wellBase.name = "WellBase";
            wellBase.transform.SetParent(parent.transform);
            wellBase.transform.localPosition = new Vector3(0, 0.5f, 0);
            wellBase.transform.localScale = new Vector3(1.2f, 0.5f, 1.2f);
            
            var baseRenderer = wellBase.GetComponent<Renderer>();
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.55f, 0.52f, 0.48f);
                mat.SetFloat("_Smoothness", 0.1f);
                baseRenderer.material = mat;
            }
            
            // Roof posts
            for (int i = 0; i < 2; i++)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"Post_{i}";
                post.transform.SetParent(parent.transform);
                float xPos = (i == 0) ? -0.5f : 0.5f;
                post.transform.localPosition = new Vector3(xPos, 1.5f, 0);
                post.transform.localScale = new Vector3(0.1f, 2f, 0.1f);
                
                var postRenderer = post.GetComponent<Renderer>();
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.color = new Color(0.35f, 0.22f, 0.12f);
                    mat.SetFloat("_Smoothness", 0.1f);
                    postRenderer.material = mat;
                }
            }
            
            // Roof
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(parent.transform);
            roof.transform.localPosition = new Vector3(0, 2.7f, 0);
            roof.transform.localScale = new Vector3(1.4f, 0.15f, 0.8f);
            roof.transform.localRotation = Quaternion.Euler(0, 0, 15f);
            
            var roofRenderer = roof.GetComponent<Renderer>();
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.40f, 0.28f, 0.15f);
                mat.SetFloat("_Smoothness", 0.15f);
                roofRenderer.material = mat;
            }
        }
        
        private void CreateLantern(GameObject parent, Shader shader)
        {
            // Post
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Post";
            post.transform.SetParent(parent.transform);
            post.transform.localPosition = new Vector3(0, 1.5f, 0);
            post.transform.localScale = new Vector3(0.08f, 1.5f, 0.08f);
            
            var postRenderer = post.GetComponent<Renderer>();
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.2f, 0.18f, 0.15f);
                mat.SetFloat("_Smoothness", 0.4f);
                postRenderer.material = mat;
            }
            
            // Lantern box
            GameObject lantern = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lantern.name = "LanternBox";
            lantern.transform.SetParent(parent.transform);
            lantern.transform.localPosition = new Vector3(0, 3.2f, 0);
            lantern.transform.localScale = new Vector3(0.3f, 0.4f, 0.3f);
            
            var lanternRenderer = lantern.GetComponent<Renderer>();
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.9f, 0.75f, 0.3f);
                mat.SetFloat("_Smoothness", 0.1f);
                mat.SetFloat("_Metallic", 0f);
                // Make it emissive for a glow effect
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.4f) * 0.5f);
                lanternRenderer.material = mat;
            }
        }

        private void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }
        
        /// <summary>
        /// Get world position from geo coordinates relative to origin
        /// </summary>
        public Vector3 GeoToWorld(double lat, double lon)
        {
            // Simple equirectangular projection
            double metersPerDegreeLat = 111320;
            double metersPerDegreeLon = 111320 * Math.Cos(_originLat * Math.PI / 180);
            
            float x = (float)((lon - _originLon) * metersPerDegreeLon);
            float z = (float)((lat - _originLat) * metersPerDegreeLat);
            
            return new Vector3(x, 0, z);
        }

        private void CreateGround()
        {
            // Check if Mapbox is providing satellite imagery as the ground
            var mapboxRenderer = FindAnyObjectByType<ApexCitadels.Map.MapboxTileRenderer>();
            if (mapboxRenderer != null && mapboxRenderer.gameObject.activeInHierarchy)
            {
                Logger.Log("Using Mapbox satellite tiles as ground - skipping procedural grass", "FantasyWorld");
                // Remove any existing procedural ground
                Transform existingGround = transform.Find("GeneratedGround");
                if (existingGround != null) DestroyImmediate(existingGround.gameObject);
                return;
            }
            
            // Fallback: Create fantasy grass ground if no Mapbox
            Logger.Log("Creating fantasy grass ground (no Mapbox tiles)", "FantasyWorld");
            
            // Check if ground already exists
            Transform existingGround2 = transform.Find("GeneratedGround");
            if (existingGround2 != null) DestroyImmediate(existingGround2.gameObject);

            // Create a large plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GeneratedGround";
            ground.transform.SetParent(transform);
            ground.transform.localPosition = new Vector3(0, -0.05f, 0); // Just barely below buildings
            
            // Texture tiling
            float planeSize = 10f;
            float targetSize = config.radiusMeters * 3.0f;
            float scale = targetSize / planeSize;
            
            ground.transform.localScale = new Vector3(scale, 1, scale);
            
            var renderer = ground.GetComponent<Renderer>();
            if (groundMaterial != null)
            {
                renderer.material = groundMaterial;
                float tileCount = targetSize / 10f;
                renderer.material.mainTextureScale = new Vector2(tileCount, tileCount);
            }
            else
            {
                // Fallback lush green with Noise
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                {
                    renderer.material = new Material(shader);
                    renderer.material.SetFloat("_Smoothness", 0.05f); // Rough
                    renderer.material.color = new Color(0.2f, 0.4f, 0.15f); // Base green color
                    
                    // Generate Organic Grass Texture
                    Color c1 = new Color(0.12f, 0.28f, 0.08f); // Deep Forest Green
                    Color c2 = new Color(0.25f, 0.45f, 0.15f); // Vibrant Grass Green
                    
                    Texture2D tex = GenerateNoiseTexture(512, 512, c1, c2, 15f);
                    renderer.material.mainTexture = tex;
                    renderer.material.mainTextureScale = new Vector2(targetSize / 30f, targetSize / 30f);
                }
            }
            
            // Ensure the ground receives shadows
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private Texture2D GenerateNoiseTexture(int width, int height, Color colorA, Color colorB, float scale)
        {
            Texture2D tex = new Texture2D(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float xCoord = (float)x / width * scale;
                    float yCoord = (float)y / height * scale;
                    float sample = Mathf.PerlinNoise(xCoord, yCoord);
                    // Add some high frequency noise for detail
                    float detail = Mathf.PerlinNoise(xCoord * 10f, yCoord * 10f) * 0.2f;
                    tex.SetPixel(x, y, Color.Lerp(colorA, colorB, Mathf.Clamp01(sample + detail)));
                }
            }
            tex.Apply();
            return tex;
        }

        private IEnumerator GenerateRoadsCoroutine(List<OSMRoad> roads)
        {
            // Check if we have Synty prefabs to use - validate they actually exist
            bool usePrefabs = false;
            if (prefabLibrary != null && 
                prefabLibrary.cobblestoneSegments != null && 
                prefabLibrary.cobblestoneSegments.Length > 0)
            {
                // Verify at least one prefab is not null (GUIDs may be invalid)
                int validPrefabs = 0;
                foreach (var prefab in prefabLibrary.cobblestoneSegments)
                {
                    if (prefab != null) validPrefabs++;
                }
                
                if (validPrefabs > 0)
                {
                    usePrefabs = true;
                    Logger.Log($"Using {validPrefabs} valid Synty cobblestone prefabs for roads", "FantasyWorld");
                }
                else
                {
                    Logger.LogWarning($"Cobblestone prefabs array has {prefabLibrary.cobblestoneSegments.Length} entries but all are null/missing. Run 'Apex Citadels > Populate Prefab Library' to fix.", "FantasyWorld");
                }
            }
            
            if (usePrefabs)
            {
                yield return GenerateRoadsWithPrefabs(roads);
            }
            else
            {
                Logger.Log("No valid path prefabs found - using procedural cobblestone mesh", "FantasyWorld");
                yield return GenerateRoadsWithMesh(roads);
            }
        }
        
        /// <summary>
        /// Generate roads using Synty prefab segments placed along the path
        /// </summary>
        private IEnumerator GenerateRoadsWithPrefabs(List<OSMRoad> roads)
        {
            int count = 0;
            int prefabsPlaced = 0;
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            
            // Build list of valid (non-null) prefabs - FILTER for straight segments only
            // We want consistent straight road pieces, not corners/intersections/grass variants
            var validPrefabs = new List<GameObject>();
            GameObject preferredPrefab = null;
            
            foreach (var p in prefabLibrary.cobblestoneSegments)
            {
                if (p == null) continue;
                
                string name = p.name.ToLower();
                
                // Skip unwanted types
                bool isCorner = name.Contains("corner") || name.Contains("turn");
                bool isIntersection = name.Contains("cross") || name.Contains("intersection") || name.Contains("4way") || name.Contains("tsection");
                bool isEnd = name.Contains("end") || name.Contains("cap");
                bool hasGrass = name.Contains("grass") || name.Contains("green") || name.Contains("flower");
                bool isSnow = name.Contains("snow");
                bool isWood = name.Contains("wood");
                bool isDirt = name.Contains("dirt");
                
                // PRIORITIZE: Small cobblestone paths from PolygonKnights
                // Look for: SM_Env_Path_Cobble_01, SM_Env_Path_Cobble_02
                if (name.Contains("cobble") && !name.Contains("stone_01") && !isSnow && !hasGrass)
                {
                    // This is a good cobblestone - prefer Cobble_01 or Cobble_02
                    if (preferredPrefab == null || name.Contains("cobble_01"))
                    {
                        preferredPrefab = p;
                    }
                    validPrefabs.Add(p);
                }
                // Secondary: Stone paths
                else if (name.Contains("stone") && name.Contains("path") && !isSnow && !hasGrass && !isDirt && !isWood)
                {
                    if (!isCorner && !isIntersection && !isEnd)
                    {
                        validPrefabs.Add(p);
                    }
                }
            }
            
            // Use the preferred prefab (small cobblestone) or fall back to first valid
            GameObject singlePrefab = preferredPrefab ?? (validPrefabs.Count > 0 ? validPrefabs[0] : null);
            
            if (singlePrefab == null)
            {
                Logger.LogError("No valid cobblestone prefabs found - falling back to mesh", "FantasyWorld");
                yield return GenerateRoadsWithMesh(roads);
                yield break;
            }
            
            Logger.Log($"Using cobblestone prefab: {singlePrefab.name} (selected from {validPrefabs.Count} candidates)", "FantasyWorld");
            Logger.Log($"Cobblestone prefab scale: {_prefabScale:F3} (world scale: {_worldScale:F2})", "FantasyWorld");
            
            // Pre-calculate segment length from sample prefab using LOCAL mesh bounds (not world bounds)
            // World bounds include prefab transform scale and can be misleadingly large
            float nativeSegmentLength = 2f; // Default: 2 meters
            
            // Try to get the actual mesh bounds (unscaled local space)
            var meshFilter = singlePrefab.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                // Use the mesh's local bounds - this is the true unscaled size
                Vector3 meshSize = meshFilter.sharedMesh.bounds.size;
                // Multiply by the prefab's local scale to get actual native size
                Vector3 prefabScale = meshFilter.transform.localScale;
                float actualZ = meshSize.z * Mathf.Abs(prefabScale.z);
                float actualX = meshSize.x * Mathf.Abs(prefabScale.x);
                // Use the larger of X or Z as segment length (path could be oriented either way)
                nativeSegmentLength = Mathf.Max(actualZ, actualX);
                Logger.Log($"Cobblestone mesh bounds: {meshSize}, prefab scale: {prefabScale}, calculated native: {nativeSegmentLength:F2}m", "FantasyWorld");
            }
            else
            {
                // Fallback to renderer bounds but with sanity check
                var sampleRenderer = singlePrefab.GetComponentInChildren<Renderer>();
                if (sampleRenderer != null)
                {
                    nativeSegmentLength = sampleRenderer.bounds.size.z;
                    Logger.Log($"Using renderer bounds fallback: {nativeSegmentLength:F2}m", "FantasyWorld");
                }
            }
            
            // Synty cobblestone paths are typically 2-6 meters - clamp to reasonable range
            if (nativeSegmentLength < 1f || nativeSegmentLength > 10f)
            {
                Logger.Log($"Segment length {nativeSegmentLength:F2}m outside expected range (1-10m), using 3m default", "FantasyWorld");
                nativeSegmentLength = 3f;
            }
            
            float scaledSegmentLength = nativeSegmentLength * _prefabScale; // Segment length after scaling
            
            Logger.Log($"Cobblestone segment: native={nativeSegmentLength:F2}m, scaled={scaledSegmentLength:F3} world units", "FantasyWorld");
            
            // Batch instantiation for performance
            int instantiationsPerFrame = 50;
            int instantiationCount = 0;
            
            foreach (var road in roads)
            {
                if (road.Points == null || road.Points.Count < 2) continue;
                
                // Create container for this road
                GameObject roadContainer = new GameObject($"Road_{road.Name ?? count.ToString()}");
                roadContainer.transform.SetParent(pathsParent);
                roadContainer.transform.localPosition = Vector3.zero;
                
                // Calculate road length in scaled world units
                float totalLength = 0f;
                for (int i = 1; i < road.Points.Count; i++)
                {
                    totalLength += Vector3.Distance(road.Points[i], road.Points[i - 1]);
                }
                
                // Walk along the path and place scaled prefabs
                float distanceCovered = 0f;
                int pointIndex = 0;
                float segmentProgress = 0f;
                
                while (distanceCovered < totalLength && pointIndex < road.Points.Count - 1)
                {
                    Vector3 currentPoint = road.Points[pointIndex];
                    Vector3 nextPoint = road.Points[pointIndex + 1];
                    float segmentDistance = Vector3.Distance(currentPoint, nextPoint);
                    
                    if (segmentDistance < 0.001f)
                    {
                        pointIndex++;
                        continue;
                    }
                    
                    // Calculate position along this segment
                    float t = Mathf.Clamp01(segmentProgress / segmentDistance);
                    Vector3 position = Vector3.Lerp(currentPoint, nextPoint, t);
                    position.y = 0.02f; // Slightly above ground
                    
                    // Calculate rotation to face along path, then add random 180° flip for variety
                    Vector3 direction = (nextPoint - currentPoint).normalized;
                    float baseYRotation = direction.sqrMagnitude > 0.001f ? 
                        Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg : 0f;
                    
                    // Add variation: 50% chance to flip 180° (maintains path direction but varies look)
                    float rotationVariation = UnityEngine.Random.value > 0.5f ? 180f : 0f;
                    Quaternion rotation = Quaternion.Euler(0, baseYRotation + rotationVariation, 0);
                    
                    // Pick random prefab from valid list for variation
                    var prefab = validPrefabs.Count > 1 ? 
                        validPrefabs[UnityEngine.Random.Range(0, validPrefabs.Count)] : singlePrefab;
                    
                    // Instantiate and SCALE to match world
                    // Roads need to be WIDER to match actual road widths
                    // Add slight random scale variation (95%-105%) for organic look
                    float scaleVariation = UnityEngine.Random.Range(0.95f, 1.05f);
                    var segment = Instantiate(prefab, position, rotation, roadContainer.transform);
                    segment.transform.localScale = new Vector3(
                        _prefabScale * _roadWidthMultiplier * scaleVariation,  // Wider with variation
                        _prefabScale * scaleVariation,                          // Height with variation
                        _prefabScale * scaleVariation                           // Length with variation
                    );
                    segment.name = $"Cobble_{prefabsPlaced}";
                    
                    // Fix pink/magenta materials (Synty uses Standard shader, URP shows as pink)
                    ApplyFallbackMaterial(segment);
                    
                    prefabsPlaced++;
                    instantiationCount++;
                    
                    // Yield occasionally for responsiveness
                    if (instantiationCount >= instantiationsPerFrame)
                    {
                        instantiationCount = 0;
                        yield return null;
                    }
                    
                    // Move along the path using SCALED segment length
                    float step = scaledSegmentLength * 0.85f; // Overlap for continuity
                    segmentProgress += step;
                    distanceCovered += step;
                    
                    // Check if we need to move to next path segment
                    while (segmentProgress >= segmentDistance && pointIndex < road.Points.Count - 2)
                    {
                        segmentProgress -= segmentDistance;
                        pointIndex++;
                        if (pointIndex < road.Points.Count - 1)
                        {
                            segmentDistance = Vector3.Distance(road.Points[pointIndex], road.Points[pointIndex + 1]);
                        }
                    }
                    
                    if (pointIndex >= road.Points.Count - 1) break;
                }
                
                count++;
            }
            
            sw.Stop();
            Logger.Log($"Generated {count} roads with {prefabsPlaced} prefab segments in {sw.ElapsedMilliseconds}ms", "FantasyWorld");
        }
        
        /// <summary>
        /// Fallback: Generate roads using procedural mesh
        /// </summary>
        private IEnumerator GenerateRoadsWithMesh(List<OSMRoad> roads)
        {
            int count = 0;
            
            // Pre-generate road texture (share across all roads)
            Color c1 = new Color(0.35f, 0.28f, 0.18f); // Dark Cobblestone
            Color c2 = new Color(0.5f, 0.42f, 0.32f);  // Light Cobblestone
            Texture2D roadTex = GenerateNoiseTexture(128, 128, c1, c2, 8f);
            
            Material sharedRoadMat = null;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                sharedRoadMat = new Material(shader);
                sharedRoadMat.SetFloat("_Smoothness", 0.2f);
                sharedRoadMat.mainTexture = roadTex;
            }
            
            foreach (var road in roads)
            {
                if (road.Points == null || road.Points.Count < 2) continue;
                
                // Create road mesh instead of LineRenderer for better visuals
                GameObject roadObj = new GameObject($"Road_{road.Name ?? count.ToString()}");
                roadObj.transform.SetParent(pathsParent);
                roadObj.transform.localPosition = Vector3.zero;
                
                // Use mesh-based road for better appearance
                MeshFilter meshFilter = roadObj.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = roadObj.AddComponent<MeshRenderer>();
                
                // Generate road mesh
                float width = (road.Width > 0 ? road.Width : config.pathWidth) * 1.5f;
                Mesh roadMesh = GenerateRoadMesh(road.Points, width);
                meshFilter.mesh = roadMesh;
                
                if (sharedRoadMat != null)
                {
                    meshRenderer.material = sharedRoadMat;
                }
                
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = true;

                count++;
                if (count % 10 == 0) yield return null;
            }
            
            Logger.Log($"Generated {count} roads with procedural mesh", "FantasyWorld");
        }
        
        private Mesh GenerateRoadMesh(List<Vector3> points, float width)
        {
            if (points.Count < 2) return null;
            
            Mesh mesh = new Mesh();
            
            int vertCount = points.Count * 2;
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[(points.Count - 1) * 6];
            
            float roadHeight = 0.25f; // Above Mapbox tiles to be visible
            float uvDistance = 0f;
            
            for (int i = 0; i < points.Count; i++)
            {
                // Calculate perpendicular direction
                Vector3 forward;
                if (i < points.Count - 1)
                    forward = (points[i + 1] - points[i]).normalized;
                else
                    forward = (points[i] - points[i - 1]).normalized;
                
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                
                // Left and right vertices
                vertices[i * 2] = points[i] + right * (width / 2f) + Vector3.up * roadHeight;
                vertices[i * 2 + 1] = points[i] - right * (width / 2f) + Vector3.up * roadHeight;
                
                // UVs
                if (i > 0)
                    uvDistance += Vector3.Distance(points[i], points[i - 1]);
                
                uvs[i * 2] = new Vector2(0, uvDistance / width);
                uvs[i * 2 + 1] = new Vector2(1, uvDistance / width);
            }
            
            // Triangles
            for (int i = 0; i < points.Count - 1; i++)
            {
                int baseIndex = i * 6;
                int vertIndex = i * 2;
                
                triangles[baseIndex] = vertIndex;
                triangles[baseIndex + 1] = vertIndex + 2;
                triangles[baseIndex + 2] = vertIndex + 1;
                
                triangles[baseIndex + 3] = vertIndex + 1;
                triangles[baseIndex + 4] = vertIndex + 2;
                triangles[baseIndex + 5] = vertIndex + 3;
            }
            
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
    }
    
    /// <summary>
    /// Metadata attached to generated fantasy buildings
    /// </summary>
    public class FantasyBuildingMeta : MonoBehaviour
    {
        public FantasyBuildingType BuildingType;
        public BuildingSize Size;
        public string OriginalOSMType;
        public float FootprintArea;
    }
}
