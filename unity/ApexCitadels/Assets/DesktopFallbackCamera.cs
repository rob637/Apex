using UnityEngine;

public class DesktopFallbackCamera : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float lookSpeed = 2f;
    
    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        // Check if XR is active - if so, disable this camera
        #if !UNITY_EDITOR
        if (UnityEngine.XR.XRSettings.enabled)
        {
            gameObject.SetActive(false);
            return;
        }
        #endif
    }

    void Update()
    {
        // WASD movement
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float y = 0f;
        if (Input.GetKey(KeyCode.E)) y = 1f;
        if (Input.GetKey(KeyCode.Q)) y = -1f;
        
        Vector3 move = transform.right * h + transform.forward * v + Vector3.up * y;
        transform.position += move * moveSpeed * Time.deltaTime;

        // Mouse look (hold right mouse button)
        if (Input.GetMouseButton(1))
        {
            rotationX -= Input.GetAxis("Mouse Y") * lookSpeed;
            rotationY += Input.GetAxis("Mouse X") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
        }
    }
}
