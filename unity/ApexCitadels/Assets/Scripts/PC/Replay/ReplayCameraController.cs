using Camera = UnityEngine.Camera;
using UnityEngine;
using System;
using System.Collections;
using ApexCitadels.Core;

namespace ApexCitadels.PC.Replay
{
    /// <summary>
    /// Replay Camera Controller - Cinematic camera for battle replays
    /// Supports free-look, follow unit, bird's eye, and cinematic auto-camera modes.
    /// </summary>
    public class ReplayCameraController : MonoBehaviour
    {
        [Header("Camera Reference")]
        [SerializeField] private Camera replayCamera;
        
        [Header("Free Look Settings")]
        [SerializeField] private float moveSpeed = 15f;
        [SerializeField] private float shiftMultiplier = 2.5f;
        [SerializeField] private float rotationSpeed = 3f;
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 100f;
        
        [Header("Follow Settings")]
        [SerializeField] private float followDistance = 8f;
        [SerializeField] private float followHeight = 5f;
        [SerializeField] private float followSmoothness = 5f;
        [SerializeField] private float followRotationSmooth = 3f;
        
        [Header("Birds Eye Settings")]
        [SerializeField] private float birdsEyeHeight = 50f;
        [SerializeField] private float birdsEyeMinHeight = 20f;
        [SerializeField] private float birdsEyeMaxHeight = 100f;
        
        [Header("Cinematic Settings")]
        [SerializeField] private float cinematicTransitionDuration = 1.5f;
        [SerializeField] private float cinematicHoldDuration = 3f;
        [SerializeField] private AnimationCurve cinematicCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Boundaries")]
        [SerializeField] private Vector3 boundaryMin = new Vector3(-100, 0, -100);
        [SerializeField] private Vector3 boundaryMax = new Vector3(100, 80, 100);
        
        // Singleton
        private static ReplayCameraController _instance;
        public static ReplayCameraController Instance => _instance;
        
        // State
        private CameraMode _currentMode = CameraMode.FreeLook;
        private Transform _followTarget;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private float _currentZoom;
        
        // Cinematic
        private bool _cinematicActive;
        private Coroutine _cinematicCoroutine;
        private CinematicShot[] _cinematicShots;
        private int _currentShotIndex;
        
        // Input state
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private float _zoomInput;
        private bool _isRightMouseDown;
        
        // Events
        public event Action<CameraMode> OnModeChanged;
        public event Action<Transform> OnFollowTargetChanged;
        
        // Properties
        public CameraMode CurrentMode => _currentMode;
        public Transform FollowTarget => _followTarget;
        public Camera Camera => replayCamera;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            if (replayCamera == null)
            {
                replayCamera = GetComponent<Camera>() ?? Camera.main;
            }
            
            _currentZoom = followDistance;
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
        }
        
        private void Update()
        {
            HandleInput();
            
            switch (_currentMode)
            {
                case CameraMode.FreeLook:
                    UpdateFreeLook();
                    break;
                    
                case CameraMode.Follow:
                    UpdateFollow();
                    break;
                    
                case CameraMode.BirdsEye:
                    UpdateBirdsEye();
                    break;
                    
                case CameraMode.Cinematic:
                    // Handled by coroutine
                    break;
            }
        }
        
        #region Input
        
