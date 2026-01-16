using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ApexCitadels.Map
{
    /// <summary>
    /// Supported map tile providers
    /// </summary>
    public enum MapProvider
    {
        OpenStreetMap,      // Free, no API key required
        Mapbox,             // Customizable, requires API key
        GoogleMaps,         // Best quality, requires API key
        MapTiler,           // Good alternative, requires API key
        Custom              // Custom tile server
    }

    /// <summary>
    /// Map style presets
    /// </summary>
    public enum MapStyle
    {
        Streets,            // Default street map
        Satellite,          // Satellite imagery
        Hybrid,             // Satellite with labels
        Dark,               // Dark theme for gaming
        Light,              // Light minimal theme
        Terrain,            // Topographic
        GameOverlay         // Custom game style
    }

    /// <summary>
    /// Represents a single map tile
    /// </summary>
    public class MapTile
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Zoom { get; set; }
        public Texture2D Texture { get; set; }
        public bool IsLoading { get; set; }
        public bool HasError { get; set; }
        public DateTime LoadedAt { get; set; }

        public string Key => $"{Zoom}/{X}/{Y}";

        public MapTile(int x, int y, int zoom)
        {
            X = x;
            Y = y;
            Zoom = zoom;
        }
    }

    /// <summary>
    /// Provides map tiles from various sources with caching and offline support
    /// </summary>
    public class MapTileProvider : MonoBehaviour
    {
        public static MapTileProvider Instance { get; private set; }

        [Header("Provider Settings")]
        [SerializeField] private MapProvider provider = MapProvider.OpenStreetMap;
        [SerializeField] private MapStyle style = MapStyle.Dark;
        [SerializeField] private string apiKey = "";
        [SerializeField] private string customTileUrl = "";

        [Header("Tile Settings")]
        [SerializeField] private int tileSize = 256;
        [SerializeField] private int maxCachedTiles = 200;
        [SerializeField] private float tileCacheExpiry = 3600f; // 1 hour
        [SerializeField] private int maxConcurrentDownloads = 4;
        [SerializeField] private int retryAttempts = 3;

        [Header("Offline Support")]
        [SerializeField] private bool enableOfflineCache = true;
        [SerializeField] private int offlineCacheMaxMB = 100;

        // Tile cache
        private Dictionary<string, MapTile> _tileCache = new Dictionary<string, MapTile>();
        private Queue<string> _tileCacheOrder = new Queue<string>();
        private Dictionary<string, Coroutine> _loadingTiles = new Dictionary<string, Coroutine>();

        // Events
        public event Action<MapTile> OnTileLoaded;
        public event Action<string, string> OnTileError;
        public event Action OnProviderChanged;

        // Properties
        public MapProvider CurrentProvider => provider;
        public MapStyle CurrentStyle => style;
        public int TileSize => tileSize;
        public int CachedTileCount => _tileCache.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoadOfflineCache();
        }

        private void OnDestroy()
        {
            SaveOfflineCache();
        }

        #region Provider Configuration

        /// <summary>
        /// Set the map tile provider
        /// </summary>
        public void SetProvider(MapProvider newProvider, string newApiKey = null)
        {
            provider = newProvider;
            if (!string.IsNullOrEmpty(newApiKey))
            {
                apiKey = newApiKey;
            }

            // Clear cache when provider changes
            ClearCache();
            OnProviderChanged?.Invoke();

            Debug.Log($"[MapTileProvider] Provider set to: {provider}");
        }

        /// <summary>
        /// Set the map style
        /// </summary>
        public void SetStyle(MapStyle newStyle)
        {
            if (style == newStyle) return;

            style = newStyle;
            ClearCache();
            OnProviderChanged?.Invoke();

            Debug.Log($"[MapTileProvider] Style set to: {style}");
        }

        /// <summary>
        /// Configure a custom tile server
        /// </summary>
        public void SetCustomProvider(string tileUrlTemplate)
        {
            provider = MapProvider.Custom;
            customTileUrl = tileUrlTemplate;
            ClearCache();
            OnProviderChanged?.Invoke();
        }

        #endregion

        #region Tile URL Generation

        /// <summary>
        /// Get the tile URL for a specific provider and tile coordinates
        /// </summary>
        public string GetTileUrl(int x, int y, int zoom)
        {
            return provider switch
            {
                MapProvider.OpenStreetMap => GetOpenStreetMapUrl(x, y, zoom),
                MapProvider.Mapbox => GetMapboxUrl(x, y, zoom),
                MapProvider.GoogleMaps => GetGoogleMapsUrl(x, y, zoom),
                MapProvider.MapTiler => GetMapTilerUrl(x, y, zoom),
                MapProvider.Custom => GetCustomUrl(x, y, zoom),
                _ => GetOpenStreetMapUrl(x, y, zoom)
            };
        }

        private string GetOpenStreetMapUrl(int x, int y, int zoom)
        {
            // OSM has multiple tile servers (a, b, c) for load balancing
            char server = (char)('a' + (x + y) % 3);
            
            string stylePrefix = style switch
            {
                MapStyle.Streets => "tile.openstreetmap.org",
                MapStyle.Dark => "tiles.stadiamaps.com/tiles/alidade_smooth_dark",
                MapStyle.Light => "tiles.stadiamaps.com/tiles/alidade_smooth",
                MapStyle.Terrain => "tile.opentopomap.org",
                _ => "tile.openstreetmap.org"
            };

            if (style == MapStyle.Dark || style == MapStyle.Light)
            {
                return $"https://{stylePrefix}/{zoom}/{x}/{y}.png";
            }

            return $"https://{server}.{stylePrefix}/{zoom}/{x}/{y}.png";
        }

        private string GetMapboxUrl(int x, int y, int zoom)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogWarning("[MapTileProvider] Mapbox requires an API key");
                return GetOpenStreetMapUrl(x, y, zoom);
            }

            string styleId = style switch
            {
                MapStyle.Streets => "streets-v12",
                MapStyle.Satellite => "satellite-v9",
                MapStyle.Hybrid => "satellite-streets-v12",
                MapStyle.Dark => "dark-v11",
                MapStyle.Light => "light-v11",
                MapStyle.Terrain => "outdoors-v12",
                MapStyle.GameOverlay => "dark-v11",
                _ => "streets-v12"
            };

            return $"https://api.mapbox.com/styles/v1/mapbox/{styleId}/tiles/{zoom}/{x}/{y}?access_token={apiKey}";
        }

        private string GetGoogleMapsUrl(int x, int y, int zoom)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogWarning("[MapTileProvider] Google Maps requires an API key");
                return GetOpenStreetMapUrl(x, y, zoom);
            }

            string mapType = style switch
            {
                MapStyle.Streets => "roadmap",
                MapStyle.Satellite => "satellite",
                MapStyle.Hybrid => "hybrid",
                MapStyle.Terrain => "terrain",
                _ => "roadmap"
            };

            // Google Static Maps API (note: has usage limits)
            return $"https://maps.googleapis.com/maps/api/staticmap?center=0,0&zoom={zoom}&size={tileSize}x{tileSize}&maptype={mapType}&key={apiKey}";
        }

        private string GetMapTilerUrl(int x, int y, int zoom)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogWarning("[MapTileProvider] MapTiler requires an API key");
                return GetOpenStreetMapUrl(x, y, zoom);
            }

            string styleId = style switch
            {
                MapStyle.Streets => "streets-v2",
                MapStyle.Satellite => "satellite",
                MapStyle.Hybrid => "hybrid",
                MapStyle.Dark => "streets-v2-dark",
                MapStyle.Light => "streets-v2-light",
                MapStyle.Terrain => "outdoor-v2",
                _ => "streets-v2"
            };

            return $"https://api.maptiler.com/maps/{styleId}/{zoom}/{x}/{y}.png?key={apiKey}";
        }

        private string GetCustomUrl(int x, int y, int zoom)
        {
            if (string.IsNullOrEmpty(customTileUrl))
            {
                return GetOpenStreetMapUrl(x, y, zoom);
            }

            return customTileUrl
                .Replace("{x}", x.ToString())
                .Replace("{y}", y.ToString())
                .Replace("{z}", zoom.ToString())
                .Replace("{zoom}", zoom.ToString());
        }

        #endregion

        #region Tile Loading

        /// <summary>
        /// Get a map tile (from cache or download)
        /// </summary>
        public void GetTile(int x, int y, int zoom, Action<MapTile> callback)
        {
            string key = $"{zoom}/{x}/{y}";

            // Check cache
            if (_tileCache.TryGetValue(key, out MapTile cached))
            {
                // Check if expired
                if ((DateTime.Now - cached.LoadedAt).TotalSeconds < tileCacheExpiry)
                {
                    callback?.Invoke(cached);
                    return;
                }
                // Expired, remove from cache
                _tileCache.Remove(key);
            }

            // Check if already loading
            if (_loadingTiles.ContainsKey(key))
            {
                // Wait for existing load
                StartCoroutine(WaitForTile(key, callback));
                return;
            }

            // Start loading
            var tile = new MapTile(x, y, zoom) { IsLoading = true };
            _loadingTiles[key] = StartCoroutine(LoadTileCoroutine(tile, callback));
        }

        /// <summary>
        /// Get a tile synchronously (returns null if not cached)
        /// </summary>
        public MapTile GetTileCached(int x, int y, int zoom)
        {
            string key = $"{zoom}/{x}/{y}";
            _tileCache.TryGetValue(key, out MapTile tile);
            return tile;
        }

        /// <summary>
        /// Preload tiles for an area
        /// </summary>
        public void PreloadTiles(double centerLat, double centerLon, int zoom, int radiusTiles = 3)
        {
            var (centerX, centerY) = LatLonToTile(centerLat, centerLon, zoom);

            for (int dx = -radiusTiles; dx <= radiusTiles; dx++)
            {
                for (int dy = -radiusTiles; dy <= radiusTiles; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    // Validate tile coordinates
                    int maxTile = (1 << zoom) - 1;
                    if (x >= 0 && x <= maxTile && y >= 0 && y <= maxTile)
                    {
                        GetTile(x, y, zoom, null);
                    }
                }
            }
        }

        private IEnumerator LoadTileCoroutine(MapTile tile, Action<MapTile> callback)
        {
            string url = GetTileUrl(tile.X, tile.Y, tile.Zoom);
            int attempts = 0;

            while (attempts < retryAttempts)
            {
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.downloadHandler = new DownloadHandlerBuffer();
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        // Create texture from downloaded bytes
                        Texture2D texture = new Texture2D(2, 2);
                        texture.LoadImage(request.downloadHandler.data);
                        tile.Texture = texture;
                        tile.IsLoading = false;
                        tile.LoadedAt = DateTime.Now;

                        // Add to cache
                        CacheTile(tile);

                        callback?.Invoke(tile);
                        OnTileLoaded?.Invoke(tile);

                        _loadingTiles.Remove(tile.Key);
                        yield break;
                    }

                    attempts++;
                    if (attempts < retryAttempts)
                    {
                        yield return new WaitForSeconds(0.5f * attempts);
                    }
                }
            }

            // Failed after all attempts
            tile.IsLoading = false;
            tile.HasError = true;
            
            OnTileError?.Invoke(tile.Key, $"Failed to load tile after {retryAttempts} attempts");
            _loadingTiles.Remove(tile.Key);
            
            callback?.Invoke(tile);
        }

        private IEnumerator WaitForTile(string key, Action<MapTile> callback)
        {
            while (_loadingTiles.ContainsKey(key))
            {
                yield return null;
            }

            if (_tileCache.TryGetValue(key, out MapTile tile))
            {
                callback?.Invoke(tile);
            }
        }

        #endregion

        #region Cache Management

        private void CacheTile(MapTile tile)
        {
            if (_tileCache.ContainsKey(tile.Key))
            {
                _tileCache[tile.Key] = tile;
                return;
            }

            // Enforce cache limit
            while (_tileCache.Count >= maxCachedTiles && _tileCacheOrder.Count > 0)
            {
                string oldest = _tileCacheOrder.Dequeue();
                if (_tileCache.TryGetValue(oldest, out MapTile oldTile))
                {
                    if (oldTile.Texture != null)
                    {
                        Destroy(oldTile.Texture);
                    }
                    _tileCache.Remove(oldest);
                }
            }

            _tileCache[tile.Key] = tile;
            _tileCacheOrder.Enqueue(tile.Key);
        }

        /// <summary>
        /// Clear all cached tiles
        /// </summary>
        public void ClearCache()
        {
            foreach (var tile in _tileCache.Values)
            {
                if (tile.Texture != null)
                {
                    Destroy(tile.Texture);
                }
            }
            _tileCache.Clear();
            _tileCacheOrder.Clear();

            Debug.Log("[MapTileProvider] Cache cleared");
        }

        /// <summary>
        /// Get cache size in MB (approximate)
        /// </summary>
        public float GetCacheSizeMB()
        {
            float totalBytes = 0;
            foreach (var tile in _tileCache.Values)
            {
                if (tile.Texture != null)
                {
                    totalBytes += tile.Texture.width * tile.Texture.height * 4; // RGBA
                }
            }
            return totalBytes / (1024 * 1024);
        }

        #endregion

        #region Offline Cache

        private void LoadOfflineCache()
        {
            if (!enableOfflineCache) return;

            // Load from persistent storage
            // In production, use proper file I/O or SQLite
            Debug.Log("[MapTileProvider] Offline cache loading not implemented in stub");
        }

        private void SaveOfflineCache()
        {
            if (!enableOfflineCache) return;

            // Save to persistent storage
            Debug.Log("[MapTileProvider] Offline cache saving not implemented in stub");
        }

        /// <summary>
        /// Download tiles for offline use
        /// </summary>
        public void DownloadAreaForOffline(double centerLat, double centerLon, int minZoom, int maxZoom, int radiusTiles)
        {
            StartCoroutine(DownloadAreaCoroutine(centerLat, centerLon, minZoom, maxZoom, radiusTiles));
        }

        private IEnumerator DownloadAreaCoroutine(double centerLat, double centerLon, int minZoom, int maxZoom, int radiusTiles)
        {
            int totalTiles = 0;
            int downloadedTiles = 0;

            // Calculate total tiles
            for (int zoom = minZoom; zoom <= maxZoom; zoom++)
            {
                int tilesAtZoom = (radiusTiles * 2 + 1) * (radiusTiles * 2 + 1);
                totalTiles += tilesAtZoom;
            }

            Debug.Log($"[MapTileProvider] Downloading {totalTiles} tiles for offline use");

            for (int zoom = minZoom; zoom <= maxZoom; zoom++)
            {
                var (centerX, centerY) = LatLonToTile(centerLat, centerLon, zoom);

                for (int dx = -radiusTiles; dx <= radiusTiles; dx++)
                {
                    for (int dy = -radiusTiles; dy <= radiusTiles; dy++)
                    {
                        int x = centerX + dx;
                        int y = centerY + dy;

                        int maxTile = (1 << zoom) - 1;
                        if (x >= 0 && x <= maxTile && y >= 0 && y <= maxTile)
                        {
                            bool completed = false;
                            GetTile(x, y, zoom, (tile) => completed = true);

                            while (!completed)
                            {
                                yield return null;
                            }

                            downloadedTiles++;
                        }

                        // Limit concurrent downloads
                        while (_loadingTiles.Count >= maxConcurrentDownloads)
                        {
                            yield return null;
                        }
                    }
                }
            }

            Debug.Log($"[MapTileProvider] Downloaded {downloadedTiles} tiles");
            SaveOfflineCache();
        }

        #endregion

        #region Coordinate Conversion

        /// <summary>
        /// Convert latitude/longitude to tile coordinates
        /// </summary>
        public static (int X, int Y) LatLonToTile(double lat, double lon, int zoom)
        {
            int n = 1 << zoom;
            int x = (int)((lon + 180.0) / 360.0 * n);
            int y = (int)((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) + 1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * n);

            x = Mathf.Clamp(x, 0, n - 1);
            y = Mathf.Clamp(y, 0, n - 1);

            return (x, y);
        }

        /// <summary>
        /// Convert tile coordinates to latitude/longitude (top-left corner)
        /// </summary>
        public static (double Lat, double Lon) TileToLatLon(int x, int y, int zoom)
        {
            int n = 1 << zoom;
            double lon = x / (double)n * 360.0 - 180.0;
            double latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / (double)n)));
            double lat = latRad * 180.0 / Math.PI;
            return (lat, lon);
        }

        /// <summary>
        /// Get the bounds of a tile in lat/lon
        /// </summary>
        public static (double MinLat, double MinLon, double MaxLat, double MaxLon) GetTileBounds(int x, int y, int zoom)
        {
            var (topLeftLat, topLeftLon) = TileToLatLon(x, y, zoom);
            var (bottomRightLat, bottomRightLon) = TileToLatLon(x + 1, y + 1, zoom);

            return (bottomRightLat, topLeftLon, topLeftLat, bottomRightLon);
        }

        /// <summary>
        /// Get position within a tile (0-1 normalized)
        /// </summary>
        public static (float X, float Y) GetPositionInTile(double lat, double lon, int tileX, int tileY, int zoom)
        {
            var (minLat, minLon, maxLat, maxLon) = GetTileBounds(tileX, tileY, zoom);

            float x = (float)((lon - minLon) / (maxLon - minLon));
            float y = (float)((maxLat - lat) / (maxLat - minLat));

            return (x, y);
        }

        #endregion
    }
}
