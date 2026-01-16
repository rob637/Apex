using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Functions;
using Newtonsoft.Json;

namespace ApexCitadels.WorldEvents
{
    /// <summary>
    /// Types of world events that can occur
    /// </summary>
    public enum WorldEventType
    {
        WorldBoss,
        TerritoryRush,
        ResourceSurge,
        AllianceWarWeekend,
        ConquestFrenzy,
        DoubleXp,
        MysteryBoxRain,
        DefendTheRealm,
        TreasureHunt,
        FactionWar
    }

    /// <summary>
    /// Represents an active world event
    /// </summary>
    [Serializable]
    public class WorldEvent
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public WorldEventType EventType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public EventRewards Rewards { get; set; }
        public Dictionary<string, object> Config { get; set; }
        public int ParticipantCount { get; set; }
        public bool IsActive => DateTime.UtcNow >= StartTime && DateTime.UtcNow <= EndTime;
        public TimeSpan TimeRemaining => EndTime - DateTime.UtcNow;
    }

    [Serializable]
    public class EventRewards
    {
        public int Gold { get; set; }
        public int Xp { get; set; }
        public int Gems { get; set; }
        public List<string> Items { get; set; }
    }

    [Serializable]
    public class EventParticipation
    {
        public string EventId { get; set; }
        public string UserId { get; set; }
        public int Contribution { get; set; }
        public int Rank { get; set; }
        public bool RewardsClaimed { get; set; }
    }

    /// <summary>
    /// Manager for world events - creates FOMO and drives engagement
    /// </summary>
    public class WorldEventManager : MonoBehaviour
    {
        public static WorldEventManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float refreshIntervalSeconds = 60f;
        [SerializeField] private bool showEventNotifications = true;

        // Events
        public event Action<List<WorldEvent>> OnEventsUpdated;
        public event Action<WorldEvent> OnEventStarted;
        public event Action<WorldEvent> OnEventEnded;
        public event Action<WorldEvent, EventParticipation> OnParticipationUpdated;

        // State
        private List<WorldEvent> _activeEvents = new List<WorldEvent>();
        private Dictionary<string, EventParticipation> _participations = new Dictionary<string, EventParticipation>();
        private FirebaseFunctions _functions;
        private FirebaseFirestore _firestore;
        private float _lastRefresh;

        public List<WorldEvent> ActiveEvents => _activeEvents;

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

#if FIREBASE_ENABLED
        private void Start()
        {
            _functions = FirebaseFunctions.DefaultInstance;
            _firestore = FirebaseFirestore.DefaultInstance;
            
            // Initial fetch
            RefreshEvents();
            
            // Subscribe to real-time updates
            SubscribeToEvents();
        }

        private void Update()
        {
            // Periodic refresh
            if (Time.time - _lastRefresh > refreshIntervalSeconds)
            {
                RefreshEvents();
            }

            // Check for event endings
            CheckEventTimers();
        }

        /// <summary>
        /// Fetch all active world events
        /// </summary>
        public async Task RefreshEvents()
        {
            _lastRefresh = Time.time;

            try
            {
                var callable = _functions.GetHttpsCallable("getActiveEvents");
                var result = await callable.CallAsync();
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("events"))
                {
                    var eventsJson = JsonConvert.SerializeObject(response["events"]);
                    _activeEvents = JsonConvert.DeserializeObject<List<WorldEvent>>(eventsJson);
                    OnEventsUpdated?.Invoke(_activeEvents);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to fetch world events: {e.Message}");
            }
        }

        /// <summary>
        /// Subscribe to real-time event updates
        /// </summary>
        private void SubscribeToEvents()
        {
            var query = _firestore.Collection("world_events")
                .WhereEqualTo("status", "active");

            query.Listen(snapshot =>
            {
                foreach (var change in snapshot.GetChanges())
                {
                    var eventData = change.Document.ConvertTo<Dictionary<string, object>>();
                    var worldEvent = ParseWorldEvent(change.Document.Id, eventData);

                    switch (change.ChangeType)
                    {
                        case DocumentChange.Type.Added:
                            if (!_activeEvents.Exists(e => e.Id == worldEvent.Id))
                            {
                                _activeEvents.Add(worldEvent);
                                OnEventStarted?.Invoke(worldEvent);
                                ShowEventNotification(worldEvent, true);
                            }
                            break;

                        case DocumentChange.Type.Modified:
                            var index = _activeEvents.FindIndex(e => e.Id == worldEvent.Id);
                            if (index >= 0)
                            {
                                _activeEvents[index] = worldEvent;
                            }
                            break;

                        case DocumentChange.Type.Removed:
                            _activeEvents.RemoveAll(e => e.Id == worldEvent.Id);
                            OnEventEnded?.Invoke(worldEvent);
                            break;
                    }
                }

                OnEventsUpdated?.Invoke(_activeEvents);
            });
        }

        /// <summary>
        /// Join a world event
        /// </summary>
        public async Task<bool> JoinEvent(string eventId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("joinEvent");
                var data = new Dictionary<string, object> { { "eventId", eventId } };
                var result = await callable.CallAsync(data);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("participation"))
                {
                    var participationJson = JsonConvert.SerializeObject(response["participation"]);
                    var participation = JsonConvert.DeserializeObject<EventParticipation>(participationJson);
                    _participations[eventId] = participation;
                    
                    var worldEvent = _activeEvents.Find(e => e.Id == eventId);
                    OnParticipationUpdated?.Invoke(worldEvent, participation);
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to join event: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Contribute to an event (e.g., damage to world boss)
        /// </summary>
        public async Task<bool> ContributeToEvent(string eventId, int amount, string contributionType)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("contributeToEvent");
                var data = new Dictionary<string, object>
                {
                    { "eventId", eventId },
                    { "amount", amount },
                    { "contributionType", contributionType }
                };
                var result = await callable.CallAsync(data);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("participation"))
                {
                    var participationJson = JsonConvert.SerializeObject(response["participation"]);
                    var participation = JsonConvert.DeserializeObject<EventParticipation>(participationJson);
                    _participations[eventId] = participation;
                    
                    var worldEvent = _activeEvents.Find(e => e.Id == eventId);
                    OnParticipationUpdated?.Invoke(worldEvent, participation);
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to contribute to event: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Claim rewards for a completed event
        /// </summary>
        public async Task<EventRewards> ClaimEventRewards(string eventId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("claimEventRewards");
                var data = new Dictionary<string, object> { { "eventId", eventId } };
                var result = await callable.CallAsync(data);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("rewards"))
                {
                    var rewardsJson = JsonConvert.SerializeObject(response["rewards"]);
                    return JsonConvert.DeserializeObject<EventRewards>(rewardsJson);
                }
                
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to claim rewards: {e.Message}");
                return null;
            }
        }
