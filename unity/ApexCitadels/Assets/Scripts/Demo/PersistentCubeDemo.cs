using UnityEngine;
using UnityEngine.UI;
using ApexCitadels.AR;
using ApexCitadels.Backend;

namespace ApexCitadels.Demo
{
    /// <summary>
    /// Demo script for testing persistent AR cubes.
    /// Tap to place a cube, and it will be saved to the cloud.
    /// Auto-creates UI if not assigned in Inspector.
    /// </summary>
    public class PersistentCubeDemo : MonoBehaviour
    {
        [Header("UI (Auto-created if not assigned)")]
        [SerializeField] private Button _placeCubeButton;
        [SerializeField] private Button _loadCubesButton;
        [SerializeField] private Button _clearCubesButton;
        [SerializeField] private Text _statusText;

        [Header("Prefab")]
        [SerializeField] private GameObject _cubePrefab;

        private bool _isPlacementMode = false;
        private Canvas _autoCanvas;

        private void Start()
        {
            // Auto-create UI if not assigned
            if (_placeCubeButton == null || _statusText == null)
            {
                CreateUI();
            }

            if (_placeCubeButton != null)
                _placeCubeButton.onClick.AddListener(OnPlaceCubeClicked);
            
            if (_loadCubesButton != null)
                _loadCubesButton.onClick.AddListener(OnLoadCubesClicked);
            
            if (_clearCubesButton != null)
                _clearCubesButton.onClick.AddListener(OnClearCubesClicked);

            UpdateStatus("Initializing...");

            // Wait for services to initialize
            if (AnchorPersistenceService.Instance != null)
            {
                AnchorPersistenceService.Instance.OnInitialized += OnServicesReady;
                if (AnchorPersistenceService.Instance.IsInitialized)
                {
                    OnServicesReady();
                }
            }
            else
            {
                Invoke(nameof(OnServicesReady), 1f);
            }
        }

        private void CreateUI()
        {
            Debug.Log("[PersistentCubeDemo] Auto-creating UI...");

            // Create Canvas
            var canvasGO = new GameObject("DemoCanvas_AutoCreated");
            _autoCanvas = canvasGO.AddComponent<Canvas>();
            _autoCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Find a font - try multiple options
            Font font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            if (font == null) font = Font.CreateDynamicFontFromOSFont(Font.GetOSInstalledFontNames()[0], 24);

            // Create Status Text at top
            var statusGO = new GameObject("StatusText");
            statusGO.transform.SetParent(canvasGO.transform, false);
            _statusText = statusGO.AddComponent<Text>();
            _statusText.font = font;
            _statusText.fontSize = 24;
            _statusText.alignment = TextAnchor.UpperCenter;
            _statusText.color = Color.white;
            _statusText.text = "Initializing...";
            
            var statusRect = statusGO.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0.85f);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            // Add background to status
            var statusBgGO = new GameObject("StatusBG");
            statusBgGO.transform.SetParent(statusGO.transform, false);
            statusBgGO.transform.SetAsFirstSibling();
            var statusBg = statusBgGO.AddComponent<Image>();
            statusBg.color = new Color(0, 0, 0, 0.5f);
            var bgRect = statusBgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Create button panel at bottom
            var panelGO = new GameObject("ButtonPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0.15f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelBg = panelGO.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.5f);

            var layout = panelGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Create buttons
            _placeCubeButton = CreateButton(panelGO.transform, "Place Cube", new Color(0.2f, 0.6f, 1f));
            _loadCubesButton = CreateButton(panelGO.transform, "Load Cubes", new Color(0.2f, 0.8f, 0.4f));
            _clearCubesButton = CreateButton(panelGO.transform, "Clear All", new Color(0.9f, 0.3f, 0.3f));

            Debug.Log("[PersistentCubeDemo] UI created successfully!");
        }

        private Button CreateButton(Transform parent, string text, Color color)
        {
            var btnGO = new GameObject(text.Replace(" ", "") + "Button");
            btnGO.transform.SetParent(parent, false);

            var image = btnGO.AddComponent<Image>();
            image.color = color;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = image;

            // Find a font
            Font font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 20);
            if (font == null) font = Font.CreateDynamicFontFromOSFont(Font.GetOSInstalledFontNames()[0], 20);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            var btnText = textGO.AddComponent<Text>();
            btnText.font = font;
            btnText.text = text;
            btnText.fontSize = 20;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;

            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return btn;
        }

        private void OnServicesReady()
        {
            UpdateStatus("Ready! Tap 'Place Cube' then tap on a surface.");
        }

