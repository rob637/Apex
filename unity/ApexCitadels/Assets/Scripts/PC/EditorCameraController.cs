// ============================================================================
// APEX CITADELS - EDITOR CAMERA CONTROLLER
// Smooth camera controls for PC Base Editor
// Supports orbit, pan, zoom with edge scrolling
// ============================================================================
using UnityEngine;
using System;

namespace ApexCitadels.PC
{
    /// <summary>
    /// Camera controller optimized for base building editor.
    /// Provides smooth orbit, pan, zoom, and edge scrolling.
    /// </summary>
    public class EditorCameraController : MonoBehaviour
    {
        public static EditorCameraController Instance { get; private set; }

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = Vector3.zero;
        [SerializeField] private bool autoFindTarget = true;

        [Header("Orbit Settings")]
        [SerializeField] private float orbitSensitivity = 5f;
        [SerializeField] private float minVerticalAngle = 10f;
        [SerializeField] private float maxVerticalAngle = 85f;
        [SerializeField] private float orbitSmoothTime = 0.1f;

        [Header("Zoom Settings")]
        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float zoomSmoothTime = 0.1f;

        [Header("Pan Settings")]
        [SerializeField] private float panSpeed = 20f;
        [SerializeField] private float panSmoothTime = 0.15f;
        [SerializeField] private bool invertPanY = false;

        [Header("Edge Scrolling")]
        [SerializeField] private bool enableEdgeScrolling = true;
        [SerializeField] private float edgeScrollSpeed = 15f;
        [SerializeField] private float edgeBorderSize = 20f;

        [Header("Keyboard Movement")]
        [SerializeField] private float keyboardMoveSpeed = 25f;
        [SerializeField] private KeyCode moveForward = KeyCode.W;
        [SerializeField] private KeyCode moveBack = KeyCode.S;
        [SerializeField] private KeyCode moveLeft = KeyCode.A;
        [SerializeField] private KeyCode moveRight = KeyCode.D;
        [SerializeField] private KeyCode moveUp = KeyCode.E;
        [SerializeField] private KeyCode moveDown = KeyCode.Q;

        [Header("Focus")]
        [SerializeField] private float focusTransitionTime = 0.5f;
        [SerializeField] private float focusDistanceMultiplier = 2f;

        [Header("Boundaries")]
        [SerializeField] private bool useBoundaries = true;
        [SerializeField] private Vector3 boundaryMin = new Vector3(-100, 0, -100);
        [SerializeField] private Vector3 boundaryMax = new Vector3(100, 50, 100);

        // Current state
        private float currentDistance = 30f;
        private float currentYaw = 45f;
        private float currentPitch = 45f;
        private Vector3 currentPivot = Vector3.zero;

        // Target state (for smoothing)
        private float targetDistance;
        private float targetYaw;
        private float targetPitch;
        private Vector3 targetPivot;

        // Velocity for SmoothDamp
        private float distanceVelocity;
        private float yawVelocity;
        private float pitchVelocity;
        private Vector3 pivotVelocity;

        // Focus transition
        private bool isFocusing;
        private Vector3 focusStartPivot;
        private Vector3 focusEndPivot;
        private float focusStartDistance;
        private float focusEndDistance;
        private float focusStartTime;

        // Input state
        private bool isOrbiting;
        private bool isPanning;
        private Vector3 lastMousePosition;

        // Camera reference
        private Camera cam;

        // Events
        public event Action<Vector3> OnCameraPositionChanged;
        public event Action<float> OnZoomChanged;

        public float CurrentDistance => currentDistance;
        public float CurrentYaw => currentYaw;
        public float CurrentPitch => currentPitch;
        public Vector3 CurrentPivot => currentPivot;
        public bool IsEditorMode { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = Camera.main;
            }

            // Initialize to current values
            targetDistance = currentDistance;
            targetYaw = currentYaw;
            targetPitch = currentPitch;
            targetPivot = currentPivot;
        }

        private void Start()
        {
            if (autoFindTarget && target == null)
            {
                // Look for territory center or default to origin
                var territoryCenter = GameObject.FindWithTag("TerritoryCenter");
                if (territoryCenter != null)
                {
                    target = territoryCenter.transform;
                }
            }

            if (target != null)
            {
                targetPivot = target.position + targetOffset;
                currentPivot = targetPivot;
            }

            ApplyCameraTransform();
        }

        private void Update()
        {
            if (!IsEditorMode) return;

            HandleInput();
            UpdateFocus();
            SmoothUpdate();
            ApplyCameraTransform();
        }

        private void LateUpdate()
        {
            if (!IsEditorMode) return;

            // Ensure camera is properly positioned after all updates
            ApplyCameraTransform();
        }

        #region Input Handling

        private void HandleInput()
        {
            HandleOrbit();
            HandlePan();
            HandleZoom();
            HandleKeyboardMovement();
            HandleEdgeScrolling();
        }

        private void HandleOrbit()
        {
            // Middle mouse button or Alt + Left mouse for orbit
            bool orbitInput = Input.GetMouseButton(2) || 
                              (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftAlt));

