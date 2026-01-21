using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using ApexCitadels.Core;

namespace ApexCitadels.Map
{
    /// <summary>
    /// AAA-quality Mapbox tile renderer for real-world map display.
    /// This is the PRIMARY map system - all other terrain systems should be disabled when this is active.
    /// </summary>
    public class MapboxTileRenderer : MonoBehaviour
    {
        public static MapboxTileRenderer Instance { get; private set; }
        
        [Header("Configuration")]
        [SerializeField] private MapboxConfiguration config;
        
        [Header("Tile Grid")]
        [SerializeField] private int gridSize = 7;            // 7x7 = 49 tiles (reduced for performance)
        [SerializeField] private float tileWorldSize = 80f;   // World units per tile
        [SerializeField] private float groundHeight = 0f;  // Y position of tiles (0 = aligned with buildings)
        
        [Header("Streaming")]
        [SerializeField] private bool enableStreaming = true;  // Enable infinite tile streaming
        [SerializeField] private float streamCheckInterval = 0.5f; // How often to check for needed tiles
        [SerializeField] private int maxConcurrentLoads = 4;   // Max simultaneous tile downloads
        
        [Header("Location")]
        [SerializeField] private double centerLatitude = 38.9032;   // Vienna, VA
        [SerializeField] private double centerLongitude = -77.2646;
        [SerializeField] private int zoomLevel = 16;
        
