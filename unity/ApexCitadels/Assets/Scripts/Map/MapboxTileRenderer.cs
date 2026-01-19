using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using ApexCitadels.Core;

namespace ApexCitadels.Map
{
    /// <summary>
    /// Renders real-world Mapbox tiles as the ground plane.
    /// Replaces the procedural green ground with actual geographic imagery.
    /// AAA-quality rendering with mipmaps, anisotropic filtering, and retina support.
    /// </summary>
    public class MapboxTileRenderer : MonoBehaviour
    {
        public static MapboxTileRenderer Instance { get; private set; }
        
        [Header("Configuration")]
        [SerializeField] private MapboxConfiguration config;
        
        [Header("Tile Grid")]
        [SerializeField] private int gridSize = 7;  // 7x7 grid for better coverage
        [SerializeField] private float tileWorldSize = 100f;  // Size of each tile in world units
        [SerializeField] private float groundHeight = -1f;  // Height of ground plane (lowered to avoid z-fighting)
        
        [Header("Location")]
        [SerializeField] private double centerLatitude = 38.9012;  // Vienna, VA
        [SerializeField] private double centerLongitude = -77.2653;
        [SerializeField] private int zoomLevel = 15;
        
        [Header("AAA Visual Quality")]
        [SerializeField] private bool useUnlit = true;  // Unlit for map tiles looks cleaner
        [SerializeField] private FilterMode textureFilterMode = FilterMode.Trilinear;
        [SerializeField] private int anisotropicLevel = 16;  // Max anisotropic filtering
        [SerializeField] private bool generateMipmaps = true;  // Crisp at all distances
        [SerializeField] private bool useHighQualityMaterial = true;
        
        // Runtime state
        private Dictionary<string, TileData> _tiles = new Dictionary<string, TileData>();
        private Transform _tilesContainer;
        private int _centerTileX;
        private int _centerTileY;
        private bool _isInitialized;
        private Material _sharedTileMaterial;
        
        // Events
        public System.Action OnMapLoaded;
        public System.Action<double, double> OnLocationChanged;
        
        private class TileData
        {
            public GameObject GameObject;
            public Texture2D Texture;
            public bool IsLoading;
            public int X;
            public int Y;
        }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private void Start()
        {
            Debug.Log("[Mapbox] === MapboxTileRenderer Starting ===");
            
            // Try to load config from Resources
            if (config == null)
            {
                config = UnityEngine.Resources.Load<MapboxConfiguration>("MapboxConfig");
                Debug.Log($"[Mapbox] Loaded config from Resources: {(config != null ? "SUCCESS" : "FAILED")}");
            }
            
            if (config == null)
            {
                Debug.LogError("[Mapbox] ERROR: MapboxConfig.asset not found in Resources folder!");
                return;
            }
            
            if (!config.IsValid)
            {
                Debug.LogError($"[Mapbox] ERROR: Config invalid. Token: {(string.IsNullOrEmpty(config.AccessToken) ? "EMPTY" : config.AccessToken.Substring(0, 10) + "...")}");
                return;
            }
            
            Debug.Log($"[Mapbox] Config valid. Style: {config.Style}, Location: {config.DefaultLatitude}, {config.DefaultLongitude}");
            
            // Use config defaults
            centerLatitude = config.DefaultLatitude;
            centerLongitude = config.DefaultLongitude;
            zoomLevel = config.DefaultZoom;
            
            Debug.Log($"[Mapbox] Initializing at {centerLatitude}, {centerLongitude} zoom {zoomLevel}");
            
            Initialize();
        }
        
        /// <summary>
        /// Initialize the map tile grid
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            
            ApexLogger.Log($"[Mapbox] Initializing at {centerLatitude}, {centerLongitude} zoom {zoomLevel}", ApexLogger.LogCategory.Map);
            
            // Create container
            _tilesContainer = new GameObject("MapboxTiles").transform;
            _tilesContainer.parent = transform;
            _tilesContainer.localPosition = Vector3.zero;
            
