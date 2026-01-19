// ============================================================================
// APEX CITADELS - AR HUD (Mobile)
// Touch-friendly HUD for AR gameplay on mobile devices
// ============================================================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;
using ApexCitadels.Territory;
using ApexCitadels.Building;
using ApexCitadels.Data;

namespace ApexCitadels.AR.UI
{
    /// <summary>
    /// Mobile AR HUD with large touch targets and minimal UI
    /// optimized for outdoor play with one-handed operation.
    /// </summary>
    public class ARHUD : MonoBehaviour
    {
        public static ARHUD Instance { get; private set; }

        [Header("Main Panels")]
        [SerializeField] private GameObject mainHUD;
        [SerializeField] private GameObject buildModePanel;
        [SerializeField] private GameObject territoryPanel;
        [SerializeField] private GameObject combatPanel;

        [Header("Status Bar (Top)")]
        [SerializeField] private TextMeshProUGUI locationStatusText;
        [SerializeField] private Image gpsSignalIcon;
        [SerializeField] private TextMeshProUGUI territoryNameText;
        [SerializeField] private Slider territoryHealthBar;

        [Header("Resource Display")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI stoneText;
        [SerializeField] private TextMeshProUGUI woodText;

        [Header("Action Buttons (Large Touch Targets)")]
        [SerializeField] private Button claimTerritoryButton;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button mapButton;

        [Header("Build Mode")]
        [SerializeField] private Transform blockButtonContainer;
        [SerializeField] private GameObject blockButtonPrefab;
        [SerializeField] private Button confirmPlacementButton;
        [SerializeField] private Button cancelPlacementButton;
        [SerializeField] private Button rotateLeftButton;
        [SerializeField] private Button rotateRightButton;

        [Header("Quick Actions")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button helpButton;

        [Header("Notifications")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private float notificationDuration = 3f;

        [Header("Geospatial Info")]
        [SerializeField] private TextMeshProUGUI gpsCoordinatesText;
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private GameObject localizingOverlay;
        [SerializeField] private TextMeshProUGUI localizingText;

        [Header("Touch Feedback")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private float buttonScalePunch = 1.1f;

        // State
        private ARHUDState currentState = ARHUDState.Normal;
        private bool isBuildMode = false;
        private BlockType selectedBlock = BlockType.Stone;
        private Coroutine notificationCoroutine;

        public ARHUDState CurrentState => currentState;
        public bool IsBuildMode => isBuildMode;

        // Events
        public event Action OnClaimTerritoryPressed;
        public event Action OnBuildModeEntered;
        public event Action OnBuildModeExited;
        public event Action<BlockType> OnBlockSelected;
        public event Action OnPlacementConfirmed;
        public event Action OnPlacementCanceled;

        #region Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SetupButtons();
            SetupBlockButtons();
            SubscribeToEvents();
            SetState(ARHUDState.Normal);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            UpdateGPSDisplay();
            UpdateTerritoryDisplay();
        }

        #endregion

        #region Setup

        private void SetupButtons()
        {
            // Main action buttons
            if (claimTerritoryButton != null)
                claimTerritoryButton.onClick.AddListener(OnClaimTerritoryClicked);

            if (buildButton != null)
                buildButton.onClick.AddListener(EnterBuildMode);

            if (attackButton != null)
                attackButton.onClick.AddListener(OnAttackClicked);

            if (inventoryButton != null)
                inventoryButton.onClick.AddListener(OnInventoryClicked);

            if (mapButton != null)
                mapButton.onClick.AddListener(OnMapClicked);

            // Build mode buttons
            if (confirmPlacementButton != null)
                confirmPlacementButton.onClick.AddListener(ConfirmPlacement);

            if (cancelPlacementButton != null)
                cancelPlacementButton.onClick.AddListener(CancelPlacement);

            if (rotateLeftButton != null)
                rotateLeftButton.onClick.AddListener(() => RotateBlock(-90));

            if (rotateRightButton != null)
                rotateRightButton.onClick.AddListener(() => RotateBlock(90));

            // Settings
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (helpButton != null)
                helpButton.onClick.AddListener(OnHelpClicked);
        }

        private void SetupBlockButtons()
        {
            if (blockButtonContainer == null || blockButtonPrefab == null) return;

            // Create buttons for each block type
            foreach (BlockType blockType in Enum.GetValues(typeof(BlockType)))
            {
                GameObject buttonObj = Instantiate(blockButtonPrefab, blockButtonContainer);
                Button button = buttonObj.GetComponent<Button>();
                
                // Set button text/icon
                var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = blockType.ToString();
                }

                // Add click handler
                BlockType capturedType = blockType;
                button.onClick.AddListener(() => SelectBlock(capturedType));
            }
        }

        private void SubscribeToEvents()
        {
            if (GeospatialManager.Instance != null)
            {
                GeospatialManager.Instance.OnGeospatialReady += OnGeospatialReady;
                GeospatialManager.Instance.OnGeospatialLost += OnGeospatialLost;
                GeospatialManager.Instance.OnError += ShowNotification;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GeospatialManager.Instance != null)
            {
                GeospatialManager.Instance.OnGeospatialReady -= OnGeospatialReady;
                GeospatialManager.Instance.OnGeospatialLost -= OnGeospatialLost;
                GeospatialManager.Instance.OnError -= ShowNotification;
            }
        }

        #endregion

        #region State Management

        public void SetState(ARHUDState state)
        {
            currentState = state;

            // Hide all panels first
            if (mainHUD != null) mainHUD.SetActive(false);
            if (buildModePanel != null) buildModePanel.SetActive(false);
            if (territoryPanel != null) territoryPanel.SetActive(false);
            if (combatPanel != null) combatPanel.SetActive(false);
            if (localizingOverlay != null) localizingOverlay.SetActive(false);

            switch (state)
            {
                case ARHUDState.Localizing:
                    if (localizingOverlay != null) localizingOverlay.SetActive(true);
                    if (localizingText != null) localizingText.text = "Finding your location...\nPoint camera at buildings or street features";
                    break;

                case ARHUDState.Normal:
                    if (mainHUD != null) mainHUD.SetActive(true);
                    isBuildMode = false;
                    break;

                case ARHUDState.BuildMode:
                    if (mainHUD != null) mainHUD.SetActive(true);
                    if (buildModePanel != null) buildModePanel.SetActive(true);
                    isBuildMode = true;
                    break;

                case ARHUDState.TerritoryView:
                    if (mainHUD != null) mainHUD.SetActive(true);
                    if (territoryPanel != null) territoryPanel.SetActive(true);
                    break;

                case ARHUDState.Combat:
                    if (combatPanel != null) combatPanel.SetActive(true);
                    break;
            }

            ApexLogger.Log($"[ARHUD] State changed to: {state}", ApexLogger.LogCategory.UI);
        }

        #endregion

        #region GPS Display

        private void UpdateGPSDisplay()
        {
            if (GeospatialManager.Instance == null) return;

            var pos = GeospatialManager.Instance.CurrentPosition;

            if (gpsCoordinatesText != null)
            {
                gpsCoordinatesText.text = $"{pos.Latitude:F5}, {pos.Longitude:F5}";
            }

            if (accuracyText != null)
            {
                accuracyText.text = $"±{pos.HorizontalAccuracy:F1}m";
            }

            // Update GPS signal strength indicator
            if (gpsSignalIcon != null)
            {
                if (pos.HorizontalAccuracy <= 5f)
                    gpsSignalIcon.color = Color.green; // Excellent
                else if (pos.HorizontalAccuracy <= 15f)
                    gpsSignalIcon.color = Color.yellow; // Good
                else
                    gpsSignalIcon.color = Color.red; // Poor
            }

            // Update location status
            if (locationStatusText != null)
            {
                if (GeospatialManager.Instance.IsReady)
                {
                    locationStatusText.text = "GPS Ready";
                    locationStatusText.color = Color.green;
                }
                else if (GeospatialManager.Instance.IsLocalizing)
                {
                    locationStatusText.text = "Localizing...";
                    locationStatusText.color = Color.yellow;
                }
                else
                {
                    locationStatusText.text = "GPS Unavailable";
                    locationStatusText.color = Color.red;
                }
            }
        }

        private void UpdateTerritoryDisplay()
        {
            // Check if player is in a territory
            if (TerritoryManager.Instance == null || GeospatialManager.Instance == null) return;

            var pos = GeospatialManager.Instance.CurrentPosition;
            // TODO: Get territory at current location
            
            // Update claim button visibility
            if (claimTerritoryButton != null)
            {
                bool canClaim = GeospatialManager.Instance.CanClaimTerritoryHere();
                claimTerritoryButton.gameObject.SetActive(canClaim);
            }
        }

        #endregion

        #region Button Handlers

        private void OnClaimTerritoryClicked()
        {
            PlayButtonFeedback(claimTerritoryButton);
            OnClaimTerritoryPressed?.Invoke();
            
            if (GeospatialManager.Instance != null)
            {
                GeospatialManager.Instance.ClaimTerritoryAtCurrentLocation();
            }
        }

        private void EnterBuildMode()
        {
            PlayButtonFeedback(buildButton);
            SetState(ARHUDState.BuildMode);
            OnBuildModeEntered?.Invoke();
            
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.EnterPlacementMode();
            }
        }

        private void ExitBuildMode()
        {
            SetState(ARHUDState.Normal);
            OnBuildModeExited?.Invoke();
            
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.ExitPlacementMode();
            }
        }

