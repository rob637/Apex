using System;
using System.Collections.Generic;
using UnityEngine;
using ApexCitadels.PC.WebGL;

namespace ApexCitadels.PC
{
    /// <summary>
    /// PC-specific camera controller supporting multiple view modes:
    /// - World Map: Strategic overhead view
    /// - Territory: Zoomed view of a specific territory
    /// - First Person: Walk through your citadel
    /// - Cinematic: Auto-tour of your empire
    /// </summary>
    public class PCCameraController : MonoBehaviour
    {
        public static PCCameraController Instance { get; private set; }

        [Header("Camera Modes")]
        [SerializeField] private PCCameraMode defaultMode = PCCameraMode.WorldMap;

        [Header("World Map Settings")]
        [SerializeField] private float worldMapHeight = 200f;  // Start closer to see territories
        [SerializeField] private float worldMapMinHeight = 50f;
        [SerializeField] private float worldMapMaxHeight = 2000f;
        [SerializeField] private float worldMapPanSpeed = 150f;
        [SerializeField] private float worldMapZoomSpeed = 100f;

        [Header("Territory View Settings")]
        [SerializeField] private float territoryViewHeight = 50f;
        [SerializeField] private float territoryViewMinHeight = 20f;
        [SerializeField] private float territoryViewMaxHeight = 200f;
        [SerializeField] private float territoryOrbitSpeed = 2f;
        [SerializeField] private float territoryZoomSpeed = 10f;

        [Header("First Person Settings")]
        [SerializeField] private float fpMoveSpeed = 5f;
        [SerializeField] private float fpSprintMultiplier = 2f;
        [SerializeField] private float fpLookSensitivity = 2f;
        [SerializeField] private float fpMaxLookAngle = 85f;

        [Header("Cinematic Settings")]
        [SerializeField] private float cinematicSpeed = 5f;
        [SerializeField] private float cinematicTransitionDuration = 2f;

        [Header("Smooth Movement")]
        [SerializeField] private float smoothTime = 0.1f;
        [SerializeField] private float smoothZoomTime = 0.2f;

        // Events
        public event Action<PCCameraMode> OnModeChanged;
        public event Action<Vector3> OnPositionChanged;
        public event Action<string> OnTerritoryFocused;

        // State
        private PCCameraMode _currentMode;
        private Camera _camera;
        private string _focusedTerritoryId;
        private Vector3 _targetPosition;
        private Vector3 _velocity = Vector3.zero;
        private float _targetZoom;
        private float _zoomVelocity;

        // First person state
        private float _fpRotationX;
        private float _fpRotationY;

        // Territory view state
        private float _orbitAngle;
        private Vector3 _orbitCenter;

        // Cinematic state
        private bool _cinematicPlaying;
        private int _cinematicWaypointIndex;
        private Vector3[] _cinematicWaypoints;

        public PCCameraMode CurrentMode => _currentMode;
        public string FocusedTerritoryId => _focusedTerritoryId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = gameObject.AddComponent<Camera>();
            }

