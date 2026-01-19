using System;
using ApexCitadels.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.UI;
using ApexCitadels.Core;
using UIOutline = UnityEngine.UI.Outline;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Season/Battle Pass UI showing 100-tier progression with free and premium tracks.
    /// Core monetization and engagement loop.
    /// </summary>
    public class SeasonPassPanel : MonoBehaviour
    {
        [Header("Season Settings")]
        [SerializeField] private string seasonName = "Season 1: Rise of Citadels";
        [SerializeField] private int totalTiers = 100;
        [SerializeField] private int xpPerTier = 1000;
        
        [Header("Visuals")]
        [SerializeField] private Color freeTrackColor = new Color(0.4f, 0.6f, 0.4f);
        [SerializeField] private Color premiumTrackColor = new Color(0.9f, 0.7f, 0.2f);
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color claimedColor = new Color(0.2f, 0.8f, 0.2f);
        
        // State
        private int _currentTier = 1;
        private int _currentXP = 0;
        private bool _hasPremium = false;
        private HashSet<int> _claimedFree = new HashSet<int>();
        private HashSet<int> _claimedPremium = new HashSet<int>();
        
        // UI
        private GameObject _panel;
        private Transform _tiersContainer;
        private TextMeshProUGUI _tierText;
        private TextMeshProUGUI _xpText;
        private Image _xpBar;
        private ScrollRect _scrollRect;
        
        // Rewards data
        private List<SeasonReward> _freeRewards = new List<SeasonReward>();
        private List<SeasonReward> _premiumRewards = new List<SeasonReward>();
        
        public static SeasonPassPanel Instance { get; private set; }
        
        public event Action<SeasonReward> OnRewardClaimed;
        public event Action<int> OnTierUp;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            GenerateRewardsData();
            LoadProgress();
            CreateSeasonPassUI();
            RefreshDisplay();
        }

        /// <summary>
        /// Add XP to the season pass
        /// </summary>
        public void AddXP(int amount)
        {
            _currentXP += amount;
            
            // Check for tier ups
            while (_currentXP >= xpPerTier && _currentTier < totalTiers)
            {
                _currentXP -= xpPerTier;
                _currentTier++;
                OnTierUp?.Invoke(_currentTier);
                ApexLogger.Log(ApexLogger.LogCategory.UI, $"[SeasonPass] Tier up! Now tier {_currentTier}");
            }
            
            SaveProgress();
            RefreshDisplay();
        }

        /// <summary>
        /// Claim a reward at the specified tier
        /// </summary>
        public bool ClaimReward(int tier, bool premium)
        {
            if (tier > _currentTier) return false;
            if (premium && !_hasPremium) return false;
            
            var claimedSet = premium ? _claimedPremium : _claimedFree;
            if (claimedSet.Contains(tier)) return false;
            
            claimedSet.Add(tier);
            
            var rewards = premium ? _premiumRewards : _freeRewards;
            var reward = rewards.Find(r => r.Tier == tier);
            if (reward != null)
            {
                OnRewardClaimed?.Invoke(reward);
                GrantReward(reward);
            }
            
            SaveProgress();
            RefreshDisplay();
            return true;
        }

        /// <summary>
        /// Upgrade to premium battle pass
        /// </summary>
        public void UpgradeToPremium()
        {
            _hasPremium = true;
            SaveProgress();
            RefreshDisplay();
            ApexLogger.Log(ApexLogger.LogCategory.UI, "[SeasonPass] Upgraded to Premium!");
        }

        public void Show()
        {
            if (_panel != null)
            {
                _panel.SetActive(true);
                ScrollToCurrentTier();
            }
        }

        public void Hide()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        public void Toggle()
        {
            if (_panel != null && _panel.activeSelf)
                Hide();
            else
                Show();
        }

        private void CreateSeasonPassUI()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel (nearly full screen)
            _panel = new GameObject("SeasonPassPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.1f);
            rect.anchorMax = new Vector2(0.95f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Background
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.98f);
            
            // Header
            CreateHeader();
            
            // Progress bar
            CreateProgressBar();
            
            // Tier display
            CreateTierDisplay();
            
            // Premium upsell
            CreatePremiumButton();
            
            // Close button
            CreateCloseButton();
            
            // Initially hidden
            _panel.SetActive(false);
        }

        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_panel.transform, false);
            
            RectTransform rect = header.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.92f);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(20, 0);
            rect.offsetMax = new Vector2(-50, -10);
            
            // Season name
            TextMeshProUGUI title = header.AddComponent<TextMeshProUGUI>();
            title.text = $"🎖️ {seasonName}";
            title.fontSize = 28;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Left;
            title.color = premiumTrackColor;
            
            // Days remaining
            GameObject daysObj = new GameObject("DaysRemaining");
            daysObj.transform.SetParent(header.transform, false);
            
            RectTransform daysRect = daysObj.AddComponent<RectTransform>();
            daysRect.anchorMin = new Vector2(0.7f, 0);
            daysRect.anchorMax = new Vector2(1, 1);
            daysRect.offsetMin = Vector2.zero;
            daysRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI daysText = daysObj.AddComponent<TextMeshProUGUI>();
            daysText.text = "⏱️ 42 days remaining";
            daysText.fontSize = 16;
            daysText.alignment = TextAlignmentOptions.Right;
            daysText.color = new Color(0.7f, 0.7f, 0.7f);
        }

        private void CreateProgressBar()
        {
            GameObject progressArea = new GameObject("ProgressArea");
            progressArea.transform.SetParent(_panel.transform, false);
            
            RectTransform rect = progressArea.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.85f);
            rect.anchorMax = new Vector2(1, 0.92f);
            rect.offsetMin = new Vector2(20, 0);
            rect.offsetMax = new Vector2(-20, 0);
            
            // Tier text
            GameObject tierObj = new GameObject("TierText");
            tierObj.transform.SetParent(progressArea.transform, false);
            
            RectTransform tierRect = tierObj.AddComponent<RectTransform>();
            tierRect.anchorMin = new Vector2(0, 0);
            tierRect.anchorMax = new Vector2(0.15f, 1);
            tierRect.offsetMin = Vector2.zero;
            tierRect.offsetMax = Vector2.zero;
            
            _tierText = tierObj.AddComponent<TextMeshProUGUI>();
            _tierText.text = $"TIER {_currentTier}";
            _tierText.fontSize = 24;
            _tierText.fontStyle = FontStyles.Bold;
            _tierText.alignment = TextAlignmentOptions.Left;
            _tierText.color = Color.white;
            
            // XP Bar background
            GameObject barBg = new GameObject("XPBarBg");
            barBg.transform.SetParent(progressArea.transform, false);
            
            RectTransform barBgRect = barBg.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.16f, 0.3f);
            barBgRect.anchorMax = new Vector2(0.85f, 0.7f);
            barBgRect.offsetMin = Vector2.zero;
            barBgRect.offsetMax = Vector2.zero;
            
            Image barBgImg = barBg.AddComponent<Image>();
            barBgImg.color = new Color(0.2f, 0.2f, 0.2f);
            
            // XP Bar fill
            GameObject barFill = new GameObject("XPBarFill");
            barFill.transform.SetParent(barBg.transform, false);
            
            RectTransform barFillRect = barFill.AddComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = new Vector2(0.5f, 1);
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;
            
            _xpBar = barFill.AddComponent<Image>();
            _xpBar.color = premiumTrackColor;
            
            // XP text
            GameObject xpObj = new GameObject("XPText");
            xpObj.transform.SetParent(progressArea.transform, false);
            
            RectTransform xpRect = xpObj.AddComponent<RectTransform>();
            xpRect.anchorMin = new Vector2(0.86f, 0);
            xpRect.anchorMax = new Vector2(1, 1);
            xpRect.offsetMin = Vector2.zero;
            xpRect.offsetMax = Vector2.zero;
            
            _xpText = xpObj.AddComponent<TextMeshProUGUI>();
            _xpText.text = $"{_currentXP}/{xpPerTier} XP";
            _xpText.fontSize = 14;
            _xpText.alignment = TextAlignmentOptions.Right;
            _xpText.color = new Color(0.8f, 0.8f, 0.8f);
        }

        private void CreateTierDisplay()
        {
            // Scroll view for tiers
            GameObject scrollView = new GameObject("TierScrollView");
            scrollView.transform.SetParent(_panel.transform, false);
            
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0.12f);
            scrollRect.anchorMax = new Vector2(1, 0.83f);
            scrollRect.offsetMin = new Vector2(10, 0);
            scrollRect.offsetMax = new Vector2(-10, 0);
            
            _scrollRect = scrollView.AddComponent<ScrollRect>();
            _scrollRect.horizontal = true;
            _scrollRect.vertical = false;
            _scrollRect.movementType = ScrollRect.MovementType.Elastic;
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            Image viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = Color.white;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            
            _scrollRect.viewport = viewportRect;
            
            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(0, 1);
            contentRect.pivot = new Vector2(0, 0.5f);
            contentRect.sizeDelta = new Vector2(totalTiers * 120 + 50, 0);
            
            HorizontalLayoutGroup layout = content.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.spacing = 10;
            layout.padding = new RectOffset(20, 20, 10, 10);
            
            _scrollRect.content = contentRect;
            _tiersContainer = content.transform;
            
            // Create tier slots
            CreateTierSlots();
        }

        private void CreateTierSlots()
        {
            for (int tier = 1; tier <= totalTiers; tier++)
            {
                CreateTierSlot(tier);
            }
        }

        private void CreateTierSlot(int tier)
        {
            GameObject slot = new GameObject($"Tier_{tier}");
            slot.transform.SetParent(_tiersContainer, false);
            
            RectTransform rect = slot.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(110, 0);
            
            VerticalLayoutGroup layout = slot.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 5, 5);
            
            // Tier number
            CreateTierNumber(slot.transform, tier);
            
            // Premium reward (top)
            CreateRewardSlot(slot.transform, tier, true);
            
            // Connection line
            CreateConnectionLine(slot.transform);
            
            // Free reward (bottom)
            CreateRewardSlot(slot.transform, tier, false);
        }

        private void CreateTierNumber(Transform parent, int tier)
        {
            GameObject numObj = new GameObject("TierNum");
            numObj.transform.SetParent(parent, false);
            
            LayoutElement le = numObj.AddComponent<LayoutElement>();
            le.preferredHeight = 25;
            
            TextMeshProUGUI text = numObj.AddComponent<TextMeshProUGUI>();
            text.text = tier.ToString();
            text.fontSize = 16;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = tier <= _currentTier ? premiumTrackColor : lockedColor;
        }

        private void CreateRewardSlot(Transform parent, int tier, bool isPremium)
        {
            var rewards = isPremium ? _premiumRewards : _freeRewards;
            var reward = rewards.Find(r => r.Tier == tier);
            var claimed = isPremium ? _claimedPremium : _claimedFree;
            
            GameObject slotObj = new GameObject(isPremium ? "PremiumReward" : "FreeReward");
            slotObj.transform.SetParent(parent, false);
            
            LayoutElement le = slotObj.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            // Background
            Image bg = slotObj.AddComponent<Image>();
            
            bool isLocked = tier > _currentTier || (isPremium && !_hasPremium);
            bool isClaimed = claimed.Contains(tier);
            
            if (isClaimed)
                bg.color = claimedColor;
            else if (isLocked)
                bg.color = lockedColor;
            else
                bg.color = isPremium ? premiumTrackColor : freeTrackColor;
            
            // Outline for premium
            if (isPremium)
            {
                UnityEngine.UI.Outline outline = slotObj.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = premiumTrackColor;
                outline.effectDistance = new Vector2(2, 2);
            }
            
            // Make clickable if claimable
            if (!isClaimed && !isLocked)
            {
                Button btn = slotObj.AddComponent<Button>();
                int t = tier;
                bool p = isPremium;
                btn.onClick.AddListener(() => ClaimReward(t, p));
            }
            
            // Reward icon/text
            if (reward != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(slotObj.transform, false);
                
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.1f, 0.3f);
                iconRect.anchorMax = new Vector2(0.9f, 0.9f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                
                TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
                iconText.text = reward.Icon;
                iconText.fontSize = 24;
                iconText.alignment = TextAlignmentOptions.Center;
                
                // Amount text
                GameObject amtObj = new GameObject("Amount");
                amtObj.transform.SetParent(slotObj.transform, false);
                
                RectTransform amtRect = amtObj.AddComponent<RectTransform>();
                amtRect.anchorMin = new Vector2(0, 0);
                amtRect.anchorMax = new Vector2(1, 0.35f);
                amtRect.offsetMin = Vector2.zero;
                amtRect.offsetMax = Vector2.zero;
                
                TextMeshProUGUI amtText = amtObj.AddComponent<TextMeshProUGUI>();
                amtText.text = reward.Amount > 1 ? $"x{reward.Amount}" : reward.Name;
                amtText.fontSize = 10;
                amtText.alignment = TextAlignmentOptions.Center;
                amtText.color = Color.white;
            }
            
            // Claimed checkmark
            if (isClaimed)
            {
                GameObject checkObj = new GameObject("Check");
                checkObj.transform.SetParent(slotObj.transform, false);
                
                RectTransform checkRect = checkObj.AddComponent<RectTransform>();
                checkRect.anchorMin = Vector2.zero;
                checkRect.anchorMax = Vector2.one;
                checkRect.offsetMin = Vector2.zero;
                checkRect.offsetMax = Vector2.zero;
                
                TextMeshProUGUI checkText = checkObj.AddComponent<TextMeshProUGUI>();
                checkText.text = "✓";
                checkText.fontSize = 36;
                checkText.alignment = TextAlignmentOptions.Center;
                checkText.color = Color.white;
            }
            
            // Lock icon
            if (isLocked && !isClaimed)
            {
                GameObject lockObj = new GameObject("Lock");
                lockObj.transform.SetParent(slotObj.transform, false);
                
                RectTransform lockRect = lockObj.AddComponent<RectTransform>();
                lockRect.anchorMin = new Vector2(0.7f, 0.65f);
                lockRect.anchorMax = new Vector2(1, 1);
                lockRect.offsetMin = Vector2.zero;
                lockRect.offsetMax = Vector2.zero;
                
                TextMeshProUGUI lockText = lockObj.AddComponent<TextMeshProUGUI>();
                lockText.text = isPremium && !_hasPremium ? "👑" : "🔒";
                lockText.fontSize = 14;
                lockText.alignment = TextAlignmentOptions.Center;
            }
        }

        private void CreateConnectionLine(Transform parent)
        {
            GameObject line = new GameObject("Line");
            line.transform.SetParent(parent, false);
            
            LayoutElement le = line.AddComponent<LayoutElement>();
            le.preferredHeight = 20;
            
            Image img = line.AddComponent<Image>();
            img.color = new Color(0.4f, 0.4f, 0.4f);
            
            RectTransform rect = line.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(4, 20);
        }

        private void CreatePremiumButton()
        {
            if (_hasPremium) return;
            
            GameObject btnObj = new GameObject("PremiumButton");
            btnObj.transform.SetParent(_panel.transform, false);
            
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.35f, 0.02f);
            rect.anchorMax = new Vector2(0.65f, 0.1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = premiumTrackColor;
            
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(UpgradeToPremium);
            
            // Gradient/shine effect via outline
            UnityEngine.UI.Outline outline = btnObj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(1, 1);
            
            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "👑 UPGRADE TO PREMIUM - $9.99";
            text.fontSize = 18;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.1f, 0.1f, 0.1f);
        }

        private void CreateCloseButton()
        {
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(_panel.transform, false);
            
            RectTransform rect = closeBtn.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-10, -10);
            rect.sizeDelta = new Vector2(35, 35);
            
            Image bg = closeBtn.AddComponent<Image>();
            bg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            
            Button btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(Hide);
            
            GameObject textObj = new GameObject("X");
            textObj.transform.SetParent(closeBtn.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "✕";
            text.fontSize = 22;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }

        private void RefreshDisplay()
        {
            if (_tierText != null)
                _tierText.text = $"TIER {_currentTier}";
            
            if (_xpText != null)
                _xpText.text = $"{_currentXP}/{xpPerTier} XP";
            
            if (_xpBar != null)
            {
                float progress = (float)_currentXP / xpPerTier;
                RectTransform rect = _xpBar.GetComponent<RectTransform>();
                rect.anchorMax = new Vector2(progress, 1);
            }
            
            // Rebuild tier slots to update states
            if (_tiersContainer != null)
            {
                foreach (Transform child in _tiersContainer)
                {
                    Destroy(child.gameObject);
                }
                CreateTierSlots();
            }
        }

        private void ScrollToCurrentTier()
        {
            if (_scrollRect == null) return;
            
            float progress = (float)_currentTier / totalTiers;
            _scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(progress - 0.1f);
        }

        private void GrantReward(SeasonReward reward)
        {
            ApexLogger.Log(ApexLogger.LogCategory.UI, $"[SeasonPass] Granted reward: {reward.Name} x{reward.Amount}");
            
            // Grant to resource system if available
            if (PCResourceSystem.Instance != null && reward.Type == RewardType.Resource)
            {
                // Parse resource type from name
                if (Enum.TryParse<ResourceType>(reward.Name, true, out var resType))
                {
                    PCResourceSystem.Instance.AddResource(resType, reward.Amount);
                }
            }
        }

        private void GenerateRewardsData()
        {
            _freeRewards.Clear();
            _premiumRewards.Clear();
            
            string[] freeIcons = { "💎", "⚡", "🛡️", "🗡️", "📦" };
            string[] premiumIcons = { "👑", "🌟", "🎨", "🐉", "🏆" };
            string[] freeNames = { "Gems", "Energy", "Shield", "Weapon", "Crate" };
            string[] premiumNames = { "Crown", "Star Skin", "Epic Decor", "Dragon Pet", "Legendary" };
            
            for (int tier = 1; tier <= totalTiers; tier++)
            {
                // Free track
                _freeRewards.Add(new SeasonReward
                {
                    Tier = tier,
                    Name = freeNames[tier % freeNames.Length],
                    Icon = freeIcons[tier % freeIcons.Length],
                    Amount = 50 + tier * 10,
                    Type = RewardType.Resource,
                    IsPremium = false
                });
                
                // Premium track (better rewards)
                _premiumRewards.Add(new SeasonReward
                {
                    Tier = tier,
                    Name = tier % 10 == 0 ? premiumNames[(tier / 10) % premiumNames.Length] : freeNames[tier % freeNames.Length],
                    Icon = tier % 10 == 0 ? premiumIcons[(tier / 10) % premiumIcons.Length] : freeIcons[tier % freeIcons.Length],
                    Amount = tier % 10 == 0 ? 1 : 100 + tier * 20,
                    Type = tier % 10 == 0 ? RewardType.Cosmetic : RewardType.Resource,
                    IsPremium = true
                });
            }
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt("SeasonPass_Tier", _currentTier);
            PlayerPrefs.SetInt("SeasonPass_XP", _currentXP);
            PlayerPrefs.SetInt("SeasonPass_Premium", _hasPremium ? 1 : 0);
            
            // Save claimed sets
            PlayerPrefs.SetString("SeasonPass_ClaimedFree", string.Join(",", _claimedFree));
            PlayerPrefs.SetString("SeasonPass_ClaimedPremium", string.Join(",", _claimedPremium));
            
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            _currentTier = PlayerPrefs.GetInt("SeasonPass_Tier", 1);
            _currentXP = PlayerPrefs.GetInt("SeasonPass_XP", 0);
            _hasPremium = PlayerPrefs.GetInt("SeasonPass_Premium", 0) == 1;
            
            // Load claimed sets
            string claimedFreeStr = PlayerPrefs.GetString("SeasonPass_ClaimedFree", "");
            if (!string.IsNullOrEmpty(claimedFreeStr))
            {
                foreach (var s in claimedFreeStr.Split(','))
                {
                    if (int.TryParse(s, out int tier))
                        _claimedFree.Add(tier);
                }
            }
            
            string claimedPremStr = PlayerPrefs.GetString("SeasonPass_ClaimedPremium", "");
            if (!string.IsNullOrEmpty(claimedPremStr))
            {
                foreach (var s in claimedPremStr.Split(','))
                {
                    if (int.TryParse(s, out int tier))
                        _claimedPremium.Add(tier);
                }
            }
            
            // Demo: start at tier 5 with some XP
            if (_currentTier == 1 && _currentXP == 0)
            {
                _currentTier = 5;
                _currentXP = 450;
            }
        }
    }

    public enum RewardType
    {
        Resource,
        Cosmetic,
        Booster,
        Character,
        Building
    }

    public class SeasonReward
    {
        public int Tier;
        public string Name;
        public string Icon;
        public int Amount;
        public RewardType Type;
        public bool IsPremium;
    }
}
