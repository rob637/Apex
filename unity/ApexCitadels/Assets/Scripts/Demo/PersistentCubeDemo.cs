using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.AR;
using ApexCitadels.Backend;

namespace ApexCitadels.Demo
{
    /// <summary>
    /// THE PERSISTENT CUBE TEST
    /// 
    /// This demo scene proves the core technology:
    /// 1. User A places a cube on their kitchen table
    /// 2. User A closes the app and walks away
    /// 3. User B opens the app, walks to User A's kitchen
    /// 4. User B sees the SAME CUBE in the SAME LOCATION
    /// 
    /// If this test passes, we can build anything on top of it.
    /// </summary>
    public class PersistentCubeDemo : MonoBehaviour
    {
        #region Inspector Fields
        [Header("UI Elements")]
        [SerializeField] private Button _placeCubeButton;
        [SerializeField] private Button _loadCubesButton;
        [SerializeField] private Button _clearCubesButton;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _trackingStatusText;
        [SerializeField] private TextMeshProUGUI _anchorCountText;

        [Header("Prefabs")]
        [SerializeField] private GameObject _cubePrefab;
        [SerializeField] private GameObject _placementIndicatorPrefab;

        [Header("Settings")]
        [SerializeField] private float _loadRadiusMeters = 100f;
        #endregion

        #region Private Fields
        private SpatialAnchorManager _anchorManager;
        private AnchorPersistenceService _persistenceService;
        private GameObject _placementIndicator;
        private bool _isPlacementMode = false;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // Get references
            _anchorManager = SpatialAnchorManager.Instance;
            _persistenceService = AnchorPersistenceService.Instance;

            // Setup UI
            _placeCubeButton.onClick.AddListener(OnPlaceCubeClicked);
            _loadCubesButton.onClick.AddListener(OnLoadCubesClicked);
            _clearCubesButton.onClick.AddListener(OnClearCubesClicked);

            // Subscribe to events
            _anchorManager.OnGeospatialStateChanged += OnGeospatialStateChanged;
            _anchorManager.OnAnchorCreated += OnAnchorCreated;
            _anchorManager.OnAnchorResolved += OnAnchorResolved;
            _anchorManager.OnAnchorFailed += OnAnchorFailed;

            // Create placement indicator
            if (_placementIndicatorPrefab != null)
            {
                _placementIndicator = Instantiate(_placementIndicatorPrefab);
                _placementIndicator.SetActive(false);
            }

            UpdateStatus("Initializing AR...");
        }

        private void Update()
        {
            UpdateTrackingStatus();
            UpdateAnchorCount();
            UpdatePlacementIndicator();
            HandleTouchInput();
        }

        private void OnDestroy()
        {
            if (_anchorManager != null)
            {
                _anchorManager.OnGeospatialStateChanged -= OnGeospatialStateChanged;
                _anchorManager.OnAnchorCreated -= OnAnchorCreated;
                _anchorManager.OnAnchorResolved -= OnAnchorResolved;
                _anchorManager.OnAnchorFailed -= OnAnchorFailed;
            }
        }
        #endregion

        #region UI Handlers
        private void OnPlaceCubeClicked()
        {
            if (!_anchorManager.IsTrackingReady)
            {
                UpdateStatus("❌ Tracking not ready. Move your phone slowly to scan the area.");
                return;
            }

            _isPlacementMode = !_isPlacementMode;
            _placeCubeButton.GetComponentInChildren<TextMeshProUGUI>().text = 
                _isPlacementMode ? "Cancel" : "Place Cube";

            if (_isPlacementMode)
            {
                UpdateStatus("👆 Tap on a surface to place a cube");
                _placementIndicator?.SetActive(true);
            }
            else
            {
                UpdateStatus("Ready");
                _placementIndicator?.SetActive(false);
            }
        }

        private async void OnLoadCubesClicked()
        {
            if (!_anchorManager.IsTrackingReady)
            {
                UpdateStatus("❌ Tracking not ready");
                return;
            }

            UpdateStatus("📡 Loading nearby cubes...");
            _loadCubesButton.interactable = false;

            try
            {
                var pose = _anchorManager.GetCurrentGeospatialPose();
                var anchors = await _persistenceService.GetAnchorsInRadius(
                    pose.Latitude,
                    pose.Longitude,
                    _loadRadiusMeters
                );

                if (anchors.Count == 0)
                {
                    UpdateStatus("No cubes found nearby. Be the first to place one!");
                }
                else
                {
                    int resolved = await _anchorManager.ResolveAnchorsInArea(
                        anchors,
                        (anchorData) => CreateCubeVisual()
                    );
                    UpdateStatus($"✅ Loaded {resolved} cubes from the cloud!");
                }
            }
            catch (System.Exception ex)
            {
                UpdateStatus($"❌ Error loading cubes: {ex.Message}");
            }
            finally
            {
                _loadCubesButton.interactable = true;
            }
        }

        private void OnClearCubesClicked()
        {
            // Clear all local anchor visuals (doesn't delete from cloud)
            var cubes = GameObject.FindGameObjectsWithTag("PersistentCube");
            foreach (var cube in cubes)
            {
                Destroy(cube);
            }
            UpdateStatus("🗑️ Cleared local cubes (still saved in cloud)");
        }
        #endregion

        #region Touch Handling
        private void HandleTouchInput()
        {
            if (!_isPlacementMode) return;
            if (Input.touchCount == 0) return;

            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began) return;

            // Check if touch is over UI
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;

