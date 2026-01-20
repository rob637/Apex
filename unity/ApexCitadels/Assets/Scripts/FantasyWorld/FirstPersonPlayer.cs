using UnityEngine;

namespace ApexCitadels.FantasyWorld
{
    /// <summary>
    /// A simple First Person Controller for walking on the generated terrain.
    /// Replaces the Fly Camera for a more grounded experience.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonPlayer : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 5f;
        public float runSpeed = 10f;
        public float jumpHeight = 1.5f;
        public float gravity = -20f;

        [Header("Look")]
        public Transform cameraTransform;
        public float mouseSensitivity = 2f;
        public float lookXLimit = 85f;

        private CharacterController _characterController;
        private Vector3 _velocity;
        private float _rotationX = 0;
        
        // State
        private bool _isGrounded;

        private void Start()
        {
            _characterController = GetComponent<CharacterController>();

            // If camera not assigned, try to find the main camera
            if (cameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
                else
                {
                    // If no camera on us, maybe grab the main camera and attach it
                    if (Camera.main != null)
                    {
                        cameraTransform = Camera.main.transform;
                        cameraTransform.SetParent(transform);
                        cameraTransform.localPosition = new Vector3(0, 1.6f, 0); // Eye height
                        cameraTransform.localRotation = Quaternion.identity;
                    }
                }
            }

            // Lock Cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            // Cursor unlock
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Calculate movement
            _isGrounded = _characterController.isGrounded;
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Small downward force to keep grounded
            }

            // Input
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float curSpeedX = (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical");
            float curSpeedY = (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal");
            
            // Move direction (Y is handled by gravity)
            float movementDirectionY = _velocity.y;
            Vector3 move = (forward * curSpeedX) + (right * curSpeedY);

            // Jump
            if (Input.GetButton("Jump") && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            else
            {
                _velocity.y = movementDirectionY;
            }

            // Apply Gravity
            _velocity.y += gravity * Time.deltaTime;

            // Move Controller
            _characterController.Move((move + _velocity) * Time.deltaTime);

            // Rotation
            if (cameraTransform != null && Cursor.lockState == CursorLockMode.Locked)
            {
                _rotationX += -Input.GetAxis("Mouse Y") * mouseSensitivity;
                _rotationX = Mathf.Clamp(_rotationX, -lookXLimit, lookXLimit);
                cameraTransform.localRotation = Quaternion.Euler(_rotationX, 0, 0);
                transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * mouseSensitivity, 0);
            }
        }
    }
}
