using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Functions;
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
        private FirebaseFunctions _functions;
        private FirebaseFirestore _firestore;

        public List<Friend> Friends => _friends;
        public List<FriendRequest> IncomingRequests => _incomingRequests;
        public List<FriendRequest> OutgoingRequests => _outgoingRequests;
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
                Debug.LogError($"Failed to load friends list: {e.Message}");
            }
        }

        /// <summary>
        /// Send a friend request
        /// </summary>
        public async Task<bool> SendFriendRequest(string toUserId)
        {
            if (_friends.Count >= maxFriends)
            {
                Debug.LogWarning("Max friends limit reached");
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
                Debug.LogError($"Failed to send friend request: {e.Message}");
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
                Debug.LogError($"Failed to accept friend request: {e.Message}");
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
                Debug.LogError($"Failed to decline friend request: {e.Message}");
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
                Debug.LogError($"Failed to remove friend: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send a gift to a friend
        /// </summary>
        public async Task<bool> SendGift(string toUserId, string giftType = "energy")
        {
            if (!CanSendGift)
            {
                Debug.LogWarning("Daily gift limit reached");
                return false;
            }

            try
            {
                var callable = _functions.GetHttpsCallable("sendGift");
                var data = new Dictionary<string, object>
                {
                    { "toUserId", toUserId },
                    { "giftType", giftType }
                };
                var result = await callable.CallAsync(data);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("success") && Convert.ToBoolean(response["success"]))
                {
                    _dailyGiftsSent++;
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to send gift: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Claim a received gift
        /// </summary>
        public async Task<bool> ClaimGift(string giftId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("claimGift");
                var data = new Dictionary<string, object> { { "giftId", giftId } };
                var result = await callable.CallAsync(data);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("success") && Convert.ToBoolean(response["success"]))
                {
                    _pendingGifts.RemoveAll(g => g.Id == giftId);
                    OnPendingGiftsUpdated?.Invoke(_pendingGifts);
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to claim gift: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Claim all pending gifts
        /// </summary>
        public async Task<int> ClaimAllGifts()
        {
            int claimed = 0;
            foreach (var gift in _pendingGifts.ToArray())
            {
                if (await ClaimGift(gift.Id))
                {
                    claimed++;
                }
            }
            return claimed;
        }

        /// <summary>
        /// Like an activity
        /// </summary>
        public async Task<bool> LikeActivity(string activityId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("likeActivity");
                var data = new Dictionary<string, object> { { "activityId", activityId } };
                await callable.CallAsync(data);
                
                var activity = _activityFeed.Find(a => a.Id == activityId);
                if (activity != null)
                {
                    activity.Likes++;
                    activity.HasLiked = true;
                    OnActivityFeedUpdated?.Invoke(_activityFeed);
                }
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to like activity: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Search for users to add as friends
        /// </summary>
        public async Task<List<Friend>> SearchUsers(string searchQuery)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("searchUsers");
                var data = new Dictionary<string, object> { { "query", searchQuery } };
                var result = await callable.CallAsync(data);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("users"))
                {
                    var usersJson = JsonConvert.SerializeObject(response["users"]);
                    return JsonConvert.DeserializeObject<List<Friend>>(usersJson);
                }
                
                return new List<Friend>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to search users: {e.Message}");
                return new List<Friend>();
            }
        }

        /// <summary>
        /// Get friend leaderboard
        /// </summary>
        public async Task<List<Friend>> GetFriendLeaderboard(string metric = "level")
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getFriendLeaderboard");
                var data = new Dictionary<string, object> { { "metric", metric } };
                var result = await callable.CallAsync(data);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("leaderboard"))
                {
                    var leaderboardJson = JsonConvert.SerializeObject(response["leaderboard"]);
                    return JsonConvert.DeserializeObject<List<Friend>>(leaderboardJson);
                }
                
                return new List<Friend>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get friend leaderboard: {e.Message}");
                return new List<Friend>();
            }
        }

        private async Task LoadPendingRequests()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getFriendRequests");
                var result = await callable.CallAsync();
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                
                if (response.ContainsKey("incoming"))
                {
                    var incomingJson = JsonConvert.SerializeObject(response["incoming"]);
                    _incomingRequests = JsonConvert.DeserializeObject<List<FriendRequest>>(incomingJson);
                }
                
                if (response.ContainsKey("outgoing"))
                {
                    var outgoingJson = JsonConvert.SerializeObject(response["outgoing"]);
                    _outgoingRequests = JsonConvert.DeserializeObject<List<FriendRequest>>(outgoingJson);
                }
                
                OnRequestsUpdated?.Invoke(_incomingRequests);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load friend requests: {e.Message}");
            }
        }

        private async Task LoadPendingGifts()
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
                Debug.LogError($"Failed to load pending gifts: {e.Message}");
            }
        }

        private async Task LoadActivityFeed()
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
                Debug.LogError($"Failed to load activity feed: {e.Message}");
            }
        }

        private void SubscribeToFriendRequests()
        {
            var userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            var query = _firestore.Collection("friend_requests")
                .WhereEqualTo("toUserId", userId)
                .WhereEqualTo("status", "pending");

            query.Listen(snapshot =>
            {
                foreach (var change in snapshot.GetChanges())
                {
                    if (change.ChangeType == DocumentChange.Type.Added)
                    {
                        var data = change.Document.ToDictionary();
                        var request = new FriendRequest
                        {
                            Id = change.Document.Id,
                            FromUserId = data.GetValueOrDefault("fromUserId", "").ToString(),
                            FromUserName = data.GetValueOrDefault("fromUserName", "Unknown").ToString(),
                            FromUserLevel = Convert.ToInt32(data.GetValueOrDefault("fromUserLevel", 1))
                        };

                        if (!_incomingRequests.Exists(r => r.Id == request.Id))
                        {
                            _incomingRequests.Add(request);
                            OnFriendRequestReceived?.Invoke(request);
                            OnRequestsUpdated?.Invoke(_incomingRequests);
                        }
                    }
                }
            });
        }

        private void SubscribeToGifts()
        {
            var userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            var query = _firestore.Collection("gifts")
                .WhereEqualTo("toUserId", userId)
                .WhereEqualTo("claimed", false);

            query.Listen(snapshot =>
            {
                foreach (var change in snapshot.GetChanges())
                {
                    if (change.ChangeType == DocumentChange.Type.Added)
                    {
                        var data = change.Document.ToDictionary();
                        var gift = new Gift
                        {
                            Id = change.Document.Id,
                            FromUserId = data.GetValueOrDefault("fromUserId", "").ToString(),
                            FromUserName = data.GetValueOrDefault("fromUserName", "Unknown").ToString(),
                            GiftType = data.GetValueOrDefault("giftType", "energy").ToString(),
                            Amount = Convert.ToInt32(data.GetValueOrDefault("amount", 1))
                        };

                        if (!_pendingGifts.Exists(g => g.Id == gift.Id))
                        {
                            _pendingGifts.Add(gift);
                            OnGiftReceived?.Invoke(gift);
                            OnPendingGiftsUpdated?.Invoke(_pendingGifts);
                        }
                    }
                }
            });
        }

        private void UpdateOnlineStatuses()
        {
            // Update friend online statuses based on last seen
            var now = DateTime.UtcNow;
            foreach (var friend in _friends)
            {
                var timeSinceSeen = now - friend.LastSeen;
                if (timeSinceSeen.TotalMinutes < 5)
                {
                    friend.Status = FriendStatus.Online;
                    friend.IsOnline = true;
                }
                else if (timeSinceSeen.TotalMinutes < 30)
                {
                    friend.Status = FriendStatus.Away;
                    friend.IsOnline = false;
                }
                else
                {
                    friend.Status = FriendStatus.Offline;
                    friend.IsOnline = false;
                }
            }
            OnFriendsListUpdated?.Invoke(_friends);
        }

        /// <summary>
        /// Get online friends count
        /// </summary>
        public int GetOnlineFriendsCount()
        {
            return _friends.FindAll(f => f.IsOnline).Count;
        }

        /// <summary>
        /// Check if a user is already a friend
        /// </summary>
        public bool IsFriend(string userId)
        {
            return _friends.Exists(f => f.UserId == userId);
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
