using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApexCitadels.PC.UI
{
    /// <summary>
    /// Rotating event carousel for displaying multiple events
    /// </summary>
    public class EventCarousel : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
        [SerializeField] private float cardWidth = 400f;
        [SerializeField] private float cardSpacing = 20f;
        
        [Header("Navigation")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private RectTransform indicatorContainer;
        [SerializeField] private GameObject indicatorPrefab;
        
        [Header("Animation")]
        [SerializeField] private float scrollDuration = 0.3f;
        [SerializeField] private float autoScrollInterval = 8f;
        [SerializeField] private bool autoScroll = true;
        
        [Header("Card Prefab")]
        [SerializeField] private GameObject eventCardPrefab;
        
        private List<EventCard> _cards = new List<EventCard>();
        private List<Image> _indicators = new List<Image>();
        private int _currentIndex;
        private Coroutine _autoScrollCoroutine;
        private Coroutine _scrollCoroutine;
        
        public event Action<LiveEvent> OnEventSelected;
        
        private void Awake()
        {
            prevButton?.onClick.AddListener(ScrollPrev);
            nextButton?.onClick.AddListener(ScrollNext);
        }
        
        private void OnEnable()
        {
            if (autoScroll)
            {
                _autoScrollCoroutine = StartCoroutine(AutoScrollRoutine());
            }
        }
        
        private void OnDisable()
        {
            if (_autoScrollCoroutine != null)
            {
                StopCoroutine(_autoScrollCoroutine);
            }
        }
        
        /// <summary>
        /// Populate carousel with events
        /// </summary>
        public void SetEvents(List<LiveEvent> events)
        {
            ClearCards();
            
            foreach (var evt in events)
            {
                AddCard(evt);
            }
            
            UpdateIndicators();
            ScrollToIndex(0, false);
        }
        
        private void AddCard(LiveEvent evt)
        {
            if (eventCardPrefab == null || content == null) return;
            
            var cardObj = Instantiate(eventCardPrefab, content);
            var card = cardObj.GetComponent<EventCard>();
            
            if (card != null)
            {
                card.Setup(evt);
                card.OnClicked += () => OnEventSelected?.Invoke(evt);
                _cards.Add(card);
            }
            
            // Position card
            var rect = cardObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                float x = _cards.Count * (cardWidth + cardSpacing);
                rect.anchoredPosition = new Vector2(x, 0);
                rect.sizeDelta = new Vector2(cardWidth, rect.sizeDelta.y);
            }
        }
        
        private void ClearCards()
        {
            foreach (var card in _cards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            _cards.Clear();
        }
        
        private void UpdateIndicators()
        {
            // Clear existing
            foreach (var indicator in _indicators)
            {
                if (indicator != null)
                {
                    Destroy(indicator.gameObject);
                }
            }
            _indicators.Clear();
            
            if (indicatorContainer == null || indicatorPrefab == null) return;
            
            // Create indicators
            for (int i = 0; i < _cards.Count; i++)
            {
                var indicatorObj = Instantiate(indicatorPrefab, indicatorContainer);
                var indicator = indicatorObj.GetComponent<Image>();
                if (indicator != null)
                {
                    _indicators.Add(indicator);
                    
                    int index = i;
                    var button = indicatorObj.GetComponent<Button>();
                    button?.onClick.AddListener(() => ScrollToIndex(index, true));
                }
            }
            
            RefreshIndicators();
        }
        
        private void RefreshIndicators()
        {
            for (int i = 0; i < _indicators.Count; i++)
            {
                if (_indicators[i] != null)
                {
                    _indicators[i].color = i == _currentIndex 
                        ? Color.white 
                        : new Color(1, 1, 1, 0.3f);
                }
            }
        }
        
        public void ScrollPrev()
        {
            int newIndex = _currentIndex - 1;
            if (newIndex < 0) newIndex = _cards.Count - 1;
            ScrollToIndex(newIndex, true);
        }
        
        public void ScrollNext()
        {
            int newIndex = (_currentIndex + 1) % _cards.Count;
            ScrollToIndex(newIndex, true);
        }
        
        public void ScrollToIndex(int index, bool animate)
        {
            if (index < 0 || index >= _cards.Count) return;
            
            _currentIndex = index;
            RefreshIndicators();
            
            float targetX = -index * (cardWidth + cardSpacing);
            
            if (_scrollCoroutine != null)
            {
                StopCoroutine(_scrollCoroutine);
            }
            
            if (animate)
            {
                _scrollCoroutine = StartCoroutine(AnimateScroll(targetX));
            }
            else
            {
                content.anchoredPosition = new Vector2(targetX, content.anchoredPosition.y);
            }
        }
        
        private IEnumerator AnimateScroll(float targetX)
        {
            Vector2 startPos = content.anchoredPosition;
            Vector2 targetPos = new Vector2(targetX, startPos.y);
            
            float elapsed = 0;
            while (elapsed < scrollDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / scrollDuration);
                content.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }
            
            content.anchoredPosition = targetPos;
        }
        
        private IEnumerator AutoScrollRoutine()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(autoScrollInterval);
                
                if (_cards.Count > 1)
                {
                    ScrollNext();
                }
            }
        }
    }
    
    /// <summary>
    /// Individual event card in the carousel
    /// </summary>
    public class EventCard : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI rewardPreviewText;
        [SerializeField] private Button cardButton;
        
        [Header("Type Indicators")]
        [SerializeField] private GameObject territoryIcon;
        [SerializeField] private GameObject warIcon;
        [SerializeField] private GameObject bonusIcon;
        [SerializeField] private GameObject tournamentIcon;
        
        private LiveEvent _event;
        private Coroutine _countdownCoroutine;
        
        public event Action OnClicked;
        
        private void Awake()
        {
            cardButton?.onClick.AddListener(() => OnClicked?.Invoke());
        }
        
        private void OnDestroy()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
            }
        }
        
        public void Setup(LiveEvent evt)
        {
            _event = evt;
            
            if (titleText != null)
            {
                titleText.text = evt.title;
            }
            
            if (descriptionText != null)
            {
                descriptionText.text = evt.description;
            }
            
            if (rewardPreviewText != null && evt.rewards?.Length > 0)
            {
                rewardPreviewText.text = $"[?] {evt.rewards[0]}";
                if (evt.rewards.Length > 1)
                {
                    rewardPreviewText.text += $" +{evt.rewards.Length - 1} more";
                }
            }
            
            // Set icon
            SetTypeIcon(evt.type);
            
            // Apply theme color
            Color themeColor = GetTypeColor(evt.type);
            if (borderImage != null)
            {
                borderImage.color = themeColor;
            }
            
            // Start countdown
            _countdownCoroutine = StartCoroutine(UpdateCountdown());
        }
        
        private void SetTypeIcon(LiveEventType type)
        {
            territoryIcon?.SetActive(type == LiveEventType.TerritoryContest);
            warIcon?.SetActive(type == LiveEventType.AllianceWar);
            bonusIcon?.SetActive(type == LiveEventType.ResourceBonus);
            tournamentIcon?.SetActive(type == LiveEventType.Tournament);
        }
        
        private Color GetTypeColor(LiveEventType type)
        {
            return type switch
            {
                LiveEventType.TerritoryContest => new Color(0.2f, 0.7f, 0.3f),
                LiveEventType.ResourceBonus => new Color(0.9f, 0.7f, 0.2f),
                LiveEventType.AllianceWar => new Color(0.8f, 0.2f, 0.2f),
                LiveEventType.Tournament => new Color(0.9f, 0.5f, 0.2f),
                LiveEventType.SeasonPass => new Color(0.6f, 0.4f, 0.9f),
                LiveEventType.WorldBoss => new Color(0.6f, 0.1f, 0.6f),
                _ => new Color(0.3f, 0.5f, 0.9f)
            };
        }
        
        private IEnumerator UpdateCountdown()
        {
            while (_event != null)
            {
                var timeLeft = _event.endTime - DateTime.Now;
                
                if (timeLeft.TotalSeconds <= 0)
                {
                    if (countdownText != null)
                    {
                        countdownText.text = "ENDED";
                    }
                    yield break;
                }
                
                if (countdownText != null)
                {
                    if (timeLeft.TotalHours >= 1)
                    {
                        countdownText.text = $"⏱ {(int)timeLeft.TotalHours}h {timeLeft.Minutes}m";
                    }
                    else
                    {
                        countdownText.text = $"⏱ {timeLeft.Minutes}m {timeLeft.Seconds}s";
                    }
                    
                    // Urgency color
                    if (timeLeft.TotalMinutes <= 15)
                    {
                        countdownText.color = new Color(0.9f, 0.2f, 0.2f);
                    }
                    else if (timeLeft.TotalMinutes <= 60)
                    {
                        countdownText.color = new Color(0.9f, 0.7f, 0.2f);
                    }
                }
                
                yield return new WaitForSecondsRealtime(1f);
            }
        }
    }
    
    /// <summary>
    /// FOMO (Fear of Missing Out) urgency indicator
    /// </summary>
    public class UrgencyIndicator : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI urgencyText;
        [SerializeField] private Image flashOverlay;
        
        [Header("Settings")]
        [SerializeField] private float flashSpeed = 2f;
        [SerializeField] private Color normalColor = new Color(0.3f, 0.6f, 0.3f);
        [SerializeField] private Color urgentColor = new Color(0.9f, 0.7f, 0.2f);
        [SerializeField] private Color criticalColor = new Color(0.9f, 0.2f, 0.2f);
        
        private UrgencyLevel _level;
        private Coroutine _flashCoroutine;
        
        public void SetUrgency(TimeSpan timeRemaining)
        {
            UrgencyLevel newLevel;
            
            if (timeRemaining.TotalMinutes <= 5)
            {
                newLevel = UrgencyLevel.Critical;
            }
            else if (timeRemaining.TotalMinutes <= 30)
            {
                newLevel = UrgencyLevel.Urgent;
            }
            else if (timeRemaining.TotalHours <= 1)
            {
                newLevel = UrgencyLevel.Warning;
            }
            else
            {
                newLevel = UrgencyLevel.Normal;
            }
            
            if (newLevel != _level)
            {
                _level = newLevel;
                UpdateDisplay();
            }
        }
        
        private void UpdateDisplay()
        {
            Color color;
            string text;
            bool flash = false;
            
            switch (_level)
            {
                case UrgencyLevel.Critical:
                    color = criticalColor;
                    text = "[!] ENDING SOON!";
                    flash = true;
                    break;
                case UrgencyLevel.Urgent:
                    color = urgentColor;
                    text = "[T] HURRY!";
                    flash = true;
                    break;
                case UrgencyLevel.Warning:
                    color = urgentColor;
                    text = "⏱️ LIMITED TIME";
                    break;
                default:
                    color = normalColor;
                    text = "ACTIVE";
                    break;
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.color = color;
            }
            
            if (urgencyText != null)
            {
                urgencyText.text = text;
            }
            
            // Handle flashing
            if (flash && _flashCoroutine == null)
            {
                _flashCoroutine = StartCoroutine(FlashRoutine());
            }
            else if (!flash && _flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
                if (flashOverlay != null)
                {
                    flashOverlay.color = new Color(1, 1, 1, 0);
                }
            }
        }
        
        private IEnumerator FlashRoutine()
        {
            while (true)
            {
                if (flashOverlay != null)
                {
                    float alpha = (Mathf.Sin(Time.unscaledTime * flashSpeed * Mathf.PI) + 1) * 0.25f;
                    flashOverlay.color = new Color(1, 1, 1, alpha);
                }
                yield return null;
            }
        }
    }
    
    public enum UrgencyLevel
    {
        Normal,
        Warning,
        Urgent,
        Critical
    }
    
    /// <summary>
    /// Reward preview panel showing event rewards
    /// </summary>
    public class RewardPreviewPanel : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private RectTransform rewardGrid;
        [SerializeField] private GameObject rewardItemPrefab;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Animation")]
        [SerializeField] private float showDuration = 0.3f;
        
        private List<GameObject> _rewardItems = new List<GameObject>();
        
        public void ShowRewards(string[] rewards)
        {
            ClearRewards();
            
            if (rewards == null) return;
            
            foreach (var reward in rewards)
            {
                AddRewardItem(reward);
            }
            
            StartCoroutine(AnimateShow());
        }
        
        public void Hide()
        {
            StartCoroutine(AnimateHide());
        }
        
        private void AddRewardItem(string reward)
        {
            if (rewardItemPrefab == null || rewardGrid == null) return;
            
            var item = Instantiate(rewardItemPrefab, rewardGrid);
            
            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = reward;
            }
            
            // Parse reward for icon
            var icon = item.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.color = GetRewardColor(reward);
            }
            
            _rewardItems.Add(item);
        }
        
        private Color GetRewardColor(string reward)
        {
            reward = reward.ToLower();
            
            if (reward.Contains("legendary") || reward.Contains("1000"))
            {
                return new Color(1f, 0.8f, 0.2f); // Gold
            }
            else if (reward.Contains("rare") || reward.Contains("epic"))
            {
                return new Color(0.6f, 0.3f, 0.9f); // Purple
            }
            else if (reward.Contains("gem"))
            {
                return new Color(0.3f, 0.8f, 0.9f); // Cyan
            }
            else if (reward.Contains("gold"))
            {
                return new Color(0.9f, 0.7f, 0.1f); // Yellow
            }
            else
            {
                return new Color(0.7f, 0.7f, 0.7f); // Silver
            }
        }
        
        private void ClearRewards()
        {
            foreach (var item in _rewardItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            _rewardItems.Clear();
        }
        
        private IEnumerator AnimateShow()
        {
            if (canvasGroup == null) yield break;
            
            canvasGroup.alpha = 0;
            gameObject.SetActive(true);
            
            float elapsed = 0;
            while (elapsed < showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = elapsed / showDuration;
                yield return null;
            }
            
            canvasGroup.alpha = 1;
        }
        
        private IEnumerator AnimateHide()
        {
            if (canvasGroup == null) yield break;
            
            float elapsed = 0;
            while (elapsed < showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1 - (elapsed / showDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Event details popup with full information
    /// </summary>
    public class EventDetailsPopup : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image backgroundOverlay;
        [SerializeField] private RectTransform popupPanel;
        
        [Header("Content")]
        [SerializeField] private Image eventBanner;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI rulesText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private RewardPreviewPanel rewardPanel;
        [SerializeField] private TextMeshProUGUI participantsText;
        [SerializeField] private TextMeshProUGUI leaderboardPreviewText;
        
        [Header("Buttons")]
        [SerializeField] private Button joinButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button leaderboardButton;
        
        [Header("Animation")]
        [SerializeField] private float animationDuration = 0.3f;
        
        private LiveEvent _event;
        private Coroutine _countdownCoroutine;
        
        public event Action<LiveEvent> OnJoin;
        public event Action OnClose;
        
        private void Awake()
        {
            joinButton?.onClick.AddListener(OnJoinClicked);
            closeButton?.onClick.AddListener(Hide);
            backgroundOverlay?.GetComponent<Button>()?.onClick.AddListener(Hide);
        }
        
        public void Show(LiveEvent evt)
        {
            _event = evt;
            gameObject.SetActive(true);
            
            // Populate content
            if (titleText != null) titleText.text = evt.title;
            if (descriptionText != null) descriptionText.text = evt.description;
            if (participantsText != null) participantsText.text = $"{evt.participants:N0} participants";
            
            // Show rewards
            rewardPanel?.ShowRewards(evt.rewards);
            
            // Start countdown
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
            }
            _countdownCoroutine = StartCoroutine(UpdateCountdown());
            
            // Animate in
            StartCoroutine(AnimateIn());
        }
        
        public void Hide()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
            }
            
            OnClose?.Invoke();
            StartCoroutine(AnimateOut());
        }
        
        private void OnJoinClicked()
        {
            if (_event != null)
            {
                OnJoin?.Invoke(_event);
            }
            Hide();
        }
        
        private IEnumerator AnimateIn()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
            }
            
            if (popupPanel != null)
            {
                popupPanel.localScale = Vector3.one * 0.8f;
            }
            
            float elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / animationDuration;
                
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = t;
                }
                
                if (popupPanel != null)
                {
                    popupPanel.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);
                }
                
                yield return null;
            }
        }
        
        private IEnumerator AnimateOut()
        {
            float elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / animationDuration;
                
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1 - t;
                }
                
                if (popupPanel != null)
                {
                    popupPanel.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.8f, t);
                }
                
                yield return null;
            }
            
            gameObject.SetActive(false);
        }
        
        private IEnumerator UpdateCountdown()
        {
            while (_event != null && gameObject.activeInHierarchy)
            {
                var timeLeft = _event.endTime - DateTime.Now;
                
                if (countdownText != null)
                {
                    if (timeLeft.TotalSeconds <= 0)
                    {
                        countdownText.text = "Event has ended";
                    }
                    else if (timeLeft.TotalDays >= 1)
                    {
                        countdownText.text = $"Ends in {(int)timeLeft.TotalDays}d {timeLeft.Hours}h {timeLeft.Minutes}m";
                    }
                    else if (timeLeft.TotalHours >= 1)
                    {
                        countdownText.text = $"Ends in {(int)timeLeft.TotalHours}h {timeLeft.Minutes}m {timeLeft.Seconds}s";
                    }
                    else
                    {
                        countdownText.text = $"Ends in {timeLeft.Minutes}m {timeLeft.Seconds}s";
                    }
                }
                
                yield return new WaitForSecondsRealtime(1f);
            }
        }
    }
    
    /// <summary>
    /// Mini banner for persistent event reminder
    /// </summary>
    public class MiniEventBanner : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private Button expandButton;
        
        [Header("Pulse Animation")]
        [SerializeField] private Image pulseRing;
        [SerializeField] private float pulseSpeed = 1f;
        
        private LiveEvent _trackedEvent;
        
        public event Action OnExpand;
        
        private void Awake()
        {
            expandButton?.onClick.AddListener(() => OnExpand?.Invoke());
        }
        
        private void Update()
        {
            if (_trackedEvent != null)
            {
                UpdateCountdown();
            }
            
            // Pulse animation
            if (pulseRing != null)
            {
                float scale = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI) * 0.1f;
                pulseRing.transform.localScale = Vector3.one * scale;
                
                float alpha = (Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI) + 1) * 0.3f + 0.2f;
                Color c = pulseRing.color;
                c.a = alpha;
                pulseRing.color = c;
            }
        }
        
        public void TrackEvent(LiveEvent evt)
        {
            _trackedEvent = evt;
            gameObject.SetActive(true);
        }
        
        public void StopTracking()
        {
            _trackedEvent = null;
            gameObject.SetActive(false);
        }
        
        private void UpdateCountdown()
        {
            if (countdownText == null) return;
            
            var timeLeft = _trackedEvent.endTime - DateTime.Now;
            
            if (timeLeft.TotalSeconds <= 0)
            {
                countdownText.text = "00:00";
                return;
            }
            
            if (timeLeft.TotalHours >= 1)
            {
                countdownText.text = $"{(int)timeLeft.TotalHours}:{timeLeft.Minutes:D2}";
            }
            else
            {
                countdownText.text = $"{timeLeft.Minutes}:{timeLeft.Seconds:D2}";
            }
        }
    }
}
