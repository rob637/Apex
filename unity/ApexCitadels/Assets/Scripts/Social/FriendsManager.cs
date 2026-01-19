using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Core;
#if FIREBASE_ENABLED
using Firebase.Firestore;
using Firebase.Functions;
#endif
using Newtonsoft.Json;

namespace ApexCitadels.Social
{
    /// <summary>
    /// Represents a friend relationship
    /// </summary>
    [Serializable]
    public class Friend
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string AvatarUrl { get; set; }
        public int Level { get; set; }
        public string AllianceName { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsOnline { get; set; }
        public FriendStatus Status { get; set; }
    }

    public enum FriendStatus
    {
        Online,
        Away,
        Offline
    }

    /// <summary>
    /// Friend request data
    /// </summary>
    [Serializable]
    public class FriendRequest
    {
        public string Id { get; set; }
        public string FromUserId { get; set; }
        public string FromUserName { get; set; }
        public string FromUserAvatar { get; set; }
        public int FromUserLevel { get; set; }
        public DateTime CreatedAt { get; set; }

        // Compatibility property
        public string SenderName => FromUserName;
    }

    /// <summary>
    /// Gift data
    /// </summary>
    [Serializable]
    public class Gift
    {
        public string Id { get; set; }
        public string FromUserId { get; set; }
        public string FromUserName { get; set; }
        public string GiftType { get; set; }
        public int Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Activity feed item
    /// </summary>
    [Serializable]
    public class ActivityItem
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ActivityType { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public int Likes { get; set; }
        public bool HasLiked { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Manages friends and social features
    /// </summary>
    public class FriendsManager : MonoBehaviour
    {
        public static FriendsManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxFriends = 100;
        [SerializeField] private int dailyGiftLimit = 5;
        [SerializeField] private float onlineCheckIntervalSeconds = 60f;

        // Events
        public event Action<List<Friend>> OnFriendsListUpdated;
        public event Action<List<FriendRequest>> OnRequestsUpdated;
        public event Action<FriendRequest> OnFriendRequestReceived;
        public event Action<Friend> OnFriendAdded;
        public event Action<string> OnFriendRemoved;
        public event Action<Gift> OnGiftReceived;
        public event Action<List<Gift>> OnPendingGiftsUpdated;
        public event Action<List<ActivityItem>> OnActivityFeedUpdated;

        // State
        private List<Friend> _friends = new List<Friend>();
        private List<FriendRequest> _incomingRequests = new List<FriendRequest>();
        private List<FriendRequest> _outgoingRequests = new List<FriendRequest>();
        private List<Gift> _pendingGifts = new List<Gift>();
        private List<ActivityItem> _activityFeed = new List<ActivityItem>();
        private int _dailyGiftsSent = 0;
#if FIREBASE_ENABLED
        private FirebaseFunctions _functions;
        private FirebaseFirestore _firestore;
#endif

        public List<Friend> Friends => _friends;
        public List<FriendRequest> IncomingRequests => _incomingRequests;
        public List<FriendRequest> OutgoingRequests => _outgoingRequests;
        public List<FriendRequest> PendingRequests => _incomingRequests;
        public List<Gift> PendingGifts => _pendingGifts;
        public List<ActivityItem> ActivityFeed => _activityFeed;
        public int DailyGiftsRemaining => dailyGiftLimit - _dailyGiftsSent;
        public bool CanSendGift => _dailyGiftsSent < dailyGiftLimit;

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
            
            // Load initial data
            LoadFriendsList();
            LoadPendingRequests();
            LoadPendingGifts();
            LoadActivityFeed();

            // Subscribe to real-time updates
            SubscribeToFriendRequests();
            SubscribeToGifts();

            // Periodic online status check
            InvokeRepeating(nameof(UpdateOnlineStatuses), onlineCheckIntervalSeconds, onlineCheckIntervalSeconds);
        }

        /// <summary>
        /// Load the friends list
        /// </summary>
        public async Task LoadFriendsList()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getFriendsList");
                var result = await callable.CallAsync();
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("friends"))
                {
                    var friendsJson = JsonConvert.SerializeObject(response["friends"]);
                    _friends = JsonConvert.DeserializeObject<List<Friend>>(friendsJson);
                    OnFriendsListUpdated?.Invoke(_friends);
                }
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"Failed to load friends list: {e.Message}", ApexLogger.LogCategory.Network);
            }
        }

        /// <summary>
        /// Send a friend request
        /// </summary>
        public async Task<bool> SendFriendRequest(string toUserId)
        {
            if (_friends.Count >= maxFriends)
            {
                ApexLogger.LogWarning("Max friends limit reached", ApexLogger.LogCategory.Network);
                return false;
            }

            try
            {
                var callable = _functions.GetHttpsCallable("sendFriendRequest");
                var data = new Dictionary<string, object> { { "toUserId", toUserId } };
                var result = await callable.CallAsync(data);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                return response.ContainsKey("success") && Convert.ToBoolean(response["success"]);
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"Failed to send friend request: {e.Message}", ApexLogger.LogCategory.Network);
                return false;
            }
        }

        /// <summary>
        /// Accept a friend request
        /// </summary>
        public async Task<bool> AcceptFriendRequest(string requestId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("respondToFriendRequest");
                var data = new Dictionary<string, object>
                {
                    { "requestId", requestId },
                    { "accept", true }
                };
                var result = await callable.CallAsync(data);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("friend"))
                {
                    var friendJson = JsonConvert.SerializeObject(response["friend"]);
                    var friend = JsonConvert.DeserializeObject<Friend>(friendJson);
                    _friends.Add(friend);
                    _incomingRequests.RemoveAll(r => r.Id == requestId);
                    
                    OnFriendAdded?.Invoke(friend);
                    OnFriendsListUpdated?.Invoke(_friends);
                    OnRequestsUpdated?.Invoke(_incomingRequests);
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"Failed to accept friend request: {e.Message}", ApexLogger.LogCategory.Network);
                return false;
            }
        }

        /// <summary>
        /// Decline a friend request
        /// </summary>
        public async Task<bool> DeclineFriendRequest(string requestId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("respondToFriendRequest");
                var data = new Dictionary<string, object>
                {
                    { "requestId", requestId },
                    { "accept", false }
                };
                await callable.CallAsync(data);
                
                _incomingRequests.RemoveAll(r => r.Id == requestId);
                OnRequestsUpdated?.Invoke(_incomingRequests);
                return true;
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"Failed to decline friend request: {e.Message}", ApexLogger.LogCategory.Network);
                return false;
            }
        }

        /// <summary>
        /// Remove a friend
        /// </summary>
        public async Task<bool> RemoveFriend(string friendUserId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("removeFriend");
                var data = new Dictionary<string, object> { { "friendUserId", friendUserId } };
                await callable.CallAsync(data);
                
                _friends.RemoveAll(f => f.UserId == friendUserId);
                OnFriendRemoved?.Invoke(friendUserId);
                OnFriendsListUpdated?.Invoke(_friends);
                return true;
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"Failed to remove friend: {e.Message}", ApexLogger.LogCategory.Network);
                return false;
            }
        }

        /// <summary>
        /// Send a gift to a friend
        /// </summary>
        public Task<bool> SendGiftAsync(string toUserId, string giftType = "energy")
        {
            if (!CanSendGift)
            {
                ApexLogger.LogWarning("Daily gift limit reached", ApexLogger.LogCategory.Network);
                return Task.FromResult(false);
            }
            ApexLogger.LogWarning("Firebase SDK not installed. SendGift is a stub.", ApexLogger.LogCategory.Network);
            return Task.FromResult(false);
        }

        /// <summary>
        /// Load pending friend requests
        /// </summary>
        private async void LoadPendingRequests()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getPendingRequests");
                var result = await callable.CallAsync();
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                if (response.ContainsKey("requests"))
                {
                    var requestsJson = JsonConvert.SerializeObject(response["requests"]);
                    _incomingRequests = JsonConvert.DeserializeObject<List<FriendRequest>>(requestsJson);
                    OnRequestsUpdated?.Invoke(_incomingRequests);
                }
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"Failed to load pending requests: {e.Message}", ApexLogger.LogCategory.Network);
            }
        }

        /// <summary>
        /// Load pending gifts
        /// </summary>
        private async void LoadPendingGifts()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getPendingGifts");
                var result = await callable.CallAsync();
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                if (response.ContainsKey("gifts"))
                {
                    var giftsJson = JsonConvert.SerializeObject(response["gifts"]);
                    _pendingGifts = JsonConvert.DeserializeObject<List<Gift>>(giftsJson);
                    OnPendingGiftsUpdated?.Invoke(_pendingGifts);
                }
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"Failed to load pending gifts: {e.Message}", ApexLogger.LogCategory.Network);
            }
        }

        /// <summary>
        /// Load activity feed
        /// </summary>
        private async void LoadActivityFeed()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getActivityFeed");
                var result = await callable.CallAsync();
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                if (response.ContainsKey("activities"))
                {
                    var activitiesJson = JsonConvert.SerializeObject(response["activities"]);
                    _activityFeed = JsonConvert.DeserializeObject<List<ActivityItem>>(activitiesJson);
                    OnActivityFeedUpdated?.Invoke(_activityFeed);
                }
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"Failed to load activity feed: {e.Message}", ApexLogger.LogCategory.Network);
            }
        }

        /// <summary>
        /// Subscribe to friend requests
        /// </summary>
        private void SubscribeToFriendRequests()
        {
            // Real-time listener for friend requests would go here
            ApexLogger.LogVerbose("Subscribed to friend requests", ApexLogger.LogCategory.Network);
        }

        /// <summary>
        /// Subscribe to gifts
        /// </summary>
        private void SubscribeToGifts()
        {
            // Real-time listener for gifts would go here
            ApexLogger.LogVerbose("Subscribed to gifts", ApexLogger.LogCategory.Network);
        }

        /// <summary>
        /// Update online statuses
        /// </summary>
        private async void UpdateOnlineStatuses()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getFriendsOnlineStatus");
                var result = await callable.CallAsync();
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                if (response.ContainsKey("statuses"))
                {
                    var statusesJson = JsonConvert.SerializeObject(response["statuses"]);
                    var statuses = JsonConvert.DeserializeObject<Dictionary<string, bool>>(statusesJson);
                    foreach (var friend in _friends)
                    {
                        if (statuses.ContainsKey(friend.UserId))
                        {
                            friend.IsOnline = statuses[friend.UserId];
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"Failed to update online statuses: {e.Message}", ApexLogger.LogCategory.Network);
            }
        }

        /// <summary>
        /// Claim all pending gifts
        /// </summary>
        public async Task<bool> ClaimAllGifts()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("claimAllGifts");
                var result = await callable.CallAsync();
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                if (response.ContainsKey("success") && Convert.ToBoolean(response["success"]))
                {
                    _pendingGifts.Clear();
                    OnPendingGiftsUpdated?.Invoke(_pendingGifts);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                ApexLogger.LogError($"Failed to claim all gifts: {e.Message}", ApexLogger.LogCategory.Network);
                return false;
            }
        }