            if (orbitInput)
            {
                if (!isOrbiting)
                {
                    isOrbiting = true;
                    lastMousePosition = Input.mousePosition;
                }
                else
                {
                    Vector3 delta = Input.mousePosition - lastMousePosition;
                    lastMousePosition = Input.mousePosition;

                    targetYaw += delta.x * orbitSensitivity * 0.1f;
                    targetPitch -= delta.y * orbitSensitivity * 0.1f;
                    targetPitch = Mathf.Clamp(targetPitch, minVerticalAngle, maxVerticalAngle);

                    // Normalize yaw
                    if (targetYaw > 360f) targetYaw -= 360f;
                    if (targetYaw < 0f) targetYaw += 360f;
                }
            }
            else
            {
                isOrbiting = false;
            }

            // Q/E for rotation
            if (Input.GetKey(KeyCode.Q) && !Input.GetKey(KeyCode.LeftShift))
            {
                targetYaw -= orbitSensitivity * 2f * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.E) && !Input.GetKey(KeyCode.LeftShift))
            {
                targetYaw += orbitSensitivity * 2f * Time.deltaTime;
            }
        }

        private void HandlePan()
        {
            // Right mouse button for pan
            if (Input.GetMouseButton(1))
            {
                if (!isPanning)
                {
                    isPanning = true;
                    lastMousePosition = Input.mousePosition;
                }
                else
                {
                    Vector3 delta = Input.mousePosition - lastMousePosition;
                    lastMousePosition = Input.mousePosition;

                    // Calculate pan direction based on camera orientation
                    Vector3 right = Quaternion.Euler(0, currentYaw, 0) * Vector3.right;
                    Vector3 forward = Quaternion.Euler(0, currentYaw, 0) * Vector3.forward;

                    float panMultiplier = currentDistance * 0.001f * panSpeed;
                    targetPivot -= right * delta.x * panMultiplier;
                    
                    float yDelta = invertPanY ? delta.y : -delta.y;
                    targetPivot -= forward * yDelta * panMultiplier;
                }
            }
            else
            {
                isPanning = false;
            }
        }

        private void HandleZoom()
        {
            float scrollDelta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                // Zoom towards mouse position
                float zoomFactor = 1f - scrollDelta * zoomSpeed * 0.1f;
                targetDistance *= zoomFactor;
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);

                OnZoomChanged?.Invoke(targetDistance);
            }
        }

        private void HandleKeyboardMovement()
        {
            Vector3 moveDir = Vector3.zero;

            // Camera-relative movement
            Vector3 forward = Quaternion.Euler(0, currentYaw, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, currentYaw, 0) * Vector3.right;

            if (Input.GetKey(moveForward) || Input.GetKey(KeyCode.UpArrow))
                moveDir += forward;
            if (Input.GetKey(moveBack) || Input.GetKey(KeyCode.DownArrow))
                moveDir -= forward;
            if (Input.GetKey(moveLeft) || Input.GetKey(KeyCode.LeftArrow))
                moveDir -= right;
            if (Input.GetKey(moveRight) || Input.GetKey(KeyCode.RightArrow))
                moveDir += right;

            // Vertical movement (Shift + Q/E)
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetKey(moveUp))
                    moveDir += Vector3.up;
                if (Input.GetKey(moveDown))
                    moveDir -= Vector3.up;
            }

            if (moveDir.sqrMagnitude > 0.01f)
            {
                moveDir.Normalize();
                float speed = keyboardMoveSpeed * (currentDistance / 30f); // Scale with zoom
                
                if (Input.GetKey(KeyCode.LeftShift))
                    speed *= 2f; // Sprint

                targetPivot += moveDir * speed * Time.deltaTime;
            }
        }

        private void HandleEdgeScrolling()
        {
            if (!enableEdgeScrolling) return;
            if (isPanning || isOrbiting) return;

            Vector3 mousePos = Input.mousePosition;
            Vector3 moveDir = Vector3.zero;

            Vector3 forward = Quaternion.Euler(0, currentYaw, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, currentYaw, 0) * Vector3.right;

            // Check edges
            if (mousePos.x <= edgeBorderSize)
                moveDir -= right;
            else if (mousePos.x >= Screen.width - edgeBorderSize)
                moveDir += right;

            if (mousePos.y <= edgeBorderSize)
                moveDir -= forward;
            else if (mousePos.y >= Screen.height - edgeBorderSize)
                moveDir += forward;

            if (moveDir.sqrMagnitude > 0.01f)
            {
                moveDir.Normalize();
                float speed = edgeScrollSpeed * (currentDistance / 30f);
                targetPivot += moveDir * speed * Time.deltaTime;
            }
        }

        #endregion

        #region Smooth Updates

        private void SmoothUpdate()
        {
            // Smooth all values
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, orbitSmoothTime);
            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, orbitSmoothTime);
            currentPivot = Vector3.SmoothDamp(currentPivot, targetPivot, ref pivotVelocity, panSmoothTime);

            // Apply boundaries
            if (useBoundaries)
            {
                currentPivot.x = Mathf.Clamp(currentPivot.x, boundaryMin.x, boundaryMax.x);
                currentPivot.y = Mathf.Clamp(currentPivot.y, boundaryMin.y, boundaryMax.y);
                currentPivot.z = Mathf.Clamp(currentPivot.z, boundaryMin.z, boundaryMax.z);
                
                targetPivot.x = Mathf.Clamp(targetPivot.x, boundaryMin.x, boundaryMax.x);
                targetPivot.y = Mathf.Clamp(targetPivot.y, boundaryMin.y, boundaryMax.y);
                targetPivot.z = Mathf.Clamp(targetPivot.z, boundaryMin.z, boundaryMax.z);
            }
        }

        private void ApplyCameraTransform()
        {
            // Calculate camera position
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            Vector3 offset = rotation * new Vector3(0, 0, -currentDistance);
            Vector3 newPosition = currentPivot + offset;

            transform.position = newPosition;
            transform.LookAt(currentPivot);

            OnCameraPositionChanged?.Invoke(newPosition);
        }

        #endregion

        #region Focus Methods

        /// <summary>
        /// Focus on a specific world position
        /// </summary>
        public void FocusOn(Vector3 position, float? customDistance = null)
        {
            isFocusing = true;
            focusStartPivot = currentPivot;
            focusEndPivot = position;
            focusStartDistance = currentDistance;
            focusEndDistance = customDistance ?? (currentDistance * focusDistanceMultiplier);
            focusStartTime = Time.time;
        }

        /// <summary>
        /// Focus on a transform
        /// </summary>
        public void FocusOn(Transform focusTarget, float? customDistance = null)
        {
            if (focusTarget != null)
            {
                // Calculate bounds if renderer exists
                Renderer renderer = focusTarget.GetComponent<Renderer>();
                float dist = customDistance ?? (renderer != null 
                    ? renderer.bounds.size.magnitude * focusDistanceMultiplier 
                    : currentDistance);

                FocusOn(focusTarget.position, dist);
            }
        }

        /// <summary>
        /// Focus on a building block
        /// </summary>
        public void FocusOnBlock(GameObject block)
        {
            if (block != null)
            {
                FocusOn(block.transform, 10f);
            }
        }

        private void UpdateFocus()
        {
            if (!isFocusing) return;

            float elapsed = Time.time - focusStartTime;
            float t = Mathf.Clamp01(elapsed / focusTransitionTime);
            
            // Smooth step for natural feel
            t = t * t * (3f - 2f * t);

            targetPivot = Vector3.Lerp(focusStartPivot, focusEndPivot, t);
            targetDistance = Mathf.Lerp(focusStartDistance, focusEndDistance, t);

            if (t >= 1f)
            {
                isFocusing = false;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set camera to top-down orthographic view
        /// </summary>
        public void SetTopDownView()
        {
            targetPitch = 89f;
            targetYaw = 0f;
        }

        /// <summary>
        /// Set camera to isometric view
        /// </summary>
        public void SetIsometricView()
        {
            targetPitch = 45f;
            targetYaw = 45f;
        }

        /// <summary>
        /// Set camera to first-person-like ground view
        /// </summary>
        public void SetGroundView()
        {
            targetPitch = 15f;
            targetDistance = 10f;
        }

        /// <summary>
        /// Reset camera to default position
        /// </summary>
        public void ResetCamera()
        {
            targetPivot = target != null ? target.position + targetOffset : Vector3.zero;
            targetDistance = 30f;
            targetYaw = 45f;
            targetPitch = 45f;
        }

        /// <summary>
        /// Set the pivot point directly
        /// </summary>
        public void SetPivot(Vector3 pivot)
        {
            targetPivot = pivot;
            currentPivot = pivot;
        }

        /// <summary>
        /// Set zoom distance directly
        /// </summary>
        public void SetDistance(float distance)
        {
            targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        /// <summary>
        /// Set rotation directly
        /// </summary>
        public void SetRotation(float yaw, float pitch)
        {
            targetYaw = yaw;
            targetPitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
        }

        /// <summary>
        /// Get the world position under the mouse cursor
        /// </summary>
        public Vector3 GetMouseWorldPosition(float height = 0f)
        {
            if (cam == null) return Vector3.zero;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, new Vector3(0, height, 0));

            if (plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Set camera boundaries
        /// </summary>
        public void SetBoundaries(Vector3 min, Vector3 max)
        {
            boundaryMin = min;
            boundaryMax = max;
        }

        /// <summary>
        /// Set territory-based boundaries
        /// </summary>
        public void SetTerritoryBoundaries(Vector3 center, float radius)
        {
            boundaryMin = center - new Vector3(radius, 0, radius);
            boundaryMax = center + new Vector3(radius, 50f, radius);
        }

        #endregion

        #region Editor Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (useBoundaries)
            {
                Gizmos.color = Color.yellow;
                Vector3 center = (boundaryMin + boundaryMax) / 2f;
                Vector3 size = boundaryMax - boundaryMin;
                Gizmos.DrawWireCube(center, size);
            }

            // Draw pivot point
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentPivot, 0.5f);

            // Draw camera frustum line
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(currentPivot, transform.position);
        }
#endif

        #endregion
    }
}
