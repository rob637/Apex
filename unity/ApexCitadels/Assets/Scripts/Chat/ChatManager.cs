using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
#if FIREBASE_ENABLED
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.Functions;
#endif
using Newtonsoft.Json;

namespace ApexCitadels.Chat
{
    /// <summary>
    /// Chat channel types
    /// </summary>
    public enum ChatChannelType
    {
        Global,
        Alliance,
        DirectMessage,
        Territory,
        Event
    }

    /// <summary>
    /// Chat message data
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        public string Id;
        public string ChannelId;
        public string SenderId;
        public string SenderName;
        public string SenderAvatarUrl;
        public string Content;
        public ChatMessageType Type;
        public Dictionary<string, object> Metadata;
        public DateTime Timestamp;
        public bool IsDeleted;
        public List<string> Reactions;

#if FIREBASE_ENABLED
        public bool IsMine => SenderId == Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
#else
        public bool IsMine => false;
#endif
    }

    /// <summary>
    /// Message types
    /// </summary>
    public enum ChatMessageType
    {
        Text,
        System,
        Emote,
        Achievement,
        BattleResult,
        TerritoryCapture
    }

    /// <summary>
    /// Chat channel data
    /// </summary>
    [Serializable]
    public class ChatChannel
    {
        public string Id;
        public string Name;
        public ChatChannelType Type;
        public List<string> MemberIds;
        public string LastMessageContent;
        public DateTime LastMessageTime;
        public int UnreadCount;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Manages all chat functionality
    /// </summary>
    public class ChatManager : MonoBehaviour
    {
        public static ChatManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int messagesPerPage = 50;
        [SerializeField] private float typingIndicatorTimeoutSeconds = 5f;

        // Events
        public event Action<List<ChatChannel>> OnChannelsUpdated;
        public event Action<ChatMessage> OnNewMessage;
        public event Action<string, List<ChatMessage>> OnMessagesLoaded;
        public event Action<string, List<string>> OnTypingUsersUpdated;
        public event Action<ChatChannel> OnChannelCreated;
        public event Action<string> OnChannelDeleted;

        // State
#if FIREBASE_ENABLED
        private FirebaseFirestore _db;
        private FirebaseFunctions _functions;
#endif
        private string _userId;
        
        private List<ChatChannel> _channels = new List<ChatChannel>();
        private Dictionary<string, List<ChatMessage>> _messageCache = new Dictionary<string, List<ChatMessage>>();
#if FIREBASE_ENABLED
        private Dictionary<string, ListenerRegistration> _messageListeners = new Dictionary<string, ListenerRegistration>();
        private ListenerRegistration _channelListener;
#endif
        
        private string _currentChannelId;
        private Dictionary<string, HashSet<string>> _typingUsers = new Dictionary<string, HashSet<string>>();
        private Dictionary<string, DateTime> _typingTimestamps = new Dictionary<string, DateTime>();

        public List<ChatChannel> Channels => _channels;
        public string CurrentChannelId => _currentChannelId;

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
            _db = FirebaseFirestore.DefaultInstance;
            _functions = FirebaseFunctions.DefaultInstance;
            _userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;

            if (!string.IsNullOrEmpty(_userId))
            {
                SubscribeToChannels();
            }

            // Typing indicator cleanup
            InvokeRepeating(nameof(CleanupTypingIndicators), 1f, 1f);
        }

        private void OnDestroy()
        {
            _channelListener?.Stop();
            foreach (var listener in _messageListeners.Values)
            {
                listener?.Stop();
            }
        }
#else
        private void Start()
        {
            Debug.LogWarning("[ChatManager] Firebase SDK not installed. Running in stub mode.");
            InvokeRepeating(nameof(CleanupTypingIndicators), 1f, 1f);
        }

        private void OnDestroy()
        {
            // No listeners to clean up in stub mode
        }
#endif

        /// <summary>
        /// Subscribe to user's chat channels
        /// </summary>
#if FIREBASE_ENABLED
        private void SubscribeToChannels()
        {
            _channelListener = _db.Collection("chat_members")
                .WhereEqualTo("userId", _userId)
                .Listen(snapshot =>
                {
                    LoadChannelDetails(snapshot.Documents.Select(d => d.GetValue<string>("channelId")).ToList());
                });
        }

        private async void LoadChannelDetails(List<string> channelIds)
        {
            _channels.Clear();

            foreach (var channelId in channelIds)
            {
                try
                {
                    var doc = await _db.Collection("chat_channels").Document(channelId).GetSnapshotAsync();
                    if (doc.Exists)
                    {
                        var channel = ParseChannel(doc);
                        
                        // Get unread count
                        var memberDoc = await _db.Collection("chat_members")
                            .WhereEqualTo("channelId", channelId)
                            .WhereEqualTo("userId", _userId)
                            .Limit(1)
                            .GetSnapshotAsync();

                        if (memberDoc.Documents.Any())
                        {
                            var lastRead = memberDoc.Documents.First().GetValue<Timestamp>("lastReadAt").ToDateTime();
                            
                            var unreadQuery = await _db.Collection("chat_messages")
                                .WhereEqualTo("channelId", channelId)
                                .WhereGreaterThan("createdAt", Timestamp.FromDateTime(lastRead))
                                .GetSnapshotAsync();
                            
                            channel.UnreadCount = unreadQuery.Count;
                        }

                        _channels.Add(channel);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load channel {channelId}: {e.Message}");
                }
            }

            // Sort by last message time
            _channels = _channels.OrderByDescending(c => c.LastMessageTime).ToList();
            
            OnChannelsUpdated?.Invoke(_channels);
        }
#else
        private void SubscribeToChannels()
        {
            Debug.LogWarning("[ChatManager] SubscribeToChannels called but Firebase SDK not installed.");
            OnChannelsUpdated?.Invoke(_channels);
        }

        private void LoadChannelDetails(List<string> channelIds)
        {
            Debug.LogWarning("[ChatManager] LoadChannelDetails called but Firebase SDK not installed.");
            OnChannelsUpdated?.Invoke(_channels);
        }
#endif

        /// <summary>
        /// Get or create global chat channel
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<ChatChannel> GetGlobalChannel()
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getOrCreateGlobalChannel");
                var result = await callable.CallAsync(null);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                return JsonConvert.DeserializeObject<ChatChannel>(JsonConvert.SerializeObject(response["channel"]));
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get global channel: {e.Message}");
                return null;
            }
        }
