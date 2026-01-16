using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ApexCitadels.WorldEvents;
using ApexCitadels.SeasonPass;
using ApexCitadels.Social;
using ApexCitadels.Chat;
using ApexCitadels.Referrals;
using ApexCitadels.Analytics;

namespace ApexCitadels.UI
{
    /// <summary>
    /// Main HUD controller for gameplay
    /// </summary>
    public class GameHUDController : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Slider xpBar;
        [SerializeField] private TextMeshProUGUI currencyText;
        [SerializeField] private TextMeshProUGUI premiumCurrencyText;

        [Header("Event Banner")]
        [SerializeField] private GameObject eventBannerContainer;
        [SerializeField] private TextMeshProUGUI eventNameText;
        [SerializeField] private TextMeshProUGUI eventTimerText;
        [SerializeField] private Image eventIcon;

        [Header("Season Pass Widget")]
        [SerializeField] private GameObject seasonPassWidget;
        [SerializeField] private TextMeshProUGUI seasonLevelText;
        [SerializeField] private Slider seasonXpBar;
        [SerializeField] private Button claimRewardButton;
        [SerializeField] private GameObject newRewardIndicator;

        [Header("Social Widget")]
        [SerializeField] private Button friendsButton;
        [SerializeField] private TextMeshProUGUI friendRequestCountText;
        [SerializeField] private Button chatButton;
        [SerializeField] private TextMeshProUGUI unreadMessagesText;
        [SerializeField] private Button giftButton;
        [SerializeField] private TextMeshProUGUI pendingGiftsText;

        [Header("Quick Actions")]
        [SerializeField] private Button shareButton;
        [SerializeField] private Button dailyRewardButton;
        [SerializeField] private GameObject dailyRewardAvailableIndicator;

        [Header("Notifications")]
        [SerializeField] private Transform notificationContainer;
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private float notificationDuration = 5f;

        [Header("Panels")]
        [SerializeField] private GameObject worldEventsPanel;
        [SerializeField] private GameObject seasonPassPanel;
        [SerializeField] private GameObject friendsPanel;
        [SerializeField] private GameObject chatPanel;
        [SerializeField] private GameObject referralPanel;

        private Queue<NotificationData> _notificationQueue = new Queue<NotificationData>();
        private bool _isShowingNotification;

