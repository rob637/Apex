using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;
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
        [SerializeField] private TMP_Text _statusText;

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
            ApexLogger.LogVerbose("Auto-creating UI...", LogCategory.General);

            // Create Canvas
            var canvasGO = new GameObject("DemoCanvas_AutoCreated");
            _autoCanvas = canvasGO.AddComponent<Canvas>();
            _autoCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _autoCanvas.sortingOrder = 100; // Make sure it's on top
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Create Status Text at top using TextMeshPro
            var statusGO = new GameObject("StatusText");
            statusGO.transform.SetParent(canvasGO.transform, false);
            var tmpText = statusGO.AddComponent<TextMeshProUGUI>();
            _statusText = tmpText;
            tmpText.fontSize = 36;
            tmpText.alignment = TextAlignmentOptions.Top;
            tmpText.color = Color.white;
            tmpText.text = "Initializing...";
            
            var statusRect = statusGO.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0.85f);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.offsetMin = new Vector2(20, 0);
            statusRect.offsetMax = new Vector2(-20, -10);

            // Add background to status
            var statusBgGO = new GameObject("StatusBG");
            statusBgGO.transform.SetParent(canvasGO.transform, false);
            statusBgGO.transform.SetSiblingIndex(0); // Put behind text
            var statusBg = statusBgGO.AddComponent<Image>();
            statusBg.color = new Color(0, 0, 0, 0.7f);
            var bgRect = statusBgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.85f);
            bgRect.anchorMax = new Vector2(1, 1);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Create button panel at bottom
            var panelGO = new GameObject("ButtonPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0.12f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelBg = panelGO.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.7f);

            var layout = panelGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            // Create buttons with TextMeshPro
            _placeCubeButton = CreateButton(panelGO.transform, "PLACE CUBE", new Color(0.2f, 0.6f, 1f));
            _loadCubesButton = CreateButton(panelGO.transform, "LOAD CUBES", new Color(0.2f, 0.8f, 0.4f));
            _clearCubesButton = CreateButton(panelGO.transform, "CLEAR ALL", new Color(0.9f, 0.3f, 0.3f));

            // Connect button listeners immediately
            _placeCubeButton.onClick.AddListener(() => {
                ApexLogger.LogVerbose("PLACE CUBE clicked!", LogCategory.General);
                OnPlaceCubeClicked();
            });
            _loadCubesButton.onClick.AddListener(() => {
                ApexLogger.LogVerbose("LOAD CUBES clicked!", LogCategory.General);
                OnLoadCubesClicked();
            });
            _clearCubesButton.onClick.AddListener(() => {
                ApexLogger.LogVerbose("CLEAR ALL clicked!", LogCategory.General);
                OnClearCubesClicked();
            });

            ApexLogger.LogVerbose("UI created and listeners connected!", LogCategory.General);
        }

        private Button CreateButton(Transform parent, string text, Color color)
        {
            var btnGO = new GameObject(text.Replace(" ", "") + "Button");
            btnGO.transform.SetParent(parent, false);

            var image = btnGO.AddComponent<Image>();
            image.color = color;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = image;
            
            // Add color transition
            var colors = btn.colors;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            btn.colors = colors;

            // Use TextMeshPro for button text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            var btnText = textGO.AddComponent<TextMeshProUGUI>();
            btnText.text = text;
            btnText.fontSize = 24;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;
            btnText.fontStyle = FontStyles.Bold;

            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
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
            ApexLogger.LogVerbose("Place Cube button clicked!", LogCategory.General);
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
                cam = FindFirstObjectByType<Camera>();
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
            ApexLogger.Log(message, LogCategory.General);
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }
    }
}
