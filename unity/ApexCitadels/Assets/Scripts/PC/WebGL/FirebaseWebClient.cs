using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using ApexCitadels.Core;

namespace ApexCitadels.PC.WebGL
{
    /// <summary>
    /// Firebase client for WebGL builds.
    /// Uses HTTP calls to Firebase Cloud Functions (callable functions use HTTPS).
    /// Works on all platforms including WebGL where native Firebase SDK isn't available.
    /// </summary>
    public class FirebaseWebClient : MonoBehaviour
    {
        public static FirebaseWebClient Instance { get; private set; }

        [Header("Firebase Configuration")]
        [SerializeField] private string projectId = "apex-citadels-dev";
        [SerializeField] private string region = "us-central1";
        
        [Header("Development Settings")]
        [SerializeField] private bool useEmulator = false;  // Set to false for production
        [SerializeField] private string emulatorHost = "localhost";
        [SerializeField] private int emulatorPort = 5001;
        [SerializeField] private int firestoreEmulatorPort = 8080;

        // Auth token (for authenticated calls)
        private string _authToken = null;
        private string _userId = null;

        // Events
        public event Action<List<TerritorySnapshot>> OnTerritoriesReceived;
        public event Action<string> OnError;

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
            // Check if we're running in WebGL and should auto-detect emulator
#if UNITY_WEBGL && !UNITY_EDITOR
            // In production WebGL, check the URL
            string url = Application.absoluteURL;
            if (url.Contains("localhost") || url.Contains("127.0.0.1"))
            {
                useEmulator = true;
                ApexLogger.Log("Detected localhost, using emulator", ApexLogger.LogCategory.Network);
            }
#endif
        }

        /// <summary>
        /// Get the base URL for Cloud Functions
        /// </summary>
        private string GetFunctionsBaseUrl()
        {
            if (useEmulator)
            {
                return $"http://{emulatorHost}:{emulatorPort}/{projectId}/{region}";
            }
            return $"https://{region}-{projectId}.cloudfunctions.net";
        }

