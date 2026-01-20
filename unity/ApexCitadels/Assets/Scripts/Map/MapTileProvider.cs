// ============================================================================
// APEX CITADELS - AAA MAP TILE PROVIDER
// Unified map tile system combining the best features from all implementations.
// This is the SINGLE source of truth for map tile fetching in the game.
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using ApexCitadels.Core;

namespace ApexCitadels.Map
{
    #region Enums
    
    /// <summary>
    /// Supported map tile providers.
    /// Includes both free (no API key) and paid options.
    /// </summary>
    public enum MapProvider
    {
        // Free providers (no API key required)
        OpenStreetMap,
        OpenStreetMapDE,
        CartoDBPositron,
        CartoDBDarkMatter,
        CartoDBVoyager,
        StamenTerrain,
        StamenWatercolor,
        EsriWorldImagery,
        EsriWorldStreetMap,
        OpenTopoMap,
        
        // API key required
        Mapbox,
        GoogleMaps,
        MapTiler,
        
        // Custom server
        Custom
    }
    
    /// <summary>
    /// Map style presets.
    /// </summary>
    public enum MapStyle
    {
        Streets,
        Satellite,
        Hybrid,
        Dark,
        Light,
        Terrain,
        Fantasy,
        GameOverlay
    }
    
    #endregion
    
    #region Data Classes
    
    /// <summary>
    /// Tile coordinate in Web Mercator projection.
    /// </summary>
    [Serializable]
    public struct TileCoordinate
    {
        public int X;
        public int Y;
        public int Zoom;
        
        public TileCoordinate(int x, int y, int zoom)
        {
            X = x;
            Y = y;
            Zoom = zoom;
        }
        
        public string Key => $"{Zoom}/{X}/{Y}";
        
        public override string ToString() => $"Tile({X},{Y}@z{Zoom})";
        
        /// <summary>
        /// Create tile coordinate from geographic coordinates.
        /// </summary>
        public static TileCoordinate FromLatLon(double lat, double lon, int zoom)
        {
            int n = 1 << zoom;
            int x = (int)Math.Floor((lon + 180.0) / 360.0 * n);
            double latRad = lat * Math.PI / 180.0;
            int y = (int)Math.Floor((1.0 - Math.Asinh(Math.Tan(latRad)) / Math.PI) / 2.0 * n);
            
            // Clamp to valid range
            x = Math.Max(0, Math.Min(n - 1, x));
            y = Math.Max(0, Math.Min(n - 1, y));
            
            return new TileCoordinate(x, y, zoom);
        }
        
