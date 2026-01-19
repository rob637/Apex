using UnityEngine;
using System;
using System.Collections;

namespace ApexCitadels.PC.GeoMapping
{
    /// <summary>
    /// Fantasy Walking Controller - Immersive first-person exploration through
    /// the fantasy-rendered neighborhood. Features medieval-style movement,
    /// interaction with buildings and NPCs, and seamless integration with the world.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FantasyWalkingController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float crouchSpeed = 2f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float gravity = 20f;
        
        [Header("Look Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float lookUpLimit = 80f;
        [SerializeField] private float lookDownLimit = -80f;
        [SerializeField] private bool invertY = false;
        
        [Header("Camera Settings")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float defaultCameraHeight = 1.7f;
        [SerializeField] private float crouchCameraHeight = 0.9f;
        [SerializeField] private float cameraHeightSmoothTime = 0.2f;
        
        [Header("Head Bob")]
        [SerializeField] private bool enableHeadBob = true;
        [SerializeField] private float walkBobSpeed = 14f;
        [SerializeField] private float runBobSpeed = 18f;
        [SerializeField] private float walkBobAmount = 0.03f;
        [SerializeField] private float runBobAmount = 0.06f;
        
        [Header("Footsteps")]
        [SerializeField] private AudioSource footstepAudioSource;
        [SerializeField] private AudioClip[] stoneFootsteps;
        [SerializeField] private AudioClip[] dirtFootsteps;
        [SerializeField] private AudioClip[] grassFootsteps;
        [SerializeField] private AudioClip[] woodFootsteps;
        [SerializeField] private float walkStepInterval = 0.5f;
        [SerializeField] private float runStepInterval = 0.3f;
        
        [Header("Interaction")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private GameObject interactionPromptUI;
        
        [Header("Stamina System")]
        [SerializeField] private bool useStamina = true;
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaDrainRate = 20f;
        [SerializeField] private float staminaRegenRate = 10f;
        [SerializeField] private float staminaRegenDelay = 1f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        // Components
        private CharacterController _characterController;
        private Camera _camera;
        
        // State
        private Vector3 _moveDirection = Vector3.zero;
        private float _verticalRotation = 0f;
        private float _currentCameraHeight;
        private float _cameraHeightVelocity;
        private bool _isGrounded = true;
        private bool _isRunning = false;
        private bool _isCrouching = false;
        private bool _isMoving = false;
        
        // Stamina
        private float _currentStamina;
        private float _staminaRegenTimer = 0f;
        
        // Head bob
        private float _headBobTimer = 0f;
        private Vector3 _originalCameraPosition;
        
        // Footsteps
        private float _footstepTimer = 0f;
        private SurfaceType _currentSurface = SurfaceType.Stone;
        
        // Interaction
        private IInteractable _currentInteractable;
        
        // Events
        public event Action OnJump;
        public event Action<bool> OnRunningChanged;
        public event Action<bool> OnCrouchingChanged;
        public event Action<IInteractable> OnInteract;
        public event Action<float> OnStaminaChanged;
        
        public bool IsMoving => _isMoving;
        public bool IsRunning => _isRunning;
        public bool IsCrouching => _isCrouching;
        public bool IsGrounded => _isGrounded;
        public float CurrentStamina => _currentStamina;
        public float StaminaPercent => _currentStamina / maxStamina;
        
        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            
            if (cameraTransform == null)
            {
                _camera = GetComponentInChildren<Camera>();
                if (_camera != null) cameraTransform = _camera.transform;
            }
            else
            {
                _camera = cameraTransform.GetComponent<Camera>();
            }
            
            _currentCameraHeight = defaultCameraHeight;
            _currentStamina = maxStamina;
            
            if (cameraTransform != null)
            {
                _originalCameraPosition = cameraTransform.localPosition;
            }
        }
        
        private void Start()
        {
            // Lock cursor for FPS control
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        private void Update()
        {
            HandleInput();
            HandleMovement();
            HandleLook();
            HandleHeadBob();
            HandleFootsteps();
            HandleInteraction();
            HandleStamina();
            UpdateCameraHeight();
        }
        
        #region Input Handling
        
        private void HandleInput()
        {
            // Running
            bool wantsToRun = Input.GetKey(KeyCode.LeftShift) && !_isCrouching;
            bool canRun = _currentStamina > 0 || !useStamina;
            
            if (wantsToRun && canRun && _isMoving != _isRunning)
            {
                _isRunning = true;
                OnRunningChanged?.Invoke(true);
            }
            else if (!wantsToRun && _isRunning)
            {
                _isRunning = false;
                OnRunningChanged?.Invoke(false);
            }
            
            // Crouching
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
            {
                _isCrouching = !_isCrouching;
                _characterController.height = _isCrouching ? 1f : 2f;
                _characterController.center = _isCrouching ? new Vector3(0, 0.5f, 0) : new Vector3(0, 1f, 0);
                OnCrouchingChanged?.Invoke(_isCrouching);
                
                if (_isCrouching) _isRunning = false;
            }
            
            // Jump
            if (Input.GetButtonDown("Jump") && _isGrounded && !_isCrouching)
            {
                _moveDirection.y = jumpForce;
                OnJump?.Invoke();
            }
            
            // Interaction
            if (Input.GetKeyDown(KeyCode.E) && _currentInteractable != null)
            {
                _currentInteractable.Interact(this);
                OnInteract?.Invoke(_currentInteractable);
            }
            
            // Cursor unlock
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            
            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        #endregion
        
        #region Movement
        
        private void HandleMovement()
        {
            _isGrounded = _characterController.isGrounded;
            
            if (_isGrounded && _moveDirection.y < 0)
            {
                _moveDirection.y = -1f; // Small downward force to keep grounded
            }
            
            // Get input
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            
            _isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;
            
            // Calculate movement direction
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            
            float currentSpeed = _isCrouching ? crouchSpeed : (_isRunning ? runSpeed : walkSpeed);
            
            Vector3 moveInput = (forward * vertical + right * horizontal).normalized;
            _moveDirection.x = moveInput.x * currentSpeed;
            _moveDirection.z = moveInput.z * currentSpeed;
            
            // Apply gravity
            _moveDirection.y -= gravity * Time.deltaTime;
            
            // Move
            _characterController.Move(_moveDirection * Time.deltaTime);
            
            // Detect surface
            DetectSurface();
        }
        
        private void DetectSurface()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2f))
            {
                string tag = hit.collider.tag;
                
                _currentSurface = tag switch
                {
                    "Stone" or "Road" => SurfaceType.Stone,
                    "Dirt" or "Path" => SurfaceType.Dirt,
                    "Grass" or "Forest" => SurfaceType.Grass,
                    "Wood" or "Floor" => SurfaceType.Wood,
                    _ => SurfaceType.Stone
                };
            }
        }
        
        #endregion
        
        #region Camera & Look
        
        private void HandleLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;
            
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertY ? 1 : -1);
            
            // Horizontal rotation (body)
            transform.Rotate(Vector3.up, mouseX);
            
            // Vertical rotation (camera)
            _verticalRotation += mouseY;
            _verticalRotation = Mathf.Clamp(_verticalRotation, lookDownLimit, lookUpLimit);
            
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(_verticalRotation, 0, 0);
            }
        }
        
        private void UpdateCameraHeight()
        {
            if (cameraTransform == null) return;
            
            float targetHeight = _isCrouching ? crouchCameraHeight : defaultCameraHeight;
            _currentCameraHeight = Mathf.SmoothDamp(_currentCameraHeight, targetHeight, ref _cameraHeightVelocity, cameraHeightSmoothTime);
            
            Vector3 camPos = cameraTransform.localPosition;
            camPos.y = _currentCameraHeight;
            cameraTransform.localPosition = camPos;
        }
        
        private void HandleHeadBob()
        {
            if (!enableHeadBob || cameraTransform == null) return;
            
            if (_isMoving && _isGrounded)
            {
                float bobSpeed = _isRunning ? runBobSpeed : walkBobSpeed;
                float bobAmount = _isRunning ? runBobAmount : walkBobAmount;
                
                _headBobTimer += Time.deltaTime * bobSpeed;
                
                float bobY = Mathf.Sin(_headBobTimer) * bobAmount;
                float bobX = Mathf.Cos(_headBobTimer * 0.5f) * bobAmount * 0.5f;
                
                Vector3 camPos = cameraTransform.localPosition;
                camPos.x = _originalCameraPosition.x + bobX;
                camPos.y = _currentCameraHeight + bobY;
                cameraTransform.localPosition = camPos;
            }
            else
            {
                // Smoothly return to original position
                _headBobTimer = 0;
                Vector3 camPos = cameraTransform.localPosition;
                camPos.x = Mathf.Lerp(camPos.x, _originalCameraPosition.x, Time.deltaTime * 5f);
                cameraTransform.localPosition = camPos;
            }
        }
        
        #endregion
        
        #region Footsteps
        
        private void HandleFootsteps()
        {
            if (!_isMoving || !_isGrounded || footstepAudioSource == null) return;
            
            float stepInterval = _isRunning ? runStepInterval : walkStepInterval;
            _footstepTimer += Time.deltaTime;
            
            if (_footstepTimer >= stepInterval)
            {
                _footstepTimer = 0;
                PlayFootstep();
            }
        }
        
        private void PlayFootstep()
        {
            AudioClip[] clips = _currentSurface switch
            {
                SurfaceType.Stone => stoneFootsteps,
                SurfaceType.Dirt => dirtFootsteps,
                SurfaceType.Grass => grassFootsteps,
                SurfaceType.Wood => woodFootsteps,
                _ => stoneFootsteps
            };
            
            if (clips != null && clips.Length > 0)
            {
                AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
                if (clip != null)
                {
                    footstepAudioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                    footstepAudioSource.PlayOneShot(clip);
                }
            }
        }
        
        #endregion
        
        #region Interaction
        
        private void HandleInteraction()
        {
            // Raycast for interactables
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayer))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                
                if (interactable != null && interactable != _currentInteractable)
                {
                    _currentInteractable = interactable;
                    ShowInteractionPrompt(interactable.GetInteractionPrompt());
                }
            }
            else if (_currentInteractable != null)
            {
                _currentInteractable = null;
                HideInteractionPrompt();
            }
        }
        
        private void ShowInteractionPrompt(string prompt)
        {
            if (interactionPromptUI != null)
            {
                interactionPromptUI.SetActive(true);
                // Set text if there's a text component
            }
        }
        
        private void HideInteractionPrompt()
        {
            if (interactionPromptUI != null)
            {
                interactionPromptUI.SetActive(false);
            }
        }
        
        #endregion
        
        #region Stamina
        
        private void HandleStamina()
        {
            if (!useStamina) return;
            
            if (_isRunning && _isMoving)
            {
                _currentStamina -= staminaDrainRate * Time.deltaTime;
                _currentStamina = Mathf.Max(0, _currentStamina);
                _staminaRegenTimer = staminaRegenDelay;
                
                if (_currentStamina <= 0)
                {
                    _isRunning = false;
                    OnRunningChanged?.Invoke(false);
                }
                
                OnStaminaChanged?.Invoke(_currentStamina);
            }
            else
            {
                _staminaRegenTimer -= Time.deltaTime;
                
                if (_staminaRegenTimer <= 0 && _currentStamina < maxStamina)
                {
                    _currentStamina += staminaRegenRate * Time.deltaTime;
                    _currentStamina = Mathf.Min(maxStamina, _currentStamina);
                    OnStaminaChanged?.Invoke(_currentStamina);
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Teleport player to position
        /// </summary>
        public void TeleportTo(Vector3 position)
        {
            _characterController.enabled = false;
            transform.position = position;
            _characterController.enabled = true;
        }
        
        /// <summary>
        /// Set look direction
        /// </summary>
        public void SetLookDirection(Vector3 forward)
        {
            Vector3 flatForward = new Vector3(forward.x, 0, forward.z).normalized;
            transform.rotation = Quaternion.LookRotation(flatForward);
            
            _verticalRotation = Vector3.SignedAngle(flatForward, forward, transform.right);
            _verticalRotation = Mathf.Clamp(_verticalRotation, lookDownLimit, lookUpLimit);
            
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(_verticalRotation, 0, 0);
            }
        }
        
        /// <summary>
        /// Enable/disable movement
        /// </summary>
        public void SetMovementEnabled(bool enabled)
        {
            this.enabled = enabled;
            
            if (!enabled)
            {
                _moveDirection = Vector3.zero;
            }
        }
        
        /// <summary>
        /// Force crouch state
        /// </summary>
        public void SetCrouching(bool crouch)
        {
            if (_isCrouching != crouch)
            {
                _isCrouching = crouch;
                _characterController.height = _isCrouching ? 1f : 2f;
                _characterController.center = _isCrouching ? new Vector3(0, 0.5f, 0) : new Vector3(0, 1f, 0);
                OnCrouchingChanged?.Invoke(_isCrouching);
            }
        }
        
        /// <summary>
        /// Add external velocity (for knockback, etc.)
        /// </summary>
        public void AddForce(Vector3 force)
        {
            _moveDirection += force;
        }
        
        #endregion
        
        #region Debug
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 200, 200));
            GUILayout.Label($"Speed: {new Vector3(_moveDirection.x, 0, _moveDirection.z).magnitude:F1} m/s");
            GUILayout.Label($"Grounded: {_isGrounded}");
            GUILayout.Label($"Running: {_isRunning}");
            GUILayout.Label($"Crouching: {_isCrouching}");
            GUILayout.Label($"Stamina: {_currentStamina:F0}/{maxStamina}");
            GUILayout.Label($"Surface: {_currentSurface}");
            GUILayout.EndArea();
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    public enum SurfaceType
    {
        Stone,
        Dirt,
        Grass,
        Wood,
        Water
    }
    
    public interface IInteractable
    {
        string GetInteractionPrompt();
        void Interact(FantasyWalkingController player);
    }
    
    #endregion
}