#else
        public Task<ChatChannel> GetGlobalChannel()
        {
            Debug.LogWarning("[ChatManager] GetGlobalChannel called but Firebase SDK not installed.");
            return Task.FromResult<ChatChannel>(null);
        }
#endif

        /// <summary>
        /// Get or create alliance chat channel
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<ChatChannel> GetAllianceChannel(string allianceId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("getOrCreateAllianceChannel");
                var data = new Dictionary<string, object> { { "allianceId", allianceId } };
                var result = await callable.CallAsync(data);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                return JsonConvert.DeserializeObject<ChatChannel>(JsonConvert.SerializeObject(response["channel"]));
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get alliance channel: {e.Message}");
                return null;
            }
        }
#else
        public Task<ChatChannel> GetAllianceChannel(string allianceId)
        {
            Debug.LogWarning("[ChatManager] GetAllianceChannel called but Firebase SDK not installed.");
            return Task.FromResult<ChatChannel>(null);
        }
#endif

        /// <summary>
        /// Create or get direct message channel with another user
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<ChatChannel> CreateDirectMessage(string otherUserId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("createDirectMessage");
                var data = new Dictionary<string, object> { { "recipientId", otherUserId } };
                var result = await callable.CallAsync(data);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                var channel = JsonConvert.DeserializeObject<ChatChannel>(JsonConvert.SerializeObject(response["channel"]));
                
                OnChannelCreated?.Invoke(channel);
                return channel;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create DM: {e.Message}");
                return null;
            }
        }
