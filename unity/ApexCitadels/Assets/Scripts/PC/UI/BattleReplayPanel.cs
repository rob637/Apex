using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// PC-exclusive Battle Replay UI Panel
    /// Provides controls and analysis for viewing battle replays
    /// </summary>
    public class BattleReplayPanel : MonoBehaviour
    {
        [Header("Main Panels")]
        [SerializeField] private GameObject replayListPanel;
        [SerializeField] private GameObject replayViewerPanel;
        [SerializeField] private GameObject analysisPanel;
        
        [Header("Replay List")]
        [SerializeField] private Transform replayListContent;
        [SerializeField] private GameObject replayListItemPrefab;
        [SerializeField] private TMP_Dropdown filterDropdown;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private Button refreshButton;
        
        [Header("Playback Controls")]
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button skipBackButton;
        [SerializeField] private Button skipForwardButton;
        [SerializeField] private Button slowMotionButton;
        [SerializeField] private Button normalSpeedButton;
        [SerializeField] private Button fastForwardButton;
        [SerializeField] private Slider timelineSlider;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text speedText;
        
        [Header("Battle Info Display")]
        [SerializeField] private TMP_Text attackerNameText;
        [SerializeField] private TMP_Text defenderNameText;
        [SerializeField] private TMP_Text territoryNameText;
        [SerializeField] private TMP_Text battleDateText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Image attackerIcon;
        [SerializeField] private Image defenderIcon;
        
        [Header("Live Stats")]
        [SerializeField] private TMP_Text attackerDamageText;
        [SerializeField] private TMP_Text defenderDamageText;
        [SerializeField] private TMP_Text buildingsRemainingText;
        [SerializeField] private TMP_Text unitsActiveText;
        [SerializeField] private Slider attackerHealthBar;
        [SerializeField] private Slider defenderHealthBar;
        
        [Header("Event Log")]
        [SerializeField] private Transform eventLogContent;
        [SerializeField] private GameObject eventLogItemPrefab;
        [SerializeField] private ScrollRect eventLogScrollRect;
        [SerializeField] private int maxEventLogItems = 50;
        
        [Header("Analysis Tab")]
        [SerializeField] private TMP_Text totalDamageText;
        [SerializeField] private TMP_Text avgDPSText;
        [SerializeField] private TMP_Text unitsDeployedText;
        [SerializeField] private TMP_Text unitsLostText;
        [SerializeField] private TMP_Text buildingsDestroyedText;
        [SerializeField] private TMP_Text battleDurationText;
        [SerializeField] private Image damageBreakdownChart;
        
        [Header("Icons")]
        [SerializeField] private Sprite playIcon;
        [SerializeField] private Sprite pauseIcon;
        [SerializeField] private Sprite victoryIcon;
        [SerializeField] private Sprite defeatIcon;
        
        // State
        private BattleReplaySystem _replaySystem;
        private List<BattleReplaySummary> _loadedReplays = new List<BattleReplaySummary>();
        private List<GameObject> _replayListItems = new List<GameObject>();
        private List<GameObject> _eventLogItems = new List<GameObject>();
        private bool _isPlaying;
        private int _runningDamage;
        private int _buildingsRemaining;
        private int _unitsActive;
        
        private void Awake()
        {
            _replaySystem = FindObjectOfType<BattleReplaySystem>();
            SetupEventHandlers();
        }
        
        private void SetupEventHandlers()
        {
            // Button handlers
            playPauseButton?.onClick.AddListener(OnPlayPauseClicked);
            restartButton?.onClick.AddListener(OnRestartClicked);
            skipBackButton?.onClick.AddListener(OnSkipBackClicked);
            skipForwardButton?.onClick.AddListener(OnSkipForwardClicked);
            slowMotionButton?.onClick.AddListener(OnSlowMotionClicked);
            normalSpeedButton?.onClick.AddListener(OnNormalSpeedClicked);
            fastForwardButton?.onClick.AddListener(OnFastForwardClicked);
            refreshButton?.onClick.AddListener(OnRefreshClicked);
            
            // Timeline slider
            timelineSlider?.onValueChanged.AddListener(OnTimelineChanged);
            
            // Filter dropdown
            filterDropdown?.onValueChanged.AddListener(OnFilterChanged);
            
            // Search input
            searchInput?.onValueChanged.AddListener(OnSearchChanged);
            
            // Replay system events
            if (_replaySystem != null)
            {
                _replaySystem.OnReplayLoaded += OnReplayLoaded;
                _replaySystem.OnReplayStarted += OnReplayStarted;
                _replaySystem.OnReplayPaused += OnReplayPaused;
                _replaySystem.OnReplayEnded += OnReplayEnded;
                _replaySystem.OnEventPlayed += OnEventPlayed;
            }
        }
        
        private void OnDestroy()
        {
            if (_replaySystem != null)
            {
                _replaySystem.OnReplayLoaded -= OnReplayLoaded;
                _replaySystem.OnReplayStarted -= OnReplayStarted;
                _replaySystem.OnReplayPaused -= OnReplayPaused;
                _replaySystem.OnReplayEnded -= OnReplayEnded;
                _replaySystem.OnEventPlayed -= OnEventPlayed;
            }
        }
        
        #region Panel Control
        
        public void Show()
        {
            gameObject.SetActive(true);
            ShowReplayList();
            RefreshReplayList();
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
            _replaySystem?.Stop();
        }
        
        public void ShowReplayList()
        {
            replayListPanel?.SetActive(true);
            replayViewerPanel?.SetActive(false);
            analysisPanel?.SetActive(false);
        }
        
        public void ShowReplayViewer()
        {
            replayListPanel?.SetActive(false);
            replayViewerPanel?.SetActive(true);
            analysisPanel?.SetActive(false);
        }
        
        public void ShowAnalysis()
        {
            replayListPanel?.SetActive(false);
            replayViewerPanel?.SetActive(false);
            analysisPanel?.SetActive(true);
            UpdateAnalysisPanel();
        }
        
        public void BackToList()
        {
            _replaySystem?.Stop();
            ShowReplayList();
        }
        
        #endregion
        
        #region Replay List
        
        private async void RefreshReplayList()
        {
            // Clear existing items
            foreach (var item in _replayListItems)
            {
                if (item != null) Destroy(item);
            }
            _replayListItems.Clear();
            _loadedReplays.Clear();
            
            if (_replaySystem == null) return;
            
            // Get current filter
            ReplayFilter filter = (ReplayFilter)(filterDropdown?.value ?? 0);
            
            // Load replays based on filter
            string currentPlayerId = GetCurrentPlayerId();
            
            switch (filter)
            {
                case ReplayFilter.AllMine:
                    _loadedReplays = await _replaySystem.GetPlayerReplays(currentPlayerId);
                    break;
                case ReplayFilter.MyAttacks:
                    _loadedReplays = await _replaySystem.GetPlayerReplays(currentPlayerId, attacksOnly: true);
                    break;
                case ReplayFilter.MyDefenses:
                    _loadedReplays = await _replaySystem.GetPlayerReplays(currentPlayerId, defensesOnly: true);
                    break;
            }
            
            // Apply search filter
            string searchText = searchInput?.text?.ToLower() ?? "";
            if (!string.IsNullOrEmpty(searchText))
            {
                _loadedReplays = _loadedReplays.FindAll(r =>
                    r.AttackerName.ToLower().Contains(searchText) ||
                    r.DefenderName.ToLower().Contains(searchText) ||
                    r.TerritoryId.ToLower().Contains(searchText));
            }
            
            // Create list items
            foreach (var replay in _loadedReplays)
            {
                CreateReplayListItem(replay);
            }
        }
        
        private void CreateReplayListItem(BattleReplaySummary replay)
        {
            if (replayListItemPrefab == null || replayListContent == null) return;
            
            var item = Instantiate(replayListItemPrefab, replayListContent);
            _replayListItems.Add(item);
            
            // Configure item display
            var texts = item.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 3)
            {
                texts[0].text = replay.WasAttacker 
                    ? $"Attack on {replay.DefenderName}" 
                    : $"Defense vs {replay.AttackerName}";
                texts[1].text = replay.Timestamp.ToString("MMM dd, HH:mm");
                texts[2].text = GetResultText(replay);
            }
            
            // Result indicator
            var images = item.GetComponentsInChildren<Image>();
            if (images.Length > 0)
            {
                bool won = (replay.WasAttacker && replay.AttackerWon) || 
                           (!replay.WasAttacker && !replay.AttackerWon);
                images[0].color = won ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f);
            }
            
            // Click handler
            var button = item.GetComponent<Button>();
            if (button != null)
            {
                string replayId = replay.Id;
                button.onClick.AddListener(() => LoadAndPlayReplay(replayId));
            }
        }
        
        private string GetResultText(BattleReplaySummary replay)
        {
            bool won = (replay.WasAttacker && replay.AttackerWon) || 
                       (!replay.WasAttacker && !replay.AttackerWon);
            string result = won ? "VICTORY" : "DEFEAT";
            var duration = TimeSpan.FromSeconds(replay.Duration);
            return $"{result} • {duration:mm\\:ss}";
        }
        
        private async void LoadAndPlayReplay(string replayId)
        {
            if (_replaySystem == null) return;
            
            bool loaded = await _replaySystem.LoadReplay(replayId);
            if (loaded)
            {
                ShowReplayViewer();
            }
        }
        
        private string GetCurrentPlayerId()
        {
#if FIREBASE_ENABLED
            return Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "";
#else
            return "local_player";
#endif
        }
        
        #endregion
        
        #region Playback Controls
        
        private void OnPlayPauseClicked()
        {
            _replaySystem?.TogglePlayPause();
        }
        
        private void OnRestartClicked()
        {
            _replaySystem?.Restart();
            ClearEventLog();
        }
        
        private void OnSkipBackClicked()
        {
            _replaySystem?.SkipToPreviousEvent();
        }
        
        private void OnSkipForwardClicked()
        {
            _replaySystem?.SkipToNextEvent();
        }
        
        private void OnSlowMotionClicked()
        {
            _replaySystem?.SlowMotion();
            UpdateSpeedText("0.25x");
        }
        
        private void OnNormalSpeedClicked()
        {
            _replaySystem?.NormalSpeed();
            UpdateSpeedText("1x");
        }
        
        private void OnFastForwardClicked()
        {
            _replaySystem?.FastForward();
            UpdateSpeedText("4x");
        }
        
        private void OnTimelineChanged(float value)
        {
            _replaySystem?.OnTimelineSliderChanged(value);
        }
        
        private void OnFilterChanged(int index)
        {
            RefreshReplayList();
        }
        
        private void OnSearchChanged(string text)
        {
            RefreshReplayList();
        }
        
        private void OnRefreshClicked()
        {
            RefreshReplayList();
        }
        
        private void UpdateSpeedText(string speed)
        {
            if (speedText != null)
            {
                speedText.text = speed;
            }
        }
        
        #endregion
        
        #region Replay System Events
        
        private void OnReplayLoaded(BattleReplay replay)
        {
            // Update battle info display
            if (attackerNameText != null) attackerNameText.text = replay.AttackerName;
            if (defenderNameText != null) defenderNameText.text = replay.DefenderName;
            if (territoryNameText != null) territoryNameText.text = replay.TerritoryName;
            if (battleDateText != null) battleDateText.text = replay.Timestamp.ToString("MMMM dd, yyyy HH:mm");
            
            if (resultText != null)
            {
                resultText.text = replay.AttackerWon ? "Attacker Victory" : "Defender Victory";
                resultText.color = replay.AttackerWon ? Color.red : Color.blue;
            }
            
            // Initialize stats
            _runningDamage = 0;
            _buildingsRemaining = replay.InitialBuildings.Count;
            _unitsActive = 0;
            
            UpdateLiveStats();
            ClearEventLog();
            
            // Reset timeline
            if (timelineSlider != null) timelineSlider.value = 0f;
            UpdateTimeText(0f, replay.Duration);
        }
        
        private void OnReplayStarted()
        {
            _isPlaying = true;
            UpdatePlayPauseButton(true);
        }
        
        private void OnReplayPaused()
        {
            _isPlaying = false;
            UpdatePlayPauseButton(false);
        }
        
        private void OnReplayEnded()
        {
            _isPlaying = false;
            UpdatePlayPauseButton(false);
            
            // Show analysis prompt
            // Could show a popup here suggesting to view the analysis
        }
        
        private void OnEventPlayed(BattleEvent evt)
        {
            // Update live stats based on event
            switch (evt.Type)
            {
                case BattleEventType.UnitSpawned:
                    _unitsActive++;
                    break;
                    
                case BattleEventType.UnitDied:
                    _unitsActive--;
                    break;
                    
                case BattleEventType.UnitAttacked:
                case BattleEventType.BuildingDamaged:
                    _runningDamage += (int)evt.Value;
                    break;
                    
                case BattleEventType.BuildingDestroyed:
                    _buildingsRemaining--;
                    break;
            }
            
            UpdateLiveStats();
            AddEventLogEntry(evt);
        }
        
        private void UpdatePlayPauseButton(bool isPlaying)
        {
            if (playPauseButton == null) return;
            
            var image = playPauseButton.GetComponentInChildren<Image>();
            if (image != null)
            {
                image.sprite = isPlaying ? pauseIcon : playIcon;
            }
        }
        
        private void UpdateTimeText(float current, float total)
        {
            if (timeText == null) return;
            
            var currentSpan = TimeSpan.FromSeconds(current);
            var totalSpan = TimeSpan.FromSeconds(total);
            timeText.text = $"{currentSpan:mm\\:ss} / {totalSpan:mm\\:ss}";
        }
        
        private void UpdateLiveStats()
        {
            if (attackerDamageText != null)
                attackerDamageText.text = _runningDamage.ToString("N0");
                
            if (buildingsRemainingText != null)
                buildingsRemainingText.text = _buildingsRemaining.ToString();
                
            if (unitsActiveText != null)
                unitsActiveText.text = _unitsActive.ToString();
        }
        
        #endregion
        
        #region Event Log
        
        private void AddEventLogEntry(BattleEvent evt)
        {
            if (eventLogItemPrefab == null || eventLogContent == null) return;
            
            // Limit items
            while (_eventLogItems.Count >= maxEventLogItems)
            {
                var oldest = _eventLogItems[0];
                _eventLogItems.RemoveAt(0);
                if (oldest != null) Destroy(oldest);
            }
            
            // Create new entry
            var item = Instantiate(eventLogItemPrefab, eventLogContent);
            _eventLogItems.Add(item);
            
            // Configure display
            var text = item.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                var time = TimeSpan.FromSeconds(evt.Timestamp);
                text.text = $"[{time:mm\\:ss}] {GetEventDescription(evt)}";
                text.color = GetEventColor(evt);
            }
            
            // Auto-scroll to bottom
            if (eventLogScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                eventLogScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        private string GetEventDescription(BattleEvent evt)
        {
            switch (evt.Type)
            {
                case BattleEventType.UnitSpawned:
                    return $"{evt.UnitType} deployed";
                    
                case BattleEventType.UnitDied:
                    return $"{evt.UnitType} destroyed";
                    
                case BattleEventType.UnitAttacked:
                    return $"{evt.UnitType} dealt {evt.Value:N0} damage";
                    
                case BattleEventType.BuildingDamaged:
                    return $"Building took {evt.Value:N0} damage";
                    
                case BattleEventType.BuildingDestroyed:
                    return "Building destroyed!";
                    
                case BattleEventType.DefenseFired:
                    return "Defense fired";
                    
                case BattleEventType.SpecialAbilityUsed:
                    return $"Special: {evt.AbilityType}";
                    
                case BattleEventType.BattleEnded:
                    return "Battle ended";
                    
                default:
                    return evt.Type.ToString();
            }
        }
        
        private Color GetEventColor(BattleEvent evt)
        {
            switch (evt.Type)
            {
                case BattleEventType.UnitSpawned:
                    return Color.cyan;
                    
                case BattleEventType.UnitDied:
                    return evt.IsAttackerUnit ? Color.red : Color.blue;
                    
                case BattleEventType.BuildingDestroyed:
                    return Color.yellow;
                    
                case BattleEventType.SpecialAbilityUsed:
                    return Color.magenta;
                    
                default:
                    return Color.white;
            }
        }
        
        private void ClearEventLog()
        {
            foreach (var item in _eventLogItems)
            {
                if (item != null) Destroy(item);
            }
            _eventLogItems.Clear();
        }
        
        #endregion
        
        #region Analysis Panel
        
        private void UpdateAnalysisPanel()
        {
            if (_replaySystem == null) return;
            
            // Get damage breakdown
            var damageByUnit = _replaySystem.GetDamageByUnitType();
            
            float totalDamage = 0f;
            foreach (var kvp in damageByUnit)
            {
                totalDamage += kvp.Value;
            }
            
            if (totalDamageText != null)
                totalDamageText.text = totalDamage.ToString("N0");
            
            // Calculate average DPS
            var timeline = _replaySystem.GetDamageTimeline(1f);
            if (timeline.Count > 0 && avgDPSText != null)
            {
                float totalTime = timeline[timeline.Count - 1].x;
                if (totalTime > 0)
                {
                    avgDPSText.text = (totalDamage / totalTime).ToString("N1") + " DPS";
                }
            }
            
            // Build damage breakdown description
            // In a real implementation, this would render a pie chart
            Debug.Log("[BattleReplayPanel] Damage breakdown:");
            foreach (var kvp in damageByUnit)
            {
                float percentage = (kvp.Value / totalDamage) * 100f;
                Debug.Log($"  {kvp.Key}: {kvp.Value:N0} ({percentage:N1}%)");
            }
        }
        
        #endregion
        
        #region Enums
        
        private enum ReplayFilter
        {
            AllMine = 0,
            MyAttacks = 1,
            MyDefenses = 2
        }
        
        #endregion
    }
}
