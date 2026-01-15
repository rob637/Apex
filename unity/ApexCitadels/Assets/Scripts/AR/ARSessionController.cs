using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ApexCitadels.AR
{
    /// <summary>
    /// Controls the AR session lifecycle, including initialization, tracking state,
    /// camera permissions, and session management.
    /// </summary>
    public class ARSessionController : MonoBehaviour
    {
        public static ARSessionController Instance { get; private set; }

        [Header("AR Components")]
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARCameraManager arCameraManager;
        [SerializeField] private Camera arCamera;

        [Header("Session Settings")]
        [SerializeField] private bool autoStartSession = true;
        [SerializeField] private float initializationTimeout = 15f;
        [SerializeField] private bool resetOnTrackingLoss = false;
        [SerializeField] private float trackingRecoveryTimeout = 10f;

        [Header("Quality Settings")]
        [SerializeField] private ARSessionQuality sessionQuality = ARSessionQuality.High;
        [SerializeField] private bool enableOcclusion = true;
        [SerializeField] private bool enableLightEstimation = true;

        [Header("Fallback")]
        [SerializeField] private bool enableFallbackCamera = true;
        [SerializeField] private Camera fallbackCamera;

        [Header("UI Feedback")]
        [SerializeField] private GameObject trackingUI;
        [SerializeField] private GameObject initializingUI;
        [SerializeField] private GameObject errorUI;

        // State
        private ARSessionState currentState = ARSessionState.None;
        private TrackingState trackingState = TrackingState.None;
        private bool isSessionRunning = false;
        private bool isARSupported = true;
        private float trackingLostTime = 0f;

        // Events
        public event Action OnSessionInitialized;
        public event Action OnSessionStarted;
        public event Action OnSessionPaused;
        public event Action OnSessionResumed;
        public event Action<ARSessionState> OnSessionStateChanged;
        public event Action<TrackingState> OnTrackingStateChanged;
        public event Action<LightEstimation> OnLightEstimationUpdated;
        public event Action OnARNotSupported;
        public event Action<string> OnARError;

        // Properties
        public bool IsSessionRunning => isSessionRunning;
        public bool IsARSupported => isARSupported;
        public bool IsTracking => trackingState == TrackingState.Tracking;
        public ARSessionState CurrentSessionState => currentState;
        public TrackingState CurrentTrackingState => trackingState;
        public Camera ActiveCamera => (isARSupported && arCamera != null && arCamera.enabled) ? arCamera : fallbackCamera;

        public enum ARSessionQuality
        {
            Low,
            Medium,
            High
        }

        [Serializable]
        public struct LightEstimation
        {
            public float brightness;
            public float colorTemperature;
            public Color colorCorrection;
            public Vector3 mainLightDirection;
            public float mainLightIntensity;
            public Color mainLightColor;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            FindARComponents();
        }

        private void Start()
        {
            if (autoStartSession)
            {
                StartARSession();
            }
        }

        private void OnEnable()
        {
            ARSession.stateChanged += HandleSessionStateChanged;
            
            if (arCameraManager != null)
            {
                arCameraManager.frameReceived += HandleFrameReceived;
            }
        }

        private void OnDisable()
        {
            ARSession.stateChanged -= HandleSessionStateChanged;
            
            if (arCameraManager != null)
            {
                arCameraManager.frameReceived -= HandleFrameReceived;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PauseSession();
            }
            else
            {
                ResumeSession();
            }
        }

        #region Component Discovery

        private void FindARComponents()
        {
            if (arSession == null)
            {
                arSession = FindObjectOfType<ARSession>();
            }

            if (arCameraManager == null)
            {
                arCameraManager = FindObjectOfType<ARCameraManager>();
            }

            if (arCamera == null && arCameraManager != null)
            {
                arCamera = arCameraManager.GetComponent<Camera>();
            }

            if (fallbackCamera == null)
            {
                // Find a camera tagged as fallback or create one
                var cameras = FindObjectsOfType<Camera>();
                foreach (var cam in cameras)
                {
                    if (cam != arCamera && cam.gameObject.name.Contains("Fallback"))
                    {
                        fallbackCamera = cam;
                        break;
                    }
                }
            }
        }

        #endregion

        #region Session Control

        /// <summary>
        /// Start the AR session
        /// </summary>
        public void StartARSession()
        {
            if (isSessionRunning)
            {
                Debug.LogWarning("[ARSessionController] Session already running");
                return;
            }

            StartCoroutine(InitializeSession());
        }

        private IEnumerator InitializeSession()
        {
            Debug.Log("[ARSessionController] Initializing AR session...");
            ShowUI(initializingUI);

            // Check AR availability
            if (ARSession.state == ARSessionState.None ||
                ARSession.state == ARSessionState.CheckingAvailability)
            {
                yield return ARSession.CheckAvailability();
            }

            if (ARSession.state == ARSessionState.Unsupported)
            {
                Debug.LogWarning("[ARSessionController] AR is not supported on this device");
                isARSupported = false;
                OnARNotSupported?.Invoke();
                
                if (enableFallbackCamera)
                {
                    EnableFallbackMode();
                }
                else
                {
                    ShowUI(errorUI);
                }
                yield break;
            }

            if (ARSession.state == ARSessionState.NeedsInstall)
            {
                Debug.Log("[ARSessionController] AR software needs to be installed");
                yield return ARSession.Install();
                
                if (ARSession.state == ARSessionState.NeedsInstall)
                {
                    OnARError?.Invoke("AR software installation required");
                    ShowUI(errorUI);
                    yield break;
                }
            }

            // Enable the AR session
            if (arSession != null)
            {
                arSession.enabled = true;
            }

            // Apply quality settings
            ApplyQualitySettings();

            // Wait for session to start tracking
            float elapsed = 0f;
            while (ARSession.state != ARSessionState.SessionTracking && elapsed < initializationTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (ARSession.state == ARSessionState.SessionTracking)
            {
                isSessionRunning = true;
                OnSessionStarted?.Invoke();
                HideAllUI();
                Debug.Log("[ARSessionController] AR session started successfully");
            }
            else
            {
                Debug.LogWarning($"[ARSessionController] Session initialization timed out (state: {ARSession.state})");
                
                if (enableFallbackCamera)
                {
                    EnableFallbackMode();
                }
            }
        }

        /// <summary>
        /// Stop the AR session
        /// </summary>
        public void StopSession()
        {
            if (!isSessionRunning) return;

            if (arSession != null)
            {
                arSession.enabled = false;
            }

            isSessionRunning = false;
            Debug.Log("[ARSessionController] AR session stopped");
        }

        /// <summary>
        /// Pause the AR session
        /// </summary>
        public void PauseSession()
        {
            if (!isSessionRunning) return;

            if (arSession != null)
            {
                arSession.enabled = false;
            }

            OnSessionPaused?.Invoke();
            Debug.Log("[ARSessionController] AR session paused");
        }

        /// <summary>
        /// Resume the AR session
        /// </summary>
        public void ResumeSession()
        {
            if (!isSessionRunning) return;

            if (arSession != null)
            {
                arSession.enabled = true;
            }

            OnSessionResumed?.Invoke();
            Debug.Log("[ARSessionController] AR session resumed");
        }

        /// <summary>
        /// Reset the AR session
        /// </summary>
        public void ResetSession()
        {
            if (arSession != null)
            {
                arSession.Reset();
            }

            Debug.Log("[ARSessionController] AR session reset");
        }

        #endregion

        #region Fallback Mode

        private void EnableFallbackMode()
        {
            Debug.Log("[ARSessionController] Enabling fallback camera mode");

            // Disable AR camera
            if (arCamera != null)
            {
                arCamera.enabled = false;
            }

            // Enable fallback camera
            if (fallbackCamera != null)
            {
                fallbackCamera.enabled = true;
                fallbackCamera.gameObject.SetActive(true);
            }
            else
            {
                // Create a basic fallback camera
                CreateFallbackCamera();
            }

            HideAllUI();
            
            // Consider session "running" in fallback mode
            isSessionRunning = true;
            isARSupported = false;
            OnSessionStarted?.Invoke();
        }

        private void CreateFallbackCamera()
        {
            GameObject camObj = new GameObject("FallbackCamera");
            fallbackCamera = camObj.AddComponent<Camera>();
            
            // Position camera to look at origin
            camObj.transform.position = new Vector3(0, 2, -5);
            camObj.transform.LookAt(Vector3.zero);

            // Add basic controls
            var controller = camObj.AddComponent<SimpleCameraController>();
            
            Debug.Log("[ARSessionController] Created fallback camera");
        }

        #endregion

        #region Event Handlers

        private void HandleSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            ARSessionState previousState = currentState;
            currentState = args.state;

            OnSessionStateChanged?.Invoke(currentState);

            Debug.Log($"[ARSessionController] Session state changed: {previousState} -> {currentState}");

            switch (currentState)
            {
                case ARSessionState.SessionInitializing:
                    ShowUI(initializingUI);
                    break;

                case ARSessionState.SessionTracking:
                    HideAllUI();
                    trackingLostTime = 0f;
                    OnSessionInitialized?.Invoke();
                    break;

                case ARSessionState.Unsupported:
                    isARSupported = false;
                    OnARNotSupported?.Invoke();
                    break;
            }
        }

        private void HandleFrameReceived(ARCameraFrameEventArgs args)
        {
            // Update tracking state
            TrackingState newTrackingState = TrackingState.None;
            
            if (arCamera != null)
            {
                // Get tracking state from session
                if (ARSession.state == ARSessionState.SessionTracking)
                {
                    newTrackingState = TrackingState.Tracking;
                }
                else if (ARSession.state == ARSessionState.SessionInitializing)
                {
                    newTrackingState = TrackingState.Limited;
                }
            }

            if (newTrackingState != trackingState)
            {
                trackingState = newTrackingState;
                OnTrackingStateChanged?.Invoke(trackingState);
                UpdateTrackingUI();
            }

            // Handle tracking recovery
            if (trackingState != TrackingState.Tracking)
            {
                trackingLostTime += Time.deltaTime;
                
                if (resetOnTrackingLoss && trackingLostTime > trackingRecoveryTimeout)
                {
                    Debug.Log("[ARSessionController] Tracking lost for too long, resetting session");
                    ResetSession();
                    trackingLostTime = 0f;
                }
            }
            else
            {
                trackingLostTime = 0f;
            }

            // Process light estimation
            if (enableLightEstimation && args.lightEstimation.averageBrightness.HasValue)
            {
                LightEstimation estimation = new LightEstimation
                {
                    brightness = args.lightEstimation.averageBrightness ?? 1f,
                    colorTemperature = args.lightEstimation.averageColorTemperature ?? 6500f,
                    colorCorrection = args.lightEstimation.colorCorrection ?? Color.white
                };

                OnLightEstimationUpdated?.Invoke(estimation);
            }
        }

        #endregion

        #region Quality Settings

        private void ApplyQualitySettings()
        {
            if (arCameraManager == null) return;

            switch (sessionQuality)
            {
                case ARSessionQuality.Low:
                    // Lower quality for better performance
                    if (arCameraManager != null)
                    {
                        arCameraManager.autoFocusRequested = false;
                    }
                    break;

                case ARSessionQuality.Medium:
                    if (arCameraManager != null)
                    {
                        arCameraManager.autoFocusRequested = true;
                    }
                    break;

                case ARSessionQuality.High:
                    if (arCameraManager != null)
                    {
                        arCameraManager.autoFocusRequested = true;
                    }
                    break;
            }
        }

        /// <summary>
        /// Set session quality level
        /// </summary>
        public void SetQuality(ARSessionQuality quality)
        {
            sessionQuality = quality;
            ApplyQualitySettings();
        }

        /// <summary>
        /// Enable or disable light estimation
        /// </summary>
        public void SetLightEstimation(bool enabled)
        {
            enableLightEstimation = enabled;
            
            if (arCameraManager != null)
            {
                arCameraManager.requestedLightEstimation = enabled 
                    ? LightEstimationMode.AmbientIntensity | LightEstimationMode.AmbientColor
                    : LightEstimationMode.Disabled;
            }
        }

        #endregion

        #region UI Management

        private void ShowUI(GameObject ui)
        {
            HideAllUI();
            
            if (ui != null)
            {
                ui.SetActive(true);
            }
        }

        private void HideAllUI()
        {
            if (trackingUI != null) trackingUI.SetActive(false);
            if (initializingUI != null) initializingUI.SetActive(false);
            if (errorUI != null) errorUI.SetActive(false);
        }

        private void UpdateTrackingUI()
        {
            if (trackingUI != null)
            {
                trackingUI.SetActive(trackingState != TrackingState.Tracking && isSessionRunning);
            }
        }

        #endregion

        #region Public Utilities

        /// <summary>
        /// Check if AR is available on this device
        /// </summary>
        public static bool CheckARAvailability()
        {
            return ARSession.state != ARSessionState.Unsupported &&
                   ARSession.state != ARSessionState.None;
        }

        /// <summary>
        /// Get current frame rate
        /// </summary>
        public float GetCurrentFrameRate()
        {
            return 1f / Time.deltaTime;
        }

        /// <summary>
        /// Toggle AR/Fallback mode
        /// </summary>
        public void ToggleARMode()
        {
            if (isARSupported)
            {
                if (arCamera != null && arCamera.enabled)
                {
                    EnableFallbackMode();
                }
                else
                {
                    // Re-enable AR
                    if (arCamera != null)
                    {
                        arCamera.enabled = true;
                    }
                    if (fallbackCamera != null)
                    {
                        fallbackCamera.enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Request camera permission (mobile)
        /// </summary>
        public void RequestCameraPermission(Action<bool> callback)
        {
            #if UNITY_ANDROID
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            }
            callback?.Invoke(true); // Assume granted for now
            #elif UNITY_IOS
            callback?.Invoke(true); // iOS handles this automatically
            #else
            callback?.Invoke(true);
            #endif
        }

        #endregion
    }

    /// <summary>
    /// Simple camera controller for fallback mode
    /// </summary>
    public class SimpleCameraController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float lookSpeed = 2f;
        [SerializeField] private float zoomSpeed = 5f;

        private Vector3 lastMousePosition;

        private void Update()
        {
            // WASD movement
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            Vector3 movement = (transform.right * horizontal + transform.forward * vertical) * moveSpeed * Time.deltaTime;
            
            // Up/Down with Q/E
            if (Input.GetKey(KeyCode.Q)) movement.y -= moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) movement.y += moveSpeed * Time.deltaTime;
            
            transform.position += movement;

            // Mouse look
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
                float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
                
                transform.eulerAngles += new Vector3(-mouseY, mouseX, 0);
            }

            // Scroll zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                transform.position += transform.forward * scroll * zoomSpeed;
            }
        }
    }
}
