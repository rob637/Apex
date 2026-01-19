using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Barracks Panel - Train and manage your troops!
    /// Core military management for conquering territories.
    /// </summary>
    public class BarracksPanel : MonoBehaviour
    {
        [Header("Training Settings")]
        [SerializeField] private float baseTrainingTime = 10f; // seconds per unit
        [SerializeField] private int maxQueueSize = 20;
        
        [Header("Colors")]
        [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color stoneColor = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private Color woodColor = new Color(0.6f, 0.4f, 0.2f);
        [SerializeField] private Color progressColor = new Color(0.3f, 0.7f, 0.3f);
        
        // UI Elements
        private GameObject _panel;
        private Dictionary<TroopType, TroopTrainingCard> _troopCards = new Dictionary<TroopType, TroopTrainingCard>();
        private List<TrainingQueueItem> _trainingQueue = new List<TrainingQueueItem>();
        private GameObject _queueContainer;
        private TextMeshProUGUI _queueStatusText;
        
        // Training state
        private float _currentTrainingProgress;
        private bool _isTraining;
        
        // Army counts
        private Dictionary<TroopType, int> _armyCounts = new Dictionary<TroopType, int>();
        
        public static BarracksPanel Instance { get; private set; }
        
        // Events
        public event Action<TroopType, int> OnTroopsTrained;
        public event Action<TroopType, int> OnTrainingStarted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeArmyCounts();
        }

        private void Start()
        {
            CreateBarracksPanel();
            Hide();
        }

        private void Update()
        {
            if (_isTraining && _trainingQueue.Count > 0)
            {
                UpdateTraining();
            }
        }

        private void InitializeArmyCounts()
        {
            _armyCounts[TroopType.Infantry] = 100;
            _armyCounts[TroopType.Archer] = 50;
            _armyCounts[TroopType.Cavalry] = 25;
            _armyCounts[TroopType.Siege] = 10;
            _armyCounts[TroopType.Elite] = 5;
        }

        private void CreateBarracksPanel()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            
            // Main panel
            _panel = new GameObject("BarracksPanel");
            _panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = _panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.1f);
            rect.anchorMax = new Vector2(0.9f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.98f);
            
            HorizontalLayoutGroup hlayout = _panel.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.UpperCenter;
            hlayout.childForceExpandWidth = true;
            hlayout.childForceExpandHeight = true;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 15, 15);
            
            // Left: Troop cards
            CreateTroopCardsSection();
            
            // Right: Queue and army overview
            CreateQueueSection();
        }

        private void CreateTroopCardsSection()
        {
            GameObject cardsSection = new GameObject("TroopCards");
            cardsSection.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = cardsSection.AddComponent<LayoutElement>();
            le.flexibleWidth = 2;
            
            Image bg = cardsSection.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.06f, 0.1f, 0.8f);
            
            VerticalLayoutGroup vlayout = cardsSection.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            // Header
            CreateSectionHeader(cardsSection.transform, "⚔️ BARRACKS - Train Your Army", true);
            
            // Troop cards
            foreach (TroopType type in Enum.GetValues(typeof(TroopType)))
            {
                CreateTroopCard(cardsSection.transform, type);
            }
        }

        private void CreateSectionHeader(Transform parent, string title, bool withClose = false)
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(parent, false);
            
            LayoutElement le = header.AddComponent<LayoutElement>();
            le.preferredHeight = 45;
            
            HorizontalLayoutGroup hlayout = header.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.childForceExpandWidth = true;
            
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(header.transform, false);
            
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = goldColor;
            
            if (withClose)
            {
                GameObject closeBtn = new GameObject("CloseBtn");
                closeBtn.transform.SetParent(header.transform, false);
                
                LayoutElement closeLe = closeBtn.AddComponent<LayoutElement>();
                closeLe.preferredWidth = 40;
                
                Image closeBg = closeBtn.AddComponent<Image>();
                closeBg.color = new Color(0.6f, 0.2f, 0.2f, 0.8f);
                
                Button btn = closeBtn.AddComponent<Button>();
                btn.onClick.AddListener(Hide);
                
                GameObject x = new GameObject("X");
                x.transform.SetParent(closeBtn.transform, false);
                
                TextMeshProUGUI xText = x.AddComponent<TextMeshProUGUI>();
                xText.text = "✕";
                xText.fontSize = 20;
                xText.alignment = TextAlignmentOptions.Center;
                
                RectTransform xRect = x.GetComponent<RectTransform>();
                xRect.anchorMin = Vector2.zero;
                xRect.anchorMax = Vector2.one;
            }
        }

        private void CreateTroopCard(Transform parent, TroopType type)
        {
            TroopDefinition def = GetTroopDefinition(type);
            
            GameObject card = new GameObject($"Card_{type}");
            card.transform.SetParent(parent, false);
            
            LayoutElement le = card.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            
            Image bg = card.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
            
            HorizontalLayoutGroup hlayout = card.AddComponent<HorizontalLayoutGroup>();
            hlayout.childAlignment = TextAnchor.MiddleCenter;
            hlayout.spacing = 15;
            hlayout.padding = new RectOffset(15, 15, 10, 10);
            
            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(card.transform, false);
            
            LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 50;
            
            TextMeshProUGUI icon = iconObj.AddComponent<TextMeshProUGUI>();
            icon.text = def.Icon;
            icon.fontSize = 36;
            icon.alignment = TextAlignmentOptions.Center;
            
            // Info
            GameObject infoObj = new GameObject("Info");
            infoObj.transform.SetParent(card.transform, false);
            
            LayoutElement infoLE = infoObj.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            VerticalLayoutGroup infoVLayout = infoObj.AddComponent<VerticalLayoutGroup>();
            infoVLayout.childAlignment = TextAnchor.MiddleLeft;
            infoVLayout.spacing = 2;
            
            // Name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(infoObj.transform, false);
            
            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = def.Name;
            nameText.fontSize = 18;
            nameText.fontStyle = FontStyles.Bold;
            
            // Stats
            GameObject statsObj = new GameObject("Stats");
            statsObj.transform.SetParent(infoObj.transform, false);
            
            TextMeshProUGUI statsText = statsObj.AddComponent<TextMeshProUGUI>();
            statsText.text = $"ATK:{def.Attack} DEF:{def.Defense} SPD:{def.Speed}";
            statsText.fontSize = 12;
            statsText.color = new Color(0.7f, 0.7f, 0.7f);
            
            // Cost
            GameObject costObj = new GameObject("Cost");
            costObj.transform.SetParent(infoObj.transform, false);
            
            TextMeshProUGUI costText = costObj.AddComponent<TextMeshProUGUI>();
            costText.text = $"💰{def.GoldCost} 🪨{def.StoneCost} ⏱️{def.TrainingTime}s";
            costText.fontSize = 11;
            costText.color = goldColor;
            
            // Current count
            GameObject countObj = new GameObject("Count");
            countObj.transform.SetParent(card.transform, false);
            
            LayoutElement countLE = countObj.AddComponent<LayoutElement>();
            countLE.preferredWidth = 60;
            
            VerticalLayoutGroup countVL = countObj.AddComponent<VerticalLayoutGroup>();
            countVL.childAlignment = TextAnchor.MiddleCenter;
            
            GameObject countLabel = new GameObject("Label");
            countLabel.transform.SetParent(countObj.transform, false);
            
            TextMeshProUGUI labelText = countLabel.AddComponent<TextMeshProUGUI>();
            labelText.text = "ARMY";
            labelText.fontSize = 10;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0.6f, 0.6f, 0.6f);
            
            GameObject countValue = new GameObject("Value");
            countValue.transform.SetParent(countObj.transform, false);
            
            TextMeshProUGUI countValueText = countValue.AddComponent<TextMeshProUGUI>();
            countValueText.text = _armyCounts[type].ToString();
            countValueText.fontSize = 22;
            countValueText.fontStyle = FontStyles.Bold;
            countValueText.alignment = TextAlignmentOptions.Center;
            countValueText.color = progressColor;
            
            // Train buttons
            GameObject buttonsObj = new GameObject("Buttons");
            buttonsObj.transform.SetParent(card.transform, false);
            
            LayoutElement buttonsLE = buttonsObj.AddComponent<LayoutElement>();
            buttonsLE.preferredWidth = 140;
            
            HorizontalLayoutGroup buttonsHL = buttonsObj.AddComponent<HorizontalLayoutGroup>();
            buttonsHL.childAlignment = TextAnchor.MiddleCenter;
            buttonsHL.spacing = 5;
            
            // Train x1
            CreateTrainButton(buttonsObj.transform, "+1", () => QueueTroop(type, 1), new Color(0.2f, 0.5f, 0.3f));
            
            // Train x5
            CreateTrainButton(buttonsObj.transform, "+5", () => QueueTroop(type, 5), new Color(0.3f, 0.5f, 0.2f));
            
            // Train x10
            CreateTrainButton(buttonsObj.transform, "+10", () => QueueTroop(type, 10), new Color(0.4f, 0.5f, 0.2f));
            
            // Store reference
            _troopCards[type] = new TroopTrainingCard
            {
                Type = type,
                CountText = countValueText,
                Definition = def
            };
        }

        private void CreateTrainButton(Transform parent, string label, Action onClick, Color color)
        {
            GameObject btn = new GameObject($"Btn{label}");
            btn.transform.SetParent(parent, false);
            
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredWidth = 40;
            le.preferredHeight = 35;
            
            Image bg = btn.AddComponent<Image>();
            bg.color = color;
            
            Button button = btn.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            ColorBlock colors = button.colors;
            colors.highlightedColor = color * 1.2f;
            colors.pressedColor = color * 0.8f;
            button.colors = colors;
            
            GameObject txt = new GameObject("Label");
            txt.transform.SetParent(btn.transform, false);
            
            TextMeshProUGUI text = txt.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            
            RectTransform txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
        }

        private void CreateQueueSection()
        {
            GameObject queueSection = new GameObject("QueueSection");
            queueSection.transform.SetParent(_panel.transform, false);
            
            LayoutElement le = queueSection.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            
            Image bg = queueSection.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.06f, 0.1f, 0.8f);
            
            VerticalLayoutGroup vlayout = queueSection.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childForceExpandWidth = true;
            vlayout.spacing = 10;
            vlayout.padding = new RectOffset(10, 10, 10, 10);
            
            // Header
            CreateSectionHeader(queueSection.transform, "📋 Training Queue");
            
            // Queue status
            GameObject statusObj = new GameObject("QueueStatus");
            statusObj.transform.SetParent(queueSection.transform, false);
            
            LayoutElement statusLE = statusObj.AddComponent<LayoutElement>();
            statusLE.preferredHeight = 30;
            
            _queueStatusText = statusObj.AddComponent<TextMeshProUGUI>();
            _queueStatusText.text = "Queue: 0/20 | Idle";
            _queueStatusText.fontSize = 14;
            _queueStatusText.alignment = TextAlignmentOptions.Center;
            _queueStatusText.color = new Color(0.7f, 0.7f, 0.7f);
            
            // Current training progress
            CreateTrainingProgressBar(queueSection.transform);
            
            // Queue items container
            _queueContainer = new GameObject("QueueItems");
            _queueContainer.transform.SetParent(queueSection.transform, false);
            
            LayoutElement queueLE = _queueContainer.AddComponent<LayoutElement>();
            queueLE.flexibleHeight = 1;
            
            VerticalLayoutGroup queueVL = _queueContainer.AddComponent<VerticalLayoutGroup>();
            queueVL.childAlignment = TextAnchor.UpperCenter;
            queueVL.childForceExpandWidth = true;
            queueVL.childForceExpandHeight = false;
            queueVL.spacing = 5;
            
            // Army Overview
            CreateArmyOverview(queueSection.transform);
            
            // Clear queue button
            GameObject clearBtn = new GameObject("ClearBtn");
            clearBtn.transform.SetParent(queueSection.transform, false);
            
            LayoutElement clearLE = clearBtn.AddComponent<LayoutElement>();
            clearLE.preferredHeight = 35;
            
            Image clearBg = clearBtn.AddComponent<Image>();
            clearBg.color = new Color(0.5f, 0.2f, 0.2f);
            
            Button clear = clearBtn.AddComponent<Button>();
            clear.onClick.AddListener(ClearQueue);
            
            GameObject clearTxt = new GameObject("Label");
            clearTxt.transform.SetParent(clearBtn.transform, false);
            
            TextMeshProUGUI clearText = clearTxt.AddComponent<TextMeshProUGUI>();
            clearText.text = "🗑️ Clear Queue";
            clearText.fontSize = 14;
            clearText.alignment = TextAlignmentOptions.Center;
            
            RectTransform clearRect = clearTxt.GetComponent<RectTransform>();
            clearRect.anchorMin = Vector2.zero;
            clearRect.anchorMax = Vector2.one;
        }

        private Image _trainingProgressFill;
        private TextMeshProUGUI _trainingProgressText;

        private void CreateTrainingProgressBar(Transform parent)
        {
            GameObject progressObj = new GameObject("TrainingProgress");
            progressObj.transform.SetParent(parent, false);
            
            LayoutElement le = progressObj.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            
            VerticalLayoutGroup vl = progressObj.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.MiddleCenter;
            vl.spacing = 3;
            
            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(progressObj.transform, false);
            
            _trainingProgressText = labelObj.AddComponent<TextMeshProUGUI>();
            _trainingProgressText.text = "Currently Training: None";
            _trainingProgressText.fontSize = 12;
            _trainingProgressText.alignment = TextAlignmentOptions.Center;
            
            LayoutElement labelLE = labelObj.AddComponent<LayoutElement>();
            labelLE.preferredHeight = 18;
            
            // Progress bar
            GameObject barBg = new GameObject("BarBg");
            barBg.transform.SetParent(progressObj.transform, false);
            
            LayoutElement barLE = barBg.AddComponent<LayoutElement>();
            barLE.preferredHeight = 18;
            
            Image bgImg = barBg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f);
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(barBg.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            _trainingProgressFill = fill.AddComponent<Image>();
            _trainingProgressFill.color = progressColor;
        }

        private TextMeshProUGUI _armyOverviewText;

        private void CreateArmyOverview(Transform parent)
        {
            GameObject overview = new GameObject("ArmyOverview");
            overview.transform.SetParent(parent, false);
            
            LayoutElement le = overview.AddComponent<LayoutElement>();
            le.preferredHeight = 100;
            
            Image bg = overview.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.1f, 0.8f);
            
            VerticalLayoutGroup vl = overview.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.padding = new RectOffset(10, 10, 10, 10);
            vl.spacing = 5;
            
            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(overview.transform, false);
            
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "🏰 Total Army";
            title.fontSize = 16;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = goldColor;
            
            LayoutElement titleLE = titleObj.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 22;
            
            // Army text
            GameObject armyObj = new GameObject("ArmyText");
            armyObj.transform.SetParent(overview.transform, false);
            
            _armyOverviewText = armyObj.AddComponent<TextMeshProUGUI>();
            _armyOverviewText.fontSize = 12;
            _armyOverviewText.alignment = TextAlignmentOptions.Center;
            UpdateArmyOverview();
            
            LayoutElement armyLE = armyObj.AddComponent<LayoutElement>();
            armyLE.flexibleHeight = 1;
        }

        private void UpdateArmyOverview()
        {
            int total = 0;
            int power = 0;
            string text = "";
            
            foreach (var kvp in _armyCounts)
            {
                TroopDefinition def = GetTroopDefinition(kvp.Key);
                total += kvp.Value;
                power += kvp.Value * (def.Attack + def.Defense);
                text += $"{def.Icon} {kvp.Key}: {kvp.Value}\n";
            }
            
            _armyOverviewText.text = text + $"\n<color=#FFD700>Total: {total} | Power: {power}</color>";
        }

        #region Training Logic

        private void QueueTroop(TroopType type, int count)
        {
            TroopDefinition def = GetTroopDefinition(type);
            
            for (int i = 0; i < count; i++)
            {
                if (_trainingQueue.Count >= maxQueueSize)
                {
                    Debug.Log("[Barracks] Queue is full!");
                    break;
                }
                
                // Check resources
                if (PCResourceSystem.Instance != null)
                {
                    int gold = (int)PCResourceSystem.Instance.GetResource(ResourceType.Gold);
                    int stone = (int)PCResourceSystem.Instance.GetResource(ResourceType.Stone);
                    
                    if (gold < def.GoldCost || stone < def.StoneCost)
                    {
                        Debug.Log("[Barracks] Not enough resources!");
                        break;
                    }
                    
                    PCResourceSystem.Instance.SpendResource(ResourceType.Gold, def.GoldCost);
                    PCResourceSystem.Instance.SpendResource(ResourceType.Stone, def.StoneCost);
                }
                
                _trainingQueue.Add(new TrainingQueueItem
                {
                    Type = type,
                    TrainingTime = def.TrainingTime
                });
            }
            
            if (!_isTraining && _trainingQueue.Count > 0)
            {
                _isTraining = true;
                _currentTrainingProgress = 0f;
                OnTrainingStarted?.Invoke(type, count);
            }
            
            UpdateQueueDisplay();
        }

        private void UpdateTraining()
        {
            if (_trainingQueue.Count == 0)
            {
                _isTraining = false;
                return;
            }
            
            TrainingQueueItem current = _trainingQueue[0];
            TroopDefinition def = GetTroopDefinition(current.Type);
            
            _currentTrainingProgress += Time.deltaTime;
            
            // Update progress bar
            float progress = _currentTrainingProgress / def.TrainingTime;
            _trainingProgressFill.rectTransform.anchorMax = new Vector2(progress, 1f);
            _trainingProgressText.text = $"Training: {def.Icon} {current.Type} ({progress * 100:F0}%)";
            
            if (_currentTrainingProgress >= def.TrainingTime)
            {
                // Training complete!
                _trainingQueue.RemoveAt(0);
                _armyCounts[current.Type]++;
                _currentTrainingProgress = 0f;
                
                // Update UI
                if (_troopCards.ContainsKey(current.Type))
                {
                    _troopCards[current.Type].CountText.text = _armyCounts[current.Type].ToString();
                }
                
                UpdateQueueDisplay();
                UpdateArmyOverview();
                
                OnTroopsTrained?.Invoke(current.Type, 1);
                
                Debug.Log($"[Barracks] Trained 1 {current.Type}! Total: {_armyCounts[current.Type]}");
            }
        }

        private void UpdateQueueDisplay()
        {
            _queueStatusText.text = $"Queue: {_trainingQueue.Count}/{maxQueueSize} | " +
                (_isTraining ? "Training..." : "Idle");
            
            // Clear existing queue items
            foreach (Transform child in _queueContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Create queue item displays (show first 10)
            int displayCount = Mathf.Min(_trainingQueue.Count, 10);
            for (int i = 0; i < displayCount; i++)
            {
                CreateQueueItemDisplay(_trainingQueue[i], i);
            }
            
            if (_trainingQueue.Count > 10)
            {
                GameObject moreObj = new GameObject("MoreItems");
                moreObj.transform.SetParent(_queueContainer.transform, false);
                
                TextMeshProUGUI moreText = moreObj.AddComponent<TextMeshProUGUI>();
                moreText.text = $"... and {_trainingQueue.Count - 10} more";
                moreText.fontSize = 11;
                moreText.alignment = TextAlignmentOptions.Center;
                moreText.color = new Color(0.5f, 0.5f, 0.5f);
                
                LayoutElement le = moreObj.AddComponent<LayoutElement>();
                le.preferredHeight = 18;
            }
        }

        private void CreateQueueItemDisplay(TrainingQueueItem item, int index)
        {
            TroopDefinition def = GetTroopDefinition(item.Type);
            
            GameObject queueItem = new GameObject($"QueueItem_{index}");
            queueItem.transform.SetParent(_queueContainer.transform, false);
            
            LayoutElement le = queueItem.AddComponent<LayoutElement>();
            le.preferredHeight = 25;
            
            Image bg = queueItem.AddComponent<Image>();
            bg.color = index == 0 ? new Color(0.2f, 0.3f, 0.2f, 0.8f) : new Color(0.15f, 0.15f, 0.2f, 0.6f);
            
            HorizontalLayoutGroup hl = queueItem.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.padding = new RectOffset(10, 10, 2, 2);
            hl.spacing = 10;
            
            // Position
            GameObject posObj = new GameObject("Pos");
            posObj.transform.SetParent(queueItem.transform, false);
            
            TextMeshProUGUI posText = posObj.AddComponent<TextMeshProUGUI>();
            posText.text = $"#{index + 1}";
            posText.fontSize = 11;
            posText.alignment = TextAlignmentOptions.Center;
            posText.color = new Color(0.5f, 0.5f, 0.5f);
            
            LayoutElement posLE = posObj.AddComponent<LayoutElement>();
            posLE.preferredWidth = 30;
            
            // Icon + name
            GameObject infoObj = new GameObject("Info");
            infoObj.transform.SetParent(queueItem.transform, false);
            
            TextMeshProUGUI infoText = infoObj.AddComponent<TextMeshProUGUI>();
            infoText.text = $"{def.Icon} {item.Type}";
            infoText.fontSize = 12;
            infoText.alignment = TextAlignmentOptions.Left;
            
            LayoutElement infoLE = infoObj.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            
            // Cancel button (not for current)
            if (index > 0)
            {
                GameObject cancelBtn = new GameObject("Cancel");
                cancelBtn.transform.SetParent(queueItem.transform, false);
                
                LayoutElement cancelLE = cancelBtn.AddComponent<LayoutElement>();
                cancelLE.preferredWidth = 20;
                
                Image cancelBg = cancelBtn.AddComponent<Image>();
                cancelBg.color = new Color(0.5f, 0.2f, 0.2f, 0.8f);
                
                Button btn = cancelBtn.AddComponent<Button>();
                int idx = index;
                btn.onClick.AddListener(() => CancelQueueItem(idx));
                
                GameObject x = new GameObject("X");
                x.transform.SetParent(cancelBtn.transform, false);
                
                TextMeshProUGUI xText = x.AddComponent<TextMeshProUGUI>();
                xText.text = "✕";
                xText.fontSize = 12;
                xText.alignment = TextAlignmentOptions.Center;
                
                RectTransform xRect = x.GetComponent<RectTransform>();
                xRect.anchorMin = Vector2.zero;
                xRect.anchorMax = Vector2.one;
            }
        }

        private void CancelQueueItem(int index)
        {
            if (index > 0 && index < _trainingQueue.Count)
            {
                TrainingQueueItem item = _trainingQueue[index];
                TroopDefinition def = GetTroopDefinition(item.Type);
                
                // Refund resources (partial)
                if (PCResourceSystem.Instance != null)
                {
                    PCResourceSystem.Instance.AddResource(ResourceType.Gold, def.GoldCost / 2);
                    PCResourceSystem.Instance.AddResource(ResourceType.Stone, def.StoneCost / 2);
                }
                
                _trainingQueue.RemoveAt(index);
                UpdateQueueDisplay();
            }
        }

        private void ClearQueue()
        {
            // Refund all queued (except current)
            for (int i = _trainingQueue.Count - 1; i > 0; i--)
            {
                CancelQueueItem(i);
            }
        }

        #endregion

        #region Troop Definitions

        private TroopDefinition GetTroopDefinition(TroopType type)
        {
            return type switch
            {
                TroopType.Infantry => new TroopDefinition
                {
                    Name = "Infantry",
                    Icon = "🗡️",
                    Attack = 10,
                    Defense = 15,
                    Speed = 5,
                    GoldCost = 50,
                    StoneCost = 20,
                    TrainingTime = 10f
                },
                TroopType.Archer => new TroopDefinition
                {
                    Name = "Archer",
                    Icon = "🏹",
                    Attack = 15,
                    Defense = 5,
                    Speed = 6,
                    GoldCost = 75,
                    StoneCost = 10,
                    TrainingTime = 15f
                },
                TroopType.Cavalry => new TroopDefinition
                {
                    Name = "Cavalry",
                    Icon = "🐴",
                    Attack = 20,
                    Defense = 10,
                    Speed = 10,
                    GoldCost = 150,
                    StoneCost = 50,
                    TrainingTime = 25f
                },
                TroopType.Siege => new TroopDefinition
                {
                    Name = "Siege Engine",
                    Icon = "💣",
                    Attack = 50,
                    Defense = 5,
                    Speed = 2,
                    GoldCost = 300,
                    StoneCost = 200,
                    TrainingTime = 60f
                },
                TroopType.Elite => new TroopDefinition
                {
                    Name = "Elite Guard",
                    Icon = "⚔️",
                    Attack = 30,
                    Defense = 25,
                    Speed = 7,
                    GoldCost = 500,
                    StoneCost = 100,
                    TrainingTime = 45f
                },
                _ => new TroopDefinition()
            };
        }

        #endregion

        #region Public API

        public void Show()
        {
            _panel.SetActive(true);
            UpdateQueueDisplay();
            UpdateArmyOverview();
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

        public int GetTroopCount(TroopType type)
        {
            return _armyCounts.ContainsKey(type) ? _armyCounts[type] : 0;
        }

        public Dictionary<TroopType, int> GetAllTroopCounts()
        {
            return new Dictionary<TroopType, int>(_armyCounts);
        }

        #endregion
    }

    public class TroopTrainingCard
    {
        public TroopType Type;
        public TextMeshProUGUI CountText;
        public TroopDefinition Definition;
    }

    public class BarracksQueueItem
    {
        public TroopType Type;
        public float TrainingTime;
    }

    public class TroopDefinition
    {
        public string Name;
        public string Icon;
        public int Attack;
        public int Defense;
        public int Speed;
        public int GoldCost;
        public int StoneCost;
        public float TrainingTime;
    }
}
