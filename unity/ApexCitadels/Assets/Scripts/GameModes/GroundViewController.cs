// ============================================================================
// APEX CITADELS - GROUND VIEW CONTROLLER
// First-person character controller for ground level exploration
// ============================================================================
using UnityEngine;

namespace ApexCitadels.GameModes
{
    /// <summary>
    /// Controls the player character and camera in Ground View mode.
    /// First-person perspective with visible arms when walking.
    /// </summary>
    public class GroundViewController : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("View Mode")]
        [SerializeField] private bool firstPersonMode = true;  // Default to first-person
        [SerializeField] private float eyeHeight = 1.6f;       // Camera height for first-person
        
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float gravity = 20f;
        
        [Header("Camera - Third Person")]
        [SerializeField] private float cameraDistance = 5f;
        [SerializeField] private float cameraHeight = 2f;
        [SerializeField] private float cameraLookAhead = 1f;
        [SerializeField] private float cameraSmoothTime = 0.15f;
        [SerializeField] private float minVerticalAngle = -80f;
        [SerializeField] private float maxVerticalAngle = 80f;
        
        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private bool invertY = false;
        
        #endregion
        
        #region Private Fields
        
        private GameObject _player;
        private Camera _camera;
        private CharacterController _characterController;
        private Animator _animator;
        
        private Vector3 _moveDirection;
        private float _verticalVelocity;
        
        // Camera orbit
        private float _cameraYaw;
        private float _cameraPitch = 20f;
        private Vector3 _cameraVelocity;
        
        // Animation states
        private bool _isMoving;
        private bool _isRunning;
        
        #endregion
        
        #region Properties
        
        public GameObject Player => _player;
        public bool IsFirstPerson => firstPersonMode;
        
        /// <summary>
        /// Set view mode: true = first-person, false = third-person
        /// </summary>
        public void SetFirstPersonMode(bool enabled)
        {
            firstPersonMode = enabled;
            Debug.Log($"[GroundView] Set to {(enabled ? "First" : "Third")} Person mode");
        }
        
        #endregion
        
        #region Setup
        
        public void SetPlayer(GameObject player)
        {
            _player = player;
            _characterController = player.GetComponent<CharacterController>();
            _animator = player.GetComponentInChildren<Animator>();
            
            // Initialize camera position behind player
            _cameraYaw = player.transform.eulerAngles.y;
        }
        
        public void SetCamera(Camera camera)
        {
            _camera = camera;
        }
        
        #endregion
        
        #region Update
        
        private void Update()
        {
            if (_player == null || _camera == null) return;
            
            HandleInput();
            HandleMovement();
            HandleCameraOrbit();
            UpdateCamera();
            UpdateAnimations();
        }
        
        private void HandleInput()
        {
            // Mouse look - always active in ground view
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            if (invertY) mouseY = -mouseY;
            
            _cameraYaw += mouseX;
            _cameraPitch -= mouseY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, minVerticalAngle, maxVerticalAngle);
            
            // Running
            _isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            
            // Toggle first/third person with V key
            if (Input.GetKeyDown(KeyCode.V))
            {
                firstPersonMode = !firstPersonMode;
                Debug.Log($"[GroundView] Switched to {(firstPersonMode ? "First" : "Third")} Person mode");
            }
        }
        
        private void HandleMovement()
        {
            if (_characterController == null) return;
            
            // Get input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            _isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;
            
            if (_isMoving)
            {
                // Calculate move direction relative to camera yaw (horizontal only)
                Vector3 cameraForward = Quaternion.Euler(0, _cameraYaw, 0) * Vector3.forward;
                Vector3 cameraRight = Quaternion.Euler(0, _cameraYaw, 0) * Vector3.right;
                
                Vector3 moveDir = (cameraForward * vertical + cameraRight * horizontal).normalized;
                
                // In first person, player always faces camera direction
                // In third person, rotate character to face movement direction
                if (!firstPersonMode && moveDir.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                    _player.transform.rotation = Quaternion.RotateTowards(
                        _player.transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );
                }
                
                // Apply movement
                float speed = _isRunning ? runSpeed : walkSpeed;
                _moveDirection = moveDir * speed;
            }
            else
            {
                _moveDirection = Vector3.zero;
            }
            
            // Apply gravity
            if (_characterController.isGrounded)
            {
                _verticalVelocity = -0.5f; // Small downward force to stay grounded
            }
            else
            {
                _verticalVelocity -= gravity * Time.deltaTime;
            }
            
            // Final movement
            Vector3 finalMove = _moveDirection + Vector3.up * _verticalVelocity;
            _characterController.Move(finalMove * Time.deltaTime);
        }
        
        private void HandleCameraOrbit()
        {
            // Q/E to orbit camera
            if (Input.GetKey(KeyCode.Q))
            {
                _cameraYaw -= 90f * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.E))
            {
                _cameraYaw += 90f * Time.deltaTime;
            }
            
            // Scroll to zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                cameraDistance -= scroll * 3f;
                cameraDistance = Mathf.Clamp(cameraDistance, 2f, 15f);
            }
        }
        
        private void UpdateCamera()
        {
            if (_player == null || _camera == null) return;
            
            if (firstPersonMode)
            {
                UpdateFirstPersonCamera();
            }
            else
            {
                UpdateThirdPersonCamera();
            }
        }
        
        private void UpdateFirstPersonCamera()
        {
            // Position camera at eye level
            Vector3 eyePos = _player.transform.position + Vector3.up * eyeHeight;
            _camera.transform.position = eyePos;
            
            // Look in direction based on pitch/yaw
            Quaternion rotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0);
            _camera.transform.rotation = rotation;
            
            // Rotate player body to match yaw (but not pitch)
            _player.transform.rotation = Quaternion.Euler(0, _cameraYaw, 0);
        }
        
        private void UpdateThirdPersonCamera()
        {
            // Calculate desired camera position
            Quaternion rotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0);
            Vector3 offset = rotation * new Vector3(0, 0, -cameraDistance);
            
            // Position relative to player
            Vector3 playerCenter = _player.transform.position + Vector3.up * 1.0f; // Aim at chest height
            Vector3 targetPos = playerCenter + offset + Vector3.up * cameraHeight;
            
            // Check for obstacles between camera and player
            RaycastHit hit;
            Vector3 dirToTarget = targetPos - playerCenter;
            if (Physics.Raycast(playerCenter, dirToTarget.normalized, out hit, dirToTarget.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                // Pull camera in front of obstacle
                targetPos = hit.point - dirToTarget.normalized * 0.2f;
            }
            
            // Smooth camera movement
            _camera.transform.position = Vector3.SmoothDamp(
                _camera.transform.position, 
                targetPos, 
                ref _cameraVelocity, 
                cameraSmoothTime
            );
            
            // Look at player (slightly ahead if moving)
            Vector3 lookTarget = playerCenter;
            if (_isMoving)
            {
                lookTarget += _player.transform.forward * cameraLookAhead;
            }
            
            _camera.transform.LookAt(lookTarget);
        }
        
        private void UpdateAnimations()
        {
            if (_animator == null) return;
            
            // Set animation parameters
            _animator.SetBool("IsMoving", _isMoving);
            _animator.SetBool("IsRunning", _isRunning && _isMoving);
            _animator.SetFloat("Speed", _isMoving ? (_isRunning ? 1f : 0.5f) : 0f);
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Teleport player to position
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            if (_characterController != null)
            {
                _characterController.enabled = false;
                _player.transform.position = position;
                _characterController.enabled = true;
            }
            else if (_player != null)
            {
                _player.transform.position = position;
            }
        }
        
        /// <summary>
        /// Set camera distance
        /// </summary>
        public void SetCameraDistance(float distance)
        {
            cameraDistance = Mathf.Clamp(distance, 2f, 15f);
        }
        
        #endregion
    }
}
