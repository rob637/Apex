using UnityEngine;

namespace ApexCitadels.Map
{
    /// <summary>
    /// ScriptableObject to store Mapbox configuration.
    /// Create via Apex > PC > Configure Mapbox API
    /// </summary>
    [CreateAssetMenu(fileName = "MapboxConfig", menuName = "Apex Citadels/Mapbox Configuration")]
    public class MapboxConfiguration : ScriptableObject
    {
        [Header("API Settings")]
        [Tooltip("Your Mapbox access token from mapbox.com")]
        public string AccessToken = "";
        
        [Header("Map Style")]
        [Tooltip("The visual style of the map")]
        public MapboxStyle Style = MapboxStyle.Streets;
        
        [Header("Default Location")]
        [Tooltip("Default latitude when no GPS is available")]
        public double DefaultLatitude = 38.9032; // Vienna Town Green, VA
        
        [Tooltip("Default longitude when no GPS is available")]
        public double DefaultLongitude = -77.2646;
        
        [Tooltip("Default zoom level (1-18, higher = more zoomed in)")]
        [Range(1, 18)]
        public int DefaultZoom = 16;  // Zoom 16 for good balance
        
        [Header("Tile Settings")]
        [Tooltip("Size of map tiles in pixels")]
        public int TileSize = 512;
        
        [Tooltip("Use high-DPI (retina) tiles")]
        public bool UseRetinaScale = true;
        
        [Header("Caching")]
        [Tooltip("Maximum number of tiles to cache in memory")]
        public int MaxCachedTiles = 100;
        
        [Tooltip("Cache tiles to disk for offline use")]
        public bool EnableDiskCache = true;
        
        /// <summary>
        /// Check if the configuration is valid
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(AccessToken) && AccessToken.StartsWith("pk.");
        
        /// <summary>
        /// Get the tile URL for a specific coordinate
        /// </summary>
        public string GetTileUrl(int x, int y, int zoom)
        {
            if (!IsValid)
            {
                Debug.LogError("Mapbox access token is not configured!");
                return null;
            }
            
            string styleId = GetStyleId();
            string scale = UseRetinaScale ? "@2x" : "";
            
            return $"https://api.mapbox.com/styles/v1/mapbox/{styleId}/tiles/{TileSize}/{zoom}/{x}/{y}{scale}?access_token={AccessToken}";
        }
        
        /// <summary>
        /// Get the static map image URL for a location
        /// </summary>
        public string GetStaticMapUrl(double lat, double lon, int zoom, int width, int height)
        {
            if (!IsValid) return null;
            
            string styleId = GetStyleId();
            string scale = UseRetinaScale ? "@2x" : "";
            
            return $"https://api.mapbox.com/styles/v1/mapbox/{styleId}/static/{lon},{lat},{zoom}/{width}x{height}{scale}?access_token={AccessToken}";
        }
        
        private string GetStyleId()
        {
            return Style switch
            {
                MapboxStyle.Streets => "streets-v12",
                MapboxStyle.Outdoors => "outdoors-v12",
                MapboxStyle.Light => "light-v11",
                MapboxStyle.Dark => "dark-v11",
                MapboxStyle.Satellite => "satellite-v9",
                MapboxStyle.SatelliteStreets => "satellite-streets-v12",
                MapboxStyle.NavigationDay => "navigation-day-v1",
                MapboxStyle.NavigationNight => "navigation-night-v1",
                _ => "dark-v11"
            };
        }
    }
    
    /// <summary>
    /// Available Mapbox map styles
    /// </summary>
    public enum MapboxStyle
    {
        Streets,            // Standard street map
        Outdoors,           // Topographic/hiking style
        Light,              // Light minimal theme
        Dark,               // Dark theme (best for gaming)
        Satellite,          // Satellite imagery only
        SatelliteStreets,   // Satellite with street labels
        NavigationDay,      // Navigation optimized (day)
        NavigationNight     // Navigation optimized (night)
    }
}
