// ============================================================================
// SIMPLE FLY CAMERA - For testing Fantasy World
// WASD to move, mouse to look, Space/Ctrl for up/down
// ============================================================================
using UnityEngine;

namespace ApexCitadels.FantasyWorld
{
    public class SimpleFlyCamera : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 20f;
        public float fastMultiplier = 3f;
        
        [Header("Look")]
        public float lookSensitivity = 2f;
        public float maxPitch = 89f;
        
        private float _yaw;
        private float _pitch;
        private bool _isLooking;
        
        private void Start()
        {
            // Initialize rotation from current transform
            _yaw = transform.eulerAngles.y;
            _pitch = transform.eulerAngles.x;
            
            // Lock cursor when right-clicking
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        private void Update()
        {
            HandleLook();
            HandleMovement();
        }
        
        private void HandleLook()
        {
            // Right mouse button to look around
            if (Input.GetMouseButtonDown(1))
            {
                _isLooking = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            
            if (Input.GetMouseButtonUp(1))
            {
                _isLooking = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            
            if (_isLooking)
            {
                _yaw += Input.GetAxis("Mouse X") * lookSensitivity;
                _pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
                _pitch = Mathf.Clamp(_pitch, -maxPitch, maxPitch);
                
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0);
            }
        }
        
        private void HandleMovement()
        {
            float speed = moveSpeed;
            
            // Hold Shift to move faster
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                speed *= fastMultiplier;
            }
            
            Vector3 move = Vector3.zero;
            
            // WASD movement
            if (Input.GetKey(KeyCode.W)) move += transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= transform.forward;
            if (Input.GetKey(KeyCode.A)) move -= transform.right;
            if (Input.GetKey(KeyCode.D)) move += transform.right;
            
            // Space/Q for up/down
            if (Input.GetKey(KeyCode.Space)) move += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;
            
            // Arrow keys also work
            if (Input.GetKey(KeyCode.UpArrow)) move += transform.forward;
            if (Input.GetKey(KeyCode.DownArrow)) move -= transform.forward;
            if (Input.GetKey(KeyCode.LeftArrow)) move -= transform.right;
            if (Input.GetKey(KeyCode.RightArrow)) move += transform.right;
            
            transform.position += move.normalized * speed * Time.deltaTime;
        }
        
        private void OnGUI()
        {
            // Show controls hint
            GUILayout.BeginArea(new Rect(10, Screen.height - 80, 400, 70));
            GUILayout.Label("<b>Controls:</b> WASD to move, Right-click + Mouse to look", 
                new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label("Space = Up, Q = Down, Shift = Fast");
            GUILayout.EndArea();
        }
    }
}
