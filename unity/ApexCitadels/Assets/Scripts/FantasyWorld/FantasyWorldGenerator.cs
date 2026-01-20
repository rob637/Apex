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
        public float radiusMeters = 500f;
        public float cellSize = 100f; // For chunked loading
        
        [Header("Building Settings")]
        public bool generateBuildings = true;
        public float buildingScaleMultiplier = 1f;
        public bool randomizeRotation = true;
        
        [Header("Vegetation Settings")]
        public bool generateVegetation = true;
        [Range(0f, 1f)]
        public float treeDensity = 0.7f;
        [Range(0f, 1f)]
        public float bushDensity = 0.8f;
        public float minTreeSpacing = 4f;
        
        [Header("Roads/Paths")]
        public bool generatePaths = true;
        public float pathWidth = 4f;
        
        [Header("Props & Details")]
        public bool generateProps = true;
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
        
        // OSM Data Fetcher
        private OSMDataFetcher _osmFetcher;
        
        // Cell management
        private Dictionary<Vector2Int, FantasyWorldCell> _cells = new Dictionary<Vector2Int, FantasyWorldCell>();
        private Vector2Int _lastPlayerCell;
        
        // Current geo location
        private double _originLat;
        private double _originLon;
        private bool _isInitialized;
        
        // Generation queue
        private Queue<Vector2Int> _cellGenerationQueue = new Queue<Vector2Int>();
        private bool _isGenerating;
        
        public event Action<FantasyWorldCell> OnCellGenerated;
        public event Action<string> OnGenerationProgress;
        
        private void Awake()
        {
            _osmFetcher = gameObject.AddComponent<OSMDataFetcher>();
            
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
                pathsParent.SetParent(transform);
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
            
            Logger.Log($"FantasyWorldGenerator initialized at {latitude}, {longitude}", "FantasyWorld");
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
            
            if (prefabLibrary == null)
            {
                Logger.LogError("No prefab library assigned!", "FantasyWorld");
                return;
            }
            
            OnGenerationProgress?.Invoke("Fetching map data...");
            
            // Fetch OSM data for the area (callback-based)
            _osmFetcher.FetchArea(_originLat, _originLon, config.radiusMeters, OnOSMDataReceived);
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
        /// </summary>
        private void ConvertCoordinatesToWorldSpace(OSMData osmData)
        {
            double metersPerDegreeLat = 111320;
            double metersPerDegreeLon = 111320 * Math.Cos(_originLat * Math.PI / 180);
            
            // Convert buildings
            foreach (var building in osmData.Buildings)
            {
                building.WorldPoints = new List<Vector3>();
                foreach (var p in building.FootprintPoints)
                {
                    // p.x is lon, p.y is lat (from OSM parser)
                    float x = (float)((p.x - _originLon) * metersPerDegreeLon);
                    float z = (float)((p.y - _originLat) * metersPerDegreeLat);
                    building.WorldPoints.Add(new Vector3(x, 0, z));
                }
            }
            
            // Convert roads
            foreach (var road in osmData.Roads)
            {
                road.ConvertToWorldSpace(_originLat, _originLon);
            }
            
            // Convert areas
            foreach (var area in osmData.Areas)
            {
                area.WorldPoints = new List<Vector3>();
                foreach (var p in area.Polygon)
                {
                    float x = (float)((p.x - _originLon) * metersPerDegreeLon);
                    float z = (float)((p.y - _originLat) * metersPerDegreeLat);
                    area.WorldPoints.Add(new Vector3(x, 0, z));
                }
            }
        }

        
        /// <summary>
        /// Generate world in coroutine for smooth performance
        /// </summary>
        private IEnumerator GenerateWorldCoroutine(OSMData osmData)
        {
            _isGenerating = true;
            
            // Show stats in console
            string stats = $"Generating World: {osmData.Buildings.Count} Buildings, {osmData.Roads.Count} Roads, {osmData.Areas.Count} Areas";
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
            
            // Generate ground
            CreateGround();
            
            // Generate roads
            if (config.generatePaths)
            {
                OnGenerationProgress?.Invoke("Paving roads...");
                yield return StartCoroutine(GenerateRoadsCoroutine(osmData.Roads));
            }

            // Generate ground
            CreateGround();
            
            // Generate roads
            if (config.generatePaths)
            {
                OnGenerationProgress?.Invoke("Paving roads...");
                yield return StartCoroutine(GenerateRoadsCoroutine(osmData.Roads));
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
        /// Generate fantasy buildings from OSM building data
        /// </summary>
        private IEnumerator GenerateBuildingsCoroutine(List<OSMBuilding> buildings, NeighborhoodContext context)
        {
            int count = 0;
            
            foreach (var osmBuilding in buildings)
            {
                // Classify the building
                var fantasyType = BuildingClassifier.ClassifyBuilding(osmBuilding, context);
                var size = BuildingClassifier.ClassifySize(osmBuilding.CalculateArea());
                
                // Get appropriate prefab
                var prefab = prefabLibrary.GetBuilding(size, fantasyType);
                if (prefab == null)
                {
                    Logger.LogWarning($"No prefab found for {fantasyType} ({size})", "FantasyWorld");
                    continue;
                }
                
                // Calculate position (centroid of building footprint)
                var position = osmBuilding.CalculateCentroid();
                
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
                
                // Calculate scale based on real-world footprint
                var dimensions = osmBuilding.CalculateDimensions();
                var prefabBounds = GetPrefabBounds(prefab);
                
                float scaleX = dimensions.x / Mathf.Max(prefabBounds.size.x, 0.1f);
                float scaleZ = dimensions.z / Mathf.Max(prefabBounds.size.z, 0.1f);
                float scale = Mathf.Max(scaleX, scaleZ) * config.buildingScaleMultiplier;
                
                // Clamp scale to reasonable bounds
                scale = Mathf.Clamp(scale, 0.5f, 3f);
                
                // Instantiate building
                var building = Instantiate(prefab, position, Quaternion.Euler(0, rotation, 0), buildingsParent);
                building.transform.localScale = Vector3.one * scale;
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
            
            Logger.Log($"Generated {count} buildings", "FantasyWorld");
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
                    
                    // Get tree prefab
                    bool useFantasyTree = area.AreaType == "forest" || UnityEngine.Random.value < 0.2f;
                    var treePrefab = prefabLibrary.GetTree(useFantasyTree);
                    if (treePrefab == null) continue;
                    
                    // Instantiate tree
                    float rotation = UnityEngine.Random.Range(0f, 360f);
                    float scale = UnityEngine.Random.Range(0.8f, 1.2f);
                    
                    var tree = Instantiate(treePrefab, pos, Quaternion.Euler(0, rotation, 0), vegetationParent);
                    tree.transform.localScale = Vector3.one * scale;
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
                    
                    var bushPrefab = prefabLibrary.GetBush();
                    if (bushPrefab == null) continue;
                    
                    float rotation = UnityEngine.Random.Range(0f, 360f);
                    float scale = UnityEngine.Random.Range(0.6f, 1.0f);
                    
                    var bush = Instantiate(bushPrefab, pos, Quaternion.Euler(0, rotation, 0), vegetationParent);
                    bush.transform.localScale = Vector3.one * scale;
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
                        
                        var treePrefab = prefabLibrary.GetTree(false);
                        if (treePrefab == null) continue;
                        
                        var tree = Instantiate(treePrefab, pos, Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0), vegetationParent);
                        tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.8f, 1.1f);
                        tree.name = $"StreetTree_{treeCount}";
                        
                        treeCount++;
                    }
                }
            }
            
            Logger.Log($"Generated {treeCount} trees, {bushCount} bushes", "FantasyWorld");
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
                    var propPrefab = prefabLibrary.GetProp(propType);
                    if (propPrefab == null) continue;
                    
                    var prop = Instantiate(propPrefab, pos, Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0), propsParent);
                    prop.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.8f, 1.2f);
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
        /// Clear all generated content
        /// </summary>
        public void ClearWorld()
        {
            // Clear all children of parent objects
            ClearChildren(buildingsParent);
            ClearChildren(vegetationParent);
            ClearChildren(propsParent);
            ClearChildren(pathsParent);
            
            _cells.Clear();
            
            Logger.Log("World cleared", "FantasyWorld");
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
            // Check if ground already exists
            Transform existingGround = transform.Find("GeneratedGround");
            if (existingGround != null) DestroyImmediate(existingGround.gameObject);

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
            
            Logger.Log($"Generated {count} roads", "FantasyWorld");
        }
        
        private Mesh GenerateRoadMesh(List<Vector3> points, float width)
        {
            if (points.Count < 2) return null;
            
            Mesh mesh = new Mesh();
            
            int vertCount = points.Count * 2;
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[(points.Count - 1) * 6];
            
            float roadHeight = 0.15f; // Slightly above ground
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
