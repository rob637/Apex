using System;
using System.Runtime.InteropServices;
using UnityEngine;
using ApexCitadels.Core;

namespace ApexCitadels.PC.WebGL
{
    /// <summary>
    /// Bridge for JavaScript-Unity communication in WebGL builds.
    /// Allows the web page to interact with the Unity game.
    /// Add this component to a GameObject named "WebGLBridge" in your scene.
    /// </summary>
    [AddComponentMenu("Apex Citadels/PC/WebGL Bridge")]
    public class WebGLBridgeComponent : MonoBehaviour
    {
        public static WebGLBridgeComponent Instance { get; private set; }

    // Events for Unity-side listeners
    public event Action<string> OnLoginTokenReceived;
    public event Action<string> OnTerritorySelectedFromWeb;
    public event Action<string> OnCommandReceived;

#if UNITY_WEBGL && !UNITY_EDITOR
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
        NotifyGameReady();
    }

    #region Unity → JavaScript

    public void NotifyGameReady()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            JS_SendGameReady();
            ApexLogger.Log("[WebGLBridge] Notified web page: Game Ready", ApexLogger.LogCategory.Network);
        }
        catch (Exception e)
        {
            ApexLogger.LogWarning($"[WebGLBridge] JS_SendGameReady failed: {e.Message}", ApexLogger.LogCategory.Network);
        }
#else
        ApexLogger.Log("[WebGLBridge] Game Ready (non-WebGL)", ApexLogger.LogCategory.Network);
#endif
    }

    public void SendTerritorySelected(string territoryId)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JS_SendTerritorySelected(territoryId);
#endif
        ApexLogger.Log($"[WebGLBridge] Territory selected: {territoryId}", ApexLogger.LogCategory.Network);
    }

    public void SendNotification(string title, string message)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JS_SendNotification(title, message);
#endif
        ApexLogger.Log($"[WebGLBridge] Notification: {title} - {message}", ApexLogger.LogCategory.Network);
    }

    public void RequestFullscreen()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JS_RequestFullscreen();
#endif
    }

    #endregion

    #region JavaScript → Unity (called via SendMessage)

    public void ReceiveLoginToken(string token)
    {
        ApexLogger.Log("[WebGLBridge] Received login token", ApexLogger.LogCategory.Network);
        OnLoginTokenReceived?.Invoke(token);
    }

    public void SelectTerritoryFromWeb(string territoryId)
    {
        ApexLogger.Log($"[WebGLBridge] Web requested territory: {territoryId}", ApexLogger.LogCategory.Network);
        OnTerritorySelectedFromWeb?.Invoke(territoryId);

        // Forward to PC game controller
        if (PCGameController.Instance != null)
        {
            PCGameController.Instance.SelectTerritory(territoryId);
        }
    }

    public void ExecuteCommand(string commandJson)
    {
        ApexLogger.Log($"[WebGLBridge] Command: {commandJson}", ApexLogger.LogCategory.Network);
        OnCommandReceived?.Invoke(commandJson);
    }

    public void TogglePanel(string panelName)
    {
        ApexLogger.Log($"[WebGLBridge] Toggle panel: {panelName}", ApexLogger.LogCategory.Network);
        
        if (UI.PCUIManager.Instance != null)
        {
            UI.PCUIManager.Instance.TogglePanel(panelName);
        }
    }

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

    #endregion
}
}