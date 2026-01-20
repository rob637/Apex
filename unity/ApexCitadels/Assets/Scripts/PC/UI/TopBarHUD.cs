using System;
using ApexCitadels.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.UI;
using ApexCitadels.Environment;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Top HUD bar showing resources, quick stats, and access buttons.
    /// Always visible during gameplay.
    /// </summary>
    public class TopBarHUD : MonoBehaviour
    {
        // UI Elements
        private GameObject _hudBar;
        private TextMeshProUGUI _goldText;
        private TextMeshProUGUI _crystalText;
        private TextMeshProUGUI _apexCoinsText;
        private TextMeshProUGUI _playerLevelText;
        private TextMeshProUGUI _timeText;
        
        // Quick access buttons
        private Button _leaderboardBtn;
        private Button _seasonPassBtn;
        private Button _dailyRewardsBtn;
        private Button _settingsBtn;
        
        public static TopBarHUD Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            CreateHUD();
            
            // Subscribe to resource updates
            if (PCResourceSystem.Instance != null)
            {
                PCResourceSystem.Instance.OnResourceChanged += UpdateResourceDisplay;
            }
        }

        private void Update()
        {
            // Update time display
            if (_timeText != null && DayNightCycle.Instance != null)
            {
                int hour = Mathf.FloorToInt(DayNightCycle.Instance.CurrentHour);
                int min = Mathf.FloorToInt((DayNightCycle.Instance.CurrentHour - hour) * 60);
                string ampm = hour >= 12 ? "PM" : "AM";
                int displayHour = hour > 12 ? hour - 12 : (hour == 0 ? 12 : hour);
                _timeText.text = $"{GameIcons.Clock} {displayHour}:{min:00} {ampm}";
            }
        }

        private void OnDestroy()
        {
            if (PCResourceSystem.Instance != null)
            {
                PCResourceSystem.Instance.OnResourceChanged -= UpdateResourceDisplay;
            }
        }

        private void CreateHUD()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main HUD bar at top
            _hudBar = new GameObject("TopBarHUD");
            _hudBar.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _hudBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 50);
            
            // Background
            Image bg = _hudBar.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);
            
            // Layout
            HorizontalLayoutGroup layout = _hudBar.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.spacing = 20;
            layout.padding = new RectOffset(20, 20, 5, 5);
            
            // Left side: Resources
            CreateResourceDisplay();
            
            // Spacer
            CreateSpacer();
            
            // Center: Player level and time
            CreateCenterInfo();
            
            // Spacer
            CreateSpacer();
            
            // Right side: Quick access buttons
            CreateQuickAccessButtons();
        }

        private void CreateResourceDisplay()
        {
            // Use GameIcons for proper sprite rendering
            // Gold
            _goldText = CreateResourceItem(GameIcons.Gold, "0", new Color(1f, 0.85f, 0f));
            
            // Crystal  
            _crystalText = CreateResourceItem(GameIcons.Gems, "0", new Color(0.3f, 0.8f, 1f));
            
            // ApexCoins (premium)
            _apexCoinsText = CreateResourceItem(GameIcons.ApexCoins, "0", new Color(0.8f, 0.3f, 0.9f));
        }

        private TextMeshProUGUI CreateResourceItem(string icon, string value, Color color)
        {
            GameObject item = new GameObject("Resource");
            item.transform.SetParent(_hudBar.transform, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredWidth = 100;
            
            HorizontalLayoutGroup layout = item.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.spacing = 5;
            
            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(item.transform, false);
            
            TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
            iconText.text = icon;
            iconText.fontSize = 20;
            iconText.alignment = TextAlignmentOptions.Center;
            
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 30;
            
            // Value
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(item.transform, false);
            
            TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = value;
            valueText.fontSize = 16;
            valueText.fontStyle = FontStyles.Bold;
            valueText.alignment = TextAlignmentOptions.Left;
            valueText.color = color;
            
            LayoutElement valueLE = valueObj.AddComponent<LayoutElement>();
            valueLE.preferredWidth = 70;
            
            return valueText;
        }

        private void CreateSpacer()
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(_hudBar.transform, false);
            
            LayoutElement le = spacer.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
        }

        private void CreateCenterInfo()
        {
            GameObject center = new GameObject("CenterInfo");
            center.transform.SetParent(_hudBar.transform, false);
            
            HorizontalLayoutGroup layout = center.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 30;
            
            // Player level
            GameObject levelObj = new GameObject("Level");
            levelObj.transform.SetParent(center.transform, false);
            
            _playerLevelText = levelObj.AddComponent<TextMeshProUGUI>();
            _playerLevelText.text = $"{GameIcons.StarFilled} Level 1";
            _playerLevelText.fontSize = 18;
            _playerLevelText.fontStyle = FontStyles.Bold;
            _playerLevelText.alignment = TextAlignmentOptions.Center;
            _playerLevelText.color = new Color(1f, 0.9f, 0.4f);
            
            LayoutElement levelLE = levelObj.AddComponent<LayoutElement>();
            levelLE.preferredWidth = 100;
            
            // Time
            GameObject timeObj = new GameObject("Time");
            timeObj.transform.SetParent(center.transform, false);
            
            _timeText = timeObj.AddComponent<TextMeshProUGUI>();
            _timeText.text = $"{GameIcons.Clock} 12:00 PM";
            _timeText.fontSize = 14;
            _timeText.alignment = TextAlignmentOptions.Center;
            _timeText.color = new Color(0.7f, 0.7f, 0.7f);
            
            LayoutElement timeLE = timeObj.AddComponent<LayoutElement>();
            timeLE.preferredWidth = 100;
        }

        private void CreateQuickAccessButtons()
        {
            // Daily Rewards
            _dailyRewardsBtn = CreateQuickButton(GameIcons.Gift, "Daily", () =>
            {
                if (DailyRewardsUI.Instance != null)
                    DailyRewardsUI.Instance.Toggle();
            });
            
            // Season Pass
            _seasonPassBtn = CreateQuickButton(GameIcons.Medal, "Pass", () =>
            {
                if (SeasonPassPanel.Instance != null)
                    SeasonPassPanel.Instance.Toggle();
            });
            
            // Leaderboard
            _leaderboardBtn = CreateQuickButton(GameIcons.Trophy, "Ranks", () =>
            {
                if (LeaderboardPanel.Instance != null)
                    LeaderboardPanel.Instance.Toggle();
            });
            
            // Settings
            _settingsBtn = CreateQuickButton(GameIcons.Settings, "Menu", () =>
            {
                if (SettingsPanel.Instance != null)
                    SettingsPanel.Instance.Toggle();
                else
                    ApexCitadels.Core.ApexLogger.LogWarning("SettingsPanel not found", ApexCitadels.Core.ApexLogger.LogCategory.UI);
            });
        }

        private Button CreateQuickButton(string icon, string label, Action onClick)
        {
            GameObject btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(_hudBar.transform, false);
            
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = 60;
            le.preferredHeight = 40;
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
            
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.4f, 0.6f);
            colors.pressedColor = new Color(0.2f, 0.3f, 0.5f);
            btn.colors = colors;
            
            // Vertical layout for icon + label
            VerticalLayoutGroup vlayout = btnObj.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 0;
            vlayout.padding = new RectOffset(2, 2, 2, 2);
            
            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);
            
            TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
            iconText.text = icon;
            iconText.fontSize = 18;
            iconText.alignment = TextAlignmentOptions.Center;
            
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredHeight = 22;
            
            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);
            
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 10;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0.7f, 0.7f, 0.7f);
            
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.preferredHeight = 14;
            
            return btn;
        }

        private void UpdateResourceDisplay(ResourceType type, int oldVal, int newVal)
        {
            switch (type)
            {
                case ResourceType.Gold:
                    if (_goldText != null) _goldText.text = FormatNumber(newVal);
                    break;
                case ResourceType.Crystal:
                    if (_crystalText != null) _crystalText.text = FormatNumber(newVal);
                    break;
                case ResourceType.ApexCoins:
                    if (_apexCoinsText != null) _apexCoinsText.text = FormatNumber(newVal);
                    break;
            }
        }

        private string FormatNumber(int value)
        {
            if (value >= 1000000) return $"{value / 1000000f:F1}M";
            if (value >= 1000) return $"{value / 1000f:F1}K";
            return value.ToString();
        }

        /// <summary>
        /// Update player level display
        /// </summary>
        public void SetPlayerLevel(int level)
        {
            if (_playerLevelText != null)
            {
                _playerLevelText.text = $"[*] Level {level}";
            }
        }

        /// <summary>
        /// Show notification badge on a button
        /// </summary>
        public void ShowNotificationBadge(string buttonName, bool show, int count = 0)
        {
            // TODO: Implement notification badges
        }
    }
}
