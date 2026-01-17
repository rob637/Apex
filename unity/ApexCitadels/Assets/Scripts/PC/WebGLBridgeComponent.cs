using System;
using System.Runtime.InteropServices;
using UnityEngine;

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

    public void SendTerritorySelected(string territoryId)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JS_SendTerritorySelected(territoryId);
#endif
        Debug.Log($"[WebGLBridge] Territory selected: {territoryId}");
    }

    public void SendNotification(string title, string message)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        JS_SendNotification(title, message);
#endif
        Debug.Log($"[WebGLBridge] Notification: {title} - {message}");
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
        Debug.Log("[WebGLBridge] Received login token");
        OnLoginTokenReceived?.Invoke(token);
    }

    public void SelectTerritoryFromWeb(string territoryId)
    {
        Debug.Log($"[WebGLBridge] Web requested territory: {territoryId}");
        OnTerritorySelectedFromWeb?.Invoke(territoryId);

        // Forward to PC game controller
        if (ApexCitadels.PC.PCGameController.Instance != null)
        {
            ApexCitadels.PC.PCGameController.Instance.SelectTerritory(territoryId);
        }
    }

    public void ExecuteCommand(string commandJson)
    {
        Debug.Log($"[WebGLBridge] Command: {commandJson}");
        OnCommandReceived?.Invoke(commandJson);
    }

    public void TogglePanel(string panelName)
    {
        Debug.Log($"[WebGLBridge] Toggle panel: {panelName}");
        
        if (ApexCitadels.PC.UI.PCUIManager.Instance != null)
        {
            ApexCitadels.PC.UI.PCUIManager.Instance.TogglePanel(panelName);
        }
    }

    public void SetCameraMode(string mode)
    {
        if (ApexCitadels.PC.PCCameraController.Instance == null) return;

        switch (mode.ToLower())
        {
            case "worldmap":
                ApexCitadels.PC.PCCameraController.Instance.EnterWorldMapMode();
                break;
            case "territory":
                ApexCitadels.PC.PCCameraController.Instance.EnterTerritoryMode(
                    ApexCitadels.PC.PCGameController.Instance?.SelectedTerritoryId);
                break;
            case "firstperson":
                ApexCitadels.PC.PCCameraController.Instance.EnterFirstPersonMode();
                break;
            case "cinematic":
                ApexCitadels.PC.PCCameraController.Instance.EnterCinematicMode();
                break;
        }
    }

    #endregion
}
