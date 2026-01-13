using UnityEngine;
using UnityEngine.UI;
using ApexCitadels.AR;
using ApexCitadels.Backend;

namespace ApexCitadels.Demo
{
    /// <summary>
    /// Demo script for testing persistent AR cubes.
    /// Tap to place a cube, and it will be saved to the cloud.
    /// </summary>
    public class PersistentCubeDemo : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button _placeCubeButton;
        [SerializeField] private Button _loadCubesButton;
        [SerializeField] private Button _clearCubesButton;
        [SerializeField] private Text _statusText;

        [Header("Prefab")]
        [SerializeField] private GameObject _cubePrefab;

        private bool _isPlacementMode = false;

        private void Start()
        {
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
            }
            else
            {
                Invoke(nameof(OnServicesReady), 1f);
            }
        }

        private void OnServicesReady()
        {
            UpdateStatus("Ready! Tap 'Place Cube' then tap on a surface.");
        }

        private void Update()
        {
            if (_isPlacementMode && Input.GetMouseButtonDown(0))
            {
                TryPlaceCube(Input.mousePosition);
            }

            // Touch support
            if (_isPlacementMode && Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    TryPlaceCube(touch.position);
                }
            }
        }

        private void OnPlaceCubeClicked()
        {
            _isPlacementMode = true;
            UpdateStatus("Tap on a surface to place a cube...");
        }

        private async void TryPlaceCube(Vector2 screenPosition)
        {
            _isPlacementMode = false;

            // Check if SpatialAnchorManager exists
            if (SpatialAnchorManager.Instance == null)
            {
                // Fallback: Place cube at a position in front of camera
                PlaceCubeAtPosition(Camera.main.transform.position + Camera.main.transform.forward * 2f);
                return;
            }

            if (SpatialAnchorManager.Instance.TryGetHitPosition(screenPosition, out Vector3 position, out Quaternion rotation))
            {
                PlaceCubeAtPosition(position);
            }
            else
            {
                UpdateStatus("Couldn't find a surface. Try again.");
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
                SpatialAnchorManager.Instance?.GetCurrentGeospatialPose(out double lat, out double lon, out double alt);
                
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

            SpatialAnchorManager.Instance?.GetCurrentGeospatialPose(out double lat, out double lon, out double _);
            
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
            var cubes = GameObject.FindObjectsOfType<MeshFilter>();
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
