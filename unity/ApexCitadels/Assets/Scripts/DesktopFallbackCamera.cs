using UnityEngine;
using ApexCitadels.Core;

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
        ApexLogger.Log("[DesktopFallback] Awake called - disabling AR first", ApexLogger.LogCategory.General);
        
        // IMMEDIATELY disable AR GameObjects before they initialize
        DisableARGameObjects();
        
        cam = GetComponent<Camera>();
        
        if (cam == null)
        {
            ApexLogger.LogError("[DesktopFallback] No Camera component found!", ApexLogger.LogCategory.General);
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
        
        ApexLogger.Log($"[DesktopFallback] Camera configured: enabled={cam.enabled}, depth={cam.depth}, pos={transform.position}", ApexLogger.LogCategory.General);
        
        // FORCE create objects immediately in Awake to ensure they exist
        CreateDebugEnvironmentImmediate();
    }
    
    void CreateDebugEnvironmentImmediate()
    {
        ApexLogger.Log("[DesktopFallback] Creating objects in Awake...", ApexLogger.LogCategory.General);
        
        // Simple cube right in front of camera
        GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube.name = "TestCube_Immediate";
        testCube.transform.position = new Vector3(0, 0.5f, 2f);
        testCube.transform.localScale = Vector3.one * 0.5f;
        ApexLogger.Log($"[DesktopFallback] Created test cube at {testCube.transform.position}", ApexLogger.LogCategory.General);
    }
    
    void DisableARGameObjects()
    {
        // Find and disable entire AR Session GameObject
        var arSession = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARSession>(FindObjectsInactive.Include);
        if (arSession != null)
        {
            ApexLogger.Log($"[DesktopFallback] Disabling AR Session GameObject: {arSession.gameObject.name}", ApexLogger.LogCategory.General);
            arSession.gameObject.SetActive(false);
        }
        
        // Find and disable XR Origin / AR Session Origin
        var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsInactive.Include);
        if (xrOrigin != null)
        {
            ApexLogger.Log($"[DesktopFallback] Disabling XR Origin GameObject: {xrOrigin.gameObject.name}", ApexLogger.LogCategory.General);
            xrOrigin.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        ApexLogger.Log("[DesktopFallback] Start called", ApexLogger.LogCategory.General);
        
        // Disable any remaining cameras
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        ApexLogger.Log($"[DesktopFallback] Found {allCameras.Length} cameras", ApexLogger.LogCategory.General);
        foreach (var c in allCameras)
        {
            if (c != cam)
            {
                ApexLogger.Log($"[DesktopFallback] Disabling camera: {c.gameObject.name}", ApexLogger.LogCategory.General);
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
            ApexLogger.Log("[DesktopFallback] Created directional light", ApexLogger.LogCategory.General);
        }
    }
    
    void DisableARComponents()
    {
        // Disable AR Session
        var arSession = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARSession>();
        if (arSession != null)
        {
            ApexLogger.Log("[DesktopFallback] Disabling AR Session", ApexLogger.LogCategory.General);
            arSession.enabled = false;
        }
        
        // Disable AR Camera Background
        var arCamBg = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARCameraBackground>();
        if (arCamBg != null)
        {
            ApexLogger.Log("[DesktopFallback] Disabling AR Camera Background", ApexLogger.LogCategory.General);
            arCamBg.enabled = false;
        }
        
        // Disable AR Camera Manager
        var arCamMgr = FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARCameraManager>();
        if (arCamMgr != null)
        {
            ApexLogger.Log("[DesktopFallback] Disabling AR Camera Manager", ApexLogger.LogCategory.General);
            arCamMgr.enabled = false;
        }
    }

    void CreateDebugEnvironment()
    {
        ApexLogger.Log("[DesktopFallback] Creating debug environment...", ApexLogger.LogCategory.General);
        
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
        
        ApexLogger.Log("[DesktopFallback] Ground created", ApexLogger.LogCategory.General);
        
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
        
        ApexLogger.Log("[DesktopFallback] Debug environment created with 4 cubes", ApexLogger.LogCategory.General);
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
