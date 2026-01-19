using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ApexCitadels.Core;
using ApexCitadels.Player;
#if FIREBASE_ENABLED
using Firebase.Firestore;
using Firebase.Extensions;
#endif

namespace ApexCitadels.Leaderboard
{
    /// <summary>
    /// Leaderboard types
    /// </summary>
    public enum LeaderboardType
    {
        Global,             // All players worldwide
        Regional,           // Players in same city/region
        Alliance,           // Alliance rankings
        Weekly,             // Weekly competition
        AllTime             // All-time stats
    }

    /// <summary>
    /// Leaderboard category for filtering
    /// </summary>
    public enum LeaderboardCategory
    {
        TotalXP,
        TerritoriesOwned,
        TerritoriesCaptured,
        AttacksWon,
        DefensesWon,
        BuildingsPlaced,
        ResourcesCollected
    }

    /// <summary>
    /// Leaderboard entry
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public int Rank;
        public string PlayerId;
        public string PlayerName;
        public string AllianceTag;
        public int Level;
        public long Score;
        public string Region;
        public DateTime LastActive;

        // Display
        public bool IsCurrentPlayer;
        public int RankChange; // Positive = moved up, negative = moved down
    }

    /// <summary>
    /// Manages leaderboards and rankings
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int entriesPerPage = 100;
        [SerializeField] private float refreshInterval = 300f; // 5 minutes

        // Events
        public event Action<List<LeaderboardEntry>> OnLeaderboardLoaded;
        public event Action<int> OnPlayerRankUpdated;

        // State
        private Dictionary<LeaderboardType, List<LeaderboardEntry>> _cachedLeaderboards;
        private DateTime _lastRefresh;
        private int _currentPlayerRank = -1;

        public int CurrentPlayerRank => _currentPlayerRank;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _cachedLeaderboards = new Dictionary<LeaderboardType, List<LeaderboardEntry>>();
        }

        #region Loading Leaderboards

        /// <summary>
        /// Get global leaderboard
        /// </summary>
        public async Task<List<LeaderboardEntry>> GetGlobalLeaderboard(LeaderboardCategory category = LeaderboardCategory.TotalXP, int limit = 100)
        {
            // Check cache
            if (_cachedLeaderboards.TryGetValue(LeaderboardType.Global, out var cached) &&
                (DateTime.UtcNow - _lastRefresh).TotalSeconds < refreshInterval)
            {
                return cached;
            }

            var entries = await LoadLeaderboardFromCloud(LeaderboardType.Global, category, limit);
            _cachedLeaderboards[LeaderboardType.Global] = entries;
            _lastRefresh = DateTime.UtcNow;

            // Find current player rank
            string playerId = PlayerManager.Instance?.GetCurrentPlayerId() ?? "";
            var playerEntry = entries.Find(e => e.PlayerId == playerId);
            if (playerEntry != null)
            {
                _currentPlayerRank = playerEntry.Rank;
                OnPlayerRankUpdated?.Invoke(_currentPlayerRank);
            }

            OnLeaderboardLoaded?.Invoke(entries);
            return entries;
        }

        /// <summary>
        /// Get regional leaderboard (players nearby)
        /// </summary>
        public async Task<List<LeaderboardEntry>> GetRegionalLeaderboard(string region, int limit = 100)
        {
            var entries = await LoadLeaderboardFromCloud(LeaderboardType.Regional, LeaderboardCategory.TotalXP, limit, region);
            _cachedLeaderboards[LeaderboardType.Regional] = entries;
            return entries;
        }

        /// <summary>
        /// Get alliance rankings
        /// </summary>
        public async Task<List<LeaderboardEntry>> GetAllianceLeaderboard(int limit = 100)
        {
            var entries = await LoadLeaderboardFromCloud(LeaderboardType.Alliance, LeaderboardCategory.TotalXP, limit);
            _cachedLeaderboards[LeaderboardType.Alliance] = entries;
            return entries;
        }

        /// <summary>
        /// Get weekly competition leaderboard
        /// </summary>
        public async Task<List<LeaderboardEntry>> GetWeeklyLeaderboard(int limit = 100)
        {
            var entries = await LoadLeaderboardFromCloud(LeaderboardType.Weekly, LeaderboardCategory.TerritoriesCaptured, limit);
            _cachedLeaderboards[LeaderboardType.Weekly] = entries;
            return entries;
        }

        /// <summary>
        /// Get entries around current player
        /// </summary>
        public async Task<List<LeaderboardEntry>> GetEntriesAroundPlayer(LeaderboardType type, int range = 10)
        {
            string playerId = PlayerManager.Instance?.GetCurrentPlayerId() ?? "";
            
            // Find player's position first
            int playerRank = await GetPlayerRank(playerId, type);
            if (playerRank < 0) return new List<LeaderboardEntry>();

            int startRank = Math.Max(1, playerRank - range);
            int endRank = playerRank + range;

            return await LoadLeaderboardRange(type, startRank, endRank);
        }

        #endregion

        #region Player Stats

        /// <summary>
        /// Get current player's rank in a leaderboard
        /// </summary>
        public async Task<int> GetPlayerRank(string playerId, LeaderboardType type)
        {
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                
                // Get player's score
                var playerDoc = await db.Collection("users").Document(playerId).GetSnapshotAsync();
                if (!playerDoc.Exists) return -1;
                
                long playerScore = playerDoc.TryGetValue<long>("experience", out var score) ? score : 0;
                
                // Count how many players have higher scores
                var query = db.Collection("users")
                    .WhereGreaterThan("experience", playerScore);
                var snapshot = await query.GetSnapshotAsync();
                
                int rank = snapshot.Count + 1;
                _currentPlayerRank = rank;
                OnPlayerRankUpdated?.Invoke(_currentPlayerRank);
                
                return rank;
            }
            catch (Exception ex)
            {
                ApexLogger.LogWarning($"Failed to get player rank: {ex.Message}", LogCategory.Network);
                return _currentPlayerRank;
            }
