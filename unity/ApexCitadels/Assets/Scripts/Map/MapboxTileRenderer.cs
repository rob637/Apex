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
        [SerializeField] private int gridSize = 9;  // 9x9 grid (81 tiles) for initial coverage
        [SerializeField] private float tileWorldSize = 50f;  // Smaller tiles for higher zoom
        [SerializeField] private float groundHeight = -1f;  // Height of ground plane (lowered to avoid z-fighting)
        
        [Header("Location")]
        [SerializeField] private double centerLatitude = 38.9032;  // Vienna Town Green, VA
        [SerializeField] private double centerLongitude = -77.2646;
        [SerializeField] private int zoomLevel = 17;  // Higher zoom = more detail, fewer tiles needed
        
        [Header("AAA Visual Quality")]
        [SerializeField] private bool useUnlit = true;  // Unlit for map tiles looks cleaner
        [SerializeField] private FilterMode textureFilterMode = FilterMode.Trilinear;
        [SerializeField] private int anisotropicLevel = 16;  // Max anisotropic filtering
        [SerializeField] private bool generateMipmaps = true;  // Crisp at all distances
        [SerializeField] private bool useHighQualityMaterial = true;
        
        [Header("Dynamic Loading")]
        [SerializeField] private bool enableDynamicLoading = true;  // Load tiles as camera moves
        [SerializeField] private float loadDistance = 200f;  // How far ahead to load tiles
        [SerializeField] private float unloadDistance = 1000f;  // How far before unloading tiles
        [SerializeField] private float updateInterval = 0.5f;  // How often to check for new tiles
        
        // Runtime state
        private Dictionary<string, TileData> _tiles = new Dictionary<string, TileData>();
        private Transform _tilesContainer;
        private int _centerTileX;
        private int _centerTileY;
        private bool _isInitialized;
        private Material _sharedTileMaterial;
        private Camera _mainCamera;
        private Vector3 _lastCameraPosition;
        private float _lastUpdateTime;
        
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
            
            Debug.Log($"[Mapbox] === LOCATION CHECK ===");
            Debug.Log($"[Mapbox] Config values: lat={config.DefaultLatitude}, lon={config.DefaultLongitude}, zoom={config.DefaultZoom}");
            Debug.Log($"[Mapbox] After assignment: lat={centerLatitude}, lon={centerLongitude}, zoom={zoomLevel}");
            Debug.Log($"[Mapbox] Expected: Vienna Town Green, VA (38.9032, -77.2646)");
            
            // Safety check: if coordinates are way off, reset to Vienna
            if (centerLatitude < 38.0 || centerLatitude > 40.0 || centerLongitude < -78.0 || centerLongitude > -76.0)
            {
                Debug.LogWarning($"[Mapbox] Coordinates look wrong! Resetting to Vienna, VA");
                centerLatitude = 38.9032;
                centerLongitude = -77.2646;
            }
            
            Debug.Log($"[Mapbox] Final location: {centerLatitude}, {centerLongitude} zoom {zoomLevel}");
            
            Initialize();
        }
        
        /// <summary>
        /// Initialize the map tile grid
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            
            ApexLogger.Log($"[Mapbox] Initializing at {centerLatitude}, {centerLongitude} zoom {zoomLevel}", ApexLogger.LogCategory.Map);
            
            // Destroy conflicting terrain systems
            DestroyConflictingTerrain();
            
            // Create container
            _tilesContainer = new GameObject("MapTiles").transform;
            _tilesContainer.parent = transform;
            _tilesContainer.localPosition = Vector3.zero;
            
            // Get main camera for dynamic loading
            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                _lastCameraPosition = _mainCamera.transform.position;
            }
            
            // Calculate center tile coordinates
            CalculateCenterTile();
            
            // Create tile grid
            CreateTileGrid();
            
            _isInitialized = true;
            
            // Start loading tiles
            StartCoroutine(LoadAllTiles());
        }
        
        private void Update()
        {
            if (!_isInitialized || !enableDynamicLoading) return;
            
            // Throttle updates
            if (Time.time - _lastUpdateTime < updateInterval) return;
            _lastUpdateTime = Time.time;
            
            // Get camera if we don't have it
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
                _lastCameraPosition = _mainCamera.transform.position;
            }
            
            // Check if camera moved significantly
            Vector3 cameraPos = _mainCamera.transform.position;
            float moveDistance = Vector3.Distance(cameraPos, _lastCameraPosition);
            
            if (moveDistance > tileWorldSize * 0.5f)
            {
                _lastCameraPosition = cameraPos;
                UpdateTilesAroundCamera(cameraPos);
            }
        }
        
        /// <summary>
        /// Load tiles around the camera position
        /// </summary>
        private void UpdateTilesAroundCamera(Vector3 cameraPos)
        {
            // Convert camera world position to tile offset from center
            int cameraTileOffsetX = Mathf.RoundToInt(cameraPos.x / tileWorldSize);
            int cameraTileOffsetY = Mathf.RoundToInt(-cameraPos.z / tileWorldSize);  // Negate Z
            
            int halfGrid = gridSize / 2;
            int tilesLoaded = 0;
            
            // Check tiles around camera
            for (int dy = -halfGrid; dy <= halfGrid; dy++)
            {
                for (int dx = -halfGrid; dx <= halfGrid; dx++)
                {
                    int localX = cameraTileOffsetX + dx;
                    int localY = cameraTileOffsetY + dy;
                    int tileX = _centerTileX + localX;
                    int tileY = _centerTileY + localY;
                    
                    string key = $"{zoomLevel}/{tileX}/{tileY}";
                    
                    // Create tile if it doesn't exist
                    if (!_tiles.ContainsKey(key))
                    {
                        CreateTilePlane(tileX, tileY, localX, localY);
                        if (_tiles.TryGetValue(key, out var tile))
                        {
                            StartCoroutine(LoadTile(key, tile));
                        }
                        tilesLoaded++;
                    }
                }
            }
            
            if (tilesLoaded > 0)
            {
                Debug.Log($"[Mapbox] Dynamic loading: added {tilesLoaded} new tiles around camera");
            }
            
            // Optional: Unload distant tiles to save memory
            UnloadDistantTiles(cameraPos);
        }
        
        /// <summary>
        /// Unload tiles that are too far from camera
        /// </summary>
        private void UnloadDistantTiles(Vector3 cameraPos)
        {
            List<string> tilesToRemove = new List<string>();
            
            foreach (var kvp in _tiles)
            {
                if (kvp.Value.GameObject == null) continue;
                
                float distance = Vector3.Distance(kvp.Value.GameObject.transform.position, cameraPos);
                if (distance > unloadDistance)
                {
                    tilesToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in tilesToRemove)
            {
                if (_tiles.TryGetValue(key, out TileData tile))
                {
                    if (tile.Texture != null) Destroy(tile.Texture);
                    if (tile.GameObject != null) Destroy(tile.GameObject);
                    _tiles.Remove(key);
                }
            }
            
            if (tilesToRemove.Count > 0)
            {
                Debug.Log($"[Mapbox] Unloaded {tilesToRemove.Count} distant tiles");
            }
        }
        
        /// <summary>
        /// Destroy terrain systems that would conflict with Mapbox tiles
        /// </summary>
        private void DestroyConflictingTerrain()
        {
            // List of objects to destroy - includes other map systems
            string[] conflictingNames = { 
                "GridOverlay", "FantasyTerrain", "ProceduralTerrain", 
                "WorldTerrain", "TerrainMesh", "WaterPlane", "GroundPlane",
                "ProceduralTerrainSystem", "GeoMapSystem", "RealWorldMap",
                "MapTiles", "Tiles"  // Old tile containers
            };
            
            foreach (var name in conflictingNames)
            {
                var obj = GameObject.Find(name);
                if (obj != null && obj != gameObject && !obj.transform.IsChildOf(transform))
                {
                    Debug.Log($"[Mapbox] Destroying conflicting object: {name}");
                    Destroy(obj);
                }
            }
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
            // Convert lat/lon to tile coordinates using Web Mercator projection
            double n = System.Math.Pow(2, zoomLevel);
            double latRad = centerLatitude * System.Math.PI / 180.0;
            
            _centerTileX = (int)((centerLongitude + 180.0) / 360.0 * n);
            _centerTileY = (int)((1.0 - System.Math.Log(System.Math.Tan(latRad) + 1.0 / System.Math.Cos(latRad)) / System.Math.PI) / 2.0 * n);
            
            Debug.Log($"[Mapbox] === TILE CALCULATION ===");
            Debug.Log($"[Mapbox] Input: lat={centerLatitude}, lon={centerLongitude}, zoom={zoomLevel}");
            Debug.Log($"[Mapbox] n = 2^{zoomLevel} = {n}");
            Debug.Log($"[Mapbox] Center tile: X={_centerTileX}, Y={_centerTileY}");
            
            // Expected for Vienna (38.9, -77.3) at zoom 14: roughly X=4699, Y=6304
            // Expected for Vienna (38.9, -77.3) at zoom 15: roughly X=9398, Y=12609
            Debug.Log($"[Mapbox] Expected Vienna zoom 14: ~X=4699, Y=6304");
            Debug.Log($"[Mapbox] Expected Vienna zoom 15: ~X=9398, Y=12609");
            
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
                Debug.LogError("[Mapbox] Cannot load tiles - no valid configuration");
                yield break;
            }
            
            int loaded = 0;
            int failed = 0;
            int total = _tiles.Count;
            int batchSize = 5;  // Load 5 tiles at once for faster loading
            int currentBatch = 0;
            
            Debug.Log($"[Mapbox] Starting to load {total} tiles (batch size: {batchSize})...");
            
            // Collect tiles to load
            var tilesToLoad = new List<KeyValuePair<string, TileData>>();
            foreach (var kvp in _tiles)
            {
                if (!kvp.Value.IsLoading && kvp.Value.Texture == null)
                {
                    tilesToLoad.Add(kvp);
                }
            }
            
            // Load in batches for smoother performance
            for (int i = 0; i < tilesToLoad.Count; i++)
            {
                var kvp = tilesToLoad[i];
                StartCoroutine(LoadTileSilent(kvp.Key, kvp.Value, (success) => {
                    if (success) loaded++; else failed++;
                }));
                
                currentBatch++;
                
                // Wait every batchSize tiles to let them load
                if (currentBatch >= batchSize)
                {
                    currentBatch = 0;
                    yield return new WaitForSeconds(0.1f);  // Small delay between batches
                }
            }
            
            // Wait for all tiles to finish
            yield return new WaitForSeconds(1f);
            
            Debug.Log($"[Mapbox] COMPLETE: {loaded} tiles loaded, {failed} failed out of {total}");
            OnMapLoaded?.Invoke();
        }
        
        private IEnumerator LoadTileSilent(string key, TileData tile, System.Action<bool> onComplete)
        {
            yield return StartCoroutine(LoadTile(key, tile));
            onComplete?.Invoke(tile.Texture != null);
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
                    Debug.LogError($"[Mapbox] FAILED to load tile {key}: {www.error} (HTTP {www.responseCode})");
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
