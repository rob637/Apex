using UnityEngine;

// Run before AR Foundation components
[DefaultExecutionOrder(-1000)]
public class DesktopFallbackCamera : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float lookSpeed = 2f;
    
    private float rotationX = 0f;
    private float rotationY = 0f;
    private Camera cam;

    void Awake()
    {
        Debug.Log("[DesktopFallback] Awake called - disabling AR first");
        
        // IMMEDIATELY disable AR GameObjects before they initialize
        DisableARGameObjects();
        
        cam = GetComponent<Camera>();
        
        if (cam == null)
        {
            Debug.LogError("[DesktopFallback] No Camera component found!");
            return;
        }
        
        // Tag as MainCamera so Camera.main works
        gameObject.tag = "MainCamera";
        
        // Force camera settings
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.2f, 0.3f, 0.4f); // Blue-grey sky
        cam.cullingMask = -1; // Everything
        cam.depth = 100; // Render on top of other cameras
        cam.enabled = true;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;
        
        // Position camera to look at ground
        transform.position = new Vector3(0, 1.6f, -5f);
        transform.rotation = Quaternion.Euler(15f, 0f, 0f); // Look slightly down
        rotationX = 15f;
        rotationY = 0f;
        
        Debug.Log($"[DesktopFallback] Camera configured: enabled={cam.enabled}, depth={cam.depth}, pos={transform.position}");
        
        // FORCE create objects immediately in Awake to ensure they exist
        CreateDebugEnvironmentImmediate();
    }
    
    void CreateDebugEnvironmentImmediate()
    {
        Debug.Log("[DesktopFallback] Creating objects in Awake...");
        
        // Simple cube right in front of camera
        GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube.name = "TestCube_Immediate";
        testCube.transform.position = new Vector3(0, 0.5f, 2f);
        testCube.transform.localScale = Vector3.one * 0.5f;
        Debug.Log($"[DesktopFallback] Created test cube at {testCube.transform.position}");
    }
    
    void DisableARGameObjects()
    {
        // Find and disable entire AR Session GameObject
        var arSession = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARSession>(FindObjectsInactive.Include);
        if (arSession != null)
        {
            Debug.Log($"[DesktopFallback] Disabling AR Session GameObject: {arSession.gameObject.name}");
            arSession.gameObject.SetActive(false);
        }
        
        // Find and disable XR Origin / AR Session Origin
        var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsInactive.Include);
        if (xrOrigin != null)
        {
            Debug.Log($"[DesktopFallback] Disabling XR Origin GameObject: {xrOrigin.gameObject.name}");
            xrOrigin.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        Debug.Log("[DesktopFallback] Start called");
        
        // Disable any remaining cameras
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        Debug.Log($"[DesktopFallback] Found {allCameras.Length} cameras");
        foreach (var c in allCameras)
        {
            if (c != cam)
            {
                Debug.Log($"[DesktopFallback] Disabling camera: {c.gameObject.name}");
                c.enabled = false;
            }
        }
        
        // Create visible environment
        CreateDebugEnvironment();
        
        // Add a light if none exists
        if (FindFirstObjectByType<Light>() == null)
        {
            var lightGO = new GameObject("DebugLight");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            Debug.Log("[DesktopFallback] Created directional light");
        }
    }
    
    void DisableARComponents()
    {
        // Disable AR Session
        var arSession = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARSession>();
        if (arSession != null)
        {
            Debug.Log("[DesktopFallback] Disabling AR Session");
            arSession.enabled = false;
        }
        
        // Disable AR Camera Background
        var arCamBg = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARCameraBackground>();
        if (arCamBg != null)
        {
            Debug.Log("[DesktopFallback] Disabling AR Camera Background");
            arCamBg.enabled = false;
        }
        
        // Disable AR Camera Manager
        var arCamMgr = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARCameraManager>();
        if (arCamMgr != null)
        {
            Debug.Log("[DesktopFallback] Disabling AR Camera Manager");
            arCamMgr.enabled = false;
        }
    }

    void CreateDebugEnvironment()
    {
        Debug.Log("[DesktopFallback] Creating debug environment...");
        
        // Create ground plane - use the default material from primitives
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "DebugGround";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(10, 1, 10);
        
        // Get the default material and just tint it
        var groundRenderer = ground.GetComponent<Renderer>();
        if (groundRenderer.material != null)
        {
            // Try URP property first, fall back to standard
            if (groundRenderer.material.HasProperty("_BaseColor"))
                groundRenderer.material.SetColor("_BaseColor", new Color(0.3f, 0.5f, 0.3f));
            else if (groundRenderer.material.HasProperty("_Color"))
                groundRenderer.material.SetColor("_Color", new Color(0.3f, 0.5f, 0.3f));
        }
        
        Debug.Log("[DesktopFallback] Ground created");
        
        // Create some reference cubes
        Color[] colors = { Color.red, Color.blue, Color.yellow, Color.magenta };
        for (int i = 0; i < 4; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"DebugCube_{i}";
            float angle = i * 90f * Mathf.Deg2Rad;
            cube.transform.position = new Vector3(Mathf.Sin(angle) * 5f, 0.5f, Mathf.Cos(angle) * 5f);
            
            var cubeRenderer = cube.GetComponent<Renderer>();
            if (cubeRenderer.material != null)
            {
                if (cubeRenderer.material.HasProperty("_BaseColor"))
                    cubeRenderer.material.SetColor("_BaseColor", colors[i]);
                else if (cubeRenderer.material.HasProperty("_Color"))
                    cubeRenderer.material.SetColor("_Color", colors[i]);
            }
        }
        
        Debug.Log("[DesktopFallback] Debug environment created with 4 cubes");
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