#else
        public Task<ChatChannel> CreateDirectMessage(string otherUserId)
        {
            Debug.LogWarning("[ChatManager] CreateDirectMessage called but Firebase SDK not installed.");
            return Task.FromResult<ChatChannel>(null);
        }
#endif

        /// <summary>
        /// Join a chat channel
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<bool> JoinChannel(string channelId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("joinChatChannel");
                var data = new Dictionary<string, object> { { "channelId", channelId } };
                await callable.CallAsync(data);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to join channel: {e.Message}");
                return false;
            }
        }
#else
        public Task<bool> JoinChannel(string channelId)
        {
            Debug.LogWarning("[ChatManager] JoinChannel called but Firebase SDK not installed.");
            return Task.FromResult(false);
        }
#endif

        /// <summary>
        /// Leave a chat channel
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<bool> LeaveChannel(string channelId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("leaveChatChannel");
                var data = new Dictionary<string, object> { { "channelId", channelId } };
                await callable.CallAsync(data);
                
                // Cleanup
                if (_messageListeners.TryGetValue(channelId, out var listener))
                {
                    listener?.Stop();
                    _messageListeners.Remove(channelId);
                }
                _messageCache.Remove(channelId);
                
                OnChannelDeleted?.Invoke(channelId);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to leave channel: {e.Message}");
                return false;
            }
        }
#else
        public Task<bool> LeaveChannel(string channelId)
        {
            Debug.LogWarning("[ChatManager] LeaveChannel called but Firebase SDK not installed.");
            _messageCache.Remove(channelId);
            OnChannelDeleted?.Invoke(channelId);
            return Task.FromResult(false);
        }
#endif

        /// <summary>
        /// Open a channel and start listening to messages
        /// </summary>
#if FIREBASE_ENABLED
        public void OpenChannel(string channelId)
        {
            _currentChannelId = channelId;
            
            // Subscribe to new messages if not already
            if (!_messageListeners.ContainsKey(channelId))
            {
                var listener = _db.Collection("chat_messages")
                    .WhereEqualTo("channelId", channelId)
                    .OrderByDescending("createdAt")
                    .Limit(messagesPerPage)
                    .Listen(snapshot =>
                    {
                        foreach (var change in snapshot.GetChanges())
                        {
                            if (change.ChangeType == DocumentChange.Type.Added)
                            {
                                var message = ParseMessage(change.Document);
                                
                                // Add to cache
                                if (!_messageCache.ContainsKey(channelId))
                                {
                                    _messageCache[channelId] = new List<ChatMessage>();
                                }
                                
                                if (!_messageCache[channelId].Any(m => m.Id == message.Id))
                                {
                                    _messageCache[channelId].Insert(0, message);
                                    OnNewMessage?.Invoke(message);
                                }
                            }
                        }
                    });
                
                _messageListeners[channelId] = listener;
            }

            // Load initial messages
            LoadMessages(channelId);
            
            // Mark as read
            MarkChannelAsRead(channelId);
        }
#else
        public void OpenChannel(string channelId)
        {
            Debug.LogWarning("[ChatManager] OpenChannel called but Firebase SDK not installed.");
            _currentChannelId = channelId;
            OnMessagesLoaded?.Invoke(channelId, new List<ChatMessage>());
        }
#endif

        /// <summary>
        /// Load messages for a channel
        /// </summary>
