using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Live Event Banner System for displaying time-limited events.
    /// Features:
    /// - Animated event banners with countdown
    /// - Multiple event types with unique visuals
    /// - FOMO mechanics (urgency indicators)
    /// - Reward previews
    /// - Auto-rotation between events
    /// </summary>
    public class LiveEventBannerManager : MonoBehaviour
    {
        [Header("Banner Components")]
        [SerializeField] private RectTransform bannerContainer;
        [SerializeField] private CanvasGroup bannerCanvasGroup;
        [SerializeField] private Image bannerBackground;
        [SerializeField] private Image eventIcon;
        [SerializeField] private Image eventBorder;
        [SerializeField] private Image glowEffect;
        
        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI eventTitleText;
        [SerializeField] private TextMeshProUGUI eventDescriptionText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private TextMeshProUGUI participantsText;
        
        [Header("Buttons")]
        [SerializeField] private Button joinButton;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Button detailsButton;
        
        [Header("Reward Preview")]
        [SerializeField] private RectTransform rewardContainer;
        [SerializeField] private GameObject rewardItemPrefab;
        
        [Header("Animation")]
        [SerializeField] private float slideInDuration = 0.5f;
        [SerializeField] private float slideOutDuration = 0.3f;
        [SerializeField] private float rotationInterval = 15f;
        [SerializeField] private AnimationCurve slideCurve;
        
        [Header("Urgency Settings")]
        [SerializeField] private float urgentThresholdMinutes = 60f;
        [SerializeField] private float criticalThresholdMinutes = 15f;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.4f, 0.8f);
        [SerializeField] private Color urgentColor = new Color(0.8f, 0.6f, 0.1f);
        [SerializeField] private Color criticalColor = new Color(0.8f, 0.2f, 0.2f);
        
        // Singleton
        private static LiveEventBannerManager _instance;
        public static LiveEventBannerManager Instance => _instance;
        
        // State
        private List<LiveEvent> _activeEvents = new List<LiveEvent>();
        private int _currentEventIndex;
        private LiveEvent _displayedEvent;
        private bool _isVisible;
        private Coroutine _rotationCoroutine;
        private Coroutine _countdownCoroutine;
        
        // Events
        public event Action<LiveEvent> OnEventJoined;
        public event Action<LiveEvent> OnEventDismissed;
        public event Action<LiveEvent> OnEventExpired;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            if (slideCurve == null || slideCurve.length == 0)
            {
                slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            }
            
            InitializeButtons();
        }
        
        private void InitializeButtons()
        {
            joinButton?.onClick.AddListener(OnJoinClicked);
            dismissButton?.onClick.AddListener(OnDismissClicked);
            detailsButton?.onClick.AddListener(OnDetailsClicked);
        }
        
        private void Start()
        {
            // Hide initially
            if (bannerCanvasGroup != null)
            {
                bannerCanvasGroup.alpha = 0;
            }
            
            // Start with mock events for demo
            LoadMockEvents();
        }
        
        private void LoadMockEvents()
        {
            // Demo events
            AddEvent(new LiveEvent
            {
                id = "territory_rush",
                title = "TERRITORY RUSH",
                description = "Capture 5 territories for bonus rewards!",
                type = EventType.TerritoryContest,
                endTime = DateTime.Now.AddHours(2),
                rewards = new[] { "500 Gold", "100 Gems", "Rare Banner" },
                participants = 1247
            });
            
            AddEvent(new LiveEvent
            {
                id = "double_resources",
                title = "DOUBLE RESOURCES",
                description = "All resource production doubled!",
                type = EventType.ResourceBonus,
                endTime = DateTime.Now.AddMinutes(45),
                rewards = new[] { "2x Production" },
                participants = 3892
            });
            
            AddEvent(new LiveEvent
            {
                id = "alliance_war",
                title = "ALLIANCE WAR: FINALS",
                description = "Top 8 alliances battle for supremacy!",
                type = EventType.AllianceWar,
                endTime = DateTime.Now.AddHours(6),
                rewards = new[] { "Legendary Chest", "1000 Gems", "Exclusive Title" },
                participants = 8
            });
        }
        
        #region Public API
        
        /// <summary>
        /// Add a new live event
        /// </summary>
        public void AddEvent(LiveEvent evt)
        {
            if (evt == null || _activeEvents.Exists(e => e.id == evt.id))
                return;
            
            _activeEvents.Add(evt);
            _activeEvents.Sort((a, b) => a.endTime.CompareTo(b.endTime));
            
            if (!_isVisible && _activeEvents.Count > 0)
            {
                ShowBanner();
            }
        }
        
        /// <summary>
        /// Remove an event
        /// </summary>
        public void RemoveEvent(string eventId)
        {
            _activeEvents.RemoveAll(e => e.id == eventId);
            
            if (_displayedEvent?.id == eventId)
            {
                ShowNextEvent();
            }
            
            if (_activeEvents.Count == 0)
            {
                HideBanner();
            }
        }
        
        /// <summary>
        /// Show the banner
        /// </summary>
        public void ShowBanner()
        {
            if (_activeEvents.Count == 0) return;
            
            _isVisible = true;
            StartCoroutine(SlideIn());
            
            _rotationCoroutine = StartCoroutine(RotateEvents());
            _countdownCoroutine = StartCoroutine(UpdateCountdowns());
        }
        
        /// <summary>
        /// Hide the banner
        /// </summary>
        public void HideBanner()
        {
            _isVisible = false;
            
            if (_rotationCoroutine != null)
            {
                StopCoroutine(_rotationCoroutine);
            }
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
            }
            
            StartCoroutine(SlideOut());
        }
        
        /// <summary>
        /// Force show specific event
        /// </summary>
        public void ShowEvent(string eventId)
        {
            var evt = _activeEvents.Find(e => e.id == eventId);
            if (evt != null)
            {
                DisplayEvent(evt);
            }
        }
        
        /// <summary>
        /// Get all active events
        /// </summary>
        public List<LiveEvent> GetActiveEvents()
        {
            return new List<LiveEvent>(_activeEvents);
        }
        
        #endregion
        
        #region Display
        
        private void DisplayEvent(LiveEvent evt)
        {
            if (evt == null) return;
            
            _displayedEvent = evt;
            
            // Update text
            if (eventTitleText != null)
            {
                eventTitleText.text = evt.title;
            }
            
            if (eventDescriptionText != null)
            {
                eventDescriptionText.text = evt.description;
            }
            
            if (participantsText != null)
            {
                participantsText.text = $"{evt.participants:N0} participating";
            }
            
            // Update visuals based on type
            ApplyEventStyle(evt);
            
            // Update rewards
            UpdateRewardPreview(evt);
            
            // Update countdown
            UpdateCountdownDisplay(evt);
        }
        
        private void ApplyEventStyle(LiveEvent evt)
        {
            Color themeColor = GetEventColor(evt.type);
            
            // Check urgency
            var timeLeft = evt.endTime - DateTime.Now;
            if (timeLeft.TotalMinutes <= criticalThresholdMinutes)
            {
                themeColor = criticalColor;
                StartPulseAnimation();
            }
            else if (timeLeft.TotalMinutes <= urgentThresholdMinutes)
            {
                themeColor = urgentColor;
            }
            
            // Apply colors
            if (eventBorder != null)
            {
                eventBorder.color = themeColor;
            }
            
            if (glowEffect != null)
            {
                glowEffect.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.5f);
            }
            
            // Set icon based on type
            if (eventIcon != null)
            {
                // Would load actual sprite in production
                eventIcon.color = themeColor;
            }
        }
        
        private Color GetEventColor(EventType type)
        {
            return type switch
            {
                EventType.TerritoryContest => new Color(0.2f, 0.6f, 0.3f),
                EventType.ResourceBonus => new Color(0.8f, 0.7f, 0.2f),
                EventType.AllianceWar => new Color(0.7f, 0.2f, 0.2f),
                EventType.SeasonPass => new Color(0.6f, 0.4f, 0.8f),
                EventType.LimitedOffer => new Color(0.2f, 0.5f, 0.8f),
                EventType.Tournament => new Color(0.8f, 0.5f, 0.2f),
                EventType.WorldBoss => new Color(0.5f, 0.1f, 0.5f),
                _ => normalColor
            };
        }
        
        private void UpdateRewardPreview(LiveEvent evt)
        {
            if (rewardContainer == null || evt.rewards == null) return;
            
            // Clear existing
            foreach (Transform child in rewardContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Add reward items
            foreach (var reward in evt.rewards)
            {
                if (rewardItemPrefab != null)
                {
                    var item = Instantiate(rewardItemPrefab, rewardContainer);
                    var text = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        text.text = reward;
                    }
                }
            }
            
            // Update summary text
            if (rewardText != null)
            {
                rewardText.text = $"Rewards: {string.Join(", ", evt.rewards)}";
            }
        }
        
        private void UpdateCountdownDisplay(LiveEvent evt)
        {
            if (countdownText == null) return;
            
            var timeLeft = evt.endTime - DateTime.Now;
            
            if (timeLeft.TotalSeconds <= 0)
            {
                countdownText.text = "ENDED";
                countdownText.color = Color.gray;
                return;
            }
            
            string timeString;
            if (timeLeft.TotalHours >= 24)
            {
                timeString = $"{(int)timeLeft.TotalDays}d {timeLeft.Hours}h";
            }
            else if (timeLeft.TotalHours >= 1)
            {
                timeString = $"{(int)timeLeft.TotalHours}h {timeLeft.Minutes}m";
            }
            else if (timeLeft.TotalMinutes >= 1)
            {
                timeString = $"{timeLeft.Minutes}m {timeLeft.Seconds}s";
            }
            else
            {
                timeString = $"{timeLeft.Seconds}s";
            }
            
            countdownText.text = $"⏱ {timeString}";
            
            // Color based on urgency
            if (timeLeft.TotalMinutes <= criticalThresholdMinutes)
            {
                countdownText.color = criticalColor;
            }
            else if (timeLeft.TotalMinutes <= urgentThresholdMinutes)
            {
                countdownText.color = urgentColor;
            }
            else
            {
                countdownText.color = Color.white;
            }
        }
        
        #endregion
        
        #region Animation
        
        private IEnumerator SlideIn()
        {
            if (bannerContainer == null) yield break;
            
            // Start off-screen
            Vector2 startPos = new Vector2(bannerContainer.anchoredPosition.x, 200);
            Vector2 endPos = new Vector2(bannerContainer.anchoredPosition.x, 0);
            
            bannerContainer.anchoredPosition = startPos;
            if (bannerCanvasGroup != null)
            {
                bannerCanvasGroup.alpha = 0;
            }
            
            float elapsed = 0;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = slideCurve.Evaluate(elapsed / slideInDuration);
                
                bannerContainer.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                if (bannerCanvasGroup != null)
                {
                    bannerCanvasGroup.alpha = t;
                }
                
                yield return null;
            }
            
            bannerContainer.anchoredPosition = endPos;
            if (bannerCanvasGroup != null)
            {
                bannerCanvasGroup.alpha = 1;
            }
            
            // Show first event
            if (_activeEvents.Count > 0)
            {
                DisplayEvent(_activeEvents[0]);
            }
        }
        
        private IEnumerator SlideOut()
        {
            if (bannerContainer == null) yield break;
            
            Vector2 startPos = bannerContainer.anchoredPosition;
            Vector2 endPos = new Vector2(bannerContainer.anchoredPosition.x, 200);
            
            float elapsed = 0;
            while (elapsed < slideOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / slideOutDuration;
                
                bannerContainer.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                if (bannerCanvasGroup != null)
                {
                    bannerCanvasGroup.alpha = 1 - t;
                }
                
                yield return null;
            }
        }
        
        private IEnumerator RotateEvents()
        {
            while (_isVisible)
            {
                yield return new WaitForSecondsRealtime(rotationInterval);
                
                if (_activeEvents.Count > 1)
                {
                    ShowNextEvent();
                }
            }
        }
        
        private void ShowNextEvent()
        {
            if (_activeEvents.Count == 0) return;
            
            _currentEventIndex = (_currentEventIndex + 1) % _activeEvents.Count;
            StartCoroutine(TransitionToEvent(_activeEvents[_currentEventIndex]));
        }
        
        private IEnumerator TransitionToEvent(LiveEvent evt)
        {
            // Fade out
            if (bannerCanvasGroup != null)
            {
                float elapsed = 0;
                while (elapsed < 0.2f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    bannerCanvasGroup.alpha = 1 - (elapsed / 0.2f);
                    yield return null;
                }
            }
            
            // Change content
            DisplayEvent(evt);
            
            // Fade in
            if (bannerCanvasGroup != null)
            {
                float elapsed = 0;
                while (elapsed < 0.2f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    bannerCanvasGroup.alpha = elapsed / 0.2f;
                    yield return null;
                }
                bannerCanvasGroup.alpha = 1;
            }
        }
        
        private IEnumerator UpdateCountdowns()
        {
            while (_isVisible)
            {
                // Update displayed event countdown
                if (_displayedEvent != null)
                {
                    UpdateCountdownDisplay(_displayedEvent);
                    
                    // Check for expiration
                    if (_displayedEvent.endTime <= DateTime.Now)
                    {
                        OnEventExpired?.Invoke(_displayedEvent);
                        RemoveEvent(_displayedEvent.id);
                    }
                }
                
                // Check all events for expiration
                for (int i = _activeEvents.Count - 1; i >= 0; i--)
                {
                    if (_activeEvents[i].endTime <= DateTime.Now)
                    {
                        OnEventExpired?.Invoke(_activeEvents[i]);
                        _activeEvents.RemoveAt(i);
                    }
                }
                
                yield return new WaitForSecondsRealtime(1f);
            }
        }
        
        private void StartPulseAnimation()
        {
            if (glowEffect != null)
            {
                StartCoroutine(PulseGlow());
            }
        }
        
        private IEnumerator PulseGlow()
        {
            while (_displayedEvent != null && 
                   (_displayedEvent.endTime - DateTime.Now).TotalMinutes <= criticalThresholdMinutes)
            {
                float t = (Mathf.Sin(Time.unscaledTime * 4f) + 1) * 0.5f;
                if (glowEffect != null)
                {
                    Color c = glowEffect.color;
                    c.a = Mathf.Lerp(0.3f, 0.8f, t);
                    glowEffect.color = c;
                }
                yield return null;
            }
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnJoinClicked()
        {
            if (_displayedEvent != null)
            {
                OnEventJoined?.Invoke(_displayedEvent);
                
                // Open event details panel
                Debug.Log($"Joining event: {_displayedEvent.title}");
            }
        }
        
        private void OnDismissClicked()
        {
            if (_displayedEvent != null)
            {
                OnEventDismissed?.Invoke(_displayedEvent);
            }
            
            // Show next event or hide
            if (_activeEvents.Count > 1)
            {
                ShowNextEvent();
            }
            else
            {
                HideBanner();
            }
        }
        
        private void OnDetailsClicked()
        {
            if (_displayedEvent != null)
            {
                // Would open detailed event panel
                Debug.Log($"Opening details for: {_displayedEvent.title}");
            }
        }
        
        #endregion
    }
    
    #region Data Classes
    
    [Serializable]
    public class LiveEvent
    {
        public string id;
        public string title;
        public string description;
        public LiveEventType type;
        public DateTime endTime;
        public string[] rewards;
        public int participants;
        public Sprite icon;
        public Sprite banner;
        public string deepLink;
    }
    
    public enum LiveEventType
    {
        TerritoryContest,
        ResourceBonus,
        AllianceWar,
        SeasonPass,
        LimitedOffer,
        Tournament,
        WorldBoss,
        Challenge,
        Special
    }
    
    #endregion
}
