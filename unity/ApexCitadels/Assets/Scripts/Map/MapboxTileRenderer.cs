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
        [SerializeField] private int gridSize = 11;           // 11x11 = 121 tiles
        [SerializeField] private float tileWorldSize = 80f;   // World units per tile
        [SerializeField] private float groundHeight = -0.5f;  // Y position of tiles
        
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
        private bool _isInitialized;
        private bool _isLoading;
        private int _loadedCount;
        private int _totalCount;
        
        // Events
        public System.Action OnMapLoaded;
        public System.Action<double, double> OnLocationChanged;
        public bool IsLoading => _isLoading;
        public float LoadProgress => _totalCount > 0 ? (float)_loadedCount / _totalCount : 0f;
        
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
            
            // Load config
            if (config == null)
            {
                config = Resources.Load<MapboxConfiguration>("MapboxConfig");
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
            
            Debug.Log($"[Mapbox] Center tile: {_centerTileX}, {_centerTileY}");
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
            
            // Position and rotate
            quad.transform.localPosition = new Vector3(
                localX * tileWorldSize,
                groundHeight,
                -localY * tileWorldSize
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
            
            // Load tiles sequentially to avoid overwhelming the network
            foreach (var kvp in _tiles)
            {
                if (!kvp.Value.IsLoaded && !kvp.Value.IsLoading)
                {
                    yield return StartCoroutine(LoadTileCoroutine(kvp.Key, kvp.Value));
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
            var worldMapRenderer = FindFirstObjectByType<PC.WorldMapRenderer>();
            if (worldMapRenderer != null)
            {
                Debug.Log("[Mapbox] WorldMapRenderer found - it should skip ground plane creation");
            }
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
        /// </summary>
        public (double lat, double lon) WorldToLatLon(Vector3 worldPos)
        {
            double metersPerUnit = GetMetersPerWorldUnit();
            double latRad = centerLatitude * Mathf.Deg2Rad;
            
            double lon = centerLongitude + (worldPos.x * metersPerUnit) / (111320 * System.Math.Cos(latRad));
            double lat = centerLatitude + (worldPos.z * metersPerUnit) / 110540;
            
            return (lat, lon);
        }
        
        /// <summary>
        /// Convert lat/lon to world position
        /// </summary>
        public Vector3 LatLonToWorld(double latitude, double longitude)
        {
            double metersPerUnit = GetMetersPerWorldUnit();
            double latRad = centerLatitude * Mathf.Deg2Rad;
            
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
        
        #endregion
    }
}