            // Calculate center tile coordinates
            CalculateCenterTile();
            
            // Create tile grid
            CreateTileGrid();
            
            _isInitialized = true;
            
            // Start loading tiles
            StartCoroutine(LoadAllTiles());
        }
        
        /// <summary>
        /// Set a new center location
        /// </summary>
        public void SetLocation(double latitude, double longitude, int? zoom = null)
        {
            centerLatitude = latitude;
            centerLongitude = longitude;
            if (zoom.HasValue) zoomLevel = Mathf.Clamp(zoom.Value, 1, 18);
            
            ApexLogger.Log($"[Mapbox] Location changed to {latitude}, {longitude} zoom {zoomLevel}", ApexLogger.LogCategory.Map);
            
            // Recalculate and refresh tiles
            CalculateCenterTile();
            RefreshTiles();
            
            OnLocationChanged?.Invoke(latitude, longitude);
        }
        
        /// <summary>
        /// Move the map by a delta in world units
        /// </summary>
        public void Pan(Vector3 delta)
        {
            // Convert world delta to lat/lon delta
            // At zoom 15, one tile is about 1.2km
            double metersPerUnit = GetMetersPerWorldUnit();
            
            // Longitude changes with latitude (cosine projection)
            double latRadians = centerLatitude * Mathf.Deg2Rad;
            
            double lonDelta = (delta.x * metersPerUnit) / (111320 * System.Math.Cos(latRadians));
            double latDelta = (delta.z * metersPerUnit) / 110540;  // Meters per degree latitude
            
            SetLocation(centerLatitude + latDelta, centerLongitude + lonDelta);
        }
        
        /// <summary>
        /// Zoom in or out
        /// </summary>
        public void Zoom(int delta)
        {
            SetLocation(centerLatitude, centerLongitude, zoomLevel + delta);
        }
        
        private void CalculateCenterTile()
        {
            // Convert lat/lon to tile coordinates
            double n = System.Math.Pow(2, zoomLevel);
            double latRad = centerLatitude * System.Math.PI / 180.0;
            
            _centerTileX = (int)((centerLongitude + 180.0) / 360.0 * n);
            _centerTileY = (int)((1.0 - System.Math.Log(System.Math.Tan(latRad) + 1.0 / System.Math.Cos(latRad)) / System.Math.PI) / 2.0 * n);
            
            ApexLogger.Log($"[Mapbox] Center tile: {_centerTileX}, {_centerTileY}", ApexLogger.LogCategory.Map);
        }
        
        private void CreateTileGrid()
        {
            int halfGrid = gridSize / 2;
            
            for (int dy = -halfGrid; dy <= halfGrid; dy++)
            {
                for (int dx = -halfGrid; dx <= halfGrid; dx++)
                {
                    int tileX = _centerTileX + dx;
                    int tileY = _centerTileY + dy;
                    
                    CreateTilePlane(tileX, tileY, dx, dy);
                }
            }
        }
        
        private void CreateTilePlane(int tileX, int tileY, int localX, int localY)
        {
            string key = $"{zoomLevel}/{tileX}/{tileY}";
            
            if (_tiles.ContainsKey(key)) return;
            
            // Create plane using a Quad instead of Plane to avoid z-fighting
            // Quad faces +Z, we rotate to face up
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plane.name = $"Tile_{tileX}_{tileY}";
            plane.transform.parent = _tilesContainer;
            
            // Rotate to face up (Quad faces +Z by default)
            plane.transform.localRotation = Quaternion.Euler(90, 0, 0);
            
            // Position based on local offset with small Y offset per tile to prevent z-fighting
            float zFightOffset = (localX + localY) * 0.001f; // Tiny offset per tile
            Vector3 position = new Vector3(
                localX * tileWorldSize,
                groundHeight + zFightOffset,
                -localY * tileWorldSize  // Negate because tile Y increases downward
            );
            plane.transform.localPosition = position;
            
            // Scale to tile size (Quad is 1x1 by default)
            plane.transform.localScale = new Vector3(tileWorldSize, tileWorldSize, 1);
            
            // Create AAA-quality material
            Material mat = CreateTileMaterial();
            plane.GetComponent<Renderer>().material = mat;
            
            // Enable shadows for realism
            var renderer = plane.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            
            // Remove collider (we'll add our own for picking)
            Destroy(plane.GetComponent<Collider>());
            
            // Store tile data
            _tiles[key] = new TileData
            {
                GameObject = plane,
                X = tileX,
                Y = tileY,
                IsLoading = false
            };
        }
        