        private void Update()
        {
            if (_isPlacementMode && Input.GetMouseButtonDown(0))
            {
                // Don't place cube if clicking on UI
                if (UnityEngine.EventSystems.EventSystem.current != null && 
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
                TryPlaceCube(Input.mousePosition);
            }

            // Touch support
            if (_isPlacementMode && Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    // Don't place cube if touching UI
                    if (UnityEngine.EventSystems.EventSystem.current != null && 
                        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        return;
                    }
                    TryPlaceCube(touch.position);
                }
            }
        }

        private void OnPlaceCubeClicked()
        {
            Debug.Log("[PersistentCubeDemo] Place Cube button clicked!");
            _isPlacementMode = true;
            UpdateStatus("Tap on a surface to place a cube...");
        }

        private void TryPlaceCube(Vector2 screenPosition)
        {
            _isPlacementMode = false;

            // Find any active camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = FindObjectOfType<Camera>();
            }

            // Check if SpatialAnchorManager exists
            if (SpatialAnchorManager.Instance == null)
            {
                // Fallback: Place cube at a position in front of camera
                if (cam != null)
                {
                    PlaceCubeAtPosition(cam.transform.position + cam.transform.forward * 2f);
                }
                else
                {
                    UpdateStatus("No camera found!");
                }
                return;
            }

            if (SpatialAnchorManager.Instance.TryGetHitPosition(screenPosition, out Vector3 position, out Quaternion rotation))
            {
                PlaceCubeAtPosition(position);
            }
            else
            {
                // Fallback: place in front of camera anyway
                if (cam != null)
                {
                    PlaceCubeAtPosition(cam.transform.position + cam.transform.forward * 2f);
                }
                else
                {
                    UpdateStatus("Couldn't find a surface. Try again.");
                }
            }
        }

        private async void PlaceCubeAtPosition(Vector3 position)
        {
            // Create the cube
            GameObject cube;
            if (_cubePrefab != null)
            {
                cube = Instantiate(_cubePrefab, position, Quaternion.identity);
            }
            else
            {
                cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = position;
                cube.transform.localScale = Vector3.one * 0.2f;
                
                var renderer = cube.GetComponent<Renderer>();
                renderer.material.color = Color.cyan;
            }

            UpdateStatus("Cube placed! Saving to cloud...");

            // Save to cloud
            if (AnchorPersistenceService.Instance != null)
            {
                double lat = 0, lon = 0, alt = 0;
                SpatialAnchorManager.Instance?.GetCurrentGeospatialPose(out lat, out lon, out alt);
                
                var anchorId = await AnchorPersistenceService.Instance.SaveAnchorAsync(
                    lat, lon, alt,
                    cube.transform.rotation,
                    "demo_user"
                );

                if (!string.IsNullOrEmpty(anchorId))
                {
                    UpdateStatus($"Cube saved! ID: {anchorId.Substring(0, 8)}...");
                }
                else
                {
                    UpdateStatus("Cube placed (local only - cloud save failed)");
                }
            }
            else
            {
                UpdateStatus("Cube placed! (No cloud service)");
            }
        }

        private async void OnLoadCubesClicked()
        {
            UpdateStatus("Loading cubes from cloud...");

            if (AnchorPersistenceService.Instance == null)
            {
                UpdateStatus("No cloud service available");
                return;
            }

            double lat = 0, lon = 0;
            SpatialAnchorManager.Instance?.GetCurrentGeospatialPose(out lat, out lon, out double _);
            
            var anchors = await AnchorPersistenceService.Instance.LoadAnchorsNearbyAsync(lat, lon, 100);
            
            if (anchors.Count == 0)
            {
                UpdateStatus("No cubes found nearby");
                return;
            }

            foreach (var anchor in anchors)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = new Vector3((float)anchor.Longitude, 0, (float)anchor.Latitude); // Simplified
                cube.transform.localScale = Vector3.one * 0.2f;
                cube.transform.rotation = anchor.Rotation;
                cube.GetComponent<Renderer>().material.color = Color.green;
            }

            UpdateStatus($"Loaded {anchors.Count} cube(s)!");
        }

        private void OnClearCubesClicked()
        {
            var cubes = GameObject.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
            foreach (var cube in cubes)
            {
                if (cube.sharedMesh != null && cube.sharedMesh.name == "Cube")
                {
                    Destroy(cube.gameObject);
                }
            }
            UpdateStatus("Cleared all cubes");
        }

        private void UpdateStatus(string message)
        {
            Debug.Log($"[PersistentCubeDemo] {message}");
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }
    }
}