            // Setup camera defaults
            _camera.clearFlags = CameraClearFlags.Skybox;
            _camera.nearClipPlane = 0.3f;
            _camera.farClipPlane = 10000f;  // Far enough to see distant territories
        }

        private void Start()
        {
            SetMode(defaultMode);
        }

        private void Update()
        {
            // Allow camera control on PC, Editor, and WebGL
            if (!PlatformManager.HasKeyboardMouse)
                return;

            switch (_currentMode)
            {
                case PCCameraMode.WorldMap:
                    UpdateWorldMapMode();
                    break;
                case PCCameraMode.Territory:
                    UpdateTerritoryMode();
                    break;
                case PCCameraMode.FirstPerson:
                    UpdateFirstPersonMode();
                    break;
                case PCCameraMode.Cinematic:
                    UpdateCinematicMode();
                    break;
            }

            // Smooth position interpolation
            transform.position = Vector3.SmoothDamp(
                transform.position, _targetPosition, ref _velocity, smoothTime);

            OnPositionChanged?.Invoke(transform.position);
        }

        #region Mode Management

        /// <summary>
        /// Set the camera mode
        /// </summary>
        public void SetMode(PCCameraMode mode)
        {
            PCCameraMode previousMode = _currentMode;
            _currentMode = mode;

            switch (mode)
            {
                case PCCameraMode.WorldMap:
                    InitializeWorldMapMode();
                    break;
                case PCCameraMode.Territory:
                    InitializeTerritoryMode();
                    break;
                case PCCameraMode.FirstPerson:
                    InitializeFirstPersonMode();
                    break;
                case PCCameraMode.Cinematic:
                    InitializeCinematicMode();
                    break;
            }

            Debug.Log($"[PCCamera] Mode changed: {previousMode} -> {mode}");
            OnModeChanged?.Invoke(mode);
        }

        /// <summary>
        /// Enter World Map view
        /// </summary>
        public void EnterWorldMapMode()
        {
            SetMode(PCCameraMode.WorldMap);
        }

        /// <summary>
        /// Enter Territory detail view
        /// </summary>
        public void EnterTerritoryMode(string territoryId)
        {
            _focusedTerritoryId = territoryId;
            SetMode(PCCameraMode.Territory);
            OnTerritoryFocused?.Invoke(territoryId);
        }

        /// <summary>
        /// Enter First Person walkthrough mode
        /// </summary>
        public void EnterFirstPersonMode()
        {
            SetMode(PCCameraMode.FirstPerson);
        }

        /// <summary>
        /// Enter Cinematic auto-tour mode
        /// </summary>
        public void EnterCinematicMode()
        {
            SetMode(PCCameraMode.Cinematic);
        }

        /// <summary>
        /// Toggle between world map and territory view
        /// </summary>
        public void ToggleMapTerritoryView()
        {
            if (_currentMode == PCCameraMode.WorldMap && !string.IsNullOrEmpty(_focusedTerritoryId))
            {
                SetMode(PCCameraMode.Territory);
            }
            else
            {
                SetMode(PCCameraMode.WorldMap);
            }
        }

        #endregion

        #region World Map Mode

        private void InitializeWorldMapMode()
        {
            // Position camera looking down at world at an angle (not straight down)
            _targetPosition = new Vector3(0, worldMapHeight, -worldMapHeight * 0.5f);
            _targetZoom = worldMapHeight;
            // Look down at 60 degrees instead of 90 - this lets you see the beacons
            transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            transform.position = _targetPosition;
            
            // Set sky color
            if (_camera != null)
            {
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = new Color(0.4f, 0.6f, 0.9f, 1f); // Light blue sky
            }
        }

        private void UpdateWorldMapMode()
        {
            // WASD / Arrow keys for panning
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // Pan in the direction the camera is facing (adjusted for the angled view)
            Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 right = new Vector3(transform.right.x, 0, transform.right.z).normalized;
            Vector3 panDirection = (right * horizontal + forward * vertical).normalized;
            _targetPosition += panDirection * worldMapPanSpeed * Time.deltaTime;

            // Mouse scroll for zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _targetZoom -= scroll * worldMapZoomSpeed * _targetZoom;
                _targetZoom = Mathf.Clamp(_targetZoom, worldMapMinHeight, worldMapMaxHeight);
            }
            
            // Update camera position based on zoom (maintain angled view)
            _targetPosition.y = _targetZoom;
            _targetPosition.z = -_targetZoom * 0.5f + _targetPosition.z - transform.position.z + _targetZoom * 0.5f;
            
            // Smoothly move camera
            transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _velocity, smoothTime);

            // Middle mouse drag for panning
            if (Input.GetMouseButton(2))
            {
                float mouseX = -Input.GetAxis("Mouse X") * worldMapPanSpeed * 0.5f;
                float mouseY = -Input.GetAxis("Mouse Y") * worldMapPanSpeed * 0.5f;
                _targetPosition += right * mouseX + forward * mouseY;
            }

            // Click to focus on territory (handled by input manager)
        }

        #endregion

        #region Territory Mode

        private void InitializeTerritoryMode()
        {
            // Get territory center position (would come from TerritoryManager)
            _orbitCenter = GetTerritoryWorldPosition(_focusedTerritoryId);
            _orbitAngle = 0f;
            _targetZoom = territoryViewHeight;

            UpdateOrbitPosition();
        }

        private void UpdateTerritoryMode()
        {
            // Right mouse drag to orbit
            if (Input.GetMouseButton(1))
            {
                _orbitAngle += Input.GetAxis("Mouse X") * territoryOrbitSpeed;
            }

            // Scroll to zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _targetZoom -= scroll * territoryZoomSpeed * _targetZoom;
                _targetZoom = Mathf.Clamp(_targetZoom, territoryViewMinHeight, territoryViewMaxHeight);
            }

            UpdateOrbitPosition();
        }

        private void UpdateOrbitPosition()
        {
            // Calculate orbit position around territory center
            float x = Mathf.Sin(_orbitAngle * Mathf.Deg2Rad) * _targetZoom;
            float z = Mathf.Cos(_orbitAngle * Mathf.Deg2Rad) * _targetZoom;

            _targetPosition = _orbitCenter + new Vector3(x, _targetZoom * 0.5f, z);
            transform.LookAt(_orbitCenter);
        }

        private Vector3 GetTerritoryWorldPosition(string territoryId)
        {
            // TODO: Integrate with TerritoryManager to get actual position
            // For now return world origin
            return Vector3.zero;
        }

        #endregion

        #region First Person Mode

        private void InitializeFirstPersonMode()
        {
            // Start at current position but at eye level
            _targetPosition = new Vector3(
                transform.position.x,
                1.8f, // Eye height
                transform.position.z
            );

            Vector3 euler = transform.eulerAngles;
            _fpRotationX = euler.y;
            _fpRotationY = 0f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void UpdateFirstPersonMode()
        {
            // Mouse look
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                _fpRotationX += Input.GetAxis("Mouse X") * fpLookSensitivity;
                _fpRotationY -= Input.GetAxis("Mouse Y") * fpLookSensitivity;
                _fpRotationY = Mathf.Clamp(_fpRotationY, -fpMaxLookAngle, fpMaxLookAngle);

                transform.rotation = Quaternion.Euler(_fpRotationY, _fpRotationX, 0f);
            }

            // WASD movement
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            float speed = fpMoveSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                speed *= fpSprintMultiplier;
            }

            Vector3 move = transform.right * horizontal + transform.forward * vertical;
            move.y = 0; // Keep on ground plane
            move = move.normalized * speed * Time.deltaTime;

            _targetPosition += move;
            _targetPosition.y = 1.8f; // Keep at eye height

            // E/Q for up/down
            if (Input.GetKey(KeyCode.E)) _targetPosition.y += speed * Time.deltaTime;
            if (Input.GetKey(KeyCode.Q)) _targetPosition.y -= speed * Time.deltaTime;

            // Escape to release cursor
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Click to re-lock
            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        #endregion

        #region Cinematic Mode

        private void InitializeCinematicMode()
        {
            _cinematicPlaying = true;
            _cinematicWaypointIndex = 0;

            // TODO: Get waypoints from owned territories
            // For now create a simple circular path
            _cinematicWaypoints = GenerateCinematicPath();

            if (_cinematicWaypoints.Length > 0)
            {
                _targetPosition = _cinematicWaypoints[0];
            }
        }

        private void UpdateCinematicMode()
        {
            if (!_cinematicPlaying || _cinematicWaypoints == null || _cinematicWaypoints.Length == 0)
                return;

            // Move towards current waypoint
            Vector3 currentWaypoint = _cinematicWaypoints[_cinematicWaypointIndex];
            _targetPosition = Vector3.MoveTowards(_targetPosition, currentWaypoint, cinematicSpeed * Time.deltaTime);

            // Look at waypoint
            if (Vector3.Distance(transform.position, currentWaypoint) > 0.1f)
            {
                Vector3 lookDir = (currentWaypoint - transform.position).normalized;
                if (lookDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
                }
            }

            // Check if reached waypoint
            if (Vector3.Distance(transform.position, currentWaypoint) < 1f)
            {
                _cinematicWaypointIndex++;
                if (_cinematicWaypointIndex >= _cinematicWaypoints.Length)
                {
                    _cinematicWaypointIndex = 0; // Loop
                }
            }

            // Any input exits cinematic mode
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                _cinematicPlaying = false;
                SetMode(PCCameraMode.WorldMap);
            }
        }

        private Vector3[] GenerateCinematicPath()
        {
            // Try to get territories from WorldMapRenderer
            if (WorldMapRenderer.Instance != null && WorldMapRenderer.Instance.AllTerritories != null && WorldMapRenderer.Instance.AllTerritories.Count > 0)
            {
                var territories = WorldMapRenderer.Instance.AllTerritories;
                int count = Mathf.Min(territories.Count, 10);
                Vector3[] path = new Vector3[count];

                for (int i = 0; i < count; i++)
                {
                    var t = territories[i];
                    Vector3 worldPos = WorldMapRenderer.Instance.GPSToWorldPosition(t.latitude, t.longitude);
                    worldPos.y += 50f; // Height above territory
                    path[i] = worldPos;
                }
                
                return path;
            }

            // Generate a circular tour path (Fallback)
            int numPoints = 8;
            Vector3[] circularPath = new Vector3[numPoints];
            float radius = 100f;
            float height = 50f;

            for (int i = 0; i < numPoints; i++)
            {
                float angle = (i / (float)numPoints) * Mathf.PI * 2;
                circularPath[i] = new Vector3(
                    Mathf.Cos(angle) * radius,
                    height,
                    Mathf.Sin(angle) * radius
                );
            }

            return circularPath;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Focus camera on a specific world position
        /// </summary>
        public void FocusOnPosition(Vector3 position)
        {
            if (_currentMode == PCCameraMode.WorldMap)
            {
                _targetPosition = new Vector3(position.x, _targetZoom, position.z);
            }
            else if (_currentMode == PCCameraMode.Territory)
            {
                _orbitCenter = position;
                UpdateOrbitPosition();
            }
        }

        /// <summary>
        /// Focus camera on a GPS coordinate
        /// </summary>
        public void FocusOnGPS(double latitude, double longitude)
        {
            // Convert GPS to world position (simplified - would need proper geo conversion)
            Vector3 worldPos = GPSToWorldPosition(latitude, longitude);
            FocusOnPosition(worldPos);
        }

        /// <summary>
        /// Convert GPS coordinates to Unity world position
        /// </summary>
        private Vector3 GPSToWorldPosition(double latitude, double longitude)
        {
            // Simplified conversion - in real implementation, use proper projection
            // Using reference point at (0,0) = some central location
            const double metersPerDegLat = 111319.9;
            const double metersPerDegLon = 111319.9; // Approximate at equator

            float x = (float)((longitude) * metersPerDegLon);
            float z = (float)((latitude) * metersPerDegLat);

            return new Vector3(x, 0, z);
        }

        /// <summary>
        /// Get current camera bounds in world space
        /// </summary>
        public Bounds GetViewBounds()
        {
            if (_currentMode == PCCameraMode.WorldMap)
            {
                // Calculate visible area from height
                float halfWidth = _targetZoom * _camera.aspect;
                float halfHeight = _targetZoom;
                return new Bounds(
                    new Vector3(_targetPosition.x, 0, _targetPosition.z),
                    new Vector3(halfWidth * 2, 10, halfHeight * 2)
                );
            }

            return new Bounds(transform.position, Vector3.one * 100);
        }

        #endregion
    }

    /// <summary>
    /// Camera modes for PC client
    /// </summary>
    public enum PCCameraMode
    {
        WorldMap,       // Strategic overhead view
        Territory,      // Zoomed view of specific territory
        FirstPerson,    // Walk through citadel
        Cinematic       // Auto-tour
    }
}