        private void HandleInput()
        {
            // WASD / Arrow keys for movement
            _moveInput = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                _moveInput.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                _moveInput.y -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                _moveInput.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                _moveInput.x += 1f;
            
            // Q/E for vertical movement in free look
            float verticalInput = 0f;
            if (Input.GetKey(KeyCode.Q)) verticalInput -= 1f;
            if (Input.GetKey(KeyCode.E)) verticalInput += 1f;
            
            // Mouse look (right click hold)
            _isRightMouseDown = Input.GetMouseButton(1);
            if (_isRightMouseDown)
            {
                _lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            }
            else
            {
                _lookInput = Vector2.zero;
            }
            
            // Scroll wheel for zoom
            _zoomInput = -Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
            
            // Mode switching hotkeys
            if (Input.GetKeyDown(KeyCode.F1))
                SetMode(CameraMode.FreeLook);
            if (Input.GetKeyDown(KeyCode.F2))
                SetMode(CameraMode.Follow);
            if (Input.GetKeyDown(KeyCode.F3))
                SetMode(CameraMode.BirdsEye);
            if (Input.GetKeyDown(KeyCode.F4))
                StartCinematicMode();
            
            // Quick recenter
            if (Input.GetKeyDown(KeyCode.Home))
                RecenterCamera();
                
            // Double-click to follow
            if (Input.GetMouseButtonDown(0) && Time.time - _lastClickTime < 0.3f)
            {
                TryFollowClickedUnit();
            }
            if (Input.GetMouseButtonDown(0))
            {
                _lastClickTime = Time.time;
            }
        }
        
        private float _lastClickTime;
        
