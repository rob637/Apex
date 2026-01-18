using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ApexCitadels.PC.WebGL
{
    /// <summary>
    /// Bridge for JavaScript-Unity communication in WebGL builds.
    /// Allows the web page to interact with the Unity game.
    /// Includes Firebase SDK integration via the embedding HTML page.
    /// </summary>
    public class WebGLBridge : MonoBehaviour
    {
        public static WebGLBridge Instance { get; private set; }

        // Events for Unity-side listeners
        public event Action<string> OnLoginTokenReceived;
        public event Action<string> OnTerritorySelectedFromWeb;
        public event Action<string> OnCommandReceived;
        public event Action<string> OnFirebaseReady;
        public event Action<List<TerritorySnapshot>> OnTerritoriesReceived;

#if UNITY_WEBGL && !UNITY_EDITOR
        // =====================================================
        // JavaScript Imports - Unity → Browser
        // =====================================================
        
        [DllImport("__Internal")]
        private static extern void JS_SendGameReady();

        [DllImport("__Internal")]
        private static extern void JS_SendTerritorySelected(string territoryId);

        [DllImport("__Internal")]
        private static extern void JS_SendPlayerStats(string statsJson);

        [DllImport("__Internal")]
        private static extern void JS_SendNotification(string title, string message);

        [DllImport("__Internal")]
        private static extern string JS_GetAuthToken();

        [DllImport("__Internal")]
        private static extern void JS_RequestFullscreen();

        // =====================================================
        // Firebase Integration Imports
        // =====================================================
        
        [DllImport("__Internal")]
        private static extern int JS_IsFirebaseReady();

        [DllImport("__Internal")]
        private static extern string JS_GetFirebaseUserId();

        [DllImport("__Internal")]
        private static extern void JS_GetAllTerritories(string gameObjectName, string callbackMethod);

        [DllImport("__Internal")]
        private static extern void JS_GetTerritoriesInArea(
            double north, double south, double east, double west, 
            int maxResults, string gameObjectName, string callbackMethod);

        [DllImport("__Internal")]
        private static extern void JS_GetTerritory(string territoryId, string gameObjectName, string callbackMethod);

        [DllImport("__Internal")]
        private static extern string JS_GetCachedTerritories();

        [DllImport("__Internal")]
        private static extern void JS_SubscribeToTerritories(string gameObjectName, string callbackMethod);

        [DllImport("__Internal")]
        private static extern void JS_Log(string message);

        [DllImport("__Internal")]
        private static extern void JS_LogError(string message);
#endif

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
            // Notify web page that game is ready
            NotifyGameReady();
        }

        #region Unity → JavaScript

        /// <summary>
        /// Notify the web page that the game has loaded and is ready
        /// </summary>
        public void NotifyGameReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                JS_SendGameReady();
                Debug.Log("[WebGLBridge] Notified web page: Game Ready");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WebGLBridge] JS_SendGameReady failed: {e.Message}");
            }
#else
            Debug.Log("[WebGLBridge] Game Ready (non-WebGL)");
#endif
        }

        /// <summary>
        /// Send territory selection to web page
        /// </summary>
        public void SendTerritorySelected(string territoryId)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_SendTerritorySelected(territoryId);
#endif
            Debug.Log($"[WebGLBridge] Territory selected: {territoryId}");
        }

        /// <summary>
        /// Send player stats to web page (for external display)
        /// </summary>
        public void SendPlayerStats(PlayerStatsData stats)
        {
            string json = JsonUtility.ToJson(stats);
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_SendPlayerStats(json);
#endif
            Debug.Log($"[WebGLBridge] Stats sent: {json}");
        }

        /// <summary>
        /// Send a notification to be displayed in the browser
        /// </summary>
        public void SendNotification(string title, string message)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_SendNotification(title, message);
#endif
            Debug.Log($"[WebGLBridge] Notification: {title} - {message}");
        }

        /// <summary>
        /// Request fullscreen mode
        /// </summary>
        public void RequestFullscreen()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_RequestFullscreen();
#endif
        }

        /// <summary>
        /// Get auth token from browser (for Firebase auth)
        /// </summary>
        public string GetAuthToken()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return JS_GetAuthToken();
