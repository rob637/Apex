// ============================================================================
// APEX CITADELS - GEOCODING SERVICE
// Convert addresses to lat/lon coordinates using free geocoding APIs
// ============================================================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace ApexCitadels.Map
{
    /// <summary>
    /// Simple geocoding service to convert addresses to coordinates.
    /// Uses Nominatim (OpenStreetMap's free geocoding service).
    /// </summary>
    public class GeocodingService : MonoBehaviour
    {
        private static GeocodingService _instance;
        public static GeocodingService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("GeocodingService");
                    _instance = go.AddComponent<GeocodingService>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Result of a geocoding request
        /// </summary>
        public class GeocodingResult
        {
            public bool Success;
            public double Latitude;
            public double Longitude;
            public string DisplayName;
            public string Error;
        }
        
        /// <summary>
        /// Search for an address and get coordinates
        /// </summary>
        public void SearchAddress(string address, Action<GeocodingResult> callback)
        {
            StartCoroutine(SearchAddressCoroutine(address, callback));
        }
        
        private IEnumerator SearchAddressCoroutine(string address, Action<GeocodingResult> callback)
        {
            // URL encode the address
            string encodedAddress = UnityWebRequest.EscapeURL(address);
            
            // Use Nominatim (OpenStreetMap's free geocoding)
            string url = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&limit=1";
            
            Debug.Log($"[Geocoding] Searching for: {address}");
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                // Nominatim requires a user agent
                request.SetRequestHeader("User-Agent", "ApexCitadels/1.0 (Unity Game)");
                
                yield return request.SendWebRequest();
                
                var result = new GeocodingResult();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    Debug.Log($"[Geocoding] Response: {json}");
                    
                    // Parse the JSON array manually (simple approach)
                    if (json.Contains("\"lat\"") && json.Contains("\"lon\""))
                    {
                        try
                        {
                            // Extract lat
                            int latStart = json.IndexOf("\"lat\":\"") + 7;
                            int latEnd = json.IndexOf("\"", latStart);
                            string latStr = json.Substring(latStart, latEnd - latStart);
                            
                            // Extract lon
                            int lonStart = json.IndexOf("\"lon\":\"") + 7;
                            int lonEnd = json.IndexOf("\"", lonStart);
                            string lonStr = json.Substring(lonStart, lonEnd - lonStart);
                            
                            // Extract display name
                            int nameStart = json.IndexOf("\"display_name\":\"") + 16;
                            int nameEnd = json.IndexOf("\"", nameStart);
                            string displayName = json.Substring(nameStart, nameEnd - nameStart);
                            
                            result.Success = true;
                            result.Latitude = double.Parse(latStr, System.Globalization.CultureInfo.InvariantCulture);
                            result.Longitude = double.Parse(lonStr, System.Globalization.CultureInfo.InvariantCulture);
                            result.DisplayName = displayName;
                            
                            Debug.Log($"[Geocoding] Found: {result.DisplayName} at {result.Latitude:F6}, {result.Longitude:F6}");
                        }
                        catch (Exception e)
                        {
                            result.Success = false;
                            result.Error = $"Parse error: {e.Message}";
                            Debug.LogWarning($"[Geocoding] {result.Error}");
                        }
                    }
                    else
                    {
                        result.Success = false;
                        result.Error = "No results found for that address";
                        Debug.LogWarning($"[Geocoding] {result.Error}");
                    }
                }
                else
                {
                    result.Success = false;
                    result.Error = request.error;
                    Debug.LogError($"[Geocoding] Request failed: {result.Error}");
                }
                
                callback?.Invoke(result);
            }
        }
        
        /// <summary>
        /// Quick presets for common locations
        /// </summary>
        public static class Presets
        {
            public static readonly (double lat, double lon, string name) NewYorkCity = (40.7128, -74.0060, "New York City, NY");
            public static readonly (double lat, double lon, string name) LosAngeles = (34.0522, -118.2437, "Los Angeles, CA");
            public static readonly (double lat, double lon, string name) Chicago = (41.8781, -87.6298, "Chicago, IL");
            public static readonly (double lat, double lon, string name) SanFrancisco = (37.7749, -122.4194, "San Francisco, CA");
            public static readonly (double lat, double lon, string name) Seattle = (47.6062, -122.3321, "Seattle, WA");
            public static readonly (double lat, double lon, string name) Miami = (25.7617, -80.1918, "Miami, FL");
            public static readonly (double lat, double lon, string name) Boston = (42.3601, -71.0589, "Boston, MA");
            public static readonly (double lat, double lon, string name) Denver = (39.7392, -104.9903, "Denver, CO");
            public static readonly (double lat, double lon, string name) Austin = (30.2672, -97.7431, "Austin, TX");
            public static readonly (double lat, double lon, string name) ViennaVA = (38.9012, -77.2653, "Vienna, VA");
            public static readonly (double lat, double lon, string name) London = (51.5074, -0.1278, "London, UK");
            public static readonly (double lat, double lon, string name) Paris = (48.8566, 2.3522, "Paris, France");
            public static readonly (double lat, double lon, string name) Tokyo = (35.6762, 139.6503, "Tokyo, Japan");
        }
    }
}
