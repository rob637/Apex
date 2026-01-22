// ============================================================================
// APEX CITADELS - GPS FANTASY KINGDOM GENERATOR
// Transforms real-world map data into a fantasy kingdom overlay
// Your neighborhood becomes a medieval village!
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        // Default: 6709 Reynard Drive, Springfield, VA 22152
        [SerializeField] private double latitude = 38.7700021;
        [SerializeField] private double longitude = -77.2481544;
        [SerializeField] private float worldRadius = 250f; // meters from center (250m = ~2-3 blocks, denser buildings)
        
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
            public float area; // square meters - calculated from footprint
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
        
        [ContextMenu("Clear OSM Cache")]
        public void ClearOSMCache()
        {
            string cacheKey = $"osm_{latitude:F4}_{longitude:F4}_{worldRadius:F0}";
            PlayerPrefs.DeleteKey(cacheKey);
            PlayerPrefs.Save();
            Debug.Log($"[GPSFantasy] Cleared cache for {cacheKey}");
        }
        
        [ContextMenu("Log Building Positions")]
        public void LogBuildingPositions()
        {
            Debug.Log($"[GPSFantasy] Total buildings in list: {buildings.Count}");
            int nearHome = 0;
            int onReynard = 0;
            
            // Sort buildings by distance from home
            var sortedBuildings = buildings.OrderBy(b => Vector3.Distance(b.center, Vector3.zero)).ToList();
            
            Debug.Log("[GPSFantasy] === CLOSEST 20 BUILDINGS TO HOME ===");
            for (int i = 0; i < Math.Min(20, sortedBuildings.Count); i++)
            {
                var b = sortedBuildings[i];
                float dist = Vector3.Distance(b.center, Vector3.zero);
                Debug.Log($"[GPSFantasy] #{i+1}: Building {b.id} at ({b.center.x:F1}, {b.center.z:F1}), dist={dist:F1}m, area={b.area:F0}sqm");
            }
            
            // Count buildings near Reynard Drive (roughly x: -100 to 300, z: -150 to 200)
            foreach (var b in buildings)
            {
                float dist = Vector3.Distance(b.center, Vector3.zero);
                if (dist < 50f) nearHome++;
                
                // Approximate Reynard Drive corridor
                if (b.center.x > -150 && b.center.x < 300 && b.center.z > -200 && b.center.z < 250)
                {
                    onReynard++;
                }
            }
            Debug.Log($"[GPSFantasy] Buildings within 50m of home: {nearHome}");
            Debug.Log($"[GPSFantasy] Buildings in Reynard Drive area: {onReynard}");
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
            
            // Overpass API query for buildings and roads - increased timeout for larger radius
            string overpassQuery = $@"[out:json][timeout:60];(way[""building""]({south},{west},{north},{east});way[""highway""]({south},{west},{north},{east}););out body;>;out skel qt;";
            
            // Check for cached data first - include radius in cache key so radius changes invalidate cache
            string cacheKey = $"osm_{latitude:F4}_{longitude:F4}_{worldRadius:F0}";
            string cachedData = PlayerPrefs.GetString(cacheKey, "");
            
            if (!string.IsNullOrEmpty(cachedData) && cachedData.Contains("\"elements\""))
            {
                Debug.Log("[GPSFantasy] Using cached OSM data");
                ParseOverpassResponse(cachedData);
                if (buildings.Count > 0 || roads.Count > 0)
                {
                    Debug.Log($"[GPSFantasy] Loaded from cache: {buildings.Count} buildings, {roads.Count} roads");
                    yield break;
                }
            }
            
            // Try multiple Overpass API servers - Kumi first (most reliable)
            string[] servers = new string[] {
                "https://overpass.kumi.systems/api/interpreter",
                "https://overpass-api.de/api/interpreter",
                "https://maps.mail.ru/osm/tools/overpass/api/interpreter"
            };
            
            bool success = false;
            
            foreach (string server in servers)
            {
                Debug.Log($"[GPSFantasy] Trying OSM server: {server}");
                
                WWWForm form = new WWWForm();
                form.AddField("data", overpassQuery);
                
                using (UnityWebRequest request = UnityWebRequest.Post(server, form))
                {
                    request.timeout = 45; // Longer timeout
                    yield return request.SendWebRequest();
                    
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string response = request.downloadHandler.text;
                        if (response.Contains("\"elements\""))
                        {
                            ParseOverpassResponse(response);
                            Debug.Log($"[GPSFantasy] Fetched {buildings.Count} buildings, {roads.Count} roads from {server}");
                            
                            // Cache successful response for next time
                            if (buildings.Count > 0 || roads.Count > 0)
                            {
                                PlayerPrefs.SetString(cacheKey, response);
                                PlayerPrefs.Save();
                                Debug.Log("[GPSFantasy] Cached OSM data for future use");
                            }
                            
                            success = true;
                            break;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[GPSFantasy] Server {server} failed: {request.error}");
                    }
                }
                
                // Small delay before trying next server
                yield return new WaitForSeconds(0.5f);
            }
            
            if (!success)
            {
                Debug.LogWarning("[GPSFantasy] All OSM servers failed. Using procedural generation.");
                GenerateProceduralFallback();
            }
        }
        
        private void ParseOverpassResponse(string json)
        {
            try
            {
                Debug.Log($"[GPSFantasy] Parsing response ({json.Length} chars)...");
                
                // Normalize JSON - remove whitespace around colons for easier parsing
                json = System.Text.RegularExpressions.Regex.Replace(json, @"\s*:\s*", ":");
                
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
                
                // Find all nodes - search for "type":"node"
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
                                building.area = CalculatePolygonArea(building.footprint, validNodes);
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
                else if (buildings.Count == 0 && roads.Count > 0)
                {
                    // We have roads but no buildings - generate houses along the roads
                    Debug.Log("[GPSFantasy] Roads found but no buildings - generating houses along roads");
                    GenerateHousesAlongRoads();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GPSFantasy] Parse error: {e.Message}\n{e.StackTrace}");
                GenerateProceduralFallback();
            }
        }
        
        private void GenerateHousesAlongRoads()
        {
            int houseId = 1000;
            foreach (var road in roads)
            {
                if (road.roadType == "residential" || road.roadType == "tertiary" || road.roadType == "secondary")
                {
                    // Generate houses along both sides of residential roads
                    for (int i = 0; i < road.points.Length - 1; i++)
                    {
                        Vector3 start = road.points[i];
                        Vector3 end = road.points[i + 1];
                        Vector3 direction = (end - start).normalized;
                        Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x);
                        float segmentLength = Vector3.Distance(start, end);
                        
                        // Place houses every 30 meters along the road
                        int housesPerSegment = Mathf.Max(1, Mathf.FloorToInt(segmentLength / 30f));
                        for (int h = 0; h < housesPerSegment; h++)
                        {
                            float t = (h + 0.5f) / housesPerSegment;
                            Vector3 roadPoint = Vector3.Lerp(start, end, t);
                            
                            // House on left side
                            buildings.Add(new BuildingData
                            {
                                id = houseId++,
                                center = roadPoint + perpendicular * 20f + new Vector3(
                                    UnityEngine.Random.Range(-3f, 3f), 0, UnityEngine.Random.Range(-3f, 3f)),
                                buildingType = "house",
                                height = UnityEngine.Random.Range(6f, 10f)
                            });
                            
                            // House on right side
                            buildings.Add(new BuildingData
                            {
                                id = houseId++,
                                center = roadPoint - perpendicular * 20f + new Vector3(
                                    UnityEngine.Random.Range(-3f, 3f), 0, UnityEngine.Random.Range(-3f, 3f)),
                                buildingType = "house",
                                height = UnityEngine.Random.Range(6f, 10f)
                            });
                        }
                    }
                }
            }
            Debug.Log($"[GPSFantasy] Generated {buildings.Count} houses along {roads.Count} roads");
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
            // Generate a realistic neighborhood layout when OSM data unavailable
            Debug.Log("[GPSFantasy] Creating realistic neighborhood layout (OSM data unavailable)");
            
            // Create a main street through the center (like "Reynard Drive")
            roads.Add(new RoadData
            {
                id = 1,
                streetName = "Reynard Way",
                points = new[] { 
                    new Vector3(-worldRadius, 0, 0), 
                    new Vector3(-50, 0, 0),
                    new Vector3(0, 0, 0),  // YOUR HOUSE is at the origin
                    new Vector3(50, 0, 0),
                    new Vector3(worldRadius, 0, 0) 
                },
                width = 8f,
                roadType = "residential"
            });
            
            // Cross streets
            roads.Add(new RoadData
            {
                id = 2,
                streetName = "Oak Lane",
                points = new[] { new Vector3(-80, 0, -worldRadius), new Vector3(-80, 0, worldRadius) },
                width = 6f,
                roadType = "residential"
            });
            
            roads.Add(new RoadData
            {
                id = 3,
                streetName = "Elm Trail",
                points = new[] { new Vector3(80, 0, -worldRadius), new Vector3(80, 0, worldRadius) },
                width = 6f,
                roadType = "residential"
            });
            
            // Generate houses along the main street (YOUR house is at origin)
            float[] housePositionsX = { -120, -90, -60, -30, 30, 60, 90, 120 };
            float[] houseOffsetsZ = { 25, -25 }; // Both sides of street
            
            int houseId = 100;
            foreach (float x in housePositionsX)
            {
                foreach (float zOffset in houseOffsetsZ)
                {
                    buildings.Add(new BuildingData
                    {
                        id = houseId++,
                        center = new Vector3(x + UnityEngine.Random.Range(-5f, 5f), 0, zOffset + UnityEngine.Random.Range(-3f, 3f)),
                        buildingType = "house",
                        height = UnityEngine.Random.Range(6f, 10f)
                    });
                }
            }
            
            // Add YOUR house right at the center (origin)
            buildings.Add(new BuildingData
            {
                id = 504, // Your address number!
                center = new Vector3(0, 0, 20), // Slightly off the road
                buildingType = "house",
                height = 8f
            });
            
            // Houses on cross streets
            float[] crossStreetZ = { -60, -30, 30, 60 };
            foreach (float z in crossStreetZ)
            {
                // Houses on Oak Lane (x = -80)
                buildings.Add(new BuildingData
                {
                    id = houseId++,
                    center = new Vector3(-100, 0, z),
                    buildingType = "house",
                    height = UnityEngine.Random.Range(6f, 9f)
                });
                buildings.Add(new BuildingData
                {
                    id = houseId++,
                    center = new Vector3(-60, 0, z),
                    buildingType = "house",
                    height = UnityEngine.Random.Range(6f, 9f)
                });
                
                // Houses on Elm Trail (x = 80)
                buildings.Add(new BuildingData
                {
                    id = houseId++,
                    center = new Vector3(60, 0, z),
                    buildingType = "house",
                    height = UnityEngine.Random.Range(6f, 9f)
                });
                buildings.Add(new BuildingData
                {
                    id = houseId++,
                    center = new Vector3(100, 0, z),
                    buildingType = "house",
                    height = UnityEngine.Random.Range(6f, 9f)
                });
            }
            
            Debug.Log($"[GPSFantasy] Created neighborhood: {buildings.Count} houses, {roads.Count} streets");
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
        
        /// <summary>
        /// Calculate polygon area using Shoelace formula (in square meters)
        /// </summary>
        private float CalculatePolygonArea(Vector2[] polygon, int validPoints)
        {
            if (validPoints < 3) return 0;
            
            float area = 0;
            for (int i = 0; i < validPoints; i++)
            {
                int j = (i + 1) % validPoints;
                area += polygon[i].x * polygon[j].y;
                area -= polygon[j].x * polygon[i].y;
            }
            return Mathf.Abs(area / 2f);
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
            int nearHome = 0;
            int skipped = 0;
            
            foreach (var building in buildings)
            {
                // Select prefab based on building type and size
                GameObject prefab = SelectBuildingPrefab(building);
                if (prefab == null) 
                {
                    skipped++;
                    continue;
                }
                
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
                instance.name = $"Building_{building.id}_{building.area:F0}sqm";
                
                // Track buildings near home
                float distFromHome = Vector3.Distance(building.center, Vector3.zero);
                if (distFromHome < 100f) nearHome++;
                
                // Scale based on actual building footprint
                // Real suburban houses: ~150-250 sqm footprint, ~12-15m wide
                // We want buildings to fit their actual footprint, not overlap neighbors
                // Typical Synty house prefab is ~10m wide at scale 1.0
                
                // Calculate approximate building width from area (assume roughly square)
                float buildingWidth = Mathf.Sqrt(building.area);
                
                // Scale to match actual footprint (prefab is ~10m at scale 1.0)
                float scale = buildingWidth / 10f;
                
                // Clamp to reasonable range
                scale = Mathf.Clamp(scale, 0.8f, 2.5f);
                
                instance.transform.localScale = Vector3.one * scale;
                
                // Fix materials
                URPMaterialFixer.FixGameObject(instance);
                
                count++;
                if (count % 10 == 0) yield return null;
            }
            
            Debug.Log($"[GPSFantasy] Placed {count} fantasy buildings ({nearHome} within 100m of home, {skipped} skipped)");
            Debug.Log($"[GPSFantasy] Building distribution: check Hierarchy > Buildings to see all {count} buildings");
        }
        
        private GameObject SelectBuildingPrefab(BuildingData building)
        {
            // Suburban neighborhood - most houses are 150-300 sqm
            // Use townHouses and houses primarily - these look like proper medieval homes
            // AVOID cottages for main houses (too small/shabby looking)
            
            GameObject[] prefabs = null;
            
            if (building.buildingType == "house" || building.buildingType == "residential")
            {
                if (building.area > 400f)
                {
                    // Very large - use manors (big estates)
                    prefabs = prefabLibrary.manors;
                    if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.nobleEstates;
                    if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.townHouses;
                }
                else if (building.area > 200f)
                {
                    // Large suburban house - use townHouses (2-story, substantial)
                    prefabs = prefabLibrary.townHouses;
                    if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.houses;
                    if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.manors;
                }
                else if (building.area > 80f)
                {
                    // Standard house (most common) - prefer houses array
                    // Mix of houses and townHouses for variety
                    if (UnityEngine.Random.value < 0.7f)
                    {
                        prefabs = prefabLibrary.houses;
                        if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.townHouses;
                    }
                    else
                    {
                        prefabs = prefabLibrary.townHouses;
                        if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.houses;
                    }
                }
                else if (building.area > 30f)
                {
                    // Small structure - garage/shed - still use small houses, not cottages
                    prefabs = prefabLibrary.houses;
                    if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.townHouses;
                }
                else
                {
                    // Very small - skip these (likely sheds, garages, not real buildings)
                    return null;
                }
            }
            else if (building.buildingType == "commercial")
            {
                prefabs = prefabLibrary.shops;
                if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.taverns;
                if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.marketStalls;
            }
            else if (building.buildingType == "religious")
            {
                prefabs = prefabLibrary.churches;
                if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.chapels;
            }
            else if (building.buildingType == "industrial")
            {
                prefabs = prefabLibrary.warehouses;
                if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.barns;
                if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.workshops;
            }
            else
            {
                // Default - use proper houses not cottages
                if (building.area > 250f)
                    prefabs = prefabLibrary.townHouses;
                else
                    prefabs = prefabLibrary.houses;
            }
            
            // Fallback to any available buildings - prefer houses over cottages
            if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.houses;
            if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.townHouses;
            if (prefabs == null || prefabs.Length == 0) prefabs = prefabLibrary.cottages;
            
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
                
                // Create street sign near the CENTER of the road (not at edges)
                if (!string.IsNullOrEmpty(road.streetName) && !signedStreets.Contains(road.streetName))
                {
                    signedStreets.Add(road.streetName);
                    
                    if (road.points.Length >= 2)
                    {
                        // Find the point on the road closest to world center (0,0)
                        Vector3 bestPos = road.points[0];
                        float bestDist = float.MaxValue;
                        int bestIndex = 0;
                        
                        for (int i = 0; i < road.points.Length; i++)
                        {
                            float dist = new Vector2(road.points[i].x, road.points[i].z).magnitude;
                            if (dist < bestDist)
                            {
                                bestDist = dist;
                                bestPos = road.points[i];
                                bestIndex = i;
                            }
                        }
                        
                        // Skip individual street signs - we use intersection signs only
                        // This prevents duplicate/overlapping signs
                    }
                }
                
                yield return null;
            }
            
            // Find and create signs at intersections
            int signCount = CreateIntersectionSigns();
            Debug.Log($"[GPSFantasy] Created {signCount} intersection signs");
        }
        
        private int CreateIntersectionSigns()
        {
            // Find all intersections (where roads cross or meet)
            var intersections = new List<IntersectionData>();
            
            for (int i = 0; i < roads.Count; i++)
            {
                for (int j = i + 1; j < roads.Count; j++)
                {
                    var road1 = roads[i];
                    var road2 = roads[j];
                    
                    // Check each segment of road1 against each segment of road2
                    for (int p1 = 0; p1 < road1.points.Length - 1; p1++)
                    {
                        for (int p2 = 0; p2 < road2.points.Length - 1; p2++)
                        {
                            Vector3 intersection;
                            if (LineSegmentsIntersect(
                                road1.points[p1], road1.points[p1 + 1],
                                road2.points[p2], road2.points[p2 + 1],
                                out intersection))
                            {
                                intersections.Add(new IntersectionData
                                {
                                    position = intersection,
                                    street1 = road1.streetName,
                                    street2 = road2.streetName,
                                    direction1 = (road1.points[p1 + 1] - road1.points[p1]).normalized,
                                    direction2 = (road2.points[p2 + 1] - road2.points[p2]).normalized
                                });
                            }
                        }
                    }
                }
            }
            
            // Also check for road endpoints that are close together (T-intersections)
            for (int i = 0; i < roads.Count; i++)
            {
                for (int j = 0; j < roads.Count; j++)
                {
                    if (i == j) continue;
                    
                    var road1 = roads[i];
                    var road2 = roads[j];
                    
                    // Check if road1's start or end is near any point on road2
                    Vector3[] endpoints = { road1.points[0], road1.points[road1.points.Length - 1] };
                    
                    foreach (var endpoint in endpoints)
                    {
                        for (int p = 0; p < road2.points.Length - 1; p++)
                        {
                            float dist = DistanceToLineSegment(endpoint, road2.points[p], road2.points[p + 1]);
                            if (dist < 5f) // Within 5 meters
                            {
                                // Check if we already have an intersection nearby
                                bool exists = false;
                                foreach (var existing in intersections)
                                {
                                    if (Vector3.Distance(existing.position, endpoint) < 10f)
                                    {
                                        exists = true;
                                        break;
                                    }
                                }
                                
                                if (!exists)
                                {
                                    intersections.Add(new IntersectionData
                                    {
                                        position = endpoint,
                                        street1 = road1.streetName,
                                        street2 = road2.streetName,
                                        direction1 = (road1.points.Length > 1) ? 
                                            (road1.points[1] - road1.points[0]).normalized : Vector3.forward,
                                        direction2 = (road2.points[p + 1] - road2.points[p]).normalized
                                    });
                                }
                            }
                        }
                    }
                }
            }
            
            // Create signs at each intersection
            // De-duplicate intersections that are too close together
            var dedupedIntersections = new List<IntersectionData>();
            foreach (var intersection in intersections)
            {
                bool tooClose = false;
                foreach (var existing in dedupedIntersections)
                {
                    if (Vector3.Distance(intersection.position, existing.position) < 20f)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (!tooClose)
                {
                    dedupedIntersections.Add(intersection);
                }
            }
            
            foreach (var intersection in dedupedIntersections)
            {
                CreateIntersectionSign(intersection);
            }
            
            return dedupedIntersections.Count;
        }
        
        private struct IntersectionData
        {
            public Vector3 position;
            public string street1;
            public string street2;
            public Vector3 direction1;
            public Vector3 direction2;
        }
        
        private bool LineSegmentsIntersect(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, out Vector3 intersection)
        {
            intersection = Vector3.zero;
            
            // Project to 2D (ignore Y)
            float x1 = p1.x, y1 = p1.z;
            float x2 = p2.x, y2 = p2.z;
            float x3 = p3.x, y3 = p3.z;
            float x4 = p4.x, y4 = p4.z;
            
            float denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (Mathf.Abs(denom) < 0.0001f) return false; // Parallel
            
            float t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;
            float u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / denom;
            
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                float x = x1 + t * (x2 - x1);
                float z = y1 + t * (y2 - y1);
                intersection = new Vector3(x, 0, z);
                return true;
            }
            
            return false;
        }
        
        private float DistanceToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 line = lineEnd - lineStart;
            float len = line.magnitude;
            if (len < 0.001f) return Vector3.Distance(point, lineStart);
            
            line /= len;
            float t = Mathf.Clamp01(Vector3.Dot(point - lineStart, line) / len);
            Vector3 closest = lineStart + t * len * line;
            return Vector3.Distance(point, closest);
        }
        
        private void CreateIntersectionSign(IntersectionData intersection)
        {
            // Use REAL street names (not fantasy converted)
            string street1 = !string.IsNullOrEmpty(intersection.street1) ? intersection.street1 : "Main St";
            string street2 = !string.IsNullOrEmpty(intersection.street2) ? intersection.street2 : "Cross St";
            
            Debug.Log($"[GPSFantasy] Creating intersection sign: {street1} & {street2} at {intersection.position}");
            
            // Create sign post
            var signPost = new GameObject($"IntersectionSign_{street1}_{street2}");
            signPost.transform.SetParent(roadsParent);
            signPost.transform.position = intersection.position + new Vector3(3f, 0, 3f); // Corner offset
            
            // Create the pole
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(signPost.transform);
            pole.transform.localPosition = new Vector3(0, 4f, 0);
            pole.transform.localScale = new Vector3(0.2f, 4f, 0.2f);
            var poleMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            poleMat.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.3f)); // Metal gray
            pole.GetComponent<Renderer>().material = poleMat;
            
            // Create sign for street 1 (horizontal)
            CreateStreetNamePlate(signPost.transform, street1, new Vector3(0, 8f, 0), intersection.direction1);
            
            // Create sign for street 2 (perpendicular, slightly lower)
            CreateStreetNamePlate(signPost.transform, street2, new Vector3(0, 7.2f, 0), intersection.direction2);
        }
        
        private void CreateStreetNamePlate(Transform parent, string streetName, Vector3 localPos, Vector3 direction)
        {
            // Green background like real street signs - THICK so text doesn't show through
            var signBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signBoard.name = $"Sign_{streetName}";
            signBoard.transform.SetParent(parent);
            signBoard.transform.localPosition = localPos;
            signBoard.transform.localScale = new Vector3(5f, 1f, 0.5f); // THICK sign board (0.5 instead of 0.15)
            
            // Sign faces perpendicular to road so drivers can read it
            // We want text to be readable when approaching, so face OPPOSITE of road direction
            Vector3 faceDir = Vector3.Cross(direction, Vector3.up).normalized;
            if (faceDir.sqrMagnitude < 0.01f) faceDir = Vector3.forward;
            // The sign board's local -Z is forward (where text appears), so rotate 180 to flip text
            signBoard.transform.rotation = Quaternion.LookRotation(-faceDir);
            
            var boardMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            boardMat.SetColor("_BaseColor", new Color(0.0f, 0.4f, 0.2f)); // Green like real signs
            signBoard.GetComponent<Renderer>().material = boardMat;
            
            // White text on front of sign ONLY - no back text to avoid see-through
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(signBoard.transform);
            textObj.transform.localPosition = new Vector3(0, 0, -0.52f); // Just in front of thick sign
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = new Vector3(0.18f, 0.85f, 1f);
            
            var textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = streetName.ToUpper();
            textMesh.fontSize = 60;
            textMesh.characterSize = 0.1f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
            textMesh.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        
        // Keep the old method for backward compatibility but it won't be used for intersections
        private void CreateStreetSign(Vector3 position, Vector3 roadDirection, string streetName)
        {
            // Convert to fantasy-style street name
            string fantasyName = ConvertToFantasyStreetName(streetName);
            
            Debug.Log($"[GPSFantasy] Creating street sign: {fantasyName} at {position}");
            
            // Create sign post - make it BIG and visible!
            var signPost = new GameObject($"StreetSign_{streetName}");
            signPost.transform.SetParent(roadsParent);
            signPost.transform.position = position + Vector3.right * 5f; // Offset from road center
            
            // Create the pole - taller!
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(signPost.transform);
            pole.transform.localPosition = new Vector3(0, 3f, 0);
            pole.transform.localScale = new Vector3(0.3f, 3f, 0.3f);
            var poleMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            poleMat.SetColor("_BaseColor", new Color(0.4f, 0.25f, 0.1f)); // Wood brown
            pole.GetComponent<Renderer>().material = poleMat;
            
            // Create the sign board - MUCH bigger!
            var signBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signBoard.name = "SignBoard";
            signBoard.transform.SetParent(signPost.transform);
            signBoard.transform.localPosition = new Vector3(0, 6.5f, 0);
            signBoard.transform.localScale = new Vector3(6f, 1.5f, 0.2f);
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
            
            // Base road surface - gray cobblestone base
            var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "CobblestoneRoad";
            road.transform.SetParent(parent);
            road.transform.position = (start + end) / 2f + Vector3.up * 0.02f;
            road.transform.rotation = Quaternion.LookRotation(direction);
            road.transform.localScale = new Vector3(width, 0.1f, length);
            
            // Gray cobblestone base color
            var renderer = road.GetComponent<Renderer>();
            var roadMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            roadMat.SetColor("_BaseColor", new Color(0.45f, 0.42f, 0.38f)); // Gray stone base
            roadMat.SetFloat("_Smoothness", 0.05f); // Very rough surface
            renderer.material = roadMat;
            
            // Add cobblestone pattern overlay with individual stones
            AddCobblestonePattern(road.transform, width, length);
        }
        
        private void AddCobblestonePattern(Transform roadBase, float width, float length)
        {
            // Create raised stone pattern to simulate cobblestones
            float stoneSize = 0.8f;
            float gap = 0.15f;
            float stoneSpacing = stoneSize + gap;
            
            // Calculate number of stones that fit
            int stonesAcross = Mathf.FloorToInt(width / stoneSpacing);
            int stonesAlong = Mathf.FloorToInt(length / stoneSpacing);
            
            // Limit density for performance
            if (stonesAcross * stonesAlong > 200)
            {
                stoneSpacing *= 2f;
                stonesAcross = Mathf.FloorToInt(width / stoneSpacing);
                stonesAlong = Mathf.FloorToInt(length / stoneSpacing);
            }
            
            // Dark mortar lines (grooves between stones)
            var mortarMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mortarMat.SetColor("_BaseColor", new Color(0.25f, 0.22f, 0.18f)); // Dark mortar
            mortarMat.SetFloat("_Smoothness", 0.0f);
            
            // Stone material variations
            Color[] stoneColors = new Color[]
            {
                new Color(0.5f, 0.48f, 0.42f),  // Medium gray
                new Color(0.55f, 0.5f, 0.45f),  // Light gray
                new Color(0.4f, 0.38f, 0.35f),  // Dark gray  
                new Color(0.52f, 0.47f, 0.4f),  // Warm gray
            };
            
            // Create stone grid pattern
            for (int row = 0; row < stonesAlong; row++)
            {
                float zOffset = (row - stonesAlong / 2f + 0.5f) * stoneSpacing / length;
                bool offsetRow = row % 2 == 1; // Stagger alternate rows
                
                for (int col = 0; col < stonesAcross; col++)
                {
                    float xOffset = (col - stonesAcross / 2f + 0.5f) * stoneSpacing / width;
                    if (offsetRow) xOffset += (stoneSpacing * 0.5f) / width; // Stagger
                    
                    // Skip some stones randomly for natural look
                    if (UnityEngine.Random.value < 0.05f) continue;
                    
                    var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stone.name = "Stone";
                    stone.transform.SetParent(roadBase);
                    
                    // Random size variation
                    float sizeVar = UnityEngine.Random.Range(0.85f, 1.0f);
                    float stoneW = (stoneSize / width) * sizeVar;
                    float stoneL = (stoneSize / length) * sizeVar;
                    
                    stone.transform.localPosition = new Vector3(xOffset, 0.55f, zOffset);
                    stone.transform.localScale = new Vector3(stoneW, 0.08f, stoneL);
                    stone.transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(-3f, 3f), 0);
                    
                    // Random stone color
                    var stoneMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    stoneMat.SetColor("_BaseColor", stoneColors[UnityEngine.Random.Range(0, stoneColors.Length)]);
                    stoneMat.SetFloat("_Smoothness", UnityEngine.Random.Range(0.0f, 0.15f));
                    stone.GetComponent<Renderer>().material = stoneMat;
                }
            }
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
                
                // Check if position is far from buildings
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
                
                // Check if position is far from roads (avoid trees in streets!)
                foreach (var road in roads)
                {
                    foreach (var roadPoint in road.points)
                    {
                        float dist = Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(roadPoint.x, 0, roadPoint.z));
                        if (dist < 12f) // Road width buffer
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) break;
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