        [Header("Quality")]
        [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;
        [SerializeField] private int anisotropicLevel = 8;
        
        // Runtime
        private Dictionary<string, TileData> _tiles = new Dictionary<string, TileData>();
        private Transform _tilesContainer;
        private int _centerTileX;
        private int _centerTileY;
        private double _tileCenterLat;  // Geometric center of the center tile
        private double _tileCenterLon;  // Geometric center of the center tile
        private float _tileOffsetX;     // Offset to center tiles on user location
        private float _tileOffsetZ;     // Offset to center tiles on user location
        private bool _isInitialized;
        private bool _isLoading;
        private int _loadedCount;
        private int _totalCount;
        
        // Events
        public System.Action OnMapLoaded;
        public System.Action<double, double> OnLocationChanged;
        public bool IsLoading => _isLoading;
        public float LoadProgress => _totalCount > 0 ? (float)_loadedCount / _totalCount : 0f;
        
        // Streaming state
        private Vector3 _lastCameraPosition;
        private float _lastStreamCheck;
        private int _currentLoadingCount;
        private Camera _mainCamera;
        private bool _isStreaming; // Lock to prevent concurrent streaming
        
        /// <summary>
        /// Get the geometric center of the center tile (where world 0,0,0 is)
        /// </summary>
        public (double lat, double lon) GetTileCenterLatLon() => (_tileCenterLat, _tileCenterLon);
        
        private class TileData
        {
            public GameObject GameObject;
            public Renderer Renderer;
            public Material Material;
            public Texture2D Texture;
            public bool IsLoading;
            public bool IsLoaded;
            public int TileX;
            public int TileY;
        }
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[Mapbox] Duplicate MapboxTileRenderer destroyed");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private void Start()
        {
            Debug.Log("[Mapbox] ========================================");
            Debug.Log("[Mapbox] MapboxTileRenderer Starting");
            Debug.Log("[Mapbox] ========================================");
            
            // Register with SystemCoordinator if available
            var coordinator = SystemCoordinator.Instance;
            if (coordinator != null)
            {
                Debug.Log("[Mapbox] Registered with SystemCoordinator");
            }
            
            // Load config
            if (config == null)
            {
                config = UnityEngine.Resources.Load<MapboxConfiguration>("MapboxConfig");
            }
            
            if (config == null || !config.IsValid)
            {
                Debug.LogError("[Mapbox] No valid MapboxConfiguration found!");
                return;
            }
            
            // Use config values
            centerLatitude = config.DefaultLatitude;
            centerLongitude = config.DefaultLongitude;
            zoomLevel = Mathf.Clamp(config.DefaultZoom, 14, 18);
            
            Debug.Log($"[Mapbox] Location: {centerLatitude}, {centerLongitude} @ zoom {zoomLevel}");
            
            // Clean up conflicting systems BEFORE initializing
            CleanupConflictingSystems();
            
            // Initialize
            Initialize();
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            
            // Cleanup textures
            foreach (var tile in _tiles.Values)
            {
                if (tile.Texture != null) Destroy(tile.Texture);
                if (tile.Material != null) Destroy(tile.Material);
            }
            _tiles.Clear();
        }
        
        private void Update()
        {
            if (!_isInitialized || !enableStreaming) return;
            if (_isLoading || _isStreaming) return; // Don't stream during initial load or if already streaming
            
            // Get camera for position tracking
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
            if (_mainCamera == null) return;
            
            // Check periodically, not every frame
            if (Time.time - _lastStreamCheck < streamCheckInterval) return;
            _lastStreamCheck = Time.time;
            
            // Get camera position projected to ground
            Vector3 camPos = _mainCamera.transform.position;
            Vector3 groundPos = new Vector3(camPos.x, 0, camPos.z);
            
            // Check if camera moved significantly
            float moveDist = Vector3.Distance(groundPos, _lastCameraPosition);
            if (moveDist > tileWorldSize * 0.3f) // Moved 30% of a tile
            {
                _lastCameraPosition = groundPos;
                StartCoroutine(StreamTilesAroundPosition(groundPos));
            }
        }
        
        /// <summary>
        /// Stream tiles around a world position, loading new ones and unloading distant ones
        /// </summary>
        private IEnumerator StreamTilesAroundPosition(Vector3 worldPos)
        {
            if (_isStreaming) yield break;
            _isStreaming = true;
            
            // Convert world position to lat/lon
            var (lat, lon) = WorldToLatLon(worldPos);
            
            // Calculate what tile this corresponds to
            double n = System.Math.Pow(2, zoomLevel);
            double latRad = lat * System.Math.PI / 180.0;
            int newCenterTileX = (int)((lon + 180.0) / 360.0 * n);
            int newCenterTileY = (int)((1.0 - System.Math.Log(System.Math.Tan(latRad) + 1.0 / System.Math.Cos(latRad)) / System.Math.PI) / 2.0 * n);
            
            // Check if we need to shift the grid
            int dx = newCenterTileX - _centerTileX;
            int dy = newCenterTileY - _centerTileY;
            
            if (System.Math.Abs(dx) > 0 || System.Math.Abs(dy) > 0)
            {
                Debug.Log($"[Mapbox] Streaming: shifting grid by ({dx}, {dy})");
                
                // Update center
                centerLatitude = lat;
                centerLongitude = lon;
                _centerTileX = newCenterTileX;
                _centerTileY = newCenterTileY;
                
                // Recalculate tile offset
                _tileCenterLon = (_centerTileX + 0.5) / n * 360.0 - 180.0;
                double tileCenterLatRad = System.Math.Atan(System.Math.Sinh(System.Math.PI * (1.0 - 2.0 * (_centerTileY + 0.5) / n)));
                _tileCenterLat = tileCenterLatRad * 180.0 / System.Math.PI;
                
                double metersPerUnit = GetMetersPerWorldUnit();
                _tileOffsetX = (float)((_tileCenterLon - centerLongitude) * 111320 * System.Math.Cos(latRad) / metersPerUnit);
                _tileOffsetZ = (float)((_tileCenterLat - centerLatitude) * 110540 / metersPerUnit);
                
                // Find tiles to remove (too far from new center)
                int halfGrid = gridSize / 2;
                var tilesToRemove = new List<string>();
                
                foreach (var kvp in _tiles)
                {
                    int tileDx = kvp.Value.TileX - _centerTileX;
                    int tileDy = kvp.Value.TileY - _centerTileY;
                    
                    if (System.Math.Abs(tileDx) > halfGrid || System.Math.Abs(tileDy) > halfGrid)
                    {
                        tilesToRemove.Add(kvp.Key);
                    }
                }
                
                // Remove distant tiles
                foreach (var key in tilesToRemove)
                {
                    if (_tiles.TryGetValue(key, out var tile))
                    {
                        if (tile.GameObject != null) Destroy(tile.GameObject);
                        if (tile.Texture != null) Destroy(tile.Texture);
                        if (tile.Material != null) Destroy(tile.Material);
                        _tiles.Remove(key);
                    }
                }
                
                if (tilesToRemove.Count > 0)
                {
                    Debug.Log($"[Mapbox] Unloaded {tilesToRemove.Count} distant tiles");
                }
                
                // Create new tiles that are needed
                int tilesCreated = 0;
                for (int localY = -halfGrid; localY <= halfGrid; localY++)
                {
                    for (int localX = -halfGrid; localX <= halfGrid; localX++)
                    {
                        int tileX = _centerTileX + localX;
                        int tileY = _centerTileY + localY;
                        string key = $"{zoomLevel}/{tileX}/{tileY}";
                        
                        if (!_tiles.ContainsKey(key))
                        {
                            CreateTileAt(tileX, tileY, localX, localY);
                            tilesCreated++;
                            
                            // Limit concurrent loads
                            if (_currentLoadingCount >= maxConcurrentLoads)
                            {
                                yield return null;
                            }
                        }
                    }
                }
                
                if (tilesCreated > 0)
                {
                    Debug.Log($"[Mapbox] Created {tilesCreated} new tiles");
                }
                
                // Reposition all tiles relative to new center
                foreach (var kvp in _tiles)
                {
                    if (kvp.Value.GameObject != null)
                    {
                        int localX = kvp.Value.TileX - _centerTileX;
                        int localY = kvp.Value.TileY - _centerTileY;
                        kvp.Value.GameObject.transform.localPosition = new Vector3(
                            localX * tileWorldSize + _tileOffsetX,
                            groundHeight,
                            -localY * tileWorldSize + _tileOffsetZ
                        );
                    }
                }
            }
            
            _isStreaming = false;
        }
        
        /// <summary>
        /// Create a tile at specific tile coordinates
        /// </summary>
        private void CreateTileAt(int tileX, int tileY, int localX, int localY)
        {
            string key = $"{zoomLevel}/{tileX}/{tileY}";
            if (_tiles.ContainsKey(key)) return;
            
            // Create quad
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = $"Tile_{localX}_{localY}";
            quad.transform.SetParent(_tilesContainer);
            
            // Position tile
            quad.transform.localPosition = new Vector3(
                localX * tileWorldSize + _tileOffsetX,
                groundHeight,
                -localY * tileWorldSize + _tileOffsetZ
            );
            quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
            quad.transform.localScale = new Vector3(tileWorldSize, tileWorldSize, 1);
            
            // Remove collider
            var collider = quad.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            
            // Create material
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? 
                         Shader.Find("Unlit/Texture") ?? 
                         Shader.Find("Standard");
            var material = new Material(shader);
            material.color = new Color(0.15f, 0.15f, 0.18f); // Dark loading color
            
            var renderer = quad.GetComponent<Renderer>();
            renderer.material = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            
            // Store tile data
            var tileData = new TileData
            {
                GameObject = quad,
                Renderer = renderer,
                Material = material,
                TileX = tileX,
                TileY = tileY,
                IsLoading = false,
                IsLoaded = false
            };
            _tiles[key] = tileData;
            
            // Start loading texture
            StartCoroutine(LoadTileTexture(key, tileData));
        }
        
        private IEnumerator LoadTileTexture(string key, TileData tile)
        {
            if (tile.IsLoading || tile.IsLoaded) yield break;
            
            tile.IsLoading = true;
            _currentLoadingCount++;
            
            // Use the same URL format as LoadTileCoroutine (via config)
            string url = config.GetTileUrl(tile.TileX, tile.TileY, zoomLevel);
            
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();
                
                _currentLoadingCount--;
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Create texture from downloaded data
                    var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    if (texture.LoadImage(request.downloadHandler.data))
                    {
                        texture.filterMode = filterMode;
                        texture.wrapMode = TextureWrapMode.Clamp;
                        texture.anisoLevel = anisotropicLevel;
                        
                        tile.Texture = texture;
                        tile.Material.mainTexture = texture;
                        tile.Material.color = Color.white;
                        tile.IsLoaded = true;
                    }
                }
                
                tile.IsLoading = false;
            }
        }
        