        /// <summary>
        /// Create high-quality material for map tiles
        /// </summary>
        private Material CreateTileMaterial()
        {
            Material mat;
            
            if (useUnlit)
            {
                // Try URP Unlit first, then fallback
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Texture");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
            }
            else if (useHighQualityMaterial)
            {
                // URP Lit with proper settings for ground
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                
                // Set up for ground rendering
                mat.SetFloat("_Smoothness", 0.1f);  // Low smoothness for ground
                mat.SetFloat("_Metallic", 0f);
                mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            }
            else
            {
                mat = new Material(Shader.Find("Standard"));
            }
            
            // Initial loading color (subtle gray)
            mat.color = new Color(0.25f, 0.25f, 0.28f);
            
            return mat;
        }
        
        private void RefreshTiles()
        {
            // Clear old tiles
            foreach (var tile in _tiles.Values)
            {
                if (tile.GameObject != null)
                    Destroy(tile.GameObject);
                if (tile.Texture != null)
                    Destroy(tile.Texture);
            }
            _tiles.Clear();
            
            // Recreate grid
            CreateTileGrid();
            
            // Reload
            StartCoroutine(LoadAllTiles());
        }
        
        private IEnumerator LoadAllTiles()
        {
            if (config == null || !config.IsValid)
            {
                ApexLogger.LogWarning("[Mapbox] Cannot load tiles - no valid configuration", ApexLogger.LogCategory.Map);
                yield break;
            }
            
            int loaded = 0;
            int total = _tiles.Count;
            
            foreach (var kvp in _tiles)
            {
                if (!kvp.Value.IsLoading && kvp.Value.Texture == null)
                {
                    yield return StartCoroutine(LoadTile(kvp.Key, kvp.Value));
                    loaded++;
                    
                    // Small delay to avoid hammering the API
                    if (loaded % 4 == 0)
                        yield return new WaitForSeconds(0.1f);
                }
            }
            
            ApexLogger.Log($"[Mapbox] Loaded {loaded} tiles", ApexLogger.LogCategory.Map);
            OnMapLoaded?.Invoke();
        }
        