            PlaceCubeAtTouch(touch.position);
        }

        private async void PlaceCubeAtTouch(Vector2 screenPosition)
        {
            UpdateStatus("📍 Placing cube...");
            _isPlacementMode = false;
            _placeCubeButton.GetComponentInChildren<TextMeshProUGUI>().text = "Place Cube";
            _placementIndicator?.SetActive(false);

            try
            {
                // Create cube visual
                var cube = CreateCubeVisual();

                // Create anchor at raycast position
                var anchorData = await _anchorManager.CreateAnchorFromRaycast(screenPosition, cube);

                if (anchorData != null)
                {
                    // Add owner ID (in production, get from auth)
                    anchorData.OwnerId = SystemInfo.deviceUniqueIdentifier;
                    anchorData.AttachedObjectType = "DemoCube";

                    // Save to cloud
                    bool saved = await _persistenceService.SaveAnchor(anchorData);
                    
                    if (saved)
                    {
                        UpdateStatus($"✅ Cube placed and saved to cloud!\n" +
                                   $"Lat: {anchorData.Latitude:F6}\n" +
                                   $"Lng: {anchorData.Longitude:F6}");
                    }
                    else
                    {
                        UpdateStatus("⚠️ Cube placed locally but failed to save to cloud");
                    }
                }
                else
                {
                    Destroy(cube);
                    UpdateStatus("❌ Failed to create anchor. Try pointing at a flat surface.");
                }
            }
            catch (System.Exception ex)
            {
                UpdateStatus($"❌ Error: {ex.Message}");
            }
        }
        #endregion

        #region Visual Updates
        private void UpdatePlacementIndicator()
        {
            if (!_isPlacementMode || _placementIndicator == null) return;

            // Cast ray from screen center
            var screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            var hits = new System.Collections.Generic.List<UnityEngine.XR.ARFoundation.ARRaycastHit>();
            
            var raycastManager = FindObjectOfType<UnityEngine.XR.ARFoundation.ARRaycastManager>();
            if (raycastManager != null && 
                raycastManager.Raycast(screenCenter, hits, UnityEngine.XR.ARSubsystems.TrackableTypes.PlaneWithinPolygon))
            {
                _placementIndicator.SetActive(true);
                _placementIndicator.transform.position = hits[0].pose.position;
                _placementIndicator.transform.rotation = hits[0].pose.rotation;
            }
            else
            {
                _placementIndicator.SetActive(false);
            }
        }

        private void UpdateTrackingStatus()
        {
            if (_trackingStatusText == null) return;

            var state = _anchorManager?.CurrentGeospatialState ?? GeospatialState.Initializing;
            var emoji = state switch
            {
                GeospatialState.Ready => "🟢",
                GeospatialState.Localizing => "🟡",
                GeospatialState.LowAccuracy => "🟠",
                GeospatialState.NotTracking => "🔴",
                GeospatialState.Error => "❌",
                _ => "⚪"
            };

            _trackingStatusText.text = $"{emoji} {state}";

            if (_anchorManager?.IsTrackingReady == true)
            {
                var pose = _anchorManager.GetCurrentGeospatialPose();
                _trackingStatusText.text += $"\n📍 {pose.Latitude:F4}, {pose.Longitude:F4}";
                _trackingStatusText.text += $"\n📏 ±{pose.HorizontalAccuracy:F1}m";
            }
        }

        private void UpdateAnchorCount()
        {
            if (_anchorCountText == null) return;
            _anchorCountText.text = $"Active Anchors: {_anchorManager?.ActiveAnchorCount ?? 0}";
        }

        private void UpdateStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
            Debug.Log($"[PersistentCubeDemo] {message}");
        }
        #endregion

        #region Event Handlers
        private void OnGeospatialStateChanged(GeospatialState state)
        {
            Debug.Log($"[PersistentCubeDemo] Geospatial state changed: {state}");
            
            if (state == GeospatialState.Ready)
            {
                UpdateStatus("✅ Ready! You can now place and load cubes.");
            }
        }

        private void OnAnchorCreated(SpatialAnchorData anchorData)
        {
            Debug.Log($"[PersistentCubeDemo] Anchor created: {anchorData.Id}");
        }

        private void OnAnchorResolved(SpatialAnchorData anchorData)
        {
            Debug.Log($"[PersistentCubeDemo] Anchor resolved: {anchorData.Id}");
        }

        private void OnAnchorFailed(string error)
        {
            UpdateStatus($"❌ Anchor failed: {error}");
        }
        #endregion

        #region Helpers
        private GameObject CreateCubeVisual()
        {
            GameObject cube;
            
            if (_cubePrefab != null)
            {
                cube = Instantiate(_cubePrefab);
            }
            else
            {
                // Create default cube if no prefab
                cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.localScale = Vector3.one * 0.1f; // 10cm cube
                
                var renderer = cube.GetComponent<Renderer>();
                renderer.material.color = GetRandomBrightColor();
            }

            cube.tag = "PersistentCube";
            return cube;
        }

        private Color GetRandomBrightColor()
        {
            var colors = new Color[]
            {
                new Color(1f, 0.3f, 0.3f),   // Red
                new Color(0.3f, 1f, 0.3f),   // Green
                new Color(0.3f, 0.3f, 1f),   // Blue
                new Color(1f, 1f, 0.3f),     // Yellow
                new Color(1f, 0.3f, 1f),     // Magenta
                new Color(0.3f, 1f, 1f),     // Cyan
                new Color(1f, 0.6f, 0.2f),   // Orange
            };
            return colors[Random.Range(0, colors.Length)];
        }
        #endregion
    }
}