#if FIREBASE_ENABLED
        public async void LoadMessages(string channelId, DocumentSnapshot startAfter = null)
        {
            try
            {
                Query query = _db.Collection("chat_messages")
                    .WhereEqualTo("channelId", channelId)
                    .WhereEqualTo("isDeleted", false)
                    .OrderByDescending("createdAt")
                    .Limit(messagesPerPage);

                if (startAfter != null)
                {
                    query = query.StartAfter(startAfter);
                }

                var snapshot = await query.GetSnapshotAsync();
                var messages = snapshot.Documents.Select(ParseMessage).ToList();

                if (!_messageCache.ContainsKey(channelId))
                {
                    _messageCache[channelId] = new List<ChatMessage>();
                }

                // Merge with cache
                foreach (var msg in messages)
                {
                    if (!_messageCache[channelId].Any(m => m.Id == msg.Id))
                    {
                        _messageCache[channelId].Add(msg);
                    }
                }

                // Sort by timestamp
                _messageCache[channelId] = _messageCache[channelId]
                    .OrderByDescending(m => m.Timestamp)
                    .ToList();

                OnMessagesLoaded?.Invoke(channelId, _messageCache[channelId]);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load messages: {e.Message}");
            }
        }
#else
        public void LoadMessages(string channelId, object startAfter = null)
        {
            Debug.LogWarning("[ChatManager] LoadMessages called but Firebase SDK not installed.");
            OnMessagesLoaded?.Invoke(channelId, new List<ChatMessage>());
        }
#endif

        /// <summary>
        /// Send a text message
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<bool> SendMessage(string channelId, string content)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("sendChatMessage");
                var data = new Dictionary<string, object>
                {
                    { "channelId", channelId },
                    { "content", content },
                    { "type", "text" }
                };
                await callable.CallAsync(data);
                
                // Clear typing indicator
                StopTyping(channelId);
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to send message: {e.Message}");
                return false;
            }
        }
#else
        public Task<bool> SendMessage(string channelId, string content)
        {
            Debug.LogWarning("[ChatManager] SendMessage called but Firebase SDK not installed.");
            return Task.FromResult(false);
        }
#endif

        /// <summary>
        /// Send an emote message
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<bool> SendEmote(string channelId, string emoteId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("sendChatMessage");
                var data = new Dictionary<string, object>
                {
                    { "channelId", channelId },
                    { "content", emoteId },
                    { "type", "emote" }
                };
                await callable.CallAsync(data);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to send emote: {e.Message}");
                return false;
            }
        }
#else
        public Task<bool> SendEmote(string channelId, string emoteId)
        {
            Debug.LogWarning("[ChatManager] SendEmote called but Firebase SDK not installed.");
            return Task.FromResult(false);
        }
#endif

        /// <summary>
        /// Send a system message (for game events)
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<bool> SendSystemMessage(string channelId, string content, Dictionary<string, object> metadata = null)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("sendSystemMessage");
                var data = new Dictionary<string, object>
                {
                    { "channelId", channelId },
                    { "content", content },
                    { "metadata", metadata ?? new Dictionary<string, object>() }
                };
                await callable.CallAsync(data);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to send system message: {e.Message}");
                return false;
            }
        }
#else
        public Task<bool> SendSystemMessage(string channelId, string content, Dictionary<string, object> metadata = null)
        {
            Debug.LogWarning("[ChatManager] SendSystemMessage called but Firebase SDK not installed.");
            return Task.FromResult(false);
        }
#endif

        /// <summary>
        /// Delete a message
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<bool> DeleteMessage(string messageId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("deleteChatMessage");
                var data = new Dictionary<string, object> { { "messageId", messageId } };
                await callable.CallAsync(data);
                
                // Remove from cache
                foreach (var cache in _messageCache.Values)
                {
                    cache.RemoveAll(m => m.Id == messageId);
                }
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete message: {e.Message}");
                return false;
            }
        }
#else
        public Task<bool> DeleteMessage(string messageId)
        {
            Debug.LogWarning("[ChatManager] DeleteMessage called but Firebase SDK not installed.");
            return Task.FromResult(false);
        }
#endif

        /// <summary>
        /// Report a message for moderation
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<bool> ReportMessage(string messageId, string reason)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("reportChatMessage");
                var data = new Dictionary<string, object>
                {
                    { "messageId", messageId },
                    { "reason", reason }
                };
                await callable.CallAsync(data);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to report message: {e.Message}");
                return false;
            }
        }
