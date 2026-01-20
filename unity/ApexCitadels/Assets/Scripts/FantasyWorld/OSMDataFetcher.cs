// ============================================================================
// APEX CITADELS - OPENSTREETMAP DATA FETCHER
// Fetches real-world building footprints, roads, and features from OSM
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using ApexCitadels.Core;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Types of real-world features we extract from OSM
    /// </summary>
    public enum OSMFeatureType
    {
        Building,
        Road,
        Path,
        Park,
        Water,
        Forest,
        Parking,
        Commercial,
        Residential,
        Industrial
    }
    
    /// <summary>
    /// A building footprint from OpenStreetMap
    /// </summary>
    [Serializable]
    public class OSMBuilding
    {
        public long Id;
        public List<Vector2> FootprintPoints; // Lat/Lon polygon
        public Vector2 Center;
        public float Area; // Square meters
        public float Width;
        public float Length;
        public int Levels; // Number of floors if known
        public string BuildingType; // residential, commercial, etc.
        public string Name;
        public Dictionary<string, string> Tags;
        
        // World-space converted points (set by generator)
        public List<Vector3> WorldPoints;
        
        public OSMBuilding()
        {
            FootprintPoints = new List<Vector2>();
            WorldPoints = new List<Vector3>();
            Tags = new Dictionary<string, string>();
            Levels = 1;
        }
        
        /// <summary>
        /// Calculate area and dimensions from footprint
        /// </summary>
        public void CalculateMetrics()
        {
            if (FootprintPoints.Count < 3) return;
            
            // Calculate center
            float sumX = 0, sumY = 0;
            foreach (var p in FootprintPoints)
            {
                sumX += p.x;
                sumY += p.y;
            }
            Center = new Vector2(sumX / FootprintPoints.Count, sumY / FootprintPoints.Count);
            
            // Calculate area using Shoelace formula (approximate in lat/lon)
            float area = 0;
            for (int i = 0; i < FootprintPoints.Count; i++)
            {
                int j = (i + 1) % FootprintPoints.Count;
                area += FootprintPoints[i].x * FootprintPoints[j].y;
                area -= FootprintPoints[j].x * FootprintPoints[i].y;
            }
            Area = Mathf.Abs(area) / 2f;
            
            // Convert to approximate square meters (at mid-latitudes)
            // 1 degree lat ≈ 111km, 1 degree lon ≈ 85km at 40°N
            Area *= 111000f * 85000f;
            
            // Calculate bounding box for width/length
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in FootprintPoints)
            {
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }
            Width = (maxX - minX) * 85000f; // meters
            Length = (maxY - minY) * 111000f; // meters
        }
        
        /// <summary>
        /// Calculate building area in square meters
        /// </summary>
        public float CalculateArea()
        {
            if (Area > 0) return Area;
            CalculateMetrics();
            return Area;
        }
        
        /// <summary>
        /// Get centroid as world position (uses pre-converted WorldPoints if available)
        /// </summary>
        public Vector3 CalculateCentroid()
        {
            if (WorldPoints != null && WorldPoints.Count > 0)
            {
                Vector3 sum = Vector3.zero;
                foreach (var p in WorldPoints)
                    sum += p;
                return sum / WorldPoints.Count;
            }
            
            // Fallback: use center lat/lon converted to approximate world units
            // This puts lon on X and lat on Z, centered at origin
            return new Vector3(Center.x * 85000f, 0, Center.y * 111000f);
        }
        
        /// <summary>
        /// Get building dimensions in world units (meters)
        /// </summary>
        public Vector3 CalculateDimensions()
        {
            if (Width <= 0 || Length <= 0) CalculateMetrics();
            float estimatedHeight = Levels * 3f; // 3 meters per floor
            return new Vector3(Width, estimatedHeight, Length);
        }
        
        /// <summary>
        /// Calculate building orientation (rotation around Y axis) from footprint
        /// </summary>
        public float CalculateOrientation()
        {
            if (FootprintPoints.Count < 2) return 0f;
            
            // Find the longest edge and align to it
            float maxLength = 0f;
            float angle = 0f;
            
            for (int i = 0; i < FootprintPoints.Count; i++)
            {
                int j = (i + 1) % FootprintPoints.Count;
                var p1 = FootprintPoints[i];
                var p2 = FootprintPoints[j];
                
                float dx = p2.x - p1.x;
                float dy = p2.y - p1.y;
                float length = Mathf.Sqrt(dx * dx + dy * dy);
                
                if (length > maxLength)
                {
                    maxLength = length;
                    angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
                }
            }
            
            return angle;
        }
    }

    
    /// <summary>
    /// A road or path from OpenStreetMap
    /// </summary>
    [Serializable]
    public class OSMRoad
    {
        public long Id;
        public List<Vector2> LatLonPoints; // Original Lat/Lon polyline
        public List<Vector3> Points; // World-space converted points
        public string RoadType; // highway type: residential, primary, footway, etc.
        public string Name;
        public float Width; // Estimated width in meters
        public bool IsFootpath;
        public bool IsMajorRoad;
        public Dictionary<string, string> Tags;
        
        public OSMRoad()
        {
            LatLonPoints = new List<Vector2>();
            Points = new List<Vector3>();
            Tags = new Dictionary<string, string>();
        }
        
        public void DetermineRoadClass()
        {
            IsFootpath = RoadType == "footway" || RoadType == "path" || RoadType == "cycleway";
            IsMajorRoad = RoadType == "primary" || RoadType == "secondary" || 
                          RoadType == "tertiary" || RoadType == "trunk";
            
            // Estimate width based on type
            Width = RoadType switch
            {
                "motorway" => 12f,
                "trunk" => 10f,
                "primary" => 8f,
                "secondary" => 7f,
                "tertiary" => 6f,
                "residential" => 5f,
                "service" => 4f,
                "footway" => 2f,
                "path" => 1.5f,
                "cycleway" => 2f,
                _ => 4f
            };
        }
        
        /// <summary>
        /// Convert lat/lon points to world space relative to origin
        /// </summary>
        public void ConvertToWorldSpace(double originLat, double originLon)
        {
            Points.Clear();
            double metersPerDegreeLat = 111320;
            double metersPerDegreeLon = 111320 * Math.Cos(originLat * Math.PI / 180);
            
            foreach (var p in LatLonPoints)
            {
                float x = (float)((p.x - originLon) * metersPerDegreeLon); // lon to X
                float z = (float)((p.y - originLat) * metersPerDegreeLat); // lat to Z
                Points.Add(new Vector3(x, 0, z));
            }
        }
    }
    
    /// <summary>
    /// A natural area (park, forest, water) from OpenStreetMap
    /// </summary>
    [Serializable]
    public class OSMArea
    {
        public long Id;
        public List<Vector2> Polygon;
        public OSMFeatureType FeatureType;
        public string AreaType; // "park", "water", "forest", etc.
        public string Name;
        public float Area;
        public Dictionary<string, string> Tags;
        
        // World-space converted points
        public List<Vector3> WorldPoints;
        
        public OSMArea()
        {
            Polygon = new List<Vector2>();
            WorldPoints = new List<Vector3>();
            Tags = new Dictionary<string, string>();
        }
        
        /// <summary>
        /// Calculate centroid in world coordinates
        /// </summary>
        public Vector3 CalculateCentroid()
        {
            if (WorldPoints != null && WorldPoints.Count > 0)
            {
                Vector3 sum = Vector3.zero;
                foreach (var p in WorldPoints)
                    sum += p;
                return sum / WorldPoints.Count;
            }
            
            if (Polygon.Count == 0) return Vector3.zero;
            
            // Fallback using lat/lon converted to approximate world units
            float sumX = 0, sumY = 0;
            foreach (var p in Polygon)
            {
                sumX += p.x;
                sumY += p.y;
            }
            float centerX = sumX / Polygon.Count;
            float centerY = sumY / Polygon.Count;
            
            return new Vector3(centerX * 85000f, 0, centerY * 111000f);
        }
        
        /// <summary>
        /// Estimate approximate radius of the area
        /// </summary>
        public float CalculateApproximateRadius()
        {
            var centroid = CalculateCentroid();
            float maxDist = 0;
            
            if (WorldPoints != null && WorldPoints.Count > 0)
            {
                foreach (var p in WorldPoints)
                {
                    float dist = Vector3.Distance(centroid, p);
                    maxDist = Mathf.Max(maxDist, dist);
                }
            }
            else if (Polygon.Count > 0)
            {
                // Estimate using bounding box
                float minX = float.MaxValue, maxX = float.MinValue;
                float minY = float.MaxValue, maxY = float.MinValue;
                foreach (var p in Polygon)
                {
                    minX = Mathf.Min(minX, p.x);
                    maxX = Mathf.Max(maxX, p.x);
                    minY = Mathf.Min(minY, p.y);
                    maxY = Mathf.Max(maxY, p.y);
                }
                float width = (maxX - minX) * 85000f;
                float height = (maxY - minY) * 111000f;
                maxDist = Mathf.Max(width, height) / 2f;
            }
            
            return Mathf.Max(maxDist, 10f); // Minimum 10 meters
        }
    }

    
    /// <summary>
    /// Complete OSM data for an area
    /// </summary>
    [Serializable]
    public class OSMAreaData
    {
        public Vector2 Center;
        public float RadiusMeters;
        public List<OSMBuilding> Buildings;
        public List<OSMRoad> Roads;
        public List<OSMArea> Parks;
        public List<OSMArea> Water;
        public List<OSMArea> Forests;
        public DateTime FetchTime;
        
        public OSMAreaData()
        {
            Buildings = new List<OSMBuilding>();
            Roads = new List<OSMRoad>();
            Parks = new List<OSMArea>();
            Water = new List<OSMArea>();
            Forests = new List<OSMArea>();
        }
    }
    
    /// <summary>
    /// Fetches real-world geographic data from OpenStreetMap Overpass API.
    /// This data drives the fantasy world generation.
    /// </summary>
    public class OSMDataFetcher : MonoBehaviour
    {
        #region Singleton
        
        private static OSMDataFetcher _instance;
        public static OSMDataFetcher Instance => _instance;
        
        #endregion
        
        #region Inspector Fields
        
        [Header("API Settings")]
        // Primary and backup Overpass API endpoints
        [SerializeField] private string overpassUrl = "https://overpass.kumi.systems/api/interpreter";
        [SerializeField] private string backupOverpassUrl = "https://overpass-api.de/api/interpreter";
        [SerializeField] private float requestTimeout = 90f;
        [SerializeField] private float minRequestInterval = 1f; // Rate limiting
        [SerializeField] private bool useMockDataOnFailure = true;
        
        [Header("Cache")]
        [SerializeField] private bool enableCache = true;
        [SerializeField] private float cacheExpiryHours = 24f;
        
        [Header("Debug")]
        [SerializeField] private bool logRequests = true;
        
        #endregion
        
        #region State
        
        private Dictionary<string, OSMAreaData> _cache = new Dictionary<string, OSMAreaData>();
        private float _lastRequestTime;
        private bool _isRequesting;
        
        #endregion
        
        #region Events
        
        public event Action<OSMAreaData> OnDataReceived;
        public event Action<string> OnFetchError;
        
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
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Fetch OSM data for an area around a point.
        /// </summary>
        /// <param name="latitude">Center latitude</param>
        /// <param name="longitude">Center longitude</param>
        /// <param name="radiusMeters">Radius in meters (max ~1000 for performance)</param>
        public void FetchArea(double latitude, double longitude, float radiusMeters, Action<OSMAreaData> callback = null)
        {
            string cacheKey = $"{latitude:F5},{longitude:F5},{radiusMeters:F0}";
            
            // Check cache
            if (enableCache && _cache.TryGetValue(cacheKey, out var cached))
            {
                if ((DateTime.Now - cached.FetchTime).TotalHours < cacheExpiryHours)
                {
                    if (logRequests)
                        ApexLogger.Log($"[OSM] Using cached data for {cacheKey}", ApexLogger.LogCategory.Map);
                    callback?.Invoke(cached);
                    OnDataReceived?.Invoke(cached);
                    return;
                }
            }
            
            StartCoroutine(FetchAreaCoroutine(latitude, longitude, radiusMeters, cacheKey, callback));
        }
        
        /// <summary>
        /// Fetch OSM data for a bounding box.
        /// </summary>
        public void FetchBoundingBox(double south, double west, double north, double east, Action<OSMAreaData> callback = null)
        {
            string cacheKey = $"{south:F5},{west:F5},{north:F5},{east:F5}";
            
            if (enableCache && _cache.TryGetValue(cacheKey, out var cached))
            {
                if ((DateTime.Now - cached.FetchTime).TotalHours < cacheExpiryHours)
                {
                    callback?.Invoke(cached);
                    OnDataReceived?.Invoke(cached);
                    return;
                }
            }
            
            StartCoroutine(FetchBBoxCoroutine(south, west, north, east, cacheKey, callback));
        }
        
        /// <summary>
        /// Clear the cache.
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }
        
        #endregion
        
        #region Fetch Coroutines
        
        private IEnumerator FetchAreaCoroutine(double lat, double lon, float radius, string cacheKey, Action<OSMAreaData> callback)
        {
            // Rate limiting
            while (_isRequesting || Time.time - _lastRequestTime < minRequestInterval)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            _isRequesting = true;
            _lastRequestTime = Time.time;
            
            // Convert radius to approximate degrees
            double latRadius = radius / 111000.0;
            double lonRadius = radius / (111000.0 * Math.Cos(lat * Math.PI / 180.0));
            
            double south = lat - latRadius;
            double north = lat + latRadius;
            double west = lon - lonRadius;
            double east = lon + lonRadius;
            
            yield return FetchBBoxCoroutine(south, west, north, east, cacheKey, callback);
            
            _isRequesting = false;
        }
        
        private IEnumerator FetchBBoxCoroutine(double south, double west, double north, double east, string cacheKey, Action<OSMAreaData> callback)
        {
            // Build Overpass query - simplified for speed
            string bbox = $"{south:F6},{west:F6},{north:F6},{east:F6}";
            
            // Simpler query - just buildings and roads for now (faster response)
            string query = $@"[out:json][timeout:60];
(
  way[""building""]({bbox});
  way[""highway""]({bbox});
  way[""leisure""=""park""]({bbox});
  way[""natural""=""wood""]({bbox});
);
out body;
>;
out skel qt;";

            if (logRequests)
                ApexLogger.Log($"[OSM] Fetching data for bbox {bbox}", ApexLogger.LogCategory.Map);
            
            // Make request - Overpass API expects data= prefix
            string postData = "data=" + UnityWebRequest.EscapeURL(query);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(postData);
            
            // Try primary URL first, then backup
            string[] urlsToTry = new string[] { overpassUrl, backupOverpassUrl };
            
            foreach (string apiUrl in urlsToTry)
            {
                using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                    request.timeout = (int)requestTimeout;
                    
                    if (logRequests)
                        ApexLogger.Log($"[OSM] Sending request to {apiUrl}", ApexLogger.LogCategory.Map);
                    
                    yield return request.SendWebRequest();
                    
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        // Parse response
                        string responseText = request.downloadHandler.text;
                        
                        if (logRequests)
                        {
                            ApexLogger.Log($"[OSM] Received {responseText.Length} bytes", ApexLogger.LogCategory.Map);
                            // Log first 500 chars to see what we got
                            string preview = responseText.Length > 500 ? responseText.Substring(0, 500) + "..." : responseText;
                            ApexLogger.Log($"[OSM] Response preview: {preview}", ApexLogger.LogCategory.Map);
                        }
                        
                        // Check if response is actually JSON (not HTML error page)
                        string trimmed = responseText.TrimStart();
                        if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
                        {
                            ApexLogger.LogWarning($"[OSM] Got HTML response (not JSON) from {apiUrl}, trying next...", ApexLogger.LogCategory.Map);
                            continue; // Try next URL
                        }
                        
                        OSMAreaData data = ParseOverpassResponse(responseText);
                        
                        // Check if we actually got useful data
                        if (data.Buildings.Count == 0 && data.Roads.Count == 0 && data.Parks.Count == 0)
                        {
                            ApexLogger.LogWarning($"[OSM] Empty data from {apiUrl}, trying next...", ApexLogger.LogCategory.Map);
                            continue; // Try next URL
                        }
                        
                        data.Center = new Vector2((float)((south + north) / 2), (float)((west + east) / 2));
                        data.FetchTime = DateTime.Now;
                        
                        // Cache
                        if (enableCache)
                        {
                            _cache[cacheKey] = data;
                        }
                        
                        if (logRequests)
                            ApexLogger.Log($"[OSM] Parsed {data.Buildings.Count} buildings, {data.Roads.Count} roads", ApexLogger.LogCategory.Map);
                        
                        callback?.Invoke(data);
                        OnDataReceived?.Invoke(data);
                        yield break; // Success!
                    }
                    else
                    {
                        ApexLogger.LogWarning($"[OSM] Failed with {apiUrl}: {request.error}, trying next...", ApexLogger.LogCategory.Map);
                    }
                }
            }
            
            // All URLs failed - use mock data if enabled
            if (useMockDataOnFailure)
            {
                ApexLogger.LogWarning("[OSM] All API endpoints failed, using mock data for testing", ApexLogger.LogCategory.Map);
                OSMAreaData mockData = GenerateMockData(south, west, north, east);
                callback?.Invoke(mockData);
                OnDataReceived?.Invoke(mockData);
            }
            else
            {
                string error = "OSM fetch failed: All API endpoints timed out";
                ApexLogger.LogError(error, ApexLogger.LogCategory.Map);
                OnFetchError?.Invoke(error);
                callback?.Invoke(null);
            }
        }
        
        /// <summary>
        /// Generate mock OSM data for testing when API fails
        /// </summary>
        private OSMAreaData GenerateMockData(double south, double west, double north, double east)
        {
            var data = new OSMAreaData();
            data.Center = new Vector2((float)((west + east) / 2), (float)((south + north) / 2));
            data.FetchTime = DateTime.Now;
            
            double centerLat = (south + north) / 2;
            double centerLon = (west + east) / 2;
            
            // Generate some fake buildings in a grid pattern
            int gridSize = 5;
            double latStep = (north - south) / gridSize;
            double lonStep = (east - west) / gridSize;
            
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    // Skip some cells randomly for variety
                    if (UnityEngine.Random.value < 0.3f) continue;
                    
                    double lat = south + latStep * (i + 0.5);
                    double lon = west + lonStep * (j + 0.5);
                    
                    // Random building size
                    float size = UnityEngine.Random.Range(0.0001f, 0.0003f);
                    
                    var building = new OSMBuilding
                    {
                        Id = i * 100 + j,
                        BuildingType = UnityEngine.Random.value < 0.7f ? "house" : "commercial",
                        Levels = UnityEngine.Random.Range(1, 3),
                        FootprintPoints = new System.Collections.Generic.List<Vector2>
                        {
                            new Vector2((float)lon - size, (float)lat - size),
                            new Vector2((float)lon + size, (float)lat - size),
                            new Vector2((float)lon + size, (float)lat + size),
                            new Vector2((float)lon - size, (float)lat + size)
                        }
                    };
                    building.CalculateMetrics();
                    data.Buildings.Add(building);
                }
            }
            
            // Generate a main road
            var mainRoad = new OSMRoad
            {
                Id = 1,
                RoadType = "residential",
                Name = "Fantasy Lane",
                LatLonPoints = new System.Collections.Generic.List<Vector2>
                {
                    new Vector2((float)west, (float)centerLat),
                    new Vector2((float)east, (float)centerLat)
                }
            };
            mainRoad.DetermineRoadClass();
            data.Roads.Add(mainRoad);
            
            // Add a cross street
            var crossRoad = new OSMRoad
            {
                Id = 2,
                RoadType = "residential",
                Name = "Dragon Way",
                LatLonPoints = new System.Collections.Generic.List<Vector2>
                {
                    new Vector2((float)centerLon, (float)south),
                    new Vector2((float)centerLon, (float)north)
                }
            };
            crossRoad.DetermineRoadClass();
            data.Roads.Add(crossRoad);
            
            // Add a park area
            var park = new OSMArea
            {
                Id = 1,
                AreaType = "park",
                Name = "Citadel Green",
                FeatureType = OSMFeatureType.Park,
                Polygon = new System.Collections.Generic.List<Vector2>
                {
                    new Vector2((float)(centerLon - 0.0005), (float)(centerLat - 0.0005)),
                    new Vector2((float)(centerLon + 0.0005), (float)(centerLat - 0.0005)),
                    new Vector2((float)(centerLon + 0.0005), (float)(centerLat + 0.0005)),
                    new Vector2((float)(centerLon - 0.0005), (float)(centerLat + 0.0005))
                }
            };
            data.Parks.Add(park);
            
            ApexLogger.Log($"[OSM] Generated mock data: {data.Buildings.Count} buildings, {data.Roads.Count} roads, {data.Parks.Count} parks", ApexLogger.LogCategory.Map);
            
            return data;
        }
        
        #endregion
        
        #region JSON Parsing
        
        private OSMAreaData ParseOverpassResponse(string json)
        {
            OSMAreaData data = new OSMAreaData();
            
            try
            {
                // Simple JSON parsing (Unity's JsonUtility doesn't handle dynamic structures well)
                // We'll parse the Overpass JSON format manually
                
                var parsed = ParseJson(json);
                if (parsed == null || !parsed.ContainsKey("elements"))
                    return data;
                
                var elements = parsed["elements"] as List<object>;
                if (elements == null)
                    return data;
                
                // First pass: collect all nodes for reference
                Dictionary<long, Vector2> nodes = new Dictionary<long, Vector2>();
                foreach (var elem in elements)
                {
                    var dict = elem as Dictionary<string, object>;
                    if (dict == null) continue;
                    
                    string type = dict.ContainsKey("type") ? dict["type"].ToString() : "";
                    if (type == "node")
                    {
                        long id = Convert.ToInt64(dict["id"]);
                        float lat = Convert.ToSingle(dict["lat"]);
                        float lon = Convert.ToSingle(dict["lon"]);
                        nodes[id] = new Vector2(lon, lat); // Note: x=lon, y=lat
                    }
                }
                
                // Second pass: process ways
                foreach (var elem in elements)
                {
                    var dict = elem as Dictionary<string, object>;
                    if (dict == null) continue;
                    
                    string type = dict.ContainsKey("type") ? dict["type"].ToString() : "";
                    if (type != "way") continue;
                    
                    var tags = dict.ContainsKey("tags") ? dict["tags"] as Dictionary<string, object> : null;
                    if (tags == null) continue;
                    
                    var nodeIds = dict.ContainsKey("nodes") ? dict["nodes"] as List<object> : null;
                    if (nodeIds == null) continue;
                    
                    long id = Convert.ToInt64(dict["id"]);
                    
                    // Get points
                    List<Vector2> points = new List<Vector2>();
                    foreach (var nodeId in nodeIds)
                    {
                        long nid = Convert.ToInt64(nodeId);
                        if (nodes.TryGetValue(nid, out Vector2 pos))
                        {
                            points.Add(pos);
                        }
                    }
                    
                    if (points.Count < 2) continue;
                    
                    // Classify element
                    if (tags.ContainsKey("building"))
                    {
                        var building = new OSMBuilding
                        {
                            Id = id,
                            FootprintPoints = points,
                            BuildingType = tags["building"].ToString()
                        };
                        
                        if (tags.ContainsKey("name"))
                            building.Name = tags["name"].ToString();
                        if (tags.ContainsKey("building:levels"))
                            int.TryParse(tags["building:levels"].ToString(), out building.Levels);
                        
                        foreach (var tag in tags)
                            building.Tags[tag.Key] = tag.Value.ToString();
                        
                        building.CalculateMetrics();
                        data.Buildings.Add(building);
                    }
                    else if (tags.ContainsKey("highway"))
                    {
                        var road = new OSMRoad
                        {
                            Id = id,
                            LatLonPoints = points,
                            RoadType = tags["highway"].ToString()
                        };
                        
                        if (tags.ContainsKey("name"))
                            road.Name = tags["name"].ToString();
                        
                        foreach (var tag in tags)
                            road.Tags[tag.Key] = tag.Value.ToString();
                        
                        road.DetermineRoadClass();
                        data.Roads.Add(road);
                    }
                    else if (tags.ContainsKey("leisure") && tags["leisure"].ToString() == "park")
                    {
                        var park = new OSMArea
                        {
                            Id = id,
                            Polygon = points,
                            FeatureType = OSMFeatureType.Park,
                            AreaType = "park",
                            Name = tags.ContainsKey("name") ? tags["name"].ToString() : ""
                        };
                        data.Parks.Add(park);
                    }
                    else if (tags.ContainsKey("natural") && tags["natural"].ToString() == "water")
                    {
                        var water = new OSMArea
                        {
                            Id = id,
                            Polygon = points,
                            FeatureType = OSMFeatureType.Water,
                            AreaType = "water",
                            Name = tags.ContainsKey("name") ? tags["name"].ToString() : ""
                        };
                        data.Water.Add(water);
                    }
                    else if (tags.ContainsKey("landuse") && 
                             (tags["landuse"].ToString() == "forest" || tags["landuse"].ToString() == "wood"))
                    {
                        var forest = new OSMArea
                        {
                            Id = id,
                            Polygon = points,
                            FeatureType = OSMFeatureType.Forest,
                            AreaType = "forest",
                            Name = tags.ContainsKey("name") ? tags["name"].ToString() : ""
                        };
                        data.Forests.Add(forest);
                    }
                    else if (tags.ContainsKey("landuse") && tags["landuse"].ToString() == "grass")
                    {
                        var grass = new OSMArea
                        {
                            Id = id,
                            Polygon = points,
                            FeatureType = OSMFeatureType.Park,
                            AreaType = "grass",
                            Name = tags.ContainsKey("name") ? tags["name"].ToString() : ""
                        };
                        data.Parks.Add(grass);
                    }
                }
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"[OSM] Parse error: {ex.Message}", ApexLogger.LogCategory.Map);
            }
            
            return data;
        }
        
        /// <summary>
        /// Simple JSON parser for Overpass response.
        /// </summary>
        private Dictionary<string, object> ParseJson(string json)
        {
            // Use Unity's built-in JSON parsing with a wrapper
            try
            {
                return MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
            }
            catch
            {
                return null;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Minimal JSON parser for Overpass API responses.
    /// Based on MiniJSON (public domain).
    /// </summary>
    public static class MiniJSON
    {
        public static class Json
        {
            public static object Deserialize(string json)
            {
                if (json == null) return null;
                return Parser.Parse(json);
            }
            
            sealed class Parser : IDisposable
            {
                const string WORD_BREAK = "{}[],:\"";
                StringReader json;
                
                Parser(string jsonString)
                {
                    json = new StringReader(jsonString);
                }
                
                public static object Parse(string jsonString)
                {
                    using (var instance = new Parser(jsonString))
                    {
                        return instance.ParseValue();
                    }
                }
                
                public void Dispose()
                {
                    json.Dispose();
                    json = null;
                }
                
                Dictionary<string, object> ParseObject()
                {
                    var table = new Dictionary<string, object>();
                    json.Read(); // {
                    
                    while (true)
                    {
                        switch (NextToken)
                        {
                            case TOKEN.NONE: return null;
                            case TOKEN.CURLY_CLOSE: return table;
                            case TOKEN.COMMA: continue;
                            default:
                                string name = ParseString();
                                if (name == null) return null;
                                if (NextToken != TOKEN.COLON) return null;
                                json.Read();
                                table[name] = ParseValue();
                                break;
                        }
                    }
                }
                
                List<object> ParseArray()
                {
                    var array = new List<object>();
                    json.Read(); // [
                    
                    while (true)
                    {
                        switch (NextToken)
                        {
                            case TOKEN.NONE: return null;
                            case TOKEN.SQUARED_CLOSE: return array;
                            case TOKEN.COMMA: continue;
                            default:
                                array.Add(ParseByToken(PeekToken));
                                break;
                        }
                    }
                }
                
                object ParseValue()
                {
                    return ParseByToken(NextToken);
                }
                
                object ParseByToken(TOKEN token)
                {
                    switch (token)
                    {
                        case TOKEN.STRING: return ParseString();
                        case TOKEN.NUMBER: return ParseNumber();
                        case TOKEN.CURLY_OPEN: return ParseObject();
                        case TOKEN.SQUARED_OPEN: return ParseArray();
                        case TOKEN.TRUE: return true;
                        case TOKEN.FALSE: return false;
                        case TOKEN.NULL: return null;
                        default: return null;
                    }
                }
                
                string ParseString()
                {
                    var s = new System.Text.StringBuilder();
                    json.Read(); // "
                    
                    while (true)
                    {
                        if (json.Peek() == -1) return null;
                        char c = NextChar;
                        if (c == '"') return s.ToString();
                        if (c == '\\')
                        {
                            if (json.Peek() == -1) return null;
                            c = NextChar;
                            switch (c)
                            {
                                case '"': case '\\': case '/': s.Append(c); break;
                                case 'b': s.Append('\b'); break;
                                case 'f': s.Append('\f'); break;
                                case 'n': s.Append('\n'); break;
                                case 'r': s.Append('\r'); break;
                                case 't': s.Append('\t'); break;
                                case 'u':
                                    var hex = new char[4];
                                    for (int i = 0; i < 4; i++) hex[i] = NextChar;
                                    s.Append((char)Convert.ToInt32(new string(hex), 16));
                                    break;
                            }
                        }
                        else
                        {
                            s.Append(c);
                        }
                    }
                }
                
                object ParseNumber()
                {
                    string number = NextWord;
                    if (number.Contains("."))
                    {
                        double.TryParse(number, System.Globalization.NumberStyles.Any, 
                            System.Globalization.CultureInfo.InvariantCulture, out double result);
                        return result;
                    }
                    else
                    {
                        long.TryParse(number, out long result);
                        return result;
                    }
                }
                
                void EatWhitespace()
                {
                    while (char.IsWhiteSpace(PeekChar)) json.Read();
                }
                
                char PeekChar => (char)json.Peek();
                char NextChar => (char)json.Read();
                
                string NextWord
                {
                    get
                    {
                        var word = new System.Text.StringBuilder();
                        while (!IsWordBreak(PeekChar))
                        {
                            word.Append(NextChar);
                            if (json.Peek() == -1) break;
                        }
                        return word.ToString();
                    }
                }
                
                TOKEN NextToken
                {
                    get
                    {
                        EatWhitespace();
                        if (json.Peek() == -1) return TOKEN.NONE;
                        switch (PeekChar)
                        {
                            case '{': return TOKEN.CURLY_OPEN;
                            case '}': json.Read(); return TOKEN.CURLY_CLOSE;
                            case '[': return TOKEN.SQUARED_OPEN;
                            case ']': json.Read(); return TOKEN.SQUARED_CLOSE;
                            case ',': json.Read(); return TOKEN.COMMA;
                            case '"': return TOKEN.STRING;
                            case ':': return TOKEN.COLON;
                            case '-': case '0': case '1': case '2': case '3': case '4':
                            case '5': case '6': case '7': case '8': case '9':
                                return TOKEN.NUMBER;
                        }
                        string word = NextWord;
                        switch (word)
                        {
                            case "false": return TOKEN.FALSE;
                            case "true": return TOKEN.TRUE;
                            case "null": return TOKEN.NULL;
                        }
                        return TOKEN.NONE;
                    }
                }
                
                TOKEN PeekToken
                {
                    get
                    {
                        EatWhitespace();
                        if (json.Peek() == -1) return TOKEN.NONE;
                        switch (PeekChar)
                        {
                            case '{': return TOKEN.CURLY_OPEN;
                            case '}': return TOKEN.CURLY_CLOSE;
                            case '[': return TOKEN.SQUARED_OPEN;
                            case ']': return TOKEN.SQUARED_CLOSE;
                            case ',': return TOKEN.COMMA;
                            case '"': return TOKEN.STRING;
                            case ':': return TOKEN.COLON;
                            case '-': case '0': case '1': case '2': case '3': case '4':
                            case '5': case '6': case '7': case '8': case '9':
                                return TOKEN.NUMBER;
                        }
                        return TOKEN.NONE;
                    }
                }
                
                bool IsWordBreak(char c) => char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;
                
                enum TOKEN { NONE, CURLY_OPEN, CURLY_CLOSE, SQUARED_OPEN, SQUARED_CLOSE,
                             COLON, COMMA, STRING, NUMBER, TRUE, FALSE, NULL }
            }
            
            sealed class StringReader : IDisposable
            {
                string str;
                int pos;
                
                public StringReader(string s) { str = s; pos = 0; }
                public int Peek() => pos < str.Length ? str[pos] : -1;
                public int Read() => pos < str.Length ? str[pos++] : -1;
                public void Dispose() { }
            }
        }
    }
}
