using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ApexCitadels.Backend
{
    /// <summary>
    /// Implementation of ILocationService using Firebase Cloud Functions.
    /// Handles location-based queries like density and territory radius.
    /// </summary>
    public class LocationService : MonoBehaviour, ILocationService
    {
        public static LocationService Instance { get; private set; }

        // Cache location info to avoid repeated calls
        private Dictionary<string, LocationInfo> _locationCache = new Dictionary<string, LocationInfo>();
        private const float CACHE_DURATION_SECONDS = 300f; // 5 minutes

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

        // Interface implementation - returns tuple
        async Task<(LocationDensity density, float territoryRadius)> ILocationService.GetLocationInfoAsync(
            double latitude, double longitude)
        {
            var info = await GetLocationInfoFullAsync(latitude, longitude);
            return (info.Density, info.TerritoryRadius);
        }

        public async Task SetAreaDensityAsync(string geohash, LocationDensity density)
        {
#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("setAreaDensity");
                var data = new Dictionary<string, object>
                {
                    { "geohash", geohash },
                    { "density", density.ToString().ToLower() }
                };
                await callable.CallAsync(data);
                // Clear cache for this area
                ClearCache();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocationService] SetAreaDensity failed: {ex.Message}");
                throw;
            }
#else
            Debug.Log($"[STUB] SetAreaDensity: {geohash} -> {density}");
            await Task.Delay(100);
#endif
        }

        /// <summary>
        /// Get full location info (extended version for internal use)
        /// </summary>
        public async Task<LocationInfo> GetLocationInfoFullAsync(double latitude, double longitude)
        {
            // Create cache key from coordinates (rounded to ~100m precision)
            string cacheKey = $"{Math.Round(latitude, 3)}_{Math.Round(longitude, 3)}";
            
            if (_locationCache.TryGetValue(cacheKey, out var cached) && 
                DateTime.UtcNow < cached.CachedAt.AddSeconds(CACHE_DURATION_SECONDS))
            {
                return cached;
            }

#if FIREBASE_ENABLED
            try
            {
                var functions = Firebase.Functions.FirebaseFunctions.DefaultInstance;
                var callable = functions.GetHttpsCallable("getLocationInfo");
                var data = new Dictionary<string, object>
                {
                    { "latitude", latitude },
                    { "longitude", longitude }
                };
                var result = await callable.CallAsync(data);
                var json = result.Data.ToString();
                var info = JsonUtility.FromJson<LocationInfo>(json);
                info.CachedAt = DateTime.UtcNow;
                
                _locationCache[cacheKey] = info;
                return info;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocationService] GetLocationInfo failed: {ex.Message}");
                // Return default values on error
                return GetDefaultLocationInfo(latitude, longitude);
            }
#else
            Debug.Log($"[STUB] GetLocationInfo: ({latitude}, {longitude})");
            await Task.Delay(100);
            var info = GetDefaultLocationInfo(latitude, longitude);
            _locationCache[cacheKey] = info;
            return info;
#endif
        }

        /// <summary>
        /// Get territory radius based on location density
        /// </summary>
        public async Task<float> GetTerritoryRadiusAsync(double latitude, double longitude)
        {
            var info = await GetLocationInfoAsync(latitude, longitude);
            return info.TerritoryRadius;
        }

        /// <summary>
        /// Check if a location is in a high-density urban area
        /// </summary>
        public async Task<bool> IsUrbanAreaAsync(double latitude, double longitude)
        {
            var info = await GetLocationInfoAsync(latitude, longitude);
            return info.Density == LocationDensity.Urban;
        }

        /// <summary>
        /// Get estimated population density at location
        /// </summary>
        public async Task<LocationDensity> GetDensityAsync(double latitude, double longitude)
        {
            var info = await GetLocationInfoAsync(latitude, longitude);
            return info.Density;
        }

        /// <summary>
        /// Clear location cache (useful after long period of inactivity)
        /// </summary>
        public void ClearCache()
        {
            _locationCache.Clear();
        }

        private LocationInfo GetDefaultLocationInfo(double latitude, double longitude)
        {
            // Default to suburban density
            return new LocationInfo
            {
                Latitude = latitude,
                Longitude = longitude,
                Density = LocationDensity.Suburban,
                TerritoryRadius = 50f, // 50m for suburban
                CachedAt = DateTime.UtcNow
            };
        }

        #region Distance Calculations

        /// <summary>
        /// Calculate distance between two coordinates in meters
        /// Uses Haversine formula
        /// </summary>
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double EarthRadiusMeters = 6371000;
            
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return EarthRadiusMeters * c;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        /// <summary>
        /// Check if a point is within radius of another point
        /// </summary>
        public static bool IsWithinRadius(double lat1, double lon1, double lat2, double lon2, double radiusMeters)
        {
            return CalculateDistance(lat1, lon1, lat2, lon2) <= radiusMeters;
        }

        /// <summary>
        /// Get a geohash for coordinates (for efficient spatial queries)
        /// </summary>
        public static string GetGeohash(double latitude, double longitude, int precision = 7)
        {
            const string base32 = "0123456789bcdefghjkmnpqrstuvwxyz";
            
            double[] lat = { -90.0, 90.0 };
            double[] lon = { -180.0, 180.0 };
            
            bool isLon = true;
            int bit = 0;
            int ch = 0;
            var geohash = new System.Text.StringBuilder();
            
            while (geohash.Length < precision)
            {
                double mid;
                if (isLon)
                {
                    mid = (lon[0] + lon[1]) / 2;
                    if (longitude >= mid)
                    {
                        ch |= 1 << (4 - bit);
                        lon[0] = mid;
                    }
                    else
                    {
                        lon[1] = mid;
                    }
                }
                else
                {
                    mid = (lat[0] + lat[1]) / 2;
                    if (latitude >= mid)
                    {
                        ch |= 1 << (4 - bit);
                        lat[0] = mid;
                    }
                    else
                    {
                        lat[1] = mid;
                    }
                }
                
                isLon = !isLon;
                if (bit < 4)
                {
                    bit++;
                }
                else
                {
                    geohash.Append(base32[ch]);
                    bit = 0;
                    ch = 0;
                }
            }
            
            return geohash.ToString();
        }

        #endregion
    }

    #region Location Types

    /// <summary>
    /// Location information from backend
    /// </summary>
    [Serializable]
    public class LocationInfo
    {
        public double Latitude;
        public double Longitude;
        public LocationDensity Density;
        public float TerritoryRadius;
        public string Geohash;
        public string CountryCode;
        public string RegionCode;
        public DateTime CachedAt;
    }

    /// <summary>
    /// Location density classification
    /// </summary>
    public enum LocationDensity
    {
        Rural,      // Low density - 50m radius
        Suburban,   // Medium density - 50m radius
        Urban       // High density - 25m radius
    }

    #endregion
}