        private void Start()
        {
            SubscribeToEvents();
            SetupButtonListeners();
            RefreshUI();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            // World Events
            if (WorldEventManager.Instance != null)
            {
                WorldEventManager.Instance.OnEventsUpdated += OnWorldEventsUpdated;
                WorldEventManager.Instance.OnEventStarted += OnEventStarted;
                WorldEventManager.Instance.OnEventEnded += OnEventEnded;
            }

            // Season Pass
            if (SeasonPassManager.Instance != null)
            {
                SeasonPassManager.Instance.OnProgressUpdated += OnSeasonProgressUpdated;
                SeasonPassManager.Instance.OnLevelUp += OnSeasonLevelUp;
                SeasonPassManager.Instance.OnRewardClaimed += OnSeasonRewardClaimed;
            }

            // Friends
            if (FriendsManager.Instance != null)
            {
                FriendsManager.Instance.OnRequestsUpdated += OnFriendRequestsUpdated;
                FriendsManager.Instance.OnGiftReceived += OnGiftReceived;
                FriendsManager.Instance.OnFriendRequestReceived += OnFriendRequestReceived;
            }

            // Chat
            if (ChatManager.Instance != null)
            {
                ChatManager.Instance.OnChannelsUpdated += OnChatChannelsUpdated;
                ChatManager.Instance.OnNewMessage += OnNewChatMessage;
            }

            // Referrals
            if (ReferralManager.Instance != null)
            {
                ReferralManager.Instance.OnMilestoneReached += OnReferralMilestoneReached;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (WorldEventManager.Instance != null)
            {
                WorldEventManager.Instance.OnEventsUpdated -= OnWorldEventsUpdated;
                WorldEventManager.Instance.OnEventStarted -= OnEventStarted;
                WorldEventManager.Instance.OnEventEnded -= OnEventEnded;
            }

            if (SeasonPassManager.Instance != null)
            {
                SeasonPassManager.Instance.OnProgressUpdated -= OnSeasonProgressUpdated;
                SeasonPassManager.Instance.OnLevelUp -= OnSeasonLevelUp;
                SeasonPassManager.Instance.OnRewardClaimed -= OnSeasonRewardClaimed;
            }

            if (FriendsManager.Instance != null)
            {
                FriendsManager.Instance.OnRequestsUpdated -= OnFriendRequestsUpdated;
                FriendsManager.Instance.OnGiftReceived -= OnGiftReceived;
                FriendsManager.Instance.OnFriendRequestReceived -= OnFriendRequestReceived;
            }

            if (ChatManager.Instance != null)
            {
                ChatManager.Instance.OnChannelsUpdated -= OnChatChannelsUpdated;
                ChatManager.Instance.OnNewMessage -= OnNewChatMessage;
            }

            if (ReferralManager.Instance != null)
            {
                ReferralManager.Instance.OnMilestoneReached -= OnReferralMilestoneReached;
            }
        }

        private void SetupButtonListeners()
        {
            if (eventBannerContainer != null)
            {
                var btn = eventBannerContainer.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(OpenWorldEventsPanel);
            }

            if (seasonPassWidget != null)
            {
                var btn = seasonPassWidget.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(OpenSeasonPassPanel);
            }

            if (claimRewardButton != null)
                claimRewardButton.onClick.AddListener(ClaimNextSeasonReward);

            if (friendsButton != null)
                friendsButton.onClick.AddListener(OpenFriendsPanel);

            if (chatButton != null)
                chatButton.onClick.AddListener(OpenChatPanel);

            if (giftButton != null)
                giftButton.onClick.AddListener(ClaimAllGifts);

            if (shareButton != null)
                shareButton.onClick.AddListener(ShareReferralCode);

            if (dailyRewardButton != null)
                dailyRewardButton.onClick.AddListener(ClaimDailyReward);
        }

        public void RefreshUI()
        {
            UpdatePlayerInfo();
            UpdateEventBanner();
            UpdateSeasonPassWidget();
            UpdateSocialWidgets();
        }

        private void UpdatePlayerInfo()
        {
            var player = Player.PlayerManager.Instance?.CurrentPlayer;
            if (player == null) return;

            if (playerNameText != null)
                playerNameText.text = player.DisplayName;

            if (levelText != null)
                levelText.text = $"Lv.{player.Level}";

            if (xpBar != null)
            {
                float xpProgress = (float)player.CurrentXP / player.XPToNextLevel;
                xpBar.value = xpProgress;
            }

            // Currency would come from ResourceManager
        }

        private void UpdateEventBanner()
        {
            if (eventBannerContainer == null) return;

            var events = WorldEventManager.Instance?.ActiveEvents;
            if (events == null || events.Count == 0)
            {
                eventBannerContainer.SetActive(false);
                return;
            }

            // Show the most important active event
            var topEvent = events[0];
            eventBannerContainer.SetActive(true);

            if (eventNameText != null)
                eventNameText.text = topEvent.Name;

            // Timer is updated in Update()
        }

        private void UpdateSeasonPassWidget()
        {
            if (seasonPassWidget == null) return;

            var progress = SeasonPassManager.Instance?.CurrentProgress;
            if (progress == null)
            {
                seasonPassWidget.SetActive(false);
                return;
            }

            seasonPassWidget.SetActive(true);

            if (seasonLevelText != null)
                seasonLevelText.text = $"Tier {progress.CurrentLevel}";

            if (seasonXpBar != null)
            {
                int xpForNext = SeasonPassManager.Instance.GetXPForLevel(progress.CurrentLevel + 1);
                int xpForCurrent = SeasonPassManager.Instance.GetXPForLevel(progress.CurrentLevel);
                float xpProgress = (float)(progress.TotalXP - xpForCurrent) / (xpForNext - xpForCurrent);
                seasonXpBar.value = Mathf.Clamp01(xpProgress);
            }

            // Check for unclaimed rewards
            bool hasUnclaimedRewards = SeasonPassManager.Instance.HasUnclaimedRewards;
            if (claimRewardButton != null)
                claimRewardButton.gameObject.SetActive(hasUnclaimedRewards);
            if (newRewardIndicator != null)
                newRewardIndicator.SetActive(hasUnclaimedRewards);
        }

        private void UpdateSocialWidgets()
        {
            // Friend requests
            var requests = FriendsManager.Instance?.PendingRequests;
            if (friendRequestCountText != null)
            {
                int count = requests?.Count ?? 0;
                friendRequestCountText.text = count > 0 ? count.ToString() : "";
                friendRequestCountText.transform.parent.gameObject.SetActive(count > 0);
            }

            // Unread messages
            if (unreadMessagesText != null)
            {
                int count = ChatManager.Instance?.GetTotalUnreadCount() ?? 0;
                unreadMessagesText.text = count > 0 ? count.ToString() : "";
                unreadMessagesText.transform.parent.gameObject.SetActive(count > 0);
            }

            // Pending gifts
            var gifts = FriendsManager.Instance?.PendingGifts;
            if (pendingGiftsText != null)
            {
                int count = gifts?.Count ?? 0;
                pendingGiftsText.text = count > 0 ? count.ToString() : "";
                pendingGiftsText.transform.parent.gameObject.SetActive(count > 0);
            }

            // Daily reward
            if (dailyRewardAvailableIndicator != null)
            {
                bool available = DailyRewards.DailyRewardManager.Instance?.CanClaimReward ?? false;
                dailyRewardAvailableIndicator.SetActive(available);
            }
        }

        private void Update()
        {
            UpdateEventTimer();
            ProcessNotificationQueue();
        }

        private void UpdateEventTimer()
        {
            if (eventTimerText == null || eventBannerContainer == null || !eventBannerContainer.activeSelf)
                return;

            var events = WorldEventManager.Instance?.ActiveEvents;
            if (events != null && events.Count > 0)
            {
                var topEvent = events[0];
                var remaining = topEvent.EndTime - DateTime.UtcNow;
                
                if (remaining.TotalSeconds > 0)
                {
                    if (remaining.TotalHours >= 1)
                        eventTimerText.text = $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
                    else if (remaining.TotalMinutes >= 1)
                        eventTimerText.text = $"{remaining.Minutes}m {remaining.Seconds}s";
                    else
                        eventTimerText.text = $"{remaining.Seconds}s";
                }
            }
        }

        #region Event Handlers

        private void OnWorldEventsUpdated(List<WorldEvent> events)
        {
            UpdateEventBanner();
        }

        private void OnEventStarted(WorldEvent evt)
        {
            ShowNotification($"🎉 Event Started: {evt.Name}!", NotificationType.Event);
            AnalyticsManager.Instance?.TrackEvent("event_notification_shown", 
                new Dictionary<string, object> { { "event_id", evt.Id } });
        }

        private void OnEventEnded(WorldEvent evt)
        {
            ShowNotification($"Event Ended: {evt.Name}", NotificationType.Info);
            UpdateEventBanner();
        }

        private void OnSeasonProgressUpdated(SeasonProgress progress)
        {
            UpdateSeasonPassWidget();
        }

        private void OnSeasonLevelUp(int newLevel)
        {
            ShowNotification($"🎖️ Season Pass Tier {newLevel}!", NotificationType.Achievement);
            UpdateSeasonPassWidget();
        }

        private void OnSeasonRewardClaimed(SeasonReward reward)
        {
            ShowNotification($"Reward claimed!", NotificationType.Success);
            UpdateSeasonPassWidget();
        }

        private void OnFriendRequestsUpdated(List<FriendRequest> requests)
        {
            UpdateSocialWidgets();
        }

        private void OnFriendRequestReceived(FriendRequest request)
        {
            ShowNotification($"👋 {request.SenderName} sent you a friend request!", NotificationType.Social);
            UpdateSocialWidgets();
        }

        private void OnGiftReceived(Gift gift)
        {
            ShowNotification($"🎁 You received a gift!", NotificationType.Gift);
            UpdateSocialWidgets();
        }

        private void OnChatChannelsUpdated(List<ChatChannel> channels)
        {
            UpdateSocialWidgets();
        }

        private void OnNewChatMessage(ChatMessage message)
        {
            if (!message.IsMine)
            {
                ShowNotification($"💬 {message.SenderName}: {TruncateString(message.Content, 30)}", 
                    NotificationType.Chat);
            }
            UpdateSocialWidgets();
        }

        private void OnReferralMilestoneReached(ReferralMilestone milestone)
        {
            ShowNotification($"🏆 Referral Milestone: {milestone.Description}!", NotificationType.Achievement);
        }

        #endregion

        #region Button Actions

        private void OpenWorldEventsPanel()
        {
            if (worldEventsPanel != null)
                worldEventsPanel.SetActive(true);
            
            AnalyticsManager.Instance?.TrackScreenView("world_events");
        }

        private void OpenSeasonPassPanel()
        {
            if (seasonPassPanel != null)
                seasonPassPanel.SetActive(true);
            
            AnalyticsManager.Instance?.TrackScreenView("season_pass");
        }

        private void OpenFriendsPanel()
        {
            if (friendsPanel != null)
                friendsPanel.SetActive(true);
            
            AnalyticsManager.Instance?.TrackScreenView("friends");
        }

        private void OpenChatPanel()
        {
            if (chatPanel != null)
                chatPanel.SetActive(true);
            
            AnalyticsManager.Instance?.TrackScreenView("chat");
        }

        private async void ClaimNextSeasonReward()
        {
            if (SeasonPassManager.Instance != null)
            {
                await SeasonPassManager.Instance.ClaimAllAvailableRewards();
            }
        }

        private async void ClaimAllGifts()
        {
            if (FriendsManager.Instance != null)
            {
                await FriendsManager.Instance.ClaimAllGifts();
            }
        }

        private void ShareReferralCode()
        {
            ReferralManager.Instance?.ShareReferralCode();
            
            if (referralPanel != null)
                referralPanel.SetActive(true);
        }

        private void ClaimDailyReward()
        {
            DailyRewards.DailyRewardManager.Instance?.ClaimDailyReward();
        }

        #endregion

        #region Notifications

        public enum NotificationType
        {
            Info,
            Success,
            Warning,
            Error,
            Achievement,
            Event,
            Social,
            Chat,
            Gift
        }

        private class NotificationData
        {
            public string Message;
            public NotificationType Type;
            public float Duration;
        }

        public void ShowNotification(string message, NotificationType type = NotificationType.Info, 
            float duration = 0)
        {
            _notificationQueue.Enqueue(new NotificationData
            {
                Message = message,
                Type = type,
                Duration = duration > 0 ? duration : notificationDuration
            });
        }

        private void ProcessNotificationQueue()
        {
            if (_isShowingNotification || _notificationQueue.Count == 0)
                return;

            var notification = _notificationQueue.Dequeue();
            StartCoroutine(ShowNotificationCoroutine(notification));
        }

        private IEnumerator ShowNotificationCoroutine(NotificationData data)
        {
            _isShowingNotification = true;

            if (notificationPrefab != null && notificationContainer != null)
            {
                var notifObj = Instantiate(notificationPrefab, notificationContainer);
                var text = notifObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = data.Message;

                // Set color based on type
                var bg = notifObj.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = GetNotificationColor(data.Type);
                }

                // Animate in
                var canvasGroup = notifObj.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = notifObj.AddComponent<CanvasGroup>();

                canvasGroup.alpha = 0;
                float fadeInTime = 0.3f;
                float elapsed = 0;

                while (elapsed < fadeInTime)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = elapsed / fadeInTime;
                    yield return null;
                }

                // Wait
                yield return new WaitForSeconds(data.Duration);

                // Animate out
                elapsed = 0;
                while (elapsed < fadeInTime)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = 1 - (elapsed / fadeInTime);
                    yield return null;
                }

                Destroy(notifObj);
            }

            _isShowingNotification = false;
        }

        private Color GetNotificationColor(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => new Color(0.2f, 0.7f, 0.3f, 0.9f),
                NotificationType.Warning => new Color(0.9f, 0.7f, 0.2f, 0.9f),
                NotificationType.Error => new Color(0.8f, 0.2f, 0.2f, 0.9f),
                NotificationType.Achievement => new Color(0.9f, 0.6f, 0.1f, 0.9f),
                NotificationType.Event => new Color(0.6f, 0.2f, 0.9f, 0.9f),
                NotificationType.Social => new Color(0.2f, 0.6f, 0.9f, 0.9f),
                NotificationType.Chat => new Color(0.3f, 0.5f, 0.8f, 0.9f),
                NotificationType.Gift => new Color(0.9f, 0.4f, 0.6f, 0.9f),
                _ => new Color(0.3f, 0.3f, 0.3f, 0.9f)
            };
        }

        #endregion

        private string TruncateString(string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
                return str;
            return str.Substring(0, maxLength - 3) + "...";
        }
    }
}
