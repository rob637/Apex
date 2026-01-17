using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Territory;
using ApexCitadels.Building;
using ApexCitadels.Resources;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Territory detail panel showing comprehensive territory information
    /// and management options available on PC.
    /// </summary>
    public class TerritoryDetailPanel : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TextMeshProUGUI territoryNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button editButton;

        [Header("Stats Display")]
        [SerializeField] private Slider defenseSlider;
        [SerializeField] private TextMeshProUGUI defenseText;
        [SerializeField] private Slider incomeSlider;
        [SerializeField] private TextMeshProUGUI incomeText;
        [SerializeField] private TextMeshProUGUI blockCountText;
        [SerializeField] private TextMeshProUGUI maxBlocksText;

        [Header("Health Bar")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Color healthFullColor = Color.green;
        [SerializeField] private Color healthLowColor = Color.red;

        [Header("Recent Activity")]
        [SerializeField] private Transform activityLogContainer;
        [SerializeField] private GameObject activityLogEntryPrefab;
        [SerializeField] private int maxActivityEntries = 5;

        [Header("Action Buttons")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button addBuildingButton;
        [SerializeField] private Button setRallyPointButton;
        [SerializeField] private Button transferButton;
        [SerializeField] private Button defendButton;
        [SerializeField] private Button repairButton;

        [Header("Upgrade Info")]
        [SerializeField] private GameObject upgradeInfoPanel;
        [SerializeField] private TextMeshProUGUI upgradeCostText;
        [SerializeField] private TextMeshProUGUI upgradeRequirementsText;
        [SerializeField] private Button confirmUpgradeButton;

        // Events
        public event Action OnBackClicked;
        public event Action OnEditClicked;
        public event Action OnUpgradeClicked;
        public event Action OnAddBuildingClicked;
        public event Action OnDefendClicked;
        public event Action OnRepairClicked;

        // State
        private Territory.Territory _currentTerritory;
        private List<ActivityLogEntry> _activityLog = new List<ActivityLogEntry>();

        private void Awake()
        {
            SetupButtonListeners();
        }

        private void SetupButtonListeners()
        {
            if (backButton != null)
                backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
            if (editButton != null)
                editButton.onClick.AddListener(() => OnEditClicked?.Invoke());
            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(HandleUpgradeClick);
            if (addBuildingButton != null)
                addBuildingButton.onClick.AddListener(() => OnAddBuildingClicked?.Invoke());
            if (defendButton != null)
                defendButton.onClick.AddListener(() => OnDefendClicked?.Invoke());
            if (repairButton != null)
                repairButton.onClick.AddListener(() => OnRepairClicked?.Invoke());
            if (confirmUpgradeButton != null)
                confirmUpgradeButton.onClick.AddListener(ConfirmUpgrade);
        }

        /// <summary>
        /// Set the territory to display
        /// </summary>
        public void SetTerritory(Territory.Territory territory)
        {
            _currentTerritory = territory;
            RefreshDisplay();
        }

        /// <summary>
        /// Refresh the panel display with current territory data
        /// </summary>
        public void RefreshDisplay()
        {
            if (_currentTerritory == null) return;

            // Header
            if (territoryNameText != null)
                territoryNameText.text = _currentTerritory.Name;
            if (levelText != null)
                levelText.text = $"Level {_currentTerritory.Level}";

            // Health
            UpdateHealthDisplay();

            // Stats
            UpdateStatsDisplay();

            // Action buttons
            UpdateActionButtons();

            // Activity log
            RefreshActivityLog();
        }

        private void UpdateHealthDisplay()
        {
            if (_currentTerritory == null) return;

            float healthPercent = (float)_currentTerritory.Health / _currentTerritory.MaxHealth;

            if (healthSlider != null)
                healthSlider.value = healthPercent;

            if (healthText != null)
                healthText.text = $"{_currentTerritory.Health}/{_currentTerritory.MaxHealth}";

            if (healthFillImage != null)
                healthFillImage.color = Color.Lerp(healthLowColor, healthFullColor, healthPercent);
        }

        private void UpdateStatsDisplay()
        {
            if (_currentTerritory == null) return;

            // Defense rating (based on buildings)
            int defenseRating = CalculateDefenseRating();
            if (defenseSlider != null)
                defenseSlider.value = defenseRating / 100f;
            if (defenseText != null)
                defenseText.text = $"{defenseRating}";

            // Income rate
            int incomeRate = CalculateIncomeRate();
            if (incomeSlider != null)
                incomeSlider.value = Mathf.Clamp01(incomeRate / 200f);
            if (incomeText != null)
                incomeText.text = $"{incomeRate}/h";

            // Block count
            int blockCount = GetBlockCount();
            int maxBlocks = GetMaxBlocks();
            if (blockCountText != null)
                blockCountText.text = $"{blockCount}";
            if (maxBlocksText != null)
                maxBlocksText.text = $"/{maxBlocks}";
        }

        private void UpdateActionButtons()
        {
            if (_currentTerritory == null) return;

            // Upgrade button - check if can afford
            if (upgradeButton != null)
            {
                bool canUpgrade = CanAffordUpgrade();
                upgradeButton.interactable = canUpgrade;
            }

            // Repair button - only active if damaged
            if (repairButton != null)
            {
                bool needsRepair = _currentTerritory.Health < _currentTerritory.MaxHealth;
                repairButton.interactable = needsRepair;
            }

            // Defend button - active if under attack
            if (defendButton != null)
            {
                bool isContested = _currentTerritory.IsContested;
                defendButton.gameObject.SetActive(isContested);
            }
        }

        #region Activity Log

        private void RefreshActivityLog()
        {
            if (activityLogContainer == null) return;

            // Clear existing entries
            foreach (Transform child in activityLogContainer)
            {
                Destroy(child.gameObject);
            }

            // Add entries
            foreach (var entry in _activityLog)
            {
                CreateActivityLogEntry(entry);
            }
        }

        private void CreateActivityLogEntry(ActivityLogEntry entry)
        {
            if (activityLogEntryPrefab == null || activityLogContainer == null) return;

            GameObject entryObj = Instantiate(activityLogEntryPrefab, activityLogContainer);
            var text = entryObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"• {entry.Message}";
                text.color = GetActivityColor(entry.Type);
            }
        }

        /// <summary>
        /// Add an activity log entry
        /// </summary>
        public void AddActivityEntry(string message, ActivityType type = ActivityType.Info)
        {
            _activityLog.Insert(0, new ActivityLogEntry { Message = message, Type = type, Time = DateTime.Now });

            // Trim to max entries
            while (_activityLog.Count > maxActivityEntries)
            {
                _activityLog.RemoveAt(_activityLog.Count - 1);
            }

            RefreshActivityLog();
        }

        private Color GetActivityColor(ActivityType type)
        {
            return type switch
            {
                ActivityType.Combat => new Color(1f, 0.3f, 0.3f),
                ActivityType.Build => new Color(0.3f, 0.8f, 0.3f),
                ActivityType.Resource => new Color(0.9f, 0.7f, 0.2f),
                ActivityType.Defense => new Color(0.3f, 0.6f, 1f),
                _ => Color.white
            };
        }

        #endregion

        #region Upgrade Handling

        private void HandleUpgradeClick()
        {
            if (upgradeInfoPanel != null)
            {
                upgradeInfoPanel.SetActive(!upgradeInfoPanel.activeSelf);

                if (upgradeInfoPanel.activeSelf)
                {
                    UpdateUpgradeInfo();
                }
            }

            OnUpgradeClicked?.Invoke();
        }

        private void UpdateUpgradeInfo()
        {
            if (_currentTerritory == null) return;

            int nextLevel = _currentTerritory.Level + 1;
            var cost = GetUpgradeCost(nextLevel);

            if (upgradeCostText != null)
            {
                upgradeCostText.text = $"Cost: {cost.Stone} Stone, {cost.Wood} Wood, {cost.Metal} Metal";
            }

            if (upgradeRequirementsText != null)
            {
                upgradeRequirementsText.text = $"Requires: {GetUpgradeRequirements(nextLevel)}";
            }

            if (confirmUpgradeButton != null)
            {
                confirmUpgradeButton.interactable = CanAffordUpgrade();
            }
        }

        private void ConfirmUpgrade()
        {
            if (_currentTerritory == null || !CanAffordUpgrade()) return;

            // TODO: Call backend to perform upgrade
            Debug.Log($"[TerritoryDetail] Upgrading territory {_currentTerritory.Id} to level {_currentTerritory.Level + 1}");

            // Hide upgrade panel
            if (upgradeInfoPanel != null)
                upgradeInfoPanel.SetActive(false);

            AddActivityEntry($"Upgraded to Level {_currentTerritory.Level + 1}", ActivityType.Build);
        }

        #endregion

        #region Calculations

        private int CalculateDefenseRating()
        {
            // TODO: Calculate from actual buildings
            return 50 + _currentTerritory?.Level ?? 0 * 10;
        }

        private int CalculateIncomeRate()
        {
            // TODO: Calculate from resource-generating buildings
            return 30 + _currentTerritory?.Level ?? 0 * 15;
        }

        private int GetBlockCount()
        {
            // TODO: Get from BuildingManager
            return 47;
        }

        private int GetMaxBlocks()
        {
            // Base 50 + 25 per level
            int level = _currentTerritory?.Level ?? 1;
            return 50 + (level * 25);
        }

        private bool CanAffordUpgrade()
        {
            // TODO: Check player resources against upgrade cost
            return true;
        }

        private ResourceCost GetUpgradeCost(int targetLevel)
        {
            return new ResourceCost
            {
                Stone = targetLevel * 100,
                Wood = targetLevel * 80,
                Metal = targetLevel * 50
            };
        }

        private string GetUpgradeRequirements(int targetLevel)
        {
            if (targetLevel <= 3) return "None";
            if (targetLevel <= 5) return "Alliance membership";
            if (targetLevel <= 7) return "Alliance Tier 2+";
            return "Alliance Tier 3+, 10 territories";
        }

        #endregion
    }

    /// <summary>
    /// Activity log entry
    /// </summary>
    [Serializable]
    public class ActivityLogEntry
    {
        public string Message;
        public ActivityType Type;
        public DateTime Time;
    }

    /// <summary>
    /// Activity types for log coloring
    /// </summary>
    public enum ActivityType
    {
        Info,
        Combat,
        Build,
        Resource,
        Defense
    }

    /// <summary>
    /// Simple resource cost structure
    /// </summary>
    [Serializable]
    public class ResourceCost
    {
        public int Stone;
        public int Wood;
        public int Metal;
        public int Crystal;
    }
}