        private IEnumerator LoadTile(string key, TileData tile)
        {
            tile.IsLoading = true;
            
            string url = config.GetTileUrl(tile.X, tile.Y, zoomLevel);
            Debug.Log($"[Mapbox] Loading tile {key} from: {url.Substring(0, 80)}...");
            
            // Use standard UnityWebRequest to avoid dependency on UnityWebRequestTexture module
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();
                
                if (www.result == UnityWebRequest.Result.Success)
                {
                    // Create AAA-quality texture with mipmaps for crisp rendering at all distances
                    // We need to load first without mipmaps, then recreate with mipmaps
                    Texture2D tempTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    
                    if (tempTex.LoadImage(www.downloadHandler.data))
                    {
                        // Create final texture with mipmaps for quality at distance
                        if (generateMipmaps)
                        {
                            tile.Texture = new Texture2D(tempTex.width, tempTex.height, TextureFormat.RGBA32, true);
                            tile.Texture.SetPixels(tempTex.GetPixels());
                            tile.Texture.Apply(true, false);  // Generate mipmaps
                            Destroy(tempTex);
                        }
                        else
                        {
                            tile.Texture = tempTex;
                        }
                        
                        // AAA Quality Settings
                        tile.Texture.wrapMode = TextureWrapMode.Clamp;
                        tile.Texture.filterMode = textureFilterMode;  // Trilinear for smooth blending
                        tile.Texture.anisoLevel = anisotropicLevel;   // Max anisotropic filtering
                        
                        if (tile.GameObject != null)
                        {
                            Material mat = tile.GameObject.GetComponent<Renderer>().material;
                            mat.mainTexture = tile.Texture;
                            mat.color = Color.white;
                            
                            // Ensure texture settings are applied to material
                            if (mat.HasProperty("_MainTex"))
                            {
                                mat.SetTexture("_MainTex", tile.Texture);
                            }
                        }
                        
                        ApexLogger.Log($"[Mapbox] Loaded tile {key} ({tile.Texture.width}x{tile.Texture.height}, Mipmaps: {tile.Texture.mipmapCount})", ApexLogger.LogCategory.Map);
                    }
                    else
                    {
                        ApexLogger.LogWarning($"[Mapbox] Failed to decode tile image {key}", ApexLogger.LogCategory.Map);
                        Destroy(tempTex);
                    }
                }
                else
                {
                    ApexLogger.LogWarning($"[Mapbox] Failed to load tile {key}: {www.error}", ApexLogger.LogCategory.Map);
                }
            }
            
            tile.IsLoading = false;
        }
        
        private double GetMetersPerWorldUnit()
        {
            // At zoom 15, one tile is ~1.2km, covering tileWorldSize units
            double tileMeters = 40075016.686 * System.Math.Cos(centerLatitude * System.Math.PI / 180) / System.Math.Pow(2, zoomLevel);
            return tileMeters / tileWorldSize;
        }
        
        /// <summary>
        /// Convert world position to lat/lon
        /// </summary>
        public (double lat, double lon) WorldToLatLon(Vector3 worldPos)
        {
            double metersPerUnit = GetMetersPerWorldUnit();
            double latRadians = centerLatitude * Mathf.Deg2Rad;
            
            double lon = centerLongitude + (worldPos.x * metersPerUnit) / (111320 * System.Math.Cos(latRadians));
            double lat = centerLatitude + (worldPos.z * metersPerUnit) / 110540;
            
            return (lat, lon);
        }
        
        /// <summary>
        /// Convert lat/lon to world position
        /// </summary>
        public Vector3 LatLonToWorld(double latitude, double longitude)
        {
            double metersPerUnit = GetMetersPerWorldUnit();
            double latRadians = centerLatitude * Mathf.Deg2Rad;
            
            float x = (float)((longitude - centerLongitude) * 111320 * System.Math.Cos(latRadians) / metersPerUnit);
            float z = (float)((latitude - centerLatitude) * 110540 / metersPerUnit);
            
            return new Vector3(x, 0, z);
        }
        
        /// <summary>
        /// Try to get user's current GPS location
        /// </summary>
        public void UseDeviceLocation()
        {
            StartCoroutine(GetDeviceLocation());
        }
        
        private IEnumerator GetDeviceLocation()
        {
            if (!Input.location.isEnabledByUser)
            {
                ApexLogger.LogWarning("[Mapbox] Location services not enabled", ApexLogger.LogCategory.Map);
                yield break;
            }
            
            Input.location.Start();
            
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }
            
            if (Input.location.status == LocationServiceStatus.Running)
            {
                LocationInfo loc = Input.location.lastData;
                SetLocation(loc.latitude, loc.longitude);
                ApexLogger.Log($"[Mapbox] Using device location: {loc.latitude}, {loc.longitude}", ApexLogger.LogCategory.Map);
            }
            else
            {
                ApexLogger.LogWarning("[Mapbox] Failed to get device location", ApexLogger.LogCategory.Map);
            }
            
            Input.location.Stop();
        }
    }
}