        private void TryFollowClickedUnit()
        {
            if (replayCamera == null) return;
            
            Ray ray = replayCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                // Check if we hit a unit
                var unit = hit.collider.GetComponentInParent<ReplayUnit>();
                if (unit != null)
                {
                    SetFollowTarget(unit.transform);
                    SetMode(CameraMode.Follow);
                }
            }
        }
        
        #endregion
        
        #region Camera Modes
        
        /// <summary>
        /// Set camera mode
        /// </summary>
        public void SetMode(CameraMode mode)
        {
            if (_currentMode == mode && mode != CameraMode.Cinematic) return;
            
            // Stop cinematic if running
            if (_cinematicCoroutine != null)
            {
                StopCoroutine(_cinematicCoroutine);
                _cinematicCoroutine = null;
                _cinematicActive = false;
            }
            
            _currentMode = mode;
            OnModeChanged?.Invoke(mode);
            
            ApexLogger.Log($"Mode: {mode}", ApexLogger.LogCategory.Replay);
            
            // Initialize mode
            switch (mode)
            {
                case CameraMode.BirdsEye:
                    InitBirdsEye();
                    break;
                    
                case CameraMode.Follow:
                    if (_followTarget == null)
                    {
                        // Find first unit to follow
                        var unit = FindFirstObjectByType<ReplayUnit>();
                        if (unit != null)
                        {
                            SetFollowTarget(unit.transform);
                        }
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Set follow target
        /// </summary>
        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
            OnFollowTargetChanged?.Invoke(target);
            
            if (target != null)
            {
                ApexLogger.Log($"Following: {target.name}", ApexLogger.LogCategory.Replay);
            }
        }
        
        /// <summary>
        /// Clear follow target
        /// </summary>
        public void ClearFollowTarget()
        {
            _followTarget = null;
            OnFollowTargetChanged?.Invoke(null);
        }
        
        #endregion
        
        #region Free Look
        
        private void UpdateFreeLook()
        {
            // Calculate speed
            float speed = moveSpeed * (_isRightMouseDown ? 1f : 0.5f);
            if (Input.GetKey(KeyCode.LeftShift))
                speed *= shiftMultiplier;
            
            // Movement
            Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            
            // Vertical
            if (Input.GetKey(KeyCode.Q))
                move += Vector3.down;
            if (Input.GetKey(KeyCode.E))
                move += Vector3.up;
            
            _targetPosition += move * speed * Time.deltaTime;
            
            // Clamp to boundaries
            _targetPosition = ClampToBoundaries(_targetPosition);
            
            // Rotation (only when right mouse held)
            if (_isRightMouseDown)
            {
                Vector3 euler = transform.eulerAngles;
                euler.y += _lookInput.x * rotationSpeed;
                euler.x -= _lookInput.y * rotationSpeed;
                euler.x = ClampAngle(euler.x, -80f, 80f);
                _targetRotation = Quaternion.Euler(euler);
            }
            
            // Zoom (move forward/back)
            if (Mathf.Abs(_zoomInput) > 0.01f)
            {
                _targetPosition += transform.forward * (-_zoomInput * 2f);
                _targetPosition = ClampToBoundaries(_targetPosition);
            }
            
            // Apply
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * 10f);
        }
        
        #endregion
        
        #region Follow Mode
        
        private void UpdateFollow()
        {
            if (_followTarget == null)
            {
                // Fall back to free look if no target
                UpdateFreeLook();
                return;
            }
            
            // Adjust distance with scroll
            _currentZoom = Mathf.Clamp(_currentZoom + _zoomInput, minZoom, maxZoom / 2f);
            
            // Calculate orbit position
            float horizontalAngle = transform.eulerAngles.y;
            
            // Allow rotation around target with right mouse
            if (_isRightMouseDown)
            {
                horizontalAngle += _lookInput.x * rotationSpeed * 20f;
            }
            
            // Calculate camera position
            Vector3 offset = new Vector3(
                Mathf.Sin(horizontalAngle * Mathf.Deg2Rad) * _currentZoom,
                followHeight,
                Mathf.Cos(horizontalAngle * Mathf.Deg2Rad) * _currentZoom
            );
            
            _targetPosition = _followTarget.position + offset;
            _targetPosition = ClampToBoundaries(_targetPosition);
            
            // Look at target
            _targetRotation = Quaternion.LookRotation(_followTarget.position - _targetPosition);
            
            // Smooth apply
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * followSmoothness);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * followRotationSmooth);
        }
        
        #endregion
        
        #region Birds Eye
        
        private void InitBirdsEye()
        {
            // Move camera above current position looking down
            _targetPosition = new Vector3(transform.position.x, birdsEyeHeight, transform.position.z);
            _targetRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        
        private void UpdateBirdsEye()
        {
            // Pan with WASD
            float speed = moveSpeed * 1.5f;
            if (Input.GetKey(KeyCode.LeftShift))
                speed *= shiftMultiplier;
            
            Vector3 move = new Vector3(_moveInput.x, 0f, _moveInput.y);
            _targetPosition += move * speed * Time.deltaTime;
            
            // Zoom with scroll
            float newHeight = _targetPosition.y + _zoomInput * 2f;
            _targetPosition.y = Mathf.Clamp(newHeight, birdsEyeMinHeight, birdsEyeMaxHeight);
            
            // Clamp to boundaries
            _targetPosition.x = Mathf.Clamp(_targetPosition.x, boundaryMin.x, boundaryMax.x);
            _targetPosition.z = Mathf.Clamp(_targetPosition.z, boundaryMin.z, boundaryMax.z);
            
            // Apply
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * 8f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(90f, 0f, 0f), Time.deltaTime * 5f);
        }
        
        #endregion
        
        #region Cinematic Mode
        
        /// <summary>
        /// Start automatic cinematic camera
        /// </summary>
        public void StartCinematicMode()
        {
            if (_cinematicCoroutine != null)
            {
                StopCoroutine(_cinematicCoroutine);
            }
            
            _currentMode = CameraMode.Cinematic;
            OnModeChanged?.Invoke(CameraMode.Cinematic);
            
            GenerateCinematicShots();
            _cinematicCoroutine = StartCoroutine(RunCinematicMode());
        }
        
        /// <summary>
        /// Stop cinematic mode
        /// </summary>
        public void StopCinematicMode()
        {
            if (_cinematicCoroutine != null)
            {
                StopCoroutine(_cinematicCoroutine);
                _cinematicCoroutine = null;
            }
            _cinematicActive = false;
            SetMode(CameraMode.FreeLook);
        }
        
        private void GenerateCinematicShots()
        {
            // Generate dynamic shots based on battlefield
            var shots = new System.Collections.Generic.List<CinematicShot>();
            
            // Get all units and buildings
            var units = FindObjectsByType<ReplayUnit>(FindObjectsSortMode.None);
            var buildings = FindObjectsByType<ReplayBuilding>(FindObjectsSortMode.None);
            
            // Shot 1: Establishing shot (wide)
            shots.Add(new CinematicShot
            {
                Position = new Vector3(0, 40, -30),
                LookAt = Vector3.zero,
                Duration = 4f,
                Type = ShotType.Wide
            });
            
            // Shot 2-4: Unit close-ups
            foreach (var unit in units)
            {
                if (shots.Count >= 6) break;
                
                Vector3 unitPos = unit.transform.position;
                Vector3 behindUnit = unitPos - unit.transform.forward * 5f + Vector3.up * 3f;
                
                shots.Add(new CinematicShot
                {
                    Position = behindUnit,
                    LookAt = unitPos + Vector3.up,
                    Duration = 3f,
                    Type = ShotType.CloseUp,
                    Target = unit.transform
                });
            }
            
            // Shot 5: Building shot
            foreach (var building in buildings)
            {
                if (shots.Count >= 8) break;
                
                Vector3 buildPos = building.transform.position;
                Vector3 viewPos = buildPos + new Vector3(10, 8, 10);
                
                shots.Add(new CinematicShot
                {
                    Position = viewPos,
                    LookAt = buildPos + Vector3.up * 2f,
                    Duration = 3f,
                    Type = ShotType.Medium
                });
            }
            
            // Shot 6: Final wide shot
            shots.Add(new CinematicShot
            {
                Position = new Vector3(20, 30, 20),
                LookAt = Vector3.zero,
                Duration = 4f,
                Type = ShotType.Wide
            });
            
            _cinematicShots = shots.ToArray();
        }
        
        private IEnumerator RunCinematicMode()
        {
            _cinematicActive = true;
            _currentShotIndex = 0;
            
            while (_cinematicActive && _cinematicShots != null && _cinematicShots.Length > 0)
            {
                var shot = _cinematicShots[_currentShotIndex];
                
                // Transition to shot
                yield return StartCoroutine(TransitionToShot(shot));
                
                // Hold shot
                float holdTime = shot.Duration;
                float elapsed = 0f;
                
                while (elapsed < holdTime && _cinematicActive)
                {
                    // Dynamic tracking if shot has target
                    if (shot.Target != null)
                    {
                        Vector3 lookTarget = shot.Target.position + Vector3.up;
                        Quaternion targetRot = Quaternion.LookRotation(lookTarget - transform.position);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 2f);
                    }
                    
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                
                // Next shot
                _currentShotIndex = (_currentShotIndex + 1) % _cinematicShots.Length;
            }
            
            _cinematicActive = false;
        }
        
        private IEnumerator TransitionToShot(CinematicShot shot)
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            
            Vector3 endPos = shot.Position;
            
            // Calculate look rotation
            Vector3 lookTarget = shot.Target != null ? shot.Target.position + Vector3.up : shot.LookAt;
            Quaternion endRot = Quaternion.LookRotation(lookTarget - endPos);
            
            float elapsed = 0f;
            
            while (elapsed < cinematicTransitionDuration)
            {
                float t = cinematicCurve.Evaluate(elapsed / cinematicTransitionDuration);
                
                transform.position = Vector3.Lerp(startPos, endPos, t);
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.position = endPos;
            transform.rotation = endRot;
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Instant move to position and look at target
        /// </summary>
        public void JumpTo(Vector3 position, Vector3 lookAt)
        {
            transform.position = ClampToBoundaries(position);
            transform.LookAt(lookAt);
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
        }
        
        /// <summary>
        /// Smooth move to position
        /// </summary>
        public void MoveTo(Vector3 position, float duration = 1f)
        {
            StartCoroutine(SmoothMoveTo(position, duration));
        }
        
        private IEnumerator SmoothMoveTo(Vector3 endPos, float duration)
        {
            Vector3 startPos = transform.position;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float t = cinematicCurve.Evaluate(elapsed / duration);
                transform.position = Vector3.Lerp(startPos, endPos, t);
                _targetPosition = transform.position;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.position = endPos;
            _targetPosition = endPos;
        }
        
        /// <summary>
        /// Recenter camera on battlefield
        /// </summary>
        public void RecenterCamera()
        {
            Vector3 center = Vector3.zero;
            
            // Find center of action
            var units = FindObjectsByType<ReplayUnit>(FindObjectsSortMode.None);
            if (units.Length > 0)
            {
                foreach (var unit in units)
                {
                    center += unit.transform.position;
                }
                center /= units.Length;
            }
            
            // Position camera
            _targetPosition = center + new Vector3(0, 15, -20);
            _targetRotation = Quaternion.LookRotation(center - _targetPosition);
        }
        
        /// <summary>
        /// Set camera boundaries
        /// </summary>
        public void SetBoundaries(Vector3 min, Vector3 max)
        {
            boundaryMin = min;
            boundaryMax = max;
        }
        
        private Vector3 ClampToBoundaries(Vector3 pos)
        {
            pos.x = Mathf.Clamp(pos.x, boundaryMin.x, boundaryMax.x);
            pos.y = Mathf.Clamp(pos.y, boundaryMin.y, boundaryMax.y);
            pos.z = Mathf.Clamp(pos.z, boundaryMin.z, boundaryMax.z);
            return pos;
        }
        
        private float ClampAngle(float angle, float min, float max)
        {
            if (angle > 180f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }
        
        #endregion
        
        #region Screen Effects
        
        /// <summary>
        /// Shake camera (for impacts)
        /// </summary>
        public void Shake(float intensity = 0.5f, float duration = 0.3f)
        {
            StartCoroutine(DoShake(intensity, duration));
        }
        
        private IEnumerator DoShake(float intensity, float duration)
        {
            Vector3 originalPos = transform.localPosition;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * intensity;
                float y = UnityEngine.Random.Range(-1f, 1f) * intensity;
                
                transform.localPosition = originalPos + new Vector3(x, y, 0);
                
                // Decay intensity
                intensity *= 0.95f;
                elapsed += Time.deltaTime;
                
                yield return null;
            }
            
            transform.localPosition = originalPos;
        }
        
        /// <summary>
        /// Zoom effect (for dramatic moments)
        /// </summary>
        public void ZoomPunch(float amount = 5f, float duration = 0.2f)
        {
            if (replayCamera != null)
            {
                StartCoroutine(DoZoomPunch(amount, duration));
            }
        }
        
        private IEnumerator DoZoomPunch(float amount, float duration)
        {
            float originalFOV = replayCamera.fieldOfView;
            float targetFOV = originalFOV - amount;
            
            // Zoom in
            float elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                float t = elapsed / (duration / 2f);
                replayCamera.fieldOfView = Mathf.Lerp(originalFOV, targetFOV, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Zoom out
            elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                float t = elapsed / (duration / 2f);
                replayCamera.fieldOfView = Mathf.Lerp(targetFOV, originalFOV, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            replayCamera.fieldOfView = originalFOV;
        }
        
        #endregion
    }
    
    #region Enums and Data Classes
    
    /// <summary>
    /// Camera modes for replay viewing
    /// </summary>
    public enum CameraMode
    {
        FreeLook,
        Follow,
        BirdsEye,
        Cinematic
    }
    
    /// <summary>
    /// Cinematic shot types
    /// </summary>
    public enum ShotType
    {
        Wide,
        Medium,
        CloseUp,
        Tracking
    }
    
    /// <summary>
    /// Cinematic camera shot definition
    /// </summary>
    [Serializable]
    public class CinematicShot
    {
        public Vector3 Position;
        public Vector3 LookAt;
        public float Duration = 3f;
        public ShotType Type;
        public Transform Target;
    }
    
    /// <summary>
    /// Marker component for replay units
    /// </summary>
    public class ReplayUnit : MonoBehaviour
    {
        public string UnitId;
        public string UnitType;
        public bool IsAttacker;
    }
    
    /// <summary>
    /// Marker component for replay buildings
    /// </summary>
    public class ReplayBuilding : MonoBehaviour
    {
        public string BuildingId;
        public string BuildingType;
    }
    
    #endregion
}