#else
            await Task.Delay(50);
            return _currentPlayerRank;
#endif
        }

        /// <summary>
        /// Submit score to leaderboard
        /// </summary>
        public async Task SubmitScore(LeaderboardCategory category, long score)
        {
            string playerId = PlayerManager.Instance?.GetCurrentPlayerId() ?? "";
            string playerName = PlayerManager.Instance?.CurrentPlayer?.DisplayName ?? "Unknown";
            string allianceTag = Alliance.AllianceManager.Instance?.CurrentAlliance?.Tag ?? "";

            var entry = new LeaderboardEntry
            {
                PlayerId = playerId,
                PlayerName = playerName,
                AllianceTag = allianceTag,
                Level = PlayerManager.Instance?.CurrentPlayer?.Level ?? 1,
                Score = score,
                LastActive = DateTime.UtcNow
            };

            await SaveScoreToCloud(category, entry);
            ApexLogger.Log($"Submitted score: {score} for {category}", LogCategory.Network);
        }

        /// <summary>
        /// Update all player stats on leaderboards
        /// </summary>
        public async void UpdateAllStats()
        {
            var player = PlayerManager.Instance?.CurrentPlayer;
            if (player == null) return;

            await SubmitScore(LeaderboardCategory.TotalXP, player.TotalExperience);
            await SubmitScore(LeaderboardCategory.TerritoriesOwned, player.TerritoriesOwned);
            await SubmitScore(LeaderboardCategory.TerritoriesCaptured, player.TotalTerritoriesCaptured);
            await SubmitScore(LeaderboardCategory.AttacksWon, player.AttacksWon);
            await SubmitScore(LeaderboardCategory.DefensesWon, player.DefensesWon);
            await SubmitScore(LeaderboardCategory.BuildingsPlaced, player.BuildingsPlaced);

            ApexLogger.Log("All stats updated", LogCategory.Network);
        }

        #endregion

        #region Weekly Competition

        /// <summary>
        /// Get time remaining in current weekly competition
        /// </summary>
        public TimeSpan GetWeeklyTimeRemaining()
        {
            // Week resets Sunday at midnight UTC
            DateTime now = DateTime.UtcNow;
            int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilSunday == 0 && now.Hour > 0) daysUntilSunday = 7;
            
            DateTime nextReset = now.Date.AddDays(daysUntilSunday);
            return nextReset - now;
        }

        /// <summary>
        /// Get weekly rewards based on rank
        /// </summary>
        public Dictionary<string, int> GetWeeklyRewards(int rank)
        {
            var rewards = new Dictionary<string, int>();

            if (rank == 1)
            {
                rewards["Gems"] = 500;
                rewards["Crystal"] = 1000;
                rewards["XP"] = 5000;
            }
            else if (rank <= 3)
            {
                rewards["Gems"] = 250;
                rewards["Crystal"] = 500;
                rewards["XP"] = 2500;
            }
            else if (rank <= 10)
            {
                rewards["Gems"] = 100;
                rewards["Crystal"] = 250;
                rewards["XP"] = 1000;
            }
            else if (rank <= 50)
            {
                rewards["Gems"] = 50;
                rewards["Crystal"] = 100;
                rewards["XP"] = 500;
            }
            else if (rank <= 100)
            {
                rewards["Gems"] = 20;
                rewards["Crystal"] = 50;
                rewards["XP"] = 250;
            }

            return rewards;
        }

        #endregion

        #region Cloud Operations

        private async Task<List<LeaderboardEntry>> LoadLeaderboardFromCloud(
            LeaderboardType type, 
            LeaderboardCategory category, 
            int limit,
            string region = "")
        {
            var entries = new List<LeaderboardEntry>();
            string currentPlayerId = PlayerManager.Instance?.GetCurrentPlayerId() ?? "";

#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                
                // Build query based on category
                string scoreField = category switch
                {
                    LeaderboardCategory.TotalXP => "experience",
                    LeaderboardCategory.TerritoriesOwned => "territoriesOwned",
                    LeaderboardCategory.TerritoriesCaptured => "territoriesConquered",
                    LeaderboardCategory.AttacksWon => "attacksWon",
                    LeaderboardCategory.DefensesWon => "defensesWon",
                    LeaderboardCategory.BuildingsPlaced => "buildingsPlaced",
                    _ => "experience"
                };
                
                Query query = db.Collection("users")
                    .OrderByDescending(scoreField)
                    .Limit(limit);
                
                // Add region filter for regional leaderboard
                if (type == LeaderboardType.Regional && !string.IsNullOrEmpty(region))
                {
                    query = db.Collection("users")
                        .WhereEqualTo("region", region)
                        .OrderByDescending(scoreField)
                        .Limit(limit);
                }
                
                var snapshot = await query.GetSnapshotAsync();
                
                int rank = 1;
                foreach (var doc in snapshot.Documents)
                {
                    long score = doc.TryGetValue<long>(scoreField, out var s) ? s : 0;
                    
                    entries.Add(new LeaderboardEntry
                    {
                        Rank = rank,
                        PlayerId = doc.Id,
                        PlayerName = doc.GetValue<string>("displayName") ?? "Unknown",
                        AllianceTag = doc.GetValue<string>("allianceTag") ?? "",
                        Level = doc.TryGetValue<long>("level", out var lvl) ? (int)lvl : 1,
                        Score = score,
                        Region = doc.GetValue<string>("region") ?? "",
                        IsCurrentPlayer = doc.Id == currentPlayerId,
                        RankChange = 0 // Would need to track previous ranks
                    });
                    rank++;
                }
                
                ApexLogger.Log($"Loaded {entries.Count} leaderboard entries from Firebase", LogCategory.Network);
            }
            catch (Exception ex)
            {
                ApexLogger.LogWarning($"Failed to load leaderboard: {ex.Message}", LogCategory.Network);
                // Fall through to generate placeholder data
            }
