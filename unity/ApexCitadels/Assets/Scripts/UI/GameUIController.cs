using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Core;
using ApexCitadels.Data;
using ApexCitadels.Territory;
using ApexCitadels.Building;
using ApexCitadels.Player;
using ApexCitadels.Combat;

namespace ApexCitadels.UI
{
    /// <summary>
    /// Main game UI controller - connects all systems to UI elements
    /// </summary>
    public class GameUIController : MonoBehaviour
    {
        [Header("Resource Display")]
        [SerializeField] private TextMeshProUGUI stoneText;
        [SerializeField] private TextMeshProUGUI woodText;
        [SerializeField] private TextMeshProUGUI metalText;
        [SerializeField] private TextMeshProUGUI crystalText;

        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerLevelText;
        [SerializeField] private Slider experienceBar;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private float statusDisplayTime = 3f;

        [Header("Action Buttons")]
        [SerializeField] private Button claimTerritoryButton;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button attackButton;

        [Header("Building Panel")]
        [SerializeField] private GameObject buildingPanel;
        [SerializeField] private Transform blockButtonContainer;
        [SerializeField] private GameObject blockButtonPrefab;

        [Header("Territory Info")]
        [SerializeField] private GameObject territoryInfoPanel;
        [SerializeField] private TextMeshProUGUI territoryOwnerText;
        [SerializeField] private TextMeshProUGUI territoryHealthText;
        [SerializeField] private TextMeshProUGUI territoryLevelText;

        private float _statusTimer;

        private void Start()
        {
            SetupEventListeners();
            SetupButtons();
            UpdateResourceDisplay();
            UpdatePlayerInfo();
            
            ShowStatus("Welcome to Apex Citadels!");
        }

        private void Update()
        {
            // Clear status after timer
            if (_statusTimer > 0)
            {
                _statusTimer -= Time.deltaTime;
                if (_statusTimer <= 0 && statusText != null)
                {
                    statusText.text = "";
                }
            }
        }

        private void SetupEventListeners()
        {
            // Player events
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.OnResourceChanged += OnResourceChanged;
                PlayerManager.Instance.OnLevelUp += OnLevelUp;
                PlayerManager.Instance.OnExperienceGained += OnExperienceGained;
            }

            // Territory events
            if (TerritoryManager.Instance != null)
            {
                TerritoryManager.Instance.OnTerritoryClaimed += OnTerritoryClaimed;
                TerritoryManager.Instance.OnTerritoryLost += OnTerritoryLost;
                TerritoryManager.Instance.OnTerritoryAttacked += OnTerritoryAttacked;
            }

