// ============================================================================
// APEX CITADELS - AR BOOTSTRAP
// Initializes all AR systems in the correct order
// ============================================================================
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using ApexCitadels.Core;
using ApexCitadels.AR.UI;

namespace ApexCitadels.AR
{
    /// <summary>
    /// Bootstraps the AR experience, ensuring all systems are initialized
    /// in the correct order: Permissions → AR Session → Geospatial → UI
    /// </summary>
    public class ARBootstrap : MonoBehaviour
    {
        [Header("Required Components")]
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARSessionController sessionController;
        [SerializeField] private GeospatialManager geospatialManager;
        [SerializeField] private SpatialAnchorManager anchorManager;
        [SerializeField] private ARPermissionHandler permissionHandler;
        [SerializeField] private ARHUD arHUD;

        [Header("Mock Components (Editor Only)")]
        [SerializeField] private MockGPSProvider mockGPSProvider;

        [Header("Loading UI")]
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private TMPro.TextMeshProUGUI loadingStatusText;
        [SerializeField] private UnityEngine.UI.Slider loadingProgressBar;

        [Header("Settings")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float minLoadingTime = 2f;

        private float loadingStartTime;
        private bool initializationComplete = false;

        public bool IsInitialized => initializationComplete;

        private void Start()
        {
            if (autoStart)
            {
                StartCoroutine(InitializeAR());
            }
        }

        /// <summary>
        /// Start the AR initialization sequence
        /// </summary>
        public void StartInitialization()
        {
            if (!initializationComplete)
            {
                StartCoroutine(InitializeAR());
            }
        }

        private IEnumerator InitializeAR()
        {
            loadingStartTime = Time.time;
            
            ShowLoading("Initializing...");
            SetProgress(0f);
            
            ApexLogger.Log(ApexLogger.LogCategory.AR, "[ARBootstrap] Starting AR initialization sequence");

            // Step 1: Check if running on mobile or desktop
            bool isMobile = Application.isMobilePlatform;
            bool isEditor = Application.isEditor;

            UpdateStatus($"Platform: {(isMobile ? "Mobile" : isEditor ? "Editor" : "Desktop")}");
            SetProgress(0.1f);
            yield return new WaitForSeconds(0.3f);

            // Step 2: Setup mock GPS for editor/desktop
            if (!isMobile)
            {
                UpdateStatus("Setting up GPS simulation...");
                SetProgress(0.2f);
                
                if (mockGPSProvider == null)
                {
                    var mockGO = new GameObject("MockGPSProvider");
                    mockGPSProvider = mockGO.AddComponent<MockGPSProvider>();
                }
                
                ApexLogger.Log(ApexLogger.LogCategory.AR, "[ARBootstrap] Mock GPS provider activated");
                yield return new WaitForSeconds(0.3f);
            }

            // Step 3: Request permissions (mobile only)
            if (isMobile)
            {
                UpdateStatus("Checking permissions...");
                SetProgress(0.3f);
                
                if (permissionHandler != null)
                {
                    bool permissionsGranted = false;
                    permissionHandler.CheckAndRequestAllPermissions(granted => permissionsGranted = granted);
                    
                    // Wait for permissions
                    float permissionTimeout = 30f;
                    float permissionStart = Time.time;
                    
                    while (!permissionsGranted && Time.time - permissionStart < permissionTimeout)
                    {
                        yield return null;
                    }
                    
                    if (!permissionsGranted)
                    {
                        UpdateStatus("Permissions required!");
                        ApexLogger.LogWarning(ApexLogger.LogCategory.AR, "[ARBootstrap] Permissions not granted");
                        // Continue anyway - features will be limited
                    }
                }
            }

            SetProgress(0.4f);
            yield return new WaitForSeconds(0.2f);

            // Step 4: Initialize AR Session
            UpdateStatus("Starting AR session...");
            SetProgress(0.5f);

            if (sessionController != null && isMobile)
            {
                sessionController.StartARSession();
                
                // Wait for AR session to be tracking
                float arTimeout = 15f;
                float arStart = Time.time;
                
                while (ARSession.state != ARSessionState.SessionTracking && Time.time - arStart < arTimeout)
                {
                    UpdateStatus($"AR Session: {ARSession.state}");
                    yield return new WaitForSeconds(0.5f);
                }
                
                if (ARSession.state != ARSessionState.SessionTracking)
                {
                    ApexLogger.LogWarning(ApexLogger.LogCategory.AR, "[ARBootstrap] AR session failed to start");
                }
            }

            SetProgress(0.6f);
            yield return new WaitForSeconds(0.2f);

            // Step 5: Initialize Geospatial
            UpdateStatus("Initializing GPS...");
            SetProgress(0.7f);

            // Geospatial manager will initialize itself, just wait a bit
            yield return new WaitForSeconds(0.5f);

            SetProgress(0.8f);

            // Step 6: Initialize UI
            UpdateStatus("Loading UI...");
            SetProgress(0.9f);

            if (arHUD != null)
            {
                // Set initial state based on geospatial status
                if (geospatialManager != null && !geospatialManager.IsReady)
                {
                    arHUD.SetState(ARHUDState.Localizing);
                }
                else
                {
                    arHUD.SetState(ARHUDState.Normal);
                }
            }

            yield return new WaitForSeconds(0.3f);

            // Step 7: Ensure minimum loading time for branding
            float elapsed = Time.time - loadingStartTime;
            if (elapsed < minLoadingTime)
            {
                yield return new WaitForSeconds(minLoadingTime - elapsed);
            }

            // Done!
            SetProgress(1f);
            UpdateStatus("Ready!");
            
            yield return new WaitForSeconds(0.5f);
            
            HideLoading();
            initializationComplete = true;
            
            ApexLogger.Log(ApexLogger.LogCategory.AR, "[ARBootstrap] AR initialization complete!");
        }

        #region Loading UI

        private void ShowLoading(string message)
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
            }
            UpdateStatus(message);
        }

        private void HideLoading()
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
            }
        }

        private void UpdateStatus(string message)
        {
            if (loadingStatusText != null)
            {
                loadingStatusText.text = message;
            }
            ApexLogger.Log(ApexLogger.LogCategory.AR, $"[ARBootstrap] {message}");
        }

        private void SetProgress(float progress)
        {
            if (loadingProgressBar != null)
            {
                loadingProgressBar.value = progress;
            }
        }

        #endregion

        #region Component Discovery

        private void OnValidate()
        {
            // Auto-find components in editor
            if (arSession == null)
                arSession = FindFirstObjectByType<ARSession>();
            
            if (sessionController == null)
                sessionController = FindFirstObjectByType<ARSessionController>();
            
            if (geospatialManager == null)
                geospatialManager = FindFirstObjectByType<GeospatialManager>();
            
            if (anchorManager == null)
                anchorManager = FindFirstObjectByType<SpatialAnchorManager>();
            
            if (permissionHandler == null)
                permissionHandler = FindFirstObjectByType<ARPermissionHandler>();
            
            if (arHUD == null)
                arHUD = FindFirstObjectByType<ARHUD>();
        }

        #endregion
    }
}