#else
            return null;
#endif
        }

        #endregion

        #region Firebase Integration (via JS SDK)

        /// <summary>
        /// Check if Firebase JS SDK is initialized and ready
        /// </summary>
        public bool IsFirebaseReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return JS_IsFirebaseReady() == 1;
#else
            return false;
#endif
        }

        /// <summary>
        /// Get the current Firebase user ID
        /// </summary>
        public string GetFirebaseUserId()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return JS_GetFirebaseUserId();
#else
            return null;
#endif
        }

        /// <summary>
        /// Request all territories from Firebase (async via callback)
        /// </summary>
        public void RequestAllTerritories()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_GetAllTerritories(gameObject.name, "OnTerritoriesJsonReceived");
            Debug.Log("[WebGLBridge] Requesting all territories from Firebase JS SDK");
#else
            Debug.Log("[WebGLBridge] Firebase not available outside WebGL");
#endif
        }

        /// <summary>
        /// Request territories in a geographic bounding box (async via callback)
        /// </summary>
        public void RequestTerritoriesInArea(double north, double south, double east, double west, int maxResults = 100)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_GetTerritoriesInArea(north, south, east, west, maxResults, gameObject.name, "OnTerritoriesJsonReceived");
            Debug.Log($"[WebGLBridge] Requesting territories in area: N{north} S{south} E{east} W{west}");
#endif
        }

        /// <summary>
        /// Request a single territory by ID (async via callback)
        /// </summary>
        public void RequestTerritory(string territoryId)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_GetTerritory(territoryId, gameObject.name, "OnTerritoryJsonReceived");
#endif
        }

        /// <summary>
        /// Get cached territories (synchronous, from pre-loaded data)
        /// </summary>
        public List<TerritorySnapshot> GetCachedTerritories()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                string json = JS_GetCachedTerritories();
                if (!string.IsNullOrEmpty(json) && json != "[]")
                {
                    return ParseTerritoriesJson(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebGLBridge] Error getting cached territories: {e.Message}");
            }
#endif
            return new List<TerritorySnapshot>();
        }

        /// <summary>
        /// Subscribe to real-time territory updates
        /// </summary>
        public void SubscribeToTerritoryUpdates()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_SubscribeToTerritories(gameObject.name, "OnTerritoryUpdateReceived");
            Debug.Log("[WebGLBridge] Subscribed to real-time territory updates");
#endif
        }

        /// <summary>
        /// Log to browser console
        /// </summary>
        public void LogToBrowser(string message)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_Log(message);
#else
            Debug.Log(message);
#endif
        }

        /// <summary>
        /// Log error to browser console
        /// </summary>
        public void LogErrorToBrowser(string message)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_LogError(message);
#else
            Debug.LogError(message);
