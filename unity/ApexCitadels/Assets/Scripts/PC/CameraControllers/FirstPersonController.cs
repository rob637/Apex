using Camera = UnityEngine.Camera;
using UnityEngine;
using System;
using System.Collections;

namespace ApexCitadels.PC.CameraControllers
{
    /// <summary>
    /// First-Person Mode Controller for walking through citadel interiors.
    /// Features:
    /// - Smooth WASD movement
    /// - Mouse look with sensitivity
    /// - Head bobbing
    /// - Sprint and crouch
    /// - Collision detection
    /// - Transition from strategic view
    /// </summary>
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float crouchSpeed = 2.5f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 15f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpHeight = 1.5f;
        
        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float maxLookUp = 85f;
        [SerializeField] private float maxLookDown = -85f;
        [SerializeField] private bool invertY = false;
        [SerializeField] private float smoothing = 5f;
        
        [Header("Head Bob")]
        [SerializeField] private bool enableHeadBob = true;
        [SerializeField] private float bobFrequency = 2f;
        [SerializeField] private float bobAmplitudeX = 0.02f;
        [SerializeField] private float bobAmplitudeY = 0.03f;
        [SerializeField] private float sprintBobMultiplier = 1.3f;
        
        [Header("Crouch")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchingHeight = 1f;
        [SerializeField] private float crouchTransitionSpeed = 8f;
        
        [Header("Camera")]
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private UnityEngine.Camera playerCamera;
        [SerializeField] private float defaultFOV = 75f;
        [SerializeField] private float sprintFOV = 85f;
        [SerializeField] private float fovTransitionSpeed = 5f;
        
        [Header("Footsteps")]
        [SerializeField] private AudioSource footstepSource;
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private float footstepInterval = 0.5f;
        [SerializeField] private float sprintFootstepMultiplier = 0.7f;
        
        [Header("References")]
        [SerializeField] private CharacterController characterController;
        
        // State
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private Vector2 _currentLookRotation;
        private Vector2 _targetLookRotation;
        private Vector3 _velocity;
        private Vector3 _currentVelocity;
        private float _currentSpeed;
        private float _targetHeight;
        private float _bobTimer;
        private float _footstepTimer;
        private bool _isGrounded;
        private bool _isSprinting;
        private bool _isCrouching;
        private bool _isActive;
        
        // Input handled via legacy Input system
        
        // Events
        public event Action OnEnterFirstPerson;
        public event Action OnExitFirstPerson;
        public event Action<Vector3> OnPositionChanged;
        
        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
            
            if (playerCamera == null && cameraHolder != null)
            {
                playerCamera = cameraHolder.GetComponentInChildren<UnityEngine.Camera>();
            }
            
            _targetHeight = standingHeight;
        }
        
        private void Update()
        {
            if (!_isActive) return;
            
            // Handle exit
            if (Input.GetKeyDown(KeyCode.Escape)) Deactivate();
            
            HandleInput();
            HandleMovement();
            HandleLook();
            HandleHeadBob();
            HandleCrouch();
            HandleFootsteps();
        }
        
        #region Public API
        
        /// <summary>
        /// Activate first-person mode at position
        /// </summary>
        public void Activate(Vector3 position, float yRotation = 0)
        {
            _isActive = true;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0, yRotation, 0);
            
