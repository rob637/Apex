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
            public string streetName; // Real street name from OSM
        }
        
        private void Start()
        {
            // Try to load prefab library
            if (prefabLibrary == null)
            {
                prefabLibrary = UnityEngine.Resources.Load<FantasyPrefabLibrary>("MainFantasyPrefabLibrary");
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
            
            // Step 6: Mark YOUR house at origin (center of map)
            CreateHomeMarker();
            
            // Step 7: Spawn player at origin
            SpawnPlayer();
            
            isGenerating = false;
            SetStatus("Fantasy Kingdom ready!");
            
            Debug.Log($"[GPSFantasy] Generated world with {buildings.Count} buildings, {roads.Count} roads");
            Debug.Log($"[GPSFantasy] YOUR HOME is at the center (0, 0) - look for the banner!");
            OnGenerationComplete?.Invoke();
        }
        
        private void CreateHomeMarker()
        {
            // Create a special marker at YOUR location (center of the map)
            var homeMarker = new GameObject("YourHome_Marker");
            homeMarker.transform.SetParent(buildingsParent);
            homeMarker.transform.position = Vector3.zero;
            
            // Create a tall banner pole
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "BannerPole";
            pole.transform.SetParent(homeMarker.transform);
            pole.transform.localPosition = new Vector3(0, 5f, 0);
            pole.transform.localScale = new Vector3(0.15f, 5f, 0.15f);
            var poleMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            poleMat.SetColor("_BaseColor", new Color(0.5f, 0.3f, 0.1f));
            pole.GetComponent<Renderer>().material = poleMat;
            
            // Create banner flag
            var banner = GameObject.CreatePrimitive(PrimitiveType.Quad);
            banner.name = "Banner";
            banner.transform.SetParent(homeMarker.transform);
            banner.transform.localPosition = new Vector3(0.8f, 8.5f, 0);
            banner.transform.localScale = new Vector3(1.5f, 2f, 1f);
            var bannerMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            bannerMat.SetColor("_BaseColor", new Color(0.8f, 0.1f, 0.1f)); // Red banner
            banner.GetComponent<Renderer>().material = bannerMat;
            UnityEngine.Object.Destroy(banner.GetComponent<Collider>()); // No collision for banner
            
            // Create "YOUR HOME" text
            var textObj = new GameObject("HomeText");
            textObj.transform.SetParent(homeMarker.transform);
            textObj.transform.localPosition = new Vector3(0, 11f, 0);
            
            var textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = "★ YOUR HOME ★";
            textMesh.fontSize = 48;
            textMesh.characterSize = 0.15f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = new Color(1f, 0.85f, 0f); // Gold
            textMesh.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            // Make text always face camera (billboard)
            var billboard = textObj.AddComponent<BillboardText>();
            
            // Create a glowing base
            var glowBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glowBase.name = "GlowBase";
            glowBase.transform.SetParent(homeMarker.transform);
            glowBase.transform.localPosition = new Vector3(0, 0.1f, 0);
            glowBase.transform.localScale = new Vector3(3f, 0.1f, 3f);
            var glowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            glowMat.SetColor("_BaseColor", new Color(1f, 0.9f, 0.3f)); // Golden glow
            glowMat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.2f) * 2f);
            glowMat.EnableKeyword("_EMISSION");
            glowBase.GetComponent<Renderer>().material = glowMat;
            
            Debug.Log("[GPSFantasy] Created home marker at origin - YOUR HOUSE!");
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
            
            // Create POST request with form data
            WWWForm form = new WWWForm();
            form.AddField("data", overpassQuery);
            
            using (UnityWebRequest request = UnityWebRequest.Post(url, form))
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
                Debug.Log($"[GPSFantasy] Parsing response ({json.Length} chars)...");
                
                // Manual JSON parsing since JsonUtility can't handle Dictionary<string,string>
                // Parse elements array
                int elementsStart = json.IndexOf("\"elements\"");
                if (elementsStart < 0)
                {
                    Debug.LogWarning("[GPSFantasy] No elements found in response");
                    GenerateProceduralFallback();
                    return;
                }
                
                // Build node lookup first (nodes have lat/lon)
                var nodes = new Dictionary<long, Vector2>();
                
                // Find all nodes
                int searchPos = 0;
                while ((searchPos = json.IndexOf("\"type\":\"node\"", searchPos)) >= 0)
                {
                    int blockStart = json.LastIndexOf('{', searchPos);
                    int blockEnd = FindMatchingBrace(json, blockStart);
                    if (blockEnd > blockStart)
                    {
                        string nodeBlock = json.Substring(blockStart, blockEnd - blockStart + 1);
                        
                        long id = ExtractLong(nodeBlock, "\"id\":");
                        double lat = ExtractDouble(nodeBlock, "\"lat\":");
                        double lon = ExtractDouble(nodeBlock, "\"lon\":");
                        
                        if (id > 0)
                        {
                            nodes[id] = new Vector2((float)lon, (float)lat);
                        }
                    }
                    searchPos = blockEnd > 0 ? blockEnd : searchPos + 1;
                }
                
                Debug.Log($"[GPSFantasy] Found {nodes.Count} nodes");
                
                // Find all ways (buildings and roads)
                searchPos = 0;
                while ((searchPos = json.IndexOf("\"type\":\"way\"", searchPos)) >= 0)
                {
                    int blockStart = json.LastIndexOf('{', searchPos);
                    int blockEnd = FindMatchingBrace(json, blockStart);
                    if (blockEnd > blockStart)
                    {
                        string wayBlock = json.Substring(blockStart, blockEnd - blockStart + 1);
                        
                        long id = ExtractLong(wayBlock, "\"id\":");
                        long[] nodeIds = ExtractNodeIds(wayBlock);
                        var tags = ExtractTags(wayBlock);
                        
                        // Is it a building?
                        if (tags.ContainsKey("building"))
                        {
                            var building = new BuildingData
                            {
                                id = id,
                                buildingType = GetBuildingType(tags),
                                footprint = new Vector2[nodeIds.Length]
                            };
                            
                            Vector3 sum = Vector3.zero;
                            int validNodes = 0;
                            for (int i = 0; i < nodeIds.Length; i++)
                            {
                                if (nodes.TryGetValue(nodeIds[i], out var coord))
                                {
                                    Vector3 local = GeoToLocal(coord.y, coord.x);
                                    building.footprint[i] = new Vector2(local.x, local.z);
                                    sum += local;
                                    validNodes++;
                                }
                            }
                            if (validNodes > 0)
                            {
                                building.center = sum / validNodes;
                                building.height = EstimateBuildingHeight(tags);
                                buildings.Add(building);
                            }
                        }
                        // Is it a road?
                        else if (tags.ContainsKey("highway"))
                        {
                            string streetName = "";
                            if (tags.TryGetValue("name", out var name))
                                streetName = name;
                            
                            var road = new RoadData
                            {
                                id = id,
                                roadType = tags["highway"],
                                points = new Vector3[nodeIds.Length],
                                width = GetRoadWidth(tags["highway"]),
                                streetName = streetName
                            };
                            
                            int validPoints = 0;
                            for (int i = 0; i < nodeIds.Length; i++)
                            {
                                if (nodes.TryGetValue(nodeIds[i], out var coord))
                                {
                                    road.points[validPoints] = GeoToLocal(coord.y, coord.x);
                                    validPoints++;
                                }
                            }
                            
                            if (validPoints >= 2)
                            {
                                // Trim array to valid points
                                Array.Resize(ref road.points, validPoints);
                                roads.Add(road);
                                
                                if (!string.IsNullOrEmpty(streetName))
                                {
                                    Debug.Log($"[GPSFantasy] Road: {streetName}");
                                }
                            }
                        }
                    }
                    searchPos = blockEnd > 0 ? blockEnd : searchPos + 1;
                }
                
                Debug.Log($"[GPSFantasy] Parsed {buildings.Count} buildings, {roads.Count} roads");
                
                if (buildings.Count == 0 && roads.Count == 0)
                {
                    Debug.LogWarning("[GPSFantasy] No features found, using fallback");
                    GenerateProceduralFallback();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GPSFantasy] Parse error: {e.Message}\n{e.StackTrace}");
                GenerateProceduralFallback();
            }
        }
        
        private int FindMatchingBrace(string json, int start)
        {
            if (start < 0 || json[start] != '{') return -1;
            int depth = 0;
            for (int i = start; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}') depth--;
                if (depth == 0) return i;
            }
            return -1;
        }
        
        private long ExtractLong(string block, string key)
        {
            int idx = block.IndexOf(key);
            if (idx < 0) return 0;
            idx += key.Length;
            int end = block.IndexOfAny(new char[] { ',', '}', ']' }, idx);
            if (end < 0) return 0;
            string val = block.Substring(idx, end - idx).Trim();
            return long.TryParse(val, out long result) ? result : 0;
        }
        
        private double ExtractDouble(string block, string key)
        {
            int idx = block.IndexOf(key);
            if (idx < 0) return 0;
            idx += key.Length;
            int end = block.IndexOfAny(new char[] { ',', '}', ']' }, idx);
            if (end < 0) return 0;
            string val = block.Substring(idx, end - idx).Trim();
            return double.TryParse(val, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out double result) ? result : 0;
        }
        
        private long[] ExtractNodeIds(string block)
        {
            var result = new List<long>();
            int nodesStart = block.IndexOf("\"nodes\"");
            if (nodesStart < 0) return result.ToArray();
            
            int arrStart = block.IndexOf('[', nodesStart);
            int arrEnd = block.IndexOf(']', arrStart);
            if (arrStart < 0 || arrEnd < 0) return result.ToArray();
            
            string arrContent = block.Substring(arrStart + 1, arrEnd - arrStart - 1);
            string[] parts = arrContent.Split(',');
            foreach (var part in parts)
            {
                if (long.TryParse(part.Trim(), out long id))
                {
                    result.Add(id);
                }
            }
            return result.ToArray();
        }
        
        private Dictionary<string, string> ExtractTags(string block)
        {
            var tags = new Dictionary<string, string>();
            int tagsStart = block.IndexOf("\"tags\"");
            if (tagsStart < 0) return tags;
            
            int objStart = block.IndexOf('{', tagsStart);
            int objEnd = FindMatchingBrace(block, objStart);
            if (objStart < 0 || objEnd < 0) return tags;
            
            string tagsBlock = block.Substring(objStart + 1, objEnd - objStart - 1);
            
            // Parse key-value pairs: "key":"value"
            int pos = 0;
            while (pos < tagsBlock.Length)
            {
                // Find key
                int keyStart = tagsBlock.IndexOf('"', pos);
                if (keyStart < 0) break;
                int keyEnd = tagsBlock.IndexOf('"', keyStart + 1);
                if (keyEnd < 0) break;
                string key = tagsBlock.Substring(keyStart + 1, keyEnd - keyStart - 1);
                
                // Find value
                int valStart = tagsBlock.IndexOf('"', keyEnd + 1);
                if (valStart < 0) break;
                int valEnd = tagsBlock.IndexOf('"', valStart + 1);
                if (valEnd < 0) break;
                string val = tagsBlock.Substring(valStart + 1, valEnd - valStart - 1);
                
                tags[key] = val;
                pos = valEnd + 1;
            }
            
            return tags;
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
            // Select appropriate building array based on type
            GameObject[] prefabs = null;
            
            switch (building.buildingType)
            {
                case "house":
                    prefabs = prefabLibrary.houses;
                    if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.cottages;
                    if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.smallHouses;
                    break;
                case "commercial":
                    prefabs = prefabLibrary.shops;
                    if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.taverns;
                    if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.marketStalls;
                    break;
                case "religious":
                    prefabs = prefabLibrary.churches;
                    if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.chapels;
                    break;
                case "industrial":
                    prefabs = prefabLibrary.warehouses;
                    if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.barns;
                    if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.workshops;
                    break;
                default:
                    prefabs = prefabLibrary.houses;
                    break;
            }
            
            // Fallback to any available buildings
            if (prefabs == null || prefabs.Length == 0)
            {
                prefabs = prefabLibrary.houses;
            }
            if (prefabs == null || prefabs.Length == 0)
            {
                prefabs = prefabLibrary.cottages;
            }
            
            if (prefabs == null || prefabs.Length == 0) return null;
            
            return prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
        }
        
        private IEnumerator GenerateRoads()
        {
            HashSet<string> signedStreets = new HashSet<string>(); // Track which streets already have signs
            
            foreach (var road in roads)
            {
                // Create road mesh from polyline
                for (int i = 0; i < road.points.Length - 1; i++)
                {
                    CreateRoadSegment(road.points[i], road.points[i + 1], road.width, roadsParent);
                }
                
                // Create street sign at start of named roads (only once per street name)
                if (!string.IsNullOrEmpty(road.streetName) && !signedStreets.Contains(road.streetName))
                {
                    signedStreets.Add(road.streetName);
                    
                    if (road.points.Length >= 2)
                    {
                        Vector3 signPos = road.points[0] + Vector3.up * 0.1f;
                        Vector3 direction = (road.points[1] - road.points[0]).normalized;
                        CreateStreetSign(signPos, direction, road.streetName);
                    }
                }
                
                yield return null;
            }
            
            Debug.Log($"[GPSFantasy] Created {signedStreets.Count} street signs");
        }
        
        private void CreateStreetSign(Vector3 position, Vector3 roadDirection, string streetName)
        {
            // Convert to fantasy-style street name
            string fantasyName = ConvertToFantasyStreetName(streetName);
            
            // Create sign post
            var signPost = new GameObject($"StreetSign_{streetName}");
            signPost.transform.SetParent(roadsParent);
            signPost.transform.position = position + Vector3.right * 2f; // Offset from road center
            
            // Create the pole
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(signPost.transform);
            pole.transform.localPosition = new Vector3(0, 1.5f, 0);
            pole.transform.localScale = new Vector3(0.1f, 1.5f, 0.1f);
            var poleMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            poleMat.SetColor("_BaseColor", new Color(0.4f, 0.25f, 0.1f)); // Wood brown
            pole.GetComponent<Renderer>().material = poleMat;
            
            // Create the sign board
            var signBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signBoard.name = "SignBoard";
            signBoard.transform.SetParent(signPost.transform);
            signBoard.transform.localPosition = new Vector3(0, 3f, 0);
            signBoard.transform.localScale = new Vector3(2.5f, 0.6f, 0.1f);
            signBoard.transform.rotation = Quaternion.LookRotation(Vector3.Cross(roadDirection, Vector3.up));
            var boardMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            boardMat.SetColor("_BaseColor", new Color(0.6f, 0.4f, 0.2f)); // Lighter wood
            signBoard.GetComponent<Renderer>().material = boardMat;
            
            // Create 3D text for street name
            var textObj = new GameObject("StreetNameText");
            textObj.transform.SetParent(signBoard.transform);
            textObj.transform.localPosition = new Vector3(0, 0, -0.06f);
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = new Vector3(0.4f, 1.6f, 1f);
            
            var textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = fantasyName;
            textMesh.fontSize = 32;
            textMesh.characterSize = 0.1f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = new Color(0.1f, 0.05f, 0f);
            textMesh.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            // Add text on back side too
            var textObjBack = new GameObject("StreetNameTextBack");
            textObjBack.transform.SetParent(signBoard.transform);
            textObjBack.transform.localPosition = new Vector3(0, 0, 0.06f);
            textObjBack.transform.localRotation = Quaternion.Euler(0, 180, 0);
            textObjBack.transform.localScale = new Vector3(0.4f, 1.6f, 1f);
            
            var textMeshBack = textObjBack.AddComponent<TextMesh>();
            textMeshBack.text = fantasyName;
            textMeshBack.fontSize = 32;
            textMeshBack.characterSize = 0.1f;
            textMeshBack.anchor = TextAnchor.MiddleCenter;
            textMeshBack.alignment = TextAlignment.Center;
            textMeshBack.color = new Color(0.1f, 0.05f, 0f);
            textMeshBack.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        
        private string ConvertToFantasyStreetName(string realName)
        {
            // Convert real street names to fantasy equivalents
            // "Mashie Drive" -> "Mashie Way" or keep original with fantasy suffix
            
            string result = realName;
            
            // Replace common suffixes with fantasy versions
            var replacements = new Dictionary<string, string>
            {
                { " Drive", " Way" },
                { " Street", " Lane" },
                { " Road", " Path" },
                { " Avenue", " Road" },
                { " Boulevard", " Thoroughfare" },
                { " Court", " Close" },
                { " Circle", " Ring" },
                { " Lane", " Trail" },
                { " Place", " Square" },
                { " Way", " Passage" }
            };
            
            foreach (var kvp in replacements)
            {
                if (result.EndsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    result = result.Substring(0, result.Length - kvp.Key.Length) + kvp.Value;
                    break;
                }
            }
            
            return result;
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
            
            // Get tree prefabs from the library
            var treePrefabs = prefabLibrary.trees;
            if (treePrefabs == null || treePrefabs.Length == 0) treePrefabs = prefabLibrary.treesOak;
            if (treePrefabs == null || treePrefabs.Length == 0) treePrefabs = prefabLibrary.treesPine;
            if (treePrefabs == null || treePrefabs.Length == 0) yield break;
            
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
                var treePrefab = treePrefabs[UnityEngine.Random.Range(0, treePrefabs.Length)];
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
