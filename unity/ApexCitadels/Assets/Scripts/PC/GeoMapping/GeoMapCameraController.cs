// ============================================================================
// APEX CITADELS - GEO MAP CAMERA CONTROLLER
// Camera controls specifically for real-world map navigation
// Supports pan, zoom, rotation, and location-based movement
// ============================================================================
using System;
using UnityEngine;
using ApexCitadels.PC.GeoMapping;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Camera controller for the real-world map view.
    /// Provides intuitive controls for exploring the real-world geography.
    /// </summary>
    public class GeoMapCameraController : MonoBehaviour
    {
        public static GeoMapCameraController Instance { get; private set; }

        [Header("Camera References")]
        [SerializeField] private Camera mapCamera;

        [Header("Movement")]
        [SerializeField] private float panSpeed = 100f;
        [SerializeField] private float panSpeedKeyboard = 200f;
        [SerializeField] private float panSmoothing = 8f;
        [SerializeField] private float panBorderThickness = 20f; // Screen edge panning
        [SerializeField] private bool enableEdgePan = true;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 50f;
        [SerializeField] private float zoomSmoothing = 8f;
        [SerializeField] private float minHeight = 50f;
        [SerializeField] private float maxHeight = 2000f;
        [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private float rotationSmoothing = 8f;
        [SerializeField] private float minPitch = 20f;
        [SerializeField] private float maxPitch = 85f;

        [Header("Initial Position")]
        [SerializeField] private float initialHeight = 300f;
        [SerializeField] private float initialPitch = 60f;
        [SerializeField] private float initialYaw = 0f;

        [Header("Boundaries")]
        [SerializeField] private bool enableBoundaries = false;
        [SerializeField] private float maxDistanceFromCenter = 10000f;

        // Current state
        private Vector3 _targetPosition;
        private float _targetHeight;
        private float _targetPitch;
        private float _targetYaw;
        private Vector3 _velocity = Vector3.zero;
        private float _heightVelocity;
        private float _pitchVelocity;
        private float _yawVelocity;

        // Input state
        private Vector3 _lastMousePosition;
        private bool _isPanning;
        private bool _isRotating;

        // Reference to map renderer
        private RealWorldMapRenderer _mapRenderer;

        // Events
        public event Action<float> OnZoomChanged;
        public event Action<GeoCoordinate> OnPositionChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (mapCamera == null)
            {
                mapCamera = GetComponent<Camera>();
                if (mapCamera == null)
                {
                    mapCamera = Camera.main;
                }
            }
        }

        private void Start()
        {
            _mapRenderer = RealWorldMapRenderer.Instance;

            // Initialize camera position
            _targetPosition = Vector3.zero;
            _targetHeight = initialHeight;
            _targetPitch = initialPitch;
            _targetYaw = initialYaw;

            // Snap to initial position
            ApplyCameraTransform(true);
        }

        private void Update()
        {
            if (mapCamera == null) return;

            HandleKeyboardInput();
            HandleMouseInput();
            HandleScrollZoom();
            HandleEdgePan();

            // Smooth camera movement
            ApplyCameraTransform(false);
        }

        private void HandleKeyboardInput()
        {
            // WASD / Arrow key movement
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            if (horizontal != 0 || vertical != 0)
            {
                // Move relative to camera rotation (yaw only)
                Vector3 forward = Quaternion.Euler(0, _targetYaw, 0) * Vector3.forward;
                Vector3 right = Quaternion.Euler(0, _targetYaw, 0) * Vector3.right;

                // Speed scales with height (move faster when zoomed out)
                float speedMultiplier = Mathf.Lerp(0.5f, 3f, (_targetHeight - minHeight) / (maxHeight - minHeight));
                Vector3 movement = (forward * vertical + right * horizontal) * panSpeedKeyboard * speedMultiplier * Time.deltaTime;
                
                _targetPosition += movement;
            }

            // Q/E for rotation
            if (Input.GetKey(KeyCode.Q))
            {
                _targetYaw -= rotationSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.E))
            {
                _targetYaw += rotationSpeed * Time.deltaTime;
            }

            // R/F for pitch
            if (Input.GetKey(KeyCode.R))
            {
                _targetPitch = Mathf.Clamp(_targetPitch - rotationSpeed * Time.deltaTime, minPitch, maxPitch);
            }
            if (Input.GetKey(KeyCode.F))
            {
                _targetPitch = Mathf.Clamp(_targetPitch + rotationSpeed * Time.deltaTime, minPitch, maxPitch);
            }

            // Page Up/Down for zoom
            if (Input.GetKey(KeyCode.PageUp))
            {
                _targetHeight = Mathf.Clamp(_targetHeight - zoomSpeed * Time.deltaTime, minHeight, maxHeight);
            }
            if (Input.GetKey(KeyCode.PageDown))
            {
                _targetHeight = Mathf.Clamp(_targetHeight + zoomSpeed * Time.deltaTime, minHeight, maxHeight);
            }

            // Home to reset view
            if (Input.GetKeyDown(KeyCode.Home))
            {
                ResetView();
            }
        }

        private void HandleMouseInput()
        {
            // Middle mouse button or Ctrl + Left mouse for panning
            bool panButton = Input.GetMouseButton(2) || (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftControl));
            
            // Right mouse button for rotation
            bool rotateButton = Input.GetMouseButton(1);

            if (panButton)
            {
                if (!_isPanning)
                {
                    _isPanning = true;
                    _lastMousePosition = Input.mousePosition;
                }
                else
                {
                    Vector3 delta = Input.mousePosition - _lastMousePosition;
                    _lastMousePosition = Input.mousePosition;

                    // Convert screen delta to world movement
                    Vector3 forward = Quaternion.Euler(0, _targetYaw, 0) * Vector3.forward;
                    Vector3 right = Quaternion.Euler(0, _targetYaw, 0) * Vector3.right;

                    float speedMultiplier = _targetHeight / 100f; // Scale pan speed with zoom
                    _targetPosition -= (right * delta.x + forward * delta.y) * panSpeed * speedMultiplier * 0.01f;
                }
            }
            else
            {
                _isPanning = false;
            }

            if (rotateButton)
            {
                if (!_isRotating)
                {
                    _isRotating = true;
                    _lastMousePosition = Input.mousePosition;
                }
                else
                {
                    Vector3 delta = Input.mousePosition - _lastMousePosition;
                    _lastMousePosition = Input.mousePosition;

                    _targetYaw += delta.x * rotationSpeed * 0.1f;
                    _targetPitch = Mathf.Clamp(_targetPitch - delta.y * rotationSpeed * 0.1f, minPitch, maxPitch);
                }
            }
            else
            {
                _isRotating = false;
            }
        }

        private void HandleScrollZoom()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // Zoom towards mouse position
                float zoomAmount = scroll * zoomSpeed * Mathf.Max(1f, _targetHeight / 100f);
                _targetHeight = Mathf.Clamp(_targetHeight - zoomAmount, minHeight, maxHeight);

                OnZoomChanged?.Invoke(_targetHeight);
            }
        }

        private void HandleEdgePan()
        {
            if (!enableEdgePan || _isPanning || _isRotating) return;

            // Check if mouse is near screen edges
            Vector3 mousePos = Input.mousePosition;
            Vector3 panDirection = Vector3.zero;

            if (mousePos.x < panBorderThickness)
                panDirection.x = -1;
            else if (mousePos.x > Screen.width - panBorderThickness)
                panDirection.x = 1;

            if (mousePos.y < panBorderThickness)
                panDirection.z = -1;
            else if (mousePos.y > Screen.height - panBorderThickness)
                panDirection.z = 1;

            if (panDirection != Vector3.zero)
            {
                Vector3 forward = Quaternion.Euler(0, _targetYaw, 0) * Vector3.forward;
                Vector3 right = Quaternion.Euler(0, _targetYaw, 0) * Vector3.right;

                float speedMultiplier = _targetHeight / 100f;
                Vector3 movement = (forward * panDirection.z + right * panDirection.x) * panSpeed * speedMultiplier * Time.deltaTime;
                _targetPosition += movement;
            }
        }

        private void ApplyCameraTransform(bool instant)
        {
            if (instant)
            {
                mapCamera.transform.position = CalculateCameraPosition(_targetPosition, _targetHeight, _targetPitch, _targetYaw);
                mapCamera.transform.rotation = Quaternion.Euler(_targetPitch, _targetYaw, 0);
            }
            else
            {
                // Smooth position
                Vector3 currentTarget = mapCamera.transform.position;
                Vector3 desiredPosition = CalculateCameraPosition(_targetPosition, _targetHeight, _targetPitch, _targetYaw);
                mapCamera.transform.position = Vector3.SmoothDamp(currentTarget, desiredPosition, ref _velocity, 1f / panSmoothing);

                // Smooth rotation
                Quaternion currentRotation = mapCamera.transform.rotation;
                Quaternion desiredRotation = Quaternion.Euler(_targetPitch, _targetYaw, 0);
                mapCamera.transform.rotation = Quaternion.Slerp(currentRotation, desiredRotation, Time.deltaTime * rotationSmoothing);
            }

            // Enforce boundaries
            if (enableBoundaries)
            {
                Vector3 pos = mapCamera.transform.position;
                float distance = new Vector2(pos.x, pos.z).magnitude;
                if (distance > maxDistanceFromCenter)
                {
                    Vector2 dir = new Vector2(pos.x, pos.z).normalized;
                    _targetPosition.x = dir.x * maxDistanceFromCenter;
                    _targetPosition.z = dir.y * maxDistanceFromCenter;
                }
            }
        }

        private Vector3 CalculateCameraPosition(Vector3 lookAtPoint, float height, float pitch, float yaw)
        {
            // Camera orbits around look-at point
            float pitchRad = pitch * Mathf.Deg2Rad;
            float yawRad = yaw * Mathf.Deg2Rad;

            float horizontalDistance = height / Mathf.Tan(pitchRad);
            
            float offsetX = -Mathf.Sin(yawRad) * horizontalDistance;
            float offsetZ = -Mathf.Cos(yawRad) * horizontalDistance;

            return new Vector3(
                lookAtPoint.x + offsetX,
                height,
                lookAtPoint.z + offsetZ
            );
        }

        #region Public API

        /// <summary>
        /// Move camera to look at a specific world position
        /// </summary>
        public void LookAt(Vector3 worldPosition)
        {
            _targetPosition = worldPosition;
            _targetPosition.y = 0; // Keep on ground plane
        }

        /// <summary>
        /// Move camera to look at a GPS coordinate
        /// </summary>
        public void LookAtGeo(GeoCoordinate coord)
        {
            Vector3 worldPos = GeoProjection.GeoToWorld(coord);
            LookAt(worldPos);
            OnPositionChanged?.Invoke(coord);
        }

        /// <summary>
        /// Set camera height (zoom level)
        /// </summary>
        public void SetHeight(float height)
        {
            _targetHeight = Mathf.Clamp(height, minHeight, maxHeight);
            OnZoomChanged?.Invoke(_targetHeight);
        }

        /// <summary>
        /// Set camera rotation
        /// </summary>
        public void SetRotation(float pitch, float yaw)
        {
            _targetPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            _targetYaw = yaw;
        }

        /// <summary>
        /// Reset to initial view
        /// </summary>
        public void ResetView()
        {
            _targetPosition = Vector3.zero;
            _targetHeight = initialHeight;
            _targetPitch = initialPitch;
            _targetYaw = initialYaw;

            if (_mapRenderer != null)
            {
                _mapRenderer.GoToLocation(_mapRenderer.CurrentCenter);
            }
        }

        /// <summary>
        /// Fit view to show all territories
        /// </summary>
        public void FitToTerritories()
        {
            if (_mapRenderer == null || _mapRenderer.Territories.Count == 0) return;

            // Calculate bounding box of all territories
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var territory in _mapRenderer.Territories)
            {
                var pos = GeoProjection.GeoToWorld(new GeoCoordinate(territory.latitude, territory.longitude));
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x);
                minZ = Mathf.Min(minZ, pos.z);
                maxZ = Mathf.Max(maxZ, pos.z);
            }

            // Center on bounding box
            _targetPosition = new Vector3((minX + maxX) / 2f, 0, (minZ + maxZ) / 2f);

            // Set height to see all territories
            float width = maxX - minX;
            float depth = maxZ - minZ;
            float size = Mathf.Max(width, depth);
            _targetHeight = Mathf.Clamp(size * 1.5f, minHeight, maxHeight);
        }

        /// <summary>
        /// Get current camera height
        /// </summary>
        public float Height => _targetHeight;

        /// <summary>
        /// Get current look-at position
        /// </summary>
        public Vector3 LookAtPosition => _targetPosition;

        /// <summary>
        /// Get current GPS coordinate being looked at
        /// </summary>
        public GeoCoordinate LookAtGeoCoordinate => GeoProjection.WorldToGeo(_targetPosition);

        #endregion
    }
}
