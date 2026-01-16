using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if FIREBASE_ENABLED
using Firebase.Functions;
#endif
using Newtonsoft.Json;

namespace ApexCitadels.Analytics
{
    /// <summary>
    /// Analytics event types
    /// </summary>
    public static class AnalyticsEvents
    {
        // Core gameplay
        public const string SESSION_START = "session_start";
        public const string SESSION_END = "session_end";
        public const string LEVEL_UP = "level_up";
        public const string TUTORIAL_COMPLETE = "tutorial_complete";
        
        // Combat
        public const string BATTLE_START = "battle_start";
        public const string BATTLE_END = "battle_end";
        public const string TERRITORY_CAPTURED = "territory_captured";
        public const string TERRITORY_LOST = "territory_lost";
        
        // Building
        public const string BUILDING_PLACED = "building_placed";
        public const string BUILDING_UPGRADED = "building_upgraded";
        public const string CITADEL_CREATED = "citadel_created";
        
        // Social
        public const string FRIEND_ADDED = "friend_added";
        public const string ALLIANCE_JOINED = "alliance_joined";
        public const string GIFT_SENT = "gift_sent";
        public const string CHAT_MESSAGE_SENT = "chat_message_sent";
        
        // Economy
        public const string PURCHASE_STARTED = "purchase_started";
        public const string PURCHASE_COMPLETED = "purchase_completed";
        public const string PURCHASE_FAILED = "purchase_failed";
        public const string CURRENCY_SPENT = "currency_spent";
        public const string CURRENCY_EARNED = "currency_earned";
        
        // Season Pass
        public const string SEASON_LEVEL_UP = "season_level_up";
        public const string SEASON_REWARD_CLAIMED = "season_reward_claimed";
        public const string PREMIUM_PASS_PURCHASED = "premium_pass_purchased";
        
        // Events
        public const string EVENT_JOINED = "event_joined";
        public const string EVENT_CONTRIBUTED = "event_contributed";
        public const string EVENT_REWARD_CLAIMED = "event_reward_claimed";
        
        // Referrals
        public const string REFERRAL_CODE_USED = "referral_code_used";
        public const string REFERRAL_REWARD_CLAIMED = "referral_reward_claimed";
        public const string SHARE_COMPLETED = "share_completed";
        
        // Engagement
        public const string DAILY_REWARD_CLAIMED = "daily_reward_claimed";
        public const string ACHIEVEMENT_UNLOCKED = "achievement_unlocked";
        public const string CHALLENGE_COMPLETED = "challenge_completed";
        public const string NOTIFICATION_CLICKED = "notification_clicked";
        
        // AR
        public const string AR_SESSION_START = "ar_session_start";
        public const string AR_SESSION_END = "ar_session_end";
        public const string AR_ANCHOR_PLACED = "ar_anchor_placed";
    }

    /// <summary>
    /// Manages analytics tracking and telemetry
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableAnalytics = true;
        [SerializeField] private int eventBatchSize = 10;
        [SerializeField] private float batchSendIntervalSeconds = 30f;
        [SerializeField] private bool logEventsToConsole = false;

        // State
        private string _sessionId;
        private DateTime _sessionStart;
        private List<Dictionary<string, object>> _eventQueue = new List<Dictionary<string, object>>();
        private FirebaseFunctions _functions;
        private string _userId;
        private Dictionary<string, object> _userProperties = new Dictionary<string, object>();
        private Dictionary<string, string> _abTestAssignments = new Dictionary<string, string>();

        public string SessionId => _sessionId;
        public TimeSpan SessionDuration => DateTime.UtcNow - _sessionStart;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _functions = FirebaseFunctions.DefaultInstance;
            _userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;

            // Start session
            StartSession();

            // Batch send timer
            InvokeRepeating(nameof(FlushEventQueue), batchSendIntervalSeconds, batchSendIntervalSeconds);