        /// <summary>
        /// Call a Firebase callable function
        /// </summary>
        public async Task<T> CallFunction<T>(string functionName, object data = null)
        {
            string url = $"{GetFunctionsBaseUrl()}/{functionName}";
            ApexLogger.LogVerbose($"Calling {url}", ApexLogger.LogCategory.Network);

            // Prepare request body (Firebase callable functions expect { data: ... })
            string jsonBody = data != null 
                ? JsonUtility.ToJson(new CallableRequest { data = JsonUtility.ToJson(data) })
                : "{\"data\":{}}";

            ApexLogger.LogVerbose($"Request body: {jsonBody}", ApexLogger.LogCategory.Network);

            using (var request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Add auth header if we have a token
                if (!string.IsNullOrEmpty(_authToken))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
                }

                var operation = request.SendWebRequest();

                // Wait for completion
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    ApexLogger.LogVerbose($"Response: {response}", ApexLogger.LogCategory.Network);

                    // Firebase callable functions return { result: ... }
                    var wrapper = JsonUtility.FromJson<CallableResponse<T>>(response);
                    return wrapper.result;
                }
                else
                {
                    string error = $"Error calling {functionName}: {request.error} - {request.downloadHandler?.text}";
                    ApexLogger.LogError($"{error}", ApexLogger.LogCategory.Network);
                    OnError?.Invoke(error);
                    throw new Exception(error);
                }
            }
        }

        /// <summary>
        /// Get territories in a geographic area
        /// Falls back to direct Firestore REST API if callable function fails
        /// </summary>
        public async Task<List<TerritorySnapshot>> GetTerritoriesInArea(
            double north, double south, double east, double west, int limit = 100)
        {
            // First try: Use direct Firestore REST API (more reliable)
            try
            {
                ApexLogger.Log($"Fetching territories in area via Firestore REST API...", ApexLogger.LogCategory.Network);
                var allTerritories = await GetAllTerritories();
                
                // Filter by bounds client-side
                var filtered = allTerritories.FindAll(t => 
                    t.latitude >= south && t.latitude <= north &&
                    t.longitude >= west && t.longitude <= east
                );
                
                ApexLogger.Log($"Found {filtered.Count} territories in area (filtered from {allTerritories.Count})", ApexLogger.LogCategory.Network);
                OnTerritoriesReceived?.Invoke(filtered);
                return filtered;
            }
            catch (Exception ex)
            {
                ApexLogger.LogWarning($"Firestore REST failed, trying callable function: {ex.Message}", ApexLogger.LogCategory.Network);
            }

            // Fallback: Try callable function
            try
            {
                var request = new GetTerritoriesRequest
                {
                    north = north,
                    south = south,
                    east = east,
                    west = west,
                    limit = limit
                };

                var response = await CallFunction<GetTerritoriesResponse>("getTerritoriesInArea", request);
                
                ApexLogger.Log($"Received {response.count} territories via callable", ApexLogger.LogCategory.Network);
                OnTerritoriesReceived?.Invoke(response.territories);
                
                return response.territories;
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"GetTerritoriesInArea failed completely: {ex.Message}", ApexLogger.LogCategory.Network);
                return new List<TerritorySnapshot>();
            }
        }

        /// <summary>
        /// Get map tiles (includes territories, activity, etc.)
        /// </summary>
        public async Task<List<MapTile>> GetMapTiles(double latitude, double longitude, int precision = 5)
        {
            try
            {
                var request = new GetMapTilesRequest
                {
                    latitude = latitude,
                    longitude = longitude,
                    precision = precision
                };

                var response = await CallFunction<GetMapTilesResponse>("getMapTiles", request);
                
                ApexLogger.Log($"Received {response.tiles?.Count ?? 0} map tiles", ApexLogger.LogCategory.Network);
                return response.tiles ?? new List<MapTile>();
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"GetMapTiles failed: {ex.Message}", ApexLogger.LogCategory.Network);
                return new List<MapTile>();
            }
        }

        /// <summary>
        /// Direct Firestore REST API call (for simple reads without auth)
        /// </summary>
        public async Task<List<TerritorySnapshot>> GetAllTerritories()
        {
            // Use Firestore REST API directly for simple public reads
            string url = useEmulator
                ? $"http://{emulatorHost}:{firestoreEmulatorPort}/v1/projects/{projectId}/databases/(default)/documents/territories"
                : $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/territories";

            ApexLogger.LogVerbose($"Fetching territories from: {url}", ApexLogger.LogCategory.Network);

            using (var request = UnityWebRequest.Get(url))
            {
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    ApexLogger.LogVerbose($"Firestore response length: {response.Length}", ApexLogger.LogCategory.Network);

                    // Parse Firestore REST response using simple JSON parsing
                    var territories = ParseFirestoreResponse(response);

                    ApexLogger.Log($"Parsed {territories.Count} territories", ApexLogger.LogCategory.Network);
                    OnTerritoriesReceived?.Invoke(territories);
                    return territories;
                }
                else
                {
                    ApexLogger.LogError($"Firestore error: {request.error} - {request.downloadHandler?.text}", ApexLogger.LogCategory.Network);
                    return new List<TerritorySnapshot>();
                }
            }
        }

        /// <summary>
        /// Parse Firestore REST API response manually (JsonUtility doesn't handle nested dicts well)
        /// </summary>
        private List<TerritorySnapshot> ParseFirestoreResponse(string json)
        {
            var territories = new List<TerritorySnapshot>();

            try
            {
                // Split by document entries - look for territory IDs in the name field
                // Pattern: "name": "projects/.../territories/TERRITORY_ID"
                var docIdPattern = @"""name"":\s*""projects/[^""]+/territories/([^""]+)""";
                var docMatches = Regex.Matches(json, docIdPattern);
                
                ApexLogger.LogVerbose($"Found {docMatches.Count} document matches", ApexLogger.LogCategory.Network);

                // For each document, find its section of the JSON and extract fields
                for (int i = 0; i < docMatches.Count; i++)
                {
                    string docId = docMatches[i].Groups[1].Value;
                    
                    // Get the JSON section for this document (from this match to the next, or end)
                    int startIndex = docMatches[i].Index;
                    int endIndex = (i + 1 < docMatches.Count) ? docMatches[i + 1].Index : json.Length;
                    string docSection = json.Substring(startIndex, endIndex - startIndex);

                    // Extract latitude and longitude from this document section
                    double lat = ExtractDoubleFromSection(docSection, "latitude") ?? 0;
                    double lng = ExtractDoubleFromSection(docSection, "longitude") ?? 0;
                    
                    ApexLogger.LogVerbose($"Doc {docId}: lat={lat}, lng={lng}", ApexLogger.LogCategory.Network);

                    var territory = new TerritorySnapshot
                    {
                        id = docId,
                        name = ExtractStringFromSection(docSection, "name") ?? docId,
                        latitude = lat,
                        longitude = lng,
                        radius = (float)(ExtractDoubleFromSection(docSection, "radius") ?? 
                                         ExtractDoubleFromSection(docSection, "radiusMeters") ?? 100),
                        ownerId = ExtractStringFromSection(docSection, "ownerId") ?? 
                                  ExtractStringFromSection(docSection, "controlledBy") ?? "",
                        ownerName = ExtractStringFromSection(docSection, "ownerName") ?? 
                                    ExtractStringFromSection(docSection, "controllerName") ?? "Unknown",
                        allianceId = ExtractStringFromSection(docSection, "allianceId"),
                        allianceName = ExtractStringFromSection(docSection, "allianceName"),
                        allianceTag = ExtractStringFromSection(docSection, "allianceTag"),
                        level = ExtractIntFromSection(docSection, "level") ?? 1,
                        isContested = ExtractBoolFromSection(docSection, "isContested") ?? false,
                        defenseRating = ExtractIntFromSection(docSection, "defenseRating") ?? 0,
                        totalBlocks = ExtractIntFromSection(docSection, "totalBlocks") ?? 0
                    };

                    ApexLogger.LogVerbose($"Parsed territory: {territory.name} at ({territory.latitude}, {territory.longitude})", ApexLogger.LogCategory.Network);
                    territories.Add(territory);
                }
            }
            catch (Exception ex)
            {
                ApexLogger.LogError($"Error parsing Firestore response: {ex.Message}", ApexLogger.LogCategory.Network);
            }

            return territories;
        }

        private string ExtractStringField(string json, string fieldName)
        {
            // Match: "fieldName": { "stringValue": "value" }
            var pattern = $@"""{fieldName}"":\s*\{{\s*""stringValue"":\s*""([^""]*)""\s*\}}";
            var match = Regex.Match(json, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }

        private double? ExtractDoubleField(string json, string fieldName)
        {
            // Match: "fieldName": { "doubleValue": 123.45 } or "integerValue": "123"
            // Note: doubleValue can be negative (e.g., longitude -77.2639)
            // Using [\s\S]* to match across newlines since JSON may be pretty-printed
            var doublePattern = $@"""{fieldName}"":\s*\{{\s*""doubleValue"":\s*(-?[\d.]+)\s*\}}";
            var intPattern = $@"""{fieldName}"":\s*\{{\s*""integerValue"":\s*""?(-?[\d]+)""?\s*\}}";
            
            var match = Regex.Match(json, doublePattern, RegexOptions.Singleline);
            if (match.Success)
            {
                string valStr = match.Groups[1].Value;
                if (double.TryParse(valStr, System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out double dVal))
                {
                    return dVal;
                }
            }

            match = Regex.Match(json, intPattern, RegexOptions.Singleline);
            if (match.Success)
            {
                string valStr = match.Groups[1].Value;
                if (double.TryParse(valStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double iVal))
                {
                    return iVal;
                }
            }

            return null;
        }

        private int? ExtractIntField(string json, string fieldName)
        {
            var pattern = $@"""{fieldName}"":\s*\{{\s*""integerValue"":\s*""?([\d-]+)""?\s*\}}";
            var match = Regex.Match(json, pattern);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int val))
                return val;
            return null;
        }

        private bool? ExtractBoolField(string json, string fieldName)
        {
            var pattern = $@"""{fieldName}"":\s*\{{\s*""booleanValue"":\s*(true|false)\s*\}}";
            var match = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.ToLower() == "true";
            return null;
        }

        // New simpler extraction methods that work on document sections
        private string ExtractStringFromSection(string section, string fieldName)
        {
            // Match: "fieldName": { "stringValue": "value" } with possible whitespace/newlines
            var pattern = $@"""{fieldName}""\s*:\s*\{{\s*""stringValue""\s*:\s*""([^""]*)""\s*\}}";
            var match = Regex.Match(section, pattern, RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value : null;
        }

        private double? ExtractDoubleFromSection(string section, string fieldName)
        {
            // Match: "fieldName": { "doubleValue": 123.45 } - handles negative numbers and whitespace
            var pattern = $@"""{fieldName}""\s*:\s*\{{\s*""doubleValue""\s*:\s*(-?[\d.]+)\s*\}}";
            var match = Regex.Match(section, pattern, RegexOptions.Singleline);
            if (match.Success)
            {
                if (double.TryParse(match.Groups[1].Value, 
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, 
                    out double val))
                {
                    return val;
                }
            }
            
            // Also try integerValue
            pattern = $@"""{fieldName}""\s*:\s*\{{\s*""integerValue""\s*:\s*""?(-?[\d]+)""?\s*\}}";
            match = Regex.Match(section, pattern, RegexOptions.Singleline);
            if (match.Success)
            {
                if (double.TryParse(match.Groups[1].Value, out double val))
                {
                    return val;
                }
            }
            
            return null;
        }

        private int? ExtractIntFromSection(string section, string fieldName)
        {
            var pattern = $@"""{fieldName}""\s*:\s*\{{\s*""integerValue""\s*:\s*""?(-?[\d]+)""?\s*\}}";
            var match = Regex.Match(section, pattern, RegexOptions.Singleline);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int val))
                return val;
            return null;
        }

        private bool? ExtractBoolFromSection(string section, string fieldName)
        {
            var pattern = $@"""{fieldName}""\s*:\s*\{{\s*""booleanValue""\s*:\s*(true|false)\s*\}}";
            var match = Regex.Match(section, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success)
                return match.Groups[1].Value.ToLower() == "true";
            return null;
        }

        #region Request/Response Types

        [Serializable]
        private class CallableRequest
        {
            public string data;
        }

        [Serializable]
        private class CallableResponse<T>
        {
            public T result;
        }

        [Serializable]
        private class GetTerritoriesRequest
        {
            public double north;
            public double south;
            public double east;
            public double west;
            public int limit;
        }

        [Serializable]
        private class GetTerritoriesResponse
        {
            public List<TerritorySnapshot> territories;
            public int count;
        }

        [Serializable]
        private class GetMapTilesRequest
        {
            public double latitude;
            public double longitude;
            public int precision;
        }

        [Serializable]
        private class GetMapTilesResponse
        {
            public List<MapTile> tiles;
        }

        #endregion
    }

    #region Shared Data Types

    [Serializable]
    public class TerritorySnapshot
    {
        public string id;
        public string name;
        public double latitude;
        public double longitude;
        public float radius;
        public string ownerId;
        public string ownerName;
        public string allianceId;
        public string allianceName;
        public string allianceTag;
        public string allianceColor;
        public int level;
        public string structureType;
        public bool isContested;
        public bool isShielded;
        public int defenseRating;
        public int totalBlocks;
    }

    [Serializable]
    public class MapTile
    {
        public string id;
        public string geohash;
        public List<TerritorySnapshot> territories;
        public int totalTerritories;
    }

    #endregion
}
