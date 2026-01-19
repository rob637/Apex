// ============================================================================
// APEX CITADELS - TROOP TRAINING QUICK BAR
// Compact HUD widget for quick troop training access
// ============================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using ApexCitadels.Data;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Compact HUD widget showing training progress and quick actions
    /// </summary>
    public class TroopQuickBar : MonoBehaviour
    {
        public static TroopQuickBar Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private KeyCode togglePanelKey = KeyCode.T;
        [SerializeField] private KeyCode[] quickTrainKeys = new KeyCode[] 
        { 
            KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6 
        };

        // UI References (created at runtime)
        private Canvas parentCanvas;
        private GameObject barRoot;
        private RectTransform barPanel;
        private TextMeshProUGUI queueStatusText;
        private TextMeshProUGUI armyStatusText;
        private RectTransform progressBar;
        private Image progressFill;
        private List<Button> quickTrainButtons = new List<Button>();

        // Colors
        private readonly Color BAR_BG = new Color(0.1f, 0.12f, 0.15f, 0.9f);
        private readonly Color PROGRESS_BG = new Color(0.2f, 0.2f, 0.2f, 1f);
        private readonly Color PROGRESS_FILL = new Color(0.3f, 0.7f, 0.4f, 1f);
        private readonly Color TEXT_PRIMARY = new Color(0.95f, 0.95f, 0.95f, 1f);
        private readonly Color TEXT_SECONDARY = new Color(0.7f, 0.7f, 0.7f, 1f);
        private readonly Color BUTTON_NORMAL = new Color(0.2f, 0.25f, 0.3f, 1f);
        private readonly Color BUTTON_HOVER = new Color(0.25f, 0.35f, 0.4f, 1f);

        // Troop icons
        private readonly Dictionary<TroopType, string> troopIcons = new Dictionary<TroopType, string>
        {
            { TroopType.Infantry, "⚔️" },
            { TroopType.Archer, "🏹" },
            { TroopType.Cavalry, "🐴" },
            { TroopType.Siege, "💣" },
            { TroopType.Mage, "🔮" },
            { TroopType.Guardian, "🛡️" }
        };

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            parentCanvas = FindFirstObjectByType<Canvas>();
            if (parentCanvas != null)
            {
                CreateUI();
                SubscribeToEvents();
            }
        }

        private void Update()
        {
            HandleInput();
            UpdateProgressBar();
        }

        #region UI Creation

        private void CreateUI()
        {
            // Main bar (bottom-left corner)
            barRoot = new GameObject("TroopQuickBar");
            barRoot.transform.SetParent(parentCanvas.transform, false);
            barPanel = barRoot.AddComponent<RectTransform>();

            barPanel.anchorMin = new Vector2(0, 0);
            barPanel.anchorMax = new Vector2(0, 0);
            barPanel.pivot = new Vector2(0, 0);
            barPanel.sizeDelta = new Vector2(500, 60);
            barPanel.anchoredPosition = new Vector2(10, 10);

            Image bg = barRoot.AddComponent<Image>();
            bg.color = BAR_BG;

            // Layout
            HorizontalLayoutGroup layout = barRoot.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // Training status section
            CreateTrainingStatusSection();

            // Spacer
            CreateSpacer(10);

            // Quick train buttons
            CreateQuickTrainButtons();

            // Spacer
            CreateSpacer(10);

            // Army status section
            CreateArmyStatusSection();

            // Open panel button
            CreateOpenPanelButton();
        }

        private void CreateTrainingStatusSection()
        {
            GameObject section = new GameObject("TrainingStatus");
            section.transform.SetParent(barPanel, false);
            RectTransform rt = section.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 50);

            VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = false;

            // Queue status text
            queueStatusText = CreateText(rt, "Queue: 0/5", 11);
            queueStatusText.rectTransform.sizeDelta = new Vector2(120, 18);
            queueStatusText.alignment = TextAlignmentOptions.MidlineLeft;
            queueStatusText.color = TEXT_SECONDARY;

            // Progress bar
            GameObject progressBg = new GameObject("ProgressBg");
            progressBg.transform.SetParent(rt, false);
            RectTransform progressBgRT = progressBg.AddComponent<RectTransform>();
            progressBgRT.sizeDelta = new Vector2(120, 12);

            Image progressBgImg = progressBg.AddComponent<Image>();
            progressBgImg.color = PROGRESS_BG;

            // Progress fill
            GameObject fill = new GameObject("ProgressFill");
            fill.transform.SetParent(progressBgRT, false);
            RectTransform fillRT = fill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0, 1);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            progressFill = fill.AddComponent<Image>();
            progressFill.color = PROGRESS_FILL;
            progressBar = fillRT;

            // Time remaining text
            TextMeshProUGUI timeText = CreateText(progressBgRT, "", 9);
            timeText.rectTransform.anchorMin = Vector2.zero;
            timeText.rectTransform.anchorMax = Vector2.one;
            timeText.rectTransform.offsetMin = new Vector2(3, 0);
            timeText.rectTransform.offsetMax = Vector2.zero;
            timeText.alignment = TextAlignmentOptions.MidlineLeft;
            timeText.name = "TimeText";
        }

        private void CreateQuickTrainButtons()
        {
            TroopType[] types = (TroopType[])Enum.GetValues(typeof(TroopType));

            for (int i = 0; i < types.Length && i < quickTrainKeys.Length; i++)
            {
                TroopType type = types[i];
                KeyCode key = quickTrainKeys[i];
                string icon = troopIcons.TryGetValue(type, out string ic) ? ic : "?";

                Button btn = CreateQuickButton(icon, $"Train {type} (F{i + 1})", () => QuickTrain(type));
                quickTrainButtons.Add(btn);
            }
        }

        private Button CreateQuickButton(string icon, string tooltip, Action onClick)
        {
            GameObject btnObj = new GameObject("QuickBtn");
            btnObj.transform.SetParent(barPanel, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40, 40);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = BUTTON_NORMAL;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock colors = btn.colors;
            colors.normalColor = BUTTON_NORMAL;
            colors.highlightedColor = BUTTON_HOVER;
            btn.colors = colors;

            TextMeshProUGUI iconText = CreateText(rt, icon, 20);
            iconText.rectTransform.anchorMin = Vector2.zero;
            iconText.rectTransform.anchorMax = Vector2.one;
            iconText.rectTransform.offsetMin = Vector2.zero;
            iconText.rectTransform.offsetMax = Vector2.zero;

            btn.onClick.AddListener(() => onClick?.Invoke());

            // Tooltip
            AddSimpleTooltip(btnObj, tooltip);

            return btn;
        }

        private void CreateArmyStatusSection()
        {
            GameObject section = new GameObject("ArmyStatus");
            section.transform.SetParent(barPanel, false);
            RectTransform rt = section.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 50);

            armyStatusText = CreateText(rt, "👥 0/100\n⚡ 0", 11);
            armyStatusText.rectTransform.anchorMin = Vector2.zero;
            armyStatusText.rectTransform.anchorMax = Vector2.one;
            armyStatusText.rectTransform.offsetMin = Vector2.zero;
            armyStatusText.rectTransform.offsetMax = Vector2.zero;
            armyStatusText.alignment = TextAlignmentOptions.Center;
            armyStatusText.color = TEXT_SECONDARY;
        }

        private void CreateOpenPanelButton()
        {
            GameObject btnObj = new GameObject("OpenPanelBtn");
            btnObj.transform.SetParent(barPanel, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(45, 45);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.4f, 0.3f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.2f, 0.4f, 0.3f, 1f);
            colors.highlightedColor = new Color(0.25f, 0.5f, 0.35f, 1f);
            btn.colors = colors;

            TextMeshProUGUI iconText = CreateText(rt, "🎖️", 22);
            iconText.rectTransform.anchorMin = Vector2.zero;
            iconText.rectTransform.anchorMax = Vector2.one;
            iconText.rectTransform.offsetMin = Vector2.zero;
            iconText.rectTransform.offsetMax = Vector2.zero;

            btn.onClick.AddListener(OpenTrainingPanel);

            AddSimpleTooltip(btnObj, $"Open Training Panel ({togglePanelKey})");
        }

        private void CreateSpacer(float width)
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(barPanel, false);
            RectTransform rt = spacer.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 1);
            LayoutElement le = spacer.AddComponent<LayoutElement>();
            le.minWidth = width;
        }

        #endregion

        #region Input & Actions

        private void HandleInput()
        {
            // Toggle panel
            if (Input.GetKeyDown(togglePanelKey))
            {
                OpenTrainingPanel();
            }

            // Quick train shortcuts
            TroopType[] types = (TroopType[])Enum.GetValues(typeof(TroopType));
            for (int i = 0; i < quickTrainKeys.Length && i < types.Length; i++)
            {
                if (Input.GetKeyDown(quickTrainKeys[i]))
                {
                    QuickTrain(types[i]);
                }
            }
        }

        private void QuickTrain(TroopType type)
        {
            if (TrainingQueueManager.Instance != null)
            {
                if (TrainingQueueManager.Instance.QueueTraining(type, 1))
                {
                    // Visual feedback
                    ShowTrainingStarted(type);
                }
                else
                {
                    // Show error feedback
                    Debug.Log($"[QuickBar] Cannot train {type} - queue full or insufficient resources");
                }
            }
        }

        private void OpenTrainingPanel()
        {
            TroopTrainingPanel.Instance?.Toggle();
        }

        private void ShowTrainingStarted(TroopType type)
        {
            // Flash the button or show quick feedback
            string icon = troopIcons.TryGetValue(type, out string i) ? i : "?";
            Debug.Log($"[QuickBar] Started training: {icon} {type}");
        }

        #endregion

        #region Update Display

        private void SubscribeToEvents()
        {
            if (TrainingQueueManager.Instance != null)
            {
                TrainingQueueManager.Instance.OnQueueChanged += UpdateQueueDisplay;
                TrainingQueueManager.Instance.OnTroopsAdded += (_, __) => UpdateArmyDisplay();
            }

            UpdateQueueDisplay();
            UpdateArmyDisplay();
        }

        private void UpdateQueueDisplay()
        {
            if (TrainingQueueManager.Instance == null) return;

            int queueCount = TrainingQueueManager.Instance.QueueCount;
            int maxQueue = TrainingQueueManager.Instance.MaxQueueSize;

            if (queueStatusText != null)
            {
                queueStatusText.text = $"Queue: {queueCount}/{maxQueue}";
            }
        }

        private void UpdateProgressBar()
        {
            if (TrainingQueueManager.Instance == null) return;

            var current = TrainingQueueManager.Instance.CurrentTraining;

            if (current != null && progressBar != null)
            {
                progressBar.anchorMax = new Vector2(current.Progress, 1);

                // Update time text
                var timeText = progressBar.parent?.Find("TimeText")?.GetComponent<TextMeshProUGUI>();
                if (timeText != null)
                {
                    timeText.text = FormatTime(current.RemainingSeconds);
                }
            }
            else if (progressBar != null)
            {
                progressBar.anchorMax = new Vector2(0, 1);
            }
        }

        private void UpdateArmyDisplay()
        {
            int armySize = TrainingQueueManager.Instance?.GetTotalArmySize() ?? 0;
            int maxArmy = TroopManager.Instance?.MaxArmySize ?? 100;
            int power = TrainingQueueManager.Instance?.GetTotalArmyPower() ?? 0;

            if (armyStatusText != null)
            {
                armyStatusText.text = $"👥 {armySize}/{maxArmy}\n⚡ {power:N0}";
            }
        }

        private string FormatTime(int seconds)
        {
            if (seconds < 60) return $"{seconds}s";
            if (seconds < 3600) return $"{seconds / 60}m {seconds % 60}s";
            return $"{seconds / 3600}h {(seconds % 3600) / 60}m";
        }

        #endregion

        #region Utility

        private TextMeshProUGUI CreateText(RectTransform parent, string text, int fontSize)
        {
            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = TEXT_PRIMARY;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            return tmp;
        }

        private void AddSimpleTooltip(GameObject obj, string text)
        {
            var trigger = obj.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => Debug.Log($"Tooltip: {text}"));
            trigger.triggers.Add(enterEntry);
        }

        #endregion

        #region Visibility

        public void Show()
        {
            if (barRoot != null)
                barRoot.SetActive(true);
        }

        public void Hide()
        {
            if (barRoot != null)
                barRoot.SetActive(false);
        }

        #endregion
    }
}
