using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// PC-exclusive Statistics Dashboard showing detailed analytics.
    /// Provides deep insights into player progress, combat history, and resource trends.
    /// </summary>
    public class StatisticsPanel : MonoBehaviour
    {
        [Header("Overview Stats")]
        [SerializeField] private TextMeshProUGUI totalTerritoriesText;
        [SerializeField] private TextMeshProUGUI totalBuildingsText;
        [SerializeField] private TextMeshProUGUI allianceRankText;
        [SerializeField] private TextMeshProUGUI globalRankText;
        [SerializeField] private TextMeshProUGUI playTimeText;

        [Header("Combat Stats")]
        [SerializeField] private TextMeshProUGUI attacksWonText;
        [SerializeField] private TextMeshProUGUI attacksLostText;
        [SerializeField] private TextMeshProUGUI defensesWonText;
        [SerializeField] private TextMeshProUGUI defensesLostText;
        [SerializeField] private Slider winRateSlider;
        [SerializeField] private TextMeshProUGUI winRateText;

        [Header("Resource Stats")]
        [SerializeField] private TextMeshProUGUI totalStoneCollectedText;
        [SerializeField] private TextMeshProUGUI totalWoodCollectedText;
        [SerializeField] private TextMeshProUGUI totalMetalCollectedText;
        [SerializeField] private TextMeshProUGUI totalCrystalCollectedText;
        [SerializeField] private TextMeshProUGUI currentProductionRateText;

        [Header("Territory Breakdown")]
        [SerializeField] private Transform territoryBreakdownContainer;
        [SerializeField] private GameObject territoryStatEntryPrefab;

        [Header("Resource Graph")]
        [SerializeField] private RectTransform graphContainer;
        [SerializeField] private Image graphLinePrefab;
        [SerializeField] private Color stoneGraphColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color woodGraphColor = new Color(0.6f, 0.4f, 0.2f);
        [SerializeField] private Color metalGraphColor = new Color(0.7f, 0.7f, 0.8f);
        [SerializeField] private Color crystalGraphColor = new Color(0.8f, 0.4f, 0.9f);

        [Header("Time Range")]
        [SerializeField] private Button dayButton;
        [SerializeField] private Button weekButton;
        [SerializeField] private Button monthButton;
        [SerializeField] private Button allTimeButton;

        // State
        private StatTimeRange _currentTimeRange = StatTimeRange.Week;
        private PlayerStatistics _stats;
        private List<ResourceDataPoint> _resourceHistory = new List<ResourceDataPoint>();

        private void Awake()
        {
            SetupTimeRangeButtons();
        }

        private void OnEnable()
        {
            RefreshStatistics();
        }

        private void SetupTimeRangeButtons()
        {
            if (dayButton != null)
                dayButton.onClick.AddListener(() => SetTimeRange(StatTimeRange.Day));
            if (weekButton != null)
                weekButton.onClick.AddListener(() => SetTimeRange(StatTimeRange.Week));
            if (monthButton != null)
                monthButton.onClick.AddListener(() => SetTimeRange(StatTimeRange.Month));
            if (allTimeButton != null)
                allTimeButton.onClick.AddListener(() => SetTimeRange(StatTimeRange.AllTime));
        }

        #region Time Range

        private void SetTimeRange(StatTimeRange range)
        {
            _currentTimeRange = range;
            RefreshStatistics();
        }

        private DateTime GetStartDate()
        {
            return _currentTimeRange switch
            {
                StatTimeRange.Day => DateTime.UtcNow.AddDays(-1),
                StatTimeRange.Week => DateTime.UtcNow.AddDays(-7),
                StatTimeRange.Month => DateTime.UtcNow.AddMonths(-1),
                StatTimeRange.AllTime => DateTime.MinValue,
                _ => DateTime.UtcNow.AddDays(-7)
            };
        }

        #endregion

        #region Data Loading

        public void RefreshStatistics()
        {
            // Load statistics from backend
            LoadStatistics();

            // Update displays
            UpdateOverviewStats();
            UpdateCombatStats();
            UpdateResourceStats();
            UpdateTerritoryBreakdown();
            UpdateResourceGraph();
        }

        private void LoadStatistics()
        {
            // TODO: Load from backend/analytics
            // For now, use mock data
            _stats = new PlayerStatistics
            {
                TotalTerritories = 12,
                TotalBuildings = 347,
                AllianceRank = 5,
                GlobalRank = 1247,
                TotalPlayTimeMinutes = 4320, // 72 hours

                AttacksWon = 28,
                AttacksLost = 7,
                DefensesWon = 45,
                DefensesLost = 12,

                TotalStoneCollected = 125000,
                TotalWoodCollected = 98000,
                TotalMetalCollected = 45000,
                TotalCrystalCollected = 8500,
                CurrentProductionRate = new ResourceRate
                {
                    StonePerHour = 450,
                    WoodPerHour = 380,
                    MetalPerHour = 120,
                    CrystalPerHour = 25
                },

                TerritoryStats = GenerateMockTerritoryStats()
            };

            _resourceHistory = GenerateMockResourceHistory();
        }

        private List<TerritoryStatEntry> GenerateMockTerritoryStats()
        {
            return new List<TerritoryStatEntry>
            {
                new TerritoryStatEntry { Name = "Central Citadel", Level = 5, Buildings = 87, Income = 150 },
                new TerritoryStatEntry { Name = "Northern Outpost", Level = 3, Buildings = 42, Income = 80 },
                new TerritoryStatEntry { Name = "Eastern Mine", Level = 4, Buildings = 35, Income = 120 },
                new TerritoryStatEntry { Name = "Southern Watch", Level = 2, Buildings = 28, Income = 45 },
                new TerritoryStatEntry { Name = "Crystal Valley", Level = 3, Buildings = 31, Income = 95 }
            };
        }

        private List<ResourceDataPoint> GenerateMockResourceHistory()
        {
            var history = new List<ResourceDataPoint>();
            System.Random rand = new System.Random(42);

            for (int i = 0; i < 30; i++)
            {
                history.Add(new ResourceDataPoint
                {
                    Date = DateTime.UtcNow.AddDays(-30 + i),
                    Stone = 1000 + rand.Next(500) + i * 50,
                    Wood = 800 + rand.Next(400) + i * 40,
                    Metal = 300 + rand.Next(200) + i * 20,
                    Crystal = 50 + rand.Next(50) + i * 5
                });
            }

            return history;
        }

        #endregion

        #region Display Updates

        private void UpdateOverviewStats()
        {
            if (_stats == null) return;

            if (totalTerritoriesText != null)
                totalTerritoriesText.text = _stats.TotalTerritories.ToString();

            if (totalBuildingsText != null)
                totalBuildingsText.text = FormatNumber(_stats.TotalBuildings);

            if (allianceRankText != null)
                allianceRankText.text = $"#{_stats.AllianceRank}";

            if (globalRankText != null)
                globalRankText.text = $"#{FormatNumber(_stats.GlobalRank)}";

            if (playTimeText != null)
            {
                int hours = _stats.TotalPlayTimeMinutes / 60;
                int minutes = _stats.TotalPlayTimeMinutes % 60;
                playTimeText.text = $"{hours}h {minutes}m";
            }
        }

        private void UpdateCombatStats()
        {
            if (_stats == null) return;

            if (attacksWonText != null)
                attacksWonText.text = _stats.AttacksWon.ToString();
            if (attacksLostText != null)
                attacksLostText.text = _stats.AttacksLost.ToString();
            if (defensesWonText != null)
                defensesWonText.text = _stats.DefensesWon.ToString();
            if (defensesLostText != null)
                defensesLostText.text = _stats.DefensesLost.ToString();

            // Calculate win rate
            int totalBattles = _stats.AttacksWon + _stats.AttacksLost + _stats.DefensesWon + _stats.DefensesLost;
            int totalWins = _stats.AttacksWon + _stats.DefensesWon;
            float winRate = totalBattles > 0 ? (float)totalWins / totalBattles : 0;

            if (winRateSlider != null)
                winRateSlider.value = winRate;
            if (winRateText != null)
                winRateText.text = $"{winRate * 100:F1}%";
        }

        private void UpdateResourceStats()
        {
            if (_stats == null) return;

            if (totalStoneCollectedText != null)
                totalStoneCollectedText.text = FormatNumber(_stats.TotalStoneCollected);
            if (totalWoodCollectedText != null)
                totalWoodCollectedText.text = FormatNumber(_stats.TotalWoodCollected);
            if (totalMetalCollectedText != null)
                totalMetalCollectedText.text = FormatNumber(_stats.TotalMetalCollected);
            if (totalCrystalCollectedText != null)
                totalCrystalCollectedText.text = FormatNumber(_stats.TotalCrystalCollected);

            if (currentProductionRateText != null && _stats.CurrentProductionRate != null)
            {
                currentProductionRateText.text = 
                    $"[Q] {_stats.CurrentProductionRate.StonePerHour}/h  " +
                    $"[W] {_stats.CurrentProductionRate.WoodPerHour}/h  " +
                    $"[P] {_stats.CurrentProductionRate.MetalPerHour}/h  " +
                    $"[G] {_stats.CurrentProductionRate.CrystalPerHour}/h";
            }
        }

        private void UpdateTerritoryBreakdown()
        {
            if (territoryBreakdownContainer == null || _stats?.TerritoryStats == null)
                return;

            // Clear existing
            foreach (Transform child in territoryBreakdownContainer)
            {
                Destroy(child.gameObject);
            }

            // Create entries
            foreach (var territory in _stats.TerritoryStats)
            {
                CreateTerritoryStatEntry(territory);
            }
        }

        private void CreateTerritoryStatEntry(TerritoryStatEntry entry)
        {
            if (territoryStatEntryPrefab == null) return;

            GameObject entryObj = Instantiate(territoryStatEntryPrefab, territoryBreakdownContainer);
            var texts = entryObj.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 4)
            {
                texts[0].text = entry.Name;
                texts[1].text = $"Lv.{entry.Level}";
                texts[2].text = $"{entry.Buildings} buildings";
                texts[3].text = $"+{entry.Income}/h";
            }
        }

        private void UpdateResourceGraph()
        {
            if (graphContainer == null || _resourceHistory == null || _resourceHistory.Count < 2)
                return;

            // Clear existing lines
            foreach (Transform child in graphContainer)
            {
                Destroy(child.gameObject);
            }

            // Filter by time range
            DateTime startDate = GetStartDate();
            var filteredData = _resourceHistory.FindAll(d => d.Date >= startDate);

            if (filteredData.Count < 2) return;

            // Normalize and draw lines
            DrawResourceLine(filteredData, r => r.Stone, stoneGraphColor);
            DrawResourceLine(filteredData, r => r.Wood, woodGraphColor);
            DrawResourceLine(filteredData, r => r.Metal, metalGraphColor);
            DrawResourceLine(filteredData, r => r.Crystal, crystalGraphColor);
        }

        private void DrawResourceLine(List<ResourceDataPoint> data, Func<ResourceDataPoint, int> valueSelector, Color color)
        {
            if (graphLinePrefab == null) return;

            // Find max value for scaling
            int maxValue = 1;
            foreach (var point in data)
            {
                int value = valueSelector(point);
                if (value > maxValue) maxValue = value;
            }

            float width = graphContainer.rect.width;
            float height = graphContainer.rect.height;

            // Draw line segments
            for (int i = 0; i < data.Count - 1; i++)
            {
                float x1 = (float)i / (data.Count - 1) * width;
                float y1 = (float)valueSelector(data[i]) / maxValue * height;
                float x2 = (float)(i + 1) / (data.Count - 1) * width;
                float y2 = (float)valueSelector(data[i + 1]) / maxValue * height;

                // Create line segment
                GameObject lineObj = new GameObject($"Line_{i}");
                lineObj.transform.SetParent(graphContainer, false);

                Image lineImage = lineObj.AddComponent<Image>();
                lineImage.color = color;

                RectTransform rt = lineObj.GetComponent<RectTransform>();
                
                // Calculate line position and rotation
                float dx = x2 - x1;
                float dy = y2 - y1;
                float length = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0, 0.5f);
                rt.anchoredPosition = new Vector2(x1, y1);
                rt.sizeDelta = new Vector2(length, 2f);
                rt.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        #endregion

        #region Utility

        private string FormatNumber(int value)
        {
            if (value >= 1000000)
                return $"{value / 1000000f:F1}M";
            if (value >= 1000)
                return $"{value / 1000f:F1}K";
            return value.ToString();
        }

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// Time range for statistics display
    /// </summary>
    public enum StatTimeRange
    {
        Day,
        Week,
        Month,
        AllTime
    }

    /// <summary>
    /// Player statistics data
    /// </summary>
    [Serializable]
    public class PlayerStatistics
    {
        public int TotalTerritories;
        public int TotalBuildings;
        public int AllianceRank;
        public int GlobalRank;
        public int TotalPlayTimeMinutes;

        public int AttacksWon;
        public int AttacksLost;
        public int DefensesWon;
        public int DefensesLost;

        public int TotalStoneCollected;
        public int TotalWoodCollected;
        public int TotalMetalCollected;
        public int TotalCrystalCollected;

        public ResourceRate CurrentProductionRate;
        public List<TerritoryStatEntry> TerritoryStats;
    }

    /// <summary>
    /// Resource production rate
    /// </summary>
    [Serializable]
    public class ResourceRate
    {
        public int StonePerHour;
        public int WoodPerHour;
        public int MetalPerHour;
        public int CrystalPerHour;
    }

    /// <summary>
    /// Territory statistics entry
    /// </summary>
    [Serializable]
    public class TerritoryStatEntry
    {
        public string Name;
        public int Level;
        public int Buildings;
        public int Income;
    }

    /// <summary>
    /// Resource data point for graphing
    /// </summary>
    [Serializable]
    public class ResourceDataPoint
    {
        public DateTime Date;
        public int Stone;
        public int Wood;
        public int Metal;
        public int Crystal;
    }

    #endregion
}