            // Building events
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.OnBlockPlaced += OnBlockPlaced;
            }
        }

        private void SetupButtons()
        {
            if (claimTerritoryButton != null)
                claimTerritoryButton.onClick.AddListener(OnClaimTerritoryClicked);

            if (buildButton != null)
                buildButton.onClick.AddListener(OnBuildClicked);

            if (attackButton != null)
                attackButton.onClick.AddListener(OnAttackClicked);

            // Hide panels initially
            if (buildingPanel != null)
                buildingPanel.SetActive(false);

            if (territoryInfoPanel != null)
                territoryInfoPanel.SetActive(false);
        }

        #region UI Updates

        public void UpdateResourceDisplay()
        {
            if (PlayerManager.Instance?.CurrentPlayer == null) return;

            var player = PlayerManager.Instance.CurrentPlayer;

            if (stoneText != null) stoneText.text = $"Stone: {player.Stone}";
            if (woodText != null) woodText.text = $"Wood: {player.Wood}";
            if (metalText != null) metalText.text = $"Metal: {player.Metal}";
            if (crystalText != null) crystalText.text = $"Crystal: {player.Crystal}";
        }

        public void UpdatePlayerInfo()
        {
            if (PlayerManager.Instance?.CurrentPlayer == null) return;

            var player = PlayerManager.Instance.CurrentPlayer;

            if (playerNameText != null) playerNameText.text = player.DisplayName;
            if (playerLevelText != null) playerLevelText.text = $"Level {player.Level}";
            
            if (experienceBar != null)
            {
                float progress = (float)player.Experience / player.GetExperienceForNextLevel();
                experienceBar.value = progress;
            }
        }

        public void ShowStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
                _statusTimer = statusDisplayTime;
            }
            ApexLogger.Log(message, ApexLogger.LogCategory.UI);
        }

        public void ShowTerritoryInfo(Territory.Territory territory)
        {
            if (territoryInfoPanel == null) return;

            territoryInfoPanel.SetActive(true);

            if (territoryOwnerText != null)
                territoryOwnerText.text = $"Owner: {territory.OwnerName}";

            if (territoryHealthText != null)
                territoryHealthText.text = $"Health: {territory.Health}/{territory.MaxHealth}";

            if (territoryLevelText != null)
                territoryLevelText.text = $"Level: {territory.Level}";
        }

        public void HideTerritoryInfo()
        {
            if (territoryInfoPanel != null)
                territoryInfoPanel.SetActive(false);
        }

        #endregion

        #region Button Handlers

        private async void OnClaimTerritoryClicked()
        {
            ShowStatus("Claiming territory...");

            // Get current GPS position from AR system
            double latitude = 37.7749;
            double longitude = -122.4194;

            if (AR.SpatialAnchorManager.Instance != null)
            {
                AR.SpatialAnchorManager.Instance.GetCurrentGeospatialPose(
                    out latitude, out longitude, out double _);
            }

            if (TerritoryManager.Instance != null)
            {
                var result = await TerritoryManager.Instance.TryClaimTerritory(latitude, longitude);
                ShowStatus(result.Message);

                if (result.Success)
                {
                    PlayerManager.Instance?.AwardExperience(PlayerManager.ExperienceRewards.ClaimTerritory);
                    PlayerManager.Instance?.IncrementStat("TerritoriesOwned");
                }
            }
        }

        private void OnBuildClicked()
        {
            if (buildingPanel != null)
            {
                buildingPanel.SetActive(!buildingPanel.activeSelf);
            }

            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.TogglePlacementMode();
            }
        }

        private void OnAttackClicked()
        {
            // Enter attack mode - highlight enemy territories
            if (CombatManager.Instance != null && TerritoryManager.Instance != null)
            {
                // Get current GPS
                double lat = 37.7749, lon = -122.4194;
                if (AR.SpatialAnchorManager.Instance != null)
                {
                    AR.SpatialAnchorManager.Instance.GetCurrentGeospatialPose(out lat, out lon, out double _);
                }
                
                // Find nearby enemy territory
                var territory = TerritoryManager.Instance.GetTerritoryAtLocation(lat, lon);
                var playerId = PlayerManager.Instance?.GetCurrentPlayerId();
                
                if (territory == null)
                {
                    ShowStatus("No territory here to attack!");
                }
                else if (territory.OwnerId == playerId)
                {
                    ShowStatus("You own this territory!");
                }
                else if (!CombatManager.Instance.IsRaidWindowOpen())
                {
                    ShowStatus("Raids only allowed 6PM-10PM!");
                }
                else
                {
                    ShowStatus($"Attacking {territory.Name}...");
                    _ = CombatManager.Instance.StartAttack(territory);
                }
            }
            else
            {
                ShowStatus("Combat system not ready!");
            }
        }

        public void OnBlockTypeSelected(BlockType type)
        {
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.SelectBlock(type);
                ShowStatus($"Selected: {type}");
            }
        }

        public async void OnPlaceBlockClicked()
        {
            if (BuildingManager.Instance != null)
            {
                var result = await BuildingManager.Instance.PlaceBlock();
                ShowStatus(result.Message);
            }
        }

        #endregion

        #region Event Handlers

        private void OnResourceChanged(ResourceType type, int amount)
        {
            UpdateResourceDisplay();
            
            string sign = amount > 0 ? "+" : "";
            ShowStatus($"{type}: {sign}{amount}");
        }

        private void OnLevelUp(int newLevel)
        {
            UpdatePlayerInfo();
            ShowStatus($"🎉 Level Up! Now level {newLevel}!");
        }

        private void OnExperienceGained(int amount)
        {
            UpdatePlayerInfo();
        }

        private void OnTerritoryClaimed(Territory.Territory territory)
        {
            ShowStatus($"Territory claimed! Radius: {territory.RadiusMeters}m");
            UpdatePlayerInfo();
        }

        private void OnTerritoryLost(Territory.Territory territory)
        {
            ShowStatus($"Territory lost to {territory.OwnerName}!");
        }

        private void OnTerritoryAttacked(Territory.Territory territory)
        {
            ShowStatus($"Your territory is under attack!");
        }

        private void OnBlockPlaced(BuildingBlock block)
        {
            PlayerManager.Instance?.AwardExperience(PlayerManager.ExperienceRewards.PlaceBlock);
            PlayerManager.Instance?.IncrementStat("BlocksPlaced");
            UpdateResourceDisplay();
        }

        #endregion

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.OnResourceChanged -= OnResourceChanged;
                PlayerManager.Instance.OnLevelUp -= OnLevelUp;
                PlayerManager.Instance.OnExperienceGained -= OnExperienceGained;
            }

            if (TerritoryManager.Instance != null)
            {
                TerritoryManager.Instance.OnTerritoryClaimed -= OnTerritoryClaimed;
                TerritoryManager.Instance.OnTerritoryLost -= OnTerritoryLost;
                TerritoryManager.Instance.OnTerritoryAttacked -= OnTerritoryAttacked;
            }

            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.OnBlockPlaced -= OnBlockPlaced;
            }
        }
    }
}
