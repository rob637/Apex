using UnityEngine;
using TMPro;
using ApexCitadels.AR;
using ApexCitadels.Player;

namespace ApexCitadels.Debug
{
    /// <summary>
    /// Shows debug information overlay on screen
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject overlayPanel;
        [SerializeField] private TextMeshProUGUI fpsText;
        [SerializeField] private TextMeshProUGUI memoryText;
        [SerializeField] private TextMeshProUGUI arStatusText;
        [SerializeField] private TextMeshProUGUI locationText;
        [SerializeField] private TextMeshProUGUI playerText;

        [Header("Settings")]
        [SerializeField] private bool showOnStart = false;
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;

        // FPS calculation
        private float _deltaTime = 0f;
        private float _fps = 0f;
        private float _lastUpdateTime = 0f;

        public bool IsVisible => overlayPanel != null && overlayPanel.activeSelf;

        private void Start()
        {
            if (overlayPanel != null)
                overlayPanel.SetActive(showOnStart);
        }

        private void Update()
        {
            // Toggle overlay
            if (Input.GetKeyDown(toggleKey))
            {
                Toggle();
            }

            // Calculate FPS
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
            _fps = 1f / _deltaTime;

            // Update display at interval
            if (Time.time - _lastUpdateTime >= updateInterval)
            {
                _lastUpdateTime = Time.time;
                UpdateDisplay();
            }
        }

        public void Show()
        {
            if (overlayPanel != null)
                overlayPanel.SetActive(true);
        }

        public void Hide()
        {
            if (overlayPanel != null)
                overlayPanel.SetActive(false);
        }

        public void Toggle()
        {
            if (overlayPanel != null)
                overlayPanel.SetActive(!overlayPanel.activeSelf);
        }

        private void UpdateDisplay()
        {
            if (!IsVisible) return;

            // FPS
            if (fpsText != null)
            {
                string fpsColor = _fps >= 60 ? "green" : (_fps >= 30 ? "yellow" : "red");
                fpsText.text = $"FPS: <color={fpsColor}>{_fps:F0}</color>";
            }

            // Memory
            if (memoryText != null)
            {
                long totalMem = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                float memMB = totalMem / (1024f * 1024f);
                memoryText.text = $"Memory: {memMB:F1} MB";
            }

            // AR Status
            if (arStatusText != null)
            {
                UpdateARStatus();
            }

            // Location
            if (locationText != null)
            {
                UpdateLocationStatus();
            }

            // Player
            if (playerText != null)
            {
                UpdatePlayerStatus();
            }
        }

        private void UpdateARStatus()
        {
            if (SpatialAnchorManager.Instance != null)
            {
                var sam = SpatialAnchorManager.Instance;
                string trackingColor = sam.IsTracking ? "green" : "red";
                
                arStatusText.text = 
                    $"AR: {(sam.IsDesktopMode ? "Desktop" : "Device")}\n" +
                    $"Tracking: <color={trackingColor}>{sam.CurrentTrackingState}</color>\n" +
                    $"Anchors: {sam.ActiveAnchorCount}";
            }
            else
            {
                arStatusText.text = "AR: Not initialized";
            }
        }

        private void UpdateLocationStatus()
        {
            if (!Input.location.isEnabledByUser)
            {
                locationText.text = "Location: Disabled";
                return;
            }

            if (Input.location.status == LocationServiceStatus.Running)
            {
                var loc = Input.location.lastData;
                locationText.text = 
                    $"Lat: {loc.latitude:F6}\n" +
                    $"Lon: {loc.longitude:F6}\n" +
                    $"Alt: {loc.altitude:F1}m\n" +
                    $"Accuracy: {loc.horizontalAccuracy:F1}m";
            }
            else
            {
                locationText.text = $"Location: {Input.location.status}";
            }
        }

        private void UpdatePlayerStatus()
        {
            if (PlayerManager.Instance?.CurrentPlayer != null)
            {
                var player = PlayerManager.Instance.CurrentPlayer;
                playerText.text = 
                    $"Player: {player.DisplayName}\n" +
                    $"Level: {player.Level}\n" +
                    $"XP: {player.Experience}";
            }
            else
            {
                playerText.text = "Player: Not logged in";
            }
        }
    }
}
