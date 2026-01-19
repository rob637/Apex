using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// OpenStreetMap Data Pipeline for fetching and parsing real-world geographic data.
    /// Converts OSM data to game-usable structures for fantasy rendering.
    /// Features:
    /// - Overpass API integration
    /// - Building footprint extraction
    /// - Road network parsing
    /// - Park/greenspace detection
    /// - Water feature extraction
    /// - POI (Points of Interest) detection
    /// - Local caching for performance
    /// </summary>
    public class OSMDataPipeline : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string overpassApiUrl = "https://overpass-api.de/api/interpreter";
        [SerializeField] private float requestTimeout = 30f;
        [SerializeField] private float minTimeBetweenRequests = 1f;
        
        [Header("Query Settings")]
        [SerializeField] private float defaultQueryRadius = 500f; // meters
        [SerializeField] private int maxBuildingsPerQuery = 500;
        [SerializeField] private int maxRoadsPerQuery = 200;
        
        [Header("Caching")]
        [SerializeField] private bool enableCaching = true;
        [SerializeField] private float cacheExpirationHours = 24f;
        
        [Header("Debug")]
        [SerializeField] private bool debugLogging = true;
        
        // Singleton
        private static OSMDataPipeline _instance;
        public static OSMDataPipeline Instance => _instance;
        
        // State
        private Dictionary<string, OSMAreaData> _cache = new Dictionary<string, OSMAreaData>();
        private float _lastRequestTime;
        private Queue<OSMRequest> _requestQueue = new Queue<OSMRequest>();
        private bool _isProcessingQueue;
        
        // Events
        public event Action<OSMAreaData> OnDataLoaded;
        public event Action<string> OnDataLoadError;
        public event Action<float> OnLoadProgress;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        #region Public API
        
        /// <summary>
        /// Fetch OSM data for an area centered on GPS coordinates
        /// </summary>
        public void FetchAreaData(double latitude, double longitude, float radiusMeters = 0, Action<OSMAreaData> onComplete = null, Action<string> onError = null)
        {
            if (radiusMeters <= 0) radiusMeters = defaultQueryRadius;
            
            string cacheKey = GetCacheKey(latitude, longitude, radiusMeters);
            
            // Check cache
            if (enableCaching && _cache.TryGetValue(cacheKey, out var cached))
            {
                if ((DateTime.Now - cached.timestamp).TotalHours < cacheExpirationHours)
                {
                    if (debugLogging) Debug.Log($"OSM: Returning cached data for {cacheKey}");
                    onComplete?.Invoke(cached);
                    OnDataLoaded?.Invoke(cached);
                    return;
                }
            }
            
            // Queue request
            var request = new OSMRequest
            {
                latitude = latitude,
                longitude = longitude,
                radiusMeters = radiusMeters,
                cacheKey = cacheKey,
                onComplete = onComplete,
                onError = onError
            };
            
            _requestQueue.Enqueue(request);
            
            if (!_isProcessingQueue)
            {
                StartCoroutine(ProcessRequestQueue());
            }
        }
        
        /// <summary>
        /// Fetch data for a bounding box
        /// </summary>
        public void FetchBoundingBox(double south, double west, double north, double east, Action<OSMAreaData> onComplete = null, Action<string> onError = null)
        {
            string cacheKey = $"bbox_{south:F4}_{west:F4}_{north:F4}_{east:F4}";
            
            // Check cache
            if (enableCaching && _cache.TryGetValue(cacheKey, out var cached))
            {
                if ((DateTime.Now - cached.timestamp).TotalHours < cacheExpirationHours)
                {
                    onComplete?.Invoke(cached);
                    OnDataLoaded?.Invoke(cached);
                    return;
                }
            }
            
            StartCoroutine(FetchBoundingBoxCoroutine(south, west, north, east, cacheKey, onComplete, onError));
        }
        
        /// <summary>
        /// Clear all cached data
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            if (debugLogging) Debug.Log("OSM: Cache cleared");
        }
        
        /// <summary>
        /// Get cached data if available
        /// </summary>
        public OSMAreaData GetCachedData(double latitude, double longitude, float radiusMeters)
        {
            string cacheKey = GetCacheKey(latitude, longitude, radiusMeters);
            return _cache.TryGetValue(cacheKey, out var data) ? data : null;
        }
        
        #endregion
        
        #region Query Processing
        
        private IEnumerator ProcessRequestQueue()
        {
            _isProcessingQueue = true;
            
            while (_requestQueue.Count > 0)
            {
                // Rate limiting
                float timeSinceLastRequest = Time.time - _lastRequestTime;
                if (timeSinceLastRequest < minTimeBetweenRequests)
                {
                    yield return new WaitForSeconds(minTimeBetweenRequests - timeSinceLastRequest);
                }
                
                var request = _requestQueue.Dequeue();
                yield return StartCoroutine(ExecuteRequest(request));
            }
            
            _isProcessingQueue = false;
        }
        
        private IEnumerator ExecuteRequest(OSMRequest request)
        {
            _lastRequestTime = Time.time;
            
            // Calculate bounding box from center + radius
            double latOffset = request.radiusMeters / 111000.0; // ~111km per degree latitude
            double lonOffset = request.radiusMeters / (111000.0 * Math.Cos(request.latitude * Math.PI / 180.0));
            
            double south = request.latitude - latOffset;
            double north = request.latitude + latOffset;
            double west = request.longitude - lonOffset;
            double east = request.longitude + lonOffset;
            
            yield return StartCoroutine(FetchBoundingBoxCoroutine(
                south, west, north, east,
                request.cacheKey,
                request.onComplete,
                request.onError
            ));
        }
        
        private IEnumerator FetchBoundingBoxCoroutine(double south, double west, double north, double east, string cacheKey, Action<OSMAreaData> onComplete, Action<string> onError)
        {
            if (debugLogging) Debug.Log($"OSM: Fetching bbox ({south:F4},{west:F4}) to ({north:F4},{east:F4})");
            
            OnLoadProgress?.Invoke(0.1f);
            
            var areaData = new OSMAreaData
            {
                centerLatitude = (south + north) / 2,
                centerLongitude = (west + east) / 2,
                boundsSouth = south,
                boundsWest = west,
                boundsNorth = north,
                boundsEast = east,
                timestamp = DateTime.Now
            };
            
            // Fetch buildings
            string buildingQuery = BuildBuildingQuery(south, west, north, east);
            yield return StartCoroutine(ExecuteOverpassQuery(buildingQuery, json =>
            {
                ParseBuildingsFromJson(json, areaData);
                OnLoadProgress?.Invoke(0.4f);
            }, onError));
            
            // Fetch roads
            string roadQuery = BuildRoadQuery(south, west, north, east);
            yield return StartCoroutine(ExecuteOverpassQuery(roadQuery, json =>
            {
                ParseRoadsFromJson(json, areaData);
                OnLoadProgress?.Invoke(0.6f);
            }, onError));
            
            // Fetch natural features (parks, water)
            string naturalQuery = BuildNaturalQuery(south, west, north, east);
            yield return StartCoroutine(ExecuteOverpassQuery(naturalQuery, json =>
            {
                ParseNaturalFromJson(json, areaData);
                OnLoadProgress?.Invoke(0.8f);
            }, onError));
            
            // Fetch POIs
            string poiQuery = BuildPOIQuery(south, west, north, east);
            yield return StartCoroutine(ExecuteOverpassQuery(poiQuery, json =>
            {
                ParsePOIsFromJson(json, areaData);
                OnLoadProgress?.Invoke(1.0f);
            }, onError));
            
            // Cache result
            if (enableCaching && !string.IsNullOrEmpty(cacheKey))
            {
                _cache[cacheKey] = areaData;
            }
            
            if (debugLogging)
            {
                Debug.Log($"OSM: Loaded {areaData.buildings.Count} buildings, {areaData.roads.Count} roads, {areaData.naturalAreas.Count} natural areas, {areaData.pointsOfInterest.Count} POIs");
            }
            
            onComplete?.Invoke(areaData);
            OnDataLoaded?.Invoke(areaData);
        }
        
        private IEnumerator ExecuteOverpassQuery(string query, Action<string> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest request = UnityWebRequest.Post(overpassApiUrl, query, "application/x-www-form-urlencoded"))
            {
                request.timeout = (int)requestTimeout;
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke(request.downloadHandler.text);
                }
                else
                {
                    string error = $"OSM API Error: {request.error}";
                    if (debugLogging) Debug.LogWarning(error);
                    onError?.Invoke(error);
                    OnDataLoadError?.Invoke(error);
                }
            }
        }
        
        #endregion
        
        #region Query Builders
        
        private string BuildBuildingQuery(double south, double west, double north, double east)
        {
            return $"data=[out:json][timeout:25];" +
                   $"way[\"building\"]({south:F6},{west:F6},{north:F6},{east:F6});" +
                   $"out body geom {maxBuildingsPerQuery};";
        }
        
        private string BuildRoadQuery(double south, double west, double north, double east)
        {
            return $"data=[out:json][timeout:25];" +
                   $"way[\"highway\"]({south:F6},{west:F6},{north:F6},{east:F6});" +
                   $"out body geom {maxRoadsPerQuery};";
        }
        
        private string BuildNaturalQuery(double south, double west, double north, double east)
        {
            return $"data=[out:json][timeout:25];(" +
                   $"way[\"leisure\"=\"park\"]({south:F6},{west:F6},{north:F6},{east:F6});" +
                   $"way[\"natural\"=\"water\"]({south:F6},{west:F6},{north:F6},{east:F6});" +
                   $"way[\"landuse\"=\"forest\"]({south:F6},{west:F6},{north:F6},{east:F6});" +
                   $"way[\"landuse\"=\"grass\"]({south:F6},{west:F6},{north:F6},{east:F6});" +
                   $");out body geom 100;";
        }
        
        private string BuildPOIQuery(double south, double west, double north, double east)
        {
            return $"data=[out:json][timeout:25];(" +
                   $"node[\"amenity\"]({south:F6},{west:F6},{north:F6},{east:F6});" +
                   $"node[\"shop\"]({south:F6},{west:F6},{north:F6},{east:F6});" +
                   $"node[\"tourism\"]({south:F6},{west:F6},{north:F6},{east:F6});" +
                   $");out body 200;";
        }
        
        #endregion
        
        #region JSON Parsing
        
        private void ParseBuildingsFromJson(string json, OSMAreaData areaData)
        {
            try
            {
                var response = JsonUtility.FromJson<OSMOverpassResponse>(json);
                if (response?.elements == null) return;
                
                foreach (var element in response.elements)
                {
                    if (element.type != "way" || element.geometry == null) continue;
                    
                    var building = new OSMBuilding
                    {
                        id = element.id,
                        footprint = new List<Vector2d>()
                    };
                    
                    // Parse geometry
                    foreach (var point in element.geometry)
                    {
                        building.footprint.Add(new Vector2d(point.lat, point.lon));
                    }
                    
                    // Parse tags
                    if (element.tags != null)
                    {
                        building.buildingType = GetTagValue(element.tags, "building");
                        building.name = GetTagValue(element.tags, "name");
                        building.height = ParseHeight(GetTagValue(element.tags, "height"));
                        building.levels = ParseInt(GetTagValue(element.tags, "building:levels"), 1);
                        building.amenity = GetTagValue(element.tags, "amenity");
                        building.shop = GetTagValue(element.tags, "shop");
                    }
                    
                    // Calculate properties
                    building.area = CalculatePolygonArea(building.footprint);
                    building.center = CalculatePolygonCenter(building.footprint);
                    building.fantasyType = ClassifyBuildingFantasyType(building);
                    
                    areaData.buildings.Add(building);
                }
            }
            catch (Exception ex)
            {
                if (debugLogging) Debug.LogWarning($"OSM: Failed to parse buildings: {ex.Message}");
            }
        }
        
        private void ParseRoadsFromJson(string json, OSMAreaData areaData)
        {
            try
            {
                var response = JsonUtility.FromJson<OSMOverpassResponse>(json);
                if (response?.elements == null) return;
                
                foreach (var element in response.elements)
                {
                    if (element.type != "way" || element.geometry == null) continue;
                    
                    var road = new OSMRoad
                    {
                        id = element.id,
                        points = new List<Vector2d>()
                    };
                    
                    // Parse geometry
                    foreach (var point in element.geometry)
                    {
                        road.points.Add(new Vector2d(point.lat, point.lon));
                    }
                    
                    // Parse tags
                    if (element.tags != null)
                    {
                        road.highway = GetTagValue(element.tags, "highway");
                        road.name = GetTagValue(element.tags, "name");
                        road.lanes = ParseInt(GetTagValue(element.tags, "lanes"), 1);
                        road.surface = GetTagValue(element.tags, "surface");
                        road.oneway = GetTagValue(element.tags, "oneway") == "yes";
                    }
                    
                    // Classify road
                    road.roadType = ClassifyRoadType(road.highway);
                    road.width = GetRoadWidth(road.roadType, road.lanes);
                    road.fantasyStyle = ClassifyRoadFantasyStyle(road);
                    
                    areaData.roads.Add(road);
                }
            }
            catch (Exception ex)
            {
                if (debugLogging) Debug.LogWarning($"OSM: Failed to parse roads: {ex.Message}");
            }
        }
        
        private void ParseNaturalFromJson(string json, OSMAreaData areaData)
        {
            try
            {
                var response = JsonUtility.FromJson<OSMOverpassResponse>(json);
                if (response?.elements == null) return;
                
                foreach (var element in response.elements)
                {
                    if (element.type != "way" || element.geometry == null) continue;
                    
                    var area = new OSMNaturalArea
                    {
                        id = element.id,
                        polygon = new List<Vector2d>()
                    };
                    
                    // Parse geometry
                    foreach (var point in element.geometry)
                    {
                        area.polygon.Add(new Vector2d(point.lat, point.lon));
                    }
                    
                    // Parse tags
                    if (element.tags != null)
                    {
                        area.name = GetTagValue(element.tags, "name");
                        area.leisure = GetTagValue(element.tags, "leisure");
                        area.natural = GetTagValue(element.tags, "natural");
                        area.landuse = GetTagValue(element.tags, "landuse");
                    }
                    
                    // Classify
                    area.areaType = ClassifyNaturalAreaType(area);
                    area.fantasyType = ClassifyNaturalFantasyType(area);
                    area.center = CalculatePolygonCenter(area.polygon);
                    area.area = CalculatePolygonArea(area.polygon);
                    
                    areaData.naturalAreas.Add(area);
                }
            }
            catch (Exception ex)
            {
                if (debugLogging) Debug.LogWarning($"OSM: Failed to parse natural areas: {ex.Message}");
            }
        }
        
        private void ParsePOIsFromJson(string json, OSMAreaData areaData)
        {
            try
            {
                var response = JsonUtility.FromJson<OSMOverpassResponse>(json);
                if (response?.elements == null) return;
                
                foreach (var element in response.elements)
                {
                    if (element.type != "node") continue;
                    
                    var poi = new OSMPointOfInterest
                    {
                        id = element.id,
                        location = new Vector2d(element.lat, element.lon)
                    };
                    
                    // Parse tags
                    if (element.tags != null)
                    {
                        poi.name = GetTagValue(element.tags, "name");
                        poi.amenity = GetTagValue(element.tags, "amenity");
                        poi.shop = GetTagValue(element.tags, "shop");
                        poi.tourism = GetTagValue(element.tags, "tourism");
                    }
                    
                    // Classify
                    poi.poiType = ClassifyPOIType(poi);
                    poi.fantasyRole = ClassifyPOIFantasyRole(poi);
                    
                    areaData.pointsOfInterest.Add(poi);
                }
            }
            catch (Exception ex)
            {
                if (debugLogging) Debug.LogWarning($"OSM: Failed to parse POIs: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Classification
        
        private FantasyBuildingType ClassifyBuildingFantasyType(OSMBuilding building)
        {
            // Check specific types first
            if (!string.IsNullOrEmpty(building.amenity))
            {
                switch (building.amenity.ToLower())
                {
                    case "place_of_worship": return FantasyBuildingType.Cathedral;
                    case "school":
                    case "university": return FantasyBuildingType.Academy;
                    case "hospital": return FantasyBuildingType.Sanctuary;
                    case "library": return FantasyBuildingType.Library;
                    case "bank": return FantasyBuildingType.Treasury;
                    case "restaurant":
                    case "cafe":
                    case "pub":
                    case "bar": return FantasyBuildingType.Tavern;
                    case "police": return FantasyBuildingType.Barracks;
                    case "fire_station": return FantasyBuildingType.WatchTower;
                }
            }
            
            if (!string.IsNullOrEmpty(building.shop))
            {
                switch (building.shop.ToLower())
                {
                    case "supermarket":
                    case "mall": return FantasyBuildingType.Marketplace;
                    case "bakery": return FantasyBuildingType.Bakery;
                    case "butcher": return FantasyBuildingType.Butcher;
                    case "blacksmith":
                    case "hardware": return FantasyBuildingType.Blacksmith;
                    default: return FantasyBuildingType.Shop;
                }
            }
            
            // Classify by size
            if (building.area > 5000) return FantasyBuildingType.Castle;
            if (building.area > 2000) return FantasyBuildingType.Manor;
            if (building.area > 500) return FantasyBuildingType.House;
            if (building.area > 100) return FantasyBuildingType.Cottage;
            return FantasyBuildingType.Hut;
        }
        
        private RoadType ClassifyRoadType(string highway)
        {
            if (string.IsNullOrEmpty(highway)) return RoadType.Path;
            
            return highway.ToLower() switch
            {
                "motorway" or "trunk" => RoadType.Highway,
                "primary" or "secondary" => RoadType.MainRoad,
                "tertiary" or "residential" => RoadType.Street,
                "service" or "unclassified" => RoadType.Alley,
                "footway" or "path" or "pedestrian" => RoadType.Path,
                "cycleway" => RoadType.Path,
                "track" => RoadType.DirtRoad,
                _ => RoadType.Street
            };
        }
        
        private FantasyRoadStyle ClassifyRoadFantasyStyle(OSMRoad road)
        {
            return road.roadType switch
            {
                RoadType.Highway => FantasyRoadStyle.RoyalHighway,
                RoadType.MainRoad => FantasyRoadStyle.CobblestoneRoad,
                RoadType.Street => FantasyRoadStyle.CobblestoneStreet,
                RoadType.Alley => FantasyRoadStyle.StoneAlley,
                RoadType.DirtRoad => FantasyRoadStyle.DirtPath,
                RoadType.Path => FantasyRoadStyle.ForestTrail,
                _ => FantasyRoadStyle.CobblestoneStreet
            };
        }
        
        private NaturalAreaType ClassifyNaturalAreaType(OSMNaturalArea area)
        {
            if (area.natural == "water") return NaturalAreaType.Water;
            if (area.landuse == "forest") return NaturalAreaType.Forest;
            if (area.leisure == "park") return NaturalAreaType.Park;
            if (area.landuse == "grass") return NaturalAreaType.Grassland;
            if (area.natural == "wetland") return NaturalAreaType.Swamp;
            return NaturalAreaType.Grassland;
        }
        
        private FantasyNaturalType ClassifyNaturalFantasyType(OSMNaturalArea area)
        {
            return area.areaType switch
            {
                NaturalAreaType.Water => FantasyNaturalType.MagicalLake,
                NaturalAreaType.Forest => FantasyNaturalType.EnchantedForest,
                NaturalAreaType.Park => FantasyNaturalType.RoyalGarden,
                NaturalAreaType.Grassland => FantasyNaturalType.MeadowField,
                NaturalAreaType.Swamp => FantasyNaturalType.MysticalSwamp,
                _ => FantasyNaturalType.MeadowField
            };
        }
        
        private POIType ClassifyPOIType(OSMPointOfInterest poi)
        {
            if (!string.IsNullOrEmpty(poi.amenity))
            {
                return poi.amenity.ToLower() switch
                {
                    "restaurant" or "cafe" or "fast_food" => POIType.Food,
                    "bank" or "atm" => POIType.Bank,
                    "hospital" or "clinic" or "pharmacy" => POIType.Medical,
                    "school" or "university" or "library" => POIType.Education,
                    "place_of_worship" => POIType.Religious,
                    "police" or "fire_station" => POIType.Government,
                    "fuel" or "parking" => POIType.Transportation,
                    _ => POIType.Other
                };
            }
            
            if (!string.IsNullOrEmpty(poi.shop)) return POIType.Shop;
            if (!string.IsNullOrEmpty(poi.tourism)) return POIType.Tourism;
            
            return POIType.Other;
        }
        
        private FantasyPOIRole ClassifyPOIFantasyRole(OSMPointOfInterest poi)
        {
            return poi.poiType switch
            {
                POIType.Food => FantasyPOIRole.Tavern,
                POIType.Shop => FantasyPOIRole.Merchant,
                POIType.Bank => FantasyPOIRole.Treasury,
                POIType.Medical => FantasyPOIRole.Healer,
                POIType.Education => FantasyPOIRole.Scholar,
                POIType.Religious => FantasyPOIRole.Temple,
                POIType.Government => FantasyPOIRole.GuardPost,
                POIType.Tourism => FantasyPOIRole.Landmark,
                POIType.Transportation => FantasyPOIRole.Stable,
                _ => FantasyPOIRole.Landmark
            };
        }
        
        #endregion
        
        #region Utilities
        
        private string GetCacheKey(double lat, double lon, float radius)
        {
            return $"area_{lat:F3}_{lon:F3}_{radius:F0}";
        }
        
        private string GetTagValue(OSMTag[] tags, string key)
        {
            if (tags == null) return null;
            foreach (var tag in tags)
            {
                if (tag.k == key) return tag.v;
            }
            return null;
        }
        
        private float ParseHeight(string heightStr)
        {
            if (string.IsNullOrEmpty(heightStr)) return 0;
            
            heightStr = heightStr.Replace("m", "").Trim();
            if (float.TryParse(heightStr, out float height))
            {
                return height;
            }
            return 0;
        }
        
        private int ParseInt(string str, int defaultValue)
        {
            if (string.IsNullOrEmpty(str)) return defaultValue;
            return int.TryParse(str, out int value) ? value : defaultValue;
        }
        
        private float GetRoadWidth(RoadType type, int lanes)
        {
            float baseWidth = type switch
            {
                RoadType.Highway => 12f,
                RoadType.MainRoad => 8f,
                RoadType.Street => 6f,
                RoadType.Alley => 4f,
                RoadType.DirtRoad => 3f,
                RoadType.Path => 2f,
                _ => 5f
            };
            
            return baseWidth + (lanes - 1) * 3f;
        }
        
        private Vector2d CalculatePolygonCenter(List<Vector2d> polygon)
        {
            if (polygon == null || polygon.Count == 0)
                return new Vector2d(0, 0);
            
            double lat = 0, lon = 0;
            foreach (var point in polygon)
            {
                lat += point.latitude;
                lon += point.longitude;
            }
            
            return new Vector2d(lat / polygon.Count, lon / polygon.Count);
        }
        
        private float CalculatePolygonArea(List<Vector2d> polygon)
        {
            if (polygon == null || polygon.Count < 3) return 0;
            
            // Shoelace formula in meters
            double area = 0;
            int j = polygon.Count - 1;
            
            for (int i = 0; i < polygon.Count; i++)
            {
                // Convert to approximate meters
                double x1 = polygon[j].longitude * 111320 * Math.Cos(polygon[j].latitude * Math.PI / 180);
                double y1 = polygon[j].latitude * 110540;
                double x2 = polygon[i].longitude * 111320 * Math.Cos(polygon[i].latitude * Math.PI / 180);
                double y2 = polygon[i].latitude * 110540;
                
                area += (x1 + x2) * (y1 - y2);
                j = i;
            }
            
            return (float)Math.Abs(area / 2);
        }
        
        #endregion
    }
    
    #region Data Classes
    
    [Serializable]
    public class OSMAreaData
    {
        public double centerLatitude;
        public double centerLongitude;
        public double boundsSouth;
        public double boundsWest;
        public double boundsNorth;
        public double boundsEast;
        public DateTime timestamp;
        
        public List<OSMBuilding> buildings = new List<OSMBuilding>();
        public List<OSMRoad> roads = new List<OSMRoad>();
        public List<OSMNaturalArea> naturalAreas = new List<OSMNaturalArea>();
        public List<OSMPointOfInterest> pointsOfInterest = new List<OSMPointOfInterest>();
    }
    
    [Serializable]
    public class OSMBuilding
    {
        public long id;
        public string name;
        public string buildingType;
        public float height;
        public int levels;
        public float area;
        public Vector2d center;
        public List<Vector2d> footprint;
        public string amenity;
        public string shop;
        public FantasyBuildingType fantasyType;
    }
    
    [Serializable]
    public class OSMRoad
    {
        public long id;
        public string name;
        public string highway;
        public int lanes;
        public string surface;
        public bool oneway;
        public float width;
        public List<Vector2d> points;
        public RoadType roadType;
        public FantasyRoadStyle fantasyStyle;
    }
    
    [Serializable]
    public class OSMNaturalArea
    {
        public long id;
        public string name;
        public string leisure;
        public string natural;
        public string landuse;
        public float area;
        public Vector2d center;
        public List<Vector2d> polygon;
        public NaturalAreaType areaType;
        public FantasyNaturalType fantasyType;
    }
    
    [Serializable]
    public class OSMPointOfInterest
    {
        public long id;
        public string name;
        public string amenity;
        public string shop;
        public string tourism;
        public Vector2d location;
        public POIType poiType;
        public FantasyPOIRole fantasyRole;
    }
    
    [Serializable]
    public struct Vector2d
    {
        public double latitude;
        public double longitude;
        
        public Vector2d(double lat, double lon)
        {
            latitude = lat;
            longitude = lon;
        }
    }
    
    #endregion
    
    #region Enums
    
    public enum FantasyBuildingType
    {
        Hut,
        Cottage,
        House,
        Manor,
        Castle,
        Cathedral,
        Academy,
        Sanctuary,
        Library,
        Treasury,
        Tavern,
        Barracks,
        WatchTower,
        Marketplace,
        Bakery,
        Butcher,
        Blacksmith,
        Shop,
        Fortress,
        Palace
    }
    
    public enum RoadType
    {
        Highway,
        MainRoad,
        Street,
        Alley,
        DirtRoad,
        Path
    }
    
    public enum FantasyRoadStyle
    {
        RoyalHighway,
        CobblestoneRoad,
        CobblestoneStreet,
        StoneAlley,
        DirtPath,
        ForestTrail,
        MarketSquare,
        Alley,
        Bridge
    }
    
    public enum NaturalAreaType
    {
        Park,
        Forest,
        Water,
        Grassland,
        Swamp,
        Beach,
        Mountain
    }
    
    public enum FantasyNaturalType
    {
        RoyalGarden,
        EnchantedForest,
        MagicalLake,
        MeadowField,
        MysticalSwamp,
        CoastalShore,
        DragonPeak,
        MagicalRiver,
        EnchantedLake,
        MysticalGrove
    }
    
    public enum POIType
    {
        Food,
        Shop,
        Bank,
        Medical,
        Education,
        Religious,
        Government,
        Transportation,
        Tourism,
        Other
    }
    
    public enum FantasyPOIRole
    {
        Tavern,
        Merchant,
        Treasury,
        Healer,
        Scholar,
        Temple,
        GuardPost,
        Stable,
        Landmark,
        MysterySpot
    }
    
    #endregion
    
    #region JSON Response Classes (for parsing)
    
    [Serializable]
    public class OSMOverpassResponse
    {
        public OSMElement[] elements;
    }
    
    [Serializable]
    public class OSMElement
    {
        public string type;
        public long id;
        public double lat;
        public double lon;
        public OSMTag[] tags;
        public OSMGeomPoint[] geometry;
    }
    
    [Serializable]
    public class OSMTag
    {
        public string k;
        public string v;
    }
    
    [Serializable]
    public class OSMGeomPoint
    {
        public double lat;
        public double lon;
    }
    
    #endregion
    
    internal class OSMRequest
    {
        public double latitude;
        public double longitude;
        public float radiusMeters;
        public string cacheKey;
        public Action<OSMAreaData> onComplete;
        public Action<string> onError;
    }
}