        #endregion
        
        #region Initialization
        
        public void Initialize()
        {
            if (_isInitialized) return;
            
            // Create container for tiles
            if (_tilesContainer != null)
            {
                Destroy(_tilesContainer.gameObject);
            }
            
            var containerObj = new GameObject("MapTiles");
            containerObj.transform.SetParent(transform);
            containerObj.transform.localPosition = Vector3.zero;
            containerObj.transform.localRotation = Quaternion.identity;
            _tilesContainer = containerObj.transform;
            
            // Calculate tile coordinates
            CalculateCenterTile();
            
            // Create all tile planes
            CreateTileGrid();
            
            _isInitialized = true;
            
            // Start loading
            StartCoroutine(LoadAllTilesCoroutine());
        }
        
        private void CalculateCenterTile()
        {
            double n = System.Math.Pow(2, zoomLevel);
            double latRad = centerLatitude * System.Math.PI / 180.0;
            
            _centerTileX = (int)((centerLongitude + 180.0) / 360.0 * n);
            _centerTileY = (int)((1.0 - System.Math.Log(System.Math.Tan(latRad) + 1.0 / System.Math.Cos(latRad)) / System.Math.PI) / 2.0 * n);
            
            // Calculate the lat/lon of the GEOMETRIC CENTER of the center tile
            _tileCenterLon = (_centerTileX + 0.5) / n * 360.0 - 180.0;
            double tileCenterLatRad = System.Math.Atan(System.Math.Sinh(System.Math.PI * (1.0 - 2.0 * (_centerTileY + 0.5) / n)));
            _tileCenterLat = tileCenterLatRad * 180.0 / System.Math.PI;
            
            // Calculate tile offset: how far tile center is from user position
            // This offset will be applied to tile positions so user is at world (0,0,0)
            double metersPerUnit = GetMetersPerWorldUnit();
            _tileOffsetX = (float)((_tileCenterLon - centerLongitude) * 111320 * System.Math.Cos(latRad) / metersPerUnit);
            _tileOffsetZ = (float)((_tileCenterLat - centerLatitude) * 110540 / metersPerUnit);
            
            Debug.Log($"[Mapbox] Center tile: {_centerTileX}, {_centerTileY}");
            Debug.Log($"[Mapbox] Tile center lat/lon: {_tileCenterLat:F6}, {_tileCenterLon:F6} (user: {centerLatitude:F6}, {centerLongitude:F6})");
            Debug.Log($"[Mapbox] Tile offset to center on user: ({_tileOffsetX:F2}, {_tileOffsetZ:F2})");
        }
        
