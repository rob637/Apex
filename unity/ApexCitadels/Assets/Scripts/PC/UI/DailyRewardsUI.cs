using System;
using ApexCitadels.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Daily Login Rewards System for PC client.
    /// Tracks streaks, displays reward calendar, and grants bonuses.
    /// </summary>
    public class DailyRewardsUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject rewardsPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button claimButton;
        
        [Header("Streak Display")]
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private TextMeshProUGUI nextRewardText;
        [SerializeField] private Image streakProgressBar;
        
        [Header("Rewards Grid")]
        [SerializeField] private Transform rewardsContainer;
        [SerializeField] private GameObject rewardDayPrefab;
        
        [Header("Settings")]
        [SerializeField] private int daysInCycle = 7; // Weekly cycle
        [SerializeField] private float baseGoldReward = 100f;
        [SerializeField] private float streakMultiplier = 0.1f; // +10% per day
        
        // State
        private int _currentStreak;
        private int _currentDayInCycle;
        private DateTime _lastClaimDate;
        private bool _canClaimToday;
        private List<DailyReward> _rewards;
        
        // Events
        public event Action<DailyReward> OnRewardClaimed;
        public event Action OnStreakLost;
        
        public static DailyRewardsUI Instance { get; private set; }

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
            InitializeRewards();
            LoadProgress();
            CheckDailyReset();
            
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
            if (claimButton != null)
                claimButton.onClick.AddListener(ClaimReward);
        }

        private void InitializeRewards()
        {
            _rewards = new List<DailyReward>();
            
            // Day 1: Basic gold
            _rewards.Add(new DailyReward { Day = 1, Gold = 100, Description = "Welcome Back!" });
            
            // Day 2: Gold + Stone
            _rewards.Add(new DailyReward { Day = 2, Gold = 150, Stone = 50, Description = "Building Materials" });
            
            // Day 3: Gold + Wood
            _rewards.Add(new DailyReward { Day = 3, Gold = 200, Wood = 75, Description = "Fortify!" });
            
            // Day 4: Increased gold
            _rewards.Add(new DailyReward { Day = 4, Gold = 300, Description = "Halfway There!" });
            
            // Day 5: Iron unlocks
            _rewards.Add(new DailyReward { Day = 5, Gold = 250, Iron = 30, Description = "Advanced Resources" });
            
            // Day 6: Big resource haul
            _rewards.Add(new DailyReward { Day = 6, Gold = 400, Stone = 100, Wood = 100, Description = "Empire Builder" });
            
            // Day 7: JACKPOT + Crystal
            _rewards.Add(new DailyReward { Day = 7, Gold = 1000, Crystal = 20, IsPremium = true, Description = "WEEKLY JACKPOT!" });
        }

        private void LoadProgress()
        {
            // Load from PlayerPrefs (would be Firebase in production)
            _currentStreak = PlayerPrefs.GetInt("DailyStreak", 0);
            _currentDayInCycle = PlayerPrefs.GetInt("DailyDayInCycle", 0);
            
            string lastClaimStr = PlayerPrefs.GetString("DailyLastClaim", "");
            if (!string.IsNullOrEmpty(lastClaimStr))
            {
                DateTime.TryParse(lastClaimStr, out _lastClaimDate);
            }
            
            Debug.Log($"[DailyRewards] Loaded: Streak={_currentStreak}, Day={_currentDayInCycle}, LastClaim={_lastClaimDate}");
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt("DailyStreak", _currentStreak);
            PlayerPrefs.SetInt("DailyDayInCycle", _currentDayInCycle);
            PlayerPrefs.SetString("DailyLastClaim", _lastClaimDate.ToString("O"));
            PlayerPrefs.Save();
        }

        private void CheckDailyReset()
        {
            DateTime today = DateTime.Today;
            DateTime lastClaimDay = _lastClaimDate.Date;
            
            int daysSinceLastClaim = (today - lastClaimDay).Days;
            
            if (daysSinceLastClaim == 0)
            {
                // Already claimed today
                _canClaimToday = false;
                Debug.Log("[DailyRewards] Already claimed today");
            }
            else if (daysSinceLastClaim == 1)
            {
                // Consecutive day - streak continues!
                _canClaimToday = true;
                Debug.Log("[DailyRewards] Streak continues! Can claim today.");
            }
            else if (daysSinceLastClaim > 1)
            {
                // Streak broken
                _currentStreak = 0;
                _currentDayInCycle = 0;
                _canClaimToday = true;
                OnStreakLost?.Invoke();
                Debug.Log("[DailyRewards] Streak broken! Starting over.");
            }
            else
            {
                // First time or time travel (allow claim)
                _canClaimToday = true;
            }
            
            UpdateUI();
        }

        /// <summary>
        /// Show the daily rewards panel
        /// </summary>
        public void Show()
        {
            CheckDailyReset();
            
            if (rewardsPanel != null)
            {
                rewardsPanel.SetActive(true);
            }
            else
            {
                // Create panel dynamically
                CreateRewardsPanel();
            }
            
            UpdateUI();
        }

        /// <summary>
        /// Hide the daily rewards panel
        /// </summary>
        public void Hide()
        {
            if (rewardsPanel != null)
            {
                rewardsPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Toggle the daily rewards panel visibility
        /// </summary>
        public void Toggle()
        {
            if (rewardsPanel != null)
            {
                if (rewardsPanel.activeSelf)
                    Hide();
                else
                    Show();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// Claim today's reward
        /// </summary>
        public void ClaimReward()
        {
            if (!_canClaimToday)
            {
                Debug.Log("[DailyRewards] Already claimed today!");
                return;
            }
            
            // Advance day
            _currentDayInCycle++;
            if (_currentDayInCycle > daysInCycle)
            {
                _currentDayInCycle = 1; // Reset cycle but keep streak
            }
            
            _currentStreak++;
            _lastClaimDate = DateTime.Now;
            _canClaimToday = false;
            
            // Get reward
            DailyReward reward = GetRewardForDay(_currentDayInCycle);
            
            // Apply streak multiplier
            float multiplier = 1f + (_currentStreak * streakMultiplier);
            int bonusGold = Mathf.RoundToInt(reward.Gold * multiplier);
            int bonusStone = Mathf.RoundToInt(reward.Stone * multiplier);
            int bonusWood = Mathf.RoundToInt(reward.Wood * multiplier);
            int bonusIron = Mathf.RoundToInt(reward.Iron * multiplier);
            int bonusCrystal = Mathf.RoundToInt(reward.Crystal * multiplier);
            
            // Grant resources
            if (PCResourceSystem.Instance != null)
            {
                if (bonusGold > 0) PCResourceSystem.Instance.AddResource(ResourceType.Gold, bonusGold);
                if (bonusStone > 0) PCResourceSystem.Instance.AddResource(ResourceType.Stone, bonusStone);
                if (bonusWood > 0) PCResourceSystem.Instance.AddResource(ResourceType.Wood, bonusWood);
                if (bonusIron > 0) PCResourceSystem.Instance.AddResource(ResourceType.Iron, bonusIron);
                if (bonusCrystal > 0) PCResourceSystem.Instance.AddResource(ResourceType.Crystal, bonusCrystal);
            }
            
            SaveProgress();
            UpdateUI();
            
            Debug.Log($"[DailyRewards] Claimed Day {_currentDayInCycle}! Streak: {_currentStreak}x (Multiplier: {multiplier:F1}x)");
            Debug.Log($"[DailyRewards] Rewards: {bonusGold}g, {bonusStone}s, {bonusWood}w, {bonusIron}i, {bonusCrystal}c");
            
            OnRewardClaimed?.Invoke(reward);
            
            // Show celebration (TODO: particles, sound)
            ShowClaimCelebration(reward);
        }

        private DailyReward GetRewardForDay(int day)
        {
            int index = Mathf.Clamp(day - 1, 0, _rewards.Count - 1);
            return _rewards[index];
        }

        private void UpdateUI()
        {
            // Update streak display
            if (streakText != null)
            {
                streakText.text = $"{_currentStreak} Day Streak!";
            }
            
            // Update next reward preview
            if (nextRewardText != null)
            {
                int nextDay = _canClaimToday ? _currentDayInCycle + 1 : _currentDayInCycle + 1;
                if (nextDay > daysInCycle) nextDay = 1;
                
                DailyReward nextReward = GetRewardForDay(nextDay);
                nextRewardText.text = $"Day {nextDay}: {nextReward.Description}";
            }
            
            // Update progress bar
            if (streakProgressBar != null)
            {
                streakProgressBar.fillAmount = (float)_currentDayInCycle / daysInCycle;
            }
            
            // Update claim button
            if (claimButton != null)
            {
                claimButton.interactable = _canClaimToday;
                var buttonText = claimButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = _canClaimToday ? "CLAIM!" : "Come Back Tomorrow";
                }
            }
            
            // Update reward grid
            UpdateRewardsGrid();
        }

        private void UpdateRewardsGrid()
        {
            if (rewardsContainer == null) return;
            
            // Clear existing
            foreach (Transform child in rewardsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create day tiles
            for (int i = 0; i < _rewards.Count; i++)
            {
                DailyReward reward = _rewards[i];
                bool isClaimed = i < _currentDayInCycle;
                bool isToday = i == _currentDayInCycle && _canClaimToday;
                bool isLocked = i > _currentDayInCycle;
                
                CreateRewardTile(reward, isClaimed, isToday, isLocked);
            }
        }

        private void CreateRewardTile(DailyReward reward, bool claimed, bool isToday, bool locked)
        {
            GameObject tile = new GameObject($"Day{reward.Day}");
            tile.transform.SetParent(rewardsContainer, false);
            
            RectTransform rect = tile.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 100);
            
            // Background
            Image bg = tile.AddComponent<Image>();
            if (claimed)
                bg.color = new Color(0.2f, 0.5f, 0.2f, 0.8f); // Green - claimed
            else if (isToday)
                bg.color = new Color(0.8f, 0.6f, 0.1f, 0.9f); // Gold - available
            else if (locked)
                bg.color = new Color(0.3f, 0.3f, 0.3f, 0.6f); // Gray - locked
            else
                bg.color = new Color(0.2f, 0.2f, 0.4f, 0.8f); // Blue - upcoming
            
            // Day number
            GameObject dayLabel = new GameObject("DayLabel");
            dayLabel.transform.SetParent(tile.transform, false);
            RectTransform dayRect = dayLabel.AddComponent<RectTransform>();
            dayRect.anchorMin = new Vector2(0, 0.7f);
            dayRect.anchorMax = new Vector2(1, 1);
            dayRect.offsetMin = Vector2.zero;
            dayRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI dayText = dayLabel.AddComponent<TextMeshProUGUI>();
            dayText.text = $"Day {reward.Day}";
            dayText.fontSize = 14;
            dayText.alignment = TextAlignmentOptions.Center;
            dayText.color = Color.white;
            
            // Reward amount
            GameObject rewardLabel = new GameObject("RewardLabel");
            rewardLabel.transform.SetParent(tile.transform, false);
            RectTransform rewardRect = rewardLabel.AddComponent<RectTransform>();
            rewardRect.anchorMin = new Vector2(0, 0.2f);
            rewardRect.anchorMax = new Vector2(1, 0.7f);
            rewardRect.offsetMin = Vector2.zero;
            rewardRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI rewardText = rewardLabel.AddComponent<TextMeshProUGUI>();
            rewardText.text = GetRewardSummary(reward);
            rewardText.fontSize = 12;
            rewardText.alignment = TextAlignmentOptions.Center;
            rewardText.color = reward.IsPremium ? new Color(1f, 0.8f, 0.2f) : Color.white;
            
            // Status icon
            GameObject statusIcon = new GameObject("Status");
            statusIcon.transform.SetParent(tile.transform, false);
            RectTransform statusRect = statusIcon.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0);
            statusRect.anchorMax = new Vector2(1, 0.2f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI statusText = statusIcon.AddComponent<TextMeshProUGUI>();
            if (claimed)
                statusText.text = "✓";
            else if (isToday)
                statusText.text = "★";
            else if (locked)
                statusText.text = "🔒";
            else
                statusText.text = "";
            statusText.fontSize = 18;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = Color.white;
        }

        private string GetRewardSummary(DailyReward reward)
        {
            List<string> parts = new List<string>();
            if (reward.Gold > 0) parts.Add($"{reward.Gold}g");
            if (reward.Stone > 0) parts.Add($"{reward.Stone}s");
            if (reward.Wood > 0) parts.Add($"{reward.Wood}w");
            if (reward.Iron > 0) parts.Add($"{reward.Iron}i");
            if (reward.Crystal > 0) parts.Add($"{reward.Crystal}c");
            return string.Join("\n", parts);
        }

        private void CreateRewardsPanel()
        {
            // Find canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Create panel
            rewardsPanel = new GameObject("DailyRewardsPanel");
            rewardsPanel.transform.SetParent(canvas.transform, false);
            
            RectTransform panelRect = rewardsPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.15f, 0.15f);
            panelRect.anchorMax = new Vector2(0.85f, 0.85f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            // Background
            Image bg = rewardsPanel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);
            
            // Title
            GameObject title = new GameObject("Title");
            title.transform.SetParent(rewardsPanel.transform, false);
            RectTransform titleRect = title.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "🎁 DAILY REWARDS";
            titleText.fontSize = 32;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.9f, 0.4f);
            
            // Streak text
            GameObject streak = new GameObject("StreakText");
            streak.transform.SetParent(rewardsPanel.transform, false);
            RectTransform streakRect = streak.AddComponent<RectTransform>();
            streakRect.anchorMin = new Vector2(0, 0.75f);
            streakRect.anchorMax = new Vector2(1, 0.85f);
            streakRect.offsetMin = Vector2.zero;
            streakRect.offsetMax = Vector2.zero;
            streakText = streak.AddComponent<TextMeshProUGUI>();
            streakText.fontSize = 24;
            streakText.alignment = TextAlignmentOptions.Center;
            streakText.color = Color.white;
            
            // Rewards container
            GameObject container = new GameObject("RewardsContainer");
            container.transform.SetParent(rewardsPanel.transform, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.05f, 0.25f);
            containerRect.anchorMax = new Vector2(0.95f, 0.7f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            
            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            
            rewardsContainer = container.transform;
            
            // Claim button
            GameObject claimBtn = new GameObject("ClaimButton");
            claimBtn.transform.SetParent(rewardsPanel.transform, false);
            RectTransform claimRect = claimBtn.AddComponent<RectTransform>();
            claimRect.anchorMin = new Vector2(0.3f, 0.08f);
            claimRect.anchorMax = new Vector2(0.7f, 0.18f);
            claimRect.offsetMin = Vector2.zero;
            claimRect.offsetMax = Vector2.zero;
            
            Image claimBg = claimBtn.AddComponent<Image>();
            claimBg.color = new Color(0.2f, 0.6f, 0.2f);
            claimButton = claimBtn.AddComponent<Button>();
            claimButton.targetGraphic = claimBg;
            claimButton.onClick.AddListener(ClaimReward);
            
            GameObject claimText = new GameObject("Text");
            claimText.transform.SetParent(claimBtn.transform, false);
            RectTransform claimTextRect = claimText.AddComponent<RectTransform>();
            claimTextRect.anchorMin = Vector2.zero;
            claimTextRect.anchorMax = Vector2.one;
            claimTextRect.offsetMin = Vector2.zero;
            claimTextRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI claimTmp = claimText.AddComponent<TextMeshProUGUI>();
            claimTmp.text = "CLAIM!";
            claimTmp.fontSize = 24;
            claimTmp.fontStyle = FontStyles.Bold;
            claimTmp.alignment = TextAlignmentOptions.Center;
            claimTmp.color = Color.white;
            
            // Close button
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(rewardsPanel.transform, false);
            RectTransform closeRect = closeBtn.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.9f, 0.9f);
            closeRect.anchorMax = new Vector2(0.98f, 0.98f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
            
            Image closeBg = closeBtn.AddComponent<Image>();
            closeBg.color = new Color(0.6f, 0.2f, 0.2f);
            closeButton = closeBtn.AddComponent<Button>();
            closeButton.targetGraphic = closeBg;
            closeButton.onClick.AddListener(Hide);
            
            GameObject closeText = new GameObject("X");
            closeText.transform.SetParent(closeBtn.transform, false);
            RectTransform closeTextRect = closeText.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI closeTmp = closeText.AddComponent<TextMeshProUGUI>();
            closeTmp.text = "X";
            closeTmp.fontSize = 20;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color = Color.white;
            
            UpdateUI();
        }

        private void ShowClaimCelebration(DailyReward reward)
        {
            // TODO: Particle effects, sound, screen flash
            Debug.Log($"[DailyRewards] 🎉 CELEBRATION! {reward.Description}");
        }

        /// <summary>
        /// Check if player has unclaimed daily reward
        /// </summary>
        public bool HasUnclaimedReward()
        {
            CheckDailyReset();
            return _canClaimToday;
        }
    }

    [Serializable]
    public class DailyReward
    {
        public int Day;
        public int Gold;
        public int Stone;
        public int Wood;
        public int Iron;
        public int Crystal;
        public string Description;
        public bool IsPremium;
    }
}