#else
        public Task<bool> ReportMessage(string messageId, string reason)
        {
            Debug.LogWarning("[ChatManager] ReportMessage called but Firebase SDK not installed.");
            return Task.FromResult(false);
        }
#endif

        /// <summary>
        /// Add a reaction to a message
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<bool> AddReaction(string messageId, string reaction)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("addMessageReaction");
                var data = new Dictionary<string, object>
                {
                    { "messageId", messageId },
                    { "reaction", reaction }
                };
                await callable.CallAsync(data);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to add reaction: {e.Message}");
                return false;
            }
        }
#else
        public Task<bool> AddReaction(string messageId, string reaction)
        {
            Debug.LogWarning("[ChatManager] AddReaction called but Firebase SDK not installed.");
            return Task.FromResult(false);
        }
#endif

        /// <summary>
        /// Indicate user is typing
        /// </summary>
#if FIREBASE_ENABLED
        public async void StartTyping(string channelId)
        {
            try
            {
                await _db.Collection("chat_typing")
                    .Document($"{channelId}_{_userId}")
                    .SetAsync(new Dictionary<string, object>
                    {
                        { "channelId", channelId },
                        { "userId", _userId },
                        { "timestamp", FieldValue.ServerTimestamp }
                    });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to set typing indicator: {e.Message}");
            }
        }
#else
        public void StartTyping(string channelId)
        {
            // No-op in stub mode
        }
#endif

        /// <summary>
        /// Clear typing indicator
        /// </summary>
#if FIREBASE_ENABLED
        public async void StopTyping(string channelId)
        {
            try
            {
                await _db.Collection("chat_typing")
                    .Document($"{channelId}_{_userId}")
                    .DeleteAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to clear typing indicator: {e.Message}");
            }
        }
#else
        public void StopTyping(string channelId)
        {
            // No-op in stub mode
        }
#endif

        /// <summary>
        /// Subscribe to typing indicators for a channel
        /// </summary>
#if FIREBASE_ENABLED
        public void SubscribeToTypingIndicators(string channelId)
        {
            _db.Collection("chat_typing")
                .WhereEqualTo("channelId", channelId)
                .Listen(snapshot =>
                {
                    var typingUserIds = new HashSet<string>();
                    
                    foreach (var doc in snapshot.Documents)
                    {
                        var userId = doc.GetValue<string>("userId");
                        if (userId != _userId)
                        {
                            typingUserIds.Add(userId);
                            _typingTimestamps[$"{channelId}_{userId}"] = DateTime.UtcNow;
                        }
                    }
                    
                    _typingUsers[channelId] = typingUserIds;
                    OnTypingUsersUpdated?.Invoke(channelId, typingUserIds.ToList());
                });
        }
#else
        public void SubscribeToTypingIndicators(string channelId)
        {
            // No-op in stub mode
        }
#endif

        /// <summary>
        /// Mark channel as read
        /// </summary>
#if FIREBASE_ENABLED
        private async void MarkChannelAsRead(string channelId)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("markChannelAsRead");
                var data = new Dictionary<string, object> { { "channelId", channelId } };
                await callable.CallAsync(data);

                // Update local state
                var channel = _channels.FirstOrDefault(c => c.Id == channelId);
                if (channel != null)
                {
                    channel.UnreadCount = 0;
                    OnChannelsUpdated?.Invoke(_channels);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to mark channel as read: {e.Message}");
            }
        }
#else
        private void MarkChannelAsRead(string channelId)
        {
            var channel = _channels.FirstOrDefault(c => c.Id == channelId);
            if (channel != null)
            {
                channel.UnreadCount = 0;
                OnChannelsUpdated?.Invoke(_channels);
            }
        }
#endif

        /// <summary>
        /// Get cached messages for a channel
        /// </summary>
        public List<ChatMessage> GetCachedMessages(string channelId)
        {
            return _messageCache.TryGetValue(channelId, out var messages) 
                ? messages 
                : new List<ChatMessage>();
        }

        /// <summary>
        /// Get total unread count across all channels
        /// </summary>
        public int GetTotalUnreadCount()
        {
            return _channels.Sum(c => c.UnreadCount);
        }

        /// <summary>
        /// Search messages in a channel
        /// </summary>
