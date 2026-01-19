// ============================================================================
// APEX CITADELS - MAP TILE PROVIDER SYSTEM
// Fetches real-world map tiles from OpenStreetMap, Mapbox, or other providers
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// Supported map tile providers
    /// </summary>
    public enum MapProvider
    {
        OpenStreetMap,      // Free, no API key needed, but has usage limits
        OpenStreetMapDE,    // German OSM servers (good alternative)
        CartoDBPositron,    // Light minimal style (free tier)
        CartoDBDarkMatter,  // Dark minimal style (free tier)
        CartoDBVoyager,     // Colorful detailed style (free tier)
        StamenTerrain,      // Terrain with shaded relief
        StamenWatercolor,   // Artistic watercolor style
        EsriWorldImagery,   // Satellite imagery
        EsriWorldStreetMap, // Detailed street map
        Mapbox,             // Requires API key
        Custom              // User-defined URL template
    }

    /// <summary>
    /// Map style options
    /// </summary>
    public enum MapStyle
    {
        Streets,        // Standard street map
        Satellite,      // Satellite/aerial imagery
        Hybrid,         // Satellite with labels
        Terrain,        // Topographic/relief
        Light,          // Minimal light theme
        Dark,           // Minimal dark theme
        Fantasy         // Custom fantasy overlay style
    }

    /// <summary>
    /// Configuration for map tile fetching
    /// </summary>
    [Serializable]
    public class MapTileConfig
    {
        public MapProvider Provider = MapProvider.CartoDBVoyager;
        public MapStyle Style = MapStyle.Streets;
        public int TileSize = 256;
        public int MinZoom = 1;
        public int MaxZoom = 19;
        public int CacheMaxTiles = 500;
        public float RequestTimeoutSeconds = 10f;
        
        [Header("API Keys (if required)")]
        public string MapboxAccessToken = "";
        public string CustomUrlTemplate = "";
    }

    /// <summary>
    /// Cached map tile data
    /// </summary>
    public class CachedMapTile
    {
        public TileCoordinate Coordinate;
        public Texture2D Texture;
        public DateTime FetchedAt;
        public bool IsLoading;
        public bool Failed;
    }

    /// <summary>
    /// Fetches and caches map tiles from various providers.
    /// Supports OpenStreetMap, CartoDB, Esri, Mapbox, and custom providers.
    /// </summary>
    public class MapTileProvider : MonoBehaviour
    {
        public static MapTileProvider Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private MapTileConfig config = new MapTileConfig();

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // Tile cache
        private Dictionary<string, CachedMapTile> _tileCache = new Dictionary<string, CachedMapTile>();
        private Queue<string> _cacheOrder = new Queue<string>(); // For LRU eviction
        private HashSet<string> _pendingRequests = new HashSet<string>();

        // Events
        public event Action<TileCoordinate, Texture2D> OnTileLoaded;
        public event Action<TileCoordinate, string> OnTileFailed;

        // Statistics
        private int _cacheHits = 0;
        private int _cacheMisses = 0;
        private int _failedRequests = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Get or fetch a map tile. Returns immediately if cached, otherwise starts async fetch.
        /// </summary>
        public Texture2D GetTile(TileCoordinate coord)
        {
            string key = GetCacheKey(coord);

            // Check cache first
            if (_tileCache.TryGetValue(key, out var cached))
            {
                if (cached.Texture != null && !cached.Failed)
                {
                    _cacheHits++;
                    return cached.Texture;
                }
            }

            // Not cached, start fetch
            _cacheMisses++;
            FetchTileAsync(coord);
            return null;
        }

        /// <summary>
        /// Check if a tile is available in cache without fetching
        /// </summary>
        public bool HasTile(TileCoordinate coord)
        {
            string key = GetCacheKey(coord);
            return _tileCache.TryGetValue(key, out var cached) && 
                   cached.Texture != null && !cached.Failed;
        }

        /// <summary>
        /// Preload tiles in a geographic area
        /// </summary>
        public void PreloadArea(GeoBounds bounds, int zoom)
        {
            var nwTile = TileCoordinate.FromGeo(new GeoCoordinate(bounds.North, bounds.West), zoom);
            var seTile = TileCoordinate.FromGeo(new GeoCoordinate(bounds.South, bounds.East), zoom);

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

            if (showDebugInfo)
            {
                Debug.Log($"[MapTileProvider] Preloading {count} tiles at zoom {zoom}");
            }
        }

        /// <summary>
        /// Clear all cached tiles
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
            Debug.Log("[MapTileProvider] Cache cleared");
        }

        /// <summary>
        /// Get statistics about tile cache
        /// </summary>
        public string GetStats()
        {
            float hitRate = (_cacheHits + _cacheMisses) > 0 
                ? (float)_cacheHits / (_cacheHits + _cacheMisses) * 100f : 0;
            return $"Cache: {_tileCache.Count}/{config.CacheMaxTiles} | Hit rate: {hitRate:F1}% | Failed: {_failedRequests}";
        }

        private async void FetchTileAsync(TileCoordinate coord)
        {
            string key = GetCacheKey(coord);
            
            // Don't duplicate requests
            if (_pendingRequests.Contains(key)) return;
            _pendingRequests.Add(key);

            // Create cache entry
            if (!_tileCache.ContainsKey(key))
            {
                _tileCache[key] = new CachedMapTile 
                { 
                    Coordinate = coord, 
                    IsLoading = true 
                };
                _cacheOrder.Enqueue(key);
            }
            else
            {
                _tileCache[key].IsLoading = true;
            }

            try
            {
                string url = GetTileUrl(coord);
                if (showDebugInfo)
                {
                    Debug.Log($"[MapTileProvider] Fetching: {url}");
                }

                using (var request = UnityWebRequest.Get(url))
                {
                    var downloadHandler = new DownloadHandlerTexture(true);
                    request.downloadHandler = downloadHandler;
                    request.timeout = (int)config.RequestTimeoutSeconds;
                    
                    // Add headers to be a good citizen
                    request.SetRequestHeader("User-Agent", "ApexCitadels/1.0 Unity");

                    var operation = request.SendWebRequest();
                    
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var texture = downloadHandler.texture;
                        texture.wrapMode = TextureWrapMode.Clamp;
                        texture.filterMode = FilterMode.Bilinear;

                        _tileCache[key].Texture = texture;
                        _tileCache[key].FetchedAt = DateTime.UtcNow;
                        _tileCache[key].IsLoading = false;
                        _tileCache[key].Failed = false;

                        OnTileLoaded?.Invoke(coord, texture);

                        if (showDebugInfo)
                        {
                            Debug.Log($"[MapTileProvider] Loaded tile {coord}");
                        }
                    }
                    else
                    {
                        _tileCache[key].Failed = true;
                        _tileCache[key].IsLoading = false;
                        _failedRequests++;
                        
                        OnTileFailed?.Invoke(coord, request.error);
                        Debug.LogWarning($"[MapTileProvider] Failed to load {coord}: {request.error}");
                    }
                }
            }
            catch (Exception ex)
            {
                _tileCache[key].Failed = true;
                _tileCache[key].IsLoading = false;
                _failedRequests++;
                
                OnTileFailed?.Invoke(coord, ex.Message);
                Debug.LogError($"[MapTileProvider] Exception loading {coord}: {ex.Message}");
            }
            finally
            {
                _pendingRequests.Remove(key);
                EnforceCacheLimit();
            }
        }

        private string GetTileUrl(TileCoordinate coord)
        {
            // Use different subdomains for load balancing (a, b, c)
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

                case MapProvider.Mapbox:
                    if (string.IsNullOrEmpty(config.MapboxAccessToken))
                    {
                        Debug.LogError("[MapTileProvider] Mapbox requires an access token!");
                        return "";
                    }
                    string mapboxStyle = config.Style switch
                    {
                        MapStyle.Satellite => "satellite-v9",
                        MapStyle.Hybrid => "satellite-streets-v12",
                        MapStyle.Light => "light-v11",
                        MapStyle.Dark => "dark-v11",
                        MapStyle.Terrain => "outdoors-v12",
                        _ => "streets-v12"
                    };
                    return $"https://api.mapbox.com/styles/v1/mapbox/{mapboxStyle}/tiles/{config.TileSize}/{coord.Zoom}/{coord.X}/{coord.Y}@2x?access_token={config.MapboxAccessToken}";

                case MapProvider.Custom:
                    return config.CustomUrlTemplate
                        .Replace("{z}", coord.Zoom.ToString())
                        .Replace("{x}", coord.X.ToString())
                        .Replace("{y}", coord.Y.ToString())
                        .Replace("{s}", subdomain);

                default:
                    return $"https://{subdomain}.tile.openstreetmap.org/{coord.Zoom}/{coord.X}/{coord.Y}.png";
            }
        }

        private string GetCacheKey(TileCoordinate coord)
        {
            return $"{config.Provider}_{coord.Zoom}_{coord.X}_{coord.Y}";
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

        private void OnDestroy()
        {
            ClearCache();
        }

        #region Editor/Debug

        [ContextMenu("Print Cache Stats")]
        private void PrintCacheStats()
        {
            Debug.Log($"[MapTileProvider] {GetStats()}");
        }

        [ContextMenu("Clear Cache")]
        private void ClearCacheDebug()
        {
            ClearCache();
        }

        #endregion
    }
}