#endif

            // If no Firebase data, generate placeholder entries
            if (entries.Count == 0)
            {
                for (int i = 0; i < Math.Min(limit, 10); i++)
                {
                    bool isPlayer = (i == 0 && !string.IsNullOrEmpty(currentPlayerId));
                    entries.Add(new LeaderboardEntry
                    {
                        Rank = i + 1,
                        PlayerId = isPlayer ? currentPlayerId : $"player_{i}",
                        PlayerName = isPlayer ? 
                            (PlayerManager.Instance?.CurrentPlayer?.DisplayName ?? "You") : 
                            $"Player{i + 1}",
                        AllianceTag = "",
                        Level = 1,
                        Score = 0,
                        IsCurrentPlayer = isPlayer,
                        RankChange = 0
                    });
                }
            }

            return entries;
        }

        private async Task<List<LeaderboardEntry>> LoadLeaderboardRange(LeaderboardType type, int startRank, int endRank)
        {
#if FIREBASE_ENABLED
            // Firestore doesn't support OFFSET, so we'd need to use cursor pagination
            // For now, return empty - would implement with startAfter/endBefore
            await Task.CompletedTask;
            return new List<LeaderboardEntry>();
#else
            await Task.Delay(50);
            return new List<LeaderboardEntry>();
#endif
        }

        private async Task SaveScoreToCloud(LeaderboardCategory category, LeaderboardEntry entry)
        {
#if FIREBASE_ENABLED
            try
            {
                var db = FirebaseFirestore.DefaultInstance;
                var docRef = db.Collection("users").Document(entry.PlayerId);
                
                // Update the relevant stat field
                string fieldName = category switch
                {
                    LeaderboardCategory.TotalXP => "experience",
                    LeaderboardCategory.TerritoriesOwned => "territoriesOwned",
                    LeaderboardCategory.TerritoriesCaptured => "territoriesConquered",
                    LeaderboardCategory.AttacksWon => "attacksWon",
                    LeaderboardCategory.DefensesWon => "defensesWon",
                    LeaderboardCategory.BuildingsPlaced => "buildingsPlaced",
                    _ => "experience"
                };
                
                await docRef.UpdateAsync(new Dictionary<string, object>
                {
                    { fieldName, entry.Score },
                    { "lastActiveAt", FieldValue.ServerTimestamp }
                });
                
                ApexLogger.Log($"Score saved for {category}", LogCategory.Network);
            }
            catch (Exception ex)
            {
                ApexLogger.LogWarning($"Failed to save score: {ex.Message}", LogCategory.Network);
            }
#else
            await Task.Delay(50);
#endif
        }

        #endregion

        #region Force Refresh

        /// <summary>
        /// Force refresh all leaderboards
        /// </summary>
        public async void ForceRefresh()
        {
            _lastRefresh = DateTime.MinValue;
            _cachedLeaderboards.Clear();
            await GetGlobalLeaderboard();
            ApexLogger.Log("Leaderboards refreshed", LogCategory.Network);
        }

        #endregion
    }
}