            _currentLookRotation = new Vector2(0, yRotation);
            _targetLookRotation = _currentLookRotation;
            
            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            OnEnterFirstPerson?.Invoke();
        }
        
        /// <summary>
        /// Deactivate first-person mode
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;
            
            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            OnExitFirstPerson?.Invoke();
        }
        
        /// <summary>
        /// Check if in first-person mode
        /// </summary>
        public bool IsActive => _isActive;
        
        /// <summary>
        /// Get current camera transform
        /// </summary>
        public Transform GetCameraTransform()
        {
            return cameraHolder ?? transform;
        }
        
        /// <summary>
        /// Set mouse sensitivity
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
        }
        
        /// <summary>
        /// Toggle head bob
        /// </summary>
        public void SetHeadBobEnabled(bool enabled)
        {
            enableHeadBob = enabled;
        }
        
        /// <summary>
        /// Teleport to position
        /// </summary>
        public void TeleportTo(Vector3 position)
        {
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
        }
        
        #endregion
        
        #region Movement
        
        private void HandleInput()
        {
            // Movement input (WASD)
            _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            
            // Mouse look
            _lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            
            // Sprint (Shift)
            _isSprinting = Input.GetKey(KeyCode.LeftShift) && !_isCrouching;
            
            // Toggle crouch (Ctrl)
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                _isCrouching = !_isCrouching;
            }
            
            // Jump (Space)
            if (Input.GetKeyDown(KeyCode.Space) && _isGrounded && !_isCrouching)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        
        private void HandleMovement()
        {
            _isGrounded = characterController.isGrounded;
            
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }
            
            // Calculate target speed
            float targetSpeed = _isCrouching ? crouchSpeed : (_isSprinting ? sprintSpeed : walkSpeed);
            
            // Smooth speed transition
            if (_moveInput.magnitude > 0.1f)
            {
                _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, acceleration * Time.deltaTime);
            }
            else
            {
                _currentSpeed = Mathf.Lerp(_currentSpeed, 0, deceleration * Time.deltaTime);
            }
            
            // Calculate movement direction
            Vector3 moveDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            
            // Apply movement
            Vector3 movement = moveDirection * _currentSpeed;
            _currentVelocity = Vector3.Lerp(_currentVelocity, movement, acceleration * Time.deltaTime);
            
            characterController.Move(_currentVelocity * Time.deltaTime);
            
            // Apply gravity
            _velocity.y += gravity * Time.deltaTime;
            characterController.Move(_velocity * Time.deltaTime);
            
            // FOV change when sprinting
            if (playerCamera != null)
            {
                float targetFOV = _isSprinting ? sprintFOV : defaultFOV;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
            }
            
            OnPositionChanged?.Invoke(transform.position);
        }
        
        #endregion
        
        #region Camera
        
        private void HandleLook()
        {
            // Apply sensitivity and invert
            float mouseX = _lookInput.x * mouseSensitivity * 0.1f;
            float mouseY = _lookInput.y * mouseSensitivity * 0.1f * (invertY ? 1 : -1);
            
            // Update target rotation
            _targetLookRotation.x += mouseY;
            _targetLookRotation.y += mouseX;
            
            // Clamp vertical rotation
            _targetLookRotation.x = Mathf.Clamp(_targetLookRotation.x, maxLookDown, maxLookUp);
            
            // Smooth rotation
            _currentLookRotation = Vector2.Lerp(_currentLookRotation, _targetLookRotation, smoothing * Time.deltaTime);
            
            // Apply rotation
            if (cameraHolder != null)
            {
                cameraHolder.localRotation = Quaternion.Euler(_currentLookRotation.x, 0, 0);
            }
            transform.rotation = Quaternion.Euler(0, _currentLookRotation.y, 0);
        }
        
        private void HandleHeadBob()
        {
            if (!enableHeadBob || cameraHolder == null) return;
            
            if (_isGrounded && _currentSpeed > 0.1f)
            {
                float bobMultiplier = _isSprinting ? sprintBobMultiplier : 1f;
                float speedMultiplier = _currentSpeed / walkSpeed;
                
                _bobTimer += Time.deltaTime * bobFrequency * speedMultiplier * bobMultiplier;
                
                float bobX = Mathf.Sin(_bobTimer) * bobAmplitudeX;
                float bobY = Mathf.Abs(Mathf.Sin(_bobTimer * 2)) * bobAmplitudeY;
                
                Vector3 bobOffset = new Vector3(bobX, bobY, 0);
                cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, bobOffset + Vector3.up * (_targetHeight - 0.2f), Time.deltaTime * 10f);
            }
            else
            {
                // Reset bob when standing still
                cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, Vector3.up * (_targetHeight - 0.2f), Time.deltaTime * 5f);
            }
        }
        
        #endregion
        
        #region Crouch
        
        private void HandleCrouch()
        {
            _targetHeight = _isCrouching ? crouchingHeight : standingHeight;
            
            float currentHeight = characterController.height;
            float newHeight = Mathf.Lerp(currentHeight, _targetHeight, crouchTransitionSpeed * Time.deltaTime);
            
            // Check if we can stand up
            if (!_isCrouching && currentHeight < standingHeight)
            {
                // Raycast to check clearance
                if (Physics.Raycast(transform.position, Vector3.up, standingHeight - currentHeight + 0.1f))
                {
                    // Can't stand, stay crouched
                    _isCrouching = true;
                    return;
                }
            }
            
            characterController.height = newHeight;
            characterController.center = Vector3.up * (newHeight / 2);
        }
        
        #endregion
        
        #region Audio
        
        private void HandleFootsteps()
        {
            if (footstepSource == null || footstepClips == null || footstepClips.Length == 0)
                return;
            
            if (!_isGrounded || _currentSpeed < 0.1f)
            {
                _footstepTimer = 0;
                return;
            }
            
            float interval = _isSprinting ? footstepInterval * sprintFootstepMultiplier : footstepInterval;
            _footstepTimer += Time.deltaTime;
            
            if (_footstepTimer >= interval)
            {
                _footstepTimer = 0;
                PlayFootstep();
            }
        }
        
        private void PlayFootstep()
        {
            AudioClip clip = footstepClips[UnityEngine.Random.Range(0, footstepClips.Length)];
            float volume = _isCrouching ? 0.3f : (_isSprinting ? 0.8f : 0.5f);
            footstepSource.PlayOneShot(clip, volume);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Manager for transitioning between strategic view and first-person mode
    /// </summary>
    public class FirstPersonModeManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private UnityEngine.Camera strategicCamera;
        [SerializeField] private GameObject strategicUI;
        [SerializeField] private GameObject firstPersonUI;
        
        [Header("Transition")]
        [SerializeField] private float transitionDuration = 1f;
        [SerializeField] private AnimationCurve transitionCurve;
        
        [Header("Entry Points")]
        [SerializeField] private Transform[] entryPoints;
        
        // Singleton
        private static FirstPersonModeManager _instance;
        public static FirstPersonModeManager Instance => _instance;
        
        // State
        private bool _inFirstPerson;
        private Vector3 _savedCameraPosition;
        private Quaternion _savedCameraRotation;
        private Coroutine _transitionCoroutine;
        
        // Events
        public event Action OnEnterFirstPerson;
        public event Action OnExitFirstPerson;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            if (transitionCurve == null || transitionCurve.length == 0)
            {
                transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
        }
        
        private void Start()
        {
            if (firstPersonController != null)
            {
                firstPersonController.OnExitFirstPerson += ExitFirstPerson;
            }
            
            // Start in strategic view
            firstPersonUI?.SetActive(false);
        }
        
        /// <summary>
        /// Enter first-person mode at a specific location
        /// </summary>
        public void EnterFirstPerson(Vector3 position, float yRotation = 0)
        {
            if (_inFirstPerson || _transitionCoroutine != null) return;
            
            _transitionCoroutine = StartCoroutine(TransitionToFirstPerson(position, yRotation));
        }
        
        /// <summary>
        /// Enter first-person at nearest entry point
        /// </summary>
        public void EnterFirstPersonAtNearestPoint(Vector3 fromPosition)
        {
            if (entryPoints == null || entryPoints.Length == 0)
            {
                EnterFirstPerson(fromPosition);
                return;
            }
            
            Transform nearest = entryPoints[0];
            float nearestDist = float.MaxValue;
            
            foreach (var point in entryPoints)
            {
                if (point == null) continue;
                float dist = Vector3.Distance(fromPosition, point.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = point;
                }
            }
            
            EnterFirstPerson(nearest.position, nearest.eulerAngles.y);
        }
        
        /// <summary>
        /// Exit first-person mode
        /// </summary>
        public void ExitFirstPerson()
        {
            if (!_inFirstPerson || _transitionCoroutine != null) return;
            
            _transitionCoroutine = StartCoroutine(TransitionToStrategic());
        }
        
        /// <summary>
        /// Check if in first-person mode
        /// </summary>
        public bool IsInFirstPerson => _inFirstPerson;
        
        private IEnumerator TransitionToFirstPerson(Vector3 position, float yRotation)
        {
            // Save strategic camera state
            if (strategicCamera != null)
            {
                _savedCameraPosition = strategicCamera.transform.position;
                _savedCameraRotation = strategicCamera.transform.rotation;
            }
            
            // Hide strategic UI
            strategicUI?.SetActive(false);
            
            // Animate camera to first-person position
            var fpCamera = firstPersonController?.GetCameraTransform();
            
            if (strategicCamera != null && fpCamera != null)
            {
                Vector3 startPos = strategicCamera.transform.position;
                Quaternion startRot = strategicCamera.transform.rotation;
                Vector3 endPos = position + Vector3.up * 1.6f;
                Quaternion endRot = Quaternion.Euler(0, yRotation, 0);
                
                float elapsed = 0;
                while (elapsed < transitionDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = transitionCurve.Evaluate(elapsed / transitionDuration);
                    
                    strategicCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
                    strategicCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                    
                    yield return null;
                }
            }
            
            // Disable strategic camera
            if (strategicCamera != null)
            {
                strategicCamera.gameObject.SetActive(false);
            }
            
            // Enable first-person
            firstPersonController?.gameObject.SetActive(true);
            firstPersonController?.Activate(position, yRotation);
            
            // Show first-person UI
            firstPersonUI?.SetActive(true);
            
            _inFirstPerson = true;
            _transitionCoroutine = null;
            
            OnEnterFirstPerson?.Invoke();
        }
        
        private IEnumerator TransitionToStrategic()
        {
            // Get current first-person position
            Vector3 fpPosition = firstPersonController?.transform.position ?? Vector3.zero;
            
            // Deactivate first-person
            firstPersonController?.Deactivate();
            
            // Hide first-person UI
            firstPersonUI?.SetActive(false);
            
            // Enable strategic camera
            if (strategicCamera != null)
            {
                strategicCamera.gameObject.SetActive(true);
                strategicCamera.transform.position = fpPosition + Vector3.up * 5f;
                strategicCamera.transform.rotation = Quaternion.Euler(45, 0, 0);
            }
            
            // Animate back to saved position
            if (strategicCamera != null)
            {
                Vector3 startPos = strategicCamera.transform.position;
                Quaternion startRot = strategicCamera.transform.rotation;
                
                float elapsed = 0;
                while (elapsed < transitionDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = transitionCurve.Evaluate(elapsed / transitionDuration);
                    
                    strategicCamera.transform.position = Vector3.Lerp(startPos, _savedCameraPosition, t);
                    strategicCamera.transform.rotation = Quaternion.Slerp(startRot, _savedCameraRotation, t);
                    
                    yield return null;
                }
            }
            
            // Hide first-person controller
            firstPersonController?.gameObject.SetActive(false);
            
            // Show strategic UI
            strategicUI?.SetActive(true);
            
            _inFirstPerson = false;
            _transitionCoroutine = null;
            
            OnExitFirstPerson?.Invoke();
        }
    }
    
    /// <summary>
    /// First-person interaction handler
    /// </summary>
    public class FirstPersonInteraction : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionLayer;
        [SerializeField] private UnityEngine.Camera fpCamera;
        
        [Header("UI")]
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private TMPro.TextMeshProUGUI promptText;
        
        private IInteractable _currentTarget;
        
        private void Update()
        {
            CheckForInteractable();
            
            // Handle interaction input
            if (Input.GetKeyDown(KeyCode.E) && _currentTarget != null)
            {
                _currentTarget.Interact();
            }
        }
        
        private void CheckForInteractable()
        {
            if (fpCamera == null) return;
            
            Ray ray = new Ray(fpCamera.transform.position, fpCamera.transform.forward);
            
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayer))
            {
                var interactable = hit.collider.GetComponent<IInteractable>();
                
                if (interactable != null)
                {
                    if (_currentTarget != interactable)
                    {
                        _currentTarget = interactable;
                        ShowPrompt(interactable.GetInteractionPrompt());
                    }
                    return;
                }
            }
            
            // No target
            if (_currentTarget != null)
            {
                _currentTarget = null;
                HidePrompt();
            }
        }
        
        private void ShowPrompt(string text)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
            }
            if (promptText != null)
            {
                promptText.text = $"[E] {text}";
            }
        }
        
        private void HidePrompt()
        {
            interactionPrompt?.SetActive(false);
        }
    }
    
    /// <summary>
    /// Interface for interactable objects
    /// </summary>
    public interface IInteractable
    {
        string GetInteractionPrompt();
        void Interact();
    }
    
    /// <summary>
    /// First-person HUD
    /// </summary>
    public class FirstPersonHUD : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private GameObject crosshair;
        [SerializeField] private TMPro.TextMeshProUGUI locationText;
        [SerializeField] private TMPro.TextMeshProUGUI compassText;
        [SerializeField] private RectTransform compassNeedle;
        [SerializeField] private UnityEngine.UI.Image minimap;
        
        [Header("Settings")]
        [SerializeField] private bool showCrosshair = true;
        [SerializeField] private bool showCompass = true;
        [SerializeField] private bool showMinimap = true;
        
        private Transform _playerTransform;
        
        private void Start()
        {
            var fpController = FindObjectOfType<FirstPersonController>();
            if (fpController != null)
            {
                _playerTransform = fpController.transform;
            }
            
            crosshair?.SetActive(showCrosshair);
            compassText?.gameObject.SetActive(showCompass);
            minimap?.gameObject.SetActive(showMinimap);
        }
        
        private void Update()
        {
            UpdateCompass();
        }
        
        private void UpdateCompass()
        {
            if (_playerTransform == null || compassNeedle == null) return;
            
            float yRotation = _playerTransform.eulerAngles.y;
            compassNeedle.localRotation = Quaternion.Euler(0, 0, yRotation);
            
            // Direction text
            if (compassText != null)
            {
                string direction = GetCardinalDirection(yRotation);
                compassText.text = direction;
            }
        }
        
        private string GetCardinalDirection(float angle)
        {
            angle = (angle + 360) % 360;
            
            if (angle < 22.5f || angle >= 337.5f) return "N";
            if (angle < 67.5f) return "NE";
            if (angle < 112.5f) return "E";
            if (angle < 157.5f) return "SE";
            if (angle < 202.5f) return "S";
            if (angle < 247.5f) return "SW";
            if (angle < 292.5f) return "W";
            return "NW";
        }
        
        public void SetLocation(string location)
        {
            if (locationText != null)
            {
                locationText.text = location;
            }
        }
        
        public void ToggleCrosshair(bool show)
        {
            showCrosshair = show;
            crosshair?.SetActive(show);
        }
        
        public void ToggleCompass(bool show)
        {
            showCompass = show;
            compassText?.gameObject.SetActive(show);
            compassNeedle?.gameObject.SetActive(show);
        }
        
        public void ToggleMinimap(bool show)
        {
            showMinimap = show;
            minimap?.gameObject.SetActive(show);
        }
    }
}
