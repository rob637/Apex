using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Spy & Intelligence Panel - Espionage system for scouting, sabotage, and intelligence gathering.
    /// Features spy management, intel reports, counter-intelligence, and covert operations.
    /// </summary>
    public class SpyIntelligencePanel : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.5f, 0.3f, 0.7f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color successColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color dangerColor = new Color(0.9f, 0.3f, 0.3f);
        [SerializeField] private Color warningColor = new Color(0.9f, 0.6f, 0.2f);
        
        // UI References
        private GameObject _panel;
        private GameObject _contentArea;
        private IntelTab _currentTab = IntelTab.Spies;
        
        // Spy data
        private List<SpyAgent> _spies = new List<SpyAgent>();
        private List<IntelReport> _intelReports = new List<IntelReport>();
        private List<CovertOperation> _activeOperations = new List<CovertOperation>();
        private int _maxSpies = 5;
        private int _counterIntelLevel = 3;
        private int _intelPoints = 1250;
        
        public static SpyIntelligencePanel Instance { get; private set; }
        
        public event Action<SpyAgent, CovertOperation> OnOperationStarted;
        public event Action<IntelReport> OnIntelGathered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeSpyData();
        }

        private void Start()
        {
            CreatePanel();
            Hide();
        }

        private void InitializeSpyData()
        {
            _spies = new List<SpyAgent>
            {
                new SpyAgent
                {
                    AgentId = "SPY001",
                    CodeName = "Shadow",
                    Level = 8,
                    Experience = 2450,
                    ExperienceToNext = 3000,
                    Specialty = SpySpecialty.Reconnaissance,
                    Status = SpyStatus.Available,
                    SuccessRate = 85,
                    MissionsCompleted = 23,
                    Skills = new Dictionary<string, int> { { "Stealth", 8 }, { "Observation", 9 }, { "Combat", 5 } }
                },
                new SpyAgent
                {
                    AgentId = "SPY002",
                    CodeName = "Viper",
                    Level = 6,
                    Experience = 1200,
                    ExperienceToNext = 2000,
                    Specialty = SpySpecialty.Sabotage,
                    Status = SpyStatus.OnMission,
                    CurrentMission = "Sabotage enemy barracks",
                    MissionEndTime = DateTime.Now.AddHours(2),
                    SuccessRate = 72,
                    MissionsCompleted = 15,
                    Skills = new Dictionary<string, int> { { "Stealth", 6 }, { "Demolition", 8 }, { "Combat", 7 } }
                },
                new SpyAgent
                {
                    AgentId = "SPY003",
                    CodeName = "Whisper",
                    Level = 10,
                    Experience = 4800,
                    ExperienceToNext = 5000,
                    Specialty = SpySpecialty.Assassination,
                    Status = SpyStatus.Available,
                    SuccessRate = 92,
                    MissionsCompleted = 45,
                    Skills = new Dictionary<string, int> { { "Stealth", 10 }, { "Combat", 9 }, { "Poison", 8 } }
                },
                new SpyAgent
                {
                    AgentId = "SPY004",
                    CodeName = "Ghost",
                    Level = 4,
                    Experience = 650,
                    ExperienceToNext = 1000,
                    Specialty = SpySpecialty.CounterIntelligence,
                    Status = SpyStatus.Recovering,
                    RecoveryEndTime = DateTime.Now.AddHours(6),
                    SuccessRate = 65,
                    MissionsCompleted = 8,
                    Skills = new Dictionary<string, int> { { "Detection", 7 }, { "Interrogation", 5 }, { "Combat", 4 } }
                }
            };
            
            _intelReports = new List<IntelReport>
            {
                new IntelReport
                {
                    ReportId = "IR001",
                    TargetName = "ShadowLord",
                    TargetGuild = "Shadow Legion",
                    ReportType = IntelType.TroopCount,
                    Timestamp = DateTime.Now.AddHours(-3),
                    Accuracy = 85,
                    Content = "Enemy forces estimated at 45,000 troops. Heavy cavalry presence.",
                    IsNew = true
                },
                new IntelReport
                {
                    ReportId = "IR002",
                    TargetName = "Dark Fortress",
                    ReportType = IntelType.DefenseLayout,
                    Timestamp = DateTime.Now.AddHours(-8),
                    Accuracy = 72,
                    Content = "Level 18 walls. 4 arrow towers, 2 cannon emplacements. Weak eastern flank.",
                    IsNew = true
                },
                new IntelReport
                {
                    ReportId = "IR003",
                    TargetName = "IronFist",
                    TargetGuild = "Shadow Legion",
                    ReportType = IntelType.ResourceLevel,
                    Timestamp = DateTime.Now.AddDays(-1),
                    Accuracy = 90,
                    Content = "Gold reserves low (~50K). Crystal stockpile substantial. Likely preparing upgrade.",
                    IsNew = false
                },
                new IntelReport
                {
                    ReportId = "IR004",
                    TargetName = "Shadow Legion",
                    ReportType = IntelType.AttackPlan,
                    Timestamp = DateTime.Now.AddDays(-2),
                    Accuracy = 60,
                    Content = "Intercepted communications suggest attack on northern territories within 48h.",
                    IsNew = false,
                    IsUrgent = true
                }
            };
            
            _activeOperations = new List<CovertOperation>
            {
                new CovertOperation
                {
                    OperationId = "OP001",
                    Name = "Operation Blackout",
                    Type = OperationType.Sabotage,
                    TargetName = "Enemy Barracks",
                    AssignedAgent = _spies[1],
                    StartTime = DateTime.Now.AddHours(-1),
                    EndTime = DateTime.Now.AddHours(2),
                    SuccessChance = 72,
                    Status = OperationStatus.InProgress
                }
            };
        }

        private void CreatePanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            _panel = new GameObject("SpyIntelligencePanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.12f, 0.08f);
            rect.anchorMax = new Vector2(0.88f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.04f, 0.08f, 0.98f);
            
            VerticalLayoutGroup layout = _panel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 0;
            
            CreateHeader();
            CreateTabBar();
            CreateContentArea();
        }

        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 70;
            
            Image bg = header.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.06f, 0.12f);
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 25;
            hlayout.padding = new RectOffset(25, 25, 10, 10);
            
            // Title
            CreateText(header.transform, "🕵️ INTELLIGENCE AGENCY", 22, TextAlignmentOptions.Left, accentColor);
            
            // Spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(header.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Stats
            CreateHeaderStats(header.transform);
            
            // Close button
            CreateCloseButton(header.transform);
        }

        private void CreateHeaderStats(Transform parent)
        {
            // Spies available
            int available = _spies.FindAll(s => s.Status == SpyStatus.Available).Count;
            CreateStatBadge(parent, "🕵️", $"{available}/{_maxSpies}", "Agents");
            
            // Intel points
            CreateStatBadge(parent, "📊", _intelPoints.ToString(), "Intel");
            
            // Counter-intel level
            CreateStatBadge(parent, "🛡️", $"Lv.{_counterIntelLevel}", "Defense");
        }

        private void CreateStatBadge(Transform parent, string icon, string value, string label)
        {
            GameObject badge = new GameObject("StatBadge");
            badge.transform.SetParent(parent, false);
            
            LayoutElement le = badge.AddComponent<LayoutElement>();
            le.preferredWidth = 90;
            le.preferredHeight = 50;
            
            Image bg = badge.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.15f);
            
            VerticalLayoutGroup vlayout = badge.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(5, 5, 5, 5);
            
            GameObject row = new GameObject("Row");
            row.transform.SetParent(badge.transform, false);
            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.spacing = 5;
            
            CreateText(row.transform, icon, 14, TextAlignmentOptions.Center);
            CreateText(row.transform, value, 14, TextAlignmentOptions.Center, goldColor);
            
            CreateText(badge.transform, label, 9, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject btn = new GameObject("CloseBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 40;
            le.preferredHeight = 40;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = new Color(0.5f, 0.2f, 0.3f);
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(Hide);
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = "✕";
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateTabBar()
        {
            GameObject tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = tabBar.AddComponent<LayoutElement>();
            le.preferredHeight = 45;
            
            Image bg = tabBar.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.03f, 0.06f);
            
            HorizontalLayoutGroup hlayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 5;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            CreateTab(tabBar.transform, IntelTab.Spies, "🕵️ Agents");
            CreateTab(tabBar.transform, IntelTab.Operations, "🎯 Operations");
            CreateTab(tabBar.transform, IntelTab.Reports, "📋 Intel Reports");
            CreateTab(tabBar.transform, IntelTab.CounterIntel, "🛡️ Counter-Intel");
            CreateTab(tabBar.transform, IntelTab.Training, "📚 Training");
        }

        private void CreateTab(Transform parent, IntelTab tab, string label)
        {
            GameObject tabObj = new GameObject($"Tab_{tab}");
            tabObj.transform.SetParent(parent, false);
            
            bool isActive = tab == _currentTab;
            Color bgColor = isActive ? accentColor : new Color(0.1f, 0.08f, 0.12f);
            
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
            bg.color = new Color(0.03f, 0.02f, 0.05f);
            
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
                case IntelTab.Spies:
                    CreateSpiesContent();
                    break;
                case IntelTab.Operations:
                    CreateOperationsContent();
                    break;
                case IntelTab.Reports:
                    CreateReportsContent();
                    break;
                case IntelTab.CounterIntel:
                    CreateCounterIntelContent();
                    break;
                case IntelTab.Training:
                    CreateTrainingContent();
                    break;
            }
        }

        private void CreateSpiesContent()
        {
            GameObject scrollView = CreateScrollView(_contentArea.transform);
            GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;
            
            // Recruit button if under max
            if (_spies.Count < _maxSpies)
            {
                CreateRecruitButton(content.transform);
            }
            
            // Spy cards
            foreach (var spy in _spies)
            {
                CreateSpyCard(content.transform, spy);
            }
        }

        private void CreateRecruitButton(Transform parent)
        {
            GameObject btn = new GameObject("RecruitBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.1f, 0.2f);
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(RecruitNewSpy);
            
            HorizontalLayoutGroup hlayout = btn.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            
            CreateText(btn.transform, "➕", 24, TextAlignmentOptions.Center, accentColor);
            CreateText(btn.transform, "RECRUIT NEW AGENT", 14, TextAlignmentOptions.Center, Color.white);
            CreateText(btn.transform, "💰 10,000 Gold", 12, TextAlignmentOptions.Center, goldColor);
        }

        private void CreateSpyCard(Transform parent, SpyAgent spy)
        {
            GameObject card = new GameObject($"SpyCard_{spy.AgentId}");
            card.transform.SetParent(parent, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 140;
            
            Color bgColor = spy.Status switch
            {
                SpyStatus.Available => new Color(0.08f, 0.1f, 0.08f),
                SpyStatus.OnMission => new Color(0.1f, 0.08f, 0.05f),
                SpyStatus.Recovering => new Color(0.1f, 0.05f, 0.05f),
                _ => new Color(0.06f, 0.06f, 0.06f)
            };
            
            Image bg = card.AddComponent<Image>();
            bg.color = bgColor;
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 20;
            hlayout.padding = new RectOffset(20, 20, 15, 15);
            
            // Spy portrait/icon
            CreateSpyPortrait(card.transform, spy);
            
            // Info section
            CreateSpyInfo(card.transform, spy);
            
            // Skills
            CreateSpySkills(card.transform, spy);
            
            // Actions
            CreateSpyActions(card.transform, spy);
        }

        private void CreateSpyPortrait(Transform parent, SpyAgent spy)
        {
            GameObject portrait = new GameObject("Portrait");
            portrait.transform.SetParent(parent, false);
            
            LayoutElement le = portrait.AddComponent<LayoutElement>();
            le.preferredWidth = 80;
            le.preferredHeight = 100;
            
            Image bg = portrait.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.12f, 0.2f);
            
            VerticalLayoutGroup vlayout = portrait.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 5;
            
            string icon = spy.Specialty switch
            {
                SpySpecialty.Reconnaissance => "👁️",
                SpySpecialty.Sabotage => "💣",
                SpySpecialty.Assassination => "🗡️",
                SpySpecialty.CounterIntelligence => "🛡️",
                _ => "🕵️"
            };
            
            CreateText(portrait.transform, icon, 32, TextAlignmentOptions.Center);
            CreateText(portrait.transform, $"Lv.{spy.Level}", 12, TextAlignmentOptions.Center, accentColor);
            
            // Status indicator
            Color statusColor = spy.Status switch
            {
                SpyStatus.Available => successColor,
                SpyStatus.OnMission => warningColor,
                SpyStatus.Recovering => dangerColor,
                _ => new Color(0.5f, 0.5f, 0.5f)
            };
            CreateText(portrait.transform, "●", 14, TextAlignmentOptions.Center, statusColor);
        }

        private void CreateSpyInfo(Transform parent, SpyAgent spy)
        {
            GameObject info = new GameObject("Info");
            info.transform.SetParent(parent, false);
            
            LayoutElement le = info.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            VerticalLayoutGroup vlayout = info.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleLeft;
            vlayout.spacing = 4;
            
            // Code name
            CreateText(info.transform, $"Agent \"{spy.CodeName}\"", 16, TextAlignmentOptions.Left, Color.white);
            
            // Specialty
            CreateText(info.transform, spy.Specialty.ToString(), 11, TextAlignmentOptions.Left, accentColor);
            
            // Status
            string statusText = spy.Status switch
            {
                SpyStatus.Available => "Ready for deployment",
                SpyStatus.OnMission => $"On mission: {GetTimeRemaining(spy.MissionEndTime)}",
                SpyStatus.Recovering => $"Recovering: {GetTimeRemaining(spy.RecoveryEndTime)}",
                _ => "Unknown"
            };
            CreateText(info.transform, statusText, 10, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            // Success rate and missions
            CreateText(info.transform, $"✓ {spy.SuccessRate}% success | {spy.MissionsCompleted} missions", 10, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
            
            // XP bar
            CreateXPBar(info.transform, spy);
        }

        private void CreateXPBar(Transform parent, SpyAgent spy)
        {
            GameObject xpContainer = new GameObject("XPContainer");
            xpContainer.transform.SetParent(parent, false);
            
            HorizontalLayoutGroup hlayout = xpContainer.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleLeft;
            hlayout.spacing = 8;
            
            GameObject bar = new GameObject("XPBar");
            bar.transform.SetParent(xpContainer.transform, false);
            
            LayoutElement le = bar.AddComponent<LayoutElement>();
            le.preferredWidth = 100;
            le.preferredHeight = 8;
            
            Image bgImg = bar.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f);
            
            float progress = (float)spy.Experience / spy.ExperienceToNext;
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.3f, 0.6f, 0.9f);
            
            CreateText(xpContainer.transform, $"{spy.Experience}/{spy.ExperienceToNext} XP", 8, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
        }

        private void CreateSpySkills(Transform parent, SpyAgent spy)
        {
            GameObject skills = new GameObject("Skills");
            skills.transform.SetParent(parent, false);
            
            LayoutElement le = skills.AddComponent<LayoutElement>();
            le.preferredWidth = 100;
            
            VerticalLayoutGroup vlayout = skills.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.spacing = 3;
            
            CreateText(skills.transform, "Skills", 10, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            
            foreach (var skill in spy.Skills)
            {
                CreateSkillBar(skills.transform, skill.Key, skill.Value);
            }
        }

        private void CreateSkillBar(Transform parent, string skillName, int level)
        {
            GameObject skill = new GameObject($"Skill_{skillName}");
            skill.transform.SetParent(parent, false);
            
            HorizontalLayoutGroup hlayout = skill.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 5;
            
            CreateText(skill.transform, skillName.Substring(0, 3), 8, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            // Skill dots
            for (int i = 0; i < 10; i++)
            {
                GameObject dot = new GameObject("Dot");
                dot.transform.SetParent(skill.transform, false);
                
                LayoutElement le = dot.AddComponent<LayoutElement>();
                le.preferredWidth = 6;
                le.preferredHeight = 6;
                
                Image dotImg = dot.AddComponent<Image>();
                dotImg.color = i < level ? accentColor : new Color(0.2f, 0.2f, 0.2f);
            }
        }

        private void CreateSpyActions(Transform parent, SpyAgent spy)
        {
            GameObject actions = new GameObject("Actions");
            actions.transform.SetParent(parent, false);
            
            LayoutElement le = actions.AddComponent<LayoutElement>();
            le.preferredWidth = 100;
            
            VerticalLayoutGroup vlayout = actions.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 8;
            
            if (spy.Status == SpyStatus.Available)
            {
                CreateActionButton(actions.transform, "🎯 Deploy", () => DeploySpy(spy), accentColor);
                CreateActionButton(actions.transform, "📚 Train", () => TrainSpy(spy), new Color(0.3f, 0.4f, 0.5f));
            }
            else if (spy.Status == SpyStatus.OnMission)
            {
                CreateActionButton(actions.transform, "📡 Track", () => TrackMission(spy), warningColor);
            }
            else
            {
                CreateActionButton(actions.transform, "💊 Speed Up", () => SpeedUpRecovery(spy), new Color(0.4f, 0.3f, 0.3f));
            }
        }

        private void CreateActionButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btn = new GameObject("ActionBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 10;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateOperationsContent()
        {
            GameObject scrollView = CreateScrollView(_contentArea.transform);
            GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;
            
            // Active operations
            CreateSectionHeader(content.transform, "🎯 ACTIVE OPERATIONS");
            
            if (_activeOperations.Count > 0)
            {
                foreach (var op in _activeOperations)
                {
                    CreateOperationCard(content.transform, op);
                }
            }
            else
            {
                CreateText(content.transform, "No active operations. Deploy an agent to start.", 12, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            }
            
            // Available operations
            CreateSectionHeader(content.transform, "📋 AVAILABLE MISSIONS");
            
            CreateMissionOption(content.transform, "Scout Enemy Base", OperationType.Reconnaissance, "Gather intel on enemy forces", 2, 65);
            CreateMissionOption(content.transform, "Sabotage Production", OperationType.Sabotage, "Destroy enemy resource buildings", 4, 55);
            CreateMissionOption(content.transform, "Steal Resources", OperationType.Theft, "Pilfer enemy gold reserves", 3, 60);
            CreateMissionOption(content.transform, "Assassinate Commander", OperationType.Assassination, "Eliminate enemy hero", 6, 40);
        }

        private void CreateOperationCard(Transform parent, CovertOperation op)
        {
            GameObject card = new GameObject($"Operation_{op.OperationId}");
            card.transform.SetParent(parent, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.05f);
            
            VerticalLayoutGroup vlayout = card.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.spacing = 8;
            vlayout.padding = new RectOffset(20, 20, 15, 15);
            
            // Header
            GameObject header = new GameObject("Header");
            header.transform.SetParent(card.transform, false);
            
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.spacing = 15;
            
            string typeIcon = op.Type switch
            {
                OperationType.Reconnaissance => "👁️",
                OperationType.Sabotage => "💣",
                OperationType.Theft => "💰",
                OperationType.Assassination => "🗡️",
                _ => "🎯"
            };
            
            CreateText(header.transform, typeIcon, 20, TextAlignmentOptions.Center);
            CreateText(header.transform, op.Name, 14, TextAlignmentOptions.Left, Color.white);
            
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(header.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            CreateText(header.transform, $"Agent: {op.AssignedAgent.CodeName}", 11, TextAlignmentOptions.Right, accentColor);
            
            // Progress
            CreateText(card.transform, $"Target: {op.TargetName}", 11, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            // Time remaining
            TimeSpan remaining = op.EndTime - DateTime.Now;
            float progress = 1f - (float)(remaining.TotalSeconds / (op.EndTime - op.StartTime).TotalSeconds);
            CreateProgressBar(card.transform, progress, GetTimeRemaining(op.EndTime));
            
            // Success chance
            CreateText(card.transform, $"Success chance: {op.SuccessChance}%", 10, TextAlignmentOptions.Center, GetSuccessColor(op.SuccessChance));
        }

        private void CreateMissionOption(Transform parent, string name, OperationType type, string description, int hours, int baseSuccess)
        {
            GameObject option = new GameObject($"Mission_{name}");
            option.transform.SetParent(parent, false);
            
            LayoutElement le = option.AddComponent<LayoutElement>();
            le.preferredHeight = 70;
            
            Image bg = option.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.05f, 0.08f);
            
            HorizontalLayoutGroup hlayout = option.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            // Type icon
            string icon = type switch
            {
                OperationType.Reconnaissance => "👁️",
                OperationType.Sabotage => "💣",
                OperationType.Theft => "💰",
                OperationType.Assassination => "🗡️",
                _ => "🎯"
            };
            CreateText(option.transform, icon, 24, TextAlignmentOptions.Center);
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(option.transform, false);
            info.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            VerticalLayoutGroup infoLayout = info.AddComponent<VerticalLayoutGroup>();
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            infoLayout.spacing = 2;
            
            CreateText(info.transform, name, 13, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, description, 10, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
            CreateText(info.transform, $"⏱️ {hours}h | Base success: {baseSuccess}%", 9, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            // Start button
            CreateMissionButton(option.transform, type);
        }

        private void CreateMissionButton(Transform parent, OperationType type)
        {
            int availableSpies = _spies.FindAll(s => s.Status == SpyStatus.Available).Count;
            bool canStart = availableSpies > 0;
            
            GameObject btn = new GameObject("StartBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 80;
            le.preferredHeight = 40;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = canStart ? accentColor : new Color(0.3f, 0.3f, 0.3f);
            
            if (canStart)
            {
                Button button = btn.AddComponent<Button>();
                button.onClick.AddListener(() => StartMission(type));
            }
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = canStart ? "Start" : "No Agents";
            text.fontSize = 11;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateReportsContent()
        {
            GameObject scrollView = CreateScrollView(_contentArea.transform);
            GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;
            
            // New reports
            var newReports = _intelReports.FindAll(r => r.IsNew);
            if (newReports.Count > 0)
            {
                CreateSectionHeader(content.transform, $"🆕 NEW INTEL ({newReports.Count})");
                foreach (var report in newReports)
                {
                    CreateReportCard(content.transform, report);
                }
            }
            
            // All reports
            CreateSectionHeader(content.transform, "📋 ALL INTELLIGENCE");
            foreach (var report in _intelReports)
            {
                if (!report.IsNew)
                    CreateReportCard(content.transform, report);
            }
        }

        private void CreateReportCard(Transform parent, IntelReport report)
        {
            GameObject card = new GameObject($"Report_{report.ReportId}");
            card.transform.SetParent(parent, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 90;
            
            Color bgColor = report.IsNew ? new Color(0.1f, 0.08f, 0.12f) : 
                           report.IsUrgent ? new Color(0.15f, 0.08f, 0.08f) : 
                           new Color(0.06f, 0.05f, 0.08f);
            
            Image bg = card.AddComponent<Image>();
            bg.color = bgColor;
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(20, 20, 15, 15);
            
            // Type icon
            string icon = report.ReportType switch
            {
                IntelType.TroopCount => "⚔️",
                IntelType.DefenseLayout => "🏰",
                IntelType.ResourceLevel => "💰",
                IntelType.AttackPlan => "📜",
                _ => "📊"
            };
            
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(card.transform, false);
            iconObj.AddComponent<LayoutElement>().preferredWidth = 50;
            
            VerticalLayoutGroup iconLayout = iconObj.AddComponent<VerticalLayoutGroup>();
            iconLayout.childAlignment = TextAnchor.MiddleCenter;
            
            CreateText(iconObj.transform, icon, 24, TextAlignmentOptions.Center);
            CreateText(iconObj.transform, $"{report.Accuracy}%", 10, TextAlignmentOptions.Center, GetAccuracyColor(report.Accuracy));
            
            // Content
            GameObject info = new GameObject("Info");
            info.transform.SetParent(card.transform, false);
            info.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            VerticalLayoutGroup infoLayout = info.AddComponent<VerticalLayoutGroup>();
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            infoLayout.spacing = 3;
            
            // Header with target
            GameObject headerRow = new GameObject("Header");
            headerRow.transform.SetParent(info.transform, false);
            HorizontalLayoutGroup headerLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 10;
            
            CreateText(headerRow.transform, report.TargetName, 13, TextAlignmentOptions.Left, Color.white);
            if (!string.IsNullOrEmpty(report.TargetGuild))
            {
                CreateText(headerRow.transform, $"[{report.TargetGuild}]", 10, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            }
            if (report.IsUrgent)
            {
                CreateText(headerRow.transform, "⚠️ URGENT", 10, TextAlignmentOptions.Left, dangerColor);
            }
            
            CreateText(info.transform, report.Content, 10, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f));
            CreateText(info.transform, GetTimeAgo(report.Timestamp), 9, TextAlignmentOptions.Left, new Color(0.4f, 0.4f, 0.4f));
            
            // Actions
            if (report.IsNew)
            {
                CreateReportAction(card.transform, report);
            }
        }

        private void CreateReportAction(Transform parent, IntelReport report)
        {
            GameObject btn = new GameObject("MarkRead");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 70;
            le.preferredHeight = 30;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.25f, 0.35f);
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => MarkReportRead(report));
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = "✓ Read";
            text.fontSize = 10;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateCounterIntelContent()
        {
            GameObject scrollView = CreateScrollView(_contentArea.transform);
            GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;
            
            // Counter-intelligence status
            CreateCounterIntelStatus(content.transform);
            
            // Detected threats
            CreateSectionHeader(content.transform, "⚠️ DETECTED THREATS");
            CreateThreatAlert(content.transform, "Enemy spy detected near barracks", "High", DateTime.Now.AddMinutes(-30));
            CreateThreatAlert(content.transform, "Suspicious activity at treasury", "Medium", DateTime.Now.AddHours(-2));
            
            // Upgrades
            CreateSectionHeader(content.transform, "🛡️ DEFENSE UPGRADES");
            CreateCounterIntelUpgrade(content.transform, "Watchtower Network", 3, 5, "Increases spy detection range");
            CreateCounterIntelUpgrade(content.transform, "Secret Police", 2, 5, "Faster threat response time");
            CreateCounterIntelUpgrade(content.transform, "Cipher Division", 1, 5, "Protects against intel theft");
        }

        private void CreateCounterIntelStatus(Transform parent)
        {
            GameObject status = new GameObject("CounterIntelStatus");
            status.transform.SetParent(parent, false);
            
            LayoutElement le = status.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            Image bg = status.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.08f);
            
            HorizontalLayoutGroup hlayout = status.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 40;
            hlayout.padding = new RectOffset(30, 30, 20, 20);
            
            CreateStatBox(status.transform, "🛡️", $"Level {_counterIntelLevel}", "Defense Rating");
            CreateStatBox(status.transform, "👁️", "85%", "Detection Rate");
            CreateStatBox(status.transform, "⚡", "2.5s", "Response Time");
            CreateStatBox(status.transform, "🔒", "12", "Threats Blocked");
        }

        private void CreateStatBox(Transform parent, string icon, string value, string label)
        {
            GameObject box = new GameObject("StatBox");
            box.transform.SetParent(parent, false);
            
            VerticalLayoutGroup vlayout = box.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.spacing = 3;
            
            CreateText(box.transform, icon, 24, TextAlignmentOptions.Center);
            CreateText(box.transform, value, 16, TextAlignmentOptions.Center, goldColor);
            CreateText(box.transform, label, 10, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
        }

        private void CreateThreatAlert(Transform parent, string description, string severity, DateTime time)
        {
            GameObject alert = new GameObject("ThreatAlert");
            alert.transform.SetParent(parent, false);
            
            LayoutElement le = alert.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            Color bgColor = severity == "High" ? new Color(0.15f, 0.08f, 0.08f) : new Color(0.12f, 0.1f, 0.06f);
            
            Image bg = alert.AddComponent<Image>();
            bg.color = bgColor;
            
            HorizontalLayoutGroup hlayout = alert.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            Color severityColor = severity == "High" ? dangerColor : warningColor;
            CreateText(alert.transform, "⚠️", 18, TextAlignmentOptions.Center, severityColor);
            
            GameObject info = new GameObject("Info");
            info.transform.SetParent(alert.transform, false);
            info.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            VerticalLayoutGroup infoLayout = info.AddComponent<VerticalLayoutGroup>();
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            
            CreateText(info.transform, description, 11, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, $"{severity} severity • {GetTimeAgo(time)}", 9, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
        }

        private void CreateCounterIntelUpgrade(Transform parent, string name, int current, int max, string description)
        {
            GameObject upgrade = new GameObject($"Upgrade_{name}");
            upgrade.transform.SetParent(parent, false);
            
            LayoutElement le = upgrade.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            Image bg = upgrade.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.05f, 0.08f);
            
            HorizontalLayoutGroup hlayout = upgrade.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(upgrade.transform, false);
            info.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            VerticalLayoutGroup infoLayout = info.AddComponent<VerticalLayoutGroup>();
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            
            CreateText(info.transform, $"{name} ({current}/{max})", 12, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, description, 10, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
            
            // Level indicator
            CreateLevelDots(upgrade.transform, current, max);
            
            // Upgrade button
            if (current < max)
            {
                CreateUpgradeButton(upgrade.transform, name);
            }
        }

        private void CreateLevelDots(Transform parent, int current, int max)
        {
            GameObject dots = new GameObject("LevelDots");
            dots.transform.SetParent(parent, false);
            
            HorizontalLayoutGroup hlayout = dots.AddComponent<HorizontalLayoutGroup>();
            hlayout.spacing = 3;
            
            for (int i = 0; i < max; i++)
            {
                GameObject dot = new GameObject("Dot");
                dot.transform.SetParent(dots.transform, false);
                
                LayoutElement le = dot.AddComponent<LayoutElement>();
                le.preferredWidth = 10;
                le.preferredHeight = 10;
                
                Image img = dot.AddComponent<Image>();
                img.color = i < current ? accentColor : new Color(0.2f, 0.2f, 0.2f);
            }
        }

        private void CreateUpgradeButton(Transform parent, string upgradeName)
        {
            GameObject btn = new GameObject("UpgradeBtn");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 80;
            le.preferredHeight = 35;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = accentColor;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => UpgradeCounterIntel(upgradeName));
            
            TextMeshProUGUI text = btn.AddComponent<TextMeshProUGUI>();
            text.text = "Upgrade";
            text.fontSize = 10;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateTrainingContent()
        {
            GameObject scrollView = CreateScrollView(_contentArea.transform);
            GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;
            
            CreateSectionHeader(content.transform, "📚 TRAINING PROGRAMS");
            
            CreateTrainingOption(content.transform, "Stealth Training", "Increases stealth skill", 500, 4);
            CreateTrainingOption(content.transform, "Combat Training", "Increases combat skill", 500, 4);
            CreateTrainingOption(content.transform, "Observation Course", "Increases observation skill", 400, 3);
            CreateTrainingOption(content.transform, "Demolition Expert", "Unlocks sabotage specialty", 1000, 8);
            CreateTrainingOption(content.transform, "Master Assassin", "Unlocks assassination specialty", 2000, 12);
        }

        private void CreateTrainingOption(Transform parent, string name, string description, int cost, int hours)
        {
            GameObject option = new GameObject($"Training_{name}");
            option.transform.SetParent(parent, false);
            
            LayoutElement le = option.AddComponent<LayoutElement>();
            le.preferredHeight = 70;
            
            Image bg = option.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.05f, 0.08f);
            
            HorizontalLayoutGroup hlayout = option.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            CreateText(option.transform, "📚", 24, TextAlignmentOptions.Center);
            
            GameObject info = new GameObject("Info");
            info.transform.SetParent(option.transform, false);
            info.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            VerticalLayoutGroup infoLayout = info.AddComponent<VerticalLayoutGroup>();
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            
            CreateText(info.transform, name, 13, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, description, 10, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
            CreateText(info.transform, $"💰 {cost} Gold | ⏱️ {hours}h", 9, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            GameObject btn = new GameObject("TrainBtn");
            btn.transform.SetParent(option.transform, false);
            
            LayoutElement btnLE = btn.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 70;
            btnLE.preferredHeight = 35;
            
            Image btnBg = btn.AddComponent<Image>();
            btnBg.color = new Color(0.3f, 0.4f, 0.5f);
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => StartTraining(name));
            
            TextMeshProUGUI btnText = btn.AddComponent<TextMeshProUGUI>();
            btnText.text = "Train";
            btnText.fontSize = 10;
            btnText.alignment = TextAlignmentOptions.Center;
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

        private void CreateProgressBar(Transform parent, float progress, string label)
        {
            GameObject container = new GameObject("ProgressContainer");
            container.transform.SetParent(parent, false);
            
            HorizontalLayoutGroup hlayout = container.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            
            GameObject bar = new GameObject("Bar");
            bar.transform.SetParent(container.transform, false);
            
            LayoutElement le = bar.AddComponent<LayoutElement>();
            le.preferredWidth = 200;
            le.preferredHeight = 12;
            le.flexibleWidth = 1;
            
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
            
            CreateText(container.transform, label, 10, TextAlignmentOptions.Right, new Color(0.6f, 0.6f, 0.6f));
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

        private string GetTimeRemaining(DateTime target)
        {
            TimeSpan remaining = target - DateTime.Now;
            if (remaining.TotalSeconds <= 0) return "Complete";
            if (remaining.TotalHours >= 1) return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
            return $"{remaining.Minutes}m {remaining.Seconds}s";
        }

        private string GetTimeAgo(DateTime time)
        {
            TimeSpan diff = DateTime.Now - time;
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            return $"{(int)diff.TotalDays}d ago";
        }

        private Color GetSuccessColor(int chance)
        {
            if (chance >= 80) return successColor;
            if (chance >= 50) return warningColor;
            return dangerColor;
        }

        private Color GetAccuracyColor(int accuracy)
        {
            if (accuracy >= 80) return successColor;
            if (accuracy >= 60) return warningColor;
            return dangerColor;
        }

        #endregion

        #region Actions

        private void SetTab(IntelTab tab)
        {
            _currentTab = tab;
            CreateTabBar();
            RefreshContent();
        }

        private void RecruitNewSpy()
        {
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo("Opening spy recruitment...");
            }
        }

        private void DeploySpy(SpyAgent spy)
        {
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"Select mission for Agent {spy.CodeName}");
            }
            SetTab(IntelTab.Operations);
        }

        private void TrainSpy(SpyAgent spy)
        {
            SetTab(IntelTab.Training);
        }

        private void TrackMission(SpyAgent spy)
        {
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"Tracking Agent {spy.CodeName}'s mission...");
            }
        }

        private void SpeedUpRecovery(SpyAgent spy)
        {
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo("Speed up with 50 Apex Coins?");
            }
        }

        private void StartMission(OperationType type)
        {
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"Select an agent for {type} mission");
            }
        }

        private void MarkReportRead(IntelReport report)
        {
            report.IsNew = false;
            OnIntelGathered?.Invoke(report);
            RefreshContent();
        }

        private void UpgradeCounterIntel(string upgradeName)
        {
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"Upgrading {upgradeName}...");
            }
        }

        private void StartTraining(string trainingName)
        {
            if (NotificationSystem.Instance != null)
            {
                NotificationSystem.Instance.ShowInfo($"Starting {trainingName}...");
            }
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

        public int GetAvailableSpyCount() => _spies.FindAll(s => s.Status == SpyStatus.Available).Count;
        public int GetNewIntelCount() => _intelReports.FindAll(r => r.IsNew).Count;
        public int GetActiveOperationCount() => _activeOperations.Count;

        #endregion
    }

    #region Data Classes

    public enum IntelTab
    {
        Spies,
        Operations,
        Reports,
        CounterIntel,
        Training
    }

    public enum SpySpecialty
    {
        Reconnaissance,
        Sabotage,
        Assassination,
        CounterIntelligence
    }

    public enum SpyStatus
    {
        Available,
        OnMission,
        Recovering,
        Captured
    }

    public enum IntelType
    {
        TroopCount,
        DefenseLayout,
        ResourceLevel,
        AttackPlan
    }

    public enum OperationType
    {
        Reconnaissance,
        Sabotage,
        Theft,
        Assassination
    }

    public enum OperationStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }

    public class SpyAgent
    {
        public string AgentId;
        public string CodeName;
        public int Level;
        public int Experience;
        public int ExperienceToNext;
        public SpySpecialty Specialty;
        public SpyStatus Status;
        public string CurrentMission;
        public DateTime MissionEndTime;
        public DateTime RecoveryEndTime;
        public int SuccessRate;
        public int MissionsCompleted;
        public Dictionary<string, int> Skills;
    }

    public class IntelReport
    {
        public string ReportId;
        public string TargetName;
        public string TargetGuild;
        public IntelType ReportType;
        public DateTime Timestamp;
        public int Accuracy;
        public string Content;
        public bool IsNew;
        public bool IsUrgent;
    }

    public class CovertOperation
    {
        public string OperationId;
        public string Name;
        public OperationType Type;
        public string TargetName;
        public SpyAgent AssignedAgent;
        public DateTime StartTime;
        public DateTime EndTime;
        public int SuccessChance;
        public OperationStatus Status;
    }

    #endregion
}