            // Load A/B test assignments
            LoadABTestAssignments();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                EndSession();
            }
            else
            {
                StartSession();
            }
        }

        private void OnApplicationQuit()
        {
            EndSession();
        }

        /// <summary>
        /// Start a new analytics session
        /// </summary>
        public void StartSession()
        {
            _sessionId = Guid.NewGuid().ToString();
            _sessionStart = DateTime.UtcNow;

            TrackEvent(AnalyticsEvents.SESSION_START, new Dictionary<string, object>
            {
                { "platform", Application.platform.ToString() },
                { "version", Application.version },
                { "device_model", SystemInfo.deviceModel },
                { "os", SystemInfo.operatingSystem },
                { "screen_resolution", $"{Screen.width}x{Screen.height}" }
            });

            if (logEventsToConsole)
            {
                Debug.Log($"[Analytics] Session started: {_sessionId}");
            }
        }

        /// <summary>
        /// End the current session
        /// </summary>
        public void EndSession()
        {
            TrackEvent(AnalyticsEvents.SESSION_END, new Dictionary<string, object>
            {
                { "duration_seconds", SessionDuration.TotalSeconds }
            });

            // Flush remaining events
            FlushEventQueue();

            if (logEventsToConsole)
            {
                Debug.Log($"[Analytics] Session ended: {_sessionId}, Duration: {SessionDuration}");
            }
        }

        /// <summary>
        /// Track an analytics event
        /// </summary>
        public void TrackEvent(string eventName, Dictionary<string, object> properties = null)
        {
            if (!enableAnalytics) return;

            var eventData = new Dictionary<string, object>
            {
                { "eventName", eventName },
                { "sessionId", _sessionId },
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "userId", _userId }
            };

            // Add user properties
            foreach (var prop in _userProperties)
            {
                eventData[$"user_{prop.Key}"] = prop.Value;
            }

            // Add custom properties
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    eventData[prop.Key] = prop.Value;
                }
            }

            // Add to queue
            _eventQueue.Add(eventData);

            if (logEventsToConsole)
            {
                Debug.Log($"[Analytics] Event: {eventName} - {JsonConvert.SerializeObject(properties)}");
            }

            // Flush if batch is full
            if (_eventQueue.Count >= eventBatchSize)
            {
                FlushEventQueue();
            }
        }

        /// <summary>
        /// Set a user property
        /// </summary>
        public void SetUserProperty(string name, object value)
        {
            _userProperties[name] = value;
        }

        /// <summary>
        /// Track a screen view
        /// </summary>
        public void TrackScreenView(string screenName)
        {
            TrackEvent("screen_view", new Dictionary<string, object>
            {
                { "screen_name", screenName }
            });
        }

        /// <summary>
        /// Track a timed event (start)
        /// </summary>
        private Dictionary<string, DateTime> _timedEvents = new Dictionary<string, DateTime>();

        public void StartTimedEvent(string eventName)
        {
            _timedEvents[eventName] = DateTime.UtcNow;
        }

        /// <summary>
        /// Track a timed event (end)
        /// </summary>
        public void EndTimedEvent(string eventName, Dictionary<string, object> properties = null)
        {
            if (_timedEvents.TryGetValue(eventName, out var startTime))
            {
                var duration = DateTime.UtcNow - startTime;
                var props = properties ?? new Dictionary<string, object>();
                props["duration_seconds"] = duration.TotalSeconds;
                
                TrackEvent(eventName, props);
                _timedEvents.Remove(eventName);
            }
        }

        /// <summary>
        /// Track a purchase
        /// </summary>
        public void TrackPurchase(string productId, decimal price, string currency, bool success)
        {
            TrackEvent(success ? AnalyticsEvents.PURCHASE_COMPLETED : AnalyticsEvents.PURCHASE_FAILED,
                new Dictionary<string, object>
                {
                    { "product_id", productId },
                    { "price", price },
                    { "currency", currency }
                });
        }

        /// <summary>
        /// Track currency spent
        /// </summary>
        public void TrackCurrencySpent(string currencyType, int amount, string itemCategory, string itemId)
        {
            TrackEvent(AnalyticsEvents.CURRENCY_SPENT, new Dictionary<string, object>
            {
                { "currency_type", currencyType },
                { "amount", amount },
                { "item_category", itemCategory },
                { "item_id", itemId }
            });
        }

        /// <summary>
        /// Track currency earned
        /// </summary>
        public void TrackCurrencyEarned(string currencyType, int amount, string source)
        {
            TrackEvent(AnalyticsEvents.CURRENCY_EARNED, new Dictionary<string, object>
            {
                { "currency_type", currencyType },
                { "amount", amount },
                { "source", source }
            });
        }

        /// <summary>
        /// Track level up
        /// </summary>
        public void TrackLevelUp(int newLevel)
        {
            TrackEvent(AnalyticsEvents.LEVEL_UP, new Dictionary<string, object>
            {
                { "level", newLevel }
            });
            SetUserProperty("level", newLevel);
        }

        /// <summary>
        /// Track battle result
        /// </summary>
        public void TrackBattleResult(bool won, string territoryId, int attackerPower, int defenderPower)
        {
            TrackEvent(AnalyticsEvents.BATTLE_END, new Dictionary<string, object>
            {
                { "result", won ? "victory" : "defeat" },
                { "territory_id", territoryId },
                { "attacker_power", attackerPower },
                { "defender_power", defenderPower }
            });
        }

        /// <summary>
        /// Get A/B test variant for user
        /// </summary>
        public string GetABTestVariant(string testName)
        {
            return _abTestAssignments.TryGetValue(testName, out var variant) ? variant : "control";
        }

        /// <summary>
        /// Record A/B test conversion
        /// </summary>
        public async Task RecordABTestConversion(string testName, string conversionType)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("recordABTestConversion");
                var data = new Dictionary<string, object>
                {
                    { "testName", testName },
                    { "conversionType", conversionType }
                };
                await callable.CallAsync(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to record A/B test conversion: {e.Message}");
            }
        }

        private async void LoadABTestAssignments()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getABTestVariant");
                
                // Load assignments for active tests
                string[] activeTests = { "onboarding_flow", "reward_amounts", "ui_theme" };
                
                foreach (var testName in activeTests)
                {
                    var data = new Dictionary<string, object> { { "testName", testName } };
                    var result = await callable.CallAsync(data);
                    
                    var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                    if (response.ContainsKey("variant"))
                    {
                        _abTestAssignments[testName] = response["variant"].ToString();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load A/B test assignments: {e.Message}");
            }
        }

        private async void FlushEventQueue()
        {
            if (_eventQueue.Count == 0) return;

            var eventsToSend = new List<Dictionary<string, object>>(_eventQueue);
            _eventQueue.Clear();

            try
            {
                var callable = _functions.GetHttpsCallable("trackEvent");
                
                foreach (var evt in eventsToSend)
                {
                    await callable.CallAsync(evt);
                }

                if (logEventsToConsole)
                {
                    Debug.Log($"[Analytics] Flushed {eventsToSend.Count} events");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to flush analytics events: {e.Message}");
                
                // Re-queue failed events
                _eventQueue.AddRange(eventsToSend);
            }
        }
    }
}
