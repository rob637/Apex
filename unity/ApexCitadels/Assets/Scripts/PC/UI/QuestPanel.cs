using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Quest Panel - Daily, Weekly, and Story missions for continuous engagement.
    /// Critical for the "500+ hours" gameplay target.
    /// </summary>
    public class QuestPanel : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int dailyQuestCount = 5;
        [SerializeField] private int weeklyQuestCount = 3;
        
        [Header("Colors")]
        [SerializeField] private Color dailyColor = new Color(0.3f, 0.7f, 0.3f);
        [SerializeField] private Color weeklyColor = new Color(0.3f, 0.5f, 0.8f);
        [SerializeField] private Color storyColor = new Color(0.8f, 0.6f, 0.2f);
        [SerializeField] private Color completedColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color progressColor = new Color(0.2f, 0.8f, 0.3f);
        
        // UI Elements
        private GameObject _panel;
        private Dictionary<QuestCategory, GameObject> _categoryTabs = new Dictionary<QuestCategory, GameObject>();
        private GameObject _questListContainer;
        private QuestCategory _selectedCategory = QuestCategory.Daily;
        
        // Quest data
        private List<Quest> _dailyQuests = new List<Quest>();
        private List<Quest> _weeklyQuests = new List<Quest>();
        private List<Quest> _storyQuests = new List<Quest>();
        
        // Progress tracking
        private TextMeshProUGUI _progressSummaryText;
        private int _dailyCompleted;
        private int _weeklyCompleted;
        
        public static QuestPanel Instance { get; private set; }
        
        // Events
        public event Action<Quest> OnQuestCompleted;
        public event Action<Quest> OnQuestClaimed;
        public event Action<int> OnDailyQuestsReset;

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
            GenerateQuests();
            CreateQuestPanel();
            Hide();
        }

        private void GenerateQuests()
        {
            // Generate Daily Quests
            _dailyQuests = new List<Quest>
            {
                new Quest
                {
                    Id = "daily_login",
                    Title = "Login Bonus",
                    Description = "Log in today",
                    Category = QuestCategory.Daily,
                    Type = QuestType.Login,
                    TargetAmount = 1,
                    CurrentAmount = 1, // Auto-complete on login
                    RewardGold = 100,
                    RewardXP = 50,
                    IsCompleted = true
                },
                new Quest
                {
                    Id = "daily_train",
                    Title = "Military Training",
                    Description = "Train 5 troops",
                    Category = QuestCategory.Daily,
                    Type = QuestType.TrainTroops,
                    TargetAmount = 5,
                    CurrentAmount = 0,
                    RewardGold = 200,
                    RewardXP = 100
                },
                new Quest
                {
                    Id = "daily_collect",
                    Title = "Resource Collector",
                    Description = "Collect 1000 total resources",
                    Category = QuestCategory.Daily,
                    Type = QuestType.CollectResources,
                    TargetAmount = 1000,
                    CurrentAmount = 0,
                    RewardGold = 150,
                    RewardCrystal = 10
                },
                new Quest
                {
                    Id = "daily_attack",
                    Title = "Conqueror",
                    Description = "Attack 1 territory",
                    Category = QuestCategory.Daily,
                    Type = QuestType.AttackTerritory,
                    TargetAmount = 1,
                    CurrentAmount = 0,
                    RewardGold = 300,
                    RewardXP = 200
                },
                new Quest
                {
                    Id = "daily_chat",
                    Title = "Social Butterfly",
                    Description = "Send 3 chat messages",
                    Category = QuestCategory.Daily,
                    Type = QuestType.SendChat,
                    TargetAmount = 3,
                    CurrentAmount = 0,
                    RewardGold = 50,
                    RewardXP = 25
                }
            };
            
            // Generate Weekly Quests
            _weeklyQuests = new List<Quest>
            {
                new Quest
                {
                    Id = "weekly_conquer",
                    Title = "Territory Domination",
                    Description = "Capture 10 territories this week",
                    Category = QuestCategory.Weekly,
                    Type = QuestType.CaptureTerritory,
                    TargetAmount = 10,
                    CurrentAmount = 0,
                    RewardGold = 2000,
                    RewardCrystal = 50,
                    RewardXP = 1000
                },
                new Quest
                {
                    Id = "weekly_army",
                    Title = "Army Builder",
                    Description = "Train 50 troops this week",
                    Category = QuestCategory.Weekly,
                    Type = QuestType.TrainTroops,
                    TargetAmount = 50,
                    CurrentAmount = 0,
                    RewardGold = 1500,
                    RewardXP = 750
                },
                new Quest
                {
                    Id = "weekly_alliance",
                    Title = "Alliance Warrior",
                    Description = "Win 5 alliance wars",
                    Category = QuestCategory.Weekly,
                    Type = QuestType.WinAllianceWar,
                    TargetAmount = 5,
                    CurrentAmount = 0,
                    RewardGold = 3000,
                    RewardCrystal = 100,
                    RewardApexCoins = 50
                }
            };
            
            // Generate Story Quests (permanent progression)
            _storyQuests = new List<Quest>
            {
                new Quest
                {
                    Id = "story_ch1_1",
                    Title = "Chapter 1: The Beginning",
                    Description = "Claim your first territory",
                    Category = QuestCategory.Story,
                    Type = QuestType.CaptureTerritory,
                    TargetAmount = 1,
                    CurrentAmount = 1, // Assume completed
                    RewardGold = 500,
                    RewardXP = 250,
                    IsCompleted = true,
                    IsClaimed = true
                },
                new Quest
                {
                    Id = "story_ch1_2",
                    Title = "Building Your Army",
                    Description = "Train your first 10 soldiers",
                    Category = QuestCategory.Story,
                    Type = QuestType.TrainTroops,
                    TargetAmount = 10,
                    CurrentAmount = 5,
                    RewardGold = 750,
                    RewardXP = 400
                },
                new Quest
                {
                    Id = "story_ch1_3",
                    Title = "First Blood",
                    Description = "Win your first battle",
                    Category = QuestCategory.Story,
                    Type = QuestType.WinBattle,
                    TargetAmount = 1,
                    CurrentAmount = 0,
                    RewardGold = 1000,
                    RewardCrystal = 25,
                    RewardXP = 500
                },
                new Quest
                {
                    Id = "story_ch2_1",
                    Title = "Chapter 2: Expansion",
                    Description = "Control 5 territories simultaneously",
                    Category = QuestCategory.Story,
                    Type = QuestType.ControlTerritories,
                    TargetAmount = 5,
                    CurrentAmount = 1,
                    RewardGold = 2000,
                    RewardXP = 1000,
                    IsLocked = false
                },
                new Quest
                {
                    Id = "story_ch2_2",
                    Title = "Alliance Bound",
                    Description = "Join or create an alliance",
                    Category = QuestCategory.Story,
                    Type = QuestType.JoinAlliance,
                    TargetAmount = 1,
                    CurrentAmount = 0,
                    RewardGold = 1500,
                    RewardApexCoins = 100,
                    IsLocked = true
                }
            };
        }

        private void CreateQuestPanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("QuestPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.1f);
            rect.anchorMax = new Vector2(0.85f, 0.9f);
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
            
            // Category tabs
            CreateCategoryTabs();
            
            // Progress summary
            CreateProgressSummary();
            
            // Quest list
            CreateQuestList();
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
            title.text = "📜 QUESTS & MISSIONS";
            title.fontSize = 28;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = storyColor;
            
            // Close button
            CreateCloseButton(header.transform);
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(parent, false);
            
            LayoutElement le = closeBtn.AddComponent<LayoutElement>();
            le.preferredWidth = 40;
            le.preferredHeight = 40;
            
            Image bg = closeBtn.AddComponent<Image>();
            bg.color = new Color(0.6f, 0.2f, 0.2f, 0.8f);
            
            Button btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(Hide);
            
            GameObject txt = new GameObject("X");
            txt.transform.SetParent(closeBtn.transform, false);
            
            TextMeshProUGUI x = txt.AddComponent<TextMeshProUGUI>();
            x.text = "✕";
            x.fontSize = 24;
            x.alignment = TextAlignmentOptions.Center;
            
            RectTransform xRect = txt.GetComponent<RectTransform>();
            xRect.anchorMin = Vector2.zero;
            xRect.anchorMax = Vector2.one;
        }

        private void CreateCategoryTabs()
        {
            GameObject tabs = new GameObject("Tabs");
            tabs.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = tabs.AddComponent<LayoutElement>();
            le.preferredHeight = 45;
            
            HorizontalLayoutGroup hlayout = tabs.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(50, 50, 0, 0);
            
            CreateCategoryTab(tabs.transform, QuestCategory.Daily, "📅 Daily", dailyColor);
            CreateCategoryTab(tabs.transform, QuestCategory.Weekly, "📆 Weekly", weeklyColor);
            CreateCategoryTab(tabs.transform, QuestCategory.Story, "📖 Story", storyColor);
        }

        private void CreateCategoryTab(Transform parent, QuestCategory category, string label, Color color)
        {
            GameObject tab = new GameObject($"Tab_{category}");
            tab.transform.SetParent(parent, false);
            
            LayoutElement le = tab.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 40;
            
            Image bg = tab.AddComponent<Image>();
            bg.color = category == _selectedCategory ? color : new Color(0.15f, 0.15f, 0.2f);
            
            Button btn = tab.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectCategory(category));
            
            ColorBlock colors = btn.colors;
            colors.highlightedColor = color * 0.8f;
            colors.pressedColor = color * 0.6f;
            btn.colors = colors;
            
            GameObject txt = new GameObject("Label");
            txt.transform.SetParent(tab.transform, false);
            
            TextMeshProUGUI text = txt.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 16;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            
            RectTransform txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            
            _categoryTabs[category] = tab;
        }

        private void CreateProgressSummary()
        {
            GameObject summary = new GameObject("Summary");
            summary.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = summary.AddComponent<LayoutElement>();
            le.preferredHeight = 35;
            
            Image bg = summary.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.1f, 0.8f);
            
            _progressSummaryText = summary.AddComponent<TextMeshProUGUI>();
            _progressSummaryText.fontSize = 14;
            _progressSummaryText.alignment = TextAlignmentOptions.Center;
            UpdateProgressSummary();
        }

        private void UpdateProgressSummary()
        {
            int dailyTotal = _dailyQuests.Count;
            int dailyDone = _dailyQuests.FindAll(q => q.IsCompleted).Count;
            int weeklyTotal = _weeklyQuests.Count;
            int weeklyDone = _weeklyQuests.FindAll(q => q.IsCompleted).Count;
            
            _progressSummaryText.text = $"<color=#7FBF7F>Daily: {dailyDone}/{dailyTotal}</color>  |  " +
                $"<color=#7F9FBF>Weekly: {weeklyDone}/{weeklyTotal}</color>  |  " +
                $"<color=#BFAF6F>Resets in: 12:34:56</color>";
        }

        private void CreateQuestList()
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
            _questListContainer = new GameObject("Content");
            _questListContainer.transform.SetParent(viewport.transform, false);
            
            RectTransform contentRect = _questListContainer.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            
            ContentSizeFitter fitter = _questListContainer.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            VerticalLayoutGroup contentVL = _questListContainer.AddComponent<VerticalLayoutGroup>();
            contentVL.childAlignment = TextAnchor.UpperCenter;
            contentVL.childForceExpandWidth = true;
            contentVL.childForceExpandHeight = false;
            contentVL.spacing = 8;
            contentVL.padding = new RectOffset(10, 10, 10, 10);
            
            scroll.content = contentRect;
            
            // Populate initial quests
            RefreshQuestList();
        }

        private void RefreshQuestList()
        {
            // Clear existing
            foreach (Transform child in _questListContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Get quests for selected category
            List<Quest> quests = _selectedCategory switch
            {
                QuestCategory.Daily => _dailyQuests,
                QuestCategory.Weekly => _weeklyQuests,
                QuestCategory.Story => _storyQuests,
                _ => _dailyQuests
            };
            
            // Create quest cards
            foreach (Quest quest in quests)
            {
                CreateQuestCard(quest);
            }
        }

        private void CreateQuestCard(Quest quest)
        {
            GameObject card = new GameObject($"Quest_{quest.Id}");
            card.transform.SetParent(_questListContainer.transform, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 90;
            
            Color cardColor = quest.IsCompleted ? completedColor : GetCategoryColor(quest.Category);
            
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(cardColor.r * 0.3f, cardColor.g * 0.3f, cardColor.b * 0.3f, 0.9f);
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Left: Status icon
            CreateQuestStatusIcon(card.transform, quest);
            
            // Center: Quest info
            CreateQuestInfo(card.transform, quest);
            
            // Right: Rewards + Claim button
            CreateQuestRewards(card.transform, quest);
        }

        private void CreateQuestStatusIcon(Transform parent, Quest quest)
        {
            GameObject iconObj = new GameObject("Status");
            iconObj.transform.SetParent(parent, false);
            
            LayoutElement le = iconObj.AddComponent<LayoutElement>();
            le.preferredWidth = 50;
            
            TextMeshProUGUI icon = iconObj.AddComponent<TextMeshProUGUI>();
            
            if (quest.IsLocked)
                icon.text = "🔒";
            else if (quest.IsClaimed)
                icon.text = "✅";
            else if (quest.IsCompleted)
                icon.text = "🎁";
            else
                icon.text = "⏳";
            
            icon.fontSize = 32;
            icon.alignment = TextAlignmentOptions.Center;
        }

        private void CreateQuestInfo(Transform parent, Quest quest)
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
            title.text = quest.Title;
            title.fontSize = 16;
            title.fontStyle = FontStyles.Bold;
            title.color = quest.IsLocked ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
            
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 22;
            
            // Description
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(infoObj.transform, false);
            
            TextMeshProUGUI desc = descObj.AddComponent<TextMeshProUGUI>();
            desc.text = quest.Description;
            desc.fontSize = 12;
            desc.color = new Color(0.7f, 0.7f, 0.7f);
            
            LayoutElement descLE = descObj.AddComponent<LayoutElement>();
            descLE.preferredHeight = 18;
            
            // Progress bar
            if (!quest.IsClaimed && !quest.IsLocked)
            {
                CreateQuestProgressBar(infoObj.transform, quest);
            }
        }

        private void CreateQuestProgressBar(Transform parent, Quest quest)
        {
            GameObject progressObj = new GameObject("Progress");
            progressObj.transform.SetParent(parent, false);
            
            LayoutElement le = progressObj.AddComponent<LayoutElement>();
            le.preferredHeight = 18;
            
            HorizontalLayoutGroup hl = progressObj.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.spacing = 10;
            
            // Progress bar
            GameObject barBg = new GameObject("BarBg");
            barBg.transform.SetParent(progressObj.transform, false);
            
            LayoutElement barLE = barBg.AddComponent<LayoutElement>();
            barLE.flexibleWidth = 1;
            barLE.preferredHeight = 12;
            
            Image bgImg = barBg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f);
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(barBg.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            float progress = quest.TargetAmount > 0 ? (float)quest.CurrentAmount / quest.TargetAmount : 0f;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(progress, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = quest.IsCompleted ? progressColor : new Color(0.4f, 0.6f, 0.4f);
            
            // Progress text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(progressObj.transform, false);
            
            LayoutElement textLE = textObj.AddComponent<LayoutElement>();
            textLE.preferredWidth = 70;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = $"{quest.CurrentAmount}/{quest.TargetAmount}";
            text.fontSize = 12;
            text.alignment = TextAlignmentOptions.Right;
            text.color = quest.IsCompleted ? progressColor : Color.white;
        }

        private void CreateQuestRewards(Transform parent, Quest quest)
        {
            GameObject rewardsObj = new GameObject("Rewards");
            rewardsObj.transform.SetParent(parent, false);
            
            LayoutElement le = rewardsObj.AddComponent<LayoutElement>();
            le.preferredWidth = 130;
            
            VerticalLayoutGroup vlayout = rewardsObj.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            
            // Rewards text
            GameObject rewardTextObj = new GameObject("RewardText");
            rewardTextObj.transform.SetParent(rewardsObj.transform, false);
            
            TextMeshProUGUI rewardText = rewardTextObj.AddComponent<TextMeshProUGUI>();
            string rewards = "";
            if (quest.RewardGold > 0) rewards += $"💰{quest.RewardGold} ";
            if (quest.RewardCrystal > 0) rewards += $"💎{quest.RewardCrystal} ";
            if (quest.RewardApexCoins > 0) rewards += $"🪙{quest.RewardApexCoins} ";
            if (quest.RewardXP > 0) rewards += $"⭐{quest.RewardXP}";
            rewardText.text = rewards;
            rewardText.fontSize = 11;
            rewardText.alignment = TextAlignmentOptions.Center;
            rewardText.color = new Color(1f, 0.84f, 0f);
            
            LayoutElement rewardLE = rewardTextObj.AddComponent<LayoutElement>();
            rewardLE.preferredHeight = 20;
            
            // Claim button
            if (quest.IsCompleted && !quest.IsClaimed && !quest.IsLocked)
            {
                GameObject claimBtn = new GameObject("ClaimBtn");
                claimBtn.transform.SetParent(rewardsObj.transform, false);
                
                LayoutElement btnLE = claimBtn.AddComponent<LayoutElement>();
                btnLE.preferredHeight = 30;
                
                Image btnBg = claimBtn.AddComponent<Image>();
                btnBg.color = progressColor;
                
                Button btn = claimBtn.AddComponent<Button>();
                string questId = quest.Id;
                btn.onClick.AddListener(() => ClaimQuest(questId));
                
                GameObject btnTxt = new GameObject("Label");
                btnTxt.transform.SetParent(claimBtn.transform, false);
                
                TextMeshProUGUI label = btnTxt.AddComponent<TextMeshProUGUI>();
                label.text = "CLAIM!";
                label.fontSize = 14;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                
                RectTransform txtRect = btnTxt.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
            }
        }

        private Color GetCategoryColor(QuestCategory category)
        {
            return category switch
            {
                QuestCategory.Daily => dailyColor,
                QuestCategory.Weekly => weeklyColor,
                QuestCategory.Story => storyColor,
                _ => dailyColor
            };
        }

        private void SelectCategory(QuestCategory category)
        {
            _selectedCategory = category;
            
            // Update tab colors
            foreach (var kvp in _categoryTabs)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                bg.color = kvp.Key == category ? GetCategoryColor(kvp.Key) : new Color(0.15f, 0.15f, 0.2f);
            }
            
            RefreshQuestList();
        }

        private void ClaimQuest(string questId)
        {
            Quest quest = FindQuest(questId);
            if (quest == null || !quest.IsCompleted || quest.IsClaimed) return;
            
            // Grant rewards
            if (PCResourceSystem.Instance != null)
            {
                if (quest.RewardGold > 0)
                    PCResourceSystem.Instance.AddResource(ResourceType.Gold, quest.RewardGold);
                if (quest.RewardCrystal > 0)
                    PCResourceSystem.Instance.AddResource(ResourceType.Crystal, quest.RewardCrystal);
                if (quest.RewardApexCoins > 0)
                    PCResourceSystem.Instance.AddResource(ResourceType.ApexCoins, quest.RewardApexCoins);
            }
            
            quest.IsClaimed = true;
            
            Debug.Log($"[Quest] Claimed quest: {quest.Title}! Rewards: {quest.RewardGold}g, {quest.RewardCrystal}c, {quest.RewardXP}xp");
            
            OnQuestClaimed?.Invoke(quest);
            
            RefreshQuestList();
            UpdateProgressSummary();
        }

        private Quest FindQuest(string questId)
        {
            Quest quest = _dailyQuests.Find(q => q.Id == questId);
            if (quest != null) return quest;
            
            quest = _weeklyQuests.Find(q => q.Id == questId);
            if (quest != null) return quest;
            
            quest = _storyQuests.Find(q => q.Id == questId);
            return quest;
        }

        #region Progress Tracking

        public void UpdateQuestProgress(QuestType type, int amount)
        {
            UpdateQuestsOfType(_dailyQuests, type, amount);
            UpdateQuestsOfType(_weeklyQuests, type, amount);
            UpdateQuestsOfType(_storyQuests, type, amount);
            
            if (_panel.activeSelf)
            {
                RefreshQuestList();
                UpdateProgressSummary();
            }
        }

        private void UpdateQuestsOfType(List<Quest> quests, QuestType type, int amount)
        {
            foreach (Quest quest in quests)
            {
                if (quest.Type == type && !quest.IsCompleted && !quest.IsLocked)
                {
                    quest.CurrentAmount += amount;
                    if (quest.CurrentAmount >= quest.TargetAmount)
                    {
                        quest.CurrentAmount = quest.TargetAmount;
                        quest.IsCompleted = true;
                        OnQuestCompleted?.Invoke(quest);
                        Debug.Log($"[Quest] Completed: {quest.Title}!");
                    }
                }
            }
        }

        #endregion

        #region Public API

        public void Show()
        {
            _panel.SetActive(true);
            RefreshQuestList();
            UpdateProgressSummary();
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

        public int GetUnclaimedQuestCount()
        {
            int count = 0;
            count += _dailyQuests.FindAll(q => q.IsCompleted && !q.IsClaimed).Count;
            count += _weeklyQuests.FindAll(q => q.IsCompleted && !q.IsClaimed).Count;
            count += _storyQuests.FindAll(q => q.IsCompleted && !q.IsClaimed).Count;
            return count;
        }

        #endregion
    }

    public enum QuestCategory
    {
        Daily,
        Weekly,
        Story
    }

    public enum QuestType
    {
        Login,
        TrainTroops,
        CollectResources,
        AttackTerritory,
        CaptureTerritory,
        WinBattle,
        SendChat,
        WinAllianceWar,
        ControlTerritories,
        JoinAlliance,
        UpgradeBuilding,
        SpendResources
    }

    public class Quest
    {
        public string Id;
        public string Title;
        public string Description;
        public QuestCategory Category;
        public QuestType Type;
        public int TargetAmount;
        public int CurrentAmount;
        public bool IsCompleted;
        public bool IsClaimed;
        public bool IsLocked;
        
        // Rewards
        public int RewardGold;
        public int RewardCrystal;
        public int RewardApexCoins;
        public int RewardXP;
    }
}