#else
        private void Start()
        {
            ApexLogger.LogWarning("Firebase SDK not installed. Running in stub mode.", ApexLogger.LogCategory.Network);
        }

        public Task LoadFriendsList()
        {
            ApexLogger.LogWarning("Firebase SDK not installed. LoadFriendsList is a stub.", ApexLogger.LogCategory.Network);
            return Task.CompletedTask;
        }

        public Task<bool> SendFriendRequest(string toUserId)
        {
            ApexLogger.LogWarning("Firebase SDK not installed. SendFriendRequest is a stub.", ApexLogger.LogCategory.Network);
            return Task.FromResult(false);
        }

        public Task<bool> AcceptFriendRequest(string requestId)
        {
            ApexLogger.LogWarning("Firebase SDK not installed. AcceptFriendRequest is a stub.", ApexLogger.LogCategory.Network);
            return Task.FromResult(false);
        }

        public Task<bool> DeclineFriendRequest(string requestId)
        {
            ApexLogger.LogWarning("Firebase SDK not installed. DeclineFriendRequest is a stub.", ApexLogger.LogCategory.Network);
            return Task.FromResult(false);
        }

        public Task<bool> RemoveFriend(string friendUserId)
        {
            ApexLogger.LogWarning("Firebase SDK not installed. RemoveFriend is a stub.", ApexLogger.LogCategory.Network);
            return Task.FromResult(false);
        }

        public Task<bool> SendGift(string toUserId, string giftType = "energy")
        {
            ApexLogger.LogWarning("Firebase SDK not installed. SendGift is a stub.", ApexLogger.LogCategory.Network);
            return Task.FromResult(false);
        }

        public Task<bool> ClaimGift(string giftId)
        {
            ApexLogger.LogWarning("Firebase SDK not installed. ClaimGift is a stub.", ApexLogger.LogCategory.Network);
            return Task.FromResult(false);
        }

        public Task<bool> ClaimAllGifts()
        {
            ApexLogger.LogWarning("Firebase SDK not installed. ClaimAllGifts is a stub.", ApexLogger.LogCategory.Network);
            return Task.FromResult(false);
        }

        public Task LoadPendingRequests()
        {
            ApexLogger.LogWarning("Firebase SDK not installed. LoadPendingRequests is a stub.", ApexLogger.LogCategory.Network);
            return Task.CompletedTask;
        }

        public Task LoadPendingGifts()
        {
            ApexLogger.LogWarning("Firebase SDK not installed. LoadPendingGifts is a stub.", ApexLogger.LogCategory.Network);
            return Task.CompletedTask;
        }

        public Task LoadActivityFeed()
        {
            ApexLogger.LogWarning("Firebase SDK not installed. LoadActivityFeed is a stub.", ApexLogger.LogCategory.Network);
            return Task.CompletedTask;
        }

        public Task<List<Friend>> SearchPlayers(string query)
        {
            ApexLogger.LogWarning("Firebase SDK not installed. SearchPlayers is a stub.", ApexLogger.LogCategory.Network);
            return Task.FromResult(new List<Friend>());
        }

        public Task<bool> BlockUser(string userId)
        {
            ApexLogger.LogWarning("Firebase SDK not installed. BlockUser is a stub.", ApexLogger.LogCategory.Network);
            return Task.FromResult(false);
        }

        public Task<bool> UnblockUser(string userId)
        {
            ApexLogger.LogWarning("Firebase SDK not installed. UnblockUser is a stub.", ApexLogger.LogCategory.Network);
            return Task.FromResult(false);
        }

        public Task<bool> ReportUser(string userId, string reason)
        {
            ApexLogger.LogWarning("Firebase SDK not installed. ReportUser is a stub.", ApexLogger.LogCategory.Network);
            return Task.FromResult(false);
        }
#endif

        /// <summary>
        /// Check if a user is a friend
        /// </summary>
        public bool IsFriend(string userId)
        {
            return _friends.Exists(f => f.UserId == userId);
        }

        /// <summary>
        /// Get a friend by user ID
        /// </summary>
        public Friend GetFriend(string userId)
        {
            return _friends.Find(f => f.UserId == userId);
        }

        /// <summary>
        /// Check if a friend request is pending
        /// </summary>
        public bool HasPendingRequest(string userId)
        {
            return _outgoingRequests.Exists(r => r.FromUserId == userId) ||
                   _incomingRequests.Exists(r => r.FromUserId == userId);
        }
    }
}