#if FIREBASE_ENABLED
        public async Task<List<ChatMessage>> SearchMessages(string channelId, string query)
        {
            try
            {
                var callable = _functions.GetHttpsCallable("searchChatMessages");
                var data = new Dictionary<string, object>
                {
                    { "channelId", channelId },
                    { "query", query }
                };
                var result = await callable.CallAsync(data);
                
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data.ToString());
                return JsonConvert.DeserializeObject<List<ChatMessage>>(
                    JsonConvert.SerializeObject(response["messages"]));
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to search messages: {e.Message}");
                return new List<ChatMessage>();
            }
        }
#else
        public Task<List<ChatMessage>> SearchMessages(string channelId, string query)
        {
            Debug.LogWarning("[ChatManager] SearchMessages called but Firebase SDK not installed.");
            return Task.FromResult(new List<ChatMessage>());
        }
#endif

        private void CleanupTypingIndicators()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = new List<string>();

            foreach (var kvp in _typingTimestamps)
            {
                if ((now - kvp.Value).TotalSeconds > typingIndicatorTimeoutSeconds)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                _typingTimestamps.Remove(key);
                
                var parts = key.Split('_');
                if (parts.Length >= 2)
                {
                    var channelId = parts[0];
                    var userId = parts[1];
                    
                    if (_typingUsers.TryGetValue(channelId, out var users))
                    {
                        users.Remove(userId);
                        OnTypingUsersUpdated?.Invoke(channelId, users.ToList());
                    }
                }
            }
        }

#if FIREBASE_ENABLED
        private ChatChannel ParseChannel(DocumentSnapshot doc)
        {
            return new ChatChannel
            {
                Id = doc.Id,
                Name = doc.GetValue<string>("name"),
                Type = Enum.TryParse<ChatChannelType>(doc.GetValue<string>("type"), out var type) 
                    ? type : ChatChannelType.Global,
                MemberIds = doc.ContainsField("memberIds") 
                    ? doc.GetValue<List<string>>("memberIds") 
                    : new List<string>(),
                LastMessageContent = doc.ContainsField("lastMessageContent") 
                    ? doc.GetValue<string>("lastMessageContent") 
                    : "",
                LastMessageTime = doc.ContainsField("lastMessageAt") 
                    ? doc.GetValue<Timestamp>("lastMessageAt").ToDateTime() 
                    : DateTime.MinValue,
                Metadata = doc.ContainsField("metadata") 
                    ? doc.GetValue<Dictionary<string, object>>("metadata") 
                    : new Dictionary<string, object>()
            };
        }

        private ChatMessage ParseMessage(DocumentSnapshot doc)
        {
            return new ChatMessage
            {
                Id = doc.Id,
                ChannelId = doc.GetValue<string>("channelId"),
                SenderId = doc.GetValue<string>("senderId"),
                SenderName = doc.GetValue<string>("senderName"),
                SenderAvatarUrl = doc.ContainsField("senderAvatarUrl") 
                    ? doc.GetValue<string>("senderAvatarUrl") 
                    : "",
                Content = doc.GetValue<string>("content"),
                Type = Enum.TryParse<ChatMessageType>(doc.GetValue<string>("type"), out var type) 
                    ? type : ChatMessageType.Text,
                Metadata = doc.ContainsField("metadata") 
                    ? doc.GetValue<Dictionary<string, object>>("metadata") 
                    : new Dictionary<string, object>(),
                Timestamp = doc.GetValue<Timestamp>("createdAt").ToDateTime(),
                IsDeleted = doc.ContainsField("isDeleted") && doc.GetValue<bool>("isDeleted"),
                Reactions = doc.ContainsField("reactions") 
                    ? doc.GetValue<List<string>>("reactions") 
                    : new List<string>()
            };
        }
#endif
    }
}
