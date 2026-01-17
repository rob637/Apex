using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ApexCitadels.PC.WebGL
{
    /// <summary>
    /// Bridge for JavaScript-Unity communication in WebGL builds.
    /// Allows the web page to interact with the Unity game.
    /// </summary>
    public class WebGLBridge : MonoBehaviour
    {
        public static WebGLBridge Instance { get; private set; }

        // Events for Unity-side listeners
        public event Action<string> OnLoginTokenReceived;
        public event Action<string> OnTerritorySelectedFromWeb;
        public event Action<string> OnCommandReceived;

#if UNITY_WEBGL && !UNITY_EDITOR
        // Import JavaScript functions
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
}