#endif
        }

        #endregion

        #region Firebase Callbacks (called from JavaScript via SendMessage)

        /// <summary>
        /// Called from JavaScript when Firebase is ready
        /// </summary>
        public void OnFirebaseReadyCallback(string userId)
        {
            Debug.Log($"[WebGLBridge] Firebase ready! User: {userId}");
            OnFirebaseReady?.Invoke(userId);
            
            // Auto-load territories when Firebase is ready
            RequestAllTerritories();
        }

        /// <summary>
        /// Called from JavaScript with territories JSON
        /// </summary>
        public void OnTerritoriesJsonReceived(string json)
        {
            Debug.Log($"[WebGLBridge] Received territories JSON ({json.Length} chars)");
            try
            {
                var territories = ParseTerritoriesJson(json);
                Debug.Log($"[WebGLBridge] Parsed {territories.Count} territories");
                OnTerritoriesReceived?.Invoke(territories);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebGLBridge] Error parsing territories: {e.Message}");
            }
        }

        /// <summary>
        /// Called from JavaScript with single territory JSON
        /// </summary>
        public void OnTerritoryJsonReceived(string json)
        {
            Debug.Log($"[WebGLBridge] Received territory JSON");
            // Single territory callback - you can add specific handling here
        }

        /// <summary>
        /// Called from JavaScript with real-time territory updates
        /// </summary>
        public void OnTerritoryUpdateReceived(string changesJson)
        {
            Debug.Log($"[WebGLBridge] Received territory update");
            // Handle real-time updates - parse and forward to WorldMapRenderer
        }

        private List<TerritorySnapshot> ParseTerritoriesJson(string json)
        {
            var territories = new List<TerritorySnapshot>();
            
            // Unity's JsonUtility can't parse arrays directly, so we use wrapper or manual parse
            // For now, use simple approach with wrapper
            string wrappedJson = "{\"items\":" + json + "}";
            var wrapper = JsonUtility.FromJson<TerritoryListWrapper>(wrappedJson);
            
            if (wrapper?.items != null)
            {
                territories.AddRange(wrapper.items);
            }
            
            return territories;
        }

        #endregion

        #region JavaScript → Unity (called via SendMessage)

        /// <summary>
        /// Called from JavaScript to pass login token
        /// </summary>
        public void ReceiveLoginToken(string token)
        {
            Debug.Log($"[WebGLBridge] Received login token");
            OnLoginTokenReceived?.Invoke(token);
        }

        /// <summary>
        /// Called from JavaScript to select a territory
        /// </summary>
        public void SelectTerritoryFromWeb(string territoryId)
        {
            Debug.Log($"[WebGLBridge] Web requested territory: {territoryId}");
            OnTerritorySelectedFromWeb?.Invoke(territoryId);

            // Forward to PC game controller
            if (PCGameController.Instance != null)
            {
                PCGameController.Instance.SelectTerritory(territoryId);
            }
        }

        /// <summary>
        /// Called from JavaScript to execute a command
        /// </summary>
        public void ExecuteCommand(string commandJson)
        {
            Debug.Log($"[WebGLBridge] Command: {commandJson}");
            OnCommandReceived?.Invoke(commandJson);

            try
            {
                var command = JsonUtility.FromJson<WebCommand>(commandJson);
                ProcessCommand(command);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebGLBridge] Failed to parse command: {e.Message}");
            }
        }

        /// <summary>
        /// Called from JavaScript to toggle UI panels
        /// </summary>
        public void TogglePanel(string panelName)
        {
            Debug.Log($"[WebGLBridge] Toggle panel: {panelName}");
            
            if (PC.UI.PCUIManager.Instance != null)
            {
                // Use the string overload which handles conversion
                PC.UI.PCUIManager.Instance.TogglePanel(panelName);
            }
        }

        /// <summary>
        /// Set camera mode from web
        /// </summary>
        public void SetCameraMode(string mode)
        {
            if (PCCameraController.Instance == null) return;

            switch (mode.ToLower())
            {
                case "worldmap":
                    PCCameraController.Instance.EnterWorldMapMode();
                    break;
                case "territory":
                    PCCameraController.Instance.EnterTerritoryMode(
                        PCGameController.Instance?.SelectedTerritoryId);
                    break;
                case "firstperson":
                    PCCameraController.Instance.EnterFirstPersonMode();
                    break;
                case "cinematic":
                    PCCameraController.Instance.EnterCinematicMode();
                    break;
            }
        }

        private void ProcessCommand(WebCommand command)
        {
            switch (command.type)
            {
                case "navigate":
                    // Handle navigation commands
                    break;
                case "action":
                    // Handle action commands
                    break;
                case "settings":
                    // Handle settings changes
                    break;
            }
        }

        #endregion
    }

    /// <summary>
    /// Data structure for player stats sent to web
    /// </summary>
    [Serializable]
    public class PlayerStatsData
    {
        public string playerId;
        public string displayName;
        public int level;
        public int xp;
        public int territoriesOwned;
        public int allianceId;
        public int resources;
    }

    /// <summary>
    /// Data structure for commands from web
    /// </summary>
    [Serializable]
    public class WebCommand
    {
        public string type;
        public string action;
        public string payload;
    }

    /// <summary>
    /// Wrapper for JSON array parsing
    /// </summary>
    [Serializable]
    public class TerritoryListWrapper
    {
        public TerritorySnapshot[] items;
    }
}
