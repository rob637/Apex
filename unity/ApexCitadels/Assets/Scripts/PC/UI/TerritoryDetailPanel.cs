using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.Territory;
using ApexCitadels.Building;
using ApexCitadels.Resources;
using ApexCitadels.Core;
using ApexCitadels.Player;
using ApexCitadels.PC.Economy;

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
        
        // Dynamic components found at runtime
        private TextMeshProUGUI _dynamicNameText;
        private TextMeshProUGUI _dynamicLevelText;
        private TextMeshProUGUI _dynamicInfoText;

        private void Awake()
        {
            FindDynamicComponents();
            SetupButtonListeners();
        }

        /// <summary>
        /// Find UI components if not assigned (for dynamically created panels)
        /// </summary>
        private void FindDynamicComponents()
        {
            if (territoryNameText == null)
            {
                var nameObj = transform.Find("Header/TerritoryName");
                if (nameObj != null)
                    _dynamicNameText = nameObj.GetComponent<TextMeshProUGUI>();
            }
            
            if (levelText == null)
            {
                var levelObj = transform.Find("Header/Level");
                if (levelObj != null)
                    _dynamicLevelText = levelObj.GetComponent<TextMeshProUGUI>();
            }
            
            // Find info text in content
            var infoObj = transform.Find("Content/InfoText");
            if (infoObj != null)
                _dynamicInfoText = infoObj.GetComponent<TextMeshProUGUI>();
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

            ApexLogger.Log($"[TerritoryPanel] Displaying territory: {_currentTerritory.Name}", ApexLogger.LogCategory.UI);

            // Header - use serialized or dynamic
            TextMeshProUGUI nameDisplay = territoryNameText ?? _dynamicNameText;
            TextMeshProUGUI levelDisplay = levelText ?? _dynamicLevelText;
            
            if (nameDisplay != null)
                nameDisplay.text = _currentTerritory.Name;
            if (levelDisplay != null)
                levelDisplay.text = $"★ Level {_currentTerritory.Level}";

            // Update dynamic info text with comprehensive territory data
            if (_dynamicInfoText != null)
            {
                string ownerInfo = string.IsNullOrEmpty(_currentTerritory.OwnerName) 
                    ? "Unclaimed" 
                    : $"Owner: {_currentTerritory.OwnerName}";
                    
                string healthInfo = $"Health: {_currentTerritory.Health}/{_currentTerritory.MaxHealth}";
                float healthPercent = (float)_currentTerritory.Health / Mathf.Max(1, _currentTerritory.MaxHealth) * 100f;
                
                string stateInfo = _currentTerritory.IsContested ? "⚔️ CONTESTED" : "🛡️ Secure";
                
                string allianceInfo = string.IsNullOrEmpty(_currentTerritory.AllianceId) 
                    ? "No Alliance" 
                    : $"Alliance: {_currentTerritory.AllianceId}";
                
                string locationInfo = $"Location: {_currentTerritory.CenterLatitude:F4}, {_currentTerritory.CenterLongitude:F4}";
                string radiusInfo = $"Radius: {_currentTerritory.RadiusMeters}m";
                
                _dynamicInfoText.text = $@"<b>{ownerInfo}</b>
{stateInfo}

<color=#88ff88>{healthInfo}</color> ({healthPercent:F0}%)

{allianceInfo}
{locationInfo}
{radiusInfo}

<color=#aaaaaa>Level: {_currentTerritory.Level}
Income Rate: +{_currentTerritory.Level * 10}/hr</color>

<color=#ffff88>Click a button below to interact!</color>";
            }

            // Health
            UpdateHealthDisplay();

            // Stats
            UpdateStatsDisplay();;

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
        public void AddActivityEntry(string message, TerritoryActivityType type = TerritoryActivityType.Info)
        {
            _activityLog.Insert(0, new ActivityLogEntry { Message = message, Type = type, Time = DateTime.Now });

            // Trim to max entries
            while (_activityLog.Count > maxActivityEntries)
            {
                _activityLog.RemoveAt(_activityLog.Count - 1);
            }

            RefreshActivityLog();
        }

        private Color GetActivityColor(TerritoryActivityType type)
        {
            return type switch
            {
                TerritoryActivityType.Combat => new Color(1f, 0.3f, 0.3f),
                TerritoryActivityType.Build => new Color(0.3f, 0.8f, 0.3f),
                TerritoryActivityType.Resource => new Color(0.9f, 0.7f, 0.2f),
                TerritoryActivityType.Defense => new Color(0.3f, 0.6f, 1f),
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
                upgradeCostText.text = $"Cost: {cost.Stone} Stone, {cost.Wood} Wood, {cost.Iron} Iron";
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

        private async void ConfirmUpgrade()
        {
            if (_currentTerritory == null || !CanAffordUpgrade()) return;

            int nextLevel = _currentTerritory.Level + 1;
            ApexLogger.Log($"[TerritoryDetail] Upgrading territory {_currentTerritory.Id} to level {nextLevel}", ApexLogger.LogCategory.Territory);

            // Deduct upgrade cost
            var cost = GetUpgradeCost(nextLevel);
            if (ResourceSpendingManager.Instance != null)
            {
                ResourceSpendingManager.Instance.Spend(cost, "Territory Upgrade");
            }

            // Call backend to perform upgrade
            bool success = await TerritoryManager.Instance.UpgradeTerritory(_currentTerritory.Id);
            
            if (success)
            {
                ApexLogger.Log($"[TerritoryDetail] Territory upgraded to level {nextLevel}", ApexLogger.LogCategory.Territory);
                AddActivityEntry($"Upgraded to Level {nextLevel}", TerritoryActivityType.Build);
                
                // Refresh display
                if (levelText != null)
                    levelText.text = $"Level {nextLevel}";
            }
            else
            {
                ApexLogger.LogWarning("[TerritoryDetail] Upgrade failed!", ApexLogger.LogCategory.Territory);
            }

            // Hide upgrade panel
            if (upgradeInfoPanel != null)
                upgradeInfoPanel.SetActive(false);
        }

        #endregion

        #region Calculations

        private int CalculateDefenseRating()
        {
            // Calculate defense from buildings in territory
            int baseDefense = 50 + (_currentTerritory?.Level ?? 0) * 10;
            
            if (_currentTerritory != null && BuildingManager.Instance != null)
            {
                var blocks = BuildingManager.Instance.GetBlocksInTerritory(_currentTerritory.Id);
                // Defense blocks add to defense rating
                int blockDefense = 0;
                foreach (var block in blocks)
                {
                    if (block.Type == BlockType.Stone || block.Type == BlockType.Metal)
                    {
                        blockDefense += block.Type == BlockType.Metal ? 3 : 2;
                    }
                }
                return baseDefense + blockDefense;
            }
            
            return baseDefense;
        }

        private int CalculateIncomeRate()
        {
            // Calculate income from resource-generating buildings
            int baseIncome = 30 + (_currentTerritory?.Level ?? 0) * 15;
            
            if (_currentTerritory != null && BuildingManager.Instance != null)
            {
                var blocks = BuildingManager.Instance.GetBlocksInTerritory(_currentTerritory.Id);
                // Resource blocks generate income
                int resourceBonus = 0;
                foreach (var block in blocks)
                {
                    if (block.Type == BlockType.Wood || block.Type == BlockType.Glass)
                    {
                        resourceBonus += 5;
                    }
                }
                return baseIncome + resourceBonus;
            }
            
            return baseIncome;
        }

        private int GetBlockCount()
        {
            // Get actual block count from BuildingManager
            if (_currentTerritory != null && BuildingManager.Instance != null)
            {
                var blocks = BuildingManager.Instance.GetBlocksInTerritory(_currentTerritory.Id);
                return blocks.Count;
            }
            return 0;
        }

        private int GetMaxBlocks()
        {
            // Base 50 + 25 per level
            int level = _currentTerritory?.Level ?? 1;
            return 50 + (level * 25);
        }

        private bool CanAffordUpgrade()
        {
            // Check player resources against upgrade cost
            if (_currentTerritory == null) return false;
            
            Economy.ResourceCost cost = GetUpgradeCost(_currentTerritory.Level + 1);
            
            if (ResourceSpendingManager.Instance != null)
            {
                return ResourceSpendingManager.Instance.CanAfford(cost);
            }
            
            // Fallback: always allow if manager not available
            return true;
        }

        private Economy.ResourceCost GetUpgradeCost(int targetLevel)
        {
            var cost = new Economy.ResourceCost();
            cost.Stone = targetLevel * 100;
            cost.Wood = targetLevel * 80;
            cost.Iron = targetLevel * 50;
            return cost;
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
        public TerritoryActivityType Type;
        public DateTime Time;
    }

    /// <summary>
    /// Activity types for territory log coloring
    /// </summary>
    public enum TerritoryActivityType
    {
        Info,
        Combat,
        Build,
        Resource,
        Defense
    }

}
