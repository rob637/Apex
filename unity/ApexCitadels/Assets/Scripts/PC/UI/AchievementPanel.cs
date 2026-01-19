using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.UI;
using ApexCitadels.Core;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Achievement Panel - Long-term goals and recognition system.
    /// Tracks lifetime accomplishments for bragging rights.
    /// </summary>
    public class AchievementPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
        [SerializeField] private Color silverColor = new Color(0.75f, 0.75f, 0.8f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color platinumColor = new Color(0.9f, 0.9f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.35f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _achievementContainer;
        private TextMeshProUGUI _summaryText;
        private Dictionary<AchievementCategory, GameObject> _categoryTabs = new Dictionary<AchievementCategory, GameObject>();
        private AchievementCategory _selectedCategory = AchievementCategory.Combat;
        
        // Achievement data
        private List<Achievement> _achievements = new List<Achievement>();
        private int _totalPoints;
        private int _unlockedCount;
        
        public static AchievementPanel Instance { get; private set; }
        
        // Events
        public event Action<Achievement> OnAchievementUnlocked;

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
            GenerateAchievements();
            CreateAchievementPanel();
            Hide();
        }

        private void GenerateAchievements()
        {
            // Combat Achievements
            _achievements.AddRange(new[]
            {
                new Achievement { Id = "first_blood", Title = "First Blood", Description = "Win your first battle", Category = AchievementCategory.Combat, Tier = AchievementTier.Bronze, Points = 10, TargetAmount = 1, Icon = GameIcons.Battle, IsUnlocked = true },
                new Achievement { Id = "warrior", Title = "Warrior", Description = "Win 10 battles", Category = AchievementCategory.Combat, Tier = AchievementTier.Bronze, Points = 25, TargetAmount = 10, CurrentAmount = 5, Icon = GameIcons.Sword },
                new Achievement { Id = "conqueror", Title = "Conqueror", Description = "Win 100 battles", Category = AchievementCategory.Combat, Tier = AchievementTier.Silver, Points = 50, TargetAmount = 100, CurrentAmount = 5, Icon = GameIcons.Trophy },
                new Achievement { Id = "warlord", Title = "Warlord", Description = "Win 500 battles", Category = AchievementCategory.Combat, Tier = AchievementTier.Gold, Points = 100, TargetAmount = 500, CurrentAmount = 5, Icon = GameIcons.Crown },
                new Achievement { Id = "legend", Title = "Legend of War", Description = "Win 1000 battles", Category = AchievementCategory.Combat, Tier = AchievementTier.Platinum, Points = 200, TargetAmount = 1000, CurrentAmount = 5, Icon = GameIcons.Star },
                new Achievement { Id = "perfect_victory", Title = "Perfect Victory", Description = "Win a battle with no losses", Category = AchievementCategory.Combat, Tier = AchievementTier.Silver, Points = 50, TargetAmount = 1, Icon = GameIcons.Victory },
                new Achievement { Id = "underdog", Title = "Underdog", Description = "Win against a higher level player", Category = AchievementCategory.Combat, Tier = AchievementTier.Silver, Points = 50, TargetAmount = 1, Icon = GameIcons.Shield }
            });
            
            // Territory Achievements
            _achievements.AddRange(new[]
            {
                new Achievement { Id = "landowner", Title = "Landowner", Description = "Claim your first territory", Category = AchievementCategory.Territory, Tier = AchievementTier.Bronze, Points = 10, TargetAmount = 1, IsUnlocked = true, Icon = GameIcons.Home },
                new Achievement { Id = "landlord", Title = "Landlord", Description = "Control 5 territories", Category = AchievementCategory.Territory, Tier = AchievementTier.Bronze, Points = 25, TargetAmount = 5, CurrentAmount = 2, Icon = GameIcons.Territory },
                new Achievement { Id = "baron", Title = "Baron", Description = "Control 25 territories", Category = AchievementCategory.Territory, Tier = AchievementTier.Silver, Points = 75, TargetAmount = 25, CurrentAmount = 2, Icon = GameIcons.Citadel },
                new Achievement { Id = "duke", Title = "Duke", Description = "Control 100 territories", Category = AchievementCategory.Territory, Tier = AchievementTier.Gold, Points = 150, TargetAmount = 100, CurrentAmount = 2, Icon = GameIcons.Crown },
                new Achievement { Id = "emperor", Title = "Emperor", Description = "Control 500 territories", Category = AchievementCategory.Territory, Tier = AchievementTier.Platinum, Points = 300, TargetAmount = 500, CurrentAmount = 2, Icon = GameIcons.Globe },
                new Achievement { Id = "explorer", Title = "Explorer", Description = "Visit 50 unique real-world locations", Category = AchievementCategory.Territory, Tier = AchievementTier.Silver, Points = 50, TargetAmount = 50, CurrentAmount = 10, Icon = GameIcons.Map }
            });
            
            // Building Achievements
            _achievements.AddRange(new[]
            {
                new Achievement { Id = "builder", Title = "Builder", Description = "Place 10 buildings", Category = AchievementCategory.Building, Tier = AchievementTier.Bronze, Points = 15, TargetAmount = 10, CurrentAmount = 3, Icon = GameIcons.Hammer },
                new Achievement { Id = "architect", Title = "Architect", Description = "Place 100 buildings", Category = AchievementCategory.Building, Tier = AchievementTier.Silver, Points = 50, TargetAmount = 100, CurrentAmount = 3, Icon = GameIcons.Build },
                new Achievement { Id = "master_builder", Title = "Master Builder", Description = "Place 1000 buildings", Category = AchievementCategory.Building, Tier = AchievementTier.Gold, Points = 100, TargetAmount = 1000, CurrentAmount = 3, Icon = GameIcons.Build },
                new Achievement { Id = "max_citadel", Title = "Citadel Supreme", Description = "Upgrade a citadel to max level", Category = AchievementCategory.Building, Tier = AchievementTier.Gold, Points = 100, TargetAmount = 1, Icon = GameIcons.Citadel },
                new Achievement { Id = "decorator", Title = "Decorator", Description = "Place 50 decorative items", Category = AchievementCategory.Building, Tier = AchievementTier.Bronze, Points = 20, TargetAmount = 50, CurrentAmount = 5, Icon = GameIcons.Paint }
            });
            
            // Social Achievements
            _achievements.AddRange(new[]
            {
                new Achievement { Id = "friendly", Title = "Friendly", Description = "Join an alliance", Category = AchievementCategory.Social, Tier = AchievementTier.Bronze, Points = 10, TargetAmount = 1, Icon = GameIcons.Alliance },
                new Achievement { Id = "leader", Title = "Leader", Description = "Become alliance leader", Category = AchievementCategory.Social, Tier = AchievementTier.Silver, Points = 50, TargetAmount = 1, Icon = GameIcons.Crown },
                new Achievement { Id = "diplomat", Title = "Diplomat", Description = "Form 5 alliances", Category = AchievementCategory.Social, Tier = AchievementTier.Gold, Points = 75, TargetAmount = 5, Icon = GameIcons.Peace },
                new Achievement { Id = "chatty", Title = "Chatty", Description = "Send 100 chat messages", Category = AchievementCategory.Social, Tier = AchievementTier.Bronze, Points = 15, TargetAmount = 100, CurrentAmount = 25, Icon = GameIcons.Chat },
                new Achievement { Id = "helpful", Title = "Helpful", Description = "Donate 10000 resources to alliance", Category = AchievementCategory.Social, Tier = AchievementTier.Silver, Points = 50, TargetAmount = 10000, CurrentAmount = 500, Icon = GameIcons.Gift }
            });
            
            // Collection Achievements
            _achievements.AddRange(new[]
            {
                new Achievement { Id = "hoarder", Title = "Hoarder", Description = "Collect 100,000 total resources", Category = AchievementCategory.Collection, Tier = AchievementTier.Bronze, Points = 20, TargetAmount = 100000, CurrentAmount = 15000, Icon = GameIcons.Gold },
                new Achievement { Id = "millionaire", Title = "Millionaire", Description = "Collect 1,000,000 gold", Category = AchievementCategory.Collection, Tier = AchievementTier.Gold, Points = 100, TargetAmount = 1000000, CurrentAmount = 50000, Icon = GameIcons.Gem },
                new Achievement { Id = "army_builder", Title = "Army Builder", Description = "Train 1000 troops", Category = AchievementCategory.Collection, Tier = AchievementTier.Silver, Points = 50, TargetAmount = 1000, CurrentAmount = 190, Icon = GameIcons.Medal },
                new Achievement { Id = "season_veteran", Title = "Season Veteran", Description = "Complete 5 season passes", Category = AchievementCategory.Collection, Tier = AchievementTier.Platinum, Points = 200, TargetAmount = 5, Icon = GameIcons.Medal },
                new Achievement { Id = "daily_devotee", Title = "Daily Devotee", Description = "Complete 100 daily quests", Category = AchievementCategory.Collection, Tier = AchievementTier.Silver, Points = 50, TargetAmount = 100, CurrentAmount = 15, Icon = GameIcons.Calendar }
            });
            
            // Calculate totals
            foreach (var ach in _achievements)
            {
                if (ach.IsUnlocked)
                {
                    _totalPoints += ach.Points;
                    _unlockedCount++;
                }
            }
        }

        private void CreateAchievementPanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("AchievementPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.1f);
            rect.anchorMax = new Vector2(0.9f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.98f);
            
            VerticalLayoutGroup vlayout = _panel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            // Header
            CreateHeader();
            
            // Summary
            CreateSummary();
            
            // Category tabs
            CreateCategoryTabs();
            
            // Achievement list
            CreateAchievementList();
        }

        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            
            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(header.transform, false);
            
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = $"{GameIcons.Trophy} ACHIEVEMENTS";
            title.fontSize = 28;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = goldColor;
            
            // Close button
            GameObject closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(header.transform, false);
            
            LayoutElement closeLe = closeBtn.AddComponent<LayoutElement>();
            closeLe.preferredWidth = 40;
            closeLe.preferredHeight = 40;
            
            Image closeBg = closeBtn.AddComponent<Image>();
            closeBg.color = new Color(0.6f, 0.2f, 0.2f, 0.8f);
            
            Button btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(Hide);
            
            GameObject x = new GameObject("X");
            x.transform.SetParent(closeBtn.transform, false);
            
            TextMeshProUGUI xText = x.AddComponent<TextMeshProUGUI>();
            xText.text = "✕";
            xText.fontSize = 24;
            xText.alignment = TextAlignmentOptions.Center;
            
            RectTransform xRect = x.GetComponent<RectTransform>();
            xRect.anchorMin = Vector2.zero;
            xRect.anchorMax = Vector2.one;
        }

        private void CreateSummary()
        {
            GameObject summary = new GameObject("Summary");
            summary.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = summary.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            Image bg = summary.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.1f, 0.05f, 0.8f);
            
            HorizontalLayoutGroup hl = summary.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.padding = new RectOffset(20, 20, 5, 5);
            
            _summaryText = CreateSummaryItem(summary.transform);
            UpdateSummary();
        }

        private TextMeshProUGUI CreateSummaryItem(Transform parent)
        {
            GameObject obj = new GameObject("SummaryText");
            obj.transform.SetParent(parent, false);
            
            TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.Center;
            
            return text;
        }

        private void UpdateSummary()
        {
            _summaryText.text = $"<color=#FFD700>{GameIcons.Trophy} {_totalPoints} Points</color>  |  " +
                $"<color=#88FF88>{GameIcons.Success} {_unlockedCount}/{_achievements.Count} Unlocked</color>  |  " +
                $"<color=#CD7F32>{GameIcons.BronzeMedal} Bronze: {CountTier(AchievementTier.Bronze)}</color>  " +
                $"<color=#C0C0C0>{GameIcons.SilverMedal} Silver: {CountTier(AchievementTier.Silver)}</color>  " +
                $"<color=#FFD700>{GameIcons.GoldMedal} Gold: {CountTier(AchievementTier.Gold)}</color>  " +
                $"<color=#E5E4E2>{GameIcons.Gems} Platinum: {CountTier(AchievementTier.Platinum)}</color>";
        }

        private int CountTier(AchievementTier tier)
        {
            return _achievements.FindAll(a => a.IsUnlocked && a.Tier == tier).Count;
        }

        private void CreateCategoryTabs()
        {
            GameObject tabs = new GameObject("Tabs");
            tabs.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = tabs.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            HorizontalLayoutGroup hlayout = tabs.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 5;
            hlayout.padding = new RectOffset(20, 20, 0, 0);
            
            CreateCategoryTab(tabs.transform, AchievementCategory.Combat, $"{GameIcons.Battle} Combat");
            CreateCategoryTab(tabs.transform, AchievementCategory.Territory, $"{GameIcons.Map} Territory");
            CreateCategoryTab(tabs.transform, AchievementCategory.Building, $"{GameIcons.Build} Building");
            CreateCategoryTab(tabs.transform, AchievementCategory.Social, $"{GameIcons.Alliance} Social");
            CreateCategoryTab(tabs.transform, AchievementCategory.Collection, $"{GameIcons.Chest} Collection");
        }

        private void CreateCategoryTab(Transform parent, AchievementCategory category, string label)
        {
            GameObject tab = new GameObject($"Tab_{category}");
            tab.transform.SetParent(parent, false);
            
            LayoutElement le = tab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 35;
            
            Image bg = tab.AddComponent<Image>();
            bg.color = category == _selectedCategory ? GetTierColor(AchievementTier.Gold) : new Color(0.15f, 0.15f, 0.2f);
            
            Button btn = tab.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectCategory(category));
            
            GameObject txt = new GameObject("Label");
            txt.transform.SetParent(tab.transform, false);
            
            TextMeshProUGUI text = txt.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 13;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            
            RectTransform txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            
            _categoryTabs[category] = tab;
        }

        private void CreateAchievementList()
        {
            // Scroll view
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = scrollView.AddComponent<LayoutElement>();
            le.flexibleHeight = 1;
            
            Image bgImg = scrollView.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.5f);
            
            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            
            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;
            
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>();
            
            scroll.viewport = vpRect;
            
            // Content
            _achievementContainer = new GameObject("Content");
            _achievementContainer.transform.SetParent(viewport.transform, false);
            
            RectTransform contentRect = _achievementContainer.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            
            ContentSizeFitter fitter = _achievementContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            VerticalLayoutGroup contentVL = _achievementContainer.AddComponent<VerticalLayoutGroup>();
            contentVL.childAlignment = TextAnchor.UpperCenter;
            contentVL.childForceExpandWidth = true;
            contentVL.childForceExpandHeight = false;
            contentVL.spacing = 8;
            contentVL.padding = new RectOffset(10, 10, 10, 10);
            
            scroll.content = contentRect;
            
            RefreshAchievementList();
        }

        private void RefreshAchievementList()
        {
            // Clear existing
            foreach (Transform child in _achievementContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Get achievements for selected category
            List<Achievement> categoryAchs = _achievements.FindAll(a => a.Category == _selectedCategory);
            
            // Sort: unlocked first, then by tier
            categoryAchs.Sort((a, b) =>
            {
                if (a.IsUnlocked != b.IsUnlocked) return b.IsUnlocked.CompareTo(a.IsUnlocked);
                return a.Tier.CompareTo(b.Tier);
            });
            
            // Create achievement cards
            foreach (Achievement ach in categoryAchs)
            {
                CreateAchievementCard(ach);
            }
        }

        private void CreateAchievementCard(Achievement ach)
        {
            GameObject card = new GameObject($"Ach_{ach.Id}");
            card.transform.SetParent(_achievementContainer.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            Color tierColor = ach.IsUnlocked ? GetTierColor(ach.Tier) : lockedColor;
            
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(tierColor.r * 0.25f, tierColor.g * 0.25f, tierColor.b * 0.25f, 0.9f);
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Left: Icon + Tier badge
            CreateAchievementIcon(card.transform, ach);
            
            // Center: Info
            CreateAchievementInfo(card.transform, ach);
            
            // Right: Points + Progress
            CreateAchievementProgress(card.transform, ach);
        }

        private void CreateAchievementIcon(Transform parent, Achievement ach)
        {
            GameObject iconContainer = new GameObject("IconContainer");
            iconContainer.transform.SetParent(parent, false);
            
            LayoutElement le = iconContainer.AddComponent<LayoutElement>();
            le.preferredWidth = 60;
            
            VerticalLayoutGroup vl = iconContainer.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.MiddleCenter;
            
            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(iconContainer.transform, false);
            
            TextMeshProUGUI icon = iconObj.AddComponent<TextMeshProUGUI>();
            icon.text = ach.IsUnlocked ? ach.Icon : "🔒";
            icon.fontSize = 32;
            icon.alignment = TextAlignmentOptions.Center;
            icon.color = ach.IsUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredHeight = 40;
            
            // Tier badge
            GameObject tierObj = new GameObject("Tier");
            tierObj.transform.SetParent(iconContainer.transform, false);
            
            TextMeshProUGUI tier = tierObj.AddComponent<TextMeshProUGUI>();
            tier.text = GetTierIcon(ach.Tier);
            tier.fontSize = 16;
            tier.alignment = TextAlignmentOptions.Center;
            tier.color = ach.IsUnlocked ? GetTierColor(ach.Tier) : lockedColor;
            
            LayoutElement tierLE = tierObj.AddComponent<LayoutElement>();
            tierLE.preferredHeight = 20;
        }

        private void CreateAchievementInfo(Transform parent, Achievement ach)
        {
            GameObject infoObj = new GameObject("Info");
            infoObj.transform.SetParent(parent, false);
            
            LayoutElement le = infoObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            VerticalLayoutGroup vlayout = infoObj.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleLeft;
            vlayout.spacing = 3;
            
            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(infoObj.transform, false);
            
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = ach.Title;
            title.fontSize = 16;
            title.fontStyle = FontStyles.Bold;
            title.color = ach.IsUnlocked ? GetTierColor(ach.Tier) : new Color(0.5f, 0.5f, 0.5f);
            
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 22;
            
            // Description
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(infoObj.transform, false);
            
            TextMeshProUGUI desc = descObj.AddComponent<TextMeshProUGUI>();
            desc.text = ach.Description;
            desc.fontSize = 12;
            desc.color = new Color(0.7f, 0.7f, 0.7f);
            
            LayoutElement descLE = descObj.AddComponent<LayoutElement>();
            descLE.preferredHeight = 18;
            
            // Unlocked date or progress
            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(infoObj.transform, false);
            
            TextMeshProUGUI status = statusObj.AddComponent<TextMeshProUGUI>();
            if (ach.IsUnlocked)
            {
                status.text = $"{GameIcons.Success} Unlocked!";
                status.color = new Color(0.3f, 0.8f, 0.3f);
            }
            else
            {
                float progress = ach.TargetAmount > 0 ? (float)ach.CurrentAmount / ach.TargetAmount * 100 : 0;
                status.text = $"Progress: {ach.CurrentAmount}/{ach.TargetAmount} ({progress:F0}%)";
                status.color = new Color(0.5f, 0.5f, 0.5f);
            }
            status.fontSize = 11;
            
            LayoutElement statusLE = statusObj.AddComponent<LayoutElement>();
            statusLE.preferredHeight = 16;
        }

        private void CreateAchievementProgress(Transform parent, Achievement ach)
        {
            GameObject progressObj = new GameObject("Progress");
            progressObj.transform.SetParent(parent, false);
            
            LayoutElement le = progressObj.AddComponent<LayoutElement>();
            le.preferredWidth = 80;
            
            VerticalLayoutGroup vl = progressObj.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.MiddleCenter;
            vl.spacing = 5;
            
            // Points
            GameObject pointsObj = new GameObject("Points");
            pointsObj.transform.SetParent(progressObj.transform, false);
            
            TextMeshProUGUI points = pointsObj.AddComponent<TextMeshProUGUI>();
            points.text = $"+{ach.Points}";
            points.fontSize = 20;
            points.fontStyle = FontStyles.Bold;
            points.alignment = TextAlignmentOptions.Center;
            points.color = ach.IsUnlocked ? goldColor : lockedColor;
            
            LayoutElement pointsLE = pointsObj.AddComponent<LayoutElement>();
            pointsLE.preferredHeight = 28;
            
            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(progressObj.transform, false);
            
            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "points";
            label.fontSize = 10;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.5f, 0.5f, 0.5f);
            
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.preferredHeight = 14;
            
            // Progress bar (if not unlocked)
            if (!ach.IsUnlocked && ach.TargetAmount > 0)
            {
                GameObject barBg = new GameObject("BarBg");
                barBg.transform.SetParent(progressObj.transform, false);
                
                LayoutElement barLE = barBg.AddComponent<LayoutElement>();
                barLE.preferredHeight = 8;
                
                Image bgImg = barBg.AddComponent<Image>();
                bgImg.color = new Color(0.2f, 0.2f, 0.2f);
                
                GameObject fill = new GameObject("Fill");
                fill.transform.SetParent(barBg.transform, false);
                
                RectTransform fillRect = fill.AddComponent<RectTransform>();
                float progress = (float)ach.CurrentAmount / ach.TargetAmount;
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = new Vector2(progress, 1f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                
                Image fillImg = fill.AddComponent<Image>();
                fillImg.color = GetTierColor(ach.Tier) * 0.7f;
            }
        }

        private Color GetTierColor(AchievementTier tier)
        {
            return tier switch
            {
                AchievementTier.Bronze => bronzeColor,
                AchievementTier.Silver => silverColor,
                AchievementTier.Gold => goldColor,
                AchievementTier.Platinum => platinumColor,
                _ => bronzeColor
            };
        }

        private string GetTierIcon(AchievementTier tier)
        {
            return tier switch
            {
                AchievementTier.Bronze => GameIcons.BronzeMedal,
                AchievementTier.Silver => GameIcons.SilverMedal,
                AchievementTier.Gold => GameIcons.GoldMedal,
                AchievementTier.Platinum => GameIcons.Gem,
                _ => GameIcons.BronzeMedal
            };
        }

        private void SelectCategory(AchievementCategory category)
        {
            _selectedCategory = category;
            
            // Update tab colors
            foreach (var kvp in _categoryTabs)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                bg.color = kvp.Key == category ? GetTierColor(AchievementTier.Gold) : new Color(0.15f, 0.15f, 0.2f);
            }
            
            RefreshAchievementList();
        }

        #region Progress Tracking

        public void UpdateAchievementProgress(string achievementId, int amount)
        {
            Achievement ach = _achievements.Find(a => a.Id == achievementId);
            if (ach == null || ach.IsUnlocked) return;
            
            ach.CurrentAmount += amount;
            if (ach.CurrentAmount >= ach.TargetAmount)
            {
                UnlockAchievement(ach);
            }
            
            if (_panel.activeSelf)
            {
                RefreshAchievementList();
            }
        }

        private void UnlockAchievement(Achievement ach)
        {
            ach.IsUnlocked = true;
            ach.CurrentAmount = ach.TargetAmount;
            _totalPoints += ach.Points;
            _unlockedCount++;
            
            ApexLogger.Log($"[Achievement] 🏆 UNLOCKED: {ach.Title} (+{ach.Points} points)", ApexLogger.LogCategory.UI);
            
            OnAchievementUnlocked?.Invoke(ach);
            UpdateSummary();
        }

        #endregion

        #region Public API

        public void Show()
        {
            _panel.SetActive(true);
            RefreshAchievementList();
            UpdateSummary();
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        public void Toggle()
        {
            if (_panel.activeSelf)
                Hide();
            else
                Show();
        }

        public int GetTotalPoints()
        {
            return _totalPoints;
        }

        public int GetUnlockedCount()
        {
            return _unlockedCount;
        }

        #endregion
    }

    public enum AchievementCategory
    {
        Combat,
        Territory,
        Building,
        Social,
        Collection
    }

    public enum AchievementTier
    {
        Bronze,
        Silver,
        Gold,
        Platinum
    }

    public class Achievement
    {
        public string Id;
        public string Title;
        public string Description;
        public string Icon;
        public AchievementCategory Category;
        public AchievementTier Tier;
        public int Points;
        public int TargetAmount;
        public int CurrentAmount;
        public bool IsUnlocked;
    }
}
