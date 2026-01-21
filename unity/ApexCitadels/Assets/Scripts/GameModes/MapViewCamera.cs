// ============================================================================
// APEX CITADELS - MAP VIEW CAMERA
// Controls camera movement in the overhead map view
// WASD/Arrow keys to move, mouse to look around
// ============================================================================
using UnityEngine;
using ApexCitadels.Map;

namespace ApexCitadels.GameModes
{
    /// <summary>
    /// Camera controller for Map View mode.
    /// Provides smooth flying over the Mapbox tiles.
    /// </summary>
    public class MapViewCamera : MonoBehaviour
    {
        #region Settings
        
        private float _height = 150f;
        private float _angle = 55f;
        private float _moveSpeed = 50f;
        
        [Header("Additional Settings")]
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private float smoothTime = 0.1f;
        [SerializeField] private float minHeight = 50f;
        [SerializeField] private float maxHeight = 300f; // Reduced from 500 to prevent losing map
        
        #endregion
        
        #region State
        
        private Vector3 _targetPosition;
        private Vector3 _velocity;
        private float _currentYaw = 0f;
        
        // Location tracking
        private double _latitude;
        private double _longitude;
        private MapboxTileRenderer _mapbox;
        
        // Meters per degree at equator (approximate)
        private const float MetersPerDegreeLat = 111320f;
        
        #endregion
        
        #region Properties
        
        public double CurrentLatitude => _latitude;
        public double CurrentLongitude => _longitude;
        
        #endregion
        
        #region Initialization
        
        public void Initialize(float height, float angle, float moveSpeed)
        {
            _height = height;
            _angle = angle;
            _moveSpeed = moveSpeed;
            
            // Set initial camera rotation
            transform.rotation = Quaternion.Euler(_angle, 0, 0);
            
            // Find Mapbox renderer
            _mapbox = FindAnyObjectByType<MapboxTileRenderer>();
        }
        
        public void SetPosition(double latitude, double longitude)
        {
            _latitude = latitude;
            _longitude = longitude;
            
            // Reset to center of map - use a reasonable height (not too high)
            // Clamp height to prevent camera from disappearing
            float useHeight = Mathf.Clamp(_height, 100f, 200f);
            _height = useHeight; // Also update internal height
            _targetPosition = new Vector3(0, useHeight, -useHeight * 0.5f);
            transform.position = _targetPosition;
            transform.rotation = Quaternion.Euler(_angle, 0, 0);
            _currentYaw = 0f;
            
            // Update Mapbox if available
            if (_mapbox != null)
            {
                _mapbox.SetCenter(latitude, longitude);
            }
            
            Debug.Log($"[MapCamera] Set position to {latitude}, {longitude}");
        }
        
        #endregion
        
        #region Update
        
        private void Update()
        {
            HandleMovement();
            HandleRotation();
            HandleZoom();
            UpdatePosition();
        }
        
        private void HandleMovement()
        {
            // Get input
            float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right
            float vertical = Input.GetAxis("Vertical");     // W/S or Up/Down
            
            if (Mathf.Abs(horizontal) < 0.01f && Mathf.Abs(vertical) < 0.01f)
                return;
            
            // Calculate movement direction based on camera yaw
            Vector3 forward = Quaternion.Euler(0, _currentYaw, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, _currentYaw, 0) * Vector3.right;
            
            Vector3 move = (forward * vertical + right * horizontal).normalized;
            
            // Apply movement
            float speed = _moveSpeed * Time.deltaTime;
            
            // Speed up when holding shift
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                speed *= 3f;
            }
            
            _targetPosition += move * speed;
            
            // Update lat/lon based on movement
            // Movement in Unity X = longitude, Z = latitude (roughly)
            float metersPerDegreeLon = MetersPerDegreeLat * Mathf.Cos((float)_latitude * Mathf.Deg2Rad);
            
            _longitude += (move.x * speed) / metersPerDegreeLon;
            _latitude += (move.z * speed) / MetersPerDegreeLat;
            
            // Update Mapbox tile center for streaming
            if (_mapbox != null && Time.frameCount % 30 == 0) // Update every 30 frames
            {
                _mapbox.UpdateCenterSmooth(_latitude, _longitude);
            }
        }
        
        private void HandleRotation()
        {
            // Right mouse button to rotate view
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
                _currentYaw += mouseX;
            }
            
            // Q/E to rotate
            if (Input.GetKey(KeyCode.Q))
            {
                _currentYaw -= 60f * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.E))
            {
                _currentYaw += 60f * Time.deltaTime;
            }
        }
        
        private void HandleZoom()
        {
            // DISABLED: Zoom locked at maximum detail level to reduce tile loading
            // and ensure consistent close-up view of buildings/landmarks
            return;
            // Mouse scroll to adjust height
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _height -= scroll * _height * 0.5f;
                _height = Mathf.Clamp(_height, minHeight, maxHeight);
                
                // Adjust position for new height
                _targetPosition.y = _height;
                _targetPosition.z = -_height * 0.5f;
            }
            
            // R/F for zoom
            if (Input.GetKey(KeyCode.R))
            {
                _height = Mathf.Max(minHeight, _height - 50f * Time.deltaTime);
            }
            if (Input.GetKey(KeyCode.F))
            {
                _height = Mathf.Min(maxHeight, _height + 50f * Time.deltaTime);
            }
        }
        
        private void UpdatePosition()
        {
            // Smooth movement
            Vector3 targetPos = new Vector3(_targetPosition.x, _height, _targetPosition.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime);
            
            // Apply rotation
            transform.rotation = Quaternion.Euler(_angle, _currentYaw, 0);
        }
        
        #endregion
        
        #region Public API
        
        public Vector3 GetCurrentWorldPosition()
        {
            return _targetPosition;
        }
        
        public void SetHeight(float height)
        {
            _height = Mathf.Clamp(height, minHeight, maxHeight);
        }
        
        #endregion
    }
}