        private void CreateTileGrid()
        {
            int halfGrid = gridSize / 2;
            _totalCount = gridSize * gridSize;
            _loadedCount = 0;
            
            Debug.Log($"[Mapbox] Creating {_totalCount} tiles ({gridSize}x{gridSize})");
            
            for (int dy = -halfGrid; dy <= halfGrid; dy++)
            {
                for (int dx = -halfGrid; dx <= halfGrid; dx++)
                {
                    CreateTile(dx, dy);
                }
            }
        }
        
        private void CreateTile(int localX, int localY)
        {
            int tileX = _centerTileX + localX;
            int tileY = _centerTileY + localY;
            string key = $"{zoomLevel}/{tileX}/{tileY}";
            
            if (_tiles.ContainsKey(key)) return;
            
            // Create quad
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = $"Tile_{localX}_{localY}";
            quad.transform.SetParent(_tilesContainer);
            
            // Position tiles with offset so USER is at world (0,0,0)
            // The offset accounts for user not being at exact tile center
            quad.transform.localPosition = new Vector3(
                localX * tileWorldSize + _tileOffsetX,
                groundHeight,
                -localY * tileWorldSize + _tileOffsetZ
            );
            quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
            quad.transform.localScale = new Vector3(tileWorldSize, tileWorldSize, 1);
            
            // Remove collider
            var collider = quad.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            
            // Create material
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? 
                         Shader.Find("Unlit/Texture") ?? 
                         Shader.Find("Standard");
            var material = new Material(shader);
            material.color = new Color(0.15f, 0.15f, 0.18f); // Dark loading color
            
            var renderer = quad.GetComponent<Renderer>();
            renderer.material = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            
            // Store tile data
            _tiles[key] = new TileData
            {
                GameObject = quad,
                Renderer = renderer,
                Material = material,
                TileX = tileX,
                TileY = tileY,
                IsLoading = false,
                IsLoaded = false
            };
        }
        
        #endregion
        
        #region Tile Loading
        
