// ============================================================================
// APEX CITADELS - FANTASY PLAYER CONTROLLER
// First/Third person player controller for exploring the Fantasy Kingdom
// ============================================================================
using UnityEngine;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// Player controller for Fantasy Kingdom exploration
    /// Supports first-person and third-person views
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FantasyPlayerController : MonoBehaviour
    {
        [Header("=== MOVEMENT ===")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeed = 10f;
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float gravity = -20f;
        
        [Header("=== CAMERA ===")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;
        
        [Header("=== VIEW MODE ===")]
        [SerializeField] private bool isThirdPerson = false;
        [SerializeField] private float thirdPersonDistance = 5f;
        [SerializeField] private float thirdPersonHeight = 2f;
        [SerializeField] private Transform cameraTarget; // Where camera looks in 3rd person
        
        [Header("=== GROUND CHECK ===")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float groundCheckDistance = 0.2f;
        
        private CharacterController characterController;
        private Vector3 velocity;
        private float cameraPitch = 0f;
        private float cameraYaw = 0f;
        private bool isEnabled = true;
        private bool isGrounded = false;
        
        // Third person camera smoothing
        private Vector3 currentCameraOffset;
        private Vector3 cameraVelocity;
        
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            
            // Setup camera
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null)
                {
                    // Create camera
                    var camObj = new GameObject("PlayerCamera");
                    playerCamera = camObj.AddComponent<Camera>();
                    playerCamera.tag = "MainCamera";
                }
            }
            
            // Create camera target for third person
            if (cameraTarget == null)
            {
                var targetObj = new GameObject("CameraTarget");
                targetObj.transform.SetParent(transform);
                targetObj.transform.localPosition = new Vector3(0, 1.6f, 0);
                cameraTarget = targetObj.transform;
            }
            
            // Initialize camera rotation
            cameraYaw = transform.eulerAngles.y;
        }
        
        private void Start()
        {
            // Lock cursor
            if (isEnabled)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            
            // Position camera
            UpdateCameraPosition();
        }
        
        private void Update()
        {
            if (!isEnabled) return;
            
            HandleInput();
            HandleMovement();
            HandleCamera();
            
            // Toggle view mode
            if (Input.GetKeyDown(KeyCode.V))
            {
                ToggleViewMode();
            }
            
            // Toggle cursor lock
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleCursor();
            }
        }
        
        private void HandleInput()
        {
            // Mouse look
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
                
                cameraYaw += mouseX;
                cameraPitch -= mouseY;
                cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
            }
        }
        
        private void HandleMovement()
        {
            // Ground check
            isGrounded = characterController.isGrounded;
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Small downward force to stay grounded
            }
            
            // Get input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            // Calculate move direction relative to camera
            Vector3 forward = Quaternion.Euler(0, cameraYaw, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, cameraYaw, 0) * Vector3.right;
            
            Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;
            
            // Speed
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float speed = isRunning ? runSpeed : walkSpeed;
            
            // Apply movement
            Vector3 move = moveDirection * speed;
            characterController.Move(move * Time.deltaTime);
            
            // Jump
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            
            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
            
            // Rotate character to face movement direction (third person)
            if (isThirdPerson && moveDirection.magnitude > 0.1f)
            {
                float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.Euler(0, targetAngle, 0),
                    Time.deltaTime * 10f
                );
            }
            else if (!isThirdPerson)
            {
                // First person: character faces camera direction
                transform.rotation = Quaternion.Euler(0, cameraYaw, 0);
            }
        }
        
        private void HandleCamera()
        {
            UpdateCameraPosition();
        }
        
        private void UpdateCameraPosition()
        {
            if (playerCamera == null) return;
            
            if (isThirdPerson)
            {
                // Third person: camera behind and above player
                Vector3 targetOffset = new Vector3(0, thirdPersonHeight, -thirdPersonDistance);
                Quaternion cameraRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0);
                Vector3 desiredPosition = cameraTarget.position + cameraRotation * targetOffset;
                
                // Check for obstacles
                RaycastHit hit;
                Vector3 direction = desiredPosition - cameraTarget.position;
                if (Physics.Raycast(cameraTarget.position, direction.normalized, out hit, direction.magnitude, groundLayers))
                {
                    desiredPosition = hit.point + hit.normal * 0.2f;
                }
                
                // Smooth camera movement
                playerCamera.transform.position = Vector3.SmoothDamp(
                    playerCamera.transform.position,
                    desiredPosition,
                    ref cameraVelocity,
                    0.1f
                );
                
                // Look at target
                playerCamera.transform.LookAt(cameraTarget.position);
            }
            else
            {
                // First person: camera at eye level
                playerCamera.transform.position = transform.position + new Vector3(0, 1.6f, 0);
                playerCamera.transform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0);
            }
        }
        
        private void ToggleViewMode()
        {
            isThirdPerson = !isThirdPerson;
            Debug.Log($"[FantasyPlayer] View mode: {(isThirdPerson ? "Third Person" : "First Person")}");
        }
        
        private void ToggleCursor()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        // ====================================================================
        // PUBLIC API
        // ====================================================================
        
        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            
            if (enabled)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        
        public void SetPosition(Vector3 position)
        {
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
        }
        
        public void SetRotation(float yaw)
        {
            cameraYaw = yaw;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }
        
        public bool IsGrounded => isGrounded;
        public bool IsThirdPerson => isThirdPerson;
        public float Speed => characterController.velocity.magnitude;
    }
}
