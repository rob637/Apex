using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Daily Challenges Panel - Daily quests, streak rewards, and time-limited objectives.
    /// Features daily resets, streak bonuses, and challenge progression.
    /// </summary>
    public class DailyChallengesPanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.9f, 0.65f, 0.2f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color completedColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f);
        
        // UI References
        private GameObject _panel;
        private GameObject _contentArea;
        private ChallengeTab _currentTab = ChallengeTab.Daily;
        
        // Challenge data
        private List<DailyChallenge> _dailyChallenges = new List<DailyChallenge>();
        private List<WeeklyChallenge> _weeklyChallenges = new List<WeeklyChallenge>();
        private List<StreakReward> _streakRewards = new List<StreakReward>();
        private int _currentStreak = 7;
        private int _longestStreak = 23;
        private DateTime _lastClaimTime;
        private DateTime _dailyResetTime;
        private DateTime _weeklyResetTime;
        private int _totalChallengesCompleted = 156;
        
        public static DailyChallengesPanel Instance { get; private set; }
        
        public event Action<DailyChallenge> OnChallengeCompleted;
        public event Action<int> OnStreakRewardClaimed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeChallengeData();
        }

        private void Start()
        {
            CreatePanel();
            Hide();
        }

        private void InitializeChallengeData()
        {
            _lastClaimTime = DateTime.Now.AddDays(-1);
            _dailyResetTime = DateTime.Today.AddDays(1).AddHours(5); // Reset at 5 AM
            _weeklyResetTime = DateTime.Today.AddDays(7 - (int)DateTime.Today.DayOfWeek); // Sunday
            
            _dailyChallenges = new List<DailyChallenge>
            {
                new DailyChallenge
                {
                    ChallengeId = "DC001",
                    Title = "Resource Collector",
                    Description = "Collect 50,000 total resources",
                    Category = ChallengeCategory.Resources,
                    CurrentProgress = 42500,
                    RequiredProgress = 50000,
                    Rewards = new List<ChallengeReward> { new ChallengeReward { Type = ResourceType.Gold, Amount = 5000 } },
                    ExperienceReward = 100,
                    Difficulty = ChallengeDifficulty.Easy,
                    Icon = "💰"
                },
                new DailyChallenge
                {
                    ChallengeId = "DC002",
                    Title = "Battle Ready",
                    Description = "Win 3 battles",
                    Category = ChallengeCategory.Combat,
                    CurrentProgress = 2,
                    RequiredProgress = 3,
                    Rewards = new List<ChallengeReward> { new ChallengeReward { Type = ResourceType.Iron, Amount = 2000 } },
                    ExperienceReward = 150,
                    Difficulty = ChallengeDifficulty.Easy,
                    Icon = "⚔️"
                },
                new DailyChallenge
                {
                    ChallengeId = "DC003",
                    Title = "Builder's Journey",
                    Description = "Upgrade any building",
                    Category = ChallengeCategory.Building,
                    CurrentProgress = 1,
                    RequiredProgress = 1,
                    IsCompleted = true,
                    IsClaimed = true,
                    Rewards = new List<ChallengeReward> { new ChallengeReward { Type = ResourceType.Stone, Amount = 3000 } },
                    ExperienceReward = 100,
                    Difficulty = ChallengeDifficulty.Easy,
                    Icon = "🏗️"
                },
                new DailyChallenge
                {
                    ChallengeId = "DC004",
                    Title = "Army Expansion",
                    Description = "Train 100 troops",
                    Category = ChallengeCategory.Military,
                    CurrentProgress = 65,
                    RequiredProgress = 100,
                    Rewards = new List<ChallengeReward> { new ChallengeReward { Type = ResourceType.Gold, Amount = 3000 } },
                    ExperienceReward = 120,
                    Difficulty = ChallengeDifficulty.Medium,
                    Icon = "🎖️"
                },
                new DailyChallenge
                {
                    ChallengeId = "DC005",
                    Title = "Siege Master",
                    Description = "Destroy 5 enemy buildings",
                    Category = ChallengeCategory.Combat,
                    CurrentProgress = 2,
                    RequiredProgress = 5,
                    Rewards = new List<ChallengeReward> { 
                        new ChallengeReward { Type = ResourceType.Gold, Amount = 8000 },
                        new ChallengeReward { Type = ResourceType.Crystal, Amount = 50 }
                    },
                    ExperienceReward = 200,
                    Difficulty = ChallengeDifficulty.Hard,
                    Icon = "💥"
                },
                new DailyChallenge
                {
                    ChallengeId = "DC006",
                    Title = "Alliance Helper",
                    Description = "Help 5 alliance members",
                    Category = ChallengeCategory.Social,
                    CurrentProgress = 3,
                    RequiredProgress = 5,
                    Rewards = new List<ChallengeReward> { new ChallengeReward { Type = ResourceType.Gold, Amount = 2000 } },
                    ExperienceReward = 80,
                    Difficulty = ChallengeDifficulty.Easy,
                    Icon = "🤝"
                },
                new DailyChallenge
                {
                    ChallengeId = "DC007",
                    Title = "Research Pioneer",
                    Description = "Complete a technology research",
                    Category = ChallengeCategory.Research,
                    CurrentProgress = 0,
                    RequiredProgress = 1,
                    Rewards = new List<ChallengeReward> { new ChallengeReward { Type = ResourceType.Crystal, Amount = 100 } },
                    ExperienceReward = 150,
                    Difficulty = ChallengeDifficulty.Medium,
                    Icon = "🔬"
                },
                new DailyChallenge
                {
                    ChallengeId = "DC008",
                    Title = "Perfect Defense",
                    Description = "Successfully defend your city",
                    Category = ChallengeCategory.Combat,
                    CurrentProgress = 1,
                    RequiredProgress = 1,
                    IsCompleted = true,
                    IsClaimed = false,
                    Rewards = new List<ChallengeReward> { 
                        new ChallengeReward { Type = ResourceType.Gold, Amount = 10000 },
                        new ChallengeReward { Type = ResourceType.ApexCoins, Amount = 10 }
                    },
                    ExperienceReward = 300,
                    Difficulty = ChallengeDifficulty.Hard,
                    Icon = "🛡️"
                }
            };
            
            _weeklyChallenges = new List<WeeklyChallenge>
            {
                new WeeklyChallenge
                {
                    ChallengeId = "WC001",
                    Title = "Conquest Champion",
                    Description = "Win 20 battles this week",
                    CurrentProgress = 12,
                    RequiredProgress = 20,
                    Rewards = new List<ChallengeReward> { 
                        new ChallengeReward { Type = ResourceType.Gold, Amount = 50000 },
                        new ChallengeReward { Type = ResourceType.ApexCoins, Amount = 50 }
                    },
                    ExperienceReward = 1000,
                    Icon = "👑"
                },
                new WeeklyChallenge
                {
                    ChallengeId = "WC002",
                    Title = "Economic Powerhouse",
                    Description = "Collect 500,000 resources",
                    CurrentProgress = 325000,
                    RequiredProgress = 500000,
                    Rewards = new List<ChallengeReward> { 
                        new ChallengeReward { Type = ResourceType.Crystal, Amount = 500 }
                    },
                    ExperienceReward = 800,
                    Icon = "💎"
                },
                new WeeklyChallenge
                {
                    ChallengeId = "WC003",
                    Title = "Master Builder",
                    Description = "Complete 10 building upgrades",
                    CurrentProgress = 7,
                    RequiredProgress = 10,
                    Rewards = new List<ChallengeReward> { 
                        new ChallengeReward { Type = ResourceType.ApexCoins, Amount = 100 }
                    },
                    ExperienceReward = 1200,
                    Icon = "🏰"
                }
            };
            
            _streakRewards = new List<StreakReward>
            {
                new StreakReward { Day = 1, Reward = new ChallengeReward { Type = ResourceType.Gold, Amount = 5000 }, Icon = "📦", IsClaimed = true },
                new StreakReward { Day = 2, Reward = new ChallengeReward { Type = ResourceType.Stone, Amount = 5000 }, Icon = "🪨", IsClaimed = true },
                new StreakReward { Day = 3, Reward = new ChallengeReward { Type = ResourceType.Wood, Amount = 5000 }, Icon = "🪵", IsClaimed = true },
                new StreakReward { Day = 4, Reward = new ChallengeReward { Type = ResourceType.Iron, Amount = 3000 }, Icon = "⚙️", IsClaimed = true },
                new StreakReward { Day = 5, Reward = new ChallengeReward { Type = ResourceType.Crystal, Amount = 100 }, Icon = "💎", IsClaimed = true },
                new StreakReward { Day = 6, Reward = new ChallengeReward { Type = ResourceType.Gold, Amount = 20000 }, Icon = "💰", IsClaimed = true },
                new StreakReward { Day = 7, Reward = new ChallengeReward { Type = ResourceType.ApexCoins, Amount = 50 }, Icon = "🌟", IsClaimed = true },
                new StreakReward { Day = 14, Reward = new ChallengeReward { Type = ResourceType.ApexCoins, Amount = 100 }, Icon = "👑", IsClaimed = false, IsMilestone = true },
                new StreakReward { Day = 30, Reward = new ChallengeReward { Type = ResourceType.ApexCoins, Amount = 300 }, Icon = "🏆", IsClaimed = false, IsMilestone = true }
            };
        }

        private void CreatePanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            _panel = new GameObject("DailyChallengesPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.1f);
            rect.anchorMax = new Vector2(0.85f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.06f, 0.04f, 0.98f);
            
            VerticalLayoutGroup layout = _panel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 0;
            
            CreateHeader();
            CreateStreakSection();
            CreateTabBar();
            CreateContentArea();
        }

        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            Image bg = header.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.05f);
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 20;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            // Title
            CreateText(header.transform, "📅 DAILY CHALLENGES", 20, TextAlignmentOptions.Left, accentColor);
            
            // Spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(header.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Reset timer
            CreateResetTimer(header.transform);
            
            // Close button
            CreateCloseButton(header.transform);
        }

        private void CreateResetTimer(Transform parent)
        {
            GameObject timer = new GameObject("ResetTimer");
            timer.transform.SetParent(parent, false);
            
            LayoutElement le = timer.AddComponent<LayoutElement>();
            le.preferredWidth = 150;
            
            VerticalLayoutGroup vlayout = timer.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(timer.transform, "⏰ Resets in", 10, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            CreateText(timer.transform, GetTimeRemaining(_dailyResetTime), 14, TextAlignmentOptions.Center, accentColor);
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject btn = new GameObject("CloseBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 40;
            le.preferredHeight = 40;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = new Color(0.5f, 0.2f, 0.2f);
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(Hide);
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = "✕";
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateStreakSection()
        {
            GameObject streak = new GameObject("StreakSection");
            streak.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = streak.AddComponent<LayoutElement>();
            le.preferredHeight = 120;
            
            Image bg = streak.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.1f, 0.06f);
            
            HorizontalLayoutGroup hlayout = streak.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 30;
            hlayout.padding = new RectOffset(30, 30, 15, 15);
            
            // Streak info
            CreateStreakInfo(streak.transform);
            
            // Streak calendar
            CreateStreakCalendar(streak.transform);
        }

        private void CreateStreakInfo(Transform parent)
        {
            GameObject info = new GameObject("StreakInfo");
            info.transform.SetParent(parent, false);
            
            LayoutElement le = info.AddComponent<LayoutElement>();
            le.preferredWidth = 200;
            
            VerticalLayoutGroup vlayout = info.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            
            CreateText(info.transform, "🔥 LOGIN STREAK", 12, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            CreateText(info.transform, $"{_currentStreak} Days", 32, TextAlignmentOptions.Center, goldColor);
            CreateText(info.transform, $"Best: {_longestStreak} days", 10, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
        }

        private void CreateStreakCalendar(Transform parent)
        {
            GameObject calendar = new GameObject("StreakCalendar");
            calendar.transform.SetParent(parent, false);
            
            LayoutElement le = calendar.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            HorizontalLayoutGroup hlayout = calendar.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 8;
            
            // Show 7-day streak rewards
            for (int i = 0; i < 7; i++)
            {
                int day = i + 1;
                var reward = _streakRewards.Find(r => r.Day == day);
                CreateStreakDay(calendar.transform, day, reward);
            }
            
            // Milestone markers
            CreateMilestoneMarker(calendar.transform, 14, _streakRewards.Find(r => r.Day == 14));
            CreateMilestoneMarker(calendar.transform, 30, _streakRewards.Find(r => r.Day == 30));
        }

        private void CreateStreakDay(Transform parent, int day, StreakReward reward)
        {
            GameObject dayObj = new GameObject($"Day_{day}");
            dayObj.transform.SetParent(parent, false);
            
            LayoutElement le = dayObj.AddComponent<LayoutElement>();
            le.preferredWidth = 50;
            le.preferredHeight = 70;
            
            bool isCurrent = day == _currentStreak;
            bool isClaimed = reward?.IsClaimed ?? day < _currentStreak;
            bool isAvailable = day <= _currentStreak && !isClaimed;
            
            Color bgColor;
            if (isClaimed) bgColor = new Color(0.2f, 0.35f, 0.2f);
            else if (isAvailable) bgColor = accentColor;
            else bgColor = new Color(0.15f, 0.15f, 0.15f);
            
            Image bg = dayObj.AddComponent<Image>();
            bg.color = bgColor;
            
            if (isCurrent)
            {
                UnityEngine.UI.Outline outline = dayObj.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = goldColor;
                outline.effectDistance = new Vector2(2, 2);
            }
            
            VerticalLayoutGroup vlayout = dayObj.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(5, 5, 5, 5);
            
            string icon = reward?.Icon ?? "📦";
            CreateText(dayObj.transform, icon, 18, TextAlignmentOptions.Center);
            CreateText(dayObj.transform, $"Day {day}", 9, TextAlignmentOptions.Center, Color.white);
            
            if (isClaimed)
            {
                CreateText(dayObj.transform, "✓", 12, TextAlignmentOptions.Center, completedColor);
            }
            
            if (isAvailable)
            {
                Button btn = dayObj.AddComponent<Button>();
                btn.onClick.AddListener(() => ClaimStreakReward(day));
            }
        }

        private void CreateMilestoneMarker(Transform parent, int day, StreakReward reward)
        {
            GameObject marker = new GameObject($"Milestone_{day}");
            marker.transform.SetParent(parent, false);
            
            LayoutElement le = marker.AddComponent<LayoutElement>();
            le.preferredWidth = 60;
            le.preferredHeight = 70;
            
            bool isUnlocked = _currentStreak >= day;
            bool isClaimed = reward?.IsClaimed ?? false;
            
            Color bgColor = isUnlocked ? (isClaimed ? completedColor : goldColor) : lockedColor;
            
            Image bg = marker.AddComponent<Image>();
            bg.color = bgColor;
            
            VerticalLayoutGroup vlayout = marker.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(5, 5, 5, 5);
            
            CreateText(marker.transform, reward?.Icon ?? "🏆", 20, TextAlignmentOptions.Center);
            CreateText(marker.transform, $"{day} Days", 9, TextAlignmentOptions.Center, Color.white);
            
            if (isUnlocked && !isClaimed)
            {
                Button btn = marker.AddComponent<Button>();
                btn.onClick.AddListener(() => ClaimStreakReward(day));
            }
        }

        private void CreateTabBar()
        {
            GameObject tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = tabBar.AddComponent<LayoutElement>();
            le.preferredHeight = 45;
            
            Image bg = tabBar.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.05f, 0.03f);
            
            HorizontalLayoutGroup hlayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 5;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            int dailyRemaining = _dailyChallenges.FindAll(c => !c.IsClaimed).Count;
            int weeklyRemaining = _weeklyChallenges.FindAll(c => !c.IsClaimed).Count;
            
            CreateTab(tabBar.transform, ChallengeTab.Daily, $"📅 Daily ({dailyRemaining})");
            CreateTab(tabBar.transform, ChallengeTab.Weekly, $"📆 Weekly ({weeklyRemaining})");
            CreateTab(tabBar.transform, ChallengeTab.Special, "⭐ Special");
            CreateTab(tabBar.transform, ChallengeTab.Achievements, "🏆 Progress");
        }

        private void CreateTab(Transform parent, ChallengeTab tab, string label)
        {
            GameObject tabObj = new GameObject($"Tab_{tab}");
            tabObj.transform.SetParent(parent, false);
            
            bool isActive = tab == _currentTab;
            Color bgColor = isActive ? accentColor : new Color(0.12f, 0.1f, 0.08f);
            
            Image bg = tabObj.AddComponent<Image>();
            bg.color = bgColor;
            
            Button btn = tabObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SetTab(tab));
            
            LayoutElement le = tabObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 35;
            
            TextMeshProUGUI text = tabObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 12;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
        }

        private void CreateContentArea()
        {
            _contentArea = new GameObject("ContentArea");
            _contentArea.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _contentArea.AddComponent<LayoutElement>();
            le.flexibleHeight = 1;
            
            Image bg = _contentArea.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.04f, 0.03f);
            
            RefreshContent();
        }

        private void RefreshContent()
        {
            foreach (Transform child in _contentArea.transform)
            {
                Destroy(child.gameObject);
            }
            
            switch (_currentTab)
            {
                case ChallengeTab.Daily:
                    CreateDailyContent();
                    break;
                case ChallengeTab.Weekly:
                    CreateWeeklyContent();
                    break;
                case ChallengeTab.Special:
                    CreateSpecialContent();
                    break;
                case ChallengeTab.Achievements:
                    CreateAchievementsContent();
                    break;
            }
        }

        private void CreateDailyContent()
        {
            GameObject scrollView = CreateScrollView(_contentArea.transform);
            GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;
            
            // Claim all button
            int claimable = _dailyChallenges.FindAll(c => c.IsCompleted && !c.IsClaimed).Count;
            if (claimable > 0)
            {
                CreateClaimAllButton(content.transform, claimable);
            }
            
            // Progress summary
            CreateProgressSummary(content.transform, _dailyChallenges);
            
            // Challenge list
            foreach (var challenge in _dailyChallenges)
            {
                CreateChallengeRow(content.transform, challenge);
            }
        }

        private void CreateClaimAllButton(Transform parent, int count)
        {
            GameObject btn = new GameObject("ClaimAllBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = completedColor;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(ClaimAllDaily);
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = $"🎁 CLAIM ALL REWARDS ({count})";
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateProgressSummary(Transform parent, List<DailyChallenge> challenges)
        {
            int completed = challenges.FindAll(c => c.IsCompleted).Count;
            int total = challenges.Count;
            
            GameObject summary = new GameObject("ProgressSummary");
            summary.transform.SetParent(parent, false);
            
            LayoutElement le = summary.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            Image bg = summary.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.06f);
            
            HorizontalLayoutGroup hlayout = summary.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 20;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            // Progress text
            CreateText(summary.transform, $"📊 Progress: {completed}/{total}", 14, TextAlignmentOptions.Center, Color.white);
            
            // Progress bar
            CreateProgressBar(summary.transform, (float)completed / total);
            
            // Bonus reward preview
            if (completed == total)
            {
                CreateText(summary.transform, "🎉 All Complete! +50 Bonus XP", 12, TextAlignmentOptions.Center, goldColor);
            }
        }

        private void CreateProgressBar(Transform parent, float progress)
        {
            GameObject bar = new GameObject("ProgressBar");
            bar.transform.SetParent(parent, false);
            
            LayoutElement le = bar.AddComponent<LayoutElement>();
            le.preferredWidth = 200;
            le.preferredHeight = 15;
            
            Image bgImg = bar.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f);
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = accentColor;
        }

        private void CreateChallengeRow(Transform parent, DailyChallenge challenge)
        {
            GameObject row = new GameObject($"Challenge_{challenge.ChallengeId}");
            row.transform.SetParent(parent, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 90;
            
            Color bgColor = challenge.IsClaimed ? new Color(0.1f, 0.15f, 0.1f) : 
                           challenge.IsCompleted ? new Color(0.15f, 0.2f, 0.1f) : 
                           new Color(0.08f, 0.06f, 0.05f);
            
            Image bg = row.AddComponent<Image>();
            bg.color = bgColor;
            
            HorizontalLayoutGroup hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            // Icon
            CreateChallengeIcon(row.transform, challenge);
            
            // Info
            CreateChallengeInfo(row.transform, challenge);
            
            // Rewards
            CreateChallengeRewards(row.transform, challenge);
            
            // Action button
            CreateChallengeAction(row.transform, challenge);
        }

        private void CreateChallengeIcon(Transform parent, DailyChallenge challenge)
        {
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(parent, false);
            
            LayoutElement le = icon.AddComponent<LayoutElement>();
            le.preferredWidth = 60;
            le.preferredHeight = 60;
            
            Color bgColor = GetDifficultyColor(challenge.Difficulty);
            Image bg = icon.AddComponent<Image>();
            bg.color = bgColor;
            
            VerticalLayoutGroup vlayout = icon.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(icon.transform, challenge.Icon, 24, TextAlignmentOptions.Center);
            CreateText(icon.transform, GetDifficultyLabel(challenge.Difficulty), 8, TextAlignmentOptions.Center, Color.white);
        }

        private void CreateChallengeInfo(Transform parent, DailyChallenge challenge)
        {
            GameObject info = new GameObject("Info");
            info.transform.SetParent(parent, false);
            
            LayoutElement le = info.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            VerticalLayoutGroup vlayout = info.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleLeft;
            vlayout.spacing = 3;
            
            // Title
            Color titleColor = challenge.IsClaimed ? new Color(0.5f, 0.7f, 0.5f) : Color.white;
            CreateText(info.transform, challenge.Title, 14, TextAlignmentOptions.Left, titleColor);
            
            // Description
            CreateText(info.transform, challenge.Description, 10, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            // Progress bar
            if (!challenge.IsClaimed)
            {
                float progress = (float)challenge.CurrentProgress / challenge.RequiredProgress;
                CreateProgressBarSmall(info.transform, progress, $"{challenge.CurrentProgress}/{challenge.RequiredProgress}");
            }
        }

        private void CreateProgressBarSmall(Transform parent, float progress, string label)
        {
            GameObject container = new GameObject("ProgressContainer");
            container.transform.SetParent(parent, false);
            
            HorizontalLayoutGroup hlayout = container.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 10;
            
            GameObject bar = new GameObject("Bar");
            bar.transform.SetParent(container.transform, false);
            
            LayoutElement le = bar.AddComponent<LayoutElement>();
            le.preferredWidth = 150;
            le.preferredHeight = 10;
            
            Image bgImg = bar.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f);
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            Color fillColor = progress >= 1f ? completedColor : accentColor;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;
            
            CreateText(container.transform, label, 9, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateChallengeRewards(Transform parent, DailyChallenge challenge)
        {
            GameObject rewards = new GameObject("Rewards");
            rewards.transform.SetParent(parent, false);
            
            LayoutElement le = rewards.AddComponent<LayoutElement>();
            le.preferredWidth = 120;
            
            VerticalLayoutGroup vlayout = rewards.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 2;
            
            foreach (var reward in challenge.Rewards)
            {
                string icon = GetResourceIcon(reward.Type);
                CreateText(rewards.transform, $"{icon} {FormatNumber(reward.Amount)}", 11, TextAlignmentOptions.Center, goldColor);
            }
            
            CreateText(rewards.transform, $"+{challenge.ExperienceReward} XP", 9, TextAlignmentOptions.Center, new Color(0.5f, 0.8f, 1f));
        }

        private void CreateChallengeAction(Transform parent, DailyChallenge challenge)
        {
            GameObject btn = new GameObject("ActionBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 80;
            le.preferredHeight = 40;
            
            string label;
            Color bgColor;
            bool interactable = false;
            
            if (challenge.IsClaimed)
            {
                label = "✓ Claimed";
                bgColor = new Color(0.3f, 0.4f, 0.3f);
            }
            else if (challenge.IsCompleted)
            {
                label = "🎁 Claim";
                bgColor = completedColor;
                interactable = true;
            }
            else
            {
                label = "🔓 Go";
                bgColor = new Color(0.3f, 0.25f, 0.2f);
                interactable = true;
            }
            
            Image bg = btn.AddComponent<Image>();
            bg.color = bgColor;
            
            if (interactable)
            {
                Button button = btn.AddComponent<Button>();
                if (challenge.IsCompleted)
                    button.onClick.AddListener(() => ClaimChallenge(challenge));
                else
                    button.onClick.AddListener(() => GoToChallenge(challenge));
            }
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 10;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateWeeklyContent()
        {
            GameObject scrollView = CreateScrollView(_contentArea.transform);
            GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;
            
            // Weekly reset timer
            CreateWeeklyHeader(content.transform);
            
            // Weekly challenges
            foreach (var challenge in _weeklyChallenges)
            {
                CreateWeeklyChallengeRow(content.transform, challenge);
            }
        }

        private void CreateWeeklyHeader(Transform parent)
        {
            GameObject header = new GameObject("WeeklyHeader");
            header.transform.SetParent(parent, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            Image bg = header.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.06f);
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            CreateText(header.transform, "📆 Weekly Challenges reset in:", 12, TextAlignmentOptions.Left, Color.white);
            CreateText(header.transform, GetTimeRemaining(_weeklyResetTime), 14, TextAlignmentOptions.Right, accentColor);
        }

        private void CreateWeeklyChallengeRow(Transform parent, WeeklyChallenge challenge)
        {
            GameObject row = new GameObject($"Weekly_{challenge.ChallengeId}");
            row.transform.SetParent(parent, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            Image bg = row.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.05f);
            
            VerticalLayoutGroup vlayout = row.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.spacing = 8;
            vlayout.padding = new RectOffset(20, 20, 15, 15);
            
            // Header
            GameObject header = new GameObject("Header");
            header.transform.SetParent(row.transform, false);
            
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.spacing = 10;
            
            CreateText(header.transform, challenge.Icon, 24, TextAlignmentOptions.Center);
            CreateText(header.transform, challenge.Title, 16, TextAlignmentOptions.Left, Color.white);
            
            // Description
            CreateText(row.transform, challenge.Description, 11, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            // Progress
            float progress = (float)challenge.CurrentProgress / challenge.RequiredProgress;
            CreateProgressBarSmall(row.transform, progress, $"{FormatNumber(challenge.CurrentProgress)}/{FormatNumber(challenge.RequiredProgress)}");
            
            // Rewards
            CreateWeeklyRewards(row.transform, challenge);
        }

        private void CreateWeeklyRewards(Transform parent, WeeklyChallenge challenge)
        {
            GameObject rewards = new GameObject("Rewards");
            rewards.transform.SetParent(parent, false);
            
            HorizontalLayoutGroup hlayout = rewards.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 15;
            
            CreateText(rewards.transform, "Rewards:", 10, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
            
            foreach (var reward in challenge.Rewards)
            {
                string icon = GetResourceIcon(reward.Type);
                CreateText(rewards.transform, $"{icon} {FormatNumber(reward.Amount)}", 12, TextAlignmentOptions.Center, goldColor);
            }
            
            CreateText(rewards.transform, $"+{challenge.ExperienceReward} XP", 10, TextAlignmentOptions.Center, new Color(0.5f, 0.8f, 1f));
        }

        private void CreateSpecialContent()
        {
            GameObject scrollView = CreateScrollView(_contentArea.transform);
            GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;
            
            CreateSectionHeader(content.transform, "🌟 SPECIAL EVENT CHALLENGES");
            
            // Event challenge examples
            CreateSpecialChallengeCard(content.transform, "🎃 Halloween Havoc", "Defeat 50 skeleton warriors", 35, 50, "Limited Time!", new Color(0.6f, 0.3f, 0.8f));
            CreateSpecialChallengeCard(content.transform, "❄️ Winter Siege", "Capture 3 frozen territories", 1, 3, "3 days left", new Color(0.3f, 0.6f, 0.9f));
            CreateSpecialChallengeCard(content.transform, "🔥 Dragon's Wrath", "Deal 1M damage to world boss", 650000, 1000000, "Community Goal", new Color(0.9f, 0.4f, 0.2f));
        }

        private void CreateSpecialChallengeCard(Transform parent, string title, string desc, int current, int required, string tag, Color themeColor)
        {
            GameObject card = new GameObject($"SpecialChallenge_{title}");
            card.transform.SetParent(parent, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 120;
            
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(themeColor.r * 0.2f, themeColor.g * 0.2f, themeColor.b * 0.2f);
            
            VerticalLayoutGroup vlayout = card.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.spacing = 8;
            vlayout.padding = new RectOffset(20, 20, 15, 15);
            
            // Header with tag
            GameObject header = new GameObject("Header");
            header.transform.SetParent(card.transform, false);
            
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.spacing = 10;
            
            CreateText(header.transform, title, 16, TextAlignmentOptions.Left, themeColor);
            
            GameObject tagObj = new GameObject("Tag");
            tagObj.transform.SetParent(header.transform, false);
            
            LayoutElement tagLE = tagObj.AddComponent<LayoutElement>();
            tagLE.preferredWidth = 80;
            tagLE.preferredHeight = 20;
            
            Image tagBg = tagObj.AddComponent<Image>();
            tagBg.color = themeColor;
            
            TextMeshProUGUI tagText = tagObj.AddComponent<TextMeshProUGUI>();
            tagText.text = tag;
            tagText.fontSize = 9;
            tagText.alignment = TextAlignmentOptions.Center;
            tagText.color = Color.white;
            
            CreateText(card.transform, desc, 11, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f));
            
            float progress = (float)current / required;
            CreateProgressBarSmall(card.transform, progress, $"{FormatNumber(current)}/{FormatNumber(required)}");
            
            CreateText(card.transform, "🎁 Exclusive Rewards + 500 XP", 10, TextAlignmentOptions.Center, goldColor);
        }

        private void CreateAchievementsContent()
        {
            GameObject scrollView = CreateScrollView(_contentArea.transform);
            GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;
            
            // Stats summary
            CreateStatsSummary(content.transform);
            
            // Category breakdown
            CreateCategoryBreakdown(content.transform);
        }

        private void CreateStatsSummary(Transform parent)
        {
            GameObject summary = new GameObject("StatsSummary");
            summary.transform.SetParent(parent, false);
            
            LayoutElement le = summary.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            Image bg = summary.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.06f);
            
            HorizontalLayoutGroup hlayout = summary.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 30;
            hlayout.padding = new RectOffset(30, 30, 20, 20);
            
            CreateStatBox(summary.transform, "🏆", "Total Completed", _totalChallengesCompleted.ToString());
            CreateStatBox(summary.transform, "🔥", "Current Streak", $"{_currentStreak} days");
            CreateStatBox(summary.transform, "⭐", "Best Streak", $"{_longestStreak} days");
            CreateStatBox(summary.transform, "💫", "Total XP Earned", "45,200");
        }

        private void CreateStatBox(Transform parent, string icon, string label, string value)
        {
            GameObject box = new GameObject("StatBox");
            box.transform.SetParent(parent, false);
            
            LayoutElement le = box.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            VerticalLayoutGroup vlayout = box.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 3;
            
            CreateText(box.transform, icon, 28, TextAlignmentOptions.Center);
            CreateText(box.transform, value, 16, TextAlignmentOptions.Center, goldColor);
            CreateText(box.transform, label, 10, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
        }

        private void CreateCategoryBreakdown(Transform parent)
        {
            CreateSectionHeader(parent, "📊 CHALLENGE CATEGORIES");
            
            CreateCategoryRow(parent, ChallengeCategory.Combat, "⚔️ Combat", 45, 60);
            CreateCategoryRow(parent, ChallengeCategory.Resources, "💰 Resources", 38, 50);
            CreateCategoryRow(parent, ChallengeCategory.Building, "🏗️ Building", 28, 40);
            CreateCategoryRow(parent, ChallengeCategory.Social, "🤝 Social", 22, 35);
            CreateCategoryRow(parent, ChallengeCategory.Military, "🎖️ Military", 15, 30);
            CreateCategoryRow(parent, ChallengeCategory.Research, "🔬 Research", 8, 25);
        }

        private void CreateCategoryRow(Transform parent, ChallengeCategory category, string label, int completed, int total)
        {
            GameObject row = new GameObject($"Category_{category}");
            row.transform.SetParent(parent, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            Image bg = row.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.05f, 0.04f);
            
            HorizontalLayoutGroup hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            CreateText(row.transform, label, 13, TextAlignmentOptions.Left, Color.white);
            
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(row.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            float progress = (float)completed / total;
            CreateProgressBar(row.transform, progress);
            
            CreateText(row.transform, $"{completed}/{total}", 12, TextAlignmentOptions.Right, accentColor);
        }

        #region Helpers

        private GameObject CreateScrollView(Transform parent)
        {
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(parent, false);
            
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;
            
            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;
            
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 10;
            layout.padding = new RectOffset(15, 15, 15, 15);
            
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            
            return scrollView;
        }

        private void CreateSectionHeader(Transform parent, string text)
        {
            GameObject header = new GameObject("SectionHeader");
            header.transform.SetParent(parent, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            TextMeshProUGUI tmp = header.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 13;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = accentColor;
            tmp.alignment = TextAlignmentOptions.Left;
        }

        private GameObject CreateText(Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Color? color = null)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color ?? Color.white;
            
            return obj;
        }

        private Color GetDifficultyColor(ChallengeDifficulty difficulty)
        {
            return difficulty switch
            {
                ChallengeDifficulty.Easy => new Color(0.3f, 0.5f, 0.3f),
                ChallengeDifficulty.Medium => new Color(0.5f, 0.4f, 0.2f),
                ChallengeDifficulty.Hard => new Color(0.5f, 0.25f, 0.2f),
                _ => new Color(0.3f, 0.3f, 0.3f)
            };
        }

        private string GetDifficultyLabel(ChallengeDifficulty difficulty)
        {
            return difficulty switch
            {
                ChallengeDifficulty.Easy => "EASY",
                ChallengeDifficulty.Medium => "MEDIUM",
                ChallengeDifficulty.Hard => "HARD",
                _ => "???"
            };
        }

        private string GetResourceIcon(ResourceType type)
        {
            return type switch
            {
                ResourceType.Gold => "💰",
                ResourceType.Stone => "🪨",
                ResourceType.Wood => "🪵",
                ResourceType.Iron => "⚙️",
                ResourceType.Crystal => "💎",
                ResourceType.ApexCoins => "🌟",
                _ => "📦"
            };
        }

        private string FormatNumber(int number)
        {
            if (number >= 1000000) return $"{number / 1000000f:F1}M";
            if (number >= 1000) return $"{number / 1000f:F1}K";
            return number.ToString("N0");
        }

        private string GetTimeRemaining(DateTime target)
        {
            TimeSpan remaining = target - DateTime.Now;
            if (remaining.TotalSeconds <= 0) return "Expired";
            if (remaining.TotalDays >= 1) return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
            return $"{remaining.Hours}h {remaining.Minutes}m";
        }

        #endregion

        #region Actions

        private void SetTab(ChallengeTab tab)
        {
            _currentTab = tab;
            CreateTabBar();
            RefreshContent();
        }

        private void ClaimStreakReward(int day)
        {
            var reward = _streakRewards.Find(r => r.Day == day);
            if (reward != null && !reward.IsClaimed && _currentStreak >= day)
            {
                reward.IsClaimed = true;
                OnStreakRewardClaimed?.Invoke(day);
                
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowSuccess($"Day {day} streak reward claimed!");
                }
                
                CreateStreakSection();
            }
        }

        private void ClaimChallenge(DailyChallenge challenge)
        {
            if (challenge.IsCompleted && !challenge.IsClaimed)
            {
                challenge.IsClaimed = true;
                OnChallengeCompleted?.Invoke(challenge);
                
                if (NotificationSystem.Instance != null)
                {
                    NotificationSystem.Instance.ShowSuccess($"Challenge completed: {challenge.Title}");
                }
                
                RefreshContent();
            }
        }

        private void ClaimAllDaily()
        {
            foreach (var challenge in _dailyChallenges)
            {
                if (challenge.IsCompleted && !challenge.IsClaimed)
                {
                    challenge.IsClaimed = true;
                    OnChallengeCompleted?.Invoke(challenge);
                }
            }
            
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowSuccess("All daily rewards claimed!");
            }
            
            RefreshContent();
        }

        private void GoToChallenge(DailyChallenge challenge)
        {
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"Opening {challenge.Category} menu...");
            }
            
            Debug.Log($"[DailyChallenges] Go to challenge: {challenge.Title}");
        }

        #endregion

        #region Public API

        public void Show()
        {
            _panel.SetActive(true);
            RefreshContent();
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        public void Toggle()
        {
            if (_panel.activeSelf) Hide();
            else Show();
        }

        public int GetCurrentStreak() => _currentStreak;
        public int GetDailyCompletedCount() => _dailyChallenges.FindAll(c => c.IsCompleted).Count;
        public int GetDailyClaimableCount() => _dailyChallenges.FindAll(c => c.IsCompleted && !c.IsClaimed).Count;

        public void UpdateChallengeProgress(ChallengeCategory category, int amount)
        {
            foreach (var challenge in _dailyChallenges)
            {
                if (challenge.Category == category && !challenge.IsCompleted)
                {
                    challenge.CurrentProgress = Mathf.Min(challenge.CurrentProgress + amount, challenge.RequiredProgress);
                    if (challenge.CurrentProgress >= challenge.RequiredProgress)
                    {
                        challenge.IsCompleted = true;
                        if (NotificationSystem.Instance != null)
                        {
                            NotificationSystem.Instance.ShowAlert("Challenge Complete!", challenge.Title);
                        }
                    }
                }
            }
        }

        #endregion
    }

    #region Data Classes

    public enum ChallengeTab
    {
        Daily,
        Weekly,
        Special,
        Achievements
    }

    public enum ChallengeCategory
    {
        Combat,
        Resources,
        Building,
        Military,
        Social,
        Research
    }

    public enum ChallengeDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    public class DailyChallenge
    {
        public string ChallengeId;
        public string Title;
        public string Description;
        public ChallengeCategory Category;
        public int CurrentProgress;
        public int RequiredProgress;
        public bool IsCompleted;
        public bool IsClaimed;
        public List<ChallengeReward> Rewards;
        public int ExperienceReward;
        public ChallengeDifficulty Difficulty;
        public string Icon;
    }

    public class WeeklyChallenge
    {
        public string ChallengeId;
        public string Title;
        public string Description;
        public int CurrentProgress;
        public int RequiredProgress;
        public bool IsClaimed;
        public List<ChallengeReward> Rewards;
        public int ExperienceReward;
        public string Icon;
    }

    public class ChallengeReward
    {
        public ResourceType Type;
        public int Amount;
    }

    public class StreakReward
    {
        public int Day;
        public ChallengeReward Reward;
        public string Icon;
        public bool IsClaimed;
        public bool IsMilestone;
    }

    #endregion
}
