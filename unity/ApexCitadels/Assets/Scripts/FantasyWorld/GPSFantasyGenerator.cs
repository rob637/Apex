// ============================================================================
// APEX CITADELS - GPS FANTASY KINGDOM GENERATOR
// Transforms real-world map data into a fantasy kingdom overlay
// Your neighborhood becomes a medieval village!
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Generates a fantasy kingdom based on real GPS coordinates and map data.
    /// Uses OpenStreetMap data to get building footprints and roads.
    /// </summary>
    public class GPSFantasyGenerator : MonoBehaviour
    {
        [Header("=== LOCATION ===")]
        [SerializeField] private double latitude = 38.9065479;
        [SerializeField] private double longitude = -77.2476970;
        [SerializeField] private float worldRadius = 200f; // meters from center to generate
        
        [Header("=== REFERENCES ===")]
        [SerializeField] private FantasyPrefabLibrary prefabLibrary;
        
        [Header("=== DESKTOP SIMULATION ===")]
        [SerializeField] private bool useSimulatedLocation = true;
        [SerializeField] private bool enableWASDMovement = true;
        [SerializeField] private float moveSpeed = 5f;
        
        [Header("=== RUNTIME STATE ===")]
        [SerializeField] private bool isGenerating = false;
        [SerializeField] private string statusMessage = "Ready";
        
        // Parent transforms
        private Transform terrainParent;
        private Transform buildingsParent;
        private Transform roadsParent;
        private Transform vegetationParent;
        
        // Map data
        private List<BuildingData> buildings = new List<BuildingData>();
        private List<RoadData> roads = new List<RoadData>();
        
        // Events
        public event Action OnGenerationComplete;
        public event Action<string> OnStatusChanged;
        
        // Coordinate conversion
        private double originLat;
        private double originLon;
        
        [Serializable]
        public class BuildingData
        {
            public long id;
            public Vector2[] footprint; // local coordinates
            public float height;
            public string buildingType; // house, commercial, etc.
            public Vector3 center;
        }
        
        [Serializable]
        public class RoadData
        {
            public long id;
            public Vector3[] points;
            public float width;
            public string roadType;
        }
        
        private void Start()
        {
            // Try to load prefab library
            if (prefabLibrary == null)
            {
                prefabLibrary = Resources.Load<FantasyPrefabLibrary>("MainFantasyPrefabLibrary");
            }
            
            CreateParentTransforms();
            
            // Auto-generate on start
            StartCoroutine(GenerateFromLocation());
        }
        
        private void Update()
        {
            // Desktop simulation - WASD to move around
            if (enableWASDMovement && useSimulatedLocation)
            {
                HandleDesktopMovement();
            }
        }
        
        private void HandleDesktopMovement()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            
            if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
            {
                // Move the player character instead of regenerating world
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    Vector3 move = new Vector3(h, 0, v) * moveSpeed * Time.deltaTime;
                    player.transform.Translate(move, Space.World);
                }
            }
        }
        
        /// <summary>
        /// Set location and regenerate
        /// </summary>
        public void SetLocation(double lat, double lon)
        {
            latitude = lat;
            longitude = lon;
            StartCoroutine(GenerateFromLocation());
        }
        
        /// <summary>
        /// Use device GPS (mobile only)
        /// </summary>
        public void UseDeviceGPS()
        {
            useSimulatedLocation = false;
            StartCoroutine(GetDeviceLocationAndGenerate());
        }
        
        private IEnumerator GetDeviceLocationAndGenerate()
        {
            SetStatus("Getting GPS location...");
            
            #if UNITY_ANDROID || UNITY_IOS
            if (!Input.location.isEnabledByUser)
            {
                SetStatus("GPS not enabled. Using default location.");
                yield return GenerateFromLocation();
                yield break;
            }
            
            Input.location.Start(1f, 1f);
            
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }
            
            if (Input.location.status == LocationServiceStatus.Running)
            {
                latitude = Input.location.lastData.latitude;
                longitude = Input.location.lastData.longitude;
                SetStatus($"GPS: {latitude:F4}, {longitude:F4}");
            }
            else
            {
                SetStatus("GPS failed. Using default location.");
            }
            #else
            SetStatus("GPS not available on this platform. Using simulated location.");
            #endif
            
            yield return GenerateFromLocation();
        }
        
        private IEnumerator GenerateFromLocation()
        {
            if (isGenerating) yield break;
            isGenerating = true;
            
            SetStatus($"Generating fantasy world at {latitude:F4}, {longitude:F4}...");
            
            // Store origin for coordinate conversion
            originLat = latitude;
            originLon = longitude;
            
            // Clear existing
            ClearWorld();
            yield return null;
            
            // Step 1: Fetch map data from OpenStreetMap
            SetStatus("Fetching map data...");
            yield return FetchMapData();
            
            // Step 2: Generate terrain
            SetStatus("Creating terrain...");
            yield return GenerateTerrain();
            
            // Step 3: Convert buildings to fantasy structures
            SetStatus("Building medieval village...");
            yield return GenerateBuildings();
            
            // Step 4: Convert roads to cobblestone paths
            SetStatus("Laying cobblestone roads...");
            yield return GenerateRoads();
            
            // Step 5: Add vegetation in empty areas
            SetStatus("Growing forest...");
            yield return GenerateVegetation();
            
            // Step 6: Spawn player at origin
            SpawnPlayer();
            
            isGenerating = false;
            SetStatus("Fantasy Kingdom ready!");
            
            Debug.Log($"[GPSFantasy] Generated world with {buildings.Count} buildings, {roads.Count} roads");
            OnGenerationComplete?.Invoke();
        }
        
        private IEnumerator FetchMapData()
        {
            buildings.Clear();
            roads.Clear();
            
            // Calculate bounding box
            float latOffset = worldRadius / 111000f; // ~111km per degree
            float lonOffset = worldRadius / (111000f * Mathf.Cos((float)latitude * Mathf.Deg2Rad));
            
            double south = latitude - latOffset;
            double north = latitude + latOffset;
            double west = longitude - lonOffset;
            double east = longitude + lonOffset;
            
            // Overpass API query for buildings and roads
            string overpassQuery = $@"
                [out:json][timeout:25];
                (
                  way[""building""]({south},{west},{north},{east});
                  way[""highway""]({south},{west},{north},{east});
                );
                out body;
                >;
                out skel qt;
            ";
            
            string url = "https://overpass-api.de/api/interpreter";
            
            using (UnityWebRequest request = UnityWebRequest.Post(url, overpassQuery, "application/x-www-form-urlencoded"))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    ParseOverpassResponse(request.downloadHandler.text);
                    Debug.Log($"[GPSFantasy] Fetched {buildings.Count} buildings, {roads.Count} roads");
                }
                else
                {
                    Debug.LogWarning($"[GPSFantasy] Map fetch failed: {request.error}. Using procedural generation.");
                    GenerateProceduralFallback();
                }
            }
        }
        
        private void ParseOverpassResponse(string json)
        {
            try
            {
                // Parse OSM JSON response
                var data = JsonUtility.FromJson<OverpassResponse>(json);
                if (data == null || data.elements == null)
                {
                    GenerateProceduralFallback();
                    return;
                }
                
                // Build node lookup
                var nodes = new Dictionary<long, Vector2>();
                foreach (var element in data.elements)
                {
                    if (element.type == "node")
                    {
                        nodes[element.id] = new Vector2((float)element.lon, (float)element.lat);
                    }
                }
                
                // Process ways (buildings and roads)
                foreach (var element in data.elements)
                {
                    if (element.type != "way" || element.nodes == null) continue;
                    
                    if (element.tags != null && element.tags.ContainsKey("building"))
                    {
                        var building = new BuildingData
                        {
                            id = element.id,
                            buildingType = GetBuildingType(element.tags),
                            footprint = new Vector2[element.nodes.Length]
                        };
                        
                        Vector3 sum = Vector3.zero;
                        for (int i = 0; i < element.nodes.Length; i++)
                        {
                            if (nodes.TryGetValue(element.nodes[i], out var coord))
                            {
                                Vector3 local = GeoToLocal(coord.y, coord.x);
                                building.footprint[i] = new Vector2(local.x, local.z);
                                sum += local;
                            }
                        }
                        building.center = sum / element.nodes.Length;
                        building.height = EstimateBuildingHeight(element.tags);
                        
                        buildings.Add(building);
                    }
                    else if (element.tags != null && element.tags.ContainsKey("highway"))
                    {
                        var road = new RoadData
                        {
                            id = element.id,
                            roadType = element.tags["highway"],
                            points = new Vector3[element.nodes.Length],
                            width = GetRoadWidth(element.tags["highway"])
                        };
                        
                        for (int i = 0; i < element.nodes.Length; i++)
                        {
                            if (nodes.TryGetValue(element.nodes[i], out var coord))
                            {
                                road.points[i] = GeoToLocal(coord.y, coord.x);
                            }
                        }
                        
                        roads.Add(road);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GPSFantasy] Parse error: {e.Message}. Using procedural fallback.");
                GenerateProceduralFallback();
            }
        }
        
        private void GenerateProceduralFallback()
        {
            // Generate random buildings and roads if map fetch fails
            Debug.Log("[GPSFantasy] Using procedural fallback generation");
            
            // Generate some buildings in a grid pattern
            for (int x = -3; x <= 3; x++)
            {
                for (int z = -3; z <= 3; z++)
                {
                    if (UnityEngine.Random.value > 0.6f) continue;
                    
                    float px = x * 30f + UnityEngine.Random.Range(-5f, 5f);
                    float pz = z * 30f + UnityEngine.Random.Range(-5f, 5f);
                    
                    buildings.Add(new BuildingData
                    {
                        id = x * 100 + z,
                        center = new Vector3(px, 0, pz),
                        buildingType = UnityEngine.Random.value > 0.8f ? "commercial" : "house",
                        height = UnityEngine.Random.Range(5f, 15f)
                    });
                }
            }
            
            // Generate main roads
            roads.Add(new RoadData
            {
                id = 1,
                points = new[] { new Vector3(-worldRadius, 0, 0), new Vector3(worldRadius, 0, 0) },
                width = 8f,
                roadType = "primary"
            });
            roads.Add(new RoadData
            {
                id = 2,
                points = new[] { new Vector3(0, 0, -worldRadius), new Vector3(0, 0, worldRadius) },
                width = 8f,
                roadType = "primary"
            });
        }
        
        private string GetBuildingType(Dictionary<string, string> tags)
        {
            if (tags.TryGetValue("building", out var type))
            {
                switch (type.ToLower())
                {
                    case "house":
                    case "residential":
                    case "detached":
                        return "house";
                    case "commercial":
                    case "retail":
                    case "shop":
                        return "commercial";
                    case "church":
                    case "chapel":
                    case "cathedral":
                        return "religious";
                    case "industrial":
                    case "warehouse":
                        return "industrial";
                    default:
                        return "house";
                }
            }
            return "house";
        }
        
        private float EstimateBuildingHeight(Dictionary<string, string> tags)
        {
            if (tags.TryGetValue("height", out var h))
            {
                if (float.TryParse(h.Replace("m", ""), out var height))
                    return height;
            }
            if (tags.TryGetValue("building:levels", out var levels))
            {
                if (int.TryParse(levels, out var l))
                    return l * 3f; // 3m per floor
            }
            return UnityEngine.Random.Range(5f, 10f);
        }
        
        private float GetRoadWidth(string roadType)
        {
            switch (roadType)
            {
                case "motorway":
                case "trunk":
                case "primary":
                    return 12f;
                case "secondary":
                case "tertiary":
                    return 8f;
                case "residential":
                case "service":
                    return 6f;
                case "footway":
                case "path":
                    return 2f;
                default:
                    return 6f;
            }
        }
        
        /// <summary>
        /// Convert GPS coordinates to local Unity coordinates
        /// </summary>
        private Vector3 GeoToLocal(double lat, double lon)
        {
            // Simple equirectangular projection centered on origin
            double metersPerDegreeLat = 111132.92;
            double metersPerDegreeLon = 111132.92 * Math.Cos(originLat * Math.PI / 180);
            
            float x = (float)((lon - originLon) * metersPerDegreeLon);
            float z = (float)((lat - originLat) * metersPerDegreeLat);
            
            return new Vector3(x, 0, z);
        }
        
        private void CreateParentTransforms()
        {
            terrainParent = CreateOrGetChild("Terrain");
            buildingsParent = CreateOrGetChild("Buildings");
            roadsParent = CreateOrGetChild("Roads");
            vegetationParent = CreateOrGetChild("Vegetation");
        }
        
        private Transform CreateOrGetChild(string name)
        {
            var existing = transform.Find(name);
            if (existing != null) return existing;
            
            var child = new GameObject(name);
            child.transform.SetParent(transform);
            child.transform.localPosition = Vector3.zero;
            return child.transform;
        }
        
        private void ClearWorld()
        {
            foreach (Transform child in buildingsParent) Destroy(child.gameObject);
            foreach (Transform child in roadsParent) Destroy(child.gameObject);
            foreach (Transform child in vegetationParent) Destroy(child.gameObject);
            foreach (Transform child in terrainParent) Destroy(child.gameObject);
        }
        
        private IEnumerator GenerateTerrain()
        {
            // Create ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(terrainParent);
            ground.transform.localPosition = new Vector3(0, -0.01f, 0);
            
            float scale = worldRadius * 2f / 10f;
            ground.transform.localScale = new Vector3(scale, 1, scale);
            
            // Apply grass material
            var renderer = ground.GetComponent<Renderer>();
            var grassMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            grassMat.SetColor("_BaseColor", new Color(0.2f, 0.5f, 0.15f));
            renderer.material = grassMat;
            
            // Set layer for ground
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) ground.layer = groundLayer;
            
            yield return null;
        }
        
        private IEnumerator GenerateBuildings()
        {
            if (prefabLibrary == null)
            {
                Debug.LogWarning("[GPSFantasy] No prefab library - skipping buildings");
                yield break;
            }
            
            int count = 0;
            foreach (var building in buildings)
            {
                // Select prefab based on building type and size
                GameObject prefab = SelectBuildingPrefab(building);
                if (prefab == null) continue;
                
                // Calculate rotation (align with first edge of footprint if available)
                float rotation = 0f;
                if (building.footprint != null && building.footprint.Length >= 2)
                {
                    Vector2 edge = building.footprint[1] - building.footprint[0];
                    rotation = Mathf.Atan2(edge.x, edge.y) * Mathf.Rad2Deg;
                }
                else
                {
                    rotation = UnityEngine.Random.Range(0f, 360f);
                }
                
                // Instantiate
                var instance = Instantiate(prefab, building.center, Quaternion.Euler(0, rotation, 0), buildingsParent);
                instance.name = $"Building_{building.id}";
                
                // Fix materials
                URPMaterialFixer.FixGameObject(instance);
                
                count++;
                if (count % 10 == 0) yield return null;
            }
            
            Debug.Log($"[GPSFantasy] Placed {count} fantasy buildings");
        }
        
        private GameObject SelectBuildingPrefab(BuildingData building)
        {
            var prefabs = prefabLibrary.GetBuildingsForCategory(
                building.buildingType == "house" ? BuildingCategory.Residential :
                building.buildingType == "commercial" ? BuildingCategory.Commercial :
                building.buildingType == "religious" ? BuildingCategory.Religious :
                building.buildingType == "industrial" ? BuildingCategory.Industrial :
                BuildingCategory.Residential
            );
            
            if (prefabs == null || prefabs.Length == 0)
            {
                prefabs = prefabLibrary.GetBuildingsForCategory(BuildingCategory.Residential);
            }
            
            if (prefabs == null || prefabs.Length == 0) return null;
            
            return prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
        }
        
        private IEnumerator GenerateRoads()
        {
            foreach (var road in roads)
            {
                // Create road mesh from polyline
                for (int i = 0; i < road.points.Length - 1; i++)
                {
                    CreateRoadSegment(road.points[i], road.points[i + 1], road.width, roadsParent);
                }
                yield return null;
            }
        }
        
        private void CreateRoadSegment(Vector3 start, Vector3 end, float width, Transform parent)
        {
            Vector3 direction = end - start;
            float length = direction.magnitude;
            if (length < 0.1f) return;
            
            var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "RoadSegment";
            road.transform.SetParent(parent);
            road.transform.position = (start + end) / 2f + Vector3.up * 0.02f;
            road.transform.rotation = Quaternion.LookRotation(direction);
            road.transform.localScale = new Vector3(width, 0.1f, length);
            
            // Cobblestone color
            var renderer = road.GetComponent<Renderer>();
            var roadMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            roadMat.SetColor("_BaseColor", new Color(0.4f, 0.35f, 0.3f));
            renderer.material = roadMat;
        }
        
        private IEnumerator GenerateVegetation()
        {
            if (prefabLibrary == null) yield break;
            
            var trees = prefabLibrary.GetTreePrefabs();
            if (trees == null || trees.Length == 0) yield break;
            
            int treeCount = 0;
            int attempts = 0;
            int maxTrees = 100;
            
            while (treeCount < maxTrees && attempts < 500)
            {
                attempts++;
                
                float x = UnityEngine.Random.Range(-worldRadius, worldRadius);
                float z = UnityEngine.Random.Range(-worldRadius, worldRadius);
                Vector3 pos = new Vector3(x, 0, z);
                
                // Check if position is far from buildings and roads
                bool tooClose = false;
                foreach (var building in buildings)
                {
                    if (Vector3.Distance(pos, building.center) < 15f)
                    {
                        tooClose = true;
                        break;
                    }
                }
                
                if (tooClose) continue;
                
                // Place tree
                var treePrefab = trees[UnityEngine.Random.Range(0, trees.Length)];
                var tree = Instantiate(treePrefab, pos, Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0), vegetationParent);
                tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.8f, 1.2f);
                
                URPMaterialFixer.FixGameObject(tree);
                
                treeCount++;
                if (treeCount % 20 == 0) yield return null;
            }
            
            Debug.Log($"[GPSFantasy] Placed {treeCount} trees");
        }
        
        private void SpawnPlayer()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(0, 1, 0);
                Debug.Log("[GPSFantasy] Player positioned at world origin (your GPS location)");
            }
        }
        
        private void SetStatus(string message)
        {
            statusMessage = message;
            Debug.Log($"[GPSFantasy] {message}");
            OnStatusChanged?.Invoke(message);
        }
        
        // JSON classes for Overpass API
        [Serializable]
        private class OverpassResponse
        {
            public OverpassElement[] elements;
        }
        
        [Serializable]
        private class OverpassElement
        {
            public string type;
            public long id;
            public double lat;
            public double lon;
            public long[] nodes;
            public Dictionary<string, string> tags;
        }
    }
}
