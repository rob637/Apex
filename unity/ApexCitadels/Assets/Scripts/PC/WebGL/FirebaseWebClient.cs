using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

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
                Debug.Log("[FirebaseWebClient] Detected localhost, using emulator");
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
            Debug.Log($"[FirebaseWebClient] Calling {url}");

            // Prepare request body (Firebase callable functions expect { data: ... })
            string jsonBody = data != null 
                ? JsonUtility.ToJson(new CallableRequest { data = JsonUtility.ToJson(data) })
                : "{\"data\":{}}";

            Debug.Log($"[FirebaseWebClient] Request body: {jsonBody}");

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
                    Debug.Log($"[FirebaseWebClient] Response: {response}");

                    // Firebase callable functions return { result: ... }
                    var wrapper = JsonUtility.FromJson<CallableResponse<T>>(response);
                    return wrapper.result;
                }
                else
                {
                    string error = $"Error calling {functionName}: {request.error} - {request.downloadHandler?.text}";
                    Debug.LogError($"[FirebaseWebClient] {error}");
                    OnError?.Invoke(error);
                    throw new Exception(error);
                }
            }
        }

        /// <summary>
        /// Get territories in a geographic area
        /// </summary>
        public async Task<List<TerritorySnapshot>> GetTerritoriesInArea(
            double north, double south, double east, double west, int limit = 100)
        {
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
                
                Debug.Log($"[FirebaseWebClient] Received {response.count} territories");
                OnTerritoriesReceived?.Invoke(response.territories);
                
                return response.territories;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseWebClient] GetTerritoriesInArea failed: {ex.Message}");
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
                
                Debug.Log($"[FirebaseWebClient] Received {response.tiles?.Count ?? 0} map tiles");
                return response.tiles ?? new List<MapTile>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseWebClient] GetMapTiles failed: {ex.Message}");
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

            Debug.Log($"[FirebaseWebClient] Fetching territories from: {url}");

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
                    Debug.Log($"[FirebaseWebClient] Firestore response length: {response.Length}");

                    // Parse Firestore REST response using simple JSON parsing
                    var territories = ParseFirestoreResponse(response);

                    Debug.Log($"[FirebaseWebClient] Parsed {territories.Count} territories");
                    OnTerritoriesReceived?.Invoke(territories);
                    return territories;
                }
                else
                {
                    Debug.LogError($"[FirebaseWebClient] Firestore error: {request.error} - {request.downloadHandler?.text}");
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
                // Find all document blocks
                var documentPattern = @"""name"":\s*""([^""]+/territories/([^""]+))""[^}]*?""fields"":\s*\{([^}]+(?:\{[^}]*\}[^}]*)*)\}";
                var matches = Regex.Matches(json, documentPattern, RegexOptions.Singleline);

                Debug.Log($"[FirebaseWebClient] Found {matches.Count} document matches");

                foreach (Match match in matches)
                {
                    string docId = match.Groups[2].Value;
                    string fieldsJson = match.Groups[3].Value;

                    var territory = new TerritorySnapshot
                    {
                        id = docId,
                        name = ExtractStringField(fieldsJson, "name") ?? 
                               ExtractStringField(fieldsJson, "territoryName") ?? 
                               docId,
                        latitude = ExtractDoubleField(fieldsJson, "latitude") ?? 
                                   ExtractDoubleField(fieldsJson, "centerLatitude") ?? 0,
                        longitude = ExtractDoubleField(fieldsJson, "longitude") ?? 
                                    ExtractDoubleField(fieldsJson, "centerLongitude") ?? 0,
                        radius = (float)(ExtractDoubleField(fieldsJson, "radius") ?? 
                                         ExtractDoubleField(fieldsJson, "radiusMeters") ?? 100),
                        ownerId = ExtractStringField(fieldsJson, "ownerId") ?? "",
                        ownerName = ExtractStringField(fieldsJson, "ownerName") ?? "Unknown",
                        allianceId = ExtractStringField(fieldsJson, "allianceId"),
                        allianceName = ExtractStringField(fieldsJson, "allianceName"),
                        allianceTag = ExtractStringField(fieldsJson, "allianceTag"),
                        level = ExtractIntField(fieldsJson, "level") ?? 1,
                        isContested = ExtractBoolField(fieldsJson, "isContested") ?? false,
                        defenseRating = ExtractIntField(fieldsJson, "defenseRating") ?? 0,
                        totalBlocks = ExtractIntField(fieldsJson, "totalBlocks") ?? 0
                    };

                    Debug.Log($"[FirebaseWebClient] Parsed territory: {territory.name} at ({territory.latitude}, {territory.longitude})");
                    territories.Add(territory);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseWebClient] Error parsing Firestore response: {ex.Message}");
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