        private void SelectBlock(BlockType type)
        {
            selectedBlock = type;
            OnBlockSelected?.Invoke(type);
            
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.SelectBlock(type);
            }
            
            ShowNotification($"Selected: {type}");
        }

        private void ConfirmPlacement()
        {
            PlayButtonFeedback(confirmPlacementButton);
            OnPlacementConfirmed?.Invoke();
            
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.ConfirmPlacement();
            }
        }

        private void CancelPlacement()
        {
            PlayButtonFeedback(cancelPlacementButton);
            OnPlacementCanceled?.Invoke();
            ExitBuildMode();
        }

        private void RotateBlock(float degrees)
        {
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.RotatePreview(degrees);
            }
        }

        private void OnAttackClicked()
        {
            PlayButtonFeedback(attackButton);
            ShowNotification("Attack mode - Tap an enemy territory!");
        }

        private void OnInventoryClicked()
        {
            PlayButtonFeedback(inventoryButton);
            // TODO: Open inventory panel
            ShowNotification("Inventory coming soon!");
        }

        private void OnMapClicked()
        {
            PlayButtonFeedback(mapButton);
            // Switch to map view
            UnityEngine.SceneManagement.SceneManager.LoadScene("MapView");
        }

        private void OnSettingsClicked()
        {
            PlayButtonFeedback(settingsButton);
            // TODO: Open AR settings
        }

        private void OnHelpClicked()
        {
            PlayButtonFeedback(helpButton);
            ShowNotification("Tap to place blocks. Pinch to zoom. Drag to rotate.");
        }

        #endregion

        #region Geospatial Events

        private void OnGeospatialReady()
        {
            if (currentState == ARHUDState.Localizing)
            {
                SetState(ARHUDState.Normal);
            }
            ShowNotification("GPS locked! You can now claim territory.");
        }

        private void OnGeospatialLost()
        {
            ShowNotification("GPS signal lost. Move to a clearer area.");
        }

        #endregion

        #region Notifications

        public void ShowNotification(string message)
        {
            if (notificationPanel == null || notificationText == null) return;

            if (notificationCoroutine != null)
            {
                StopCoroutine(notificationCoroutine);
            }

            notificationText.text = message;
            notificationPanel.SetActive(true);
            notificationCoroutine = StartCoroutine(HideNotificationAfterDelay());
        }

        private IEnumerator HideNotificationAfterDelay()
        {
            yield return new WaitForSeconds(notificationDuration);
            notificationPanel.SetActive(false);
        }

        #endregion

        #region Touch Feedback

        private void PlayButtonFeedback(Button button)
        {
            if (button == null) return;

            // Scale punch
            StartCoroutine(ScalePunch(button.transform));

            // Sound
            if (buttonClickSound != null && Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySFX("ui_click");
            }
        }

        private IEnumerator ScalePunch(Transform target)
        {
            Vector3 original = target.localScale;
            Vector3 punched = original * buttonScalePunch;

            float duration = 0.1f;
            float elapsed = 0;

            // Scale up
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(original, punched, elapsed / duration);
                yield return null;
            }

            // Scale back
            elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(punched, original, elapsed / duration);
                yield return null;
            }

            target.localScale = original;
        }

        #endregion

        #region Resource Updates

        public void UpdateResources(int gold, int stone, int wood)
        {
            if (goldText != null) goldText.text = FormatNumber(gold);
            if (stoneText != null) stoneText.text = FormatNumber(stone);
            if (woodText != null) woodText.text = FormatNumber(wood);
        }

        private string FormatNumber(int value)
        {
            if (value >= 1000000) return $"{value / 1000000f:F1}M";
            if (value >= 1000) return $"{value / 1000f:F1}K";
            return value.ToString();
        }

        #endregion
    }

    /// <summary>
    /// AR HUD display states
    /// </summary>
    public enum ARHUDState
    {
        Localizing,     // Waiting for GPS lock
        Normal,         // Standard gameplay
        BuildMode,      // Placing blocks
        TerritoryView,  // Viewing territory details
        Combat          // In combat
    }
}