        /// <summary>
        /// Get the center lat/lon of this tile.
        /// </summary>
        public (double lat, double lon) GetCenter()
        {
            int n = 1 << Zoom;
            double lon = (X + 0.5) / n * 360.0 - 180.0;
            double latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2.0 * (Y + 0.5) / n)));
            double lat = latRad * 180.0 / Math.PI;
            return (lat, lon);
        }
        
        /// <summary>
        /// Get the northwest corner lat/lon.
        /// </summary>
        public (double lat, double lon) GetNorthWest()
        {
            int n = 1 << Zoom;
            double lon = (double)X / n * 360.0 - 180.0;
            double latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2.0 * Y / n)));
            double lat = latRad * 180.0 / Math.PI;
            return (lat, lon);
        }
    }
    
    /// <summary>
    /// Geographic coordinate (latitude, longitude).
    /// </summary>
    [Serializable]
    public struct GeoCoordinate
    {
        public double Latitude;
        public double Longitude;
        
        public GeoCoordinate(double lat, double lon)
        {
            Latitude = lat;
            Longitude = lon;
        }
        
        public override string ToString() => $"({Latitude:F6}, {Longitude:F6})";
    }
    
    /// <summary>
    /// Geographic bounding box.
    /// </summary>
    [Serializable]
    public struct GeoBounds
    {
        public double North;
        public double South;
        public double East;
        public double West;
        
        public GeoCoordinate Center => new GeoCoordinate(
            (North + South) / 2,
            (East + West) / 2
        );
    }
    
    /// <summary>
    /// Cached map tile data.
    /// </summary>
    public class CachedMapTile
    {
        public TileCoordinate Coordinate;
        public Texture2D Texture;
        public DateTime FetchedAt;
        public bool IsLoading;
        public bool Failed;
        public int RetryCount;
        
        public string Key => Coordinate.Key;
        
        public bool IsExpired(float expirySeconds) =>
            (DateTime.UtcNow - FetchedAt).TotalSeconds > expirySeconds;
    }
    
    /// <summary>
    /// Configuration for map tile fetching.
    /// </summary>
    [Serializable]
    public class MapTileConfig
    {
        [Header("Provider")]
        public MapProvider Provider = MapProvider.CartoDBVoyager;
        public MapStyle Style = MapStyle.Streets;
        
        [Header("Tile Settings")]
        public int TileSize = 256;
        public int MinZoom = 1;
        public int MaxZoom = 19;
        
        [Header("Cache")]
        public int CacheMaxTiles = 500;
        public float CacheExpirySeconds = 3600f; // 1 hour
        
        [Header("Requests")]
        public float RequestTimeoutSeconds = 10f;
        public int RetryAttempts = 3;
        public float RetryDelaySeconds = 0.5f;
        
        [Header("API Keys")]
        public string MapboxAccessToken = "";
        public string GoogleMapsKey = "";
        public string MapTilerKey = "";
        public string CustomUrlTemplate = "";
    }
    
    #endregion
    
    /// <summary>
    /// AAA Map Tile Provider - Central map tile management for Apex Citadels.
    /// 
    /// Features:
    /// - 14 providers including 10 free options
    /// - Async/await tile fetching with retry logic
    /// - LRU cache with expiry
    /// - Offline cache stubs for future implementation
    /// - Full coordinate conversion utilities
    /// - Statistics tracking
    /// - Provider/style change events
    /// </summary>
    public class MapTileProvider : MonoBehaviour
    {
        #region Singleton
        
        private static MapTileProvider _instance;
        public static MapTileProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<MapTileProvider>();
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Inspector Fields
        
        [Header("Configuration")]
        [SerializeField] private MapTileConfig config = new MapTileConfig();
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        #endregion
        
        #region State
        
        private Dictionary<string, CachedMapTile> _tileCache = new Dictionary<string, CachedMapTile>();
        private Queue<string> _cacheOrder = new Queue<string>();
        private HashSet<string> _pendingRequests = new HashSet<string>();
        
        // Statistics
        private int _cacheHits;
        private int _cacheMisses;
        private int _failedRequests;
        
        #endregion
        
        #region Events
        
        public event Action<TileCoordinate, Texture2D> OnTileLoaded;
        public event Action<TileCoordinate, string> OnTileFailed;
        public event Action OnProviderChanged;
        
        #endregion
        
        #region Properties
        
        public MapProvider CurrentProvider => config.Provider;
        public MapStyle CurrentStyle => config.Style;
        public int TileSize => config.TileSize;
        public int CachedTileCount => _tileCache.Count;
        public int MaxCachedTiles => config.CacheMaxTiles;
        
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
            DontDestroyOnLoad(gameObject);
            
            LoadOfflineCache();
        }
        
        private void OnDestroy()
        {
            SaveOfflineCache();
            ClearCache();
            
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion
        
        #region Public API - Tile Access
        
        /// <summary>
        /// Get or fetch a map tile. Returns immediately if cached, otherwise starts async fetch.
        /// </summary>
        public Texture2D GetTile(TileCoordinate coord)
        {
            string key = GetCacheKey(coord);
            
            // Check cache
            if (_tileCache.TryGetValue(key, out var cached))
            {
                if (cached.Texture != null && !cached.Failed && !cached.IsExpired(config.CacheExpirySeconds))
                {
                    _cacheHits++;
                    return cached.Texture;
                }
                
                // Expired, mark for refresh
                if (cached.IsExpired(config.CacheExpirySeconds))
                {
                    _tileCache.Remove(key);
                }
            }
            
            // Not cached or expired, start fetch
            _cacheMisses++;
            FetchTileAsync(coord);
            return null;
        }
        
        /// <summary>
        /// Get tile by x, y, zoom coordinates.
        /// </summary>
        public Texture2D GetTile(int x, int y, int zoom)
        {
            return GetTile(new TileCoordinate(x, y, zoom));
        }
        
        /// <summary>
        /// Check if a tile is available in cache without fetching.
        /// </summary>
        public bool HasTile(TileCoordinate coord)
        {
            string key = GetCacheKey(coord);
            return _tileCache.TryGetValue(key, out var cached) &&
                   cached.Texture != null && !cached.Failed &&
                   !cached.IsExpired(config.CacheExpirySeconds);
        }
        
        /// <summary>
        /// Get tile with callback when loaded.
        /// </summary>
        public void GetTileAsync(TileCoordinate coord, Action<Texture2D> callback)
        {
            var texture = GetTile(coord);
            if (texture != null)
            {
                callback?.Invoke(texture);
                return;
            }
            
            // Set up one-time callback
            void OnLoaded(TileCoordinate loadedCoord, Texture2D loadedTexture)
            {
                if (loadedCoord.Key == coord.Key)
                {
                    OnTileLoaded -= OnLoaded;
                    callback?.Invoke(loadedTexture);
                }
            }
            OnTileLoaded += OnLoaded;
        }
        
        #endregion
        
        #region Public API - Preloading
        
        /// <summary>
        /// Preload tiles in a geographic area.
        /// </summary>
        public int PreloadArea(GeoBounds bounds, int zoom)
        {
            var nwTile = TileCoordinate.FromLatLon(bounds.North, bounds.West, zoom);
            var seTile = TileCoordinate.FromLatLon(bounds.South, bounds.East, zoom);
            
            int count = 0;
            for (int x = nwTile.X; x <= seTile.X; x++)
            {
                for (int y = nwTile.Y; y <= seTile.Y; y++)
                {
                    var coord = new TileCoordinate(x, y, zoom);
                    if (!HasTile(coord))
                    {
                        FetchTileAsync(coord);
                        count++;
                    }
                }
            }
            
            if (showDebugInfo && count > 0)
            {
                ApexLogger.LogMap($"Preloading {count} tiles at zoom {zoom}");
            }
            
            return count;
        }
        
        /// <summary>
        /// Preload tiles around a center point.
        /// </summary>
        public void PreloadTiles(double centerLat, double centerLon, int zoom, int radiusTiles = 3)
        {
            var center = TileCoordinate.FromLatLon(centerLat, centerLon, zoom);
            int maxTile = (1 << zoom) - 1;
            
            for (int dx = -radiusTiles; dx <= radiusTiles; dx++)
            {
                for (int dy = -radiusTiles; dy <= radiusTiles; dy++)
                {
                    int x = center.X + dx;
                    int y = center.Y + dy;
                    
                    if (x >= 0 && x <= maxTile && y >= 0 && y <= maxTile)
                    {
                        GetTile(new TileCoordinate(x, y, zoom));
                    }
                }
            }
        }
        
        /// <summary>
        /// Download tiles for offline use (placeholder for future implementation).
        /// </summary>
        public void DownloadAreaForOffline(GeoBounds bounds, int minZoom, int maxZoom, Action<float> onProgress, Action onComplete)
        {
            // TODO: Implement persistent offline cache
            ApexLogger.Log("[MapTileProvider] Offline download not yet implemented", ApexLogger.LogCategory.Map);
            onComplete?.Invoke();
        }
        
        #endregion
        
        #region Public API - Configuration
        
        /// <summary>
        /// Set the map tile provider.
        /// </summary>
        public void SetProvider(MapProvider newProvider, string apiKey = null)
        {
            config.Provider = newProvider;
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                switch (newProvider)
                {
                    case MapProvider.Mapbox:
                        config.MapboxAccessToken = apiKey;
                        break;
                    case MapProvider.GoogleMaps:
                        config.GoogleMapsKey = apiKey;
                        break;
                    case MapProvider.MapTiler:
                        config.MapTilerKey = apiKey;
                        break;
                }
            }
            
            ClearCache();
            OnProviderChanged?.Invoke();
            ApexLogger.LogMap($"Provider set to: {newProvider}");
        }
        
        /// <summary>
        /// Set the map style.
        /// </summary>
        public void SetStyle(MapStyle newStyle)
        {
            if (config.Style == newStyle) return;
            
            config.Style = newStyle;
            ClearCache();
            OnProviderChanged?.Invoke();
            ApexLogger.LogMap($"Style set to: {newStyle}");
        }
        
        /// <summary>
        /// Configure a custom tile server.
        /// </summary>
        public void SetCustomProvider(string urlTemplate)
        {
            config.Provider = MapProvider.Custom;
            config.CustomUrlTemplate = urlTemplate;
            ClearCache();
            OnProviderChanged?.Invoke();
        }
        
        #endregion
        
        #region Public API - Cache Management
        
        /// <summary>
        /// Clear all cached tiles.
        /// </summary>
        public void ClearCache()
        {
            foreach (var cached in _tileCache.Values)
            {
                if (cached.Texture != null)
                {
                    Destroy(cached.Texture);
                }
            }
            _tileCache.Clear();
            _cacheOrder.Clear();
            _cacheHits = 0;
            _cacheMisses = 0;
            _failedRequests = 0;
            
            ApexLogger.LogMap("Cache cleared");
        }
        
        /// <summary>
        /// Get cache size in bytes (approximate).
        /// </summary>
        public long GetCacheSizeBytes()
        {
            long size = 0;
            foreach (var cached in _tileCache.Values)
            {
                if (cached.Texture != null)
                {
                    size += cached.Texture.width * cached.Texture.height * 4; // Assuming RGBA
                }
            }
            return size;
        }
        
        /// <summary>
        /// Get cache statistics string.
        /// </summary>
        public string GetStats()
        {
            float hitRate = (_cacheHits + _cacheMisses) > 0
                ? (float)_cacheHits / (_cacheHits + _cacheMisses) * 100f : 0;
            long sizeKB = GetCacheSizeBytes() / 1024;
            return $"Cache: {_tileCache.Count}/{config.CacheMaxTiles} ({sizeKB}KB) | Hit: {hitRate:F1}% | Failed: {_failedRequests}";
        }
        
        #endregion
        
        #region Tile Fetching
        
        private async void FetchTileAsync(TileCoordinate coord)
        {
            string key = GetCacheKey(coord);
            
            // Don't duplicate requests
            if (_pendingRequests.Contains(key)) return;
            _pendingRequests.Add(key);
            
            // Create or update cache entry
            CachedMapTile cached;
            if (!_tileCache.TryGetValue(key, out cached))
            {
                cached = new CachedMapTile
                {
                    Coordinate = coord,
                    IsLoading = true
                };
                _tileCache[key] = cached;
                _cacheOrder.Enqueue(key);
            }
            else
            {
                cached.IsLoading = true;
                cached.Failed = false;
            }
            
            try
            {
                await FetchWithRetry(coord, cached);
            }
            finally
            {
                _pendingRequests.Remove(key);
                EnforceCacheLimit();
            }
        }
        
        private async Task FetchWithRetry(TileCoordinate coord, CachedMapTile cached)
        {
            int attempts = 0;
            string url = GetTileUrl(coord);
            
            while (attempts < config.RetryAttempts)
            {
                try
                {
                    if (showDebugInfo && attempts == 0)
                    {
                        ApexLogger.LogVerbose($"Fetching: {coord}", ApexLogger.LogCategory.Map);
                    }
                    
                    using (var request = UnityWebRequest.Get(url))
                    {
                        request.timeout = (int)config.RequestTimeoutSeconds;
                        request.SetRequestHeader("User-Agent", "ApexCitadels/1.0 Unity");
                        
                        var operation = request.SendWebRequest();
                        while (!operation.isDone)
                        {
                            await Task.Yield();
                        }
                        
                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            var texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
                            texture.LoadImage(request.downloadHandler.data);
                            texture.wrapMode = TextureWrapMode.Clamp;
                            texture.filterMode = FilterMode.Bilinear;
                            
                            cached.Texture = texture;
                            cached.FetchedAt = DateTime.UtcNow;
                            cached.IsLoading = false;
                            cached.Failed = false;
                            cached.RetryCount = attempts;
                            
                            OnTileLoaded?.Invoke(coord, texture);
                            
                            if (showDebugInfo)
                            {
                                ApexLogger.LogVerbose($"Loaded: {coord}", ApexLogger.LogCategory.Map);
                            }
                            return;
                        }
                        
                        // Failed, retry
                        attempts++;
                        if (attempts < config.RetryAttempts)
                        {
                            await Task.Delay((int)(config.RetryDelaySeconds * attempts * 1000));
                        }
                    }
                }
                catch (Exception ex)
                {
                    attempts++;
                    ApexLogger.LogWarning($"Fetch attempt {attempts} failed for {coord}: {ex.Message}", ApexLogger.LogCategory.Map);
                    
                    if (attempts < config.RetryAttempts)
                    {
                        await Task.Delay((int)(config.RetryDelaySeconds * attempts * 1000));
                    }
                }
            }
            
            // Failed after all retries
            cached.Failed = true;
            cached.IsLoading = false;
            _failedRequests++;
            
            OnTileFailed?.Invoke(coord, "Max retries exceeded");
            ApexLogger.LogWarning($"Failed to load {coord} after {config.RetryAttempts} attempts", ApexLogger.LogCategory.Map);
        }
        
        #endregion
        
        #region URL Generation
        
        private string GetTileUrl(TileCoordinate coord)
        {
            string subdomain = new string[] { "a", "b", "c" }[(coord.X + coord.Y) % 3];
            
            switch (config.Provider)
            {
                case MapProvider.OpenStreetMap:
                    return $"https://{subdomain}.tile.openstreetmap.org/{coord.Zoom}/{coord.X}/{coord.Y}.png";
                    
                case MapProvider.OpenStreetMapDE:
                    return $"https://{subdomain}.tile.openstreetmap.de/{coord.Zoom}/{coord.X}/{coord.Y}.png";
                    
                case MapProvider.CartoDBPositron:
                    return $"https://{subdomain}.basemaps.cartocdn.com/light_all/{coord.Zoom}/{coord.X}/{coord.Y}.png";
                    
                case MapProvider.CartoDBDarkMatter:
                    return $"https://{subdomain}.basemaps.cartocdn.com/dark_all/{coord.Zoom}/{coord.X}/{coord.Y}.png";
                    
                case MapProvider.CartoDBVoyager:
                    return $"https://{subdomain}.basemaps.cartocdn.com/rastertiles/voyager/{coord.Zoom}/{coord.X}/{coord.Y}.png";
                    
                case MapProvider.StamenTerrain:
                    return $"https://stamen-tiles-{subdomain}.a.ssl.fastly.net/terrain/{coord.Zoom}/{coord.X}/{coord.Y}.png";
                    
                case MapProvider.StamenWatercolor:
                    return $"https://stamen-tiles-{subdomain}.a.ssl.fastly.net/watercolor/{coord.Zoom}/{coord.X}/{coord.Y}.jpg";
                    
                case MapProvider.EsriWorldImagery:
                    return $"https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{coord.Zoom}/{coord.Y}/{coord.X}";
                    
                case MapProvider.EsriWorldStreetMap:
                    return $"https://server.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/{coord.Zoom}/{coord.Y}/{coord.X}";
                    
                case MapProvider.OpenTopoMap:
                    return $"https://{subdomain}.tile.opentopomap.org/{coord.Zoom}/{coord.X}/{coord.Y}.png";
                    
                case MapProvider.Mapbox:
                    return GetMapboxUrl(coord);
                    
                case MapProvider.GoogleMaps:
                    return GetGoogleMapsUrl(coord);
                    
                case MapProvider.MapTiler:
                    return GetMapTilerUrl(coord);
                    
                case MapProvider.Custom:
                    return GetCustomUrl(coord, subdomain);
                    
                default:
                    return $"https://{subdomain}.tile.openstreetmap.org/{coord.Zoom}/{coord.X}/{coord.Y}.png";
            }
        }
        
        private string GetMapboxUrl(TileCoordinate coord)
        {
            if (string.IsNullOrEmpty(config.MapboxAccessToken))
            {
                ApexLogger.LogError("Mapbox requires an access token!", ApexLogger.LogCategory.Map);
                return "";
            }
            
            string styleId = config.Style switch
            {
                MapStyle.Satellite => "satellite-v9",
                MapStyle.Hybrid => "satellite-streets-v12",
                MapStyle.Light => "light-v11",
                MapStyle.Dark => "dark-v11",
                MapStyle.Terrain => "outdoors-v12",
                _ => "streets-v12"
            };
            
            return $"https://api.mapbox.com/styles/v1/mapbox/{styleId}/tiles/{config.TileSize}/{coord.Zoom}/{coord.X}/{coord.Y}@2x?access_token={config.MapboxAccessToken}";
        }
        
        private string GetGoogleMapsUrl(TileCoordinate coord)
        {
            if (string.IsNullOrEmpty(config.GoogleMapsKey))
            {
                ApexLogger.LogWarning("Google Maps requires an API key", ApexLogger.LogCategory.Map);
                return GetTileUrl(new TileCoordinate(coord.X, coord.Y, coord.Zoom)); // Fallback
            }
            
            string mapType = config.Style switch
            {
                MapStyle.Satellite => "satellite",
                MapStyle.Hybrid => "hybrid",
                MapStyle.Terrain => "terrain",
                _ => "roadmap"
            };
            
            return $"https://maps.googleapis.com/maps/api/staticmap?center=0,0&zoom={coord.Zoom}&size={config.TileSize}x{config.TileSize}&maptype={mapType}&key={config.GoogleMapsKey}";
        }
        
        private string GetMapTilerUrl(TileCoordinate coord)
        {
            if (string.IsNullOrEmpty(config.MapTilerKey))
            {
                ApexLogger.LogWarning("MapTiler requires an API key", ApexLogger.LogCategory.Map);
                return "";
            }
            
            string styleId = config.Style switch
            {
                MapStyle.Satellite => "satellite",
                MapStyle.Hybrid => "hybrid",
                MapStyle.Dark => "streets-v2-dark",
                MapStyle.Light => "streets-v2-light",
                MapStyle.Terrain => "outdoor-v2",
                _ => "streets-v2"
            };
            
            return $"https://api.maptiler.com/maps/{styleId}/{coord.Zoom}/{coord.X}/{coord.Y}.png?key={config.MapTilerKey}";
        }
        
        private string GetCustomUrl(TileCoordinate coord, string subdomain)
        {
            if (string.IsNullOrEmpty(config.CustomUrlTemplate))
            {
                return $"https://{subdomain}.tile.openstreetmap.org/{coord.Zoom}/{coord.X}/{coord.Y}.png";
            }
            
            return config.CustomUrlTemplate
                .Replace("{z}", coord.Zoom.ToString())
                .Replace("{zoom}", coord.Zoom.ToString())
                .Replace("{x}", coord.X.ToString())
                .Replace("{y}", coord.Y.ToString())
                .Replace("{s}", subdomain);
        }
        
        #endregion
        
        #region Cache Management
        
        private string GetCacheKey(TileCoordinate coord)
        {
            return $"{config.Provider}_{config.Style}_{coord.Zoom}_{coord.X}_{coord.Y}";
        }
        
        private void EnforceCacheLimit()
        {
            while (_tileCache.Count > config.CacheMaxTiles && _cacheOrder.Count > 0)
            {
                string oldestKey = _cacheOrder.Dequeue();
                if (_tileCache.TryGetValue(oldestKey, out var cached))
                {
                    if (cached.Texture != null)
                    {
                        Destroy(cached.Texture);
                    }
                    _tileCache.Remove(oldestKey);
                }
            }
        }
        
        #endregion
        
        #region Offline Cache (Stubs)
        
        private void LoadOfflineCache()
        {
            // TODO: Load tiles from persistent storage
            if (showDebugInfo)
            {
                ApexLogger.Log("[MapTileProvider] Offline cache loading not yet implemented", ApexLogger.LogCategory.Map);
            }
        }
        
        private void SaveOfflineCache()
        {
            // TODO: Save tiles to persistent storage
            if (showDebugInfo)
            {
                ApexLogger.Log("[MapTileProvider] Offline cache saving not yet implemented", ApexLogger.LogCategory.Map);
            }
        }
        
        #endregion
        
        #region Static Coordinate Utilities
        
        /// <summary>
        /// Convert lat/lon to tile coordinates.
        /// </summary>
        public static (int x, int y) LatLonToTile(double lat, double lon, int zoom)
        {
            var coord = TileCoordinate.FromLatLon(lat, lon, zoom);
            return (coord.X, coord.Y);
        }
        
        /// <summary>
        /// Convert tile coordinates to lat/lon (center of tile).
        /// </summary>
        public static (double lat, double lon) TileToLatLon(int x, int y, int zoom)
        {
            var coord = new TileCoordinate(x, y, zoom);
            return coord.GetCenter();
        }
        
        /// <summary>
        /// Convert lat/lon to pixel coordinates at a given zoom.
        /// </summary>
        public static (double px, double py) LatLonToPixel(double lat, double lon, int zoom, int tileSize = 256)
        {
            int n = 1 << zoom;
            double x = (lon + 180.0) / 360.0 * n * tileSize;
            double latRad = lat * Math.PI / 180.0;
            double y = (1.0 - Math.Asinh(Math.Tan(latRad)) / Math.PI) / 2.0 * n * tileSize;
            return (x, y);
        }
        
        /// <summary>
        /// Convert pixel coordinates to lat/lon at a given zoom.
        /// </summary>
        public static (double lat, double lon) PixelToLatLon(double px, double py, int zoom, int tileSize = 256)
        {
            int n = 1 << zoom;
            double lon = px / (n * tileSize) * 360.0 - 180.0;
            double latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2.0 * py / (n * tileSize))));
            double lat = latRad * 180.0 / Math.PI;
            return (lat, lon);
        }
        
        /// <summary>
        /// Get meters per pixel at a given latitude and zoom.
        /// </summary>
        public static double GetMetersPerPixel(double lat, int zoom, int tileSize = 256)
        {
            const double EarthCircumference = 40075016.686;
            return EarthCircumference * Math.Cos(lat * Math.PI / 180.0) / (tileSize * (1 << zoom));
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Print Cache Stats")]
        private void PrintCacheStats()
        {
            ApexLogger.LogMap(GetStats());
        }
        
        [ContextMenu("Clear Cache")]
        private void ClearCacheDebug()
        {
            ClearCache();
        }
        
        #endregion
    }
}