        private IEnumerator LoadAllTilesCoroutine()
        {
            if (_isLoading) yield break;
            _isLoading = true;
            
            Debug.Log($"[Mapbox] Starting to load {_tiles.Count} tiles...");
            
            // Take a snapshot of keys to avoid collection modified exception
            var tileKeys = new List<string>(_tiles.Keys);
            
            // Load tiles sequentially to avoid overwhelming the network
            foreach (var key in tileKeys)
            {
                if (_tiles.TryGetValue(key, out var tile) && !tile.IsLoaded && !tile.IsLoading)
                {
                    yield return StartCoroutine(LoadTileCoroutine(key, tile));
                }
            }
            
            _isLoading = false;
            Debug.Log($"[Mapbox] Finished loading. {_loadedCount}/{_totalCount} tiles loaded.");
            OnMapLoaded?.Invoke();
        }
        
        private IEnumerator LoadTileCoroutine(string key, TileData tile)
        {
            tile.IsLoading = true;
            
            string url = config.GetTileUrl(tile.TileX, tile.TileY, zoomLevel);
            
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Create texture from downloaded data
                    var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    if (texture.LoadImage(request.downloadHandler.data))
                    {
                        texture.filterMode = filterMode;
                        texture.wrapMode = TextureWrapMode.Clamp;
                        texture.anisoLevel = anisotropicLevel;
                        
                        tile.Texture = texture;
                        tile.Material.mainTexture = texture;
                        tile.Material.color = Color.white;
                        tile.IsLoaded = true;
                        _loadedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"[Mapbox] Failed to decode tile: {key}");
                        Destroy(texture);
                    }
                }
                else
                {
                    // Set error color
                    tile.Material.color = new Color(0.3f, 0.1f, 0.1f);
                    Debug.LogWarning($"[Mapbox] Failed to load tile {key}: {request.error}");
                }
            }
            
            tile.IsLoading = false;
        }
        
        #endregion
        
        #region Conflict Resolution
        
        /// <summary>
        /// Destroy all systems that would conflict with Mapbox tile rendering
        /// </summary>
        private void CleanupConflictingSystems()
        {
            Debug.Log("[Mapbox] Cleaning up conflicting systems...");
            
            // Objects to destroy by name
            string[] objectsToDestroy = {
                "GroundPlane", "GridOverlay", "WorldTerrain", "TerrainMesh",
                "ProceduralTerrain", "ProceduralTerrainSystem", "FantasyTerrain",
                "GeoMapSystem", "RealWorldMap", "WaterPlane", "GridLines",
                "MapTiles", "Tiles"  // Old containers
            };
            
            foreach (var name in objectsToDestroy)
            {
                var obj = GameObject.Find(name);
                if (obj != null && obj != gameObject && !obj.transform.IsChildOf(transform))
                {
                    Debug.Log($"[Mapbox] Destroying: {name}");
                    Destroy(obj);
                }
            }
            
            // Disable WorldMapRenderer ground creation
            var worldMapRenderer = FindFirstObjectByTypeName("WorldMapRenderer");
            if (worldMapRenderer != null)
            {
                Debug.Log("[Mapbox] WorldMapRenderer found - it should skip ground plane creation");
            }
        }
        
        private MonoBehaviour FindFirstObjectByTypeName(string typeName)
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb.GetType().Name == typeName)
                    return mb;
            }
            return null;
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Set map center location
        /// </summary>
        public void SetLocation(double latitude, double longitude, int? zoom = null)
        {
            centerLatitude = latitude;
            centerLongitude = longitude;
            if (zoom.HasValue) zoomLevel = Mathf.Clamp(zoom.Value, 14, 18);
            
            // Reinitialize
            _isInitialized = false;
            
            // Clean up old tiles
            foreach (var tile in _tiles.Values)
            {
                if (tile.Texture != null) Destroy(tile.Texture);
                if (tile.Material != null) Destroy(tile.Material);
                if (tile.GameObject != null) Destroy(tile.GameObject);
            }
            _tiles.Clear();
            
            Initialize();
            
            OnLocationChanged?.Invoke(latitude, longitude);
        }
        
        /// <summary>
        /// Convert world position to lat/lon
        /// Uses the geometric center of the center tile as origin (0,0,0)
        /// </summary>
        public (double lat, double lon) WorldToLatLon(Vector3 worldPos)
        {
            double metersPerUnit = GetMetersPerWorldUnit();
            double latRad = centerLatitude * Mathf.Deg2Rad;
            
            // Use user's location as origin (consistent with LatLonToWorld)
            double lon = centerLongitude + (worldPos.x * metersPerUnit) / (111320 * System.Math.Cos(latRad));
            double lat = centerLatitude + (worldPos.z * metersPerUnit) / 110540;
            
            return (lat, lon);
        }
        
        /// <summary>
        /// Convert lat/lon to world position
        /// Uses the USER'S LOCATION as origin (0,0,0) so player is centered
        /// </summary>
        public Vector3 LatLonToWorld(double latitude, double longitude)
        {
            double metersPerUnit = GetMetersPerWorldUnit();
            double latRad = centerLatitude * Mathf.Deg2Rad;
            
            // Calculate relative to USER'S LOCATION (centerLatitude/centerLongitude)
            // This ensures the player (at world 0,0,0) is at their requested location
            float x = (float)((longitude - centerLongitude) * 111320 * System.Math.Cos(latRad) / metersPerUnit);
            float z = (float)((latitude - centerLatitude) * 110540 / metersPerUnit);
            
            return new Vector3(x, 0, z);
        }
        
        private double GetMetersPerWorldUnit()
        {
            double tileMeters = 40075016.686 * System.Math.Cos(centerLatitude * System.Math.PI / 180) / System.Math.Pow(2, zoomLevel);
            return tileMeters / tileWorldSize;
        }
        
        /// <summary>
        /// Refresh tiles (reload all)
        /// </summary>
        public void RefreshTiles()
        {
            SetLocation(centerLatitude, centerLongitude, zoomLevel);
        }
        
        /// <summary>
        /// Pan the map
        /// </summary>
        public void Pan(Vector3 delta)
        {
            double metersPerUnit = GetMetersPerWorldUnit();
            double latRad = centerLatitude * Mathf.Deg2Rad;
            
            double lonDelta = (delta.x * metersPerUnit) / (111320 * System.Math.Cos(latRad));
            double latDelta = (delta.z * metersPerUnit) / 110540;
            
            SetLocation(centerLatitude + latDelta, centerLongitude + lonDelta);
        }
        
        /// <summary>
        /// Zoom in or out
        /// </summary>
        public void Zoom(int delta)
        {
            SetLocation(centerLatitude, centerLongitude, zoomLevel + delta);
        }
        
        /// <summary>
        /// Set center location without reloading all tiles
        /// </summary>
        public void SetCenter(double latitude, double longitude)
        {
            centerLatitude = latitude;
            centerLongitude = longitude;
            OnLocationChanged?.Invoke(latitude, longitude);
        }
        
        /// <summary>
        /// Smoothly update center and stream new tiles as needed
        /// </summary>
        public void UpdateCenterSmooth(double latitude, double longitude)
        {
            // Calculate how far we've moved from the current center
            double latDiff = latitude - centerLatitude;
            double lonDiff = longitude - centerLongitude;
            
            // Convert to approximate meters
            double metersLat = latDiff * 110540;
            double metersLon = lonDiff * 111320 * System.Math.Cos(latitude * System.Math.PI / 180);
            double distance = System.Math.Sqrt(metersLat * metersLat + metersLon * metersLon);
            
            // If we've moved more than half a tile, recenter
            double tileSizeMeters = 40075016.686 * System.Math.Cos(latitude * System.Math.PI / 180) / System.Math.Pow(2, zoomLevel);
            
            if (distance > tileSizeMeters * 0.5)
            {
                // Significant movement - reload tiles
                SetLocation(latitude, longitude, zoomLevel);
            }
            else
            {
                // Small movement - just shift the container
                centerLatitude = latitude;
                centerLongitude = longitude;
                
                // Offset tiles to create illusion of movement
                if (_tilesContainer != null)
                {
                    float offsetX = (float)(lonDiff * 111320 * System.Math.Cos(latitude * System.Math.PI / 180) / GetMetersPerWorldUnit());
                    float offsetZ = (float)(latDiff * 110540 / GetMetersPerWorldUnit());
                    // Keep tiles centered, we move the camera not the tiles
                }
                
                OnLocationChanged?.Invoke(latitude, longitude);
            }
        }
        
        #endregion
    }
}
