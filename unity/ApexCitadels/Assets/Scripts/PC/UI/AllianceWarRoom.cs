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
    /// Alliance War Room - Coordinate alliance wars, view war status, and manage alliance combat.
    /// Critical for multiplayer engagement and alliance gameplay.
    /// </summary>
    public class AllianceWarRoom : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color accentColor = new Color(0.3f, 0.6f, 0.9f);
        [SerializeField] private Color allyColor = new Color(0.2f, 0.7f, 0.4f);
        [SerializeField] private Color enemyColor = new Color(0.8f, 0.3f, 0.3f);
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _contentContainer;
        private WarRoomTab _selectedTab = WarRoomTab.Overview;
        private Dictionary<WarRoomTab, GameObject> _tabs = new Dictionary<WarRoomTab, GameObject>();
        
        // War data
        private WarRoomData _currentWar;
        private List<WarRoomData> _warHistory = new List<WarRoomData>();
        
        public static AllianceWarRoom Instance { get; private set; }
        
        public event Action<WarRoomData> OnWarDeclared;
        public event Action<WarRoomData> OnWarEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadDemoData();
        }

        private void Start()
        {
            CreateWarRoomPanel();
            Hide();
        }

        private void LoadDemoData()
        {
            // Demo active war
            _currentWar = new WarRoomData
            {
                WarId = "WAR_001",
                AttackingAlliance = new AllianceInfo { Name = "Steel Legion", Tag = "STEEL", MemberCount = 25, Power = 2500000 },
                DefendingAlliance = new AllianceInfo { Name = "Shadow Empire", Tag = "SHADE", MemberCount = 22, Power = 2200000 },
                StartTime = DateTime.Now.AddHours(-18),
                EndTime = DateTime.Now.AddHours(30),
                Phase = WarPhase.Active,
                AttackerScore = 1250,
                DefenderScore = 980,
                TotalBattles = 45,
                AttackerWins = 26,
                DefenderWins = 19
            };
            
            // War history
            _warHistory.Add(new WarRoomData
            {
                WarId = "WAR_000",
                AttackingAlliance = new AllianceInfo { Name = "Steel Legion", Tag = "STEEL" },
                DefendingAlliance = new AllianceInfo { Name = "Iron Fist", Tag = "IRON" },
                Phase = WarPhase.Ended,
                Winner = "Steel Legion",
                AttackerScore = 2500,
                DefenderScore = 1800,
                StartTime = DateTime.Now.AddDays(-5),
                EndTime = DateTime.Now.AddDays(-3)
            });
        }

        private void CreateWarRoomPanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("AllianceWarRoom");
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
            
            // War status bar
            CreateWarStatusBar();
            
            // Tabs
            CreateTabs();
            
            // Content
            CreateContentArea();
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
            GameObject title = new GameObject("Title");
            title.transform.SetParent(header.transform, false);
            
            TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.text = "[!] ALLIANCE WAR ROOM";
            titleText.fontSize = 26;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = accentColor;
            
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
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(closeBtn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI x = textObj.AddComponent<TextMeshProUGUI>();
            x.text = "[X]";
            x.fontSize = 24;
            x.alignment = TextAlignmentOptions.Center;
        }

        private void CreateWarStatusBar()
        {
            GameObject statusBar = new GameObject("WarStatusBar");
            statusBar.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = statusBar.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            Image bg = statusBar.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.18f, 0.9f);
            
            HorizontalLayoutGroup hlayout = statusBar.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 20;
            hlayout.padding = new RectOffset(20, 20, 10, 10);
            
            if (_currentWar != null && _currentWar.Phase == WarPhase.Active)
            {
                // Attacker
                CreateAllianceDisplay(statusBar.transform, _currentWar.AttackingAlliance, _currentWar.AttackerScore, true);
                
                // VS
                GameObject vs = new GameObject("VS");
                vs.transform.SetParent(statusBar.transform, false);
                
                LayoutElement vsLe = vs.AddComponent<LayoutElement>();
                vsLe.preferredWidth = 80;
                
                VerticalLayoutGroup vsVL = vs.AddComponent<VerticalLayoutGroup>();
                vsVL.childAlignment = TextAnchor.MiddleCenter;
                
                CreateText(vs.transform, "[!]", 32, TextAlignmentOptions.Center);
                CreateText(vs.transform, "VS", 18, TextAlignmentOptions.Center, goldColor);
                
                // Time remaining
                TimeSpan remaining = _currentWar.EndTime - DateTime.Now;
                CreateText(vs.transform, $"{remaining.Hours}h {remaining.Minutes}m left", 11, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.8f));
                
                // Defender
                CreateAllianceDisplay(statusBar.transform, _currentWar.DefendingAlliance, _currentWar.DefenderScore, false);
            }
            else
            {
                // No active war
                CreateText(statusBar.transform, "No Active War", 20, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            }
        }

        private void CreateAllianceDisplay(Transform parent, AllianceInfo alliance, int score, bool isAttacker)
        {
            GameObject display = new GameObject($"Alliance_{alliance.Name}");
            display.transform.SetParent(parent, false);
            
            LayoutElement le = display.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            VerticalLayoutGroup vlayout = display.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = isAttacker ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            vlayout.spacing = 5;
            
            Color teamColor = isAttacker ? allyColor : enemyColor;
            
            // Alliance name
            CreateText(display.transform, $"[{alliance.Tag}] {alliance.Name}", 18, 
                isAttacker ? TextAlignmentOptions.Left : TextAlignmentOptions.Right, teamColor);
            
            // Score
            CreateText(display.transform, $"Score: {score:N0}", 24, 
                isAttacker ? TextAlignmentOptions.Left : TextAlignmentOptions.Right, Color.white);
            
            // Members/Power
            CreateText(display.transform, $"{alliance.MemberCount} members - {alliance.Power:N0} power", 11, 
                isAttacker ? TextAlignmentOptions.Left : TextAlignmentOptions.Right, new Color(0.7f, 0.7f, 0.7f));
        }

        private void CreateTabs()
        {
            GameObject tabs = new GameObject("Tabs");
            tabs.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = tabs.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            HorizontalLayoutGroup hlayout = tabs.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.spacing = 5;
            
            CreateTab(tabs.transform, WarRoomTab.Overview, "[#] Overview");
            CreateTab(tabs.transform, WarRoomTab.Battles, "[!] Battles");
            CreateTab(tabs.transform, WarRoomTab.Leaderboard, "[T] Leaderboard");
            CreateTab(tabs.transform, WarRoomTab.Strategy, "[T] Strategy");
            CreateTab(tabs.transform, WarRoomTab.History, "[S] History");
        }

        private void CreateTab(Transform parent, WarRoomTab tab, string label)
        {
            GameObject tabObj = new GameObject($"Tab_{tab}");
            tabObj.transform.SetParent(parent, false);
            
            LayoutElement le = tabObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 35;
            
            Color bgColor = tab == _selectedTab ? new Color(0.2f, 0.35f, 0.5f) : new Color(0.15f, 0.15f, 0.2f);
            
            Image bg = tabObj.AddComponent<Image>();
            bg.color = bgColor;
            
            Button btn = tabObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectTab(tab));
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tabObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            
            _tabs[tab] = tabObj;
        }

        private void CreateContentArea()
        {
            _contentContainer = new GameObject("Content");
            _contentContainer.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _contentContainer.AddComponent<LayoutElement>();
            le.flexibleHeight = 1;
            
            Image bg = _contentContainer.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.5f);
            
            VerticalLayoutGroup vlayout = _contentContainer.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(15, 15, 15, 15);
            
            RefreshContent();
        }

        private void RefreshContent()
        {
            foreach (Transform child in _contentContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            switch (_selectedTab)
            {
                case WarRoomTab.Overview:
                    CreateOverviewContent();
                    break;
                case WarRoomTab.Battles:
                    CreateBattlesContent();
                    break;
                case WarRoomTab.Leaderboard:
                    CreateLeaderboardContent();
                    break;
                case WarRoomTab.Strategy:
                    CreateStrategyContent();
                    break;
                case WarRoomTab.History:
                    CreateHistoryContent();
                    break;
            }
        }

        private void CreateOverviewContent()
        {
            if (_currentWar == null || _currentWar.Phase != WarPhase.Active)
            {
                CreateSectionHeader("[!] Declare War");
                CreateText(_contentContainer.transform, "Your alliance is not currently at war.", 14, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
                CreateDeclareWarButton();
                return;
            }
            
            CreateSectionHeader("[#] War Statistics");
            
            // Stats grid
            GameObject statsGrid = new GameObject("StatsGrid");
            statsGrid.transform.SetParent(_contentContainer.transform, false);
            
            GridLayoutGroup grid = statsGrid.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(180, 80);
            grid.spacing = new Vector2(15, 15);
            grid.childAlignment = TextAnchor.UpperCenter;
            
            int totalBattles = _currentWar.AttackerWins + _currentWar.DefenderWins;
            
            CreateStatCard(statsGrid.transform, "Total Battles", totalBattles.ToString());
            CreateStatCard(statsGrid.transform, "Our Wins", _currentWar.AttackerWins.ToString(), allyColor);
            CreateStatCard(statsGrid.transform, "Their Wins", _currentWar.DefenderWins.ToString(), enemyColor);
            CreateStatCard(statsGrid.transform, "Point Lead", $"+{_currentWar.AttackerScore - _currentWar.DefenderScore}");
            
            CreateSectionHeader("[+] War Objectives");
            
            CreateObjectiveItem("Capture 3 enemy territories", 2, 3, true);
            CreateObjectiveItem("Win 30 battles", 26, 30, false);
            CreateObjectiveItem("Score 2000 points", _currentWar.AttackerScore, 2000, false);
            
            CreateSectionHeader("[+] Quick Actions");
            
            GameObject actions = new GameObject("Actions");
            actions.transform.SetParent(_contentContainer.transform, false);
            
            HorizontalLayoutGroup actionsHL = actions.AddComponent<HorizontalLayoutGroup>();
            actionsHL.childAlignment = TextAnchor.MiddleCenter;
            actionsHL.spacing = 15;
            
            CreateActionButton(actions.transform, "[!] Attack", () => ApexLogger.Log("[War] Opening attack interface...", ApexLogger.LogCategory.UI));
            CreateActionButton(actions.transform, "[D] Defend", () => ApexLogger.Log("[War] Opening defense interface...", ApexLogger.LogCategory.UI));
            CreateActionButton(actions.transform, "[!] Rally", () => ApexLogger.Log("[War] Sending rally call...", ApexLogger.LogCategory.UI));
        }

        private void CreateBattlesContent()
        {
            CreateSectionHeader("[!] Recent Battles");
            
            // Battle log
            CreateBattleLogItem("Commander_Alpha", "Shadow_Knight", true, "+150 pts", "5 min ago");
            CreateBattleLogItem("Iron_Warrior", "Alpha", false, "-80 pts", "12 min ago");
            CreateBattleLogItem("Steel_Titan", "Dark_Lord", true, "+120 pts", "18 min ago");
            CreateBattleLogItem("Blade_Master", "Night_Stalker", true, "+95 pts", "25 min ago");
            CreateBattleLogItem("Thunder_Strike", "Shadow_Walker", false, "-60 pts", "32 min ago");
            CreateBattleLogItem("Storm_Bringer", "Void_Reaper", true, "+180 pts", "45 min ago");
            
            CreateSectionHeader("[*] Active Battlefields");
            
            CreateBattlefieldItem("Northern Plains", "Contested", 5, 3);
            CreateBattlefieldItem("Eastern Fortress", "Under Attack", 2, 8);
            CreateBattlefieldItem("Crystal Mines", "Secured", 0, 0);
        }

        private void CreateBattleLogItem(string attacker, string defender, bool attackerWon, string points, string timeAgo)
        {
            GameObject item = new GameObject("BattleItem");
            item.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f);
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(15, 15, 5, 5);
            
            // Result icon
            string resultIcon = attackerWon ? "[!]" : "[D]";
            CreateText(item.transform, resultIcon, 20, TextAlignmentOptions.Center);
            
            // Attacker
            Color attackerColor = attackerWon ? allyColor : new Color(0.7f, 0.7f, 0.7f);
            GameObject attackerObj = CreateText(item.transform, attacker, 13, TextAlignmentOptions.Left, attackerColor);
            attackerObj.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // VS
            CreateText(item.transform, "vs", 12, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
            
            // Defender
            Color defenderColor = !attackerWon ? enemyColor : new Color(0.7f, 0.7f, 0.7f);
            GameObject defenderObj = CreateText(item.transform, defender, 13, TextAlignmentOptions.Left, defenderColor);
            defenderObj.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Points
            Color pointsColor = points.StartsWith("+") ? allyColor : enemyColor;
            GameObject pointsObj = CreateText(item.transform, points, 13, TextAlignmentOptions.Right, pointsColor);
            pointsObj.AddComponent<LayoutElement>().preferredWidth = 80;
            
            // Time
            GameObject timeObj = CreateText(item.transform, timeAgo, 11, TextAlignmentOptions.Right, new Color(0.5f, 0.5f, 0.5f));
            timeObj.AddComponent<LayoutElement>().preferredWidth = 70;
        }

        private void CreateBattlefieldItem(string name, string status, int ourTroops, int enemyTroops)
        {
            GameObject item = new GameObject($"Battlefield_{name}");
            item.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            Color statusColor = status == "Secured" ? allyColor : 
                               status == "Under Attack" ? enemyColor : goldColor;
            
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.18f);
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 8, 8);
            
            // Name and status
            GameObject info = new GameObject("Info");
            info.transform.SetParent(item.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            
            CreateText(info.transform, name, 14, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, status, 11, TextAlignmentOptions.Left, statusColor);
            
            // Troops
            if (ourTroops > 0 || enemyTroops > 0)
            {
                CreateText(item.transform, $"[O] {ourTroops}", 14, TextAlignmentOptions.Center, allyColor);
                CreateText(item.transform, "vs", 12, TextAlignmentOptions.Center, new Color(0.5f, 0.5f, 0.5f));
                CreateText(item.transform, $"[R] {enemyTroops}", 14, TextAlignmentOptions.Center, enemyColor);
            }
            
            // Action button
            if (status != "Secured")
            {
                CreateSmallButton(item.transform, "Join", () => ApexLogger.Log($"[War] Joining battlefield: {name}", ApexLogger.LogCategory.UI));
            }
        }

        private void CreateLeaderboardContent()
        {
            CreateSectionHeader("[T] Top Warriors - Our Alliance");
            
            CreateWarriorLeaderboardItem(1, "Commander_Alpha", 850, 12, 2);
            CreateWarriorLeaderboardItem(2, "Steel_Titan", 720, 10, 3);
            CreateWarriorLeaderboardItem(3, "Blade_Master", 650, 9, 2);
            CreateWarriorLeaderboardItem(4, "Storm_Bringer", 580, 8, 4);
            CreateWarriorLeaderboardItem(5, "Thunder_Strike", 520, 7, 3);
            
            CreateSectionHeader("[#] Enemy Top Warriors");
            
            CreateWarriorLeaderboardItem(1, "Shadow_Knight", 680, 9, 1, true);
            CreateWarriorLeaderboardItem(2, "Dark_Lord", 590, 8, 2, true);
            CreateWarriorLeaderboardItem(3, "Night_Stalker", 510, 7, 3, true);
        }

        private void CreateWarriorLeaderboardItem(int rank, string name, int score, int wins, int losses, bool isEnemy = false)
        {
            GameObject item = new GameObject($"Warrior_{rank}");
            item.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            Color bgColor = rank <= 3 ? new Color(0.15f, 0.18f, 0.25f) : new Color(0.1f, 0.1f, 0.15f);
            
            Image bg = item.AddComponent<Image>();
            bg.color = bgColor;
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 5, 5);
            
            // Rank
            string rankIcon = rank switch
            {
                1 => GameIcons.GoldMedal,
                2 => GameIcons.SilverMedal,
                3 => GameIcons.BronzeMedal,
                _ => $"#{rank}"
            };
            GameObject rankObj = CreateText(item.transform, rankIcon, rank <= 3 ? 20 : 14, TextAlignmentOptions.Center);
            rankObj.AddComponent<LayoutElement>().preferredWidth = 40;
            
            // Name
            Color nameColor = isEnemy ? enemyColor : Color.white;
            GameObject nameObj = CreateText(item.transform, name, 14, TextAlignmentOptions.Left, nameColor);
            nameObj.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Score
            GameObject scoreObj = CreateText(item.transform, $"{score} pts", 14, TextAlignmentOptions.Right, goldColor);
            scoreObj.AddComponent<LayoutElement>().preferredWidth = 80;
            
            // W/L
            CreateText(item.transform, $"{wins}W / {losses}L", 12, TextAlignmentOptions.Right, new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateStrategyContent()
        {
            CreateSectionHeader("[T] War Strategy Board");
            
            CreateText(_contentContainer.transform, "Coordinate with your alliance to plan attacks and defenses!", 13, TextAlignmentOptions.Center, new Color(0.7f, 0.7f, 0.7f));
            
            CreateSectionHeader("[+] Priority Targets");
            
            CreateTargetItem("Eastern Fortress", "High Value", "3 defenders, weak walls", TargetPriority.High);
            CreateTargetItem("Crystal Mines", "Resource Node", "Under-defended", TargetPriority.Medium);
            CreateTargetItem("Shadow Keep", "Enemy HQ", "Heavy defenses", TargetPriority.Low);
            
            CreateSectionHeader("[D] Defense Assignments");
            
            CreateDefenseItem("Northern Plains", "Steel_Titan, Blade_Master", "2/3 slots filled");
            CreateDefenseItem("Western Gate", "Storm_Bringer", "1/2 slots filled");
            CreateDefenseItem("Home Base", "Thunder_Strike, Iron_Warrior", "Full");
            
            CreateSectionHeader("[C] Strategic Notes");
            
            GameObject notesBox = new GameObject("NotesBox");
            notesBox.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = notesBox.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            Image bg = notesBox.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f);
            
            TextMeshProUGUI notes = notesBox.AddComponent<TextMeshProUGUI>();
            notes.text = "- Focus attacks on Eastern Fortress at 18:00\n- Need 2 more defenders at Western Gate\n- Save siege units for final push\n- Enemy is weak on cavalry - exploit this!";
            notes.fontSize = 12;
            notes.color = new Color(0.8f, 0.8f, 0.8f);
            notes.alignment = TextAlignmentOptions.TopLeft;
            notes.margin = new Vector4(15, 10, 15, 10);
        }

        private void CreateTargetItem(string name, string type, string notes, TargetPriority priority)
        {
            GameObject item = new GameObject($"Target_{name}");
            item.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 45;
            
            Color priorityColor = priority switch
            {
                TargetPriority.High => new Color(0.8f, 0.3f, 0.3f),
                TargetPriority.Medium => goldColor,
                _ => new Color(0.5f, 0.5f, 0.5f)
            };
            
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f);
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(15, 15, 5, 5);
            
            // Priority indicator
            GameObject priorityObj = CreateText(item.transform, priority.ToString().ToUpper(), 10, TextAlignmentOptions.Center, priorityColor);
            priorityObj.AddComponent<LayoutElement>().preferredWidth = 50;
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(item.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            
            CreateText(info.transform, $"{name} ({type})", 13, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, notes, 10, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.6f));
            
            // Attack button
            CreateSmallButton(item.transform, "Attack", () => ApexLogger.Log($"[War] Attacking: {name}", ApexLogger.LogCategory.UI));
        }

        private void CreateDefenseItem(string location, string defenders, string status)
        {
            GameObject item = new GameObject($"Defense_{location}");
            item.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.18f);
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 5, 5);
            
            // Location
            GameObject locObj = CreateText(item.transform, $"[C] {location}", 13, TextAlignmentOptions.Left, Color.white);
            locObj.AddComponent<LayoutElement>().preferredWidth = 150;
            
            // Defenders
            GameObject defObj = CreateText(item.transform, defenders, 12, TextAlignmentOptions.Left, allyColor);
            defObj.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Status
            Color statusColor = status == "Full" ? allyColor : goldColor;
            CreateText(item.transform, status, 11, TextAlignmentOptions.Right, statusColor);
            
            // Join button
            if (status != "Full")
            {
                CreateSmallButton(item.transform, "Join", () => ApexLogger.Log($"[War] Joining defense: {location}", ApexLogger.LogCategory.UI));
            }
        }

        private void CreateHistoryContent()
        {
            CreateSectionHeader("[S] War History");
            
            foreach (var war in _warHistory)
            {
                CreateWarHistoryItem(war);
            }
            
            if (_warHistory.Count == 0)
            {
                CreateText(_contentContainer.transform, "No war history yet.", 14, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
            }
        }

        private void CreateWarHistoryItem(WarRoomData war)
        {
            GameObject item = new GameObject($"WarHistory_{war.WarId}");
            item.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            bool won = war.Winner == war.AttackingAlliance.Name;
            
            Image bg = item.AddComponent<Image>();
            bg.color = won ? new Color(0.1f, 0.15f, 0.12f) : new Color(0.15f, 0.1f, 0.1f);
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Result
            string resultIcon = won ? "[T]" : "[X]";
            GameObject resultObj = CreateText(item.transform, resultIcon, 28, TextAlignmentOptions.Center);
            resultObj.AddComponent<LayoutElement>().preferredWidth = 40;
            
            // Info
            GameObject info = new GameObject("Info");
            info.transform.SetParent(item.transform, false);
            
            LayoutElement infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVL = info.AddComponent<VerticalLayoutGroup>();
            
            CreateText(info.transform, $"vs [{war.DefendingAlliance.Tag}] {war.DefendingAlliance.Name}", 14, TextAlignmentOptions.Left, Color.white);
            CreateText(info.transform, $"Final Score: {war.AttackerScore} - {war.DefenderScore}", 12, TextAlignmentOptions.Left, new Color(0.7f, 0.7f, 0.7f));
            CreateText(info.transform, war.EndTime.ToString("MMM dd, yyyy"), 10, TextAlignmentOptions.Left, new Color(0.5f, 0.5f, 0.5f));
            
            // Result text
            Color resultColor = won ? allyColor : enemyColor;
            CreateText(item.transform, won ? "VICTORY" : "DEFEAT", 14, TextAlignmentOptions.Right, resultColor);
        }

        #region UI Helpers

        private void CreateSectionHeader(string text)
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 30;
            
            TextMeshProUGUI headerText = header.AddComponent<TextMeshProUGUI>();
            headerText.text = text;
            headerText.fontSize = 16;
            headerText.fontStyle = FontStyles.Bold;
            headerText.color = accentColor;
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
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal; //  = false;
            
            return obj;
        }

        private void CreateStatCard(Transform parent, string label, string value, Color? valueColor = null)
        {
            GameObject card = new GameObject($"Stat_{label}");
            card.transform.SetParent(parent, false);
            
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.15f, 0.2f);
            
            VerticalLayoutGroup vlayout = card.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(10, 10, 15, 15);
            
            CreateText(card.transform, value, 26, TextAlignmentOptions.Center, valueColor ?? Color.white);
            CreateText(card.transform, label, 11, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateObjectiveItem(string description, int current, int target, bool completed)
        {
            GameObject item = new GameObject("Objective");
            item.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 35;
            
            HorizontalLayoutGroup hlayout = item.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(10, 10, 5, 5);
            
            // Checkbox
            string check = completed ? "[OK]" : "[]";
            CreateText(item.transform, check, 18, TextAlignmentOptions.Center);
            
            // Description
            Color textColor = completed ? new Color(0.6f, 0.8f, 0.6f) : Color.white;
            GameObject descObj = CreateText(item.transform, description, 13, TextAlignmentOptions.Left, textColor);
            descObj.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Progress
            string progress = $"{current}/{target}";
            Color progressColor = completed ? allyColor : goldColor;
            CreateText(item.transform, progress, 13, TextAlignmentOptions.Right, progressColor);
        }

        private void CreateActionButton(Transform parent, string label, Action onClick)
        {
            GameObject btn = new GameObject($"Btn_{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 120;
            le.preferredHeight = 40;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = accentColor;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateSmallButton(Transform parent, string label, Action onClick)
        {
            GameObject btn = new GameObject($"Btn_{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 60;
            le.preferredHeight = 28;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.35f, 0.5f);
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 11;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void CreateDeclareWarButton()
        {
            GameObject btn = new GameObject("DeclareWarBtn");
            btn.transform.SetParent(_contentContainer.transform, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = enemyColor;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => ApexLogger.Log("[War] Opening war declaration interface...", ApexLogger.LogCategory.UI));
            
            // Text as child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btn.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "[!] DECLARE WAR";
            text.fontSize = 18;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }

        #endregion

        private void SelectTab(WarRoomTab tab)
        {
            _selectedTab = tab;
            
            foreach (var kvp in _tabs)
            {
                Image bg = kvp.Value.GetComponent<Image>();
                bg.color = kvp.Key == tab ? new Color(0.2f, 0.35f, 0.5f) : new Color(0.15f, 0.15f, 0.2f);
            }
            
            RefreshContent();
        }

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
            if (_panel.activeSelf)
                Hide();
            else
                Show();
        }

        public bool HasActiveWar()
        {
            return _currentWar != null && _currentWar.Phase == WarPhase.Active;
        }

        #endregion
    }

    #region Data Classes

    public enum WarRoomTab
    {
        Overview,
        Battles,
        Leaderboard,
        Strategy,
        History
    }

    public enum WarPhase
    {
        Preparation,
        Active,
        Ended
    }

    public enum TargetPriority
    {
        High,
        Medium,
        Low
    }

    public class WarRoomData
    {
        public string WarId;
        public AllianceInfo AttackingAlliance;
        public AllianceInfo DefendingAlliance;
        public DateTime StartTime;
        public DateTime EndTime;
        public WarPhase Phase;
        public int AttackerScore;
        public int DefenderScore;
        public int TotalBattles;
        public int AttackerWins;
        public int DefenderWins;
        public string Winner;
    }

    public class AllianceInfo
    {
        public string Name;
        public string Tag;
        public int MemberCount;
        public long Power;
    }

    #endregion
}