#else
        private void Start()
        {
            Debug.LogWarning("[WorldEventManager] Firebase SDK not installed. Running in stub mode.");
        }

        private void Update()
        {
            // Check for event endings only
            CheckEventTimers();
        }

        public Task RefreshEvents()
        {
            Debug.LogWarning("[WorldEventManager] Firebase SDK not installed. RefreshEvents is a stub.");
            return Task.CompletedTask;
        }

        public Task<bool> JoinEvent(string eventId)
        {
            Debug.LogWarning("[WorldEventManager] Firebase SDK not installed. JoinEvent is a stub.");
            return Task.FromResult(false);
        }

        public Task<bool> ContributeToEvent(string eventId, int amount, string contributionType)
        {
            Debug.LogWarning("[WorldEventManager] Firebase SDK not installed. ContributeToEvent is a stub.");
            return Task.FromResult(false);
        }

        public Task<EventRewards> ClaimEventRewards(string eventId)
        {
            Debug.LogWarning("[WorldEventManager] Firebase SDK not installed. ClaimEventRewards is a stub.");
            return Task.FromResult<EventRewards>(null);
        }
#endif

        /// <summary>
        /// Get current event multipliers (for UI display)
        /// </summary>
        public Dictionary<string, float> GetActiveMultipliers()
        {
            var multipliers = new Dictionary<string, float>
            {
                { "xp", 1f },
                { "gold", 1f },
                { "resources", 1f }
            };

            foreach (var evt in _activeEvents)
            {
                switch (evt.EventType)
                {
                    case WorldEventType.DoubleXp:
                        multipliers["xp"] = Math.Max(multipliers["xp"], 2f);
                        break;
                    case WorldEventType.ResourceSurge:
                        multipliers["resources"] = Math.Max(multipliers["resources"], 2f);
                        multipliers["gold"] = Math.Max(multipliers["gold"], 1.5f);
                        break;
                }
            }

            return multipliers;
        }

        /// <summary>
        /// Check if player is participating in an event
        /// </summary>
        public bool IsParticipating(string eventId)
        {
            return _participations.ContainsKey(eventId);
        }

        /// <summary>
        /// Get participation data for an event
        /// </summary>
        public EventParticipation GetParticipation(string eventId)
        {
            return _participations.TryGetValue(eventId, out var participation) ? participation : null;
        }

        private void CheckEventTimers()
        {
            var now = DateTime.UtcNow;
            foreach (var evt in _activeEvents.ToArray())
            {
                if (now >= evt.EndTime)
                {
                    _activeEvents.Remove(evt);
                    OnEventEnded?.Invoke(evt);
                    ShowEventNotification(evt, false);
                }
            }
        }

        private void ShowEventNotification(WorldEvent evt, bool isStarting)
        {
            if (!showEventNotifications) return;

            var title = isStarting ? "🎉 Event Started!" : "⏰ Event Ended";
            var message = isStarting 
                ? $"{evt.Name} has begun! Join now to earn rewards!"
                : $"{evt.Name} has ended. Claim your rewards!";

            // Trigger UI notification
            NotificationManager.Instance?.ShowLocalNotification(title, message, evt.Id);
        }

        private WorldEvent ParseWorldEvent(string id, Dictionary<string, object> data)
        {
            var evt = new WorldEvent
            {
                Id = id,
                Name = data.GetValueOrDefault("name", "Unknown Event").ToString(),
                Description = data.GetValueOrDefault("description", "").ToString(),
                ParticipantCount = Convert.ToInt32(data.GetValueOrDefault("participantCount", 0))
            };

            if (Enum.TryParse<WorldEventType>(data.GetValueOrDefault("eventType", "WorldBoss").ToString(), true, out var eventType))
            {
                evt.EventType = eventType;
            }

            if (data.TryGetValue("startTime", out var startTime) && startTime is Timestamp startTs)
            {
                evt.StartTime = startTs.ToDateTime();
            }

            if (data.TryGetValue("endTime", out var endTime) && endTime is Timestamp endTs)
            {
                evt.EndTime = endTs.ToDateTime();
            }

            if (data.TryGetValue("rewards", out var rewards) && rewards is Dictionary<string, object> rewardsDict)
            {
                evt.Rewards = new EventRewards
                {
                    Gold = Convert.ToInt32(rewardsDict.GetValueOrDefault("gold", 0)),
                    Xp = Convert.ToInt32(rewardsDict.GetValueOrDefault("xp", 0)),
                    Gems = Convert.ToInt32(rewardsDict.GetValueOrDefault("gems", 0))
                };
            }

            return evt;
        }
    }
}
