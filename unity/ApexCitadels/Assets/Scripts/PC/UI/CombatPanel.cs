using System.Linq;
using System;
using ApexCitadels.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.UI;
using ApexCitadels.PC.Combat;
using ApexCitadels.Core;
using TerritoryData = ApexCitadels.Territory.Territory;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Combat Panel - Attack territories with your troops!
    /// Core gameplay loop for territorial conquest.
    /// </summary>
    public class CombatPanel : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float battleSimulationSpeed = 1f;
        [SerializeField] private int maxTroopsPerAttack = 50;
        
        [Header("Colors")]
        [SerializeField] private Color attackerColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color defenderColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color victoryColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color defeatColor = new Color(0.5f, 0.5f, 0.5f);
        
        // UI Elements
        private GameObject _panel;
        private GameObject _troopSelectionPanel;
        private GameObject _battleSimulationPanel;
        private GameObject _resultsPanel;
        
        // Troop selection
        private Dictionary<TroopType, int> _selectedTroops = new Dictionary<TroopType, int>();
        private Dictionary<TroopType, TextMeshProUGUI> _troopCountTexts = new Dictionary<TroopType, TextMeshProUGUI>();
        private Dictionary<TroopType, int> _availableTroops = new Dictionary<TroopType, int>();
        
        // Battle state
        private TerritoryData _targetTerritory;
        private BattleSimulation _currentBattle;
        private bool _isBattleInProgress;
        
        // Battle visualization
        private List<GameObject> _attackerUnits = new List<GameObject>();
        private List<GameObject> _defenderUnits = new List<GameObject>();
        private Slider _battleProgressBar;
        private TextMeshProUGUI _battleLogText;
        
        public static CombatPanel Instance { get; private set; }
        
        // Events
        public event Action<TerritoryData, BattleResult> OnBattleCompleted;
        public event Action<TerritoryData> OnAttackStarted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeTroopCounts();
        }

        private void Start()
        {
            CreateCombatPanel();
            Hide();
        }

        private void InitializeTroopCounts()
        {
            // Initialize with demo troops
            _availableTroops[TroopType.Infantry] = 100;
            _availableTroops[TroopType.Archer] = 50;
            _availableTroops[TroopType.Cavalry] = 25;
            _availableTroops[TroopType.Siege] = 10;
            _availableTroops[TroopType.Elite] = 5;
            
            foreach (TroopType type in Enum.GetValues(typeof(TroopType)))
            {
                _selectedTroops[type] = 0;
            }
        }

        private void CreateCombatPanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("CombatPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.1f);
            rect.anchorMax = new Vector2(0.85f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Background
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.98f);
            
            // Layout
            VerticalLayoutGroup layout = _panel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 10;
            layout.padding = new RectOffset(20, 20, 15, 15);
            
            // Header
            CreateHeader();
            
            // Target info
            CreateTargetInfoSection();
            
            // Troop selection
            CreateTroopSelectionSection();
            
            // Battle preview
            CreateBattlePreviewSection();
            
            // Action buttons
            CreateActionButtons();
            
            // Battle simulation view (hidden initially)
            CreateBattleSimulationView();
            
            // Results view (hidden initially)
            CreateResultsView();
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
            title.text = $"{GameIcons.Battle} ATTACK TERRITORY";
            title.fontSize = 28;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(1f, 0.3f, 0.3f);
            
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

        private GameObject _targetInfoSection;
        private TextMeshProUGUI _targetNameText;
        private TextMeshProUGUI _targetStatsText;
        private Image _targetHealthBar;

        private void CreateTargetInfoSection()
        {
            _targetInfoSection = new GameObject("TargetInfo");
            _targetInfoSection.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _targetInfoSection.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            Image bg = _targetInfoSection.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.1f, 0.1f, 0.9f);
            
            VerticalLayoutGroup vlayout = _targetInfoSection.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(15, 15, 10, 10);
            vlayout.spacing = 5;
            
            // Target name
            GameObject nameObj = new GameObject("TargetName");
            nameObj.transform.SetParent(_targetInfoSection.transform, false);
            
            _targetNameText = nameObj.AddComponent<TextMeshProUGUI>();
            _targetNameText.text = "🏰 Enemy Citadel";
            _targetNameText.fontSize = 22;
            _targetNameText.fontStyle = FontStyles.Bold;
            _targetNameText.alignment = TextAlignmentOptions.Center;
            _targetNameText.color = defenderColor;
            
            LayoutElement nameLE = nameObj.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 30;
            
            // Target stats
            GameObject statsObj = new GameObject("TargetStats");
            statsObj.transform.SetParent(_targetInfoSection.transform, false);
            
            _targetStatsText = statsObj.AddComponent<TextMeshProUGUI>();
            _targetStatsText.text = "Level 5 | Defense: 250 | Garrison: 50 troops";
            _targetStatsText.fontSize = 14;
            _targetStatsText.alignment = TextAlignmentOptions.Center;
            _targetStatsText.color = new Color(0.8f, 0.8f, 0.8f);
            
            LayoutElement statsLE = statsObj.AddComponent<LayoutElement>();
            statsLE.preferredHeight = 20;
            
            // Health bar
            GameObject healthBarBg = new GameObject("HealthBarBg");
            healthBarBg.transform.SetParent(_targetInfoSection.transform, false);
            
            LayoutElement hbLE = healthBarBg.AddComponent<LayoutElement>();
            hbLE.preferredHeight = 20;
            
            Image hbBg = healthBarBg.AddComponent<Image>();
            hbBg.color = new Color(0.2f, 0.2f, 0.2f);
            
            GameObject healthFill = new GameObject("HealthFill");
            healthFill.transform.SetParent(healthBarBg.transform, false);
            
            RectTransform fillRect = healthFill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.8f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            _targetHealthBar = healthFill.AddComponent<Image>();
            _targetHealthBar.color = defenderColor;
        }

        private void CreateTroopSelectionSection()
        {
            _troopSelectionPanel = new GameObject("TroopSelection");
            _troopSelectionPanel.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = _troopSelectionPanel.AddComponent<LayoutElement>();
            le.flexibleHeight = 1;
            le.preferredHeight = 200;
            
            VerticalLayoutGroup vlayout = _troopSelectionPanel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.spacing = 5;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            // Section title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(_troopSelectionPanel.transform, false);
            
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "SELECT YOUR ARMY";
            title.fontSize = 18;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = attackerColor;
            
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 25;
            
            // Troop rows
            foreach (TroopType type in Enum.GetValues(typeof(TroopType)))
            {
                CreateTroopRow(type);
            }
        }

        private void CreateTroopRow(TroopType type)
        {
            GameObject row = new GameObject($"Troop_{type}");
            row.transform.SetParent(_troopSelectionPanel.transform, false);
            
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 45;
            
            Image bg = row.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.18f, 0.8f);
            
            HorizontalLayoutGroup hlayout = row.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 10;
            hlayout.padding = new RectOffset(15, 15, 5, 5);
            
            // Icon
            string icon = GetTroopIcon(type);
            CreateText(row.transform, icon, 24, 40);
            
            // Name + stats
            string stats = GetTroopStats(type);
            var nameText = CreateText(row.transform, $"{type}\n<size=10>{stats}</size>", 14, 0, true);
            
            // Available count
            int available = _availableTroops.ContainsKey(type) ? _availableTroops[type] : 0;
            CreateText(row.transform, $"/{available}", 12, 40, false, new Color(0.6f, 0.6f, 0.6f));
            
            // Selected count
            var countText = CreateText(row.transform, "0", 20, 50, false, attackerColor);
            _troopCountTexts[type] = countText;
            
            // Add/Remove buttons
            CreateTroopButton(row.transform, "-", () => RemoveTroop(type, 1), () => RemoveTroop(type, 10));
            CreateTroopButton(row.transform, "+", () => AddTroop(type, 1), () => AddTroop(type, 10));
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, float width, bool flexible = false, Color? color = null)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color ?? Color.white;
            
            LayoutElement le = obj.AddComponent<LayoutElement>();
            if (flexible)
                le.flexibleWidth = 1;
            else if (width > 0)
                le.preferredWidth = width;
            
            return tmp;
        }

        private void CreateTroopButton(Transform parent, string label, Action onClick, Action onRightClick)
        {
            GameObject btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);
            
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = 35;
            le.preferredHeight = 35;
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.3f, 0.4f);
            
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            
            // Right-click for x10
            TroopButtonHandler handler = btnObj.AddComponent<TroopButtonHandler>();
            handler.OnRightClick = onRightClick;
            
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.4f, 0.5f, 0.6f);
            colors.pressedColor = new Color(0.2f, 0.3f, 0.4f);
            btn.colors = colors;
            
            GameObject txt = new GameObject("Label");
            txt.transform.SetParent(btnObj.transform, false);
            
            TextMeshProUGUI tmpLabel = txt.AddComponent<TextMeshProUGUI>();
            tmpLabel.text = label;
            tmpLabel.fontSize = 20;
            tmpLabel.fontStyle = FontStyles.Bold;
            tmpLabel.alignment = TextAlignmentOptions.Center;
            
            RectTransform txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
        }

        private string GetTroopIcon(TroopType type)
        {
            return type switch
            {
                TroopType.Infantry => "🗡️",
                TroopType.Archer => "🏹",
                TroopType.Cavalry => "🐴",
                TroopType.Siege => "💣",
                TroopType.Elite => "⚔️",
                _ => "👤"
            };
        }

        private string GetTroopStats(TroopType type)
        {
            return type switch
            {
                TroopType.Infantry => "ATK:10 DEF:15 SPD:5",
                TroopType.Archer => "ATK:15 DEF:5 RNG:8",
                TroopType.Cavalry => "ATK:20 DEF:10 SPD:10",
                TroopType.Siege => "ATK:50 DEF:5 SPD:2",
                TroopType.Elite => "ATK:30 DEF:25 SPD:7",
                _ => ""
            };
        }

        private TextMeshProUGUI _powerComparisonText;
        private Image _winChanceBar;
        private TextMeshProUGUI _winChanceText;

        private void CreateBattlePreviewSection()
        {
            GameObject preview = new GameObject("BattlePreview");
            preview.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = preview.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            Image bg = preview.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.15f, 0.1f, 0.9f);
            
            VerticalLayoutGroup vlayout = preview.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(15, 15, 10, 10);
            vlayout.spacing = 5;
            
            // Power comparison
            GameObject powerObj = new GameObject("PowerComparison");
            powerObj.transform.SetParent(preview.transform, false);
            
            _powerComparisonText = powerObj.AddComponent<TextMeshProUGUI>();
            _powerComparisonText.text = "⚡ Your Power: 0 vs Enemy: 250";
            _powerComparisonText.fontSize = 16;
            _powerComparisonText.alignment = TextAlignmentOptions.Center;
            
            LayoutElement powerLE = powerObj.AddComponent<LayoutElement>();
            powerLE.preferredHeight = 25;
            
            // Win chance bar
            GameObject barBg = new GameObject("WinChanceBarBg");
            barBg.transform.SetParent(preview.transform, false);
            
            LayoutElement barLE = barBg.AddComponent<LayoutElement>();
            barLE.preferredHeight = 25;
            
            Image bgImg = barBg.AddComponent<Image>();
            bgImg.color = defenderColor;
            
            GameObject barFill = new GameObject("WinChanceFill");
            barFill.transform.SetParent(barBg.transform, false);
            
            RectTransform fillRect = barFill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            _winChanceBar = barFill.AddComponent<Image>();
            _winChanceBar.color = attackerColor;
            
            // Win chance text
            GameObject chanceObj = new GameObject("WinChanceText");
            chanceObj.transform.SetParent(preview.transform, false);
            
            _winChanceText = chanceObj.AddComponent<TextMeshProUGUI>();
            _winChanceText.text = "🎲 Win Chance: 50%";
            _winChanceText.fontSize = 14;
            _winChanceText.alignment = TextAlignmentOptions.Center;
            
            LayoutElement chanceLE = chanceObj.AddComponent<LayoutElement>();
            chanceLE.preferredHeight = 20;
        }

        private Button _attackButton;

        private void CreateActionButtons()
        {
            GameObject actions = new GameObject("Actions");
            actions.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = actions.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            
            HorizontalLayoutGroup hlayout = actions.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 20;
            hlayout.padding = new RectOffset(50, 50, 10, 10);
            
            // Scout button
            CreateActionButton(actions.transform, "🔍 SCOUT (50g)", ScoutTerritory, new Color(0.3f, 0.5f, 0.7f));
            
            // Attack button
            _attackButton = CreateActionButton(actions.transform, "⚔️ ATTACK!", LaunchAttack, new Color(0.8f, 0.2f, 0.2f));
        }

        private Button CreateActionButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btnObj = new GameObject("ActionBtn");
            btnObj.transform.SetParent(parent, false);
            
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 45;
            
            Image bg = btnObj.AddComponent<Image>();
            bg.color = color;
            
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            
            ColorBlock colors = btn.colors;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            btn.colors = colors;
            
            GameObject txt = new GameObject("Label");
            txt.transform.SetParent(btnObj.transform, false);
            
            TextMeshProUGUI tmpLabel = txt.AddComponent<TextMeshProUGUI>();
            tmpLabel.text = label;
            tmpLabel.fontSize = 18;
            tmpLabel.fontStyle = FontStyles.Bold;
            tmpLabel.alignment = TextAlignmentOptions.Center;
            
            RectTransform txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            
            return btn;
        }

        private void CreateBattleSimulationView()
        {
            _battleSimulationPanel = new GameObject("BattleSimulation");
            _battleSimulationPanel.transform.SetParent(_panel.transform, false);
            
            RectTransform rect = _battleSimulationPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(20, 20);
            rect.offsetMax = new Vector2(-20, -20);
            
            Image bg = _battleSimulationPanel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.98f);
            
            VerticalLayoutGroup vlayout = _battleSimulationPanel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.padding = new RectOffset(20, 20, 20, 20);
            vlayout.spacing = 15;
            
            // Battle title
            GameObject titleObj = new GameObject("BattleTitle");
            titleObj.transform.SetParent(_battleSimulationPanel.transform, false);
            
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = $"{GameIcons.Battle} BATTLE IN PROGRESS {GameIcons.Battle}";
            title.fontSize = 24;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(1f, 0.8f, 0.2f);
            
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 35;
            
            // Progress bar
            GameObject progressBg = new GameObject("ProgressBg");
            progressBg.transform.SetParent(_battleSimulationPanel.transform, false);
            
            LayoutElement progressLE = progressBg.AddComponent<LayoutElement>();
            progressLE.preferredHeight = 30;
            
            Image progBg = progressBg.AddComponent<Image>();
            progBg.color = new Color(0.2f, 0.2f, 0.2f);
            
            GameObject progressFill = new GameObject("ProgressFill");
            progressFill.transform.SetParent(progressBg.transform, false);
            
            _battleProgressBar = progressBg.AddComponent<Slider>();
            _battleProgressBar.fillRect = progressFill.AddComponent<RectTransform>();
            
            // Battle log
            GameObject logObj = new GameObject("BattleLog");
            logObj.transform.SetParent(_battleSimulationPanel.transform, false);
            
            LayoutElement logLE = logObj.AddComponent<LayoutElement>();
            logLE.flexibleHeight = 1;
            
            Image logBg = logObj.AddComponent<Image>();
            logBg.color = new Color(0.08f, 0.08f, 0.1f);
            
            GameObject logContent = new GameObject("LogContent");
            logContent.transform.SetParent(logObj.transform, false);
            
            RectTransform logRect = logContent.AddComponent<RectTransform>();
            logRect.anchorMin = Vector2.zero;
            logRect.anchorMax = Vector2.one;
            logRect.offsetMin = new Vector2(10, 10);
            logRect.offsetMax = new Vector2(-10, -10);
            
            _battleLogText = logContent.AddComponent<TextMeshProUGUI>();
            _battleLogText.text = "";
            _battleLogText.fontSize = 12;
            _battleLogText.alignment = TextAlignmentOptions.TopLeft;
            _battleLogText.color = new Color(0.8f, 0.8f, 0.8f);
            
            _battleSimulationPanel.SetActive(false);
        }

        private TextMeshProUGUI _resultsTitle;
        private TextMeshProUGUI _resultsStats;

        private void CreateResultsView()
        {
            _resultsPanel = new GameObject("ResultsPanel");
            _resultsPanel.transform.SetParent(_panel.transform, false);
            
            RectTransform rect = _resultsPanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(20, 20);
            rect.offsetMax = new Vector2(-20, -20);
            
            Image bg = _resultsPanel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.98f);
            
            VerticalLayoutGroup vlayout = _resultsPanel.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.MiddleCenter;
            vlayout.padding = new RectOffset(30, 30, 30, 30);
            vlayout.spacing = 20;
            
            // Result title
            GameObject titleObj = new GameObject("ResultTitle");
            titleObj.transform.SetParent(_resultsPanel.transform, false);
            
            _resultsTitle = titleObj.AddComponent<TextMeshProUGUI>();
            _resultsTitle.text = $"{GameIcons.Trophy} VICTORY!";
            _resultsTitle.fontSize = 36;
            _resultsTitle.fontStyle = FontStyles.Bold;
            _resultsTitle.alignment = TextAlignmentOptions.Center;
            _resultsTitle.color = victoryColor;
            
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 50;
            
            // Stats
            GameObject statsObj = new GameObject("Stats");
            statsObj.transform.SetParent(_resultsPanel.transform, false);
            
            _resultsStats = statsObj.AddComponent<TextMeshProUGUI>();
            _resultsStats.text = "Troops Lost: 15/50\nEnemy Eliminated: 30/30\nLoot Gained: 500 Gold, 200 Stone";
            _resultsStats.fontSize = 18;
            _resultsStats.alignment = TextAlignmentOptions.Center;
            _resultsStats.color = Color.white;
            
            LayoutElement statsLE = statsObj.AddComponent<LayoutElement>();
            statsLE.flexibleHeight = 1;
            
            // Continue button
            GameObject continueBtn = new GameObject("ContinueBtn");
            continueBtn.transform.SetParent(_resultsPanel.transform, false);
            
            LayoutElement btnLE = continueBtn.AddComponent<LayoutElement>();
            btnLE.preferredHeight = 50;
            btnLE.preferredWidth = 200;
            
            Image btnBg = continueBtn.AddComponent<Image>();
            btnBg.color = new Color(0.2f, 0.5f, 0.3f);
            
            Button btn = continueBtn.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                _resultsPanel.SetActive(false);
                Hide();
            });
            
            GameObject btnTxt = new GameObject("Label");
            btnTxt.transform.SetParent(continueBtn.transform, false);
            
            TextMeshProUGUI btnLabel = btnTxt.AddComponent<TextMeshProUGUI>();
            btnLabel.text = "CONTINUE";
            btnLabel.fontSize = 20;
            btnLabel.fontStyle = FontStyles.Bold;
            btnLabel.alignment = TextAlignmentOptions.Center;
            
            RectTransform txtRect = btnTxt.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            
            _resultsPanel.SetActive(false);
        }

        #region Troop Management

        private void AddTroop(TroopType type, int count)
        {
            int available = _availableTroops.ContainsKey(type) ? _availableTroops[type] : 0;
            int current = _selectedTroops[type];
            int totalSelected = GetTotalSelectedTroops();
            
            int canAdd = Mathf.Min(count, available - current, maxTroopsPerAttack - totalSelected);
            if (canAdd > 0)
            {
                _selectedTroops[type] += canAdd;
                UpdateTroopDisplay(type);
                UpdateBattlePreview();
            }
        }

        private void RemoveTroop(TroopType type, int count)
        {
            int current = _selectedTroops[type];
            int toRemove = Mathf.Min(count, current);
            if (toRemove > 0)
            {
                _selectedTroops[type] -= toRemove;
                UpdateTroopDisplay(type);
                UpdateBattlePreview();
            }
        }

        private void UpdateTroopDisplay(TroopType type)
        {
            if (_troopCountTexts.ContainsKey(type))
            {
                _troopCountTexts[type].text = _selectedTroops[type].ToString();
            }
        }

        private int GetTotalSelectedTroops()
        {
            int total = 0;
            foreach (var kvp in _selectedTroops)
            {
                total += kvp.Value;
            }
            return total;
        }

        private int CalculateAttackPower()
        {
            int power = 0;
            power += _selectedTroops[TroopType.Infantry] * 10;
            power += _selectedTroops[TroopType.Archer] * 15;
            power += _selectedTroops[TroopType.Cavalry] * 20;
            power += _selectedTroops[TroopType.Siege] * 50;
            power += _selectedTroops[TroopType.Elite] * 30;
            return power;
        }

        #endregion

        #region Battle Logic

        private void UpdateBattlePreview()
        {
            int attackPower = CalculateAttackPower();
            int defensePower = _targetTerritory?.Level * 50 ?? 250;
            
            _powerComparisonText.text = $"⚡ Your Power: {attackPower} vs Enemy: {defensePower}";
            
            float winChance = Mathf.Clamp01((float)attackPower / (attackPower + defensePower));
            _winChanceBar.rectTransform.anchorMax = new Vector2(winChance, 1f);
            _winChanceText.text = $"🎲 Win Chance: {winChance * 100:F0}%";
            
            // Color based on odds
            if (winChance > 0.7f)
                _winChanceText.color = attackerColor;
            else if (winChance > 0.4f)
                _winChanceText.color = Color.yellow;
            else
                _winChanceText.color = defenderColor;
            
            // Enable/disable attack button
            _attackButton.interactable = GetTotalSelectedTroops() > 0;
        }

        private void ScoutTerritory()
        {
            // TODO: Implement scouting - spend gold to reveal enemy composition
            ApexLogger.Log(ApexLogger.LogCategory.UI, "[Combat] Scouting territory...");
        }

        private void LaunchAttack()
        {
            if (_isBattleInProgress || _targetTerritory == null) return;
            
            int totalTroops = GetTotalSelectedTroops();
            if (totalTroops == 0)
            {
                ApexLogger.Log(ApexLogger.LogCategory.UI, "[Combat] Select troops first!");
                return;
            }
            
            _isBattleInProgress = true;
            OnAttackStarted?.Invoke(_targetTerritory);
            
            // Hide selection, show battle
            _troopSelectionPanel.SetActive(false);
            _battleSimulationPanel.SetActive(true);
            
            // Start simulation
            StartCoroutine(SimulateBattle());
        }

        private System.Collections.IEnumerator SimulateBattle()
        {
            _battleLogText.text = "";
            float elapsed = 0f;
            float duration = 5f; // 5 second battle
            
            int attackPower = CalculateAttackPower();
            int defensePower = _targetTerritory?.Level * 50 ?? 250;
            float winChance = (float)attackPower / (attackPower + defensePower);
            
            // Get effects integration
            var effects = CombatEffectsIntegration.Instance;
            
            // Battle start effects
            effects?.OnBattleStart();
            
            // Log start
            AddBattleLog("⚔️ Battle begins!");
            AddBattleLog($"Your army ({GetTotalSelectedTroops()} troops) advances...");
            yield return new WaitForSeconds(0.5f);
            
            // Charge effect
            effects?.OnCharge(Vector3.zero);
            
            // Simulate phases
            string[] phases = { "Opening Volley", "Melee Clash", "Siege Phase", "Final Push" };
            int phaseIndex = 0;
            foreach (string phase in phases)
            {
                AddBattleLog($"\n--- {phase} ---");
                yield return new WaitForSeconds(0.3f);
                
                // Random battle events with VFX
                int events = UnityEngine.Random.Range(2, 5);
                for (int i = 0; i < events; i++)
                {
                    var (message, eventType) = GenerateBattleEventWithType(phaseIndex);
                    AddBattleLog(message);
                    
                    // Trigger appropriate VFX based on event type
                    TriggerBattleEventVFX(eventType, effects);
                    
                    elapsed += duration / (phases.Length * events);
                    if (_battleProgressBar != null)
                        _battleProgressBar.value = elapsed / duration;
                    yield return new WaitForSeconds(0.4f);
                }
                phaseIndex++;
            }
            
            // Determine outcome
            bool victory = UnityEngine.Random.value < winChance;
            
            AddBattleLog(victory ? "\n🏆 VICTORY!" : "\n💀 DEFEAT!");
            
            // Outcome effects
            if (victory)
            {
                effects?.OnVictory(Vector3.zero);
            }
            else
            {
                effects?.OnDefeat(Vector3.zero);
            }
            
            yield return new WaitForSeconds(1f);
            
            // Show results
            ShowBattleResults(victory, attackPower, defensePower);
        }
        
        private void TriggerBattleEventVFX(BattleEventType eventType, CombatEffectsIntegration effects)
        {
            if (effects == null) return;
            
            // Random position variation for VFX
            Vector3 pos = new Vector3(UnityEngine.Random.Range(-5f, 5f), 0, UnityEngine.Random.Range(-5f, 5f));
            int damage = UnityEngine.Random.Range(10, 50);
            bool crit = UnityEngine.Random.value < 0.15f;
            
            switch (eventType)
            {
                case BattleEventType.MeleeClash:
                    effects.OnMeleeAttack(pos, damage, crit);
                    break;
                case BattleEventType.ArcherVolley:
                    effects.OnRangedAttack(pos + Vector3.back * 10f, pos, damage, crit);
                    break;
                case BattleEventType.CavalryCharge:
                    effects.OnCharge(pos);
                    break;
                case BattleEventType.SiegeWeapon:
                    effects.OnSiegeAttack(pos + Vector3.back * 15f, pos, damage * 3);
                    break;
                case BattleEventType.DefenseHold:
                    effects.OnAttackBlocked(pos);
                    break;
                case BattleEventType.FireAttack:
                    effects.OnMagicAttack(pos + Vector3.back * 8f, pos, damage, new Color(1f, 0.4f, 0.1f));
                    break;
                case BattleEventType.Breakthrough:
                    effects.VFX?.PlayExplosion(pos, ExplosionSize.Medium);
                    effects.CameraEffects?.Shake(0.3f);
                    break;
                case BattleEventType.Retreat:
                    effects.OnUnitKilled(pos, "Enemy Unit", 15);
                    break;
                case BattleEventType.Advance:
                    effects.OnBuff(pos, "ADVANCE!");
                    break;
            }
        }
        
        private (string message, BattleEventType type) GenerateBattleEventWithType(int phase)
        {
            // Phase-appropriate events
            var events = phase switch
            {
                0 => new[] { // Opening Volley
                    ("🏹 Archers rain arrows on the walls!", BattleEventType.ArcherVolley),
                    ("🔥 Fire arrows ignite the towers!", BattleEventType.FireAttack),
                    ("💣 Siege weapons launch their payload!", BattleEventType.SiegeWeapon),
                    ("🛡️ Defenders raise their shields!", BattleEventType.DefenseHold),
                },
                1 => new[] { // Melee Clash
                    ("🗡️ Infantry clashes with defenders!", BattleEventType.MeleeClash),
                    ("⚔️ Elite troops cut through enemy ranks!", BattleEventType.MeleeClash),
                    ("🛡️ Defenders hold the line!", BattleEventType.DefenseHold),
                    ("⚡ Your troops break through!", BattleEventType.Breakthrough),
                },
                2 => new[] { // Siege Phase
                    ("💣 Siege weapons breach the wall!", BattleEventType.SiegeWeapon),
                    ("💥 Battering ram breaks through!", BattleEventType.Breakthrough),
                    ("🔥 Flaming projectiles rain down!", BattleEventType.FireAttack),
                    ("🗡️ Close quarters combat ensues!", BattleEventType.MeleeClash),
                },
                _ => new[] { // Final Push
                    ("🐴 Cavalry charges through the gate!", BattleEventType.CavalryCharge),
                    ("⚔️ Elite troops lead the final assault!", BattleEventType.MeleeClash),
                    ("🏃 Enemy troops retreat!", BattleEventType.Retreat),
                    ("⚡ Your troops advance!", BattleEventType.Advance),
                    ("🏆 Victory is within reach!", BattleEventType.Advance),
                }
            };
            
            return events[UnityEngine.Random.Range(0, events.Length)];
        }

        private void AddBattleLog(string message)
        {
            _battleLogText.text += message + "\n";
        }

        // Keep old method for backward compatibility
        private string GenerateBattleEvent()
        {
            var (message, _) = GenerateBattleEventWithType(UnityEngine.Random.Range(0, 4));
            return message;
        }

        private void ShowBattleResults(bool victory, int attackPower, int defensePower)
        {
            _battleSimulationPanel.SetActive(false);
            _resultsPanel.SetActive(true);
            
            var effects = CombatEffectsIntegration.Instance;
            
            int totalTroops = GetTotalSelectedTroops();
            int troopsLost = victory ? 
                Mathf.RoundToInt(totalTroops * 0.3f) : 
                Mathf.RoundToInt(totalTroops * 0.7f);
            
            if (victory)
            {
                _resultsTitle.text = $"{GameIcons.Trophy} VICTORY!";
                _resultsTitle.color = victoryColor;
                
                int goldLoot = UnityEngine.Random.Range(200, 500);
                int stoneLoot = UnityEngine.Random.Range(100, 300);
                
                _resultsStats.text = $"Troops Lost: {troopsLost}/{totalTroops}\n" +
                    $"Enemy Eliminated: ALL\n\n" +
                    $"<color=#FFD700>{GameIcons.Gold} Gold Looted: +{goldLoot}</color>\n" +
                    $"<color=#888888>{GameIcons.Stone} Stone Looted: +{stoneLoot}</color>\n\n" +
                    $"Territory captured!";
                
                // Grant rewards
                if (PCResourceSystem.Instance != null)
                {
                    PCResourceSystem.Instance.AddResource(ResourceType.Gold, goldLoot);
                    PCResourceSystem.Instance.AddResource(ResourceType.Stone, stoneLoot);
                }
                
                // Play resource gain effects
                effects?.OnResourceGain(Vector3.zero, "Gold", goldLoot);
                effects?.AudioSFX?.PlayCoins();
            }
            else
            {
                _resultsTitle.text = $"{GameIcons.Defeat} DEFEAT";
                _resultsTitle.color = defeatColor;
                
                _resultsStats.text = $"Troops Lost: {troopsLost}/{totalTroops}\n" +
                    $"Enemy Remaining: {defensePower / 10}\n\n" +
                    $"Your forces were repelled.\n" +
                    $"Rebuild and try again!";
            }
            
            // Deduct troops
            foreach (var type in _selectedTroops.Keys)
            {
                int lost = Mathf.RoundToInt(_selectedTroops[type] * (troopsLost / (float)totalTroops));
                _availableTroops[type] = Mathf.Max(0, _availableTroops[type] - lost);
            }
            
            _isBattleInProgress = false;
            
            // Fire event
            OnBattleCompleted?.Invoke(_targetTerritory, new BattleResult { Victory = victory, TroopsLost = troopsLost });
        }

        #endregion

        #region Public API

        public void Show(TerritoryData target = null)
        {
            _targetTerritory = target;
            UpdateTargetInfo();
            UpdateBattlePreview();
            
            // Reset selection
            foreach (var type in _selectedTroops.Keys.ToArray())
            {
                _selectedTroops[type] = 0;
                UpdateTroopDisplay(type);
            }
            
            // Show selection view
            _troopSelectionPanel.SetActive(true);
            _battleSimulationPanel.SetActive(false);
            _resultsPanel.SetActive(false);
            
            _panel.SetActive(true);
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

        private void UpdateTargetInfo()
        {
            if (_targetTerritory != null)
            {
                _targetNameText.text = $"🏰 {_targetTerritory.Name}";
                _targetStatsText.text = $"Level {_targetTerritory.Level} | Defense: {_targetTerritory.Level * 50} | Owner: {_targetTerritory.OwnerName ?? "Unclaimed"}";
                
                float healthPercent = (float)_targetTerritory.Health / _targetTerritory.MaxHealth;
                _targetHealthBar.rectTransform.anchorMax = new Vector2(healthPercent, 1f);
            }
            else
            {
                _targetNameText.text = "🏰 Select a Territory";
                _targetStatsText.text = "Click on an enemy territory on the map";
            }
        }

        #endregion
    }

    /// <summary>
    /// Handles right-click on troop buttons for x10 action
    /// </summary>
    public class TroopButtonHandler : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
    {
        public Action OnRightClick;
        
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (eventData.button == UnityEngine.EventSystems.PointerEventData.InputButton.Right)
            {
                OnRightClick?.Invoke();
            }
        }
    }

    // Note: TroopType is defined in ApexCitadels.Data.BattleTypes

    public class BattleSimulation
    {
        public TerritoryData Target;
        public Dictionary<TroopType, int> AttackerTroops;
        public int DefenderStrength;
        public float Progress;
        public List<string> BattleLog = new List<string>();
    }

    public class BattleResult
    {
        public bool Victory;
        public int TroopsLost;
        public int GoldLooted;
        public int StoneLooted;
    }

    // Note: This BattleEventType is for UI display - different from replay BattleEventType
    public enum CombatEventType
    {
        MeleeClash,
        ArcherVolley,
        CavalryCharge,
        SiegeWeapon,
        DefenseHold,
        FireAttack,
        Breakthrough,
        Retreat,
        Advance
    }
}
